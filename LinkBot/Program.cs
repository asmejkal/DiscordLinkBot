using Disqord.Bot.Hosting;
using Disqord.Gateway;
using LinkBot.Services.BStage;
using LinkBot.Services.Common;
using LinkBot.Services.Instagram;
using LinkBot.Services.Threads;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

await new HostBuilder()
    .ConfigureAppConfiguration(configuration =>
    {
        // We will use the environment variable DISQORD_TOKEN for the bot token.
        configuration.AddEnvironmentVariables("LinkBot_");
    })
    .ConfigureLogging(logging =>
    {
        logging.AddSimpleConsole();
    })
    .ConfigureDiscordBot((context, bot) =>
    {
        bot.Intents = GatewayIntents.Unprivileged;
        bot.Token = context.Configuration["Token"];
    })
    .ConfigureServices(services =>
    {
        services.AddHttpClient<IMediaClient, MediaClient>();
        services.AddHttpClient<IBStageClient, BStageClient>();
        services.AddHttpClient<IThreadsClient, ThreadsClient>();
        services.AddHttpClient<IInstagramClient, InstagramClient>()
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler() { AllowAutoRedirect = false });
    })
    .RunConsoleAsync();