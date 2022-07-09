// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Generated with Bot Builder V4 SDK Template for Visual Studio EchoBot v4.15.2

using EchaBot2.CommandHandling;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EchaBot2.Bots
{
    public class EchaBot<T> : ActivityHandler where T : Dialog
    {
        protected readonly ILogger Logger;
        protected readonly BotState ConversationState;
        protected readonly Dialog Dialog;
        protected readonly BotState UserState;


        public EchaBot(ILogger<EchaBot<T>> logger,
            BotState conversationState, T dialog, BotState userState)
        {
            Logger = logger;
            ConversationState = conversationState;
            Dialog = dialog;
            UserState = userState;
        }

        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default)
        {
            await base.OnTurnAsync(turnContext, cancellationToken);

            // Save any state changes that might have occurred during the turn.
            await ConversationState.SaveChangesAsync(turnContext, false, cancellationToken);
            await UserState.SaveChangesAsync(turnContext, false, cancellationToken);

            //Agent Hero Card
            Command showOptionsCommand = new Command(Commands.ShowOptions);
            Command helpCommand = new Command(Commands.Help);

            HeroCard heroCard = new HeroCard()
            {
                Title = "Halo!",
                Subtitle = "Saya EchaBot",
                Text = $"Tujuan saya adalah sebagai Bot yang memberikan informasi seputar akademik, serta dapat menjadi penghubung bagi staff akademik dengan pengguna. Tekan/sentuh tombol di bawah atau ketik \"{new Command(Commands.ShowOptions).ToString()}\"",
                Buttons = new List<CardAction>()
                {
                    new()
                    {
                        Title = "Tampilkan Opsi",
                        Value = showOptionsCommand.ToString(),
                        Type = ActionTypes.ImBack
                    },
                    new()
                    {
                        Title = "Bantuan",
                        Value = helpCommand.ToString(),
                        Type = ActionTypes.ImBack
                    }
                }
            };

            Activity replyActivity = turnContext.Activity.CreateReply();
            replyActivity.Attachments = new List<Attachment>() { heroCard.ToAttachment() };

            // ATUR HERO CARD UNTUK ADMIN ONLY
            if (turnContext.Activity.From.Id.Contains("@"))
            {
                await turnContext.SendActivityAsync(replyActivity, cancellationToken);
            }
        }

        // ATUR DIALOG UNTUK USER ONLY
        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            Logger.LogInformation("Running dialog with Message Activity.");

            if (!turnContext.Activity.From.Id.Contains("@"))
            {
                await Dialog.RunAsync(turnContext, ConversationState.CreateProperty<DialogState>("DialogState"), cancellationToken);
            }
        }
    }
}
