using MXAccesRestAPI.Classes;
using MXAccesRestAPI.MXDataHolder;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MXAccesRestAPI.Monitoring
{
    public class AlarmDataMonitor : IDataStoreMonitor, IDisposable
    {
        private readonly IMXDataHolderService _dataHolderService;

        private bool isActive = false;

        public AlarmDataMonitor(IMXDataHolderService dataHolderService)
        {

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
                    // Console.WriteLine($"NEW      [ {data.TagName} ]");
                    break;
                case DataStoreChangeType.REMOVED:
                    // Console.WriteLine($"REMOVED  [ {data.TagName} ]");
                    break;
                case DataStoreChangeType.MODIFIED:
                    // Console.WriteLine($"MODIFIED [ {data.TagName} ] VAL -> {data.Value}");


                    if (data.TagName.EndsWith(".Alarm1") || data.TagName.EndsWith(".Alarm2"))
                    {

                        // TODO:
                        // Asset.Alarm1 = true
                        // Asset.Alarm1.InAlarm = true
                        //Console.WriteLine($"MODIFIED Alarm [ {data.TagName} ] VAL -> {data.Value}");
                        // PMCS & TUG
                        RaiseAlarm(data.TagName.Split('.')[0]);
                    }

                    else if (data.TagName.EndsWith(".ALARM_EVENT_FP") || data.TagName.EndsWith(".FAULT_EVENT_FP"))
                    {
                        RaiseEvent(data);

                    }
                    break;
            }
        }


        // PMCS TUG
        private void RaiseAlarm(string instanceTag)
        {
            List<(int, string)> tmpAlarmList = [];

            for (var i = 1; i < 16; i++)
            {
                string inAlarmRef = $"{instanceTag}.Alarm{i}.InAlarm";
                string descriptionRef = $"{instanceTag}.Alarm{i}.Description";
                string priorityRef = $"{instanceTag}.Alarm{i}.Priority";
                MXAttribute? inAlarm = _dataHolderService.GetData(inAlarmRef);
                MXAttribute? description = _dataHolderService.GetData(descriptionRef);
                MXAttribute? priority = _dataHolderService.GetData(priorityRef);

                if (inAlarm?.Value == null || description?.Value == null || priority?.Value == null)
                {
                    // Console.WriteLine($"ERROR: inAlarm, description or priority is empty");
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

            string alarmListArrRef = $"{instanceTag}.AlarmList";
            _dataHolderService.WriteData(alarmListArrRef, descriptions, DateTime.Now);
            Console.WriteLine($"AlarmList [ {alarmListArrRef} ] VAL -> {string.Join(',', descriptions)}");
        }


        private void RaiseEvent(MXAttribute mxEvent)
        {

            string instanceTag = mxEvent.TagName.Split('.')[0];
            string attrTag = mxEvent.TagName.Split('.')[1];

            int eventValue = int.Parse(mxEvent.Value.ToString() ?? "0");

            List<(int, string)> tmpAlarmList = [];

            string[] types = ["ALARM_EVENT_EV", "FAULT_EVENT_FP"];

            foreach (string test in types)
            {
                for (var i = 1; i < 33; i++)
                {
                    bool isAlarmSet = (eventValue & 1) != 0;
                    if (isAlarmSet)
                    {
                        string alarmRef = $"{instanceTag}.FAULT_EVENT_EV{i - 1}";

                        string descriptionRef = $"{alarmRef}.Description";
                        string priorityRef = $"{alarmRef}.Priority";

                        MXAttribute? description = _dataHolderService.GetData(descriptionRef);
                        MXAttribute? priority = _dataHolderService.GetData(priorityRef);

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

            string alarmListArrRef = $"{instanceTag}.AlarmList";
            _dataHolderService.WriteData(alarmListArrRef, descriptions, DateTime.Now);
            Console.WriteLine($"AlarmList [ {alarmListArrRef} ] VAL -> {string.Join(',', descriptions)}");
        }

    }
}
