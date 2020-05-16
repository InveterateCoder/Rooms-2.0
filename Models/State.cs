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
        public readonly ConcurrentDictionary<string, (long id, string guid, HubCallerContext context)> _waitingPassword = new ConcurrentDictionary<string, (long, string, HubCallerContext)>();
        public readonly ConcurrentDictionary<long, List<UserBanInfo>> _banInfo = new ConcurrentDictionary<long, List<UserBanInfo>>();
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
        public ICollection<HubCallerContext> BanUser(long adminId, string connectionId, long id, string guid, int minutes)
        {
            var room = this.GetRoom(connectionId);
            lock (room)
            {
                if (room.OwnerId != adminId) return null;
                this.AddBan(BanType.Ban, room.roomId, room.User(id, guid).ipAddress, id, guid, minutes);
                return room.GetUserHubContexts(id, guid);
            }
        }
        public bool MuteUser(long adminId, string connectionId, long id, string guid, int minutes)
        {
            var room = this.GetRoom(connectionId);
            lock (room)
            {
                if (room.OwnerId != adminId) return false;
                this.AddBan(BanType.Mute, room.roomId, room.User(id, guid).ipAddress, id, guid, minutes);
                return room.GetUserHubContexts(id, guid) != null ? true : false;
            }
        }
        public bool ClearMessages(long adminId, string connectionId, long from, long till)
        {
            var room = this.GetRoom(connectionId);
            if (room.OwnerId != adminId) return false;
            room.ClearMessages(from, till);
            return true;
        }
        public (string[], int) ConnectVoiceUser(long id, string guid, string connectionId)
        {
            var room = this.GetRoom(connectionId);
            return (room.ConnectVoiceUser(id, guid, connectionId), room.VoiceUsersCount);
        }
        public int DisconnectVoiceUser(long id, string guid, string connectionId)
        {
            var room = this.GetRoom(connectionId);
            if (room != null)
                return room.DisconnectVoiceUser(id, guid, connectionId);
            return -2;
        }

        public string[] UserConnections(long userId, string guid = null) =>
            _activeRooms.Values.Where(r => r.User(userId, guid) != null).SelectMany(r => r.GetUserConnections(userId, guid)).ToArray();
        public IEnumerable<HubCallerContext> UserConnectionContexts(long userId, string guid) =>
            _activeRooms.Values.Where(r => r.User(userId, guid) != null).SelectMany(r => r.GetUserHubContexts(userId, guid));
        public string[] Connections(long roomId) => _activeRooms.GetValueOrDefault(roomId)?.GetConnections();
        public string[] Connections(string connectionId) => this.Connections(this._activeUsers[connectionId]);
        public string[] ChangeUser(long userId, string name = null, string icon = null)
        {

            List<string> connectionIds = new List<string>();
            foreach (var room in _activeRooms.Values)
            {
                var user = room.User(userId, null);
                if (user != null)
                {
                    user.name = name ?? user.name;
                    user.icon = icon ?? user.icon;
                    connectionIds.AddRange(room.GetConnections());
                }
            }
            return connectionIds.ToArray();
        }
        public IEnumerable<RoomsMsg> GetOlderMsgs(long userId, string guid, long time, string connectionId) =>
            _activeRooms[_activeUsers[connectionId]].GetMessages(userId, guid).Where(m => m.Time < time);
        public IEnumerable<HubCallerContext> RemoveRoom(long roomId)
        {
            ActiveRoom room = null;
            IEnumerable<HubCallerContext> contexts = null;
            if (_activeRooms.Remove(roomId, out room))
            {
                lock (room)
                {
                    contexts = room.GetUserHubContexts();
                    var users = _activeUsers.Where(u => u.Value == roomId);
                    foreach (var user in users)
                        _activeUsers.Remove(user.Key, out _);
                }
            }
            return contexts;
        }
        public UserMsg SendMessage(string connectionId, string message, long[] accessIds, long id, string guid)
        {
            var usrMsg = new UserMsg();
            ActiveRoom room = GetRoom(connectionId);
            if (room == null)
                usrMsg.status = UserMsg.Status.NoRoom;
            else
            {
                var banInfo = IsBanned(room.roomId, room.User(id, guid).ipAddress, id, guid);
                if (banInfo.type != BanType.None)
                {
                    usrMsg.status = banInfo.type == BanType.Mute ? UserMsg.Status.Muted : UserMsg.Status.Banned;
                    usrMsg.timeRemains = banInfo.remains;
                }
                else
                {
                    var msg = room.AddMessage(room.roomId, connectionId, message, accessIds, id, guid);
                    usrMsg.connectionIds = room.GetConnections(connectionId: connectionId, ids: accessIds);
                    usrMsg.message = new RoomsMsg
                    {
                        UserId = msg.userId,
                        UserGuid = msg.userGuid,
                        Time = msg.timeStamp,
                        Icon = msg.senderIcon,
                        Secret = accessIds != null,
                        Sender = msg.senderName,
                        Text = msg.text
                    };
                    usrMsg.room = room;
                    usrMsg.status = UserMsg.Status.Ok;
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
            lock (room)
            {
                var user = room.User(userId, guid);
                if (user == null && room.Online >= limit)
                    return new ActiveRoomInfo
                    {
                        _status = ActiveRoomInfo.Status.Limit,
                        _room = null
                    };
                room.AddUser(ipAddress, context, connectionId, name, icon, userId, guid);
                _activeUsers[connectionId] = roomId;
            }
            return new ActiveRoomInfo
            {
                _status = ActiveRoomInfo.Status.Ok,
                _room = room
            };
        }
        public UsersRoom DisconnectUser(string connectionId)
        {
            UsersRoom data = new UsersRoom()
            {
                room = null,
                removed = false
            };
            var roomId = _activeUsers.GetValueOrDefault(connectionId);
            if (roomId == 0) return data;
            data.room = _activeRooms[roomId];
            lock (data.room)
            {
                var user = data.room.UserByConnectionId(connectionId);
                if (user.RemoveConnection(connectionId) == 0)
                    if (data.room.RemoveUser(user) == 0)
                    {
                        _activeRooms.Remove(roomId, out _);
                        data.removed = true;
                    }
                _activeUsers.Remove(connectionId, out _);
            }
            return data;
        }
    }
    public struct UsersRoom
    {
        public ActiveRoom room;
        public bool removed;
    }
    public struct UserMsg
    {
        public enum Status
        {
            Ok, NoRoom, Muted, Banned
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
            Ok, Limit, Banned
        }
        public Status _status;
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