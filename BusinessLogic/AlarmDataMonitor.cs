using MXAccesRestAPI.Classes;
using MXAccesRestAPI.MXDataHolder;

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
                        Console.WriteLine($"MODIFIED Alarm [ {data.TagName} ] VAL -> {data.Value}");
                        // PMCS & TUG
                        RaiseAlarm(data.TagName.Split('.')[0]);
                    }
                    break;
            }
        }


        // PMCS TUG
        private void RaiseAlarm(string instanceTag)
        {
            List<(int, string)> tmpAlarmList = [];

            // TODO:
            // get instance (Asset)
            // for each instance, go through Asset.AlarmX (x -> 1 - 16)
            // get attributes InAlarm
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
                    Console.WriteLine($"ERROR: inAlarm, description or priority is empty");
                    continue;
                }

                int priorityVal = int.Parse(priority.Value.ToString());
                string descriptionVal = priority.Value.ToString() ?? "";
                bool inAlarmVal = bool.Parse(inAlarm.Value.ToString());

                // collect active alarms
                if (inAlarmVal)
                {
                    tmpAlarmList.Add((priorityVal, descriptionVal));
                }
            }

            // Lower number indicates higher priority
            tmpAlarmList.Sort((a, b) => b.Item1.CompareTo(a.Item1));

            string alarmListArrRef = $"{instanceTag}.AlarmList";
            _dataHolderService.WriteData(alarmListArrRef, tmpAlarmList, DateTime.Now);

        }

    }
}
