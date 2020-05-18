using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.SignalR;

namespace Rooms.Models
{
    public enum MessageStatus
    {
        Ok, Warn, Ban
    }
    public enum AddUserStatus
    {
        Added, Active, Limit, Failed
    }
    public class ActiveRoom
    {
        private readonly ConcurrentDictionary<string, ActiveUser> _users = new ConcurrentDictionary<string, ActiveUser>();
        private readonly SortedList<long, InMemoryMessage> _messages = new SortedList<long, InMemoryMessage>();
        private readonly F23.StringSimilarity.Cosine _cosine = new F23.StringSimilarity.Cosine();
        public long OwnerId { get; set; }
        public readonly long roomId;
        public int Online { get => _users.Count(); }
        public int VoiceUsersCount { get => _users.Where(p => p.Value._voiceOn == true).Count(); }
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
        public AddUserStatus AddUser(IPAddress ipAddress, HubCallerContext context, long id, string guid, string name, string icon, int limit)
        {
            if (_users.Values.Any(u => u._id == id && u._guid == guid))
                return AddUserStatus.Active;
            else
            {
                if (_users.Count >= limit)
                    return AddUserStatus.Limit;
                if (_users.TryAdd(context.ConnectionId, new ActiveUser(id, guid, icon, name, ipAddress, context)))
                    return AddUserStatus.Added;
                else return AddUserStatus.Failed;
            }
        }
        public IEnumerable<HubCallerContext> Users
        {
            get => _users.Values.Select(u => u._context);
        }
        public ICollection<string> Connections
        {
            get => _users.Keys;
        }
        public ActiveUser GetUser(string connectionId = null, long id = 0)
        {
            if (connectionId != null) return _users.GetValueOrDefault(connectionId, null);
            if (id != 0) return _users.Values.FirstOrDefault(u => u._id == id);
            return null;
        }
        public (ActiveUser user, string[] users) RemoveUser(string connectionId)
        {
            ActiveUser user;
            _users.Remove(connectionId, out user);
            return (user, Users.Select(c => c.ConnectionId).ToArray());
        }
        public HubCallerContext ConnectionByCred(long id, string guid) =>
            _users.Values.FirstOrDefault(u => u._id == id && u._guid == guid)?._context;
        public IEnumerable<ActiveUser> GetUsers(string connectionId) =>
            _users.Where(p => p.Key != connectionId).Select(p => p.Value);
        public string[] ConnectVoiceUser(string connectionId)
        {
            var user = GetUser(connectionId);
            if (user._voiceOn == true)
                return null;
            user._voiceOn = true;
            return _users.Where(p => p.Value._voiceOn == true).Select(p => p.Key).ToArray();
        }
        public int DisconnectVoiceUser(string connectionId)
        {
            var user = this.GetUser(connectionId);
            if (user._voiceOn)
            {
                user._voiceOn = false;
                return VoiceUsersCount;
            }
            return -1;
        }
        public int MsgCount
        {
            get
            {
                lock (_messages)
                    return _messages.Count();
            }
        }
        public (InMemoryMessage message, MessageStatus status) AddMessage(string connectionId, long id, string guid, string message, bool secret)
        {
            var status = MessageStatus.Ok;
            var time = DateTime.UtcNow;
            ActiveUser user = GetUser(connectionId);
            if (time - user._lastMessageTime < TimeSpan.FromSeconds(2) || _cosine.Similarity(user._lastMessage, message) > 0.5)
            {
                user._hits++;
                if (user._hits > 2)
                {
                    if (user._warned)
                        status = MessageStatus.Ban;
                    else
                    {
                        status = MessageStatus.Warn;
                        user._warned = true;
                    }
                }
            }
            else user._hits = 0;
            user._lastMessageTime = time;
            user._lastMessage = message;
            InMemoryMessage msg = null;
            if (status == MessageStatus.Ok)
            {
                msg = new InMemoryMessage(roomId, id, guid, time.Ticks, user.name, user.icon, message);
                if (!secret)
                    lock (_messages) _messages.Add(time.Ticks, msg);
            }
            return (msg, status);
        }
        public IEnumerable<RoomsMsg> GetMessages()
        {
            lock (_messages)
            {
                return _messages.Values.Reverse().Select(m => new RoomsMsg
                {
                    UserId = m.userId,
                    UserGuid = m.userGuid,
                    Icon = m.senderIcon,
                    Secret = false,
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
        public HubCallerContext _context;
        public bool _voiceOn;
        public long _id;
        public string _guid;
        public string icon;
        public string name;
        public string _lastMessage;
        public DateTime _lastMessageTime;
        public bool _warned;
        public int _hits;
        public ActiveUser(long id, string guid, string icon, string name, IPAddress ipAddress, HubCallerContext context)
        {
            _id = id;
            _guid = guid;
            this.icon = icon;
            this.name = name;
            this.ipAddress = ipAddress;
            _context = context;
            _lastMessage = "";
            _lastMessageTime = DateTime.UtcNow;
            _warned = false;
            _hits = 0;
        }
    }
    public class InMemoryMessage
    {
        public InMemoryMessage(long roomId, long userId, string userGuid, long timeStamp, string senderName, string senderIcon, string text)
        {
            this.roomId = roomId;
            this.userId = userId;
            this.userGuid = userGuid;
            this.timeStamp = timeStamp;
            this.senderName = senderName;
            this.senderIcon = senderIcon;
            this.text = text;
        }
        public long roomId;
        public long userId;
        public string userGuid;
        public long timeStamp;
        public string senderName;
        public string senderIcon;
        public string text;
    }
}