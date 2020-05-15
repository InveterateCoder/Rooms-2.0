using System.Collections.Generic;

namespace Rooms.Models
{
    public class RoomInfo
    {
        public long RoomId { get; set; }
        public long MyId { get; set; }
        public string Flag { get; set; }
        public string Name { get; set; }
        public IEnumerable<RoomsUser> Users { get; set; }
        public IEnumerable<RoomsMsg> Messages { get; set; }
        public int VoiceUserCount { get; set; }
        public bool IsAdmin { get; set; }
    }
}