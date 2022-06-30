using EchaBot2.Model;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EchaBot2.ComponentDialogs
{
    public class AcademicWaterfallDialog : ComponentDialog
    {
        private readonly UserState _userState;
        private IBotServices BotServices;

        public AcademicWaterfallDialog(UserState userState, IBotServices botServices)
            : base(nameof(AcademicWaterfallDialog))
        {
            _userState = userState;
            BotServices = botServices;

            AddDialog(new ConfirmPrompt("ResponseConfirmPrompt"));
            AddDialog(new ConfirmPrompt("HandoffConfirmPrompt"));
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

        private async Task<DialogTurnResult> ResponseConfirmationAsync(WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            var results = await BotServices.AcademicKb.GetAnswersAsync(stepContext.Context);

            if (results.Any())
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text(results.First().Answer), cancellationToken);
            }
            else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Maaf, informasi tidak ditemukan. Mohon gunakan kata lain."), cancellationToken);
            }

            var promptOptions = new PromptOptions { Prompt = MessageFactory.Text("Apakah sudah menjawab?") };

            return await stepContext.PromptAsync("ResponseConfirmPrompt", promptOptions, cancellationToken);
        }

        private async Task<DialogTurnResult> HandoffAgentConfirmationAsync(WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            if ((bool)stepContext.Result)
            {
                await stepContext.Context.SendActivityAsync("Baik, terima kasih sudah menghubungi Echa. Semoga harimu menyenangkan!", cancellationToken: cancellationToken);

                return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
            }

            var promptOptions = new PromptOptions { Prompt = MessageFactory.Text("Apakah kamu ingin dihubungkan dengan staff akademik?") };

            return await stepContext.PromptAsync("HandoffConfirmPrompt", promptOptions, cancellationToken);
        }

        private async Task<DialogTurnResult> GetEmailAsync(WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
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

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            var userInfo = (UserInfo)stepContext.Result;

            var message = "Email kamu adalah " + userInfo.Email;

            await stepContext.Context.SendActivityAsync(message, cancellationToken: cancellationToken);

            var accessor = _userState.CreateProperty<UserInfo>(nameof(UserInfo));
            await accessor.SetAsync(stepContext.Context, userInfo, cancellationToken);

            //await stepContext.Context.SendActivityAsync("Silakan ketik agent untuk menghubungkan dengan staff akademik. Kamu dapat membatalkannya dengan mengetik cancel.", cancellationToken: cancellationToken);

            return await stepContext.EndDialogAsync(userInfo, cancellationToken);
        }
    }
}
