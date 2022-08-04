using EchaBot2.Models;
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
        private readonly UserState _userState;
        private readonly DbUtility _dbUtility;

        public MainDialog(IBotServices botServices, AcademicWaterfallDialog academicWaterfall,
            ILogger<MainDialog> logger, UserState userState, DbUtility dbUtility, ClosingWaterfallDialog closingDialog)
            : base(nameof(MainDialog))
        {
            _botServices = botServices;
            Logger = logger;
            _userState = userState;
            _dbUtility = dbUtility;

            AddDialog(closingDialog);
            AddDialog(academicWaterfall);
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
            var emailQuestionResult = (ChatBotEmailQuestion)stepContext.Result;

            if (stepContext.Result is ChatBotEmailQuestion result)
            {
                var messageText = "Silakan ketik '@staff' untuk menghubungkan dengan staff akademik. Tunggu permintaanmu diterima ya.";
                var message = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);

                var emailQuestions = new ChatBotEmailQuestion
                {
                    Id = emailQuestionResult.Id,
                    Email = emailQuestionResult.Email,
                    Question = emailQuestionResult.Question,
                    IsAnswered = emailQuestionResult.IsAnswered
                };

                await _dbUtility.InsertEmailQuestion(emailQuestions);

                await stepContext.Context.SendActivityAsync(message, cancellationToken);
                var accessor = _userState.CreateProperty<ChatBotEmailQuestion>(nameof(ChatBotEmailQuestion));
                await accessor.SetAsync(stepContext.Context, emailQuestionResult, cancellationToken);

                return await stepContext.EndDialogAsync(result, cancellationToken);
            }

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
