using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using Org.Apache.Zookeeper.Data;
using Polly;
using Sitecore.Configuration;
using Sitecore.Diagnostics;
using Svenkle.SitecoreSolrOnStartup.Models;
using ZooKeeperNet;

namespace Svenkle.SitecoreSolrOnStartup.Creators
{
    public class CreateSearchStaxSolrCore : ICreateSolrCore
    {
        protected static bool Initialized;
        protected static ACL[] Acls;

        public bool CanCreate(ISystemInformation system, ICoreInformation core, string uri, string configuration)
        {
            var commandLineArgs = system.Document
                .SelectSingleNode("/response/lst[@name='jvm']/lst[@name='jmx']/arr[@name='commandLineArgs']")
                ?.InnerText ?? string.Empty;

            return system.Mode == Mode.SolrCloud && commandLineArgs.Contains("searchstax");
        }

        public void Create(HttpClient httpClient, ISystemInformation system, ICoreInformation core, string uri, string configuration, string coreName)
        {
            if (core.HasCore(coreName))
                return;

            var retryCount = Settings.GetIntSetting("ContentSearch.Solr.SearchStax.ZooKeeperRetryCount", 5);
            var numShards = Settings.GetIntSetting("ContentSearch.Solr.SearchStax.NumShards", 1);
            var replicationFactor = Settings.GetIntSetting("ContentSearch.Solr.SearchStax.ReplicationFactor", 1);
            var maxShardsPerNode = Settings.GetIntSetting("ContentSearch.Solr.SearchStax.MaxShardsPerNode", 1);
            var zooKeeperUri = Settings.GetSetting("ContentSearch.Solr.SearchStax.ZooKeeperServiceBaseAddress", $"{new Uri(uri).Host}:2181");
            var zooKeeperRoot = "/configs";
            var configurationPath = Path.Combine(configuration, system.Version);
            var zooKeeperCoreRoot = $"{zooKeeperRoot}/{coreName}";
            var zooKeeperRetryPolicy = Policy.Handle<Exception>().Retry(retryCount, (exception, retry) =>
            {
                Log.Warn($"Error communicating with ZooKeeper. Retrying... {retry} of {retryCount}", exception);
            });

            zooKeeperRetryPolicy.Execute(() =>
            {
                using (var zooKeeper = new ZooKeeper(zooKeeperUri, TimeSpan.FromMinutes(1), null))
                {
                    while (Equals(zooKeeper.State, ZooKeeper.States.CONNECTING)) { }

                    Initialize(zooKeeper, zooKeeperRoot);

                    var paths = CreateConfigurationPathDictionary(configurationPath);

                    CreateNode(zooKeeper, zooKeeperCoreRoot, new byte[0], Acls);

                    foreach (var path in paths)
                    {
                        var data = File.Exists(path.Value) ? File.ReadAllBytes(path.Value) : new byte[0];
                        CreateNode(zooKeeper, $"{zooKeeperCoreRoot}/{path.Key}", data, Acls);
                    }
                }

                CreateCore(httpClient, uri, coreName, numShards, replicationFactor, maxShardsPerNode);
            });
        }

        private static Dictionary<string, string> CreateConfigurationPathDictionary(string configurationPath)
        {
            var files = Directory.GetFiles(configurationPath, "*.*", SearchOption.AllDirectories);
            var directories = files.Select(Path.GetDirectoryName);

            var paths = files
                .Concat(directories)
                .OrderBy(x => x.Split('\\').Length)
                .Where(x => x.Replace(configurationPath, string.Empty).Length > 0)
                .Distinct()
                .ToDictionary(x => x.Substring(configurationPath.Length + 1).Replace(@"\", "/"));

            return paths;
        }

        private void Initialize(IZooKeeper zooKeeper, string zooKeeperRoot)
        {
            if (Initialized)
                return;

            Acls = zooKeeper.GetACL("/", new Stat()).ToArray();
            CreateNode(zooKeeper, zooKeeperRoot, new byte[0], Acls);
            Initialized = true;
        }

        private void CreateNode(IZooKeeper zooKeeper, string path, byte[] data, IEnumerable<ACL> acls)
        {
            try
            {
                zooKeeper.Create(path, data, acls, CreateMode.Persistent);
                Log.Info($"Created ZooKeeper node at {path}", this);
            }
            catch (KeeperException.NodeExistsException)
            {
                Log.Warn($"ZooKeeper node {path} already exists", this);
            }
        }

        private void CreateCore(HttpClient httpClient, string solrEndpointUri, string coreName, int numShards, int replicationFactor, int maxShardsPerNode)
        {
            try
            {
                var createCoreStatusXml = HttpClientHelper.GetXmlString(httpClient, $"{solrEndpointUri}/admin/collections?action=CREATE&name={coreName}&collection.configName={coreName}&numShards={numShards}&replicationFactor={replicationFactor}&maxShardsPerNode={maxShardsPerNode}");
                var status = new Status(createCoreStatusXml);

                if (status.IsSuccess)
                    Log.Info($"Created core {coreName}", this);
                else
                    Log.Error($"Unable to create SOLR core {coreName}. {status.Message}", this);
            }
            catch (Exception exception)
            {
                Log.Error($"Unable to create SOLR core {coreName}", exception, this);
            }
        }
    }
}
