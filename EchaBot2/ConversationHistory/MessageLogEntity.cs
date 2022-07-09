using Microsoft.WindowsAzure.Storage.Table;

namespace EchaBot2.ConversationHistory
{
    public class MessageLogEntity : TableEntity
    {
        public string Body
        {
            get;
            set;
        }
    }
}
