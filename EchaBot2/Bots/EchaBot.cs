// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Generated with Bot Builder V4 SDK Template for Visual Studio EchoBot v4.15.2

using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
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
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            //var dc = new DialogContext(new DialogSet(), turnContext, new DialogState());
            //// Top intent tell us which cognitive service to use.
            //var allScores = await BotServices.LuisIntentRecognizer.RecognizeAsync(dc, (Activity)turnContext.Activity, cancellationToken);
            //var topIntent = allScores.Intents.First().Key;

            //// Next, we call the dispatcher with the top intent.
            //await DispatchToTopIntentAsync(turnContext, topIntent, cancellationToken);

            Logger.LogInformation("Running dialog with Message Activity.");

            await Dialog.RunAsync(turnContext, ConversationState.CreateProperty<DialogState>("DialogState"), cancellationToken);
        }

        /*private async Task DispatchToTopIntentAsync(ITurnContext<IMessageActivity> turnContext, string topIntent, CancellationToken cancellationToken)
        {
            var chitchatIntent = new List<string>
            {
                "About_User", "Agent_About_Life", "Agent_About_Love", "Agent_About_Technology", "Agent_Acquaintance",
                "Agent_Age", "Agent_Annoying", "Agent_Answer_My_Question", "Agent_Ask_Something", "Agent_Bad",
                "Agent_Be_Clever", "Agent_Be_Entertaining", "Agent_Beautiful", "Agent_Birth_Date", "Agent_Boring",
                "Agent_Boss", "Agent_Busy", "Agent_Can_You_Help", "Agent_Capability", "Agent_Chatbot", "Agent_Clever",
                "Agent_Crazy", "Agent_Creator", "Agent_Doing", "Agent_Family", "Agent_Favorite", "Agent_Fired",
                "Agent_Funny", "Agent_Gender", "Agent_Good", "Agent_Happy", "Agent_Hobby", "Agent_Hungry",
                "Agent_Intention", "Agent_Job", "Agent_Joking", "Agent_Lovely", "Agent_Loves_User", "Agent_Marry_User",
                "Agent_My_Friend", "Agent_Not_Funny", "Agent_Occupation", "Agent_Origin", "Agent_Other_Response",
                "Agent_Ready", "Agent_Real", "Agent_Relationship", "Agent_Residence", "Agent_Right", "Agent_Sex_Issue",
                "Agent_Singing", "Agent_Skill", "Agent_Sure", "Agent_Talk_To_Me", "Agent_There", "Agent_Ugly",
                "Another_Greetings", "Appraisal_Bad", "Appraisal_Good", "Appraisal_No_Problem", "Appraisal_Thank_You",
                "Appraisal_Welcome", "Appraisal_Well_Done", "Confirmation_Cancel", "Confirmation_No", "Confirmation_Yes",
                "Dialog_Hold_On", "Dialog_Hug", "Dialog_I_Do_Not_Care", "Dialog_Sorry", "Dialog_What_Do_You_Mean",
                "Dialog_Wrong", "Emotions_Ha_Ha", "Emotions_Wow", "Great", "Greetings_Another_Bot", "Greetings_Bye",
                "Greetings_Good_Evening", "Greetings_Good_Morning", "Greetings_Good_Night", "Greetings_Hello",
                "Greetings_How_Are_You", "Greetings_Nice_To_Meet_You", "Greetings_Nice_To_Talk_To_You",
                "How_Is_Agent_Day", "Is_User_Lovely", "None_Random_Topic", "Other_Bot", "Other_Bot_Do_Better",
                "User_Academic_Problem", "User_Agent_Appearance_Comparison", "User_Agent_Smart_Comparison",
                "User_Angry", "User_Back", "User_Bored", "User_Busy", "User_Can_Not_Sleep", "User_Date_Agent",
                "User_Does_Not_Want_To_Talk", "User_Favorite", "User_Going_To_Bed", "User_Good", "User_Happy",
                "User_Has_Birthday", "User_Here", "User_Hungry", "User_Joking", "User_Likes_Agent", "User_Lonely",
                "User_Looks_Like", "User_Loves_Agent", "User_Misses_Agent", "User_Needs_Advice", "User_Nervous",
                "User_Sad", "User_Sleepy", "User_Testing_Agent", "User_Tired", "User_Waits",
                "User_Wants_To_See_Agent_Again", "User_Wants_To_Talk", "User_Will_Be_Back", "What_Is_Agent",
                "Greetings_Good_Afternoon", "Greetings_Good_Afternoon_sore", "User_Sick", "User_Hope"
            };

            switch (topIntent)
            {
                case "Academic":
                    await ProcessAcademicResponseAsync(turnContext, cancellationToken);
                    break;
                case "None":
                    await ShowLuisResult(turnContext, cancellationToken);
                    break;
                default:
                    await ProcessChitchatResponseAsync(turnContext, cancellationToken);
                    break;
            }

            //if (topIntent == "Academic")
            //{
            //    await this.ProcessAcademicResponseAsync(turnContext, cancellationToken);
            //}
            //else if (chitchatIntent.Contains(topIntent))
            //{
            //    await this.ProcessChitchatResponseAsync(turnContext, cancellationToken);
            //}
            //else
            //{
            //    Logger.LogInformation($"Dispatch unrecognized intent: {topIntent}.");
            //    await this.ShowLuisResult(turnContext, cancellationToken);
            //}
        }

        private async Task ProcessAcademicResponseAsync(ITurnContext<IMessageActivity> turnContext,
            CancellationToken cancellationToken)
        {
            Logger.LogInformation("ProcessAcademicResponseAsync");

            var results = await BotServices.AcademicKb.GetAnswersAsync(turnContext);

            if (results.Any())
            {
                await turnContext.SendActivityAsync(MessageFactory.Text(results.First().Answer), cancellationToken);
                await Dialog.RunAsync(turnContext, ConversationState.CreateProperty<DialogState>(nameof(DialogState)), cancellationToken);
            }
            else
            {
                await turnContext.SendActivityAsync(MessageFactory.Text("Maaf, informasi tidak ditemukan. Mohon gunakan kata lain."), cancellationToken);
            }
        }

        private async Task ProcessChitchatResponseAsync(ITurnContext<IMessageActivity> turnContext,
            CancellationToken cancellationToken)
        {
            Logger.LogInformation("ProcessChitchatResponseAsync");

            var results = await BotServices.ChitchatKb.GetAnswersAsync(turnContext);

            if (results.Any())
            {
                if (results.First().Answer.Equals("No good match found in KB."))
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text("Maaf, saya belum bisa menjawab. Silakan mengguankan kata lain"), cancellationToken);
                }

                await turnContext.SendActivityAsync(MessageFactory.Text(results.First().Answer), cancellationToken);
            }
            else
            {
                await turnContext.SendActivityAsync(MessageFactory.Text("Maaf, saya belum bisa menjawab."), cancellationToken);
            }
        }

        private async Task ShowLuisResult(ITurnContext<IMessageActivity> turnContext,
            CancellationToken cancellationToken)
        {
            await turnContext.SendActivityAsync(MessageFactory.Text("Maaf saya belum mengerti yang kamu katakan, saya harus belajar lagi."), cancellationToken);
        }*/
    }
}
