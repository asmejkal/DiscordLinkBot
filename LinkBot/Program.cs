using Disqord.Bot.Hosting;
using Disqord.Gateway;
using LinkBot.Services.BStage;
using LinkBot.Services.Common;
using LinkBot.Services.Instagram;
using LinkBot.Services.Threads;
using LinkBot.Services.Twitter;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

await new HostBuilder()
    .ConfigureAppConfiguration(configuration =>
    {
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
        services.AddOptions<EzInstagramOptions>().BindConfiguration("EzInstagram").ValidateOnStart();

        services.AddHttpClient<IMediaClient, MediaClient>();
        services.AddHttpClient<IBStageClient, BStageClient>();
        services.AddHttpClient<IThreadsClient, ThreadsClient>();
        services.AddHttpClient<ITwitterClient, TwitterClient>()
            .ConfigureHttpClient(x => x.DefaultRequestHeaders.UserAgent.ParseAdd("LinkBot/1.0"));
        services.AddHttpClient<IInstagramClient, DdInstagramClient>()
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler() { AllowAutoRedirect = false });
        services.AddHttpClient<IInstagramClient, EzInstagramClient>();
    })
    .RunConsoleAsync();