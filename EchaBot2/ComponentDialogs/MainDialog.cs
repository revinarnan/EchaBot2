using EchaBot2.Models;
using EchaBot2.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
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
                InitialStepAsync,
                FinalStepAsync
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> InitialStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var luisResult = await _botServices.LuisIntentRecognizer.RecognizeAsync(stepContext, stepContext.Context.Activity, cancellationToken);
            var questionIntent = LuisRecognizer.TopIntent(luisResult);
            var questionText = luisResult.Text;

            if (stepContext.Context.Activity.Text.ToLower() is not ("yes" or "no") &&
                !stepContext.Context.Activity.Text.Contains("@"))
            {
                switch (questionIntent)
                {
                    case "Academic":
                        await ProcessAcademicResponseAsync(stepContext.Context, questionText, cancellationToken);
                        return await stepContext.BeginDialogAsync(nameof(AcademicWaterfallDialog), null, cancellationToken);
                    case "None":
                        await ShowLuisResult(stepContext.Context, cancellationToken);
                        break;
                    default:
                        await ProcessChitchatResponseAsync(stepContext.Context, questionIntent, cancellationToken);
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
                message.SuggestedActions = new SuggestedActions
                {
                    Actions = new List<CardAction> { new() { Title = "@Staff", Type = ActionTypes.ImBack, Value = "@staff" } }
                };

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

        private async Task ProcessChitchatResponseAsync(ITurnContext context, string questionIntent, CancellationToken cancellationToken)
        {
            Logger.LogInformation("ProcessChitchatResponseAsync");

            var chitchatAnswer = await _botServices.GetChitchatAnswer(questionIntent);
            await context.SendActivityAsync(MessageFactory.Text(chitchatAnswer), cancellationToken);
        }

        private async Task ProcessAcademicResponseAsync(ITurnContext context, string questionText, CancellationToken cancellationToken)
        {
            Logger.LogInformation("ProcessAcademicResponseAsync");

            var academicAnswer = await _botServices.GetAcademicAnswer(questionText);
            if (academicAnswer.Equals("No good match found in KB."))
            {
                academicAnswer = "Maaf, informasi tidak ditemukan. Mohon gunakan kata lain.";
            }

            await context.SendActivityAsync(MessageFactory.Text(academicAnswer), cancellationToken);
        }

        private async Task ShowLuisResult(ITurnContext context, CancellationToken cancellationToken)
        {
            await context.SendActivityAsync(MessageFactory.Text("Maaf saya belum mengerti yang kamu katakan, coba gunakan kata lain."), cancellationToken);
        }
    }
}
