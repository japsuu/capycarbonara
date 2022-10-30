namespace Flipper;

public static class Logger
{
    private const ConsoleColor DISCORD_COLOR_TEXT = ConsoleColor.Cyan;
    private const ConsoleColor INFO_COLOR_TEXT = ConsoleColor.Blue;
    private const ConsoleColor WARN_COLOR_TEXT = ConsoleColor.Yellow;
    private const ConsoleColor ERROR_COLOR_TEXT = ConsoleColor.Red;
    private const ConsoleColor SUCCESS_COLOR_TEXT = ConsoleColor.Green;
    
    public static void DiscordOutput(string text)
    {
        Console.ForegroundColor = DISCORD_COLOR_TEXT;
        
        Console.WriteLine($"[DISCORD]: {text}");

        Console.ResetColor();
    }
    
    public static void Info(string text)
    {
        Console.ForegroundColor = INFO_COLOR_TEXT;
        
        Console.WriteLine($"[INFO]: {text}");

        Console.ResetColor();
    }
    
    public static void Warn(string text)
    {
        Console.ForegroundColor = WARN_COLOR_TEXT;
        
        Console.WriteLine($"[WARN]: {text}");
        
        Console.ResetColor();
    }
    
    public static void Error(string text)
    {
        Console.ForegroundColor = ERROR_COLOR_TEXT;
        
        Console.WriteLine($"[ERR]: {text}");

        Console.ResetColor();
    }
    
    public static void Success(string text)
    {
        Console.ForegroundColor = SUCCESS_COLOR_TEXT;
        
        Console.WriteLine($"[INFO]: {text}");

        Console.ResetColor();
    }
    
    public static void TestOk(string text)
    {
        Console.ForegroundColor = SUCCESS_COLOR_TEXT;
        
        Console.Write("[OK]: ");

        Console.ForegroundColor = INFO_COLOR_TEXT;
        
        Console.Write(text);
        Console.WriteLine();


        Console.ResetColor();
    }
    
    public static void TestError(string text)
    {
        Console.ForegroundColor = ERROR_COLOR_TEXT;
        
        Console.Write("[ERR]: ");

        Console.ForegroundColor = INFO_COLOR_TEXT;
        
        Console.Write(text);
        Console.WriteLine();

        Console.ResetColor();
    }
}