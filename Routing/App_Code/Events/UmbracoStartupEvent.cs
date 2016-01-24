using Routing.ContentFinders;
using Routing.EmbeddedAssembly;
using System.Web;
using System.Web.Routing;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using Umbraco.Web.Mvc;
using Umbraco.Web.Routing;


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
            new Controllers.ConfigController().LoadAndCacheConfig();

            // Set default controller for all routes
            DefaultRenderMvcControllerResolver.Current.SetDefaultControllerType(typeof(Routing.Controllers.RoutingRenderMvcController));

            // Insert our own ContentFinder (before the ContentFinderByNiceUrl)
            ContentFinderResolver.Current.InsertTypeBefore<ContentFinderByNiceUrl, CustomContentFinder>();
        }

        public void OnApplicationStarted(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {
            //// Register routes for package's embedded files
            //RouteConfig.RegisterRoutes(RouteTable.Routes);

            // Events
            ContentService.Saved += ContentService_Saved;
            ContentService.Deleted += ContentService_Deleted;
            ContentService.Trashed += ContentService_Trashed;
            ContentService.Moved += ContentService_Moved;
            ContentService.RolledBack += ContentService_RolledBack;
            ContentService.Copied += ContentService_Copied;
        }

        protected void ContentService_Saved(IContentService sender, Umbraco.Core.Events.SaveEventArgs<IContent> e)
        {
            // Remove any cache associated to the modified content nodes
            var persistentCacheController = new Controllers.PersistentCacheController();
            foreach (var entity in e.SavedEntities)
            {
                Routing.Helpers.CacheHelper.Remove(string.Format(Routing.Constants.Cache.NodeDependencyCacheIdPattern, entity.Id));
                persistentCacheController.Remove(entity.Id, true);
            }
        }

        void ContentService_Copied(IContentService sender, Umbraco.Core.Events.CopyEventArgs<IContent> e)
        {
            // Remove any cache associated to the modified content node
            Routing.Helpers.CacheHelper.Remove(string.Format(Routing.Constants.Cache.NodeDependencyCacheIdPattern, e.Copy.Id));
            new Controllers.PersistentCacheController().Remove(e.Copy.Id, true);
        }

        void ContentService_RolledBack(IContentService sender, Umbraco.Core.Events.RollbackEventArgs<IContent> e)
        {
            // Remove any cache associated to the modified content nodes
            Routing.Helpers.CacheHelper.Remove(string.Format(Routing.Constants.Cache.NodeDependencyCacheIdPattern, e.Entity.Id));
            new Controllers.PersistentCacheController().Remove(e.Entity.Id, true);
        }

        void ContentService_Moved(IContentService sender, Umbraco.Core.Events.MoveEventArgs<IContent> e)
        {
            // Remove any cache associated to the modified content nodes
            Routing.Helpers.CacheHelper.Remove(string.Format(Routing.Constants.Cache.NodeDependencyCacheIdPattern, e.Entity.Id));
            new Controllers.PersistentCacheController().Remove(e.Entity.Id, true);
        }

        void ContentService_Trashed(IContentService sender, Umbraco.Core.Events.MoveEventArgs<IContent> e)
        {
            // Remove any cache associated to the modified content nodes
            Routing.Helpers.CacheHelper.Remove(string.Format(Routing.Constants.Cache.NodeDependencyCacheIdPattern, e.Entity.Id));
            new Controllers.PersistentCacheController().Remove(e.Entity.Id, true);
        }

        void ContentService_Deleted(IContentService sender, Umbraco.Core.Events.DeleteEventArgs<IContent> e)
        {
            // Remove any cache associated to the modified content nodes
            var persistentCacheController = new Controllers.PersistentCacheController();
            foreach (var entity in e.DeletedEntities)
            {
                Routing.Helpers.CacheHelper.Remove(string.Format(Routing.Constants.Cache.NodeDependencyCacheIdPattern, entity.Id));
                persistentCacheController.Remove(entity.Id, true);
            }
        }

    }

}
