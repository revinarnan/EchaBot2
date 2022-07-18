namespace EchaBot2.Model
{
    public class ChatHistory
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public bool IsDoneOnBot { get; set; }
        public bool IsDoneOnLiveChat { get; set; }
        public bool IsDoneOnEmail { get; set; }
        public string ChatHistoryFileName { get; set; } //pakai conversation Id
    }
}
