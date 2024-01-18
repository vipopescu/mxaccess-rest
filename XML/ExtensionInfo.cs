using System.Xml.Serialization;

namespace MXAccesRestAPI.XML
{


    [XmlRoot(ElementName = "ExtensionInfo")]
    public class ExtensionInfo
    {

        [XmlElement(ElementName = "ObjectExtension")]
        public List<ObjectExtension> ObjectExtensions { get; set; } = [];

        [XmlElement(ElementName = "AttributeExtension")]
        public List<AttributeExtension> AttributeExtension { get; set; } = [];

        public List<ExtensionAttribute> GetExtensionsByAttrName(string tagName)
        {
            List<ExtensionAttribute> extensions = [];

            foreach (var extension in AttributeExtension)
            {
                foreach (ExtensionAttribute exAttr in extension.Extensions)
                {
                    if (exAttr.Name == tagName)
                    {
                        extensions.Add(exAttr);
                    }
                }
            }

            return extensions;
        }
    }

    [XmlRoot(ElementName = "AttributeExtension")]
    public class AttributeExtension
    {

        [XmlElement(ElementName = "Attribute")]
        public List<ExtensionAttribute> Extensions { get; set; } = [];
    }



    [XmlRoot(ElementName = "Attribute")]
    public class ExtensionAttribute
    {
        [XmlAttribute(AttributeName = "Name")]
        public required string Name { get; set; }


        [XmlAttribute(AttributeName = "ExtensionType")]
        public string? ExtensionType { get; set; }


        [XmlAttribute(AttributeName = "InheritedFromTagName")]
        public string? InheritedFromTagName { get; set; }
    }



    [XmlRoot(ElementName = "ObjectExtension")]
    public class ObjectExtension
    {

        [XmlElement(ElementName = "Attribute")]
        public List<Extension> Extensions { get; set; } = [];

    }

    [XmlRoot(ElementName = "Extension")]
    public class Extension
    {
        [XmlAttribute(AttributeName = "Name")]
        public string? Name { get; set; }


        [XmlAttribute(AttributeName = "ExtensionType")]
        public string? ExtensionType { get; set; }


        [XmlAttribute(AttributeName = "InheritedFromTagName")]
        public string? InheritedFromTagName { get; set; }
    }


}