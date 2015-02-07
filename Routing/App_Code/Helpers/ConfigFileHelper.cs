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

using Routing.Models;


namespace Routing.Helpers
{
    public class ConfigFileHelper
    {

        public static IEnumerable<Route> getRoutes()
        {
            IEnumerable<Route> result = new List<Route>();

            // Get routes from the cache
            result = HttpContext.Current.Cache.Get(Routing.Constants.Cache.RoutesConfigCacheId) as IEnumerable<Route>;
            if (result == null)
            {
                // If no routes are cached then get them from the config file
                LoadAndCacheConfig();
                result = HttpContext.Current.Cache.Get(Routing.Constants.Cache.RoutesConfigCacheId) as IEnumerable<Route>;
            }
            return result;
        }

        public static void LoadAndCacheConfig()
        {
            try
            {
                // Get routes from cache
                if (HttpContext.Current.Cache.Get(Routing.Constants.Cache.RoutesConfigCacheId) as IEnumerable<Route> == null)
                {

                    // Check whether the config file exists
                    if (!File.Exists(Routing.Constants.Config.ConfigFilePhysicalPath))
                    {
                        // Create a new config file with default values
                        XDocument xDocument = new XDocument(
                            new XElement("Routes",
                                new XComment(@"<Route UrlSegments=""/Test/"" Enabled=""true"" DocumentTypeAlias="""" PropertyAlias="""" Template="""" ForceTemplate=""false"" FallbackNodeId="""" Description=""Example"" />")
                                )
                            );
                        xDocument.Save(Routing.Constants.Config.ConfigFilePhysicalPath);
                    }

                    // Load config
                    XElement xelement = XElement.Load(Routing.Constants.Config.ConfigFilePhysicalPath);

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
                        xelement.Save(Routing.Constants.Config.ConfigFilePhysicalPath);
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
                    HttpContext.Current.Cache.Add(Routing.Constants.Cache.RoutesConfigCacheId, Routes, new CacheDependency(Routing.Constants.Config.ConfigFilePhysicalPath), DateTime.Now.AddYears(1), Cache.NoSlidingExpiration, System.Web.Caching.CacheItemPriority.NotRemovable, null);
                }

            }
            catch (Exception ex)
            {
                LogHelper.Error(typeof(ConfigFileHelper), string.Format("Error loading routes from the config file : {0}", Routing.Constants.Config.ConfigFilePhysicalPath), ex);
            }

        }

        public static XDocument LoadConfig()
        {
            var result = new XDocument();
            LoadAndCacheConfig(); // Creates a new config file if it doesn't exist
            try
            {
                result = XDocument.Load(Routing.Constants.Config.ConfigFilePhysicalPath);
            }
            catch (Exception ex)
            {
                LogHelper.Error(typeof(ConfigFileHelper), string.Format("Error loading routes from the config file : {0}", Routing.Constants.Config.ConfigFilePhysicalPath), ex);
            }
            return result;
        }

        public static string SaveConfig(XDocument config)
        {
            string result = "Unexpected error.";
            try
            {
                config.Save(Routing.Constants.Config.ConfigFilePhysicalPath);
                result = string.Empty; // No error
            }
            catch (Exception ex)
            {
                result = string.Format("Error saving the config file {0}: {1}", Routing.Constants.Config.ConfigFilePhysicalPath, ex.Message);
                LogHelper.Error(typeof(ConfigFileHelper), string.Format("Error saving the config file : {0}", Routing.Constants.Config.ConfigFilePhysicalPath), ex);
            }
            return result;
        }


    }

}