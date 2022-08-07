using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace EchaBot2.Models
{
    public class QnAAnswer
    {
        [JsonPropertyName("questions")]
        public IList<string> Questions { get; set; }
        [JsonPropertyName("answer")]
        public string Answer { get; set; }
    }

    public class QnAResponseDto
    {
        [JsonPropertyName("answers")]
        public IList<QnAAnswer> Answers { get; set; }
    }
}
