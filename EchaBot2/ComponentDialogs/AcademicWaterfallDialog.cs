using EchaBot2.Model;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using System.Threading;
using System.Threading.Tasks;

namespace EchaBot2.ComponentDialogs
{
    public class AcademicWaterfallDialog : ComponentDialog
    {
        public AcademicWaterfallDialog()
            : base(nameof(AcademicWaterfallDialog))
        {
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(new TextPrompt(nameof(TextPrompt)));

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                ResponseConfirmationAsync,
                HandoffAgentConfirmationAsync,
                GetEmailAsync,
                FinalStepAsync
            }));

            InitialDialogId = nameof(WaterfallDialog);
        }

        //COBA DIGANTI TEXT PROMT
        // TODO
        private async Task<DialogTurnResult> ResponseConfirmationAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var promptOptions = new PromptOptions { Prompt = MessageFactory.Text("Apakah sudah menjawab?") };

            return await stepContext.PromptAsync(nameof(ConfirmPrompt), promptOptions, cancellationToken);
        }

        private async Task<DialogTurnResult> HandoffAgentConfirmationAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if ((bool)stepContext.Result)
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Baik, terima kasih sudah menghubungi Echa. Semoga harimu menyenangkan!"), cancellationToken);

                return await stepContext.EndDialogAsync(null, cancellationToken);
            }

            var promptOptions = new PromptOptions { Prompt = MessageFactory.Text("Apakah kamu ingin dihubungkan dengan staff akademik?") };

            return await stepContext.PromptAsync(nameof(ConfirmPrompt), promptOptions, cancellationToken);
        }

        private async Task<DialogTurnResult> GetEmailAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (!(bool)stepContext.Result)
            {
                await stepContext.Context.SendActivityAsync(
                    "Baik, terima kasih sudah menghubungi Echa. Semoga harimu menyenangkan!", cancellationToken: cancellationToken);

                return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
            }

            var promptOptions = new PromptOptions { Prompt = MessageFactory.Text("Bolehkah saya meminta emailmu yang dapat dihubungi?") };

            return await stepContext.PromptAsync(nameof(TextPrompt), promptOptions, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userInfo = (UserInfo)stepContext.Options;
            userInfo.Email = (string)stepContext.Result;

            var message = "Email kamu adalah " + userInfo.Email;

            await stepContext.Context.SendActivityAsync(message, cancellationToken: cancellationToken);

            return await stepContext.EndDialogAsync(userInfo, cancellationToken);
        }
    }
}
