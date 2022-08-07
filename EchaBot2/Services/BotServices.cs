using EchaBot2.Models;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace EchaBot2.Services
{
    public class BotServices : IBotServices
    {
        private readonly string _endpointKey;
        private readonly string _chitchatUri;
        private readonly string _academicUri;
        public LuisRecognizer LuisIntentRecognizer { get; private set; }

        public BotServices(IConfiguration configuration)
        {
            // Read the setting for cognitive services (LUIS, QnA) from the appsettings.json
            // If includeApiResults is set to true, the full response from the LUIS api (LuisResult)
            // will be made available in the properties collection of the RecognizerResult
            _endpointKey = configuration["QnAEndpointKey"];
            _chitchatUri = configuration["ChitchatUri"];
            _academicUri = configuration["AcademicUri"];

            LuisIntentRecognizer = CreateLuisRecognizer(configuration);
        }

        private LuisRecognizer CreateLuisRecognizer(IConfiguration configuration)
        {
            var luisApplication = new LuisApplication(
                configuration["LuisAppId"],
                configuration["LuisAPIKey"],
                configuration["LuisApiEndpointUrl"]
            );

            var recognizer = new LuisRecognizerOptionsV2(luisApplication)
            {
                IncludeAPIResults = true,
                PredictionOptions = new LuisPredictionOptions()
                {
                    IncludeAllIntents = false,
                    IncludeInstanceData = false
                }
            };

            return new LuisRecognizer(recognizer);
        }

        private async Task<string> Post(string uri, string body)
        {
            using var client = new HttpClient();
            using var request = new HttpRequestMessage();
            request.Method = HttpMethod.Post;
            request.RequestUri = new Uri(uri);
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");
            request.Headers.Add("Authorization", "EndpointKey " + _endpointKey);

            var response = await client.SendAsync(request);
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> GetAcademicAnswer(string question)
        {
            var uri = _academicUri;
            var questionJson = "{\"question\": \"" + question.Replace("\"", "'") + "\"}";

            var response = await Post(uri, questionJson);

            var answers = JsonConvert.DeserializeObject<QnAResponseDto>(response);
            if (answers != null && answers.Answers.Count > 0)
            {
                return answers.Answers[0].Answer;
            }
            else
            {
                return "Maaf, saya belum bisa menjawab. Silakan mengguankan kata lain.";
            }
        }
        public async Task<string> GetChitchatAnswer(string question)
        {
            var uri = _chitchatUri;
            var questionJson = "{\"question\": \"" + question.Replace("\"", "'") + "\"}";

            var response = await Post(uri, questionJson);

            var answers = JsonConvert.DeserializeObject<QnAResponseDto>(response);
            if (answers != null && answers.Answers.Count > 0)
            {
                return answers.Answers[0].Answer;
            }
            else
            {
                return "Maaf, saya belum bisa menjawab. Silakan mengguankan kata lain.";
            }
        }
    }
}
