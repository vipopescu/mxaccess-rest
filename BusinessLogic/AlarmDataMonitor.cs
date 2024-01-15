using MXAccesRestAPI.Classes;
using MXAccesRestAPI.MXDataHolder;

namespace MXAccesRestAPI.Monitoring
{
    public class AlarmDataMonitor : IDataStoreMonitor, IDisposable
    {



        private readonly IMXDataHolderService _dataHolderService;

        private bool isActive = false;


        public AlarmDataMonitor(IMXDataHolderService dataHolderService) { 

            _dataHolderService = dataHolderService;
            StartMonitoring();
        }

        ~AlarmDataMonitor()
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
                    Console.WriteLine($"NEW      [ {data.TagName} ]");
                    break;
                case DataStoreChangeType.REMOVED:
                    Console.WriteLine($"REMOVED  [ {data.TagName} ]");
                    break;
                case DataStoreChangeType.MODIFIED:
                    Console.WriteLine($"MODIFIED [ {data.TagName} ] VAL -> {data.Value}");
                    break;
            }
        }


    }
}
