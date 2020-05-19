using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Rooms.Models;

namespace Rooms.Hubs
{
    [Authorize]
    public class RoomsHub : Hub
    {
        private RoomsDBContext _context;
        private State _state;
        public RoomsHub(RoomsDBContext context, State state)
        {
            _context = context;
            _state = state;
        }
        public async Task<IEnumerable<RoomsMsg>> GetMessages(long oldestMsgTime, int count)
        {

            return await Task.Run(async () =>
                {
                    Identity id = JsonSerializer.Deserialize<Identity>(Context.User.Identity.Name);
                    List<RoomsMsg> messages = new List<RoomsMsg>();
                    messages.AddRange(_state.GetOlderMsgs(id.UserId, id.Guest, oldestMsgTime, Context.ConnectionId));
                    if (messages.Count < count)
                    {
                        var roomId = _state.GetRoomId(Context.ConnectionId);
                        var room = await _context.Rooms.Include(r => r.Messages).FirstAsync(r => r.RoomId == roomId);
                        messages.InsertRange(0, room.Messages.Where(m => m.TimeStamp < oldestMsgTime)
                            .OrderByDescending(m => m.TimeStamp).Select(m => new RoomsMsg
                            {
                                UserId = m.UserId,
                                UserGuid = m.GUID,
                                Icon = m.SenderIcon,
                                Secret = false,
                                Sender = m.SenderName,
                                Text = m.Text,
                                Time = m.TimeStamp
                            }).Take(count - messages.Count));
                    }
                    else if (messages.Count > count)
                        return messages.Take(count);
                    return messages;
                });
        }
        public async Task SendMessage(string message, string[] connections)
        {
            await Task.Run(async () =>
                {
                    if (message == null || message.Length == 0) throw new HubException("Wrong message.");
                    Identity id = JsonSerializer.Deserialize<Identity>(Context.User.Identity.Name);
                    var data = _state.SendMessage(Context.ConnectionId, message, id.UserId, id.Guest, connections);
                    switch (data.status)
                    {
                        case UserMsg.Status.Ok:
                            if (data.connectionIds.Length > 0)
                                try
                                {
                                    await Clients.Clients(data.connectionIds).SendAsync("recieveMessage", data.message);
                                }
                                catch { };
                            if (data.room.MsgCount > 50) await SaveRoom(data.room);
                            break;
                        case UserMsg.Status.Muted:
                            throw new HubException($"min:{(int)data.timeRemains.TotalMinutes} sec:{data.timeRemains.Seconds}");
                        case UserMsg.Status.Warn:
                            try
                            {
                                await Clients.Caller.SendAsync("spamwarn");
                            }
                            catch { };
                            break;
                        case UserMsg.Status.Ban:
                            var user = _state.AddGlobalBan(Context.ConnectionId);
                            if (user != null)
                            {
                                try
                                {
                                    await Clients.Clients(user.ConnectionId).SendAsync("spamban");
                                }
                                catch { };
                                user.Abort();
                            }
                            else Context.Abort();
                            break;
                        case UserMsg.Status.Banned:
                            Context.Abort();
                            break;
                        case UserMsg.Status.NoRoom:
                            throw new HubException("Something went terribly wrong.");
                    }
                });
        }
        public async Task<string[]> ConnectVoice()
        {
            return await Task.Run(async () =>
            {
                (var users, var count) = _state.ConnectVoiceUser(Context.ConnectionId);
                if (users == null) throw new HubException("Failed to perform connection.");
                try
                {
                    await Clients.Clients(users).SendAsync("voiceCount", count);
                }
                catch { };
                return users;
            });
        }
        public async Task DisconnectVoice()
        {
            await Task.Run(async () =>
            {
                Identity id = JsonSerializer.Deserialize<Identity>(Context.User.Identity.Name);
                var count = _state.DisconnectVoiceUser(Context.ConnectionId);
                if (count > -1)
                    try
                    {
                        List<string> connections = new List<string>(_state.Others(Context.ConnectionId));
                        connections.Add(Context.ConnectionId);
                        await Clients.Clients(connections).SendAsync("voiceCount", count);
                    }
                    catch { };
            });
        }
        public async Task PipeCandidate(string connectionId, object candidate)
        {
            try
            {
                await Clients.Client(connectionId).SendAsync("candidate", Context.ConnectionId, candidate);
            }
            catch { };
        }
        public async Task PipeOffer(string connectionId, object offer)
        {
            try
            {
                await Clients.Client(connectionId).SendAsync("offer", Context.ConnectionId, offer);
            }
            catch { };
        }
        public async Task PipeAnswer(string connectionId, object answer)
        {
            try
            {
                await Clients.Client(connectionId).SendAsync("answer", Context.ConnectionId, answer);
            }
            catch { };
        }
        public async Task<RoomInfo> Enter(string slug, string icon, string password, int msgsCount)
        {
            var ip = Context.GetHttpContext().Connection.RemoteIpAddress;
            if (icon != "man" && icon != "woman" && icon != "user") throw new HubException("Wrong icon name");
            Identity id = JsonSerializer.Deserialize<Identity>(Context.User.Identity.Name);
            return await Task.Run(async () =>
            {
                var room = _context.Rooms.Include(r => r.Messages).FirstOrDefault(r => r.Slug == slug);
                if (room == null)
                {
                    Context.Abort();
                    return null;
                }
                var isAdmin = room.UserId == id.UserId;
                if (!isAdmin && room.Password != password)
                {
                    try
                    {
                        await Clients.Caller.SendAsync("block");
                    }
                    catch { };
                    Context.Abort();
                    return null;
                }
                ActiveRoomInfo roomInfo = _state.ConnectUser(ip, Context, room.UserId, id.UserId, id.Guest, id.Name, icon, Context.ConnectionId, room.RoomId, room.Limit);
                if (roomInfo._status == ActiveRoomInfo.Status.Banned)
                {
                    try
                    {
                        await Clients.Caller.SendAsync("ban", $"min:{(int)roomInfo._timeRemains.TotalMinutes} sec:{roomInfo._timeRemains.Seconds}");
                    }
                    catch { };
                    Context.Abort();
                    return null;
                }
                else
                {
                    switch (roomInfo._addStatus)
                    {
                        case AddUserStatus.Active:
                            try
                            {
                                await Clients.Caller.SendAsync("active");
                            }
                            catch { };
                            Context.Abort();
                            return null;
                        case AddUserStatus.Limit:
                            try
                            {
                                await Clients.Caller.SendAsync("limit", room.Limit);
                            }
                            catch { };
                            Context.Abort();
                            return null;
                        case AddUserStatus.Failed:
                            Context.Abort();
                            return null;
                    }
                }
                ActiveRoom active = roomInfo._room;
                RoomInfo info = new RoomInfo()
                {
                    RoomId = room.RoomId,
                    MyId = id.UserId,
                    Flag = room.Country,
                    Name = room.Name,
                    IsAdmin = isAdmin
                };
                List<RoomsMsg> messages = new List<RoomsMsg>();
                if (active.MsgCount > 0)
                    messages.AddRange(active.GetMessages());

                var remnant = msgsCount - active.MsgCount;
                if (remnant > 0 && room.Messages.Count() > 0)
                {
                    messages.AddRange(room.Messages.OrderByDescending(m => m.TimeStamp).Take(remnant)
                        .Select(m => new RoomsMsg
                        {
                            UserId = m.UserId,
                            UserGuid = m.GUID,
                            Icon = m.SenderIcon,
                            Sender = m.SenderName,
                            Time = m.TimeStamp,
                            Text = m.Text,
                            Secret = false
                        }));
                }
                info.Users = active.GetUsers(Context.ConnectionId).Select(u => new RoomsUser
                {
                    Id = u._id,
                    Guid = u._guid,
                    ConnectionId = u._context.ConnectionId,
                    Icon = u.icon,
                    Name = u.name
                });
                info.Messages = messages;
                info.VoiceUserCount = active.VoiceUsersCount;
                try
                {
                    await Clients.Clients(info.Users.Select(u => u.ConnectionId).ToArray())
                        .SendAsync("addUser", new RoomsUser { ConnectionId = Context.ConnectionId, Id = id.UserId, Guid = id.Guest, Icon = icon, Name = id.Name });
                }
                catch { };
                return info;
            });
        }
        public async Task BanUser(string connectionId, int minutes)
        {
            await Task.Run(async () =>
            {
                Identity _id = JsonSerializer.Deserialize<Identity>(Context.User.Identity.Name);
                if (_id.UserId == 0 || minutes > 120) throw new HubException("Wrong credentials or argument.");
                var user = _state.BanUser(_id.UserId, connectionId, minutes);
                if (user == null) throw new HubException("You must be administrator and user must present.");
                try
                {
                    await Clients.Client(user.ConnectionId).SendAsync("ban", minutes);
                }
                catch { };
                user.Abort();
            });
        }
        public async Task MuteUser(string connectionId, int minutes)
        {
            await Task.Run(async () =>
            {
                Identity _id = JsonSerializer.Deserialize<Identity>(Context.User.Identity.Name);
                if (_id.UserId == 0 || minutes > 120) throw new HubException("Wrong credentials or argument.");
                var user = _state.MuteUser(_id.UserId, connectionId, minutes);
                if (user == null) throw new HubException("You must be administrator and user must present.");
                try
                {
                    await Clients.Client(user.ConnectionId).SendAsync("mute", minutes);
                }
                catch { };
            });
        }
        public async Task ClearMessages(long from, long till)
        {
            await Task.Run(async () =>
            {
                Identity _id = JsonSerializer.Deserialize<Identity>(Context.User.Identity.Name);
                if (_id.UserId == 0) return;
                if (!_state.ClearMessages(_id.UserId, Context.ConnectionId, from, till)) return;
                var room = await _context.Rooms.Include(r => r.Messages).FirstOrDefaultAsync(r => r.UserId == _id.UserId);
                if (room == null) return;
                if (from == 0 && till == 0)
                    _context.Messages.RemoveRange(room.Messages);
                else if (till == 0)
                    _context.Messages.RemoveRange(room.Messages.Where(m => m.TimeStamp > from));
                else
                {
                    if (from > till) return;
                    _context.Messages.RemoveRange(room.Messages.Where(m => m.TimeStamp > from && m.TimeStamp < till));
                }
                await _context.SaveChangesAsync();
            });
        }
        public async override Task OnConnectedAsync()
        {
            await Task.Run(() =>
            {
                Identity id = JsonSerializer.Deserialize<Identity>(Context.User.Identity.Name);
                if (_state.IsGloballyBanned(Context.GetHttpContext().Connection.RemoteIpAddress, id.UserId, id.Guest))
                {
                    Context.Abort();
                    return;
                }
            });
            await base.OnConnectedAsync();
        }
        public async override Task OnDisconnectedAsync(Exception exception)
        {
            await Task.Run(async () =>
            {
                Identity id = JsonSerializer.Deserialize<Identity>(Context.User.Identity.Name);
                var count = _state.DisconnectVoiceUser(Context.ConnectionId);
                try
                {
                    if (count > -1)
                        await Clients.Clients(_state.Others(Context.ConnectionId)).SendAsync("voiceCount", count);
                }
                catch { };
                var data = _state.DisconnectUser(Context.ConnectionId);
                if (data.room != null)
                {
                    if (data.users.Length > 0)
                    {
                        try
                        {
                            await Clients.Clients(data.users).SendAsync("removeUser",
                                new RoomsUser { ConnectionId = Context.ConnectionId, Id = id.UserId, Guid = id.Guest, Icon = null, Name = data.user.name });
                        }
                        catch { };
                    }
                    else
                    {
                        await SaveRoom(data.room);
                        List<UserBanInfo> roomBanInfo;
                        List<UserBanInfo> removeBanInfo = new List<UserBanInfo>();
                        _state._banInfo.TryGetValue(data.room.roomId, out roomBanInfo);
                        if (roomBanInfo != null)
                        {
                            foreach (var info in roomBanInfo)
                            {
                                if (!info.IsActive)
                                    removeBanInfo.Add(info);

                            }
                            if (removeBanInfo.Count > 0)
                                foreach (var info in removeBanInfo)
                                    roomBanInfo.Remove(info);
                            if (roomBanInfo.Count == 0)
                                _state._banInfo.Remove(data.room.roomId, out _);
                        }
                    }
                }
            });
            await base.OnDisconnectedAsync(exception);
        }
        private async Task SaveRoom(ActiveRoom room)
        {
            var messages = room.DumpMessages();
            if (messages.Count > 0)
            {
                await _context.Messages.AddRangeAsync(messages);
                await _context.SaveChangesAsync();
            }
        }
    }
}