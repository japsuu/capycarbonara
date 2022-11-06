namespace Flipper;

#pragma warning disable CS8618
public class Item_Old
{
    public readonly ItemData_Old Data;
        
    public int ID => Data.Id;
        
    /// <summary>
    /// Determines if this item has had it's data changed since the last time checked.
    /// </summary>
    public bool IsDataDirty { get; private set; }
    
    public bool HasEnoughData => averagePricesQueue.Count == ConfigManager.Configuration.AverageItemPricesMaxLength;

    public long AverageLow => CalculateAverageLow();
    public long LatestLow => priceObjectPrices.Low;

    public long LatestMargin => priceObjectPrices.High - priceObjectPrices.Low;
        
    /// <summary>
    /// MaxBuyAmount * LatestMargin.
    /// </summary>
    public long LatestMaxProfit => MaxBuyAmount * LatestMargin;

    /// <summary>
    /// Maximum amount a single player can buy this item in 4 hours.
    /// </summary>
    public long MaxBuyAmount => Math.Clamp(priceObject1HPrices.TotalVolume * 4, 0, Data.Limit);

    public bool OnCooldown => cooldown > 0;

    private uint cooldown;
    private long DataUpdateTimeLatest { get; set; }
    private long DataUpdateTime1H { get; set; }
    private PriceObject priceObjectPrices;
    private PriceObject1H priceObject1HPrices;
    private readonly Queue<PriceObject> averagePricesQueue;

    public Item_Old(ItemData_Old data, PriceObject newPriceObjectPrices, PriceObject1H priceObject1HPrices)
    {
        averagePricesQueue = new Queue<PriceObject>((int)ConfigManager.Configuration.AverageItemPricesMaxLength);
        Data = data;
            
        UpdateLatestPrices(newPriceObjectPrices);
        Update1HPrices(priceObject1HPrices);
    }

    private bool IsFlippable()
    {
        // Rule out all items worth under 200gp.
        if (priceObject1HPrices.AvgLowPrice < ConfigManager.Configuration.IsFlippableMinPrice) return false;

        // Rule out all items traded under 20x in 4h.
        if (priceObject1HPrices.TotalVolume < ConfigManager.Configuration.IsFlippableMinVolume) return false;
            
        long currentTimestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
        if (currentTimestamp - DataUpdateTime1H > ConfigManager.Configuration.PriceDataMaxLifetimeMinutes * 60)
            return false;
            
        if (currentTimestamp - DataUpdateTimeLatest > ConfigManager.Configuration.PriceDataMaxLifetimeMinutes * 60)
            return false;
            
        // Rule out all items worth under 500gp & 10,000 buy amount.
        /////////////////////////if (priceObject1HPrices.AvgLowPrice < 500 && MaxBuyAmount < 10000) return false;
        
        return true;
    }

    public bool Update1HPrices(PriceObject1H prices)
    {
        if (prices.Timestamp <= DataUpdateTime1H) return false;
            
        priceObject1HPrices = prices;

        DataUpdateTime1H = prices.Timestamp;
        return true;
    }

    public bool UpdateLatestPrices(PriceObject newPriceObjectPrices)
    {
        if (cooldown > 0)
            cooldown--;
            
        if (newPriceObjectPrices.LatestUpdateTime <= DataUpdateTimeLatest)
        {
            return false;
        }

        if(averagePricesQueue.Count == ConfigManager.Configuration.AverageItemPricesMaxLength)
            averagePricesQueue.Dequeue();
            
        averagePricesQueue.Enqueue(priceObjectPrices);

        priceObjectPrices = newPriceObjectPrices;

        DataUpdateTimeLatest = newPriceObjectPrices.LatestUpdateTime;
        IsDataDirty = true;
            
        return true;
    }

    public bool GetAnomaly()
    {
        if (cooldown > 0) return false;

        if (!IsFlippable()) return false;
            
        double deviationPercentage = CalculateDeviationPercentage();

        bool deviatedEnough = deviationPercentage > ConfigManager.Configuration.AnomalyDeviationPercentageMin;
            
        bool profitableEnough = LatestMaxProfit > ConfigManager.Configuration.AnomalyMinProfit;

        bool isAnomaly = deviatedEnough && profitableEnough;
            
        //TODO: Check if data is fresh enough

        if (isAnomaly)
        {
            cooldown = (uint)ConfigManager.Configuration.AnomalyCooldown;

            IsDataDirty = false;
        }

        return isAnomaly;
    }

    public double CalculateDeviationPercentage()
    {
        double latestLow = priceObjectPrices.Low;
        double averageLow = CalculateAverageLow();

        return (averageLow - latestLow) / averageLow * 100.0;
    }

    private long CalculateAverageLow()
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        long combinedLows = averagePricesQueue.Where(priceObject => priceObject != null).Sum(priceObject => priceObject.Low);

        return combinedLows / averagePricesQueue.Count;
    }
}