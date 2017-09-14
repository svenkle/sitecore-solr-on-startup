using System.Xml;

namespace Svenkle.SitecoreSolrOnStartup.Models
{
    public interface IStatus
    {
        XmlDocument Document { get; }
        bool IsSuccess { get; }
        string Message { get; }
    }
}