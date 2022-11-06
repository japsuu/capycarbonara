namespace Flipper;

public static class MathHelpers
{
    /// <summary>
    /// If the result is negative, then this is a percentage decrease.
    /// </summary>
    /// <param name="oldVal"></param>
    /// <param name="newVal"></param>
    /// <returns></returns>
    public static double CalculateChangePercentage(long oldVal, long newVal)
    {
        if (oldVal == 0 || newVal == 0) return 0;
        return 100.0 * (newVal - oldVal) / oldVal;
    }
}