using EchaBot2.Model;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EchaBot2.ComponentDialogs
{
    public class MainDialog : ComponentDialog
    {
        private readonly IBotServices _botServices;
        protected readonly ILogger Logger;

        public MainDialog(IBotServices botServices, AcademicWaterfallDialog academicWaterfall, NoAgentsDialog noAgentsDialog, ILogger<MainDialog> logger)
            : base(nameof(MainDialog))
        {
            _botServices = botServices;
            Logger = logger;

            AddDialog(academicWaterfall);
            AddDialog(noAgentsDialog);
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                ActStepAsync,
                FinalStepAsync
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> ActStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var luisResult = await _botServices.LuisIntentRecognizer.RecognizeAsync(stepContext, stepContext.Context.Activity, cancellationToken);
            var topIntent = luisResult.Intents.First().Key;
            if (stepContext.Context.Activity.Text is not ("Yes" or "No") &&
                !stepContext.Context.Activity.Text.Contains("@"))
            {
                switch (topIntent)
                {
                    case "Academic":
                        await ProcessAcademicResponseAsync(stepContext.Context, cancellationToken);
                        return await stepContext.BeginDialogAsync(nameof(AcademicWaterfallDialog), null, cancellationToken);
                    case "None":
                        await ShowLuisResult(stepContext.Context, cancellationToken);
                        break;
                    default:
                        await ProcessChitchatResponseAsync(stepContext.Context, cancellationToken);
                        break;
                }
            }
            return await stepContext.EndDialogAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (stepContext.Result is UserInfo result)
            {
                var messageText = "Silakan ketik 'human' untuk menghubungkan dengan staff akademik. Tunggu permintaanmu diterima ya.";
                var message = MessageFactory.Text(messageText, null, InputHints.ExpectingInput);
                await stepContext.Context.SendActivityAsync(message, cancellationToken);

                return await stepContext.EndDialogAsync(result, cancellationToken);
            }

            //if (stepContext.Context.Activity.Text == Strings.NoAgentsAvailable)
            //{
            //    return await stepContext.BeginDialogAsync(nameof(NoAgentsDialog), null, cancellationToken);
            //}

            return await stepContext.EndDialogAsync(null, cancellationToken);
        }

        private async Task ProcessChitchatResponseAsync(ITurnContext context, CancellationToken cancellationToken)
        {
            Logger.LogInformation("ProcessChitchatResponseAsync");

            var results = await _botServices.ChitchatKb.GetAnswersAsync(context);

            if (results.Any())
            {
                if (results.First().Answer.Equals("No good match found in KB."))
                {
                    await context.SendActivityAsync(MessageFactory.Text("Maaf, saya belum bisa menjawab. Silakan mengguankan kata lain"), cancellationToken);
                }
                else
                {
                    await context.SendActivityAsync(MessageFactory.Text(results.First().Answer), cancellationToken);
                }
            }
            else
            {
                await context.SendActivityAsync(MessageFactory.Text("Maaf, saya belum bisa menjawab."), cancellationToken);
            }
        }

        private async Task ProcessAcademicResponseAsync(ITurnContext context, CancellationToken cancellationToken)
        {
            Logger.LogInformation("ProcessAcademicResponseAsync");

            var results = await _botServices.AcademicKb.GetAnswersAsync(context);

            if (results.Any())
            {
                if (results.First().Answer.Equals("No good match found in KB."))
                {
                    await context.SendActivityAsync(MessageFactory.Text("Maaf, informasi tidak ditemukan. Mohon gunakan kata lain."), cancellationToken);
                }
                else
                {
                    await context.SendActivityAsync(MessageFactory.Text(results.First().Answer), cancellationToken);
                }
            }
            else
            {
                await context.SendActivityAsync(MessageFactory.Text("Maaf, saya belum bisa menjawab."), cancellationToken);
            }
        }

        private async Task ShowLuisResult(ITurnContext context, CancellationToken cancellationToken)
        {
            await context.SendActivityAsync(MessageFactory.Text("Maaf saya belum mengerti yang kamu katakan, saya harus belajar lagi."), cancellationToken);
        }
    }
}
