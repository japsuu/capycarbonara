#pragma warning disable CS8618

using Newtonsoft.Json;
using ScottPlot;

namespace Flipper;

public static class FlipperV2
{
    private static List<FlippableItem> flippableItems;
    private static bool isInitialized;
    private static long lastUpdateTimestamp;

    public static int FlipsOnCooldown;
    public static int FlipsMissingData;
    public static int FlipsReceivedOldData;
    public static int FlipsReceivedNewData;

    public static async Task<List<Flip>> GetFlips()
    {
        if (!isInitialized) await Initialize();

        List<Flip> results = new();
        
        ApiResponses.Latest latest = await ApiHelper.GetLatestPrices();
        //ApiResponses.Average averageShort = await ApiHelper.GetShortTimeAveragePrices();
        ApiResponses.Average averageLong = await ApiHelper.GetLongTimeAveragePrices();

        FlipsOnCooldown = 0;
        FlipsMissingData = 0;
        FlipsReceivedOldData = 0;
        FlipsReceivedNewData = 0;
        
        Logger.Write(Logger.LogType.INFO, $"Updating data of {flippableItems.Count} items...");

        foreach (FlippableItem item in flippableItems)
        {
            if(!latest.TryGetItem(item.Data.Id, out ItemLatestPrices? latestItem)) continue;
            //if(!averageShort.TryGetItem(item.Data.Id, out ItemAveragePrices? averageShortItem)) continue;
            if(!averageLong.TryGetItem(item.Data.Id, out ItemAveragePrices? averageLongItem)) continue;

            item.UpdateData(latestItem, averageLongItem);
        }

        Logger.Write(Logger.LogType.INFO, $"Done! {FlipsReceivedNewData} updated. {FlipsReceivedOldData} not updated. {FlipsMissingData} missing data.");
        Logger.Write(Logger.LogType.INFO, $"Calculating flips for {flippableItems.Count - FlipsMissingData} items...");

        foreach (FlippableItem item in flippableItems)
        {
            Flip? flip = await item.GetFlip();
            if(flip == null) continue;
            
            Logger.Write(Logger.LogType.INFO, $"Flip -> {flip.Data.Name}, PC%: {flip.PriceChangePercentage}, Avg:{flip.AverageLow} -> Cur:{flip.LatestLow}, Pot$: {flip.MaxProfit}");
            
            results.Add(flip);
        }

        Logger.Write(Logger.LogType.INFO, "Done!");

        return results;
    }

    public static async Task<bool> TestSynchronizedWithApi()
    {
        if (!isInitialized) await Initialize();
        ApiResponses.Latest latest = await ApiHelper.GetLatestPrices();

        foreach (FlippableItem item in flippableItems)
        {
            if(!latest.TryGetItem(item.Data.Id, out ItemLatestPrices? latestItem)) continue;
            
            if(latestItem.LatestTimestamp > lastUpdateTimestamp)
            {
                return true;
            }
        }

        return false;
    }

    private static async Task Initialize()
    {
        Logger.Write(Logger.LogType.INFO, "Initializing...");
        if (!await RunTests()) return;
        
        flippableItems = new List<FlippableItem>();
        
        #region LOGIC
        
        
        ApiResponses.Latest latest = await ApiHelper.GetLatestPrices();
        ApiResponses.Mapping mapping = await ApiHelper.GetItemMapping();
        ApiResponses.Average longTimeAveragePrices = await ApiHelper.GetLongTimeAveragePrices();

        // Get all valid items for flipping
        foreach (ItemData data in mapping.Data)
        {
            if(!latest.TryGetItem(data.Id, out ItemLatestPrices? latestItem)) continue;
            if(!longTimeAveragePrices.TryGetItem(data.Id, out ItemAveragePrices avgLong)) continue;
            
            // Remove price under 200
            if (avgLong.AvgLowPrice < 200) continue;
            
            // Remove volume under 200
            if (avgLong.TotalVolume < 200) continue;
            
            if(avgLong.LowPriceVolume < 500 && data.Limit < 10000) continue;
            
            // Remove volume under 10,000 when price under 20,000. Removes infrequently traded items with large enough price..
            if (avgLong.TotalVolume < 10000 && avgLong.AvgLowPrice < 20000) continue;
            
            if(latestItem.LatestTimestamp > lastUpdateTimestamp)
            {
                lastUpdateTimestamp = latestItem.LatestTimestamp;
            }
            
            FlippableItem item = new(data);
            flippableItems.Add(item);
        }

        
        #endregion
        
        isInitialized = true;
        Logger.Write(Logger.LogType.INFO, "Initialization done!");
    }
    
