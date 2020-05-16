using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.SignalR;

namespace Rooms.Models
{
    public class ActiveRoom
    {
        private readonly ConcurrentDictionary<long, ActiveUser> _registered_users = new ConcurrentDictionary<long, ActiveUser>();
        private readonly ConcurrentDictionary<string, ActiveUser> _guest_users = new ConcurrentDictionary<string, ActiveUser>();
        private readonly SortedList<long, InMemoryMessage> _messages = new SortedList<long, InMemoryMessage>();
        public long OwnerId { get; set; }
        public int VoiceUsersCount { get; set; } = 0;
        public byte Online { get => Convert.ToByte(_registered_users.Count() + _guest_users.Count()); }
        public readonly long roomId;
        public ActiveRoom(long roomId, long ownerId)
        {
            this.roomId = roomId;
            OwnerId = ownerId;
        }
        public void ClearMessages(long from, long till)
        {
            if (from == 0 && till == 0)
                _messages.Clear();
            else if (till == 0)
            {
                var keys = _messages.Where(msg => msg.Value.timeStamp > from).Select(msg => msg.Key).ToArray();
                foreach (var key in keys)
                    _messages.Remove(key);
            }
            else
            {
                var keys = _messages.Where(msg => msg.Value.timeStamp > from && msg.Value.timeStamp < till).Select(msg => msg.Key).ToArray();
                foreach (var key in keys)
                    _messages.Remove(key);
            }
        }
        public void AddUser(IPAddress ipAddress, HubCallerContext context, string connectionId, string name, string icon, long id, string guid)
        {
            var user = new ActiveUser(icon, name, ipAddress);
            if (id != 0) _registered_users.GetOrAdd(id, user).AddConnection(connectionId, context);
            else if (guid != null) _guest_users.GetOrAdd(guid, user).AddConnection(connectionId, context);
            else throw new ArgumentException("Either id or guid must be provided.");
        }
        public byte RemoveUser(ActiveUser user)
        {
            var key = _registered_users.FirstOrDefault(p => p.Value == user).Key;
            if (key != 0)
                _registered_users.Remove(key, out _);
            else
            {
                var guid = _guest_users.FirstOrDefault(p => p.Value == user).Key;
                if (guid != null)
                    _guest_users.Remove(guid, out _);
            }
            return Online;
        }
        public ActiveUser UserByConnectionId(string connectionId) =>
            _registered_users.Values.Concat(_guest_users.Values).First(u => u.ContainsConnection(connectionId));
        public string[] GetVoiceUsers(long id = 0, string guid = null)
        {
            if (id != 0) return _registered_users.Where(p => p.Key != id && p.Value.voiceConnection != null)
                 .Select(p => p.Value.voiceConnection).Concat(_guest_users.Values
                 .Where(u => u.voiceConnection != null).Select(u => u.voiceConnection)).ToArray();
            else if (guid != null) return _registered_users.Values.Where(u => u.voiceConnection != null).Select(u => u.voiceConnection)
                 .Concat(_guest_users.Where(p => p.Key != guid && p.Value.voiceConnection != null).Select(p => p.Value.voiceConnection)).ToArray();
            else return _registered_users.Values.Where(u => u.voiceConnection != null).Select(u => u.voiceConnection)
                .Concat(_guest_users.Values.Where(u => u.voiceConnection != null).Select(u => u.voiceConnection)).ToArray();
        }
        public string[] ConnectVoiceUser(long id, string guid, string connectionId)
        {
            var user = this.User(id, guid);
            if (user.voiceConnection != null)
                return null;
            VoiceUsersCount++;
            user.voiceConnection = connectionId;
            return GetVoiceUsers(id, guid);
        }
        public int DisconnectVoiceUser(long id, string guid, string connectionId)
        {
            var user = this.User(id, guid);
            if (user.voiceConnection == connectionId)
            {
                this.VoiceUsersCount--;
                user.voiceConnection = null;
                return this.VoiceUsersCount;
            }
            return -1;
        }
        public int GetOpenConnections(long id, string guid)
        {
            if (id != 0) return _registered_users[id].connections.Count;
            else if (guid != null) return _guest_users[guid].connections.Count;
            else throw new ArgumentException("Either id or guid must be provided.");
        }
        public string[] GetConnections(long userId = 0, string guid = null, string connectionId = null, long[] ids = null)
        {
            if (userId != 0) return _registered_users.Where(p => p.Key != userId)
               .SelectMany(p => p.Value.connections.Keys).Concat(_guest_users.Values.SelectMany(u => u.connections.Keys)).ToArray();
            else if (guid != null) return _registered_users.Values.SelectMany(u => u.connections.Keys).Concat(_guest_users
                .Where(p => p.Key != guid).SelectMany(p => p.Value.connections.Keys)).ToArray();
            else if (connectionId != null)
            {
                if (ids != null)
                    return _registered_users.Where(p => ids.Contains(p.Key))
                        .SelectMany(p => p.Value.connections.Keys).Where(id => id != connectionId).ToArray();
                else
                    return _registered_users.Values.Concat(_guest_users.Values)
                        .SelectMany(u => u.connections.Keys).Where(id => id != connectionId).ToArray();
            }
            else return _registered_users.Values.Concat(_guest_users.Values).SelectMany(u => u.connections.Keys).ToArray();
        }
        public IEnumerable<string> GetUserConnections(long userId, string guid)
        {
            if (userId != 0)
            {
                var result = _registered_users.GetValueOrDefault(userId);
                if (result != null) return result.connections.Keys;
            }
            else if (guid != null)
            {
                var result = _guest_users.GetValueOrDefault(guid);
                if (result != null) return result.connections.Keys;
            }
            return Enumerable.Empty<string>();
        }
        public ICollection<HubCallerContext> GetUserHubContexts(long userId, string guid)
        {
            if (userId != 0)
            {
                var result = _registered_users.GetValueOrDefault(userId);
                if (result != null) return result.connections.Values;
            }
            else if (guid != null)
            {
                var result = _guest_users.GetValueOrDefault(guid);
                if (result != null) return result.connections.Values;
            }
            return null;
        }
        public IEnumerable<RoomsUser> Users(long id = 0, string guid = null)
        {
            if (id != 0) return _registered_users.Where(p => p.Key != id).Select(p => new RoomsUser
            {
                Id = p.Key,
                Name = p.Value.name,
                Guid = null,
                Icon = p.Value.icon
            }).Concat(_guest_users.Select(p => new RoomsUser
            {
                Id = 0,
                Name = p.Value.name,
                Guid = p.Key,
                Icon = p.Value.icon
            }));
            else if (guid != null) return _registered_users.Select(p => new RoomsUser
            {
                Id = p.Key,
                Name = p.Value.name,
                Guid = null,
                Icon = p.Value.icon
            }).Concat(_guest_users.Where(p => p.Key != guid).Select(p => new RoomsUser
            {
                Id = 0,
                Name = p.Value.name,
                Guid = p.Key,
                Icon = p.Value.icon
            }));
            else throw new ArgumentException("Either id or guid must be provided.");
        }
        public ActiveUser User(long id, string guid)
        {
            if (id != 0) return _registered_users.GetValueOrDefault(id);
            else if (guid != null) return _guest_users.GetValueOrDefault(guid);
            else throw new ArgumentException("Either id or guid must be provided.");
        }
        public int MsgCount
        {
            get
            {
                lock (_messages)
                    return _messages.Count();
            }
        }
        public InMemoryMessage AddMessage(long roomId, string connectionId, string message, long[] accessIds, long id, string guid)
        {
            var time = DateTime.UtcNow.Ticks;
            ActiveUser user = UserByConnectionId(connectionId);
            var msg = new InMemoryMessage(roomId, id, guid, time, user.name, user.icon, accessIds, message);
            lock (_messages) _messages.Add(time, msg);
            return msg;
        }
        public IEnumerable<RoomsMsg> GetMessages(long id, string guid)
        {
            lock (_messages)
            {
                IEnumerable<InMemoryMessage> msgs;
                if (id != 0) msgs = _messages.Values.Reverse().Where(m => m.accessIds == null || m.accessIds.Contains(id));
                else if (guid != null) msgs = _messages.Values.Reverse().Where(m => m.accessIds == null);
                else throw new ArgumentException("Either id or guid must be provided.");
                return msgs.Select(m => new RoomsMsg
                {
                    UserId = m.userId,
                    UserGuid = m.userGuid,
                    Icon = m.senderIcon,
                    Secret = m.accessIds != null,
                    Sender = m.senderName,
                    Text = m.text,
                    Time = m.timeStamp
                });
            }
        }
        public List<Message> DumpMessages()
        {
            List<Message> result = new List<Message>();
            lock (_messages)
            {
                foreach (var m in _messages.Values)
                {
                    result.Add(new Message
                    {
                        UserId = m.userId,
                        GUID = m.userGuid,
                        AccessIds = m.accessIds,
                        RoomId = m.roomId,
                        SenderIcon = m.senderIcon,
                        SenderName = m.senderName,
                        Text = m.text,
                        TimeStamp = m.timeStamp
                    });
                }
                _messages.Clear();
            }
            return result;
        }
    }
    public class ActiveUser
    {
        public IPAddress ipAddress;
        public ConcurrentDictionary<string, HubCallerContext> connections;
        public string voiceConnection;
        public string icon;
        public string name;
        public ActiveUser(string icon, string name, IPAddress ipAddress)
        {
            this.icon = icon;
            this.name = name;
            this.ipAddress = ipAddress;
            this.connections = new ConcurrentDictionary<string, HubCallerContext>();
        }
        public void AddConnection(string connectionId, HubCallerContext context) =>
            connections[connectionId] = context;
        public int RemoveConnection(string connectionId)
        {
            connections.Remove(connectionId, out _);
            return connections.Count;
        }
        public bool ContainsConnection(string connectionId) =>
            connections.ContainsKey(connectionId);
        public ICollection<HubCallerContext> GetUserHubContexts =>
            connections.Values;
    }
    public class InMemoryMessage
    {
        public InMemoryMessage(long roomId, long userId, string userGuid, long timeStamp, string senderName,
            string senderIcon, IEnumerable<long> accessIds, string text)
        {
            this.roomId = roomId;
            this.userId = userId;
            this.userGuid = userGuid;
            this.timeStamp = timeStamp;
            this.accessIds = accessIds;
            this.senderName = senderName;
            this.senderIcon = senderIcon;
            this.text = text;
        }
        public long roomId;
        public long userId;
        public string userGuid;
        public long timeStamp;
        public IEnumerable<long> accessIds;
        public string senderName;
        public string senderIcon;
        public string text;
    }
}