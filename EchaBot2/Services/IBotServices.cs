using Microsoft.Bot.Builder.AI.Luis;
using System.Threading.Tasks;

namespace EchaBot2.Services
{
    public interface IBotServices
    {
        LuisRecognizer LuisIntentRecognizer { get; }
        Task<string> GetAcademicAnswer(string question);
        Task<string> GetChitchatAnswer(string question);
    }
}
