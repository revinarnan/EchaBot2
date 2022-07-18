using System;
using System.Collections.Generic;

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
        public string Date { get; set; }
        public string Time { get; set; }
        public string CreatedAt { get; set; }
        public string UpdatedAt { get; set; }
    }
}
