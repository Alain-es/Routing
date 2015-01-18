using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Caching;

using Umbraco.Core.Logging;
using Umbraco.Web;
using Umbraco.Web.Models;

using Routing.Models;

namespace Routing.Helpers
{
    public class ConfigFileHelper
    {
        private const string _CacheIdRoutes = "Routing.CacheId.CachedRoutes";
        private const string _ConfigFile = "~/Config/Routing.config";
        private const string _ConfigDefaultValue = @"
            <Routes>
            </Routes>
            ";

        public static IEnumerable<Route> getRoutes()
        {
            IEnumerable<Route> result = new List<Route>();

            // Get routes from the cache
            result = HttpContext.Current.Cache.Get(_CacheIdRoutes) as IEnumerable<Route>;
            if (result == null)
            {
                // If no routes are cached then get them from the config file
                LoadAndCacheConfig();
                result = HttpContext.Current.Cache.Get(_CacheIdRoutes) as IEnumerable<Route>;
            }
            return result;
        }

        public static void LoadAndCacheConfig()
        {
            string configFilePath = System.Web.Hosting.HostingEnvironment.MapPath(_ConfigFile);

            try
            {
                // Check whether the config file exists
                if (!File.Exists(configFilePath))
                {
                    // Create a new config file with default values
                    XmlDocument xmlDocument = new XmlDocument();
                    xmlDocument.LoadXml(_ConfigDefaultValue);
                    xmlDocument.PreserveWhitespace = false;
                    xmlDocument.Save(configFilePath);
                }

                // Load config
                XElement xelement = XElement.Load(configFilePath);

                // Routes
                if (HttpContext.Current.Cache.Get(_CacheIdRoutes) as List<Route> == null)
                {
                    var Routes = from route in xelement.Elements("Route")
                                 select new Route()
                                 {
                                     UrlSegments = route.Attribute("UrlSegments").Value,
                                     Enabled = Convert.ToBoolean(route.Attribute("Enabled").Value)
                                 };
                    // Cache the result for a year but with dependency on the config file
                    HttpContext.Current.Cache.Add(_CacheIdRoutes, Routes, new CacheDependency(configFilePath), DateTime.Now.AddYears(1), Cache.NoSlidingExpiration, System.Web.Caching.CacheItemPriority.NotRemovable, null);
                }

            }
            catch (Exception ex)
            {
                LogHelper.Error(typeof(ConfigFileHelper), string.Format("Error loading routes from the config file : {0}", configFilePath), ex);
            }

        }

        public static XDocument LoadConfig()
        {
            var result = new XDocument();
            string configFilePath = System.Web.Hosting.HostingEnvironment.MapPath(_ConfigFile);
            LoadAndCacheConfig(); // Creates a new config file if it doesn't exist
            try
            {
                result = XDocument.Load(configFilePath);
            }
            catch (Exception ex)
            {
                LogHelper.Error(typeof(ConfigFileHelper), string.Format("Error loading routes from the config file : {0}", configFilePath), ex);
            }
            return result;
        }

        public static string SaveConfig(XDocument config)
        {
            string result = "Unexpected error.";
            string configFilePath = System.Web.Hosting.HostingEnvironment.MapPath(_ConfigFile);
            try
            {
                config.Save(configFilePath);
                result = string.Empty; // No error
            }
            catch (Exception ex)
            {
                result = string.Format("Error saving the config file {0}: {1}", configFilePath, ex.Message);
                LogHelper.Error(typeof(ConfigFileHelper), string.Format("Error saving the config file : {0}", configFilePath), ex);
            }
            return result;
        }


    }

}