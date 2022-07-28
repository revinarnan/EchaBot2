using EchaBot2.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EchaBot2.ComponentDialogs
{
    public class AcademicWaterfallDialog : ComponentDialog
    {
        private readonly DbUtility _dbUtility;
        // Define value names for values tracked inside the dialogs.
        private const string EmailQuestion = "value-chatBotEmailQuestions";

        public AcademicWaterfallDialog(DbUtility dbUtility, ClosingWaterfallDialog closingDialog)
            : base(nameof(AcademicWaterfallDialog))
        {
            _dbUtility = dbUtility;

            AddDialog(closingDialog);
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(new TextPrompt(nameof(TextPrompt)));

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                ResponseConfirmationAsync,
                HandoffAgentConfirmationAsync,
                GetQuestionAsync,
                GetEmailAsync,
                FinalStepAsync
            }));

            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> ResponseConfirmationAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var promptOptions = new PromptOptions { Prompt = MessageFactory.Text("Apakah sudah menjawab?") };

            return await stepContext.PromptAsync(nameof(ConfirmPrompt), promptOptions, cancellationToken);
        }

        private async Task<DialogTurnResult> HandoffAgentConfirmationAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if ((bool)stepContext.Result)
            {
                return await stepContext.ReplaceDialogAsync(nameof(ClosingWaterfallDialog), null, cancellationToken);
            }

            var promptOptions = new PromptOptions { Prompt = MessageFactory.Text("Apakah kamu ingin dihubungkan dengan staff akademik?") };

            return await stepContext.PromptAsync(nameof(ConfirmPrompt), promptOptions, cancellationToken);
        }

        private async Task<DialogTurnResult> GetQuestionAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values[EmailQuestion] = new ChatBotEmailQuestion();

            if (!(bool)stepContext.Result)
            {
                return await stepContext.BeginDialogAsync(nameof(ClosingWaterfallDialog), null, cancellationToken);
            }

            var promptOptions = new PromptOptions
            {
                Prompt = MessageFactory.Text("Silakan kirim pertanyaanmu disini. " +
                "Apabila nanti tidak ada staff akademik yang tersedia, pertanyaan akan dijawab melalui email.",
                inputHint: InputHints.IgnoringInput)
            };

            return await stepContext.PromptAsync(nameof(TextPrompt), promptOptions, cancellationToken);
        }

        private async Task<DialogTurnResult> GetEmailAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var emailQuestion = (ChatBotEmailQuestion)stepContext.Values[EmailQuestion];
            emailQuestion.Question = (string)stepContext.Result;

            var promptOptions = new PromptOptions { Prompt = MessageFactory.Text("Silakan masukkan emailmu yang dapat dihubungi", inputHint: InputHints.IgnoringInput) };

            return await stepContext.PromptAsync(nameof(TextPrompt), promptOptions, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var activity = stepContext.Context.Activity;
            var emailQuestion = (ChatBotEmailQuestion)stepContext.Values[EmailQuestion];

            var convId = activity.Conversation.Id;
            var index = convId.IndexOf("|", StringComparison.Ordinal);
            if (index >= 0)
                convId = convId.Substring(0, index);

            emailQuestion.Email = (string)stepContext.Result;
            emailQuestion.Id = convId;
            emailQuestion.IsAnswered = false;

            var chatHistory = new ChatHistory //Data disave dulu, supaya bisa diambil dari web dan simpan data terbaru
            {
                UserId = activity.From.Id,
                IsDoneOnBot = true,
                IsDoneOnEmail = false,
                IsDoneOnLiveChat = false,
                ChatHistoryFileName = convId
            };

            await _dbUtility.InsertChatHistory(chatHistory);

            var message = $"Email kamu adalah {((ChatBotEmailQuestion)stepContext.Values[EmailQuestion]).Email}, " +
                          $"dan pertanyaan kamu adalah (\"{((ChatBotEmailQuestion)stepContext.Values[EmailQuestion]).Question}\").";

            await stepContext.Context.SendActivityAsync(message, cancellationToken: cancellationToken);

            return await stepContext.EndDialogAsync(stepContext.Values[EmailQuestion], cancellationToken);
        }
    }
}
