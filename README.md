# Sitecore SOLR on Startup
A Sitecore initialization pipeline that automatically creates SOLR cores on the target SOLR server - similar to the Sitecore Lucene functionality.

## Getting Started
### NuGet Package (recommended)
`Install-Package Svenkle.SitecoreSolrOnStartup`

### Sitecore Package
Install [Sitecore.Package.zip](https://github.com/svenkle/sitecore-solr-on-startup/releases/latest) 

## FAQ

### What versions are supported?
Currently v5.1.0 for both [SIM](https://marketplace.sitecore.net/en/Modules/Sitecore_Instance_Manager.aspx) and [MeasuredSearch](measuredsearch.com)

### Can I use this on SOLR version X?
Yes. Just add the default configuration files for your SOLR version into the App_Data folder following the versioning convention. The folder name must match exactly to that of your target SOLR instance.

### Will you support SOLR running in X and Y configuration?
Possibly. The supported instances may grow over time - please feel free to develop your own and submit a PR.
