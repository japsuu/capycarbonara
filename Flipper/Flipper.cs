/*using Newtonsoft.Json;
#pragma warning disable CS8618

namespace Flipper;

public static class Flipper
{
    private const string RUNELITE_API_ADDRESS_LATEST = "https://prices.runescape.wiki/api/v1/osrs/latest";
    private const string RUNELITE_API_ADDRESS_1_HOUR = "https://prices.runescape.wiki/api/v1/osrs/1h";
    private const string RUNELITE_API_ADDRESS_MAPPING = "https://prices.runescape.wiki/api/v1/osrs/mapping";

    private static HttpClient httpClient = null!;

    private static List<Item> flippableItems = null!;

    private static bool isInitialized;
    private static bool hasUpdatedData;
    
    private static int updatedItemsCount;

    private static async Task<List<ItemData>?> GetItemMapping()
    {
        string json = await QueryApiToJson(RUNELITE_API_ADDRESS_MAPPING);
        return JsonConvert.DeserializeObject<List<ItemData>>(json);
    }

    private static async Task<PriceMapping?> GetPrices_Latest()
    {
        string json = await QueryApiToJson(RUNELITE_API_ADDRESS_LATEST);
        return JsonConvert.DeserializeObject<PriceMapping>(json);
    }

    private static async Task<PriceMapping1H?> GetPrices_1h()
    {
        string json = await QueryApiToJson(RUNELITE_API_ADDRESS_1_HOUR);
        return JsonConvert.DeserializeObject<PriceMapping1H>(json);
    }

    public static async Task<bool> Test()
    {
        Logger.Info("Running tests...");

        CreateHttpClient();
        
        List<ItemData>? itemMapping = await GetItemMapping();
        PriceMapping? priceMappingLatest = await GetPrices_Latest();
        PriceMapping1H? priceMapping1H = await GetPrices_1h();

        if (itemMapping == null)
        {
            Logger.TestError("Get item mapping.");
            return false;
        }
        Logger.TestOk("Get item mapping.");

        if (priceMappingLatest == null)
        {
            Logger.TestError("Get latest price mapping.");
            return false;
        }
        Logger.TestOk("Get latest price mapping.");

        if (priceMapping1H == null)
        {
            Logger.TestError("Get 1h price mapping.");
            return false;
        }
        Logger.TestOk("Get 1h price mapping.");
        
        Logger.Info("Tests completed!");

        return true;
    }

    private static void CreateHttpClient()
    {
        httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", "Japsu#8887");
    }

    /// <summary>
    /// Refreshes the latest prices.
    /// </summary>
    public static async Task<bool> RefreshData()
    {
        if (!isInitialized)
        {
            Logger.Warn("Tried to Update before Initialization!");
            await Initialize();
        }
        //Logger.Info("Flipper refreshing...");
        
        await QueryLatestPrices();
        
        //Logger.Info("Flipper refresh complete.");
        return hasUpdatedData;
    }
    
    /// <summary>
    /// Calculates flips.
    /// </summary>
    /// <returns>Flips/anomalies.</returns>
    public static async Task<List<Item>> GetFlips()
    {
        if (isInitialized) return FindFlips();
        
        Logger.Warn("Tried to Update before Initialization!");
        await Initialize();

        return FindFlips();
    }

    public static async Task Initialize()
    {
        Logger.Info("Initializing...");
        
        flippableItems = new List<Item>();
        
        CreateHttpClient();

        List<ItemData>? itemMapping = await GetItemMapping();
        PriceMapping? priceMappingLatest = await GetPrices_Latest();
        PriceMapping1H? priceMapping1H = await GetPrices_1h();

        if (itemMapping == null)
        {
            Logger.Error("Could not get the item mapping. Program execution cannot continue.");
            return;
        }

        if (priceMappingLatest == null)
        {
            Logger.Warn("Could not get the latest price mapping.");
            return;
        }

        if (priceMapping1H == null)
        {
            Logger.Warn("Could not get the 1h price mapping.");
            return;
        }

        foreach (ItemData itemData in itemMapping)
        {
            if (!priceMappingLatest.Prices.TryGetValue(itemData.Id.ToString(), out JsonPriceObject? parsedPricesLatest)) continue;
            if (!priceMapping1H.Prices.TryGetValue(itemData.Id.ToString(), out JsonPriceObject1H? parsedPrices1H)) continue;

            PriceObject? pricesLatest = parsedPricesLatest?.ToPriceObject();
            if(pricesLatest == null) continue;
            PriceObject1H? prices1H = parsedPrices1H.ToPriceObject(priceMapping1H.Timestamp);
            if(prices1H == null) continue;
            
            Item item = new(itemData, pricesLatest, prices1H);
            
            //if(!item.IsFlippable()) continue;
            
            flippableItems.Add(item);
        }

        isInitialized = true;
        Logger.Info($"Initialized {flippableItems.Count} flippable items.");
    }

    private static async Task QueryLatestPrices()
    {
        hasUpdatedData = false;
        PriceMapping? priceMappingLatest = await GetPrices_Latest();
        PriceMapping1H? priceMapping1H = await GetPrices_1h();
        
        if (priceMappingLatest == null)
        {
            Logger.Warn("Could not get the latest price mapping.");
            return;
        }
        
        if (priceMapping1H == null)
        {
            Logger.Warn("Could not get the 1h price mapping.");
            return;
        }

        updatedItemsCount = 0;

        foreach (Item item in flippableItems)
        {
            if (!priceMappingLatest.Prices.TryGetValue(item.ID.ToString(), out JsonPriceObject? parsedPricesLatest)) continue;
            if (!priceMapping1H.Prices.TryGetValue(item.ID.ToString(), out JsonPriceObject1H? parsedPrices1H)) continue;

            PriceObject? pricesLatest = parsedPricesLatest?.ToPriceObject();
            if(pricesLatest == null)
            {
                Logger.Warn($"Could not get latest prices for item '{item.Data.Name}'.");
                continue;
            }

            PriceObject1H? prices1H = parsedPrices1H.ToPriceObject(priceMapping1H.Timestamp);
            if(prices1H == null)
            {
                Logger.Warn($"Could not get 1h prices for item '{item.Data.Name}'.");
                continue;
            }
            
            if(item.UpdateLatestPrices(pricesLatest) || item.Update1HPrices(prices1H))
                updatedItemsCount++;
        }

        if (updatedItemsCount > 0)
            hasUpdatedData = true;
    }

    private static List<Item> FindFlips()
    {
        List<Item> results = new();
        int missingDataCount = 0;
        int anomalyCount = 0;
        int onCooldownCount = 0;

        foreach (Item item in flippableItems)
        {
            if(!item.HasEnoughData)
            {
                missingDataCount++;
                continue;
            }

            if (item.OnCooldown)
                onCooldownCount++;
            
            if(!item.IsDataDirty) continue;

            if(!item.GetAnomaly())
                continue;

            Logger.Info($"ANOMALY -> Drop: {item.Data.Name}. %:{item.CalculateDeviationPercentage()} avg:{item.CalculateAverageLow()} lat:{item.LatestLow}");

            results.Add(item);

            anomalyCount++;
        }

        Logger.Info($"Found {anomalyCount} anomalies.\t{onCooldownCount} items on cooldown.\t{updatedItemsCount} items updated.\t{missingDataCount} items insufficient data.");

        return results;
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
}*/