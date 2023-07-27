using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EpsonConnectSDK
{
    public class PrintSettings
    {
        public string job_name { get; set; }
        public string print_mode { get; set; }
        public PrintSettingOptions print_setting { get; set; }
    }

    public class PrintSettingOptions
    {
        public string media_size { get; set; }
        public string media_type { get; set; }
        public bool borderless { get; set; }
        public string print_quality { get; set; }
        public string source { get; set; }
        public string color_mode { get; set; }
        [JsonProperty("2_sided")]
        public string two_sided { get; set; }
        public bool reverse_order { get; set; }
        public int copies { get; set; }
        public bool collate { get; set; }
    }

    public static class MediaSize
    {
        public const string ms_a3 = "ms_a3";
        public const string ms_a4 = "ms_a4";
        public const string ms_a5 = "ms_a5";
        public const string ms_a6 = "ms_a6";
        public const string ms_b6 = "ms_b6";
        public const string ms_tabloid = "ms_tabloid";
        public const string ms_letter = "ms_letter";
        public const string ms_legal = "ms_legal";
        public const string ms_halfletter = "ms_halfletter";
        public const string ms_kg = "ms_kg";
        public const string ms_1 = "ms_1";
        public const string ms_21 = "ms_21";
        public const string ms_10x12 = "ms_10x12";
        public const string ms_8x10 = "ms_8x10";
        public const string ms_hivision = "ms_hivision";
        public const string ms_5x8 = "ms_5x8";
        public const string ms_postcard = "ms_postcard";
    }

    public static class MediaType
    {
        public const string mt_plainpaper = "mt_plainpaper";
        public const string mt_photopaper = "mt_photopaper";
        public const string mt_hagaki = "mt_hagaki";
        public const string mt_hagakiphoto = "mt_hagakiphoto";
        public const string mt_hagakiinkjet = "mt_hagakiinkjet";
    }

    public static class ColorMode
    {
        public const string mono = "mono";
        public const string color = "color";
    }

    public static class TwoSidedPrinting
    {
        public const string none = "none";
        public const string long_edge = "long_edge";
        public const string short_edge = "short_edge";
    }

    public static class PaperSource
    {
        public const string rear = "rear";
        public const string front1 = "front1";
        public const string front2 = "front2";
        public const string front3 = "front3";
        public const string front4 = "front4";
        public const string auto = "auto";
    }

    public static class PrintQuality
    {
        public const string high = "high";
        public const string normal = "normal";
        public const string draft = "draft";
    }

    public static class PrintMode
    {
        public const string document = "document";
        public const string photo = "photo";
    }
}
