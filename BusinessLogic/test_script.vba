dim registerAlarm as System.Collections.ArrayList;
registerAlarm = new System.Collections.ArrayList(2);

dim alarmListArray as System.Collections.ArrayList;
alarmListArray = new System.Collections.ArrayList(64);

dim i as integer;
dim j as integer;

{

dim attribute_ref as indirect;
dim attribute_str as string;
dim device_name as string;
device_name = Me.Tagname;

dim alarmDescr_ref as indirect;
dim alarmDescr_str as string;

dim alarmPriority_ref as indirect;
dim alarmPriority_str as string;

'Clear AlarmListFaceplate
for i=1 to Me.AlarmListFaceplate.Dimension1
	Me.AlarmListFaceplate[i] = "";
next;


if Me.ALARM_EVENT_FP.00 then
	
	dim alarm_name as string;
	alarm_name = "ALARM_EVENT_EV1";
	attribute_str = device_name + "." + alarm_name;
	attribute_ref.BindTo(attribute_str);

	if attribute_ref.Value <> "" then
		registerAlarm = new System.Collections.ArrayList(2);
		
		alarmDescr_str = device_name + "." + alarm_name + ".Description";
		alarmDescr_ref.BindTo(alarmDescr_str);
		registerAlarm.Add(alarmDescr_ref.Value);

		alarmPriority_str = device_name + "." + alarm_name + ".Priority";
		alarmPriority_ref.BindTo(alarmPriority_str);
		registerAlarm.Add(alarmPriority_ref.Value);

		alarmListArray.Add(registerAlarm);
	endif;
endif;

if Me.ALARM_EVENT_FP.01 then
	
	dim alarm_name as string;
	alarm_name = "ALARM_EVENT_EV2";
	attribute_str = device_name + "." + alarm_name;
	attribute_ref.BindTo(attribute_str);

	if attribute_ref.Value <> "" then
		registerAlarm = new System.Collections.ArrayList(2);
		
		alarmDescr_str = device_name + "." + alarm_name + ".Description";
		alarmDescr_ref.BindTo(alarmDescr_str);
		registerAlarm.Add(alarmDescr_ref.Value);

		alarmPriority_str = device_name + "." + alarm_name + ".Priority";
		alarmPriority_ref.BindTo(alarmPriority_str);
		registerAlarm.Add(alarmPriority_ref.Value);

		alarmListArray.Add(registerAlarm);
	endif;
endif;



'FAULTS


if Me.FAULT_EVENT_FP.01 then
	
	dim alarm_name as string;
	alarm_name = "FAULT_EVENT_EV2";
	attribute_str = device_name + "." + alarm_name;
	attribute_ref.BindTo(attribute_str);

	if attribute_ref.Value <> "" then
		registerAlarm = new System.Collections.ArrayList(2);
		
		alarmDescr_str = device_name + "." + alarm_name + ".Description";
		alarmDescr_ref.BindTo(alarmDescr_str);
		registerAlarm.Add(alarmDescr_ref.Value);

		alarmPriority_str = device_name + "." + alarm_name + ".Priority";
		alarmPriority_ref.BindTo(alarmPriority_str);
		registerAlarm.Add(alarmPriority_ref.Value);

		alarmListArray.Add(registerAlarm);
	endif;
endif;

if Me.FAULT_EVENT_FP.02 then
	
	dim alarm_name as string;
	alarm_name = "FAULT_EVENT_EV3";
	attribute_str = device_name + "." + alarm_name;
	attribute_ref.BindTo(attribute_str);

	if attribute_ref.Value <> "" then
		registerAlarm = new System.Collections.ArrayList(2);
		
		alarmDescr_str = device_name + "." + alarm_name + ".Description";
		alarmDescr_ref.BindTo(alarmDescr_str);
		registerAlarm.Add(alarmDescr_ref.Value);

		alarmPriority_str = device_name + "." + alarm_name + ".Priority";
		alarmPriority_ref.BindTo(alarmPriority_str);
		registerAlarm.Add(alarmPriority_ref.Value);

		alarmListArray.Add(registerAlarm);
	endif;
endif;



' Bubble sort the alarms based on the priority
' If we have 0 or 1 alarm, we do not need to sort 
if alarmListArray.Count > 1 then
	For i = 0 To alarmListArray.Count - 2 step +1
		For j = i + 1 To alarmListArray.Count - 1  step +1
			dim firstItem as System.Collections.ArrayList;
			firstItem = alarmListArray.Item(i);
			dim secondItem as System.Collections.ArrayList;
			secondItem = alarmListArray.Item(j);
			If StringToIntg(firstItem.Item(1)) > StringToIntg(secondItem.Item(1)) Then
				dim tmp as System.Collections.ArrayList;
				tmp = alarmListArray.Item(i);
				alarmListArray.Item(i) = alarmListArray.Item(j);
				alarmListArray.Item(j) = tmp;
			endif;
		Next;
	Next;
endif;

' Insert the items in the alarm ListFaceplate
' TODO: test if we can use the ToArray() function
if alarmListArray.Count >= 1 then
	For i = 0 To alarmListArray.Count - 1
		dim item as System.Collections.ArrayList;
		item = alarmListArray.Item(i);
		Me.AlarmListFaceplate[i+1] = item.Item(0);
	Next;
endif;
}
