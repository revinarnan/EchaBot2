// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Generated with Bot Builder V4 SDK Template for Visual Studio EchoBot v4.15.2

using EchaBot2.CommandHandling;
using EchaBot2.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EchaBot2.Bots
{
    public class EchaBot<T> : ActivityHandler where T : Dialog
    {
        protected readonly ILogger Logger;
        protected readonly Dialog Dialog;
        private readonly BotState _conversationState;
        private readonly BotState _userState;


        public EchaBot(ILogger<EchaBot<T>> logger,
            BotState conversationState, T dialog, BotState userState)
        {
            Logger = logger;
            Dialog = dialog;
            _conversationState = conversationState;
            _userState = userState;
        }

        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default)
        {
            await base.OnTurnAsync(turnContext, cancellationToken);

            // HERO CARD for ADMIN ONLY
            if (turnContext.Activity.From.Id.Contains("@"))
            {
                // Agent Hero Card
                Command showOptionsCommand = new Command(Commands.ShowOptions);
                Command helpCommand = new Command(Commands.Help);

                HeroCard heroCard = new HeroCard
                {
                    Title = "Halo!",
                    Subtitle = "Saya EchaBot",
                    Text = $"Tujuan saya adalah sebagai Bot yang memberikan informasi seputar akademik, " +
                           $"serta dapat menjadi penghubung bagi staff akademik dengan pengguna. " +
                           $"Tekan/sentuh tombol di bawah atau ketik \"{new Command(Commands.ShowOptions).ToString()}\"",
                    Buttons = new List<CardAction>
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
                replyActivity.Attachments = new List<Attachment> { heroCard.ToAttachment() };
                await turnContext.SendActivityAsync(replyActivity, cancellationToken);
            }

            // Save any state changes that might have occurred during the turn.
            await _conversationState.SaveChangesAsync(turnContext, false, cancellationToken);
            await _userState.SaveChangesAsync(turnContext, false, cancellationToken);
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            Logger.LogInformation("Running dialog with Message Activity.");

            var conversationData = new ConversationData
            {
                Timestamp = DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss tt"),
                ChannelId = turnContext.Activity.ChannelId
            };

            // Get the state properties from the turn context.
            var conversationStateAccessors = _conversationState.CreateProperty<ConversationData>(nameof(ConversationData));
            await conversationStateAccessors.SetAsync(turnContext, conversationData, cancellationToken);

            // Dialog for user only
            if (!turnContext.Activity.From.Id.Contains("@"))
            {
                await Dialog.RunAsync(turnContext, _conversationState.CreateProperty<DialogState>("DialogState"), cancellationToken);
            }
        }
    }
}
