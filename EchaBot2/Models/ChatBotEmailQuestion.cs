using System;
using System.Collections.Generic;

#nullable disable

namespace EchaBot2.Models
{
    public partial class ChatBotEmailQuestion
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string Question { get; set; }
        public bool IsAnswered { get; set; }
    }
}
