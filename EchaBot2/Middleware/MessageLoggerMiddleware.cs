using Microsoft.Bot.Builder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EchaBot2.Middleware
{
    public class MessageLoggerMiddleware : IMiddleware
    {
        private readonly IStorage _storage;

        // Create cancellation token (used by Async Write operation).
        public CancellationToken CancellationToken { get; private set; }

        // Class for storing a log of utterances (text of messages) as a list.
        public class MessageLog : IStoreItem
        {
            // A list of things that users have said to the bot
            public List<string> UtteranceList { get; } = new();

            // Create concurrency control where this is used.
            public string ETag { get; set; } = "*";
        }

        public MessageLoggerMiddleware(IStorage storage)
        {
            _storage = storage;
        }

        public async Task OnTurnAsync(ITurnContext turnContext, NextDelegate next,
            CancellationToken cancellationToken = new())
        {
            if (turnContext.Activity.Type == "message")
            {
                var dateStamp = DateTime.Today.ToString("dd-MM-yyyy");

                const int delay = 100;

                var logText = $"{turnContext.Activity.From.Name}: {turnContext.Activity.Text}";
                var fileName = $"{dateStamp}_{turnContext.Activity.Conversation.Id}";

                MessageLog logItems = null;
                // See if there are previous messages saved in storage.
                try
                {
                    string[] utteranceList = { fileName };
                    logItems = _storage.ReadAsync<MessageLog>(utteranceList, cancellationToken).Result?.FirstOrDefault().Value;
                }
                catch
                {
                    // Inform the user an error occurred.
                    await turnContext.SendActivityAsync("Sorry, something went wrong reading your stored messages!", cancellationToken: cancellationToken);
                }

                if (logItems is null)
                {
                    logItems = new MessageLog();
                    logItems.UtteranceList.Add(logText);

                    var document = new Dictionary<string, object>();
                    {
                        document.Add(fileName, logItems);
                    }
                    try
                    {
                        await Task.Delay(delay, cancellationToken);
                        // Save user message to storage
                        await _storage.WriteAsync(document, CancellationToken);
                    }
                    catch
                    {
                        await turnContext.SendActivityAsync("Sorry, something went wrong storing your message!", cancellationToken: cancellationToken);
                    }
                }
                else
                {
                    logItems.UtteranceList.Add(logText);

                    var document = new Dictionary<string, object>();
                    {
                        document.Add(fileName, logItems);
                    }
                    try
                    {
                        await Task.Delay(delay, cancellationToken);
                        await _storage.WriteAsync(document, cancellationToken);
                    }
                    catch
                    {
                        await turnContext.SendActivityAsync("Sorry, something went wrong storing your message!", cancellationToken: cancellationToken);
                    }
                }

                await next(cancellationToken);
            }
            else
            {
                await next(cancellationToken);
            }
        }
    }
}
