# AlarmDataMonitor 

Contains logic for monitoring alarms & populating alarm faceplate

Tag attribute names are configured in *BusinessLogic/AlarmMonitorConfig.cs*

[Variable Naming Conventions](https://rwomcs.atlassian.net/wiki/spaces/SD/pages/30769153/Variable+Nomenclature+Standard)

## Alarm
**OnChange Triggers to populate Faceplate**
- AssetInstance1.SOME_ATTRIBUTE.InAlarm is triggered


**Population flow**

- Collect all instance alarm tags (ending in .InAlarm)
- For each instance tag
    - identify if it's it's ".InAlarm"
    - if inAlarm, then get description & priority
- Sort all active alarms that were collected by priority
- populate array **AssetInstance1.ALARMLIST** with "description"
    - ALARMLIST = [[dsaf], [dsaf], [dsaf]]




## Fault/Event Alarm

**OnChange Triggers to populate Faceplate**
- AssetInstance1.ALARM_EVENT_EV  set by PLC (ie: 34892380)
    - array of bits we get from **PLC**
- AssetInstance1.ALARM_EVENT_FP  set by PLC (ie: 34892380)
    - array of bits we get from **PLC**
- Triggered on chages for "AssetInstance1.ALARM_EVENT_FP" or "AssetInstance1.FAULT_EVENT_FP" 

**Population flow**

- Find all fault or alarm event tags part of this tag instance 
    - go through each bit in the value of "AssetInstance1.ALARM_EVENT_EV" or "AssetInstance1.ALARM_EVENT_FP"
    - if bit isn't set, then alarm is either OFF or it's not set at all
- for each active alarm
    - get the tag correlated to the InputSource of the active alarm
        - tagName => AssetInstance1.Alarm_Event_EV3.InputSource
        - value => Me.ALARM_EVENT_EV.02
    - get description & priority of the instance + attribute that is part of InputSource tag
        - instance + attribute => AssetInstance1.Alarm_Event_EV3
- Sort all active alarms that were collected by priority
- populate array **AssetInstance1.ALARMLIST** with "description"
    - ALARMLIST = [[dsaf], [dsaf], [dsaf]] 
        

![event fault alarm](./img/EventFaultAlarm.png)