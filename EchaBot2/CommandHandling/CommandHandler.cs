using EchaBot2.MessageRouting;
using EchaBot2.Models;
using EchaBot2.Resources;
using EchaBot2.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Underscore.Bot.MessageRouting;
using Underscore.Bot.MessageRouting.DataStore;
using Underscore.Bot.MessageRouting.Results;

namespace EchaBot2.CommandHandling
{
    /// <summary>
    /// Handler for bot commands related to message routing.
    /// </summary>
    public class CommandHandler
    {
        private readonly MessageRouter _messageRouter;
        private readonly MessageRouterResultHandler _messageRouterResultHandler;
        private readonly ConnectionRequestHandler _connectionRequestHandler;
        private readonly IList<string> _permittedAggregationChannels;
        private readonly DbUtility _dbUtility;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="messageRouter">The message router.</param>
        /// <param name="messageRouterResultHandler">A MessageRouterResultHandler instance for
        /// handling possible routing actions such as accepting connection requests.</param>
        /// <param name="connectionRequestHandler">The connection request handler.</param>
        /// <param name="context">Save changes in Db</param>
        /// <param name="permittedAggregationChannels">Permitted aggregation channels.
        /// Null list means all channels are allowed.</param>
        public CommandHandler(
            MessageRouter messageRouter,
            MessageRouterResultHandler messageRouterResultHandler,
            ConnectionRequestHandler connectionRequestHandler, IList<string> permittedAggregationChannels, DbUtility dbUtility)
        {
            _messageRouter = messageRouter;
            _messageRouterResultHandler = messageRouterResultHandler;
            _connectionRequestHandler = connectionRequestHandler;
            _permittedAggregationChannels = permittedAggregationChannels;
            _dbUtility = dbUtility;
        }

