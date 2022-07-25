using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EchaBot2.Middleware
{
    public class TextLoggerMiddleware : ITranscriptLogger
    {
        private readonly IStorage _storage;

        // Create cancellation token (used by Async Write operation).
        public CancellationToken CancellationToken { get; set; }

        //Class for storing a log of utterances(text of messages) as a list.
        public class MessageLog : IStoreItem
        {
            public string ConversationId { get; set; }
            public string Date { get; set; }
            // A list of things that users have said to the bot
            public List<string> TextList { get; } = new();

            // Create concurrency control where this is used.
            public string ETag { get; set; } = "*";
        }

        public TextLoggerMiddleware(IStorage storage)
        {
            _storage = storage;
        }

        public async Task LogActivityAsync(IActivity activity)
        {
            if (activity.Type == ActivityTypes.Message && !activity.From.Name.Contains("@"))
            {
                // Preserve message input
                var logText = $"{activity.From.Name}: {activity.AsMessageActivity().Text}";

                // activity only contains Text if this is a message
                var isMessage = activity.AsMessageActivity() != null;
                if (isMessage)
                {
                    // Preserve document file name
                    var dateStamp = DateTime.UtcNow.AddHours(7).ToString("dddd, dd MMMM yyyy hh:mm tt");
                    var convId = activity.Conversation.Id;
                    int index = convId.IndexOf("|", StringComparison.Ordinal);
                    if (index >= 0)
                        convId = convId.Substring(0, index);
                    var fileName = $"{convId}";

                    // adjust delay to save in cosmos
                    const int delay = 100;

                    MessageLog logItems;
                    // see if there are previous messages saved in storage.
                    try
                    {
                        string[] textList = { fileName };
                        logItems = _storage.ReadAsync<MessageLog>(textList, CancellationToken).Result?.FirstOrDefault().Value;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        throw;
                    }

                    // If no stored messages were found, create and store a new entry.
                    if (logItems is null)
                    {
                        // add the current utterance to a new object.
                        logItems = new MessageLog();
                        logItems.ConversationId = convId;
                        logItems.Date = dateStamp;
                        logItems.TextList.Add(logText);

                        // Create Dictionary object to hold messages.
                        var document = new Dictionary<string, object>();

                        document.Add(fileName, logItems);
                        document[fileName] = logItems;

                        try
                        {
                            // Save message to storage
                            await Task.Delay(delay, CancellationToken);
                            await _storage.WriteAsync(document, CancellationToken);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            throw;
                        }
                    }

                    // Else, if Storage already contained saved messages, add new one to the list.
                    else
                    {
                        // Add new message to list
                        logItems.TextList.Add(logText);

                        // Create dictionary object to hold new list of messages.
                        var document = new Dictionary<string, object>();
                        {
                            document.Add(fileName, logItems);
                        }

                        try
                        {
                            // Save new list to storage
                            await Task.Delay(delay, CancellationToken);
                            await _storage.WriteAsync(document, CancellationToken);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            throw;
                        }
                    }
                }
            }

        }
    }
}
