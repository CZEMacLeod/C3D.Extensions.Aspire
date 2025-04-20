using System.Xml.Serialization;

namespace C3D.Extensions.Aspire.IISExpress.Configuration;

public class Application
{
    [XmlAttribute("path")]
    public required string Path { get; set; }

    [XmlElement("applicationPool")]
    public required string ApplicationPool { get; set; }

    [XmlElement("virtualDirectory")]
    public required VirtualDirectory VirtualDirectory { get; set; }
}
