
namespace Routing.Constants
{
    public class Config
    {
        private static readonly string ConfigFilePath = "~/Config/Routing.config";
        public static readonly string ConfigFilePhysicalPath = System.Web.Hosting.HostingEnvironment.MapPath(ConfigFilePath);
    }

    public class Cache
    {
        public static readonly string TemplateCacheIdPattern = "Routing.CacheId.ContentTemplate{0}";
        public static readonly string RoutesConfigCacheId = "Routing.CacheId.RoutesConfig";
    }

}