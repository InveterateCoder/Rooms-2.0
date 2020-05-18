namespace Rooms
{
    public class Settings
    {
        public string Secret { get; set; }
        public string DBString { get; set; }
        public string LocalDBString { get; set; }
        public string SGKey { get; set; }
        public string EmailConfirmAddr { get; set; }
        public string SignalREndpoint { get; set; }
        public string[] CorsOrigins { get; set; }
    }
}