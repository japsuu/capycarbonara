namespace Flipper;

#pragma warning disable CS8618
public static class Constants
{
    public const string RUNELITE_API_MAPPING_ENDPOINT = "https://prices.runescape.wiki/api/v1/osrs/mapping";
    public const string RUNELITE_API_LATEST_ENDPOINT = "https://prices.runescape.wiki/api/v1/osrs/latest";
    // Price to compare the current/latest value against.
    public const string RUNELITE_API_SHORT_TIME_AVERAGE_ENDPOINT = "https://prices.runescape.wiki/api/v1/osrs/1h";
    public const string RUNELITE_API_LONG_TIME_AVERAGE_ENDPOINT = "https://prices.runescape.wiki/api/v1/osrs/24h";
    public const int ANOMALY_VOLUME_PERCENT_CHANGE_MIN = 200;   //Rise if getting false positives
    public const int ANOMALY_PRICE_PERCENT_CHANGE_MIN = 5;     //Rise if getting false positives
    public const int AVERAGE_ITEM_PRICES_MAX_COUNT = 3;         //Rise if getting false positives. Basically equals algorithm sensitivity
    // Only used for graphing, once a valid flip is found.
    public const string RUNELITE_API_TIMESERIES_ENDPOINT = "https://prices.runescape.wiki/api/v1/osrs/timeseries?timestep=5m&id=";
    public const string RUNELITE_API_ICON_ENDPOINT = "https://secure.runescape.com/m=itemdb_oldschool/obj_big.gif?id=";
    public const string RUNELITE_API_WIKI_ENDPOINT = "https://oldschool.runescape.wiki/w/Special:Lookup?type=item&amp;id=";
    public const string RUNELITE_API_INFO_ENDPOINT = "https://prices.runescape.wiki/osrs/item/";
}