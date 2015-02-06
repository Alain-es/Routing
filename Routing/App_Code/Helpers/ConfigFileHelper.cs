using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Caching;

using Umbraco.Web;
using Umbraco.Web.Models;
using Umbraco.Core;
using Umbraco.Core.Models.Membership;
using Umbraco.Core.Logging;

using Routing.Models;
using Routing.Extensions;

namespace Routing.Helpers
{
    public class ConfigFileHelper
    {
        private const string _CacheIdRoutes = "Routing.CacheId.CachedRoutes";
        private const string _ConfigFile = "~/Config/Routing.config";

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
                // Get routes from cache
                if (HttpContext.Current.Cache.Get(_CacheIdRoutes) as IEnumerable<Route> == null)
                {

                // Check whether the config file exists
                if (!File.Exists(configFilePath))
                {
                    // Create a new config file with default values
                        XDocument xDocument = new XDocument(
                            new XElement("Routes",
                                new XComment(@"<Route UrlSegments=""/Test/"" Enabled=""true"" DocumentTypeAlias="""" PropertyAlias="""" Template="""" ForceTemplate=""false"" FallbackNodeId="""" Description=""Example"" />")
                                )
                            );
                        xDocument.Save(configFilePath);
                }

                // Load config
                XElement xelement = XElement.Load(configFilePath);

                    // Check whether the "Enabled" and "ForceTemplate" attributes contain valid boolean values. 
                    bool requireSaving = false;
                    foreach (XElement element in xelement.Descendants())
                {
                        bool enabledAttribute;
                        if (!bool.TryParse(element.Attribute("Enabled").Value, out enabledAttribute))
                        {
                            element.SetAttributeValue("Enabled", "false");
                            requireSaving = true;
                        }
                        bool forceTemplateAttribute;
                        if (!bool.TryParse(element.Attribute("ForceTemplate").Value, out forceTemplateAttribute))
                        {
                            element.SetAttributeValue("ForceTemplate", "false");
                            requireSaving = true;
                        }
                    }
                    if (requireSaving)
                    {
                        xelement.Save(configFilePath);
                    }

                    var Routes = from route in xelement.Elements("Route")
                                 select new Route()
                                 {
                                     UrlSegments = route.Attribute("UrlSegments").Value,
                                      Enabled = Convert.ToBoolean(route.Attribute("Enabled").Value),
                                      DocumentTypeAlias = route.Attribute("DocumentTypeAlias").Value,
                                      PropertyAlias = route.Attribute("PropertyAlias").Value,
                                      Template = route.Attribute("Template").Value,
                                      ForceTemplate = Convert.ToBoolean(route.Attribute("ForceTemplate").Value),
                                      FallbackNodeId = route.Attribute("FallbackNodeId").Value,
                                      Description = route.Attribute("Description").Value
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