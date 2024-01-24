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

                    try
                    {

                        // (PMCS) if an tag with inAlarm is modified
                        if (data.TagName.EndsWith($".{AlarmMonitorConfig.ALARM_IN_ALARM_ATTR}"))
                        {
                            PopulateAlarmListFromAlarms(data.TagName.Split('.')[0]);
                        }

                        // (TMCS) if attr with Alarm or Fault event is modified
                        else if (data.TagName.EndsWith($".{AlarmMonitorConfig.FP_ALARM_EVENT}") ||
                        data.TagName.EndsWith($".{AlarmMonitorConfig.FP_FAULT_EVENT}"))
                        {
                            PopulateAlarmListFromEventFault(data);
                        }
                    }
                    catch (Exception ex)
                    {
                        // tag is probably not found
                        Console.WriteLine($"ERROR [DataHolderService_OnDataStoreChanged]: T[{_threadNumber}] MODIFIED [ {data.TagName} ] {ex.Message}");
                    }

                    break;
            }


        }


        /// <summary>
        /// Populates AlarmListFaceplate based on a raised alarm and alarm priority
        /// (PMCS)
        /// </summary>
        /// <param name="instanceTag"></param>
        private void PopulateAlarmListFromAlarms(string instanceTag)
        {
            List<(int, string)> tmpAlarmList = [];

            List<MXAttribute> instanceTags = _dataHolderService.GetInstanceData(instanceTag);

            // Get all alarms on this tag instance
            string[] alarmTagRefs = instanceTags.Where(tag => tag.TagName.EndsWith($".{AlarmMonitorConfig.ALARM_IN_ALARM_ATTR}")).Select(tag => tag.TagName.Replace($".{AlarmMonitorConfig.ALARM_IN_ALARM_ATTR}", "")).ToArray();

            foreach (string alarmEventRef in alarmTagRefs)
            {

                string inAlarmRef = $"{alarmEventRef}.{AlarmMonitorConfig.ALARM_IN_ALARM_ATTR}";

                MXAttribute? inAlarm = _dataHolderService.GetData(inAlarmRef);
                if (inAlarm?.Value == null)
                {
                    // Values here will be null when initiated but not populated
                    continue;
                }

                bool inAlarmVal = bool.Parse(inAlarm.Value.ToString() ?? "False");

                // collect active alarms description & priorities
                if (inAlarmVal)
                {
                    string descriptionRef = $"{alarmEventRef}.{AlarmMonitorConfig.ALARM_DESCRIPTION_ATTR}";
                    string priorityRef = $"{alarmEventRef}.{AlarmMonitorConfig.ALARM_PRIORITY_ATTR}";
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

            // Lower number indicates higher priority
            tmpAlarmList.Sort((a, b) => b.Item1.CompareTo(a.Item1));

            // Extract only the descriptions (second part of the tuple) from tmpAlarmList
            List<string> descriptions = tmpAlarmList.Select(alarm => alarm.Item2).ToList();

            string alarmListArrRef = $"{instanceTag}.{AlarmMonitorConfig.FACEPLATE_ALARM_LIST_ARRAY}";

            _dataHolderService.WriteData(alarmListArrRef, descriptions, DateTime.Now);
            if (descriptions.Count > 0)
            {
                Console.WriteLine($"T[{_threadNumber}] AlarmListFaceplate [ {alarmListArrRef} ] VAL -> {string.Join(',', descriptions)}");
            }

        }



        /// <summary>
        /// Populates AlarmListFaceplate based on from a raised event and alarm priority
        /// (TMCS)
        /// </summary>
        /// <param name="mxEvent"></param>
        private void PopulateAlarmListFromEventFault(MXAttribute mxEvent)
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

            string type = "";
            if (attrTag == AlarmMonitorConfig.FP_FAULT_EVENT)
            {
                type = AlarmMonitorConfig.PLC_IO_FAULT_EVENT;
            }
            else if (attrTag == AlarmMonitorConfig.FP_ALARM_EVENT)
            {
                type = AlarmMonitorConfig.PLC_IO_ALARM_EVENT;
            }

            string ioSource = $"Me.{type}.";

            for (var i = 0; i < 32; i++)
            {

                // defines WHEN this alarm is active (should match)
                string activeAlarmStateRef = $"{instanceTag}.{type}{i}.{AlarmMonitorConfig.ALARM_ACTIVE_ALARM_STATE}";
                MXAttribute? activeAlarmState = _dataHolderService.GetData(activeAlarmStateRef);

                if (activeAlarmState?.Value == null)
                {
                    // alarm not set
                    continue;
                }

                bool isAlarmActive = (bool)activeAlarmState.Value;
                // get i bit in eventValue
                int bit = (eventValue >> i) & 1;
                bool isBitSet = bit != 0;

                if (isBitSet == isAlarmActive)
                {

                    List<MXAttribute> instanceTags = _dataHolderService.GetInstanceData(instanceTag);
                    string ioSourceI = $"{ioSource}.{i:00}";
                    
                    MXAttribute? alarmInputSource = instanceTags.Where(tag => tag.TagName.EndsWith($".{AlarmMonitorConfig.INPUT_SOURCE}") && tag.Value?.ToString() == ioSourceI).FirstOrDefault();

                    if (alarmInputSource == null)
                    {
                        // no alarm?
                        continue;
                    }
                    
                    string alarmTagRef = alarmInputSource.TagName.Replace($".{AlarmMonitorConfig.INPUT_SOURCE}","");

                    string descriptionRef = $"{alarmTagRef}.{AlarmMonitorConfig.ALARM_DESCRIPTION_ATTR}";
                    string priorityRef = $"{alarmTagRef}.{AlarmMonitorConfig.ALARM_PRIORITY_ATTR}";

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


