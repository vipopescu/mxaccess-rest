using MXAccesRestAPI.Classes;
using MXAccesRestAPI.MXDataHolder;

namespace MXAccesRestAPI.Monitoring
{
    public class AlarmMonitor(IMXDataHolderService dataHolderService) : IDataStoreMonitor, IDisposable
    {


        private readonly IMXDataHolderService _dataHolderService = dataHolderService;

        private bool isActive = false;


        ~AlarmMonitor()
        {
            Dispose();
        }

        public void Dispose()
        {
            StopMonitoring();
            GC.SuppressFinalize(this);
        }

        public bool IsMonitoringActive()
        {
            return isActive;
        }

        public void StartMonitoring()
        {
            // Subscribing to the OnDataStoreChanged event
            _dataHolderService.OnDataStoreChanged += DataHolderService_OnDataStoreChanged;
            isActive = true;
        }

        public void StopMonitoring()
        {
            _dataHolderService.OnDataStoreChanged -= DataHolderService_OnDataStoreChanged;
            isActive = false;
        }

        private void DataHolderService_OnDataStoreChanged(int key, MXAttribute data, DataStoreChangeType changeType)
        {

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
