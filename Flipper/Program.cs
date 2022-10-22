using Discord;
using Discord.WebSocket;

namespace Flipper;

internal class Program
{
    private const string BOT_TOKEN = "MTAzMzQyODE2MDQ1MDE1NDUzNg.Gkl11H.vivlOYY1-aJcaT4LCRuYy9Y7yg1OB-rIev5_2Q";
    private DiscordSocketClient client;
    
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

        await client.LoginAsync(TokenType.Bot, BOT_TOKEN);

        await client.StartAsync();
        
        // Block this task until the program is closed.
        await Task.Delay(-1);
    }

    private Task ClientOnLatencyUpdated(int oldLatency, int newLatency)
    {
        IActivity activity = new Game($"Latency: {newLatency}", ActivityType.Listening);
        client.SetActivityAsync(activity);
        
        return Task.CompletedTask;
    }

    private static Task Log(LogMessage msg)
    {
        Console.WriteLine(msg.ToString());
        
        return Task.CompletedTask;
    }
}