using MXAccesRestAPI.Classes;
using MXAccesRestAPI.MXDataHolder;

namespace MXAccesRestAPI.Monitoring
{
    public class AlarmMonitor
    {
        private readonly MXDataHolderService _dataHolderService;

        public AlarmMonitor(MXDataHolderService dataHolderService)
        {
            _dataHolderService = dataHolderService;

            // Subscribing to the OnDataStoreChanged event
            _dataHolderService.OnDataStoreChanged += DataHolderService_OnDataStoreChanged;
        }

        private void DataHolderService_OnDataStoreChanged(int key, MXAttribute data, DataStoreChangeType changeType)
        {
            // Reacting to the data store change event
            Console.WriteLine($"Data Store Change Detected: Key={key}, ChangeType={changeType}");

            // Additional logic based on the type of change
            switch (changeType)
            {
                case DataStoreChangeType.ADDED:
                    Console.WriteLine($"New item added to the data store [{data.TagName}]");
                    break;
                case DataStoreChangeType.REMOVED:
                    Console.WriteLine($"Item removed from the data store [{data.TagName}]");
                    break;
                case DataStoreChangeType.MODIFIED:
                    Console.WriteLine($"Item updated in the data store [{data.TagName}]");
                    break;
            }
        }
    }
}
