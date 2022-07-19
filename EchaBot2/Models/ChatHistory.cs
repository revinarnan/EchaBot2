using System;

#nullable disable

namespace EchaBot2.Models
{
    public partial class ChatHistory
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public bool IsDoneOnBot { get; set; }
        public bool IsDoneOnLiveChat { get; set; }
        public bool IsDoneOnEmail { get; set; }
        public string ChatHistoryFileName { get; set; }
        public string Date { get; set; } = DateTime.Now.ToString("MM/dd/yyyy");
        public string Time { get; set; } = DateTime.Now.ToString("hh:mm:ss tt");
        public string CreatedAt { get; set; } = DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss tt");
        public string UpdatedAt { get; set; }
    }
}
