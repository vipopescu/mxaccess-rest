namespace MXAccesRestAPI.Classes
{
    public class MXAttribute
    {
        public required string TagName { get; set; }
        public DateTime TimeStamp { get; set; }
        public object? Value { get; set; }
        public int Quality { get; set; }
        public bool OnAdvise { get; set; }
    }
}