        /// <summary>
        /// Checks the given activity for a possible command.
        /// </summary>
        /// <param name="context">The context containing the activity, which in turn may contain a possible command.</param>
        /// <returns>True, if a command was detected and handled. False otherwise.</returns>
        public virtual async Task<bool> HandleCommandAsync(ITurnContext context)
        {
            var activity = context.Activity;
            var command = Command.FromMessageActivity(activity);

            if (command == null)
            {
                // Check for back channel command
                command = Command.FromChannelData(activity);
            }

            if (command == null)
            {
                return false;
            }

            var wasHandled = false;
            Activity replyActivity = null;
            var sender = MessageRouter.CreateSenderConversationReference(activity);

            // Add the sender's channel/conversation into the list of aggregation channels
            var isPermittedAggregationChannel = false;
            if (_permittedAggregationChannels is { Count: > 0 })
            {
                foreach (var permittedAggregationChannel in _permittedAggregationChannels)
                {
                    if (!string.IsNullOrWhiteSpace(activity.ChannelId)
                        && activity.ChannelId.ToLower().Equals(permittedAggregationChannel.ToLower()))
                    {
                        isPermittedAggregationChannel = true;
                        break;
                    }
                }
            }
            else
            {
                isPermittedAggregationChannel = true;
            }

            if (!isPermittedAggregationChannel)
            {
                replyActivity = activity.CreateReply(
                    string.Format(Strings.NotPermittedAggregationChannel, activity.ChannelId));
                wasHandled = true;
            }
            else
            {
                // check sender connection
                var connectionReference = _messageRouter.RoutingDataManager.FindConnection(sender);

                switch (command.BaseCommand)
                {
                    case Commands.ShowOptions:
                        // Present all command options in a card
                        replyActivity = CommandCardFactory.AddCardToActivity(
                                activity.CreateReply(), CommandCardFactory.CreateCommandOptionsCard(activity.Recipient?.Name));
                        wasHandled = true;
                        break;

                    case Commands.Watch:
                        var aggregationChannelToAdd = new ConversationReference(
                                null, null, null,
                                activity.Conversation, activity.ChannelId, activity.ServiceUrl);

                        var modifyRoutingDataResult =
                            _messageRouter.RoutingDataManager.AddAggregationChannel(aggregationChannelToAdd);

                        if (modifyRoutingDataResult.Type == ModifyRoutingDataResultType.Added)
                        {
                            replyActivity = activity.CreateReply(Strings.AggregationChannelSet);
                        }
                        else if (modifyRoutingDataResult.Type == ModifyRoutingDataResultType.AlreadyExists)
                        {
                            replyActivity = activity.CreateReply(Strings.AggregationChannelAlreadySet);
                        }
                        else if (modifyRoutingDataResult.Type == ModifyRoutingDataResultType.Error)
                        {
                            replyActivity = activity.CreateReply(
                                string.Format(Strings.FailedToSetAggregationChannel, modifyRoutingDataResult.ErrorMessage));
                        }

                        wasHandled = true;
                        break;

                    case Commands.Unwatch:
                        if (connectionReference == null)
                        {
                            // Remove the sender's channel/conversation from the list of aggregation channels
                            if (_messageRouter.RoutingDataManager.IsAssociatedWithAggregation(sender))
                            {
                                var aggregationChannelToRemove = new ConversationReference(
                                    null, null, null,
                                    activity.Conversation, activity.ChannelId, activity.ServiceUrl);

                                if (_messageRouter.RoutingDataManager.RemoveAggregationChannel(aggregationChannelToRemove))
                                {
                                    replyActivity = activity.CreateReply(Strings.AggregationChannelRemoved);
                                }
                                else
                                {
                                    replyActivity = activity.CreateReply(Strings.FailedToRemoveAggregationChannel);
                                }
                            }
                        }
                        else
                        {
                            replyActivity = activity.CreateReply(Strings.HandoffActivityIsOnGoing);
                        }

                        wasHandled = true;
                        break;

                    case Commands.GetRequests:
                        var connectionRequests =
                            _messageRouter.RoutingDataManager.GetConnectionRequests();

                        replyActivity = activity.CreateReply();

                        // The sender is associated with the aggregation and has the right to accept/reject
                        if (_messageRouter.RoutingDataManager.IsAssociatedWithAggregation(sender))
                        {
                            if (connectionRequests.Count == 0)
                            {
                                replyActivity.Text = Strings.NoPendingRequests;
                            }
                            else
                            {
                                replyActivity.Attachments = CommandCardFactory.CreateMultipleConnectionRequestCards(
                                    connectionRequests, activity.Recipient?.Name);
                            }

                            replyActivity.ChannelData = JsonConvert.SerializeObject(connectionRequests);
                        }
                        else
                        {
                            replyActivity.Text = Strings.NotifyOwnerActivateWatchCommand;
                            replyActivity.SuggestedActions = new SuggestedActions
                            {
                                Actions = new List<CardAction> { new() { Title = "Watch", Type = ActionTypes.ImBack, Value = "command Watch" } }
                            };
                        }

                        wasHandled = true;
                        break;

                    case Commands.AcceptRequest:
                    case Commands.RejectRequest:
                        // Accept/reject connection request
                        var doAccept = (command.BaseCommand == Commands.AcceptRequest);

                        replyActivity = activity.CreateReply();

                        // The sender is associated with the aggregation and has the right to accept/reject
                        if (_messageRouter.RoutingDataManager.IsAssociatedWithAggregation(sender))
                        {
                            if (command.Parameters.Count == 0)
                            {
                                connectionRequests =
                                    _messageRouter.RoutingDataManager.GetConnectionRequests();

                                if (connectionRequests.Count == 0)
                                {
                                    replyActivity.Text = Strings.NoPendingRequests;
                                }
                                else
                                {
                                    replyActivity = CommandCardFactory.AddCardToActivity(
                                        replyActivity, CommandCardFactory.CreateMultiConnectionRequestCard(
                                            connectionRequests, doAccept, activity.Recipient?.Name));
                                }
                            }
                            else if (!doAccept
                                && command.Parameters[0].Equals(Command.CommandParameterAll))
                            {
                                // Reject all pending connection requests
                                if (!await _connectionRequestHandler.RejectAllPendingRequestsAsync(
                                        _messageRouter, _messageRouterResultHandler))
                                {
                                    replyActivity = activity.CreateReply();
                                    replyActivity.Text = Strings.FailedToRejectPendingRequests;
                                }
                                else
                                {
                                    replyActivity = activity.CreateReply();
                                    replyActivity.Text = Strings.RejectAllPendingRequest;
                                }
                            }
                            else if (command.Parameters.Count > 1)
                            {
                                // check sender if accept a new connection request and already in live chat activity
                                if (doAccept && connectionReference != null)
                                {
                                    replyActivity = activity.CreateReply(Strings.HandoffActivityIsOnGoing);
                                }
                                else
                                {
                                    // Try to accept/reject the specified connection request
                                    var requestorChannelAccount =
                                        new ChannelAccount(command.Parameters[0]);
                                    var requestorConversationAccount =
                                        new ConversationAccount(null, null, command.Parameters[1]);

                                    var messageRouterResult =
                                        await _connectionRequestHandler.AcceptOrRejectRequestAsync(
                                            _messageRouter, _messageRouterResultHandler, sender, doAccept,
                                            requestorChannelAccount, requestorConversationAccount);

                                    await _messageRouterResultHandler.HandleResultAsync(messageRouterResult);
                                }
                            }
                            else
                            {
                                replyActivity = activity.CreateReply(Strings.InvalidOrMissingCommandParameter);
                            }
                        }
                        else // send message to activate watch command first
                        {
                            replyActivity.Text = Strings.NotifyOwnerActivateWatchCommand;
                            replyActivity.SuggestedActions = new SuggestedActions
                            {
                                Actions = new List<CardAction> { new() { Title = "Watch", Type = ActionTypes.ImBack, Value = "command Watch" } }
                            };
                        }

                        wasHandled = true;
                        break;

                    case Commands.Disconnect:
                        // End the 1:1 conversation(s)
                        var disconnectResults = _messageRouter.Disconnect(sender);

                        if (disconnectResults is { Count: > 0 })
                        {
                            var convId = connectionReference.ConversationReference2.Conversation.Id;
                            int index = convId.IndexOf("|", StringComparison.Ordinal);
                            if (index >= 0)
                                convId = convId.Substring(0, index);

                            var userId = connectionReference.ConversationReference2.User.Id;

                            var chatHistoryInDb = await _dbUtility.DbContext.ChatHistories.SingleOrDefaultAsync(c => c.ChatHistoryFileName == convId);
                            var emailQuestionInDb = await _dbUtility.DbContext.ChatBotEmailQuestions.SingleOrDefaultAsync(e => e.Id == convId);

                            // if data exist in db, update value
                            if (chatHistoryInDb != null && emailQuestionInDb != null)
                            {
                                emailQuestionInDb.IsAnswered = true;
                                chatHistoryInDb.IsDoneOnLiveChat = true;

                                await _dbUtility.SaveChangesAsync();
                            }

                            // if data didn't exist in db, create new record
                            if (chatHistoryInDb == null)
                            {
                                var chatHistory = new ChatHistory
                                {
                                    UserId = userId,
                                    IsDoneOnBot = false,
                                    IsDoneOnEmail = false,
                                    IsDoneOnLiveChat = true,
                                    ChatHistoryFileName = convId
                                };

                                await _dbUtility.InsertChatHistory(chatHistory);
                            }

                            foreach (var disconnectResult in disconnectResults)
                            {
                                await _messageRouterResultHandler.HandleResultAsync(disconnectResult);
                            }

                            wasHandled = true;
                        }

                        break;

                    case Commands.Help:
                        replyActivity = activity.CreateReply();
                        replyActivity.Text = "Penjelasan perintah Bot sebagai Admin:\n\n" +
                                             "1. [ShowOptions] menampilkan perintah-perintah bagi Bot.\n" +
                                             "2. [Watch] menjadikan saluran saat ini sebagai saluran agregasi permintaan yang masuk.\n" +
                                             "3. [Unwatch] menghapus saluran saat ini dari daftar saluran agregasi.\n" +
                                             "4. [GetRequests] menghasilkan daftar semua permintaan koneksi yang tertunda.\n" +
                                             "5. [AcceptRequest <user ID>] menerima permintaan koneksi percakapan dari pengguna yang diberikan.*\n" +
                                             "6. [RejectRequest <user ID>] menolak permintaan koneksi percakapan dari pengguna yang diberikan.*\n" +
                                             "7. [Disconnect] mengakhiri percakapan saat ini dengan pengguna.\n" +
                                             "\n*Jika tidak ada ID pengguna yang dimasukkan, bot akan membuat kartu yang bagus dengan tombol terima/tolak karena ada permintaan koneksi yang tertunda.";

                        wasHandled = true;
                        break;

                    default:
                        replyActivity = activity.CreateReply(string.Format(Strings.CommandNotRecognized, command.BaseCommand));
                        break;
                }
            }

            if (replyActivity != null)
            {
                await context.SendActivityAsync(replyActivity);
            }

            return wasHandled;
        }

