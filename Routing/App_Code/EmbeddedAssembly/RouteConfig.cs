using System.Web.Mvc;
using System.Web.Routing;


namespace Routing.EmbeddedAssembly
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {

            const string pluginBasePath = "App_Plugins/Routing";
            string url = string.Empty;

            RouteTable.Routes.MapRoute(
                name: "Routing.GetResourcePath0",
                url: pluginBasePath + "/{resource}",
                defaults: new
                {
                    controller = "EmbeddedResource",
                    action = "GetResourcePath0"
                },
                namespaces: new[] { "Routing.EmbeddedAssembly" }
            );

            RouteTable.Routes.MapRoute(
                name: "Routing.GetResourcePath1",
                url: pluginBasePath + "/{directory1}/{resource}",
                defaults: new
                {
                    controller = "EmbeddedResource",
                    action = "GetResourcePath1"
                },
                namespaces: new[] { "Routing.EmbeddedAssembly" }
            );

            RouteTable.Routes.MapRoute(
                name: "Routing.GetResourcePath2",
                url: pluginBasePath + "/{directory1}/{directory2}/{resource}",
                defaults: new
                {
                    controller = "EmbeddedResource",
                    action = "GetResourcePath2"
                },
                namespaces: new[] { "Routing.EmbeddedAssembly" }
            );

        }
    }
}