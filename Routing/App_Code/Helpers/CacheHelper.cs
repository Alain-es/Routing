using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Caching;


namespace Routing.Helpers
{
    public class CacheHelper
    {
        private static Cache _cache = null;
        public static Cache cache
        {
            get
            {
                if (_cache == null && HttpContext.Current != null)
                {
                    _cache = HttpContext.Current.Cache;
                }
                return _cache;
            }
        }

        public static object Get(string key)
        {
            return cache.Get(key);
        }

        public static T Get<T>(string key, T defaultValue = default (T))
        {
            object result = Get(key);

            if (result == null)
            {
                return defaultValue;
            }

            if (result is T)
            {
                return (T)result;
            }

            try
            {
                return (T)Convert.ChangeType(result, typeof(T));
            }
            catch (InvalidCastException)
            {
                return defaultValue;
            }
        }

        public static void AddToCache(string key, object value, int cacheSeconds, CacheItemPriority cacheItemPriority = CacheItemPriority.Default, string[] filenamesDependencies = null, string[] cacheKeysDependencies = null, CacheItemRemovedCallback onRemoveCallback = null)
        {
            if (!string.IsNullOrWhiteSpace(key) && value != null && cacheSeconds > 0)
            {
                cache.Add(key, value, new CacheDependency(filenamesDependencies, cacheKeysDependencies), DateTime.Now.AddSeconds(cacheSeconds), Cache.NoSlidingExpiration, cacheItemPriority, onRemoveCallback);
            }
        }

        public static void AddToCacheSlidingExpiration(string key, object value, int slidingExpirationSeconds, CacheItemPriority cacheItemPriority = CacheItemPriority.Default, string[] filenamesDependencies = null, string[] cacheKeysDependencies = null, CacheItemRemovedCallback onRemoveCallback = null)
        {
            if (!string.IsNullOrWhiteSpace(key) && value != null && slidingExpirationSeconds > 0)
            {
                cache.Add(key, value, new CacheDependency(filenamesDependencies, cacheKeysDependencies), Cache.NoAbsoluteExpiration, new TimeSpan(0, 0, 0, slidingExpirationSeconds), cacheItemPriority, onRemoveCallback);
            }
        }

        public static object GetExistingOrAddToCache(string key, int cacheSeconds, CacheItemPriority cacheItemPriority = CacheItemPriority.Default, Func<object> getContentToCache = null, string[] filenamesDependencies = null, string[] cacheKeysDependencies = null, CacheItemRemovedCallback onRemoveCallback = null)
        {
            object cachedObject = Get(key);
            if (cachedObject != null)
            {
                return cachedObject;
            }

            lock (string.Intern("b87a73ec-4947-4847-96fa-cedffb6f977a-" + key))
            {
                cachedObject = Get(key);
                if (cachedObject != null)
                {
                    return cachedObject;
                }

                if (getContentToCache != null)
                {
                    object objectToCache = getContentToCache();
                    if (objectToCache != null && cacheSeconds > 0)
                    {
                        AddToCache(key, objectToCache, cacheSeconds, cacheItemPriority, filenamesDependencies, cacheKeysDependencies, onRemoveCallback);
                    }
                    return objectToCache;
                }

                return null;
            }
        }

        public static object GetExistingOrAddToCacheSlidingExpiration(string key, int slidingExpirationSeconds, CacheItemPriority cacheItemPriority = CacheItemPriority.Default, Func<object> getContentToCache = null, string[] filenamesDependencies = null, string[] cacheKeysDependencies = null, CacheItemRemovedCallback onRemoveCallback = null)
        {
            object cachedObject = Get(key);
            if (cachedObject != null)
            {
                return cachedObject;
            }

            lock (string.Intern("233816dc-570c-4e4b-a605-ac1210782bc5-" + key))
            {
                cachedObject = Get(key);
                if (cachedObject != null)
                {
                    return cachedObject;
                }

                if (getContentToCache != null)
                {
                    object objectToCache = getContentToCache();
                    if (objectToCache != null && slidingExpirationSeconds > 0)
                    {
                        AddToCacheSlidingExpiration(key, objectToCache, slidingExpirationSeconds, cacheItemPriority, filenamesDependencies, cacheKeysDependencies, onRemoveCallback);
                    }
                    return objectToCache;
                }

                return null;
            }
        }

        public static void Remove(string key)
        {
            cache.Remove(key);
        }

    }
}