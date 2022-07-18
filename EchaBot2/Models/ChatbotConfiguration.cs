using System;
using System.Collections.Generic;

#nullable disable

namespace EchaBot2.Models
{
    public partial class ChatbotConfiguration
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string UrlClient { get; set; }
        public string UrlKb { get; set; }
        public string Date { get; set; }
        public string Time { get; set; }
        public string CreatedAt { get; set; }
        public string UpdatedAt { get; set; }
    }
}
