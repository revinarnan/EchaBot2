using EchaBot2.Models;
using System;

namespace EchaBot2
{
    public class UserRepository
    {
        public ApplicationDbContext DbContext { get; }

        public UserRepository()
        {
            DbContext = new ApplicationDbContext();
        }

        public bool InsertEmailQuestion(ChatBotEmailQuestion emailQuestions)
        {
            bool status = false;
            try
            {
                DbContext.ChatBotEmailQuestions.Add(emailQuestions);
                DbContext.SaveChanges();
                status = true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return status;
        }
    }
}
