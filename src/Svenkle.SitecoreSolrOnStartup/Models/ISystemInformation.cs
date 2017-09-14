using System.Xml;

namespace Svenkle.SitecoreSolrOnStartup.Models
{
    public interface ISystemInformation
    {
        Mode Mode { get; }
        string Path { get; }
        string Version { get; }
        XmlDocument Document { get; }
    }
}