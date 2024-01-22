namespace MXAccesRestAPI.Settings
{
    public class MxDataDataServiceSettings
    {
        public string ServerName { get; init; }
        public string LmxVerifyUser { get; init; }

        public int MxDataServiceThreads { get; init; } = 2;

    }
}