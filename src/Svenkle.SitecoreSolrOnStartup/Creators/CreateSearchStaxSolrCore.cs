using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Net.Http;
using Org.Apache.Zookeeper.Data;
using Sitecore.Configuration;
using Sitecore.Diagnostics;
using Svenkle.SitecoreSolrOnStartup.Models;
using ZooKeeperNet;

namespace Svenkle.SitecoreSolrOnStartup.Creators
{
    public class CreateSearchStaxSolrCore : ICreateSolrCore
    {
        protected readonly IFileSystem FileSystem;
        protected static bool ZooKeeperInitialised;

        public CreateSearchStaxSolrCore()
        {
            FileSystem = new FileSystem();
        }

        public bool CanCreate(ISystemInformation system, ICoreInformation core, string uri, string configuration)
        {
            return system.Mode == Mode.SolrCloud;
        }

        public void Create(HttpClient httpClient, ISystemInformation system, ICoreInformation core, string uri, string configuration, string coreName)
        {
            if (core.HasCore(coreName))
                return;

            var configurationPath = FileSystem.Path.Combine(configuration, system.Version);
            var zooKeeperUri = Settings.GetSetting("ContentSearch.Solr.ZooKeeperServiceBaseAddress", $"{new Uri(uri).Host}:2181");
            var zooKeeperRoot = "/configs";
            var zooKeeperCoreRoot = $"{zooKeeperRoot}/{coreName}";

            var zooKeeper = new ZooKeeper(zooKeeperUri, TimeSpan.FromSeconds(15), null);

            while (Equals(zooKeeper.State, ZooKeeper.States.CONNECTING)) { }
            
            var acls = zooKeeper.GetACL("/", new Stat()).ToArray();

            var files = Directory.GetFiles(configurationPath, "*.*", SearchOption.AllDirectories);
            var directories = files.Select(Path.GetDirectoryName);

            var paths = files
                .Concat(directories)
                .OrderBy(x => x.Split('\\').Length)
                .Where(x => x.Replace(configurationPath, string.Empty).Length > 0)
                .Distinct()
                .ToDictionary(x => x.Substring(configurationPath.Length + 1).Replace(@"\", "/"));

            if (!ZooKeeperInitialised)
            {
                CreateNode(zooKeeper, zooKeeperRoot, new byte[0], acls);
                CreateNode(zooKeeper, zooKeeperCoreRoot, new byte[0], acls);
                ZooKeeperInitialised = true;
            }

            foreach (var path in paths)
            {
                var data = File.Exists(path.Value) ? File.ReadAllBytes(path.Value) : new byte[0];
                CreateNode(zooKeeper, $"{zooKeeperCoreRoot}/{path.Key}", data, acls);
            }

            try
            {
                var status = CreateCore(httpClient, uri, coreName);

                if (!status.IsSuccess)
                    Log.Warn($"Unable to create SOLR core {coreName}. {status.Message}", this);
            }
            catch (Exception exception)
            {
                Log.Error($"Unable to create SOLR core {coreName}", exception, this);
            }
        }

        private static void CreateNode(IZooKeeper zooKeeper, string path, byte[] data, IEnumerable<ACL> acls)
        {
            try
            {
                zooKeeper.Create(path, data, acls, CreateMode.Persistent);
            }
            catch (KeeperException.NodeExistsException)
            {
                // Ignore if the node already exists
            }
        }

        private static Status CreateCore(HttpClient httpClient, string solrEndpointUri, string coreName)
        {
            var createCoreStatusXml = HttpClientHelper.GetXmlString(httpClient, $"{solrEndpointUri}/admin/collections?action=CREATE&name={coreName}&collection.configName={coreName}&numShards=1");
            return new Status(createCoreStatusXml);
        }
    }
}
