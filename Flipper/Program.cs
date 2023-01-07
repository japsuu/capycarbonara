
using Discord;
using Discord.WebSocket;

namespace Flipper;

internal class Program
{
    /// <summary>
    /// Larger value = less de-sync with API update,
    /// Smaller value = less delay in fetching data.
    /// </summary>
    private const int API_UPDATE_SYNCHRONIZATION_INTERVAL = 4;
    private const string BOT_TOKEN = "TOKEN HERE";
    private DiscordSocketClient client = null!;
    
    private static bool syncedWithApiUpdateCycle;
    private static bool firstStart = true;
    
    public static Task Main(string[] args) => new Program().MainAsync();

    private async Task MainAsync()
    {
        ConfigManager.LoadConfig();
        
        DiscordSocketConfig config = new()
        {
            ConnectionTimeout = 600000
        };
        
        client = new DiscordSocketClient(config);
        client.Log += Log;
        client.LatencyUpdated += ClientOnLatencyUpdated;
        client.Connected += ClientOnConnected;

        await client.LoginAsync(TokenType.Bot, BOT_TOKEN);

        await client.StartAsync();

        // Block this task until the program is closed.
        await Task.Delay(-1);
    }

    private async Task ClientOnConnected()
    {
        PeriodicTimer timer = new(TimeSpan.FromSeconds(API_UPDATE_SYNCHRONIZATION_INTERVAL));

        while (await timer.WaitForNextTickAsync())  //TODO: WARN: Do not await some calls, otherwise we get out of sync eventually
        {
            // When we are synced with the api, we can safely set the query interval to 60s for minimum delay.

            if (syncedWithApiUpdateCycle)
            {
                HandleFlips();
            }
            else
            {
                bool isSynchronized = await FlipperV2.TestSynchronizedWithApi();

                if (isSynchronized)
                {
                    timer = new PeriodicTimer(TimeSpan.FromSeconds(60));

                    syncedWithApiUpdateCycle = true;
                
                    Console.WriteLine();
                    Logger.Write(Logger.LogType.SUCCESS, "Successfully synced with API update cycle!");
                }
                else
                {
                    timer = new PeriodicTimer(TimeSpan.FromSeconds(API_UPDATE_SYNCHRONIZATION_INTERVAL));
                
                    if(firstStart)
                        Logger.Write(Logger.LogType.WARN, "Not in sync with the API update cycle! Fixing...");
                    else
                        Logger.Wait();
                }
            }

            firstStart = false;
        }
    }

    private async Task HandleFlips()
    {
        List<Flip> flips = await FlipperV2.GetFlips();

        foreach (Flip flip in flips)
        {
            EmbedBuilder builder = new()
            {
                Title = "Anomaly!"
            };
            builder.AddField($"{flip.Data.Name} ({flip.Data.Id})", flip.Type);
            builder.AddField("Price Change %:", flip.PriceChangePercentage);
            builder.AddField("Price info:", $"Avg:{flip.AverageLow} -> cur:{flip.LatestLow}");
            builder.AddField("Potential profit:", $"{flip.MaxProfit}");
            builder.WithFooter(footer => footer.Text = "Capycarbonara");
            builder.WithThumbnailUrl(flip.Data.IconAddress);
            builder.WithDescription($"[Wiki]({flip.Data.WikiAddress}) | [prices.runescape.wiki]({flip.Data.InfoAddress})");
            builder.WithCurrentTimestamp();
                
            await client.GetGuild(894462128977743893).GetTextChannel(1036649273934229534).SendMessageAsync(embed: builder.Build());
        }
    }

    private Task ClientOnLatencyUpdated(int oldLatency, int newLatency)
    {
        IActivity activity = new Game($"Latency: {newLatency}", ActivityType.Listening);
        client.SetActivityAsync(activity);

        return Task.CompletedTask;
    }

    private static Task Log(LogMessage msg)
    {
        Logger.Write(Logger.LogType.DISCORD, msg.ToString());
        
        return Task.CompletedTask;
    }
}
