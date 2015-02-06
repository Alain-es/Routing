﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

using Umbraco.Core;
using Umbraco.Core.Services;
using Umbraco.Core.Persistence;
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

            // Insert our own ContentFinder (before the ContentFinderByNiceUrl)
            ContentFinderResolver.Current.InsertTypeBefore<ContentFinderByNiceUrl, CustomContentFinder>();
        }

        public void OnApplicationStarted(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {
            // Register routes for package's embedded files
            RouteConfig.RegisterRoutes(RouteTable.Routes);
        }
    }

}
