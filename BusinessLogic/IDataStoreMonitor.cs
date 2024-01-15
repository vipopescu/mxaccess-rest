
namespace MXAccesRestAPI.Monitoring
{
    public interface IDataStoreMonitor
    {
        // Start the monitoring service
        void StartMonitoring();

        // Stop the monitoring service
        void StopMonitoring();

        // Check the status of the monitoring service
        bool IsMonitoringActive();

        // Optionally, you can add methods for handling specific monitoring tasks
        // void HandleSpecificEvent(SomeEventType event);
    }
}
