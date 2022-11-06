namespace Flipper;

#pragma warning disable CS8618
public class PriceObject
{
    public readonly long High;

    public readonly long HighTime;

    public readonly long Low;

    public readonly long LowTime;

    public long LatestUpdateTime => Math.Max(HighTime, LowTime);

    public PriceObject(long high, long highTime, long low, long lowTime)
    {
        High = high;
        HighTime = highTime;
        Low = low;
        LowTime = lowTime;
    }
}