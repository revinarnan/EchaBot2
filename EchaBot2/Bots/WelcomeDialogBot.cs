using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EchaBot2.Bots
{
    public class WelcomeDialogBot<T> : EchaBot<T> where T : Dialog
    {
        public WelcomeDialogBot(ILogger<EchaBot<T>> logger,
            ConversationState conversationState, T dialog, UserState userState)
            : base(logger, conversationState, dialog, userState)
        {
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            var welcomeText = "Halo! Selamat datang di EchaBot.";
            var messageText = "Silakan mulai bertanya dengan mengetik pertanyaan atau informasi yang ingin kamu dapatkan.";

            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text(welcomeText, welcomeText), cancellationToken);
                    await turnContext.SendActivityAsync(MessageFactory.Text(messageText, messageText), cancellationToken);
                    //await Dialog.RunAsync(turnContext, ConversationState.CreateProperty<DialogState>("DialogState"), cancellationToken);
                }
            }
        }
    }
}
