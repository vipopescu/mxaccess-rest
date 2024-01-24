namespace MXAccesRestAPI.MXDataHolder
{
    public interface IMXDataHolderServiceFactory
    {

        MXDataProcessorService Create(int threadNumber);

        void StartMonitoringAlarms();
        void StopMonitoringAlarms();


        void MonitorAlarmsOnThread(int threadNumber);
        void StopMonitorAlarmsOnThread(int threadNumber);


        void RegisterOnInitializationComplete();

    }

}