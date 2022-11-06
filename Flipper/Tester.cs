namespace Flipper;

public static class Tester
{
    private static bool testsOk;
    public static bool AllTestsOk
    {
        get
        {
            Logger.Write(Logger.LogType.INFO, $"Runtime tests completed with result {testsOk}!");

            return testsOk;
        }
        private set => testsOk = value;
    }

    public static void Reset()
    {
        Logger.Write(Logger.LogType.INFO, "Running tests...");
        AllTestsOk = true;
    }
    
    public static bool Test(bool isTestOk, string testName)
    {
        if (isTestOk)
        {
            Logger.TestOk(testName);
        }
        else
        {
            Logger.TestError(testName);
            AllTestsOk = false;
        }

        return isTestOk;
    }
}