    private static async Task<bool> RunTests()
    {
        Tester.Reset();

        ApiResponses.Mapping mapping = await ApiHelper.GetItemMapping();
        ApiResponses.Latest latest = await ApiHelper.GetLatestPrices();
        ApiResponses.Average averageShort = await ApiHelper.GetShortTimeAveragePrices();
        ApiResponses.Average averageLong = await ApiHelper.GetLongTimeAveragePrices();
        ApiResponses.Timeseries timeseries = await ApiHelper.GetTimeseries(2);

        Tester.Test(mapping.Data.Count > 1, "Get item mapping");
        Tester.Test(latest.Data.Count > 1, "Get latest prices");
        Tester.Test(averageShort.Data.Count > 1, "Get short time average prices");
        Tester.Test(averageLong.Data.Count > 1, "Get long time average prices");
        Tester.Test(timeseries.Data.Count > 1, "Get timeseries");

        bool ok = Tester.AllTestsOk;
        if(!ok) Logger.Write(Logger.LogType.ERROR, "Runtime tests were not successful. Program execution will not continue.");
        return ok;
    }
}

public class FlippableItem
{
    public ItemData Data;

    private ItemLatestPrices latestPrices;
    private ItemAveragePrices longAveragePrices;
    //private int updateCooldownCount;
    private long latestDataTimestamp;
    private readonly Queue<ItemLatestPrices> averagePricesQueue;
    
    private bool HasEnoughData => averagePricesQueue.Count == Constants.AVERAGE_ITEM_PRICES_MAX_COUNT;

    public FlippableItem(ItemData data)
    {
        Data = data;
        averagePricesQueue = new Queue<ItemLatestPrices>(Constants.AVERAGE_ITEM_PRICES_MAX_COUNT);
    }

    public void UpdateData(ItemLatestPrices latest, ItemAveragePrices averageLong)
    {
        // Check if data is fresh
        if (latest.LatestTimestamp > latestDataTimestamp)
        {
            if (averagePricesQueue.Count == Constants.AVERAGE_ITEM_PRICES_MAX_COUNT)
                averagePricesQueue.Dequeue();

            // Add the new last element.
            averagePricesQueue.Enqueue(latestPrices);

            latestPrices = latest;
            longAveragePrices = averageLong;
            latestDataTimestamp = latest.LatestTimestamp;
            FlipperV2.FlipsReceivedNewData++;
        }
        else
        {
            FlipperV2.FlipsReceivedOldData++;
        }
        
        if (!HasEnoughData) FlipperV2.FlipsMissingData++;
    }

    public async Task<Flip?> GetFlip()
    {
        // Return if on cooldown
        // if (updateCooldownCount > 0)
        // {
        //     FlipperV2.FlipsOnCooldown++;
        //     updateCooldownCount--;
        //     return null;
        // }
        
        // Return if we are missing data
        if (!HasEnoughData) return null;

        long latestLow = latestPrices.Low;
        long averageLow = CalculateAverageLow();
        
        double priceChangePercentage = MathHelpers.CalculateChangePercentage(averageLow, latestLow);
        double maxProfit = (latestPrices.High - latestPrices.Low) * Data.GetMaxBuyLimit(longAveragePrices.LowPriceVolume);
        
        Flip.FlipType type = priceChangePercentage > 0 ? Flip.FlipType.Spike : Flip.FlipType.Drop;
        
        bool isAnomaly = Math.Abs(priceChangePercentage) > Constants.ANOMALY_PRICE_PERCENT_CHANGE_MIN;
        
        // Only get price DROPs for now.
        if(!isAnomaly || type == Flip.FlipType.Spike) return null;

        // updateCooldownCount += 3;

        Logger.Write(Logger.LogType.INFO, $"Get timeseries for itemID {Data.Id}");
        ApiResponses.Timeseries timeseries = await ApiHelper.GetTimeseries(Data.Id);
        Flip flip = new(Data, type, maxProfit, priceChangePercentage, timeseries, averageLow, latestLow);
        return flip;
    }
    
    private long CalculateAverageLow()
    {
        long combinedLows = averagePricesQueue.Sum(priceObject => priceObject.Low);

        return combinedLows / averagePricesQueue.Count;
    }
}
    
