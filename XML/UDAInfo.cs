using System.Xml.Serialization;

namespace MXAccesRestAPI.XML
{
    [XmlRoot(ElementName = "Attribute")]
    public class UDAAttribute
    {

        [XmlAttribute(AttributeName = "Name")]
        public string Name { get; set; }

        [XmlAttribute(AttributeName = "DataType")]
        public string DataType { get; set; }

        [XmlAttribute(AttributeName = "Category")]
        public string Category { get; set; }

        [XmlAttribute(AttributeName = "Security")]
        public string Security { get; set; }

        [XmlAttribute(AttributeName = "IsArray")]
        public bool IsArray { get; set; }

        [XmlAttribute(AttributeName = "HasBuffer")]
        public bool HasBuffer { get; set; }

        [XmlAttribute(AttributeName = "ArrayElementCount")]
        public int ArrayElementCount { get; set; }

        [XmlAttribute(AttributeName = "InheritedFromTagName")]
        public string InheritedFromTagName { get; set; }
    }

    [XmlRoot(ElementName = "UDAInfo")]
    public class UDAInfo
    {

        [XmlElement(ElementName = "Attribute")]
        public List<UDAAttribute> Attributes { get; set; }
    }
}