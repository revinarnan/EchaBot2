using EchaBot2.Models;
using EchaBot2.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace EchaBot2.ComponentDialogs
{
    public class ClosingWaterfallDialog : ComponentDialog
    {
        private readonly DbUtility _dbUtility;

        public ClosingWaterfallDialog(DbUtility dbUtility)
            : base(nameof(ClosingWaterfallDialog))
        {
            _dbUtility = dbUtility;

            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                ClosingDialogConfirmationAsync,
                FinalStepAsync
            }));

            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> ClosingDialogConfirmationAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var promptOptions = new PromptOptions { Prompt = MessageFactory.Text("Apakah ada yang bisa Echa bantu lagi?") };

            return await stepContext.PromptAsync(nameof(ConfirmPrompt), promptOptions, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var activity = stepContext.Context.Activity;
            var convId = activity.Conversation.Id;

            if (!(bool)stepContext.Result)
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Baik, terima kasih sudah menghubungi Echa. Semoga harimu menyenangkan!"), cancellationToken);

                var chatHistoryInDb = await _dbUtility.DbContext.ChatHistories.SingleOrDefaultAsync(c => c.ChatHistoryFileName == convId, cancellationToken);
                if (chatHistoryInDb == null)
                {
                    var chatHistory = new ChatHistory
                    {
                        UserId = activity.From.Id,
                        IsDoneOnBot = true,
                        IsDoneOnEmail = false,
                        IsDoneOnLiveChat = false,
                        ChatHistoryFileName = convId
                    };

                    await _dbUtility.InsertChatHistory(chatHistory);
                }
                else
                {
                    chatHistoryInDb.IsDoneOnBot = true;
                    await _dbUtility.SaveChangesAsync();
                }

                await stepContext.CancelAllDialogsAsync(cancellationToken);
                return await stepContext.EndDialogAsync(null, cancellationToken);
            }

            var message = "Silakan bertanya hal lain yang masih membingungkan";

            await stepContext.Context.SendActivityAsync(message, cancellationToken: cancellationToken);

            return await stepContext.EndDialogAsync(null, cancellationToken);
        }
    }
}
