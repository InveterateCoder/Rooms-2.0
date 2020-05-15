using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;

namespace Rooms.Models
{
    public class State
    {
        private readonly ConcurrentDictionary<long, ActiveRoom> _activeRooms = new ConcurrentDictionary<long, ActiveRoom>();
        private readonly ConcurrentDictionary<string, long> _activeUsers = new ConcurrentDictionary<string, long>();
        public readonly ConcurrentDictionary<string, (long, string)> _waitingPassword = new ConcurrentDictionary<string, (long, string)>();
        public int RoomsCount { get => _activeRooms.Count; }
        public IEnumerable<long> RoomsKeys { get => _activeRooms.Keys; }
        public long GetRoomId(string connectionId) => _activeUsers[connectionId];
        public ActiveRoom GetRoom(long roomId) => _activeRooms[roomId];
        private ActiveRoom GetRoom(string connectionId) => GetRoom(_activeUsers[connectionId]);
        public string[] AdministrateUser(long adminId, string connectionId, long id, string guid)
        {
            var room = this.GetRoom(connectionId);
            if (room.OwnerId != adminId) return new string[0];
            return room.GetUserConnections(id, guid).ToArray();
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
        public int DisconnectVoiceUser(long id, string guid, string connectionId) =>
            this.GetRoom(connectionId).DisconnectVoiceUser(id, guid, connectionId);
        public string[] UserConnections(long userId, string guid = null) =>
            _activeRooms.Values.Where(r => r.User(userId, guid) != null).SelectMany(r => r.GetUserConnections(userId, guid)).ToArray();
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
        public string[] RemoveRoom(long roomId)
        {
            ActiveRoom room = null;
            if (_activeRooms.Remove(roomId, out room))
            {
                lock (room)
                {
                    var users = _activeUsers.Where(u => u.Value == roomId);
                    foreach (var user in users)
                        _activeUsers.Remove(user.Key, out _);
                }
            }
            return room?.GetConnections();
        }
        public UserMsg SendMessage(string connectionId, string message, long[] accessIds, long id, string guid)
        {
            ActiveRoom room = _activeRooms[_activeUsers[connectionId]];
            var msg = room.AddMessage(room.roomId, connectionId, message, accessIds, id, guid);
            return new UserMsg
            {
                connectionIds = room.GetConnections(connectionId: connectionId, ids: accessIds),
                message = new RoomsMsg
                {
                    UserId = msg.userId,
                    UserGuid = msg.userGuid,
                    Time = msg.timeStamp,
                    Icon = msg.senderIcon,
                    Secret = accessIds != null,
                    Sender = msg.senderName,
                    Text = msg.text
                },
                room = room
            };
        }
        public ActiveRoom ConnectUser(IPAddress ipAddress, long ownerId, long userId, string guid, string name, string icon, string connectionId, long roomId, byte limit)
        {
            ActiveRoom room = _activeRooms.GetOrAdd(roomId, new ActiveRoom(roomId, ownerId));
            lock (room)
            {
                var user = room.User(userId, guid);
                if (user == null && room.Online >= limit) return null;
                room.AddUser(ipAddress, connectionId, name, icon, userId, guid);
                _activeUsers[connectionId] = roomId;
            }
            return room;
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
        public string[] connectionIds;
        public RoomsMsg message;
        public ActiveRoom room;
    }
}