using MXAccesRestAPI.Classes;
using MXAccesRestAPI.MXDataHolder;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MXAccesRestAPI.Monitoring
{
    public partial class AlarmDataMonitor : IDataStoreMonitor, IDisposable

    {
        [GeneratedRegex("." + AlarmMonitorConfig.ALARM_EVENT + @"\d*$")]
        private static partial Regex AlarmRegex();

        private readonly IMXDataHolderService _dataHolderService;

        private bool isActive = false;

        private readonly int _threadNumber;


        public AlarmDataMonitor(IMXDataHolderService dataHolderService, int threadNumber)
        {

            _threadNumber = threadNumber;
            _dataHolderService = dataHolderService;
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
            Console.WriteLine($"T[{_threadNumber}] StartMonitoring");
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
                    //Console.WriteLine($"T[{_threadNumber}] NEW      [ {data.TagName} ]");
                    break;
                case DataStoreChangeType.REMOVED:
                    //Console.WriteLine($"T[{_threadNumber}] REMOVED  [ {data.TagName} ]");
                    break;
                case DataStoreChangeType.MODIFIED:
                    //Console.WriteLine($"T[{_threadNumber}] MODIFIED [ {data.TagName} ] VAL -> {data.Value}");

                    return;
                    if (AlarmRegex().IsMatch(data.TagName))
                    {
                        PopulateAlarmList(data.TagName.Split('.')[0]);
                    }


                    else if (data.TagName.EndsWith($".{AlarmMonitorConfig.FP_ALARM_EVENT}") ||
                    data.TagName.EndsWith($".{AlarmMonitorConfig.FP_FAULT_EVENT}"))
                    {
                        PopulateAlarmListFaceplate(data);
                    }
                    break;
            }
        }


        /// <summary>
        /// Populates AlarmListFaceplate based on from a raised alarm and alarm priority
        /// </summary>
        /// <param name="instanceTag"></param>
        private void PopulateAlarmList(string instanceTag)
        {
            List<(int, string)> tmpAlarmList = [];

            for (var i = 1; i < 16; i++)
            {
                string inAlarmRef = $"{instanceTag}.{AlarmMonitorConfig.ALARM_EVENT}{i}.{AlarmMonitorConfig.ALARM_IN_ALARM_ATTR}";
                string descriptionRef = $"{instanceTag}.{AlarmMonitorConfig.ALARM_EVENT}{i}.{AlarmMonitorConfig.ALARM_DESCRIPTION_ATTR}";
                string priorityRef = $"{instanceTag}.{AlarmMonitorConfig.ALARM_EVENT}{i}.{AlarmMonitorConfig.ALARM_PRIORITY_ATTR}";
                MXAttribute? inAlarm = _dataHolderService.GetData(inAlarmRef);
                MXAttribute? description = _dataHolderService.GetData(descriptionRef);
                MXAttribute? priority = _dataHolderService.GetData(priorityRef);

                if (inAlarm?.Value == null || description?.Value == null || priority?.Value == null)
                {
                    // Values here will be null when initiated but not populated
                    continue;
                }

                int priorityVal = int.Parse(priority.Value.ToString());
                string descriptionVal = description.Value.ToString() ?? "";
                bool inAlarmVal = bool.Parse(inAlarm.Value.ToString() ?? "False");

                // collect active alarms
                if (inAlarmVal)
                {
                    tmpAlarmList.Add((priorityVal, descriptionVal));
                }
            }

            // Lower number indicates higher priority
            tmpAlarmList.Sort((a, b) => b.Item1.CompareTo(a.Item1));

            // Extract only the descriptions (second part of the tuple) from tmpAlarmList
            List<string> descriptions = tmpAlarmList.Select(alarm => alarm.Item2).ToList();

            string alarmListArrRef = $"{instanceTag}.{AlarmMonitorConfig.FACEPLATE_ALARM_LIST_ARRAY}";
            _dataHolderService.WriteData(alarmListArrRef, descriptions, DateTime.Now);
            if (descriptions.Count > 0)
            {
                Console.WriteLine($"T[{_threadNumber}] AlarmList [ {alarmListArrRef} ] VAL -> {string.Join(',', descriptions)}");
            }

        }



        /// <summary>
        /// Populates AlarmListFaceplate based on from a raised event and alarm priority
        /// 
        /// </summary>
        /// <param name="mxEvent"></param>
        private void PopulateAlarmListFaceplate(MXAttribute mxEvent)
        {
            if (mxEvent.Value == null)
            {
                // initiated but not populated 
                return;
            }


            string instanceTag = mxEvent.TagName.Split('.')[0];
            string attrTag = mxEvent.TagName.Split('.')[1];

            int eventValue = int.Parse(mxEvent.Value.ToString() ?? "0");


            List<(int, string)> tmpAlarmList = [];

            string[] types = [AlarmMonitorConfig.PLC_IO_ALARM_EVENT, AlarmMonitorConfig.PLC_IO_FAULT_EVENT];


            foreach (string type in types)
            {
                for (var i = 1; i < 33; i++)
                {
                    // get i bit in eventValue
                    int bit = (eventValue >> i) & 1;
                    bool isAlarmSet = bit != 0;
                    if (isAlarmSet)
                    {
                        string alarmRef = $"{instanceTag}.{type}{i - 1}";

                        string descriptionRef = $"{alarmRef}.{AlarmMonitorConfig.ALARM_DESCRIPTION_ATTR}";
                        string priorityRef = $"{alarmRef}.{AlarmMonitorConfig.ALARM_PRIORITY_ATTR}";

                        MXAttribute? description = _dataHolderService.GetData(descriptionRef);
                        MXAttribute? priority = _dataHolderService.GetData(priorityRef);

                        if (description?.Value == null || priority?.Value == null)
                        {
                            // Values here will be null when initiated but not populated
                            continue;
                        }


                        int priorityVal = int.Parse(priority.Value.ToString());
                        string descriptionVal = description.Value.ToString() ?? "";

                        tmpAlarmList.Add((priorityVal, descriptionVal));
                    }

                }
            }


            // Lower number indicates higher priority
            tmpAlarmList.Sort((a, b) => b.Item1.CompareTo(a.Item1));

            // Extract only the descriptions (second part of the tuple) from tmpAlarmList
            List<string> descriptions = tmpAlarmList.Select(alarm => alarm.Item2).ToList();
            string alarmListArrRef = $"{instanceTag}.{AlarmMonitorConfig.FACEPLATE_ALARM_LIST_ARRAY}";
            _dataHolderService.WriteData(alarmListArrRef, descriptions, DateTime.Now);
            if (descriptions.Count > 0)
            {
                Console.WriteLine($"T[{_threadNumber}] EventsFaultAlarm [ {alarmListArrRef} ] VAL -> {string.Join(',', descriptions)}");
            }


        }

    }
}
