namespace Rooms.Models
{
    public class ReturnSignal<T>
    {
        public string Code { get; set; }
        public T Payload { get; set; }
    }
}