public class Flip
{
    public enum FlipType
    {
        Drop,
        Spike
    }
    
    public readonly ItemData Data;
    public readonly FlipType Type;
    public readonly double MaxProfit;
    public readonly double PriceChangePercentage;
    //public readonly double VolumeChangePercentage;
    //public readonly bool IsVolumeDump;
    public readonly long AverageLow;
    public readonly long LatestLow;
    
    private readonly double[] graphDataX;
    private readonly double[] graphDataY;

    public Flip(ItemData data, FlipType type, double maxProfit, double priceChangePercentage/*, double volumeChangePercentage, bool isVolumeDump*/, ApiResponses.Timeseries timeseries, long averageLow, long latestLow)
    {
        Data = data;
        Type = type;
        MaxProfit = maxProfit;
        PriceChangePercentage = priceChangePercentage;
        //VolumeChangePercentage = volumeChangePercentage;
        //IsVolumeDump = isVolumeDump;
        AverageLow = averageLow;
        LatestLow = latestLow;

        List<double> lGraphDataX = new();
        List<double> lGraphDataY = new();
        int counter = 1;
        foreach (ItemPriceTimeseriesElement element in timeseries.Data)
        {
            lGraphDataY.Add(element.AvgLowPrice);
            lGraphDataX.Add(counter);
            counter++;
        }

        graphDataX = lGraphDataX.ToArray();
        graphDataY = lGraphDataY.ToArray();
    }

    public string GenerateGraph()
    {
        Plot plt = new(400, 200);
        plt.AddScatter(graphDataX, graphDataY);
        plt.Title("Price graph");
        return plt.SaveFig($"graph_{Data.Id}.png");
    }
}

public static class ApiHelper
{
    private static HttpClient? httpClient;
    private static string GetTimeseriesAddress(long itemId) => Constants.RUNELITE_API_TIMESERIES_ENDPOINT + itemId;
    
    public static async Task<ApiResponses.Mapping> GetItemMapping()
    {
        return await FetchFromApi<ApiResponses.Mapping>(Constants.RUNELITE_API_MAPPING_ENDPOINT, "{\"data\":", "}");
    }

    public static async Task<ApiResponses.Latest> GetLatestPrices()
    {
        return await FetchFromApi<ApiResponses.Latest>(Constants.RUNELITE_API_LATEST_ENDPOINT);
    }

    public static async Task<ApiResponses.Average> GetShortTimeAveragePrices()
    {
        return await FetchFromApi<ApiResponses.Average>(Constants.RUNELITE_API_SHORT_TIME_AVERAGE_ENDPOINT);
    }

    public static async Task<ApiResponses.Average> GetLongTimeAveragePrices()
    {
        return await FetchFromApi<ApiResponses.Average>(Constants.RUNELITE_API_LONG_TIME_AVERAGE_ENDPOINT);
    }
    
    public static async Task<ApiResponses.Timeseries> GetTimeseries(long itemId)
    {
        return await FetchFromApi<ApiResponses.Timeseries>(GetTimeseriesAddress(itemId));
    }
    
    private static async Task<T> FetchFromApi<T>(string apiAddress, string jsonPrefix = "", string jsonPostfix = "")
    {
        EnsureHttpClient();
        HttpResponseMessage response = await httpClient?.GetAsync(apiAddress)!;
        response.EnsureSuccessStatusCode();
        
        string jsonString = await response.Content.ReadAsStringAsync();
        
        if (string.IsNullOrEmpty(jsonString))
        {
            Logger.Write(Logger.LogType.ERROR, $"Queried data from API ({apiAddress}) was null!");
        }

        jsonString = jsonPrefix + jsonString + jsonPostfix;

        return JsonConvert.DeserializeObject<T>(jsonString) ?? throw new InvalidOperationException();
    }
    
    private static void EnsureHttpClient()
    {
        if(httpClient != null) return;
        
        httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", "Japsu#8887");
    }
}

public static class ApiResponses
{
    #region PARENT CLASSES

    /// <summary>
    /// https://prices.runescape.wiki/api/v1/osrs/mapping.
    /// </summary>
    public class Mapping
    {
        [JsonProperty("data")]
        public List<ItemData> Data;
    }
    
