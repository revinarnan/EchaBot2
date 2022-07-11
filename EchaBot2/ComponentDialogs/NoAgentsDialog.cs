using EchaBot2.Model;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using System.Threading;
using System.Threading.Tasks;

namespace EchaBot2.ComponentDialogs
{
    public class NoAgentsDialog : ComponentDialog
    {
        // Define value names for values tracked inside the dialogs.
        private const string UserInfo = "value-userInfo";

        public NoAgentsDialog()
            : base(nameof(NoAgentsDialog))
        {
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(new TextPrompt(nameof(TextPrompt)));

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                AskSendEmailConfirmationAsync,
                GetQuestionAsync,
                GetEmailAsync,
                FinalStepAsync
            }));

            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> AskSendEmailConfirmationAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var promptOptions = new PromptOptions { Prompt = MessageFactory.Text("Apakah kamu ingin menanyakan hal tersebut melalui email?") };

            return await stepContext.PromptAsync(nameof(ConfirmPrompt), promptOptions, cancellationToken);
        }

        private async Task<DialogTurnResult> GetQuestionAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values[UserInfo] = new UserInfo();

            if (!(bool)stepContext.Result)
            {
                await stepContext.Context.SendActivityAsync(
                    "Baik, terima kasih sudah menghubungi Echa. Semoga harimu menyenangkan!", cancellationToken: cancellationToken);

                return await stepContext.EndDialogAsync(null, cancellationToken);
            }

            var promptOptions = new PromptOptions { Prompt = MessageFactory.Text("Silakan kirim pertanyaan yang ingin kamu tanyakan", inputHint: InputHints.IgnoringInput) };

            return await stepContext.PromptAsync(nameof(TextPrompt), promptOptions, cancellationToken);
        }

        private async Task<DialogTurnResult> GetEmailAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userInfo = (UserInfo)stepContext.Values[UserInfo];
            userInfo.Question = (string)stepContext.Result;

            var promptOptions = new PromptOptions { Prompt = MessageFactory.Text("Silakan masukkan emailmu yang dapat dihubungi", inputHint: InputHints.IgnoringInput) };

            return await stepContext.PromptAsync(nameof(TextPrompt), promptOptions, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userInfo = (UserInfo)stepContext.Values[UserInfo];
            userInfo.Email = (string)stepContext.Result;

            var messageQuestionReview = $"Pertanyaan kamu adalah '{((UserInfo)stepContext.Values[UserInfo]).Question}'.";
            var messageThankYou = $"Terima kasih, jawaban akan dikirimkan melalui email {((UserInfo)stepContext.Values[UserInfo]).Email}, jika sudah ada staff yang online.";

            await stepContext.Context.SendActivityAsync(messageQuestionReview, cancellationToken: cancellationToken);
            await stepContext.Context.SendActivityAsync(messageThankYou, cancellationToken: cancellationToken);

            return await stepContext.EndDialogAsync(userInfo, cancellationToken);
        }
    }
}
