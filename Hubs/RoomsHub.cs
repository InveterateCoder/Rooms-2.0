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
                        messages.InsertRange(0, room.Messages.Where(m =>
                            m.TimeStamp < oldestMsgTime &&
                            (m.AccessIdsJson == null || (id.UserId != 0 && m.AccessIds.Contains(id.UserId))))
                            .OrderByDescending(m => m.TimeStamp).Select(m => new RoomsMsg
                            {
                                UserId = m.UserId,
                                UserGuid = m.GUID,
                                Icon = m.SenderIcon,
                                Secret = m.AccessIdsJson != null,
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
        public async Task SendMessage(string message, long[] accessIds)
        {
            await Task.Run(async () =>
                {
                    Identity id = JsonSerializer.Deserialize<Identity>(Context.User.Identity.Name);
                    IEnumerable<long> ids = null;
                    if (accessIds != null && id.UserId > 0)
                        ids = accessIds.Append(id.UserId);
                    var data = _state.SendMessage(Context.ConnectionId, message, ids?.ToArray(), id.UserId, id.Guest);
                    if (data.connectionIds.Length > 0)
                        await Clients.Clients(data.connectionIds).SendAsync("recieveMessage", data.message);
                    if (data.room.MsgCount > 50) await SaveRoom(data.room);
                });
        }
        public async Task<string[]> ConnectVoice()
        {
            return await Task.Run(async () =>
            {
                Identity id = JsonSerializer.Deserialize<Identity>(Context.User.Identity.Name);
                (var voiceUsers, var count) = _state.ConnectVoiceUser(id.UserId, id.Guest, Context.ConnectionId);
                if (voiceUsers == null) throw new HubException("active");
                await Clients.Clients(_state.Connections(Context.ConnectionId)).SendAsync("voiceCount", count);
                return voiceUsers;
            });
        }
        public async Task DisconnectVoice()
        {
            await Task.Run(async () =>
            {
                Identity id = JsonSerializer.Deserialize<Identity>(Context.User.Identity.Name);
                var count = _state.DisconnectVoiceUser(id.UserId, id.Guest, Context.ConnectionId);
                if (count > -1)
                    await Clients.Clients(_state.Connections(Context.ConnectionId)).SendAsync("voiceCount", count);
            });
        }
        public async Task PipeCandidate(string connectionId, object candidate)
        {
            await Clients.Client(connectionId).SendAsync("candidate", Context.ConnectionId, candidate);
        }
        public async Task PipeOffer(string connectionId, object offer)
        {
            await Clients.Client(connectionId).SendAsync("offer", Context.ConnectionId, offer);
        }
        public async Task PipeAnswer(string connectionId, object answer)
        {
            await Clients.Client(connectionId).SendAsync("answer", Context.ConnectionId, answer);
        }
        public async Task<ReturnSignal<RoomInfo>> Enter(string slug, string icon, string password, int msgsCount)
        {
            try
            {
                if (icon != "man" && icon != "woman" && icon != "user") throw new HubException("Wrong icon name");
                Identity id = JsonSerializer.Deserialize<Identity>(Context.User.Identity.Name);
                return await Task.Run(async () =>
                {
                    var room = _context.Rooms.Include(r => r.Messages).FirstOrDefault(r => r.Slug == slug);
                    if (room == null) return new ReturnSignal<RoomInfo> { Code = "noroom" };
                    var isAdmin = room.UserId == id.UserId;
                    if (!isAdmin && room.Password != password)
                    {
                        if (!_state._waitingPassword.ContainsKey(Context.ConnectionId))
                            _state._waitingPassword[Context.ConnectionId] = (id.UserId, id.Guest);
                        return new ReturnSignal<RoomInfo> { Code = "password" };
                    }
                    else _state._waitingPassword.TryRemove(Context.ConnectionId, out _);
                    var ip = Context.GetHttpContext().Connection.RemoteIpAddress;
                    ActiveRoom active = _state.ConnectUser(ip, room.UserId, id.UserId, id.Guest, id.Name, icon, Context.ConnectionId, room.RoomId, room.Limit);
                    if (active == null) return new ReturnSignal<RoomInfo> { Code = "limit" };
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
                    {
                        Func<InMemoryMessage, bool> filter;
                        if (id.UserId > 0) filter = m => m.accessIds == null || m.accessIds.Contains(id.UserId);
                        else if (id.Guest != null) filter = m => m.accessIds == null;
                        else throw new HubException("Couldn't read neither id nor guid.");
                        messages.AddRange(active.GetMessages(id.UserId, id.Guest));
                    }
                    var remnant = msgsCount - active.MsgCount;
                    if (remnant > 0 && room.Messages.Count() > 0)
                    {
                        var filtered = room.Messages.Where(m => m.AccessIdsJson == null ||
                            (id.UserId != 0 && m.AccessIds.Contains(id.UserId)))
                            .OrderByDescending(m => m.TimeStamp);
                        messages.AddRange(filtered.Take(remnant)
                            .Select(m => new RoomsMsg
                            {
                                UserId = m.UserId,
                                UserGuid = m.GUID,
                                Icon = m.SenderIcon,
                                Sender = m.SenderName,
                                Time = m.TimeStamp,
                                Text = m.Text,
                                Secret = m.AccessIdsJson != null
                            }));
                    }
                    info.Users = active.Users(id.UserId, id.Guest);
                    info.Messages = messages;
                    info.VoiceUserCount = active.VoiceUsersCount;
                    if (active.GetOpenConnections(id.UserId, id.Guest) <= 1)
                    {
                        var connectionIds = active.GetConnections(id.UserId, id.Guest);
                        if (connectionIds.Length > 0)
                            await Clients.Clients(connectionIds)
                                .SendAsync("addUser", new RoomsUser { Id = id.UserId, Guid = id.Guest, Icon = icon, Name = id.Name });
                    }
                    return new ReturnSignal<RoomInfo>
                    {
                        Code = "ok",
                        Payload = info
                    };
                });
            }
            catch (Exception ex)
            {
                return new ReturnSignal<RoomInfo> { Code = ex.Message };
            }
        }
        public async Task BanUser(long id, string guid, int minutes)
        {
            await Task.Run(async () =>
            {
                try
                {
                    Identity _id = JsonSerializer.Deserialize<Identity>(Context.User.Identity.Name);
                    if (_id.UserId == 0 || minutes > 120) return;
                    var users = _state.AdministrateUser(_id.UserId, Context.ConnectionId, id, guid);
                    if (users.Length > 0)
                        await Clients.Clients(users).SendAsync("ban", minutes);
                }
                catch (Exception ex)
                {
                    throw new HubException(ex.Message);
                }
            });
        }
        public async Task MuteUser(long id, string guid, int minutes)
        {
            await Task.Run(async () =>
            {
                try
                {
                    Identity _id = JsonSerializer.Deserialize<Identity>(Context.User.Identity.Name);
                    if (_id.UserId == 0 || minutes > 120) return;
                    var users = _state.AdministrateUser(_id.UserId, Context.ConnectionId, id, guid);
                    if (users.Length > 0)
                        await Clients.Clients(users).SendAsync("mute", minutes);
                }
                catch (Exception ex)
                {
                    throw new HubException(ex.Message);
                }
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
        public async override Task OnDisconnectedAsync(Exception exception)
        {
            await Task.Run(async () =>
            {
                Identity id = JsonSerializer.Deserialize<Identity>(Context.User.Identity.Name);
                var count = _state.DisconnectVoiceUser(id.UserId, id.Guest, Context.ConnectionId);
                if (count > -1)
                    await Clients.Clients(_state.Connections(Context.ConnectionId)).SendAsync("voiceCount", count);
                _state._waitingPassword.TryRemove(Context.ConnectionId, out _);
                var data = _state.DisconnectUser(Context.ConnectionId);
                if (data.room != null)
                {
                    if (!data.removed)
                    {
                        if (data.room.User(id.UserId, id.Guest) == null)
                        {
                            var connectionIds = data.room.GetConnections();
                            if (connectionIds.Length > 0)
                                await Clients.Clients(connectionIds).SendAsync("removeUser",
                                    new RoomsUser { Id = id.UserId, Guid = id.Guest, Icon = null, Name = id.Name });
                        }
                    }
                    else await SaveRoom(data.room);
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