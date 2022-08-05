using EchaBot2.Models;
using System;
using System.Threading.Tasks;

namespace EchaBot2
{
    public class DbUtility
    {
        public ApplicationDbContext DbContext { get; }

        public DbUtility()
        {
            DbContext = new ApplicationDbContext();
        }

        public async Task SaveChangesAsync()
        {
            try
            {
                await DbContext.SaveChangesAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public async Task InsertEmailQuestion(ChatBotEmailQuestion emailQuestions)
        {
            try
            {
                DbContext.ChatBotEmailQuestions.Add(emailQuestions);
                await DbContext.SaveChangesAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public async Task InsertChatHistory(ChatHistory history)
        {
            try
            {
                DbContext.ChatHistories.Add(history);
                await DbContext.SaveChangesAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
