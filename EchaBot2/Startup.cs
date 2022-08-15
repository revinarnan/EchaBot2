// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Generated with Bot Builder V4 SDK Template for Visual Studio EchoBot v4.15.2

using EchaBot2.Bots;
using EchaBot2.ComponentDialogs;
using EchaBot2.Middleware;
using EchaBot2.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.Cosmos;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Azure.Blobs;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EchaBot2
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var client = new CosmosClientOptions
            {
                ConnectionMode = ConnectionMode.Gateway
            };

            var stateBlobConfig = new BlobsStorage(
                Configuration.GetValue<string>("AzureTableStorageConnectionString"),
                Configuration.GetValue<string>("ConversationStateContainerName")
            );

            var historyBlobConfig = new BlobsStorage(
                Configuration.GetValue<string>("AzureTableStorageConnectionString"),
                Configuration.GetValue<string>("ConversationHistoryContainerName")
            );

            // Add SQL Server database DbContext
            services.AddDbContext<DbContext>(op => op.UseSqlServer(Configuration.GetConnectionString("Database")));

            //AddNewtonsoftJson
            services.AddHttpClient().AddControllers();

            // Create the Bot Framework Authentication to be used with the Bot Adapter.
            services.AddSingleton<BotFrameworkAuthentication, ConfigurationBotFrameworkAuthentication>();

            // Create the Bot Adapter with error handling enabled.
            services.AddSingleton<IBotFrameworkHttpAdapter, AdapterWithErrorHandler>();

            // Register storage
            services.AddSingleton<IStorage>(stateBlobConfig);
            services.AddSingleton<IStorage>(historyBlobConfig);

            //services.AddSingleton<IStorage>(cosmosConfig);

            // Create the User state. (Used in this bot's Dialog implementation.)
            var userState = new UserState(stateBlobConfig);
            services.AddSingleton(userState);

            // Create the Conversation state. (Used by the Dialog system itself.)
            var conversationState = new ConversationState(stateBlobConfig);
            services.AddSingleton(conversationState);

            // Create the bot services (LUIS, QnA) as a singleton.
            services.AddSingleton<IBotServices, BotServices>();

            // Register AcademicWaterfallDialog.
            services.AddSingleton<AcademicWaterfallDialog>();

            // Register ClosingWaterfallDialog
            services.AddSingleton<ClosingWaterfallDialog>();

            // The MainDialog that will be run by the bot.
            services.AddSingleton<MainDialog>();
            services.AddSingleton<DbUtility>();

            // Add TranscriptLogger Middleware
            var transcriptMiddleware = new TranscriptLoggerMiddleware(new TextLoggerMiddleware(historyBlobConfig));
            services.AddSingleton(transcriptMiddleware);

            // Add Handoff Middleware
            services.AddSingleton<HandoffMiddleware>();

            // Create the bot as a transient. In this case the ASP Controller is expecting an IBot.
            services.AddTransient<IBot, WelcomeDialogBot<MainDialog>>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseDefaultFiles()
                .UseStaticFiles()
                .UseWebSockets()
                .UseRouting()
                .UseAuthorization()
                .UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers();
                });

            // app.UseHttpsRedirection();
        }
    }
}
