using System.Collections.Concurrent;
using System.Timers;
using Microsoft.Extensions.Options;
using MXAccesRestAPI.Classes;
using MXAccesRestAPI.Monitoring;
using MXAccesRestAPI.Settings;

namespace MXAccesRestAPI.MXDataHolder
{
    public class MXDataProcessorServiceFactory(IDataProviderService dataProviderService, IOptions<MxDataDataServiceSettings> settings, AttributeConfigSettings attributeConfig) : IMXDataHolderServiceFactory
    {
        private readonly System.Timers.Timer _timer = new();
        private int _lastTagCount = -1;


        private readonly ConcurrentDictionary<int, MXDataProcessorService> _services = [];
        private readonly ConcurrentDictionary<int, AlarmDataMonitor> _alarmMonitors = [];

        private readonly IDataProviderService _dataProvider = dataProviderService;
        private readonly MxDataDataServiceSettings _settings = settings.Value;
        private readonly AttributeConfigSettings _attributeConfig = attributeConfig;




        /// <summary>
        /// Register for an event when all tags are initialised
        /// </summary>
        public void RegisterOnInitializationComplete()
        {
            _timer.Enabled = false;
            _timer.Interval = 5000;

            if (!_timer.Enabled)
            {
                _timer.Elapsed += OnTimedEvent;
                _timer.Enabled = true;
            }
        }

        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            List<string> items = _dataProvider.GetAllTags();

            if (items.Count != _lastTagCount)
            {
                // still adddin tags
                Console.WriteLine($"{DateTime.Now} -> Initialised {items.Count} so far ...");
            }
            else
            {
                Console.WriteLine($"{DateTime.Now} -> Initialised {items.Count} DONE");
            }
        }




        public MXDataProcessorService Create(int threadNumber)
        {
            var service = new MXDataProcessorService(threadNumber, _settings.ServerName, _settings.LmxVerifyUser, _attributeConfig.AllowedTagAttributes, _dataProvider);

            bool isSuccess = _services.TryAdd(threadNumber, service);
            if (!isSuccess)
            {
                throw new InvalidOperationException($"Failed to add MXDataHolderService for thread number {threadNumber}. A service with the same thread number may already exist.");
            }
            return service;
        }

        /// <summary>
        /// Start monitoring alarms on all threads
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public void StartMonitoringAlarms()
        {

            foreach (int threadNo in _services.Keys)
            {
                MonitorAlarmsOnThread(threadNo);
            }
        }

        /// <summary>
        /// Stop monitoring alarms on all threads
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public void StopMonitoringAlarms()
        {

            foreach (int threadNo in _alarmMonitors.Keys)
            {
                StopMonitorAlarmsOnThread(threadNo);
            }
        }


        public void MonitorAlarmsOnThread(int threadNumber)
        {
            MXDataProcessorService service = _services[threadNumber];
            AlarmDataMonitor alarmMonitor = new(service, threadNumber);

            bool isSuccess = _alarmMonitors.TryAdd(threadNumber, alarmMonitor);
            if (!isSuccess)
            {
                throw new InvalidOperationException($"Failed to start AlarmMonitor for thread number {threadNumber}. A service with the same thread number may already exist.");
            }
            alarmMonitor.StartMonitoring();
        }


        public void StopMonitorAlarmsOnThread(int threadNumber)
        {
            _alarmMonitors[threadNumber].StopMonitoring();
        }
    }


}