        /// <summary>
        /// Checks the given activity and determines whether the message was addressed directly to
        /// the bot or not.
        /// 
        /// Note: Only mentions are inspected at the moment.
        /// </summary>
        /// <param name="messageActivity">The message activity.</param>
        /// <param name="strict">Use false for channels that do not properly support mentions.</param>
        /// <returns>True, if the message was address directly to the bot. False otherwise.</returns>
        public bool WasBotAddressedDirectly(IMessageActivity messageActivity, bool strict = true)
        {
            var botWasMentioned = false;

            if (strict)
            {
                var mentions = messageActivity.GetMentions();

                foreach (var mention in mentions)
                {
                    foreach (var bot in _messageRouter.RoutingDataManager.GetBotInstances())
                    {
                        if (mention.Mentioned.Id.Equals(RoutingDataManager.GetChannelAccount(bot).Id))
                        {
                            botWasMentioned = true;
                            break;
                        }
                    }
                }
            }
            else
            {
                // Here we assume the message starts with the bot name, for instance:
                //
                // * "@<BOT NAME>..."
                // * "<BOT NAME>: ..."
                var botName = messageActivity.Recipient?.Name;
                var message = messageActivity.Text?.Trim();

                if (!string.IsNullOrEmpty(botName) && !string.IsNullOrEmpty(message) && message.Length > botName.Length)
                {
                    try
                    {
                        message = message.Remove(botName.Length + 1, message.Length - botName.Length - 1);
                        botWasMentioned = message.Contains(botName);
                    }
                    catch (ArgumentOutOfRangeException e)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to check if bot was mentioned: {e.Message}");
                    }
                }
            }

            return botWasMentioned;
        }
    }
}