    /// <summary>
    /// https://prices.runescape.wiki/api/v1/osrs/latest.
    /// </summary>
    public class Latest
    {
        [JsonProperty("data")]
        public Dictionary<string, ItemLatestPrices> Data;

#pragma warning disable CS8601
        public bool TryGetItem(long id, out ItemLatestPrices item) => Data.TryGetValue(id.ToString(), out item) && !item.HasInvalidFields;
    }
    
    /// <summary>
    /// https://prices.runescape.wiki/api/v1/osrs/24h.
    /// </summary>
    public class Average
    {
        [JsonProperty("data")]
        public Dictionary<string, ItemAveragePrices?> Data;

        [JsonProperty("timestamp")]
        public long Timestamp;

        public bool TryGetItem(long id, out ItemAveragePrices item) => Data.TryGetValue(id.ToString(), out item) && item != null && !item.HasInvalidFields;
#pragma warning restore CS8601
    }
    
    /// <summary>
    /// https://prices.runescape.wiki/api/v1/osrs/timeseries .
    /// </summary>
    public class Timeseries
    {
        [JsonProperty("data")]
        public List<ItemPriceTimeseriesElement> Data;
    
        [JsonProperty("itemId")]
        public long ItemId;
    }

    #endregion
}

public class ItemData
{
    [JsonProperty("examine")]
    public string Examine;

    private long id;
    [JsonProperty("id")]
    public long Id
    {
        get => id;
        set
        {
            id = value;
            IconAddress = Constants.RUNELITE_API_ICON_ENDPOINT + value;
            WikiAddress = Constants.RUNELITE_API_WIKI_ENDPOINT + value;
            InfoAddress = Constants.RUNELITE_API_INFO_ENDPOINT + value;
            TimeseriesAddress = Constants.RUNELITE_API_TIMESERIES_ENDPOINT + value;
        }
    }

    [JsonProperty("members")]
    public bool Members;

    [JsonProperty("lowalch", NullValueHandling = NullValueHandling.Ignore)]
    public long Lowalch;

    [JsonProperty("limit", NullValueHandling = NullValueHandling.Ignore)]
    public long Limit = 10000000;

    [JsonProperty("value")]
    public long Value;

    [JsonProperty("highalch", NullValueHandling = NullValueHandling.Ignore)]
    public long Highalch;

    [JsonProperty("icon")]
    public string Icon;

    [JsonProperty("name")]
    public string Name;

    public string IconAddress;
    public string WikiAddress;
    public string InfoAddress;
    public string TimeseriesAddress;

    public long GetMaxBuyLimit(long longTimeAverageSellVolume) => Math.Min(longTimeAverageSellVolume, Limit);
}

public class ItemLatestPrices
{
    [JsonProperty("high", NullValueHandling = NullValueHandling.Ignore)]
    public long High;

    [JsonProperty("highTime", NullValueHandling = NullValueHandling.Ignore)]
    public long HighTime;

    [JsonProperty("low", NullValueHandling = NullValueHandling.Ignore)]
    public long Low;

    [JsonProperty("lowTime", NullValueHandling = NullValueHandling.Ignore)]
    public long LowTime;

    public long LatestTimestamp => Math.Max(HighTime, LowTime);

    public bool HasInvalidFields => High == 0 || HighTime == 0 || Low == 0 || LowTime == 0;
}

public class ItemAveragePrices
{
    [JsonProperty("avgHighPrice", NullValueHandling = NullValueHandling.Ignore)]
    public long AvgHighPrice;

    [JsonProperty("highPriceVolume")]
    public long HighPriceVolume;

    [JsonProperty("avgLowPrice", NullValueHandling = NullValueHandling.Ignore)]
    public long AvgLowPrice;

    [JsonProperty("lowPriceVolume")]
    public long LowPriceVolume;

    public long TotalVolume => LowPriceVolume + HighPriceVolume;

    public bool HasInvalidFields => AvgHighPrice == 0 || AvgLowPrice == 0;
}

public class ItemPriceTimeseriesElement
{
    [JsonProperty("timestamp")]
    public long Timestamp;

    [JsonProperty("avgHighPrice", NullValueHandling = NullValueHandling.Ignore)]
    public long AvgHighPrice;

    [JsonProperty("avgLowPrice", NullValueHandling = NullValueHandling.Ignore)]
    public long AvgLowPrice;

    [JsonProperty("highPriceVolume")]
    public long HighPriceVolume;

    [JsonProperty("lowPriceVolume")]
    public long LowPriceVolume;
}