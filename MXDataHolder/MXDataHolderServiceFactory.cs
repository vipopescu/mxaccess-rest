using System.Collections.Concurrent;
using System.Linq.Expressions;
using Microsoft.Extensions.Options;
using MXAccesRestAPI.Classes;
using MXAccesRestAPI.Monitoring;
using MXAccesRestAPI.Settings;

namespace MXAccesRestAPI.MXDataHolder
{
    public class MXDataHolderServiceFactory(IDataProviderService dataProviderService, IOptions<MxDataDataServiceSettings> settings, AttributeConfigSettings attributeConfig) : IMXDataHolderServiceFactory
    {
        
        private readonly ConcurrentDictionary<int, MXDataHolderService> _services = [];
        private readonly ConcurrentDictionary<int, AlarmDataMonitor> _alarmMonitors = [];

        private readonly IDataProviderService _dataProvider = dataProviderService;
        private readonly MxDataDataServiceSettings _settings = settings.Value;
        private readonly AttributeConfigSettings _attributeConfig = attributeConfig;

        public MXDataHolderService Create(int threadNumber)
        {
            var service = new MXDataHolderService(threadNumber, _settings.ServerName, _settings.LmxVerifyUser, _attributeConfig.AllowedTagAttributes, _dataProvider);

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
            MXDataHolderService service = _services[threadNumber];
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