using Routing.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Caching;
using System.Xml.Linq;
using Umbraco.Core;
using Umbraco.Core.Logging;


namespace Routing.Controllers
{
    public class ConfigController
    {

        public IEnumerable<Route> getRoutes()
        {
            // Get routes from the cache
            IEnumerable<Route> result = Routing.Helpers.CacheHelper.Get(Routing.Constants.Cache.ConfigRoutesCacheId) as IEnumerable<Route>;
            if (result == null)
            {
                // If no routes are cached then get them from the config file
                LoadAndCacheConfig();
                result = Routing.Helpers.CacheHelper.Get(Routing.Constants.Cache.ConfigRoutesCacheId) as IEnumerable<Route>;
            }
            return result ?? new List<Route>();
        }

        public Settings getSettings()
        {
            // Get the settings from the cache
            Settings result = Routing.Helpers.CacheHelper.Get(Routing.Constants.Cache.ConfigSettingsCacheId) as Settings;
            if (result == null)
            {
                LoadAndCacheConfig();
                result = Routing.Helpers.CacheHelper.Get(Routing.Constants.Cache.ConfigSettingsCacheId) as Settings;
            }
            return result ?? new Settings();
        }

        public void LoadAndCacheConfig()
        {
            // Setup the method that detects when the config file is modified
            DetectConfigFileChanges();

            // Check whether the config file exists and is up to date. Cache its content.
            Routing.Helpers.CacheHelper.GetExistingOrAddToCache(Routing.Constants.Cache.ConfigCacheId, 3600 * 24 * 365, System.Web.Caching.CacheItemPriority.NotRemovable,

                // Anonymous method that gets the config
                () =>
                {
                    XElement xelement = null;

                    try
                    {
                        // Check whether the config file exists
                        if (!File.Exists(Routing.Constants.Config.ConfigFilePhysicalPath) || string.IsNullOrWhiteSpace(System.IO.File.ReadAllText(Routing.Constants.Config.ConfigFilePhysicalPath)))
                        {
                            // Create a new config file with default values
                            XDocument xDocument = new XDocument(
                                new XElement("Routing",
                                    new XElement("Settings",
                                        new XElement("RoutesExamineSearchProvider", "ExternalSearcher"),
                                        new XElement("RoutesAreCaseSensitive", "false"),
                                        new XComment("Important: You will need to setup and use an accent insensitive Examine indexer/searcher in order to having case insensitive routes (setting below) to work properly"),
                                        new XElement("RoutesAreAccentSensitive", "true"),
                                        new XElement("CacheDurationInHours", "24"),
                                        new XElement("PersistentCacheMapPath", "~/App_Data/TEMP/Routing/persistentCache.dat"),
                                        new XElement("PersistentCacheUpdateFrequencyInMinutes", "10"),
                                        new XElement("EncryptionKey", "")
                                        ),
                                    new XElement("Routes",
                                        new XComment(@"<Route UrlSegments=""/Test/"" Enabled=""true"" DocumentTypeAlias="""" PropertyAlias="""" PropertyAliasExactMatch="""" MatchNodeFullUrl=""false"" Template="""" ForceTemplate=""false"" FallbackNodeId="""" Description=""Example"" />")
                                        )
                                    )
                                );
                            xDocument.Save(Routing.Constants.Config.ConfigFilePhysicalPath);
                        }

                        // Load config
                        xelement = XElement.Load(Routing.Constants.Config.ConfigFilePhysicalPath);

                        // Check whether the <Routing> root node exists. If not so then update the config file's structure
                        if (!xelement.Name.LocalName.Equals("Routing"))
                        {
                            XDocument xDocument = new XDocument(
                                new XElement("Routing",
                                    new XElement("Settings",
                                        new XElement("RoutesExamineSearchProvider", "ExternalSearcher"),
                                        new XElement("RoutesAreCaseSensitive", "false"),
                                        new XComment("Important: You will need to setup and use an accent insensitive Examine indexer/searcher in order to having case insensitive routes (setting below) to work properly"),
                                        new XElement("RoutesAreAccentSensitive", "true"),
                                        new XElement("CacheDurationInHours", "24"),
                                        new XElement("PersistentCacheMapPath", "~/App_Data/TEMP/Routing/persistentCache.dat"),
                                        new XElement("PersistentCacheUpdateFrequencyInMinutes", "10"),
                                        new XElement("EncryptionKey", "")
                                        ),
                                    xelement
                                )
                            );
                            xDocument.Save(Routing.Constants.Config.ConfigFilePhysicalPath);
                            xelement = XElement.Load(Routing.Constants.Config.ConfigFilePhysicalPath);
                        }

                        //Settings: Check whether the 'RoutesAreCaseSensitive', 'RoutesAreAccentSensitive', 'CacheDurationInHours' and 'PersistentCacheUpdateFrequencyInMinutes' attributes contain valid boolean/integer values. 
                        bool requireSaving = false;
                        if (xelement.Element("Settings") != null)
                        {
                            foreach (XElement element in xelement.Element("Settings").Descendants())
                            {
                                bool routesAreCaseSensitive;
                                if (element.Name.LocalName.InvariantEquals("RoutesAreCaseSensitive") && !bool.TryParse(element.Value, out routesAreCaseSensitive))
                                {
                                    element.SetValue("false");
                                    requireSaving = true;
                                }
                                bool routesAreAccentSensitive;
                                if (element.Name.LocalName.InvariantEquals("RoutesAreAccentSensitive") && !bool.TryParse(element.Value, out routesAreAccentSensitive))
                                {
                                    element.SetValue("true");
                                    requireSaving = true;
                                }
                                int cacheDurationInHours;
                                if (element.Name.LocalName.InvariantEquals("CacheDurationInHours") && !int.TryParse(element.Value, out cacheDurationInHours))
                                {
                                    element.SetValue("24");
                                    requireSaving = true;
                                }
                                int persistentCacheUpdateFrequencyInMinutes;
                                if (element.Name.LocalName.InvariantEquals("PersistentCacheUpdateFrequencyInMinutes") && !int.TryParse(element.Value, out persistentCacheUpdateFrequencyInMinutes))
                                {
                                    element.SetValue("10");
                                    requireSaving = true;
                                }
                            }
                        }

                        //Routes: Check whether the 'Enabled', 'ForceTemplate', 'MatchNodeFullUrl', 'CaseSensitive' and 'AccentSensitive' attributes contain valid boolean values. 
                        foreach (XElement element in xelement.Element("Routes").Descendants())
                        {
                            bool enabledAttribute;
                            if (element.Attribute("Enabled") != null && !bool.TryParse(element.Attribute("Enabled").Value, out enabledAttribute))
                            {
                                element.SetAttributeValue("Enabled", "true");
                                requireSaving = true;
                            }
                            bool forceTemplateAttribute;
                            if (element.Attribute("ForceTemplate") != null && !bool.TryParse(element.Attribute("ForceTemplate").Value, out forceTemplateAttribute))
                            {
                                element.SetAttributeValue("ForceTemplate", "false");
                                requireSaving = true;
                            }
                            bool matchNodeFullUrlAttribute;
                            if (element.Attribute("MatchNodeFullUrl") != null && !bool.TryParse(element.Attribute("MatchNodeFullUrl").Value, out matchNodeFullUrlAttribute))
                            {
                                element.SetAttributeValue("MatchNodeFullUrl", "false");
                                requireSaving = true;
                            }
                            bool caseSensitiveAttribute;
                            if (element.Attribute("CaseSensitive") != null && !bool.TryParse(element.Attribute("CaseSensitive").Value, out caseSensitiveAttribute))
                            {
                                element.SetAttributeValue("CaseSensitive", "false");
                                requireSaving = true;
                            }
                            bool accentSensitiveAttribute;
                            if (element.Attribute("AccentSensitive") != null && !bool.TryParse(element.Attribute("AccentSensitive").Value, out accentSensitiveAttribute))
                            {
                                element.SetAttributeValue("AccentSensitive", "true");
                                requireSaving = true;
                            }
                        }

                        // Save changes
                        if (requireSaving)
                        {
                            xelement.Save(Routing.Constants.Config.ConfigFilePhysicalPath);
                        }

                    }
                    catch (Exception ex)
                    {
                        LogHelper.Error<ConfigController>(string.Format("Error accessing the config file : {0}", Routing.Constants.Config.ConfigFilePhysicalPath), ex);
                    }

                    return xelement;

                },
                // Add a cache dependency on the config file in order to refresh the config when the config file changes
                filenamesDependencies: new string[1] { Routing.Constants.Config.ConfigFilePhysicalPath });


            // Get the config settings and cache them
            Routing.Helpers.CacheHelper.GetExistingOrAddToCache(Routing.Constants.Cache.ConfigSettingsCacheId, 3600 * 24 * 365, System.Web.Caching.CacheItemPriority.NotRemovable,

                // Anonymous method that gets the config settings
                () =>
                {
                    Settings settings = new Settings();
                    try
                    {
                        // Get config
                        XElement xelement = Routing.Helpers.CacheHelper.Get(Routing.Constants.Cache.ConfigCacheId) as XElement;

                        // Get the config settings
                        XElement xelementSettings = xelement.Element("Settings");
                        if (xelementSettings != null)
                        {
                            if (xelementSettings.Element("RoutesExamineSearchProvider") != null && !string.IsNullOrWhiteSpace(xelementSettings.Element("RoutesExamineSearchProvider").Value))
                            {
                                settings.RoutesExamineSearchProvider = xelementSettings.Element("RoutesExamineSearchProvider").Value;
                            }
                            bool routesAreCaseSensitive;
                            if (xelementSettings.Element("RoutesAreCaseSensitive") != null && bool.TryParse(xelementSettings.Element("RoutesAreCaseSensitive").Value, out routesAreCaseSensitive))
                            {
                                settings.RoutesAreCaseSensitive = routesAreCaseSensitive;
                            }
                            bool routesAreAccentSensitive;
                            if (xelementSettings.Element("RoutesAreAccentSensitive") != null && bool.TryParse(xelementSettings.Element("RoutesAreAccentSensitive").Value, out routesAreAccentSensitive))
                            {
                                settings.RoutesAreAccentSensitive = routesAreAccentSensitive;
                            }
                            int cacheDurationInHours;
                            if (xelementSettings.Element("CacheDurationInHours") != null && int.TryParse(xelementSettings.Element("CacheDurationInHours").Value, out cacheDurationInHours))
                            {
                                settings.CacheDurationInHours = cacheDurationInHours;
                            }
                            if (xelementSettings.Element("PersistentCacheMapPath") != null && !string.IsNullOrWhiteSpace(xelementSettings.Element("PersistentCacheMapPath").Value))
                            {
                                settings.PersistentCacheMapPath = xelementSettings.Element("PersistentCacheMapPath").Value;
                            }
                            int persistentCacheUpdateFrequencyInMinutes;
                            if (xelementSettings.Element("PersistentCacheUpdateFrequencyInMinutes") != null && int.TryParse(xelementSettings.Element("PersistentCacheUpdateFrequencyInMinutes").Value, out persistentCacheUpdateFrequencyInMinutes))
                            {
                                settings.PersistentCacheUpdateFrequencyInMinutes = persistentCacheUpdateFrequencyInMinutes;
                            }
                            if (xelementSettings.Element("EncryptionKey") != null && !string.IsNullOrWhiteSpace(xelementSettings.Element("EncryptionKey").Value))
                            {
                                settings.EncryptionKey = xelementSettings.Element("EncryptionKey").Value;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Error<ConfigController>(string.Format("Error loading settings from the config file : {0}", Routing.Constants.Config.ConfigFilePhysicalPath), ex);
                    }

                    return settings;

                },
                // Add a cache dependency on the config file in order to refresh the config when the config file changes
                filenamesDependencies: new string[1] { Routing.Constants.Config.ConfigFilePhysicalPath });

            // Get routes and cache them
            Routing.Helpers.CacheHelper.GetExistingOrAddToCache(Routing.Constants.Cache.ConfigRoutesCacheId, 3600 * 24 * 365, System.Web.Caching.CacheItemPriority.NotRemovable,

                // Anonymous method that gets the routes
                () =>
                {
                    IEnumerable<Route> routes = null;
                    try
                    {

                        // Get config
                        XElement xelement = Routing.Helpers.CacheHelper.Get(Routing.Constants.Cache.ConfigCacheId) as XElement;

                        // Get settings
                        Settings settings = Routing.Helpers.CacheHelper.Get(Routing.Constants.Cache.ConfigSettingsCacheId) as Settings;

                        // Get routes
                        int counter = 0;
                        routes = (from route in xelement.Element("Routes").Elements("Route")
                                  select new Route()
                                  {
                                      Position = counter++,
                                      RouteSegmentSettings = route.Attribute("UrlSegments") != null ? route.Attribute("UrlSegments").Value : string.Empty,
                                      Enabled = Convert.ToBoolean(route.Attribute("Enabled") != null ? route.Attribute("Enabled").Value : "true"),
                                      DocumentTypeAlias = route.Attribute("DocumentTypeAlias") != null ? route.Attribute("DocumentTypeAlias").Value : string.Empty,
                                      PropertyAlias = route.Attribute("PropertyAlias") != null ? route.Attribute("PropertyAlias").Value : string.Empty,
                                      PropertyAliasExactMatch = route.Attribute("PropertyAliasExactMatch") != null ? route.Attribute("PropertyAliasExactMatch").Value : string.Empty,
                                      Template = route.Attribute("Template") != null ? route.Attribute("Template").Value : string.Empty,
                                      ForceTemplate = Convert.ToBoolean(route.Attribute("ForceTemplate") != null ? route.Attribute("ForceTemplate").Value : "false"),
                                      MatchNodeFullUrl = Convert.ToBoolean(route.Attribute("MatchNodeFullUrl") != null ? route.Attribute("MatchNodeFullUrl").Value : "false"),
                                      FallbackNodeId = route.Attribute("FallbackNodeId") != null ? route.Attribute("FallbackNodeId").Value : string.Empty,
                                      CaseSensitive = Convert.ToBoolean(route.Attribute("CaseSensitive") != null ? route.Attribute("CaseSensitive").Value : settings.RoutesAreCaseSensitive.ToString()),
                                      AccentSensitive = Convert.ToBoolean(route.Attribute("AccentSensitive") != null ? route.Attribute("AccentSensitive").Value : settings.RoutesAreAccentSensitive.ToString()),
                                      Description = route.Attribute("Description") != null ? route.Attribute("Description").Value : string.Empty
                                  }).ToList();

                        foreach (var route in routes)
                        {
                            // UrlSegmentSettings admits comma separated lists. Converts it into into a collection
                            route.RouteSegmentSettingsCollection = route.RouteSegmentSettings
                                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(x => new RouteSegmentSettings(x))
                                .ToList();
                        }
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Error<ConfigController>(string.Format("Error loading routes from the config file : {0}", Routing.Constants.Config.ConfigFilePhysicalPath), ex);
                    }

                    return routes ?? new List<Route>();

                },
                // Add a cache dependency on the config file in order to refresh the config when the config file changes
                filenamesDependencies: new string[1] { Routing.Constants.Config.ConfigFilePhysicalPath });
        }

        private XDocument LoadConfig()
        {
            var result = new XDocument();
            LoadAndCacheConfig(); // Creates a new config file if it doesn't exist
            try
            {
                result = XDocument.Load(Routing.Constants.Config.ConfigFilePhysicalPath);
            }
            catch (Exception ex)
            {
                LogHelper.Error<ConfigController>(string.Format("Error loading routes from the config file : {0}", Routing.Constants.Config.ConfigFilePhysicalPath), ex);
            }
            return result;
        }

        private string SaveConfig(XDocument config)
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
                LogHelper.Error<ConfigController>(string.Format("Error saving the config file : {0}", Routing.Constants.Config.ConfigFilePhysicalPath), ex);
            }
            return result;
        }

        private void DetectConfigFileChanges()
        {
            // Create a cache item that never expires and setup a callbackback method that will be called when the file is modified
            Routing.Helpers.CacheHelper.GetExistingOrAddToCache(Routing.Constants.Cache.ConfigDetectFileChangesCacheId, 3600 * 24 * 730, System.Web.Caching.CacheItemPriority.NotRemovable,
                () =>
                {
                    return 1;
                },
                filenamesDependencies: new[] { Routing.Constants.Config.ConfigFilePhysicalPath },
                onRemoveCallback: new System.Web.Caching.CacheItemRemovedCallback(ConfigFileChangesCacheCallback)
                );
        }

        private void ConfigFileChangesCacheCallback(string key, object value, System.Web.Caching.CacheItemRemovedReason reason)
        {
            DetectConfigFileChanges();

            // It only considers that the file was modified when the reason is 'DependencyChanged'. This is because the cache item could be removed for other reasons, among them when the AppDomain is restarted (web.config modified, ...)
            if (reason == CacheItemRemovedReason.DependencyChanged)
            {
                // Delete the persistent cache's file and clear the persistent cache stored in memory
                new PersistentCacheController().ResetPersistentCache();

                LogHelper.Warn<ConfigController>("Routing config file change detected.");
            }
        }



    }

}