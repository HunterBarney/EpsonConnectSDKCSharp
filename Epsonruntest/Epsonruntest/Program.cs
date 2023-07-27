namespace Epsonruntest
{
    using EpsonConnectSDK;
    using System.Net.Sockets;

    internal class Program
    {
        public static string host = "api.epsonconnect.com";
        public static string clientId = "XXXXXXXXXXXXXXXXXXXXXXX";
        public static string secret = "XXXXXXXXXXXXXXXXXXXXXXX";
        public static string device = "XXXXXXXXXXX@print.epsonconnect.com";
        public async static Task Main(string[] args)
        {
            ECSDK epsonClient = new ECSDK(host, clientId, secret, device);
            await epsonClient.Authenticate();
            //You can either manually type in settings as strings, or use the classes provided to see the options
            //In A future update the size and type names will be changed(ex. ms_a4 to A4) to provide a more concise look.
            PrintSettings printSettings = new PrintSettings()
            {
                job_name = "Test job",
                print_mode = PrintMode.document,
                print_setting = new PrintSettingOptions()
                {
                    media_size = MediaSize.ms_a4,
                    media_type = MediaType.mt_plainpaper,
                    borderless = false,
                    print_quality = PrintQuality.high,
                    source = PaperSource.auto,
                    color_mode = ColorMode.color,
                    two_sided = TwoSidedPrinting.none,
                    reverse_order = false,
                    copies = 1,
                    collate = true
                }
            };
            string printJob = await epsonClient.CreatePrintJob("./EpsonTest.docx", printSettings);
            await epsonClient.ExecutePrintJob(printJob);

        }
    }
}