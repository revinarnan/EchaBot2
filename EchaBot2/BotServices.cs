using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.AI.QnA;
using Microsoft.Extensions.Configuration;

namespace EchaBot2
{
    public class BotServices : IBotServices
    {
        public BotServices(IConfiguration configuration)
        {
            // Read the setting for cognitive services (LUIS, QnA) from the appsettings.json
            // If includeApiResults is set to true, the full response from the LUIS api (LuisResult)
            // will be made available in the properties collection of the RecognizerResult
            LuisIntentRecognizer = CreateLuisRecognizer(configuration);

            AcademicKb = new QnAMaker(new QnAMakerEndpoint
            {
                KnowledgeBaseId = configuration["AcademicKnowledgebaseId"],
                EndpointKey = configuration["QnAEndpointKey"],
                Host = configuration["QnAEndpointHostName"]
            });

            ChitchatKb = new QnAMaker(new QnAMakerEndpoint
            {
                KnowledgeBaseId = configuration["ChitchatKnowledgebaseId"],
                EndpointKey = configuration["QnAEndpointKey"],
                Host = configuration["QnAEndpointHostName"]
            });
        }

        public QnAMaker AcademicKb { get; private set; }
        public QnAMaker ChitchatKb { get; private set; }
        public LuisRecognizer LuisIntentRecognizer { get; private set; }

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
                    IncludeAllIntents = true,
                    IncludeInstanceData = true
                }
            };

            return new LuisRecognizer(recognizer);
        }

    }
}
