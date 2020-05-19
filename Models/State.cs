using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.SignalR;

namespace Rooms.Models
{
    public enum BanType { None, Mute, Ban }
    public class State
    {
        private readonly ConcurrentDictionary<long, ActiveRoom> _activeRooms = new ConcurrentDictionary<long, ActiveRoom>();
        private readonly ConcurrentDictionary<string, long> _activeUsers = new ConcurrentDictionary<string, long>();
        public readonly ConcurrentDictionary<long, List<UserBanInfo>> _banInfo = new ConcurrentDictionary<long, List<UserBanInfo>>();
        public readonly List<UserBanInfo> _globalBanInfo = new List<UserBanInfo>();
        private (BanType type, TimeSpan remains) IsBanned(long roomId, IPAddress ip, long id, string guid)
        {
            List<UserBanInfo> banList;
            if (_banInfo.TryGetValue(roomId, out banList))
            {
                var banner = banList.Find(b => b.IsUser(ip, id, guid));
                if (banner != null)
                {
                    if (banner.IsActive)
                    {
                        return (banner.Type, banner.TimeRemains);
                    }
                    else banList.Remove(banner);
                }
            }
            return (BanType.None, TimeSpan.Zero);
        }
        private void AddBan(BanType type, long roomId, IPAddress ip, long id, string guid, int minutes)
        {
            List<UserBanInfo> banList;
            if (!_banInfo.TryGetValue(roomId, out banList))
            {
                banList = new List<UserBanInfo>();
                _banInfo[roomId] = banList;
            }
            var banner = banList.Find(b => b.IsUser(ip, id, guid));
            if (banner != null)
            {
                banner.ResetTime(minutes);
                banner.Type = type;
            }
            else banList.Add(new UserBanInfo(id, guid, ip, type, minutes));
        }
        public HubCallerContext AddGlobalBan(string connectionId, int minutes = 5)
        {
            var user = GetRoom(connectionId)?.GetUser(connectionId);
            if (user == null) return null;
            var banner = _globalBanInfo.Find(b => b.IsUser(user.ipAddress, user._id, user._guid));
            if (banner != null)
                banner.ResetTime(minutes);
            else _globalBanInfo.Add(new UserBanInfo(user._id, user._guid, user.ipAddress, BanType.Ban, minutes));
            return user._context;
        }
        public bool IsGloballyBanned(IPAddress ip, long id, string guid)
        {
            var banner = _globalBanInfo.Find(b => b.IsUser(ip, id, guid));
            if (banner != null)
            {
                if (banner.IsActive)
                    return true;
                else _globalBanInfo.Remove(banner);
            }
            return false;
        }
        public int RoomsCount { get => _activeRooms.Count; }
        public IEnumerable<long> RoomsKeys { get => _activeRooms.Keys; }
        public long GetRoomId(string connectionId) => _activeUsers[connectionId];
        public ActiveRoom GetRoom(long roomId)
        {
            ActiveRoom room;
            if (_activeRooms.TryGetValue(roomId, out room))
                return room;
            return null;
        }
        private ActiveRoom GetRoom(string connectionId)
        {
            long id;
            if (_activeUsers.TryGetValue(connectionId, out id))
                return GetRoom(id);
            return null;
        }
        public HubCallerContext BanUser(long adminId, string connectionId, int minutes)
        {
            var room = GetRoom(connectionId);
            lock (room)
            {
                if (room == null || room.OwnerId != adminId) return null;
                var user = room.GetUser(connectionId);
                AddBan(BanType.Ban, room.roomId, user.ipAddress, user._id, user._guid, minutes);
                return user._context;
            }
        }
        public HubCallerContext MuteUser(long adminId, string connectionId, int minutes)
        {
            var room = this.GetRoom(connectionId);
            lock (room)
            {
                if (room == null || room.OwnerId != adminId) return null;
                var user = room.GetUser(connectionId);
                AddBan(BanType.Mute, room.roomId, user.ipAddress, user._id, user._guid, minutes);
                return user._context;
            }
        }
        public bool ClearMessages(long adminId, string connectionId, long from, long till)
        {
            var room = this.GetRoom(connectionId);
            if (room.OwnerId != adminId) return false;
            room.ClearMessages(from, till);
            return true;
        }
        public (string[] users, int cout) ConnectVoiceUser(string connectionId)
        {
            var room = this.GetRoom(connectionId);
            return (room?.ConnectVoiceUser(connectionId), room.VoiceUsersCount);
        }
        public int DisconnectVoiceUser(string connectionId)
        {
            var room = this.GetRoom(connectionId);
            return room?.DisconnectVoiceUser(connectionId) ?? -2;
        }
        public List<string> ChangeUser(long userId, string name = null, string icon = null)
        {
            List<string> connectionIds = new List<string>();
            foreach (var room in _activeRooms.Values)
            {
                var user = room.GetUser(id: userId);
                if (user != null)
                {
                    user.name = name ?? user.name;
                    user.icon = icon ?? user.icon;
                    connectionIds.AddRange(room.Users.Select(c => c.ConnectionId));
                }
            }
            return connectionIds;
        }
        public IEnumerable<RoomsMsg> GetOlderMsgs(long userId, string guid, long time, string connectionId) =>
            _activeRooms[_activeUsers[connectionId]].GetMessages().Where(m => m.Time < time);
        public IEnumerable<HubCallerContext> RemoveRoom(long roomId)
        {
            ActiveRoom room = null;
            IEnumerable<HubCallerContext> users = null;
            if (_activeRooms.Remove(roomId, out room))
            {
                lock (room)
                {
                    users = room.Users;
                    foreach (var user in _activeUsers.Where(u => u.Value == roomId))
                        _activeUsers.Remove(user.Key, out _);
                }
            }
            return users;
        }
        public string[] Others(string connectionId)
        {
            ActiveRoom room = GetRoom(connectionId);
            if (room == null) return new string[0];
            return room.Connections.Where(con => con != connectionId).ToArray();
        }
        public List<HubCallerContext> AllConnections(long id, string guid)
        {
            List<HubCallerContext> userList = new List<HubCallerContext>();
            foreach(var room in _activeRooms.Values)
            {
                var user = room.ConnectionByCred(id, guid);
                if(user != null)
                    userList.Add(user);
            }
            return userList;
        }
        public UserMsg SendMessage(string connectionId, string message, long id, string guid, string[] connections)
        {
            var usrMsg = new UserMsg();
            ActiveRoom room = GetRoom(connectionId);
            if (room == null)
                usrMsg.status = UserMsg.Status.NoRoom;
            else
            {
                var banInfo = IsBanned(room.roomId, room.GetUser(connectionId).ipAddress, id, guid);
                if (banInfo.type != BanType.None)
                {
                    usrMsg.status = banInfo.type == BanType.Mute ? UserMsg.Status.Muted : UserMsg.Status.Banned;
                    usrMsg.timeRemains = banInfo.remains;
                }
                else
                {
                    var msginfo = room.AddMessage(connectionId, id, guid, message, connections != null && connections.Length > 0);
                    if (msginfo.status == MessageStatus.Ok)
                    {
                        usrMsg.connectionIds = connections != null && connections.Length > 0 ? connections : room.Connections.Where(con => con != connectionId).ToArray();
                        usrMsg.message = new RoomsMsg
                        {
                            UserId = msginfo.message.userId,
                            UserGuid = msginfo.message.userGuid,
                            Time = msginfo.message.timeStamp,
                            Icon = msginfo.message.senderIcon,
                            Secret = connections != null,
                            Sender = msginfo.message.senderName,
                            Text = msginfo.message.text
                        };
                        usrMsg.room = room;
                    }
                    usrMsg.status = msginfo.status == MessageStatus.Ok ? UserMsg.Status.Ok
                        : msginfo.status == MessageStatus.Warn ? UserMsg.Status.Warn : UserMsg.Status.Ban;
                }
            }
            return usrMsg;
        }
        public ActiveRoomInfo ConnectUser(IPAddress ipAddress, HubCallerContext context, long ownerId, long userId, string guid, string name, string icon, string connectionId, long roomId, byte limit)
        {
            var banner = IsBanned(roomId, ipAddress, userId, guid);
            if (banner.type == BanType.Ban)
                return new ActiveRoomInfo
                {
                    _status = ActiveRoomInfo.Status.Banned,
                    _timeRemains = banner.remains,
                    _room = null
                };
            ActiveRoom room = _activeRooms.GetOrAdd(roomId, new ActiveRoom(roomId, ownerId));
            ActiveRoomInfo info = new ActiveRoomInfo();
            lock (room)
            {
                var status = room.AddUser(ipAddress, context, userId, guid, name, icon, limit);
                if (status == AddUserStatus.Added)
                    _activeUsers[connectionId] = roomId;
                else if (room.Online == 0) _activeRooms.Remove(roomId, out _);
                info._addStatus = status;
            }
            info._status = ActiveRoomInfo.Status.Ok;
            info._room = room;
            return info;
        }
        public UsersRoom DisconnectUser(string connectionId)
        {
            UsersRoom data = new UsersRoom()
            {
                room = null,
                users = null
            };
            var roomId = _activeUsers.GetValueOrDefault(connectionId, 0);
            if (roomId == 0) return data;
            data.room = GetRoom(roomId);
            if (data.room == null)
            {
                _activeUsers.Remove(connectionId, out _);
                return data;
            }
            lock (data.room)
            {
                _activeUsers.Remove(connectionId, out _);
                (data.user, data.users) = data.room.RemoveUser(connectionId);
                if (data.users.Length == 0)
                    _activeRooms.Remove(roomId, out _);
            }
            return data;
        }
    }
    public struct UsersRoom
    {
        public ActiveRoom room;
        public ActiveUser user;
        public string[] users;
    }
    public struct UserMsg
    {
        public enum Status
        {
            Ok, NoRoom, Muted, Warn, Ban, Banned
        }
        public Status status;
        public TimeSpan timeRemains;
        public string[] connectionIds;
        public RoomsMsg message;
        public ActiveRoom room;
    }
    public struct ActiveRoomInfo
    {
        public enum Status
        {
            Ok, Banned
        }
        public Status _status;
        public AddUserStatus _addStatus;
        public TimeSpan _timeRemains;
        public ActiveRoom _room;
    }
    public class UserBanInfo
    {
        private DateTime _setOnTime;
        public long Id { get; set; }
        public string Guid { get; set; }
        public IPAddress Ip { get; set; }
        public BanType Type { get; set; }
        public TimeSpan Minutes { get; set; }
        public UserBanInfo(long id, string guid, IPAddress ip, BanType type, int minutes)
        {
            Id = id; Guid = guid; Ip = ip; Type = type;
            Minutes = TimeSpan.FromMinutes(minutes); _setOnTime = DateTime.UtcNow;
        }
        public void ResetTime(int minutes)
        {
            Minutes = TimeSpan.FromMinutes(minutes);
            _setOnTime = DateTime.UtcNow;
        }
        public bool IsActive
        {
            get => _setOnTime + Minutes > DateTime.UtcNow;
        }
        public TimeSpan TimeRemains
        {
            get => (_setOnTime + Minutes) - DateTime.UtcNow;
        }
        public bool IsUser(IPAddress ip, long id, string guid) =>
            (id == Id && guid == Guid) || ip.Equals(Ip);
    }
}