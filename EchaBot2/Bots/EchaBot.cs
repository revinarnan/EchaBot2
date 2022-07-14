// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Generated with Bot Builder V4 SDK Template for Visual Studio EchoBot v4.15.2

using EchaBot2.CommandHandling;
using EchaBot2.Model;
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
        protected readonly Dialog Dialog;
        private BotState _conversationState;
        private BotState _userState;

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

            // Save any state changes that might have occurred during the turn.
            await _conversationState.SaveChangesAsync(turnContext, false, cancellationToken);
            await _userState.SaveChangesAsync(turnContext, false, cancellationToken);

            // ATUR HERO CARD UNTUK ADMIN ONLY
            if (turnContext.Activity.From.Id.Contains("@"))
            {
                // Agent Hero Card
                Command showOptionsCommand = new Command(Commands.ShowOptions);
                Command helpCommand = new Command(Commands.Help);

                HeroCard heroCard = new HeroCard
                {
                    Title = "Halo!",
                    Subtitle = "Saya EchaBot",
                    Text = $"Tujuan saya adalah sebagai Bot yang memberikan informasi seputar akademik, serta dapat menjadi penghubung bagi staff akademik dengan pengguna. Tekan/sentuh tombol di bawah atau ketik \"{new Command(Commands.ShowOptions).ToString()}\"",
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
        }

        // ATUR DIALOG UNTUK USER ONLY
        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            Logger.LogInformation("Running dialog with Message Activity.");

            // Get the state properties from the turn context.
            var conversationStateAccessors = _conversationState.CreateProperty<ConversationData>(nameof(ConversationData));
            //var conversationData = await conversationStateAccessors.GetAsync(turnContext, () => new ConversationData(), cancellationToken);

            var userStateAccessors = _userState.CreateProperty<UserInfo>(nameof(UserInfo));
            //var userProfile = await userStateAccessors.GetAsync(turnContext, () => new UserInfo(), cancellationToken);


            if (!turnContext.Activity.From.Id.Contains("@"))
            {
                await Dialog.RunAsync(turnContext, _conversationState.CreateProperty<DialogState>("DialogState"), cancellationToken);
            }
        }
    }
}
