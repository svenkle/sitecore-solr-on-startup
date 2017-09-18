using System.Xml;

namespace Svenkle.SitecoreSolrOnStartup.Models
{
    public interface ICoreInformation
    {
        XmlDocument Document { get; }
        bool HasCore(string name);
    }
}