using Newtonsoft.Json;

namespace Flipper;

public static class ConfigManager
{
    public static Config Configuration = new();

    public static void LoadConfig()
    {
        File.Create("config.json").Close();
        string json = File.ReadAllText("config.json");
        Config? conf = JsonConvert.DeserializeObject<Config>(json);
        if (conf != null)
            Configuration = conf;
    }
    
    public static void SaveConfig()
    {
        string json = JsonConvert.SerializeObject(Configuration, Formatting.Indented);
        File.Create("config.json").Close();
        File.WriteAllText("config.json", json);
    }
    
    public class Config
    {
        [JsonProperty("AVERAGE_ITEM_PRICES_MAX_LENGTH")]
        public long AverageItemPricesMaxLength { get; set; }

        [JsonProperty("ANOMALY_DEVIATION_PERCENTAGE_MIN")]
        public long AnomalyDeviationPercentageMin { get; set; }

        [JsonProperty("ANOMALY_COOLDOWN")]
        public long AnomalyCooldown { get; set; }

        [JsonProperty("PRICE_DATA_MAX_LIFETIME_MINUTES")]
        public long PriceDataMaxLifetimeMinutes { get; set; }

        [JsonProperty("ANOMALY_MIN_PROFIT")]
        public long AnomalyMinProfit { get; set; }

        [JsonProperty("IS_FLIPPABLE_MIN_PRICE")]
        public long IsFlippableMinPrice { get; set; }

        [JsonProperty("IS_FLIPPABLE_MIN_VOLUME")]
        public long IsFlippableMinVolume { get; set; }

        public Config()
        {
            AverageItemPricesMaxLength = 10;
            AnomalyDeviationPercentageMin = 15;
            AnomalyCooldown = 5;
            PriceDataMaxLifetimeMinutes = 3;
            AnomalyMinProfit = 400000;
            IsFlippableMinPrice = 200;
            IsFlippableMinVolume = 20;
        }
    }
}