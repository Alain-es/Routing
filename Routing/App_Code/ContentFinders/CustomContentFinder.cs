using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Caching;
using System.Text.RegularExpressions;
using System.Diagnostics;

using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Logging;
using Umbraco.Web;
using Umbraco.Web.Routing;
using Examine;
using Examine.LuceneEngine.SearchCriteria;

using Routing.Helpers;
using Routing.Models;


namespace Routing.ContentFinders
{
    public class ContentFoundAndCached
    {
        public int NodeId { get; set; }
        public string Template { get; set; }
        public bool ForceTemplate { get; set; }
    }

    public class CustomContentFinder : IContentFinder
    {
        private const string _SearchProvider = "ExternalSearcher";

        public bool TryFindContent(PublishedContentRequest contentRequest)
        {
            Stopwatch stopwatch = new Stopwatch();

            // Load routes from config
            List<Route> routes = ConfigFileHelper.getRoutes().ToList<Route>();

            // If there are no routes in the config then exit
            if (!routes.Any())
                return false;

            string requestUrl = VirtualPathUtility.AppendTrailingSlash(contentRequest.Uri.GetAbsolutePathDecoded());

            // Check whether it is cached
            string cacheId = string.Format(Routing.Constants.Cache.RequestUrlCacheIdPattern, requestUrl);
            var contentFound = HttpContext.Current.Cache[cacheId] as ContentFoundAndCached;

            if (contentFound != null)
            {
                contentRequest.PublishedContent = UmbracoContext.Current.ContentCache.GetById(contentFound.NodeId);
                SetTemplate(contentRequest, contentFound.Template, contentFound.ForceTemplate);
                return true;
            }

            try
            {
#if DEBUG
                stopwatch.Start();
#endif

            // Split the request url into segements
            var requestUrlSegments = contentRequest.Uri.GetAbsolutePathDecoded().Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            var requestLastSegment = (requestUrlSegments.Length > 0) ? requestUrlSegments.Last() : string.Empty;

            foreach (var route in routes)
            {
                if (!route.Enabled)
                    continue;

                // Try to find the content node using the property specified
                if (!string.IsNullOrWhiteSpace(requestLastSegment) && !string.IsNullOrWhiteSpace(route.PropertyAlias))
                {
                    // Create a test route to compare with the request's route in order to see if the UrlSegments match
                    var testRoute = VirtualPathUtility.AppendTrailingSlash(string.Concat(route.UrlSegments, requestLastSegment));
                    if (requestUrl.InvariantEquals(testRoute))
                    {
                        // Use Examine to find the content node
                            var criteria = ExamineManager.Instance.SearchProviderCollection[_SearchProvider].CreateSearchCriteria(UmbracoExamine.IndexTypes.Content);
                        string searchValue = Regex.Replace(requestLastSegment, @"[^A-Za-z0-9]+", "*");
                        Examine.SearchCriteria.IBooleanOperation filter;
                        if (route.PropertyAlias.InvariantEquals("name"))
                        {
                                filter = criteria.GroupedOr(new List<string>() { "nodeName", "urlName" }, searchValue.MultipleCharacterWildcard(), requestLastSegment.MultipleCharacterWildcard());
                        }
                        else
                        {
                            filter = criteria.Field(route.PropertyAlias, searchValue);
                        }
                        if (!string.IsNullOrWhiteSpace(route.DocumentTypeAlias))
                        {
                            filter = filter.And().NodeTypeAlias(route.DocumentTypeAlias);
                        }
                        UmbracoHelper umbracoHelper = new UmbracoHelper(contentRequest.RoutingContext.UmbracoContext);
                            //var searchResults = ExamineManager.Instance.SearchProviderCollection[_SearchProvider].Search(filter.Compile());
                        var results = umbracoHelper.TypedSearch(filter.Compile()).ToArray();
                        bool found = false;
                        foreach (var result in results)
                        {
                            if (route.PropertyAlias.InvariantEquals("name"))
                            {
                                if (result.Name.ToUrlSegment(contentRequest.Culture).InvariantEquals(requestLastSegment))
                                {
                                    found = true;
                                }
                            }
                            else
                            {
                                if (result.GetPropertyValue<string>(route.PropertyAlias).ToUrlSegment(contentRequest.Culture).InvariantEquals(requestLastSegment))
                                {
                                    found = true;
                                }

                            }
                            if (found)
                            {
                                contentRequest.PublishedContent = result;
                                SetTemplate(contentRequest, route.Template, route.ForceTemplate);
                                CacheFoundContent(cacheId, result.Id, route.Template, route.ForceTemplate);
                                // Indicate that a content was found 
                                return true;
                            }

                        }
                    }
                }

                // Fallback node, what means no content was found or the property alias is empty
                if (requestUrl.InvariantEquals(route.UrlSegments) && !string.IsNullOrWhiteSpace(route.FallbackNodeId))
                {
                    int nodeId;
                    if (int.TryParse(route.FallbackNodeId, out nodeId))
                    {
                        contentRequest.PublishedContent = UmbracoContext.Current.ContentCache.GetById(nodeId);
                        SetTemplate(contentRequest, route.Template, route.ForceTemplate);
                        CacheFoundContent(cacheId, nodeId, route.Template, route.ForceTemplate);
                        // Indicate that a content was found 
                        return true;
                    }
                }
            }

            }
            finally
            {
#if DEBUG
                stopwatch.Stop();
                LogHelper.Info<CustomContentFinder>(() => string.Format("Elapsed time: {0}", stopwatch.Elapsed.ToString()));
#endif
            }
            // If no content was found then return false in order to run the next contentFinder in pipeline
            return false;
        }

