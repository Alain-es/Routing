using Routing.Helpers;
using Routing.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Core.Logging;


namespace Routing.Controllers
{
    public class PersistentCacheController
    {
        private static ConfigController _ConfigController = new ConfigController();
        private static ConcurrentDictionary<string, UrlContentNode> _PersistentCache = null;

        public void LoadPersistentCache(bool forceReload = false)
        {
            // Check whether the persistent cache file will be updated. If not so then clear the persistent cache and remove the file.
            var persistentCacheUpdateFrequencyInMinutes = _ConfigController.getSettings().PersistentCacheUpdateFrequencyInMinutes;
            if (persistentCacheUpdateFrequencyInMinutes <= 0)
            {
                RemovePersistentCacheFile();
                _PersistentCache = new ConcurrentDictionary<string, UrlContentNode>();
            }
            else
            {
                if (_PersistentCache == null || forceReload)
                {
                    try
                    {
                        // Get the path from the settings (so if it has changed it will get the updated value)
                        var persistentCachePath = System.Web.Hosting.HostingEnvironment.MapPath(_ConfigController.getSettings().PersistentCacheMapPath);

                        // Deserialize the file's contents
                        _PersistentCache = SerializationHelper.DeserializeFromFile(persistentCachePath) as ConcurrentDictionary<string, UrlContentNode>;
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Error<PersistentCacheController>(string.Format("Error in the method LoadPersistentCache(forceReload={0})", forceReload), ex);
                    }
                }
            }

            if (_PersistentCache == null)
            {
                _PersistentCache = new ConcurrentDictionary<string, UrlContentNode>();
            }
        }

        public void SavePersistentCache()
        {
            if (_PersistentCache == null)
            {
                _PersistentCache = new ConcurrentDictionary<string, UrlContentNode>();
            }

            try
            {
                // Get the path from the settings (so if it has changed it will get the updated value)
                var persistentCachePath = System.Web.Hosting.HostingEnvironment.MapPath(_ConfigController.getSettings().PersistentCacheMapPath);

                // Serialize the contents
                SerializationHelper.SerializeToFile(_PersistentCache, persistentCachePath);
            }
            catch (Exception ex)
            {
                LogHelper.Error<PersistentCacheController>("Error in the method SavePersistentCache()", ex);
            }
        }

        public void RemovePersistentCacheFile()
        {
            try
            {
                // Setup the cache persistent update process in order to make sure that the file will be recreated
                SetupPersistentCacheUpdateProcess();

                // Get the path from the settings (so if it has changed it will get the updated value)
                var persistentCachePath = System.Web.Hosting.HostingEnvironment.MapPath(_ConfigController.getSettings().PersistentCacheMapPath);

                // Remove the file
                if (System.IO.File.Exists(persistentCachePath))
                {
                    System.IO.File.Delete(persistentCachePath);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error<PersistentCacheController>("Error in the method RemovePersistentCacheFile()", ex);
            }
        }

        public void ResetPersistentCache()
        {
            // Setup the cache persistent update process in order to make sure that the file will be recreated
            SetupPersistentCacheUpdateProcess();

            // Clear and save
            _PersistentCache = new ConcurrentDictionary<string, UrlContentNode>();
            SavePersistentCache();
        }

        public UrlContentNode Get(string url)
        {
            if (_PersistentCache == null)
            {
                LoadPersistentCache();
            }

            return _PersistentCache.ContainsKey(url) ? _PersistentCache[url] : null;
        }

        public void Remove(int nodeId, bool removePersistentCacheFile = false)
        {
            if (_PersistentCache == null)
            {
                LoadPersistentCache();
            }

            if (_PersistentCache.Any())
            {
                // Remove the items from the memory collection
                var itemsToRemove = _PersistentCache.Where(x => x.Value.NodeId == nodeId);
                int numRemovedItems = 0;
                foreach (var item in itemsToRemove)
                {
                    // Try to remove the item
                    int numAttempts = 0;
                    UrlContentNode removedValue = null;
                    while (!_PersistentCache.TryRemove(item.Key, out removedValue) && numAttempts < 10)
                    {
                        numAttempts++;
                        System.Threading.Thread.Sleep(5);
                    }
                    //Check whether the item was removed successfully
                    if (numAttempts < 10)
                    {
                        numRemovedItems++;
                    }
                    else
                    {
                        // Reset the persitent cache in order to avoid inconsistencies.
                        ResetPersistentCache();
                        LogHelper.Warn<PersistentCacheController>(string.Format("Couldn't remove a UrlContentNode from the Routing persistent cache. In order to avoid inconsistencies the persitent cache was reset."));
                        return;
                    }
                }

                // Delete the file only if the memory collection has changed
                if (removePersistentCacheFile && numRemovedItems > 0)
                {
                    RemovePersistentCacheFile();
                }
            }
        }

        public void Add(UrlContentNode urlContentNode)
        {
            if (_PersistentCache == null)
            {
                LoadPersistentCache();
            }

            // Setup the cache persistent update process in order to make sure that the urlContentNode that is going to be added/updated will be saved
            SetupPersistentCacheUpdateProcess();

            // Add/Update the persistent cache
            _PersistentCache.AddOrUpdate(urlContentNode.Url, urlContentNode, (key, oldValue) =>
            {
                oldValue.Url = urlContentNode.Url;
                oldValue.NodeId = urlContentNode.NodeId;
                oldValue.Template = urlContentNode.Template;
                oldValue.ForceTemplate = urlContentNode.ForceTemplate;
                return oldValue;
            });
        }

        private void SetupPersistentCacheUpdateProcess()
        {
            // Check whether the file will be saved or not. If not so there is no point to setup the update process.
            var persistentCacheUpdateFrequencyInMinutes = _ConfigController.getSettings().PersistentCacheUpdateFrequencyInMinutes;
            if (persistentCacheUpdateFrequencyInMinutes > 0)
            {
                // Create a cache item that expires in xx minutes (PersistentCacheUpdateFrequencyInMinutes setting) and setup a callbackback method that will update the persistent cache
                // There are no cachedependencies
                Routing.Helpers.CacheHelper.GetExistingOrAddToCache(Routing.Constants.Cache.PersistentCacheSavingTimeoutCacheId, persistentCacheUpdateFrequencyInMinutes * 60, System.Web.Caching.CacheItemPriority.NotRemovable,
                    () =>
                    {
                        return 1;
                    },
                    onRemoveCallback: new System.Web.Caching.CacheItemRemovedCallback(UpdatePersistentCacheCallback)
                    );
            }
        }

        private void UpdatePersistentCacheCallback(string key, object value, System.Web.Caching.CacheItemRemovedReason reason)
        {
            SetupPersistentCacheUpdateProcess();

            // It only updates the persistent cache file when the reason is 'Expired'. This is because the cache item could be removed for other reasons, among them when the AppDomain is restarted (web.config modified, ...)
            if (reason == System.Web.Caching.CacheItemRemovedReason.Expired)
            {
                SavePersistentCache();
#if DEBUG
                LogHelper.Info<PersistentCacheController>("Routing persistent cache saved");
#endif
            }
        }


    }
}