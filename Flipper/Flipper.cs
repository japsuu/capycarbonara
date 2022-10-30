using Newtonsoft.Json;
#pragma warning disable CS8618

namespace Flipper;

public static class Flipper
{
    private const string RUNELITE_API_ADDRESS_LATEST = "https://prices.runescape.wiki/api/v1/osrs/latest";
    private const string RUNELITE_API_ADDRESS_MAPPING = "https://prices.runescape.wiki/api/v1/osrs/mapping";
    private const int AVERAGE_ITEM_PRICES_MAX_LENGTH = 4;
    private const int ANOMALY_DEVIATION_PERCENTAGE_MIN = 20;

    private static HttpClient httpClient = null!;

    private static List<Item> flippableItems = null!;

    private static bool isInitialized;
    private static bool hasUpdatedData;

    public static async Task<bool> Test()
    {
        Logger.Info("Running tests...");

        CreateHttpClient();
        
        string tMappingJson = await QueryApiToJson(RUNELITE_API_ADDRESS_MAPPING);
        List<ItemData>? tItemMapping = JsonConvert.DeserializeObject<List<ItemData>>(tMappingJson);
        
        string tLatestPricesJson = await QueryApiToJson(RUNELITE_API_ADDRESS_LATEST);
        PriceMapping? tPriceMapping = JsonConvert.DeserializeObject<PriceMapping>(tLatestPricesJson);

        if (tItemMapping == null)
        {
            Logger.TestError("Get item mapping.");
            return false;
        }

        if (tPriceMapping == null)
        {
            Logger.TestError("Get price mapping.");
            return false;
        }
        
        Logger.TestOk("Get item mapping.");
        Logger.TestOk("Get price mapping.");
        
        Logger.Info("Tests completed!");

        return true;
    }

    private static void CreateHttpClient()
    {
        httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", "Japsu#8887");
    }

    /// <summary>
    /// Refreshes the latest prices, and finds potential flips.
    /// </summary>
    public static async Task<bool> Update()
    {
        if (!isInitialized)
        {
            Logger.Warn("Tried to Update before Initialization!");
            return false;
        }
        Logger.Info("Flipper refreshing...");
        
        await QueryLatestPrices();
        FindFlips();
        
        Logger.Info("Flipper refresh complete.");
        return hasUpdatedData;
    }

    public static async Task Initialize()
    {
        Logger.Info("Initializing...");
        
        flippableItems = new List<Item>();
        
        CreateHttpClient();

        string mappingJson = await QueryApiToJson(RUNELITE_API_ADDRESS_MAPPING);
        List<ItemData>? itemMapping = JsonConvert.DeserializeObject<List<ItemData>>(mappingJson);
        
        string latestPricesJson = await QueryApiToJson(RUNELITE_API_ADDRESS_LATEST);
        PriceMapping? priceMapping = JsonConvert.DeserializeObject<PriceMapping>(latestPricesJson);

        if (itemMapping == null)
        {
            Logger.Error("Could not get the item mapping. Program execution cannot continue.");
            return;
        }

        if (priceMapping == null)
        {
            Logger.Warn("Could not get the latest price mapping.");
            return;
        }

        foreach (ItemData itemData in itemMapping)
        {
            if (!priceMapping.Prices.TryGetValue(itemData.Id.ToString(), out JsonPriceObject? parsedPrices)) continue;

            PriceObject? prices = parsedPrices?.ToPriceObject();
            if(prices == null) continue;
            
            Item item = new(itemData, prices);
            
            if(!item.IsFlippable()) continue;
            
            flippableItems.Add(item);
        }

        isInitialized = true;
        Logger.Info($"Initialized {flippableItems.Count} flippable items.");
    }

    private static async Task QueryLatestPrices()
    {
        hasUpdatedData = false;
        string latestPricesJson = await QueryApiToJson(RUNELITE_API_ADDRESS_LATEST);
        PriceMapping? priceMapping = JsonConvert.DeserializeObject<PriceMapping>(latestPricesJson);
        
        if (priceMapping == null)
        {
            Logger.Warn("Could not get the latest price mapping.");
            return;
        }

        int updatedItemsCount = 0;

        foreach (Item item in flippableItems)
        {
            if (!priceMapping.Prices.TryGetValue(item.ID.ToString(), out JsonPriceObject? parsedPrices)) continue;
            
            //TODO: Confirm that the data isn't too old!

            PriceObject? prices = parsedPrices?.ToPriceObject();
            if(prices == null)
            {
                Logger.Warn($"Could not get latest prices for item '{item.Data.Name}'.");
                continue;
            }
            
            if(item.UpdatePrices(prices))
                updatedItemsCount++;
        }

        if (updatedItemsCount > 0)
            hasUpdatedData = true;

        Logger.Info($"Updated {updatedItemsCount} items' data.");
    }

