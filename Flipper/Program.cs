
using Discord;
using Discord.WebSocket;

namespace Flipper;

internal class Program
{
    public static int ApiQueryInterval = API_UPDATE_SYNCHRONIZATION_INTERVAL;
    
    private const int API_UPDATE_SYNCHRONIZATION_INTERVAL = 2;
    private const string BOT_TOKEN = "MTAzMzQyODE2MDQ1MDE1NDUzNg.Gkl11H.vivlOYY1-aJcaT4LCRuYy9Y7yg1OB-rIev5_2Q";
    private DiscordSocketClient client;
    private DataFetchService dataFetchService;
    private DataParserService dataParserService;
    private CalculatorService calculatorService;
    
    private static bool syncedWithApiUpdateCycle;
    
    public static Task Main(string[] args) => new Program().MainAsync();

    private async Task MainAsync()
    {
        DiscordSocketConfig config = new()
        {
            ConnectionTimeout = 600000
        };
        
        client = new DiscordSocketClient(config);
        client.Log += Log;
        client.LatencyUpdated += ClientOnLatencyUpdated;
        client.Connected += ClientOnConnected;

        //dataFetchService = new DataFetchService(httpClient);
        //dataParserService = new DataParserService();
        //calculatorService = new CalculatorService();

        await client.LoginAsync(TokenType.Bot, BOT_TOKEN);

        await client.StartAsync();

        // Block this task until the program is closed.
        await Task.Delay(-1);
    }

    private async Task ClientOnConnected()
    {
        bool testsSuccessful = await Flipper.Test();

        if (!testsSuccessful)
        {
            Logger.Error("Tests were not successful. Program execution will not continue.");
            return;
        }
        
        await Flipper.Initialize();
        
        PeriodicTimer timer = new(TimeSpan.FromSeconds(API_UPDATE_SYNCHRONIZATION_INTERVAL));

        while (await timer.WaitForNextTickAsync())
        {
            // When we are synced with the api, we can safely set the query interval to 60s for minimum delay.
            bool wasDataUpdated = await Flipper.Update();

            if (!wasDataUpdated)
                syncedWithApiUpdateCycle = false;
            
            if (syncedWithApiUpdateCycle) continue;
            
            if (wasDataUpdated)
            {
                timer = new PeriodicTimer(TimeSpan.FromSeconds(60));

                syncedWithApiUpdateCycle = true;
                
                Logger.Success("Successfully synced with API update cycle!");
            }
            else
            {
                timer = new PeriodicTimer(TimeSpan.FromSeconds(API_UPDATE_SYNCHRONIZATION_INTERVAL));
                
                Logger.Warn("Not in sync with API update cycle! Fixing...");
            }
        }
    }

    // private async Task CalculateAnomalies()
    // {
    //     //TODO: ________________________________ Move to timed function _______________________________
    //     DataFetchService.DataResponse data = await dataFetchService.FetchData();
    //     DataParserService.TimeSeriesPair seriesPair = await dataParserService.ParseDataResponse(data);
    //     List<CalculatorService.Anomaly> anomalies = calculatorService.GetAnomalies(seriesPair);
    //     foreach (CalculatorService.Anomaly anomaly in anomalies)
    //     {
    //         Console.WriteLine($"{anomaly.AnomalyType}: {anomaly.ItemID}");
    //     }
    // }

    private Task ClientOnLatencyUpdated(int oldLatency, int newLatency)
    {
        IActivity activity = new Game($"Latency: {newLatency}", ActivityType.Listening);
        client.SetActivityAsync(activity);

        return Task.CompletedTask;
    }

    private static Task Log(LogMessage msg)
    {
        Logger.DiscordOutput(msg.ToString());
        
        return Task.CompletedTask;
    }
}