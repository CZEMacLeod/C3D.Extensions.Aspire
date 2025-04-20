using System.Xml.Serialization;

namespace C3D.Extensions.Aspire.IISExpress.Configuration;

public class VirtualDirectory
{
    [XmlAttribute("path")]
    public required string Path { get; set; }

    [XmlAttribute("physicalPath")]
    public required string PhysicalPath { get; set; }
}