    private static void FindFlips()
    {
        int missingDataCount = 0;
        int anomalyCount = 0;

        foreach (Item item in flippableItems)
        {
            if(!item.HasEnoughData)
            {
                missingDataCount++;
                continue;
            }
            
            if(!item.IsDataDirty) continue;

            if(!item.GetAnomaly())
                continue;

            Logger.Info($"ANOMALY -> Drop: {item.Data.Name}. %:{item.CalculateDeviationPercentage()} avg:{item.CalculateAverageLow()} lat:{item.LatestLow}");

            item.IsDataDirty = false;

            anomalyCount++;
        }

        Logger.Info($"Found {anomalyCount} anomalies.");
        Logger.Info($"{missingDataCount} items do not have enough data to determine anomalies.");
    }

    private static async Task<string> QueryApiToJson(string address)
    {
        HttpResponseMessage response = await httpClient.GetAsync(address);
        response.EnsureSuccessStatusCode();
        
        string jsonString = await response.Content.ReadAsStringAsync();
        
        if (string.IsNullOrEmpty(jsonString))
        {
            Logger.Error($"Queried data from API ({address}) was null!");
        }

        return jsonString;
    }
    
    public class Item
    {
        public readonly ItemData Data;
        
        /// <summary>
        /// Determines if this item has had it's data changed since the last time checked.
        /// </summary>
        public bool IsDataDirty { get; set; }
        
        public int ID => Data.Id;
        public bool HasEnoughData => averagePricesQueue.Count == AVERAGE_ITEM_PRICES_MAX_LENGTH;

        public long LatestLow => latestPrices.Low;

        private long LatestDataUpdateTime { get; set; }
        private PriceObject latestPrices;
        private readonly Queue<PriceObject> averagePricesQueue;

        public Item(ItemData data, PriceObject latestPrices)
        {
            averagePricesQueue = new Queue<PriceObject>(AVERAGE_ITEM_PRICES_MAX_LENGTH);
            Data = data;
            
            UpdatePrices(latestPrices);
        }

        public bool IsFlippable()
        {
            if (latestPrices.Low < 500) return false;
            if (latestPrices.High < 500) return false;
            
            //TODO: Add other checks.
        
            return true;
        }

        public bool UpdatePrices(PriceObject newLatestPrices)
        {
            if (newLatestPrices.LatestUpdateTime <= LatestDataUpdateTime)
            {
                return false;
            }

            if(averagePricesQueue.Count == AVERAGE_ITEM_PRICES_MAX_LENGTH)
                averagePricesQueue.Dequeue();
            
            averagePricesQueue.Enqueue(latestPrices);

            latestPrices = newLatestPrices;

            LatestDataUpdateTime = newLatestPrices.LatestUpdateTime;
            IsDataDirty = true;
            
            return true;
        }

        public bool GetAnomaly()
        {
            double deviationPercentage = CalculateDeviationPercentage();

            return deviationPercentage > ANOMALY_DEVIATION_PERCENTAGE_MIN;
        }

        public double CalculateDeviationPercentage()
        {
            double latestLow = latestPrices.Low;
            double averageLow = CalculateAverageLow();

            return (averageLow - latestLow) / averageLow * 100.0;
        }

        public long CalculateAverageLow()
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            long combinedLows = averagePricesQueue.Where(priceObject => priceObject != null).Sum(priceObject => priceObject.Low);

            return combinedLows / averagePricesQueue.Count;
        }
    }
    
    public class PriceMapping
    {
        [JsonProperty("data")]
        public Dictionary<string, JsonPriceObject?> Prices { get; set; }
    }
    
    public class JsonPriceObject
    {
        [JsonProperty("high")]
        public long? High { get; set; }

        [JsonProperty("highTime")]
        public long? HighTime { get; set; }

        [JsonProperty("low")]
        public long? Low { get; set; }

        [JsonProperty("lowTime")]
        public long? LowTime { get; set; }

        public PriceObject? ToPriceObject()
        {
            if (High == null || HighTime == null || Low == null || LowTime == null)
                return null;

            PriceObject prices = new(
                (long)High,
                (long)HighTime,
                (long)Low,
                (long)LowTime);

            return prices;
        }
    }

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
    
    public class ItemData
    {
        [JsonProperty("examine")]
        public string Examine { get; set; }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("members")]
        public bool Members { get; set; }

        [JsonProperty("lowalch")]
        public int Lowalch { get; set; }

        [JsonProperty("limit")]
        public int Limit { get; set; }

        [JsonProperty("value")]
        public int Value { get; set; }

        [JsonProperty("highalch")]
        public int Highalch { get; set; }

        [JsonProperty("icon")]
        public string Icon { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }
}