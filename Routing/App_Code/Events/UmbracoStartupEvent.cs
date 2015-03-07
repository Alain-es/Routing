using System.Web;
using System.Web.Routing;

using Umbraco.Core;
using Umbraco.Core.Services;
using Umbraco.Core.Models;
using Umbraco.Web.Mvc;
using Umbraco.Web.Routing;

using Routing.EmbeddedAssembly;
using Routing.ContentFinders;


namespace Routing.Events
{
    public class UmbracoStartupEvent : IApplicationEventHandler
    {

        public void OnApplicationInitialized(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {
        }

        public void OnApplicationStarting(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {
            // Load routes and create config file if it doesn't exist
            Helpers.ConfigFileHelper.LoadAndCacheConfig();

            // Set default controller for all routes
            DefaultRenderMvcControllerResolver.Current.SetDefaultControllerType(typeof(Routing.Controllers.RoutingController));

            // Insert our own ContentFinder (before the ContentFinderByNiceUrl)
            ContentFinderResolver.Current.InsertTypeBefore<ContentFinderByNiceUrl, CustomContentFinder>();
        }

        public void OnApplicationStarted(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {
            //// Register routes for package's embedded files
            //RouteConfig.RegisterRoutes(RouteTable.Routes);

            ContentService.Saved += ContentService_Saved;
        }

        /// <summary>
        ///  This event is fired when a content is created, edited, published, moved, ...
        /// </summary>
        void ContentService_Saved(IContentService sender, Umbraco.Core.Events.SaveEventArgs<IContent> e)
        {
            // Clear the cache for the modified content
            foreach (var entity in e.SavedEntities)
            {
                string NodeCacheId = string.Format(Routing.Constants.Cache.NodeCacheIdPattern, entity.Id);
                HttpContext.Current.Cache.Remove(NodeCacheId);
            }
        }

    }

}
