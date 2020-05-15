namespace Rooms.Models
{
    public class RoomsMsg
    {
        public long UserId { get; set; }
        public string UserGuid { get; set; }
        public long Time { get; set; }
        public string Sender { get; set; }
        public string Icon { get; set; }
        public bool Secret { get; set; }
        public string Text { get; set; }
    }
}