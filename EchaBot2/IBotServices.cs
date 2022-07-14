using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.AI.QnA;

namespace EchaBot2
{
    public interface IBotServices
    {
        LuisRecognizer LuisIntentRecognizer { get; }
        QnAMaker AcademicKb { get; }
        QnAMaker ChitchatKb { get; }
    }
}
