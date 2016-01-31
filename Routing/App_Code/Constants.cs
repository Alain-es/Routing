namespace Routing.Constants
{
    public class Config
    {
        private static readonly string ConfigFilePath = "~/Config/Routing.config";
        public static readonly string ConfigFilePhysicalPath = System.Web.Hosting.HostingEnvironment.MapPath(ConfigFilePath);
    }

    public class Cache
    {
        public static readonly string RequestUrlCacheIdPattern = "Routing.CacheId.RequestUrl{0}";
        public static readonly string NodeDependencyCacheIdPattern = "Routing.CacheId.NodeDependency{0}";

        public static readonly string TemplateCacheIdPattern = "Routing.CacheId.ContentTemplate{0}";

        public static readonly string ConfigCacheId = "Routing.CacheId.Config";
        public static readonly string ConfigSettingsCacheId = "Routing.CacheId.ConfigSettings";
        public static readonly string ConfigRoutesCacheId = "Routing.CacheId.ConfigRoutes";
        public static readonly string ConfigRoutesEnabledCacheId = "Routing.CacheId.ConfigRoutesEnabled";
        public static readonly string ConfigDetectFileChangesCacheId = "Routing.CacheId.ConfigDetectFileChanges";

        public static readonly string PersistentCacheSavingTimeoutCacheId = "Routing.CacheId.PersistentCacheSavingTimeout";
    }

}