        private void SetTemplate(PublishedContentRequest contentRequest, string template, bool forceTemplate)
        {
            if (contentRequest.TemplateAlias == null || string.IsNullOrWhiteSpace(contentRequest.TemplateAlias) || forceTemplate)
            {
                if (!string.IsNullOrWhiteSpace(template))
                {
                    // TODO: Use a regular expression to check if it as an alias or a path
                    if (!template.Contains("/"))
                    {
                        contentRequest.TrySetTemplate(template);
                    }
                    else
                    {
                        contentRequest.SetTemplate(new Template(template, template, Guid.NewGuid().ToString().Replace("-", string.Empty)));
                    }
                    // Cache the template in order to retrieve it from the Render MVC controller
                    CacheDependency fileDependency = new CacheDependency(Routing.Constants.Config.ConfigFilePhysicalPath);
                    HttpContext.Current.Cache.Add(Routing.Constants.Cache.EverythingCacheId, 0, null, DateTime.Now.AddDays(1), Cache.NoSlidingExpiration, System.Web.Caching.CacheItemPriority.NotRemovable, null);
                    string[] keyDependencies = { Routing.Constants.Cache.EverythingCacheId };
                    CacheDependency keyDependency = new CacheDependency(null, keyDependencies);
                    AggregateCacheDependency aggregateCacheDependency = new AggregateCacheDependency();
                    aggregateCacheDependency.Add(fileDependency);
                    aggregateCacheDependency.Add(keyDependency);
                    string cacheId = string.Format(Routing.Constants.Cache.TemplateCacheIdPattern, contentRequest.PublishedContent.Id);
                    HttpContext.Current.Cache.Add(cacheId, template, aggregateCacheDependency, DateTime.Now.AddMinutes(5), Cache.NoSlidingExpiration, System.Web.Caching.CacheItemPriority.NotRemovable, null);
                }
            }
        }

        private void CacheFoundContent(string cacheId, int nodeId, string template, bool forceTemplate)
        {
            var foundContent = new ContentFoundAndCached() { NodeId = nodeId, Template = template, ForceTemplate = forceTemplate };
            CacheDependency fileDependency = new CacheDependency(Routing.Constants.Config.ConfigFilePhysicalPath);
            HttpContext.Current.Cache.Add(Routing.Constants.Cache.EverythingCacheId, 0, null, DateTime.Now.AddDays(1), Cache.NoSlidingExpiration, System.Web.Caching.CacheItemPriority.NotRemovable, null);
            string[] keyDependencies = { Routing.Constants.Cache.EverythingCacheId };
            CacheDependency keyDependency = new CacheDependency(null, keyDependencies);
            AggregateCacheDependency aggregateCacheDependency = new AggregateCacheDependency();
            aggregateCacheDependency.Add(fileDependency);
            aggregateCacheDependency.Add(keyDependency);
            HttpContext.Current.Cache.Add(cacheId, foundContent, aggregateCacheDependency, DateTime.Now.AddDays(1), Cache.NoSlidingExpiration, System.Web.Caching.CacheItemPriority.High, null);
        }

    }

}