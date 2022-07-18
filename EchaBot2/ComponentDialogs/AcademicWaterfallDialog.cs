using EchaBot2.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using System.Threading;
using System.Threading.Tasks;

namespace EchaBot2.ComponentDialogs
{
    public class AcademicWaterfallDialog : ComponentDialog
    {
        // Define value names for values tracked inside the dialogs.
        private const string EmailQuestion = "value-chatBotEmailQuestions";

        public AcademicWaterfallDialog()
            : base(nameof(AcademicWaterfallDialog))
        {
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
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Baik, terima kasih sudah menghubungi Echa. Semoga harimu menyenangkan!"), cancellationToken);

                return await stepContext.EndDialogAsync(null, cancellationToken);
            }

            var promptOptions = new PromptOptions { Prompt = MessageFactory.Text("Apakah kamu ingin dihubungkan dengan staff akademik?") };

            return await stepContext.PromptAsync(nameof(ConfirmPrompt), promptOptions, cancellationToken);
        }

        private async Task<DialogTurnResult> GetQuestionAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values[EmailQuestion] = new ChatBotEmailQuestion();

            if (!(bool)stepContext.Result)
            {
                await stepContext.Context.SendActivityAsync(
                    "Baik, terima kasih sudah menghubungi Echa. Semoga harimu menyenangkan!", cancellationToken: cancellationToken);

                return await stepContext.EndDialogAsync(null, cancellationToken);
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
            var emailQuestion = (ChatBotEmailQuestion)stepContext.Values[EmailQuestion];
            emailQuestion.Email = (string)stepContext.Result;
            emailQuestion.Id = stepContext.Context.Activity.Conversation.Id;
            emailQuestion.IsAnswered = false;

            var message = $"Email kamu adalah {((ChatBotEmailQuestion)stepContext.Values[EmailQuestion]).Email}, " +
                          $"dan pertanyaan kamu adalah (\"{((ChatBotEmailQuestion)stepContext.Values[EmailQuestion]).Question}\").";

            await stepContext.Context.SendActivityAsync(message, cancellationToken: cancellationToken);

            return await stepContext.EndDialogAsync(stepContext.Values[EmailQuestion], cancellationToken);
        }
    }
}
