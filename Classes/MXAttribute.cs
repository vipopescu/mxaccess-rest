namespace MXAccesRestAPI.Classes
{
    public class MXAttribute
    {

        public required int Key { get; set; }
        public required string TagName { get; set; }
        public DateTime TimeStamp { get; set; }
        public object? Value { get; set; }
        public int Quality { get; set; }
        public bool OnAdvise { get; set; }
        public bool initialized = false;
        public int? CurrentThread { get; set; }
        public int InitalizedChecks { get; set; } = 0;
    }




    public class MxDevice
    {
        public string? TagName { get; set; }
        private readonly List<UdaAttribute> UdaAttributes = [];

        // TODO: depending on how big that list gets
        // public ConcurrentDictionary<int, UdaAttribute>  Attributes = new();

        public void AddAttribute(UdaAttribute attribute)
        {

            UdaAttributes.Add(attribute);
        }

        public UdaAttribute? GetByAttribute(string attributeName)
        {

            return UdaAttributes.FirstOrDefault(a => a.AttributeName == attributeName);
        }
    }



    public class UdaAttribute
    {

        public required string AttributeName { get; set; }

        public string? Description { get; set; }

        public object? Value { get; set; }
        public DateTime TimeStamp { get; set; }

        public List<IExtensions> Extensions = [];


        //TODO: do we need these?
        public int Quality { get; set; }
        public bool OnAdvise { get; set; }

        public AlarmExtension? GetAlarmExtension()
        {

            return Extensions.FirstOrDefault(a => a.GetType() == typeof(AlarmExtension)) as AlarmExtension;
        }

    }

    public class IExtensions
    {


    }

    public class AlarmExtension : IExtensions
    {
        public bool ActiveAlarmState { get; set; }
        public string? Priority { get; set; }

        public string? Description { get; set; }

    }

    // TODO
    public class IOExtension : IExtensions
    {
        public bool IOSource { get; set; }

    }


    // TODO
    public class HistExtension : IExtensions
    {

    }

}