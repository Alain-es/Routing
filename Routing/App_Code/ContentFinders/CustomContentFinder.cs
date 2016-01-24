using Examine;
using Examine.LuceneEngine.SearchCriteria;
using Routing.Controllers;
using Routing.Extensions;
using Routing.Helpers;
using Routing.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Caching;
using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Web;
using Umbraco.Web.Routing;


namespace Routing.ContentFinders
{

    public class CustomContentFinder : IContentFinder
    {
        private static UmbracoHelper _UmbracoHelper = new UmbracoHelper(UmbracoContext.Current);
        private static ConfigController _ConfigController = new ConfigController();
        private static PersistentCacheController _PersistentCacheController = new PersistentCacheController();

        public bool TryFindContent(PublishedContentRequest contentRequest)
        {

#if DEBUG
            Stopwatch stopwatch = new Stopwatch();
#endif
            string requestUrl = VirtualPathUtility.AppendTrailingSlash(contentRequest.Uri.GetAbsolutePathDecoded());

            // Get settings
            Settings settings = _ConfigController.getSettings();

            // Check whether the result is cached
            string cacheId = string.Format(Routing.Constants.Cache.RequestUrlCacheIdPattern, requestUrl);
            var urlContentNodeCached = Routing.Helpers.CacheHelper.Get(cacheId) as UrlContentNode;
            if (urlContentNodeCached == null)
            {
                // Check whether the result is in the persistent cache
                urlContentNodeCached = _PersistentCacheController.Get(requestUrl);
            }
            if (urlContentNodeCached != null)
            {
                contentRequest.PublishedContent = UmbracoContext.Current.ContentCache.GetById(urlContentNodeCached.NodeId);

                // Check whether the published content is not null since it has happened a some times
                if (contentRequest.PublishedContent != null)
            {
                    SetTemplate(contentRequest, urlContentNodeCached.Template, urlContentNodeCached.ForceTemplate, settings.CacheDurationInHours * 3600);
                    // Indicates that a content node was found 
                return true;
            }
            }

#if DEBUG
                stopwatch.Start();
#endif

            // Get enabled routes from the cache or from the config if they are not already cached
            IEnumerable<Route> routes = null;
            routes = Routing.Helpers.CacheHelper.GetExistingOrAddToCache(Routing.Constants.Cache.ConfigRoutesEnabledCacheId, settings.CacheDurationInHours * 3600, System.Web.Caching.CacheItemPriority.NotRemovable,
                // Anonymous method that gets the enabled routes
                () =>
                {
                    return _ConfigController.getRoutes().Where(r => r.Enabled).OrderBy(x => x.Position);
                }
                , filenamesDependencies: new string[1] { Routing.Constants.Config.ConfigFilePhysicalPath }) as IEnumerable<Route>;

            // If there are no routes then exit
            if (!routes.Any())
            {
                return false;
            }

            try
            {

                // Split the request url into segements
                var requestUrlSegments = contentRequest.Uri.GetAbsolutePathDecoded().Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                var requestLastSegment = (requestUrlSegments.Length > 0) ? requestUrlSegments.Last() : string.Empty;

                foreach (var route in routes)
                {
                    // Try to find the content node using the property/properties specified
                    if (!string.IsNullOrWhiteSpace(requestLastSegment) && (!string.IsNullOrWhiteSpace(route.PropertyAlias) || !string.IsNullOrWhiteSpace(route.PropertyAliasExactMatch)))
                    {
                        // Urlsegments config file attribute admits comma separated lists of UrlSegments
                        foreach (var routeUrlSegments in route.UrlSegments.Split(','))
                        {
                            // Create a test route to compare with the request's route in order to see if the UrlSegments match
                            var testRoute = VirtualPathUtility.AppendTrailingSlash(string.Concat(routeUrlSegments.Trim(), requestLastSegment));
                            if (UrlMatch(testRoute, requestUrl))
                            {
                                // Use Examine to find the content node
                                Examine.SearchCriteria.ISearchCriteria criteria;
                                Examine.SearchCriteria.IBooleanOperation filter;

                                // Try to find a content node that contains a property (PropertyAliasExactMatch) whith value matches the last url's segment
                                if (!string.IsNullOrWhiteSpace(route.PropertyAliasExactMatch))
                                {
                                    IPublishedContent result = null;

                                    // Search
                                    criteria = ExamineManager.Instance.SearchProviderCollection[settings.RoutesExamineSearchProvider].CreateSearchCriteria(UmbracoExamine.IndexTypes.Content);
                                    string searchValue = string.Format(@"""{0}""", requestLastSegment);
                                    if (route.PropertyAliasExactMatch.InvariantEquals("name"))
                                {
                                        filter = criteria.GroupedOr(new List<string>() { "nodeName", "urlName" }, searchValue);
                                }
                                else
                                {
                                        filter = criteria.Field(route.PropertyAliasExactMatch, searchValue);
                                }
                                if (!string.IsNullOrWhiteSpace(route.DocumentTypeAlias))
                                {
                                    filter = filter.And().GroupedOr(new List<string>() { "nodeTypeAlias" }, route.DocumentTypeAlias.Split(','));
                                }
                                    var results = _UmbracoHelper.TypedSearch(filter.Compile()).ToList();

                                    if (results.Any())
                                    {
                                        // Check the full url 
                                        if (route.MatchNodeFullUrl)
                                {
                                            result = results.FirstOrDefault(x => x.Url().Equals(requestUrl, route.CaseSensitive, route.AccentSensitive));
                                        }
                                        // Check the last segment of the url
                                        else
                                        {
                                            if (route.PropertyAliasExactMatch.InvariantEquals("name"))
                                            {
                                                result = results.FirstOrDefault(x => x.Name.TrimStart("/").TrimEnd("/").Equals(requestLastSegment, route.CaseSensitive, route.AccentSensitive));
                                            }
                                            else
                                            {
                                                result = results.FirstOrDefault(x => x.GetPropertyValue<string>(route.PropertyAliasExactMatch).TrimStart("/").TrimEnd("/").Equals(requestLastSegment, route.CaseSensitive, route.AccentSensitive));
                                            }
                                        }

                                        if (result != null)
                                        {
                                            contentRequest.PublishedContent = result;
                                            SetTemplate(contentRequest, route.Template, route.ForceTemplate, settings.CacheDurationInHours * 3600);
                                            AddToCacheUrlContentNode(cacheId, settings.CacheDurationInHours * 3600, requestUrl, result.Id, route.Template, route.ForceTemplate);
                                            // Indicates that a content node was found 
                                            return true;
                                        }
                                    }

                                }

                                // Try to find a content node that contains a property (PropertyAlias) whith value converted to url matches the last url's segment
                                if (!string.IsNullOrWhiteSpace(route.PropertyAlias))
                                {
                                    IPublishedContent result = null;

                                    // Search with wildcards
                                    criteria = ExamineManager.Instance.SearchProviderCollection[settings.RoutesExamineSearchProvider].CreateSearchCriteria(UmbracoExamine.IndexTypes.Content);
                                    string searchValue = Regex.Replace(requestLastSegment, @"[^A-Za-z0-9]+", "*");
                                    if (route.PropertyAlias.InvariantEquals("name"))
                                    {
                                        filter = criteria.GroupedOr(new List<string>() { "nodeName", "urlName" }, searchValue.MultipleCharacterWildcard());
                                    }
                                    else
                                        {
                                        filter = criteria.Field(route.PropertyAlias, searchValue.MultipleCharacterWildcard());
                                        }
                                    if (!string.IsNullOrWhiteSpace(route.DocumentTypeAlias))
                                    {
                                        filter = filter.And().GroupedOr(new List<string>() { "nodeTypeAlias" }, route.DocumentTypeAlias.Split(','));
                                    }
                                    var results = _UmbracoHelper.TypedSearch(filter.Compile()).ToList();

                                    // Search without wildcards
                                    criteria = ExamineManager.Instance.SearchProviderCollection[settings.RoutesExamineSearchProvider].CreateSearchCriteria(UmbracoExamine.IndexTypes.Content);
                                    searchValue = Regex.Replace(requestLastSegment, @"[^A-Za-z0-9]+", " ");
                                    if (route.PropertyAlias.InvariantEquals("name"))
                                    {
                                        filter = criteria.GroupedOr(new List<string>() { "nodeName", "urlName" }, searchValue);
                                    }
                                    else
                                    {
                                        filter = criteria.Field(route.PropertyAlias, searchValue);
                                    }
                                    if (!string.IsNullOrWhiteSpace(route.DocumentTypeAlias))
                                    {
                                        filter = filter.And().GroupedOr(new List<string>() { "nodeTypeAlias" }, route.DocumentTypeAlias.Split(','));
                                    }
                                    results.AddRange(_UmbracoHelper.TypedSearch(filter.Compile()).ToList());

                                    if (results.Any())
                                        {
                                        // Check the full url
                                        if (route.MatchNodeFullUrl)
                                        {
                                            result = results.DistinctBy(x => x.Id).FirstOrDefault(x => x.Url().Equals(requestUrl, route.CaseSensitive, route.AccentSensitive));
                                        }

                                        // Check the last segment of the url
                                        else
                                        {
                                            if (route.PropertyAlias.InvariantEquals("name"))
                                            {
                                                result = results.DistinctBy(x => x.Id).FirstOrDefault(x => x.Name.ToUrlSegment(contentRequest.Culture).Equals(requestLastSegment, route.CaseSensitive, route.AccentSensitive));
                                            }
                                            else
                                            {
                                                result = results.DistinctBy(x => x.Id).FirstOrDefault(x => x.GetPropertyValue<string>(route.PropertyAlias).ToUrlSegment(contentRequest.Culture).Equals(requestLastSegment, route.CaseSensitive, route.AccentSensitive));
                                    }
                                        }

                                        if (result != null)
                                    {
                                        contentRequest.PublishedContent = result;
                                            SetTemplate(contentRequest, route.Template, route.ForceTemplate, settings.CacheDurationInHours * 3600);
                                            AddToCacheUrlContentNode(cacheId, settings.CacheDurationInHours * 3600, requestUrl, result.Id, route.Template, route.ForceTemplate);
                                            // Indicates that a content node was found 
                                        return true;
                                    }
                                    }

                                }
                            }
                        }
                    }

                    // Fallback node, what means no content node was found or the property alias is empty
                    if (!string.IsNullOrWhiteSpace(route.FallbackNodeId))
                    {
                        // Urlsegments config file attribute admits comma separated lists of UrlSegments
                        foreach (var routeUrlSegments in route.UrlSegments.Split(',').Where(x => requestUrl.InvariantEquals(x.Trim())))
                        {
                                int nodeId;
                                if (int.TryParse(route.FallbackNodeId, out nodeId))
                                {
                                    contentRequest.PublishedContent = UmbracoContext.Current.ContentCache.GetById(nodeId);
                                SetTemplate(contentRequest, route.Template, route.ForceTemplate, settings.CacheDurationInHours * 3600);
                                AddToCacheUrlContentNode(cacheId, settings.CacheDurationInHours * 3600, requestUrl, nodeId, route.Template, route.ForceTemplate);
                                // Indicates that a content node was found 
                                    return true;
                                }
                            }
                        }
                    }

            }
            finally
            {
#if DEBUG
                stopwatch.Stop();
                LogHelper.Info<CustomContentFinder>(() => string.Format("Elapsed time: {0}  {1}", stopwatch.Elapsed.ToString(), requestUrl));
#endif
            }
            // If no content node was found then return false in order to run the next contentFinder in pipeline
            return false;
        }

        private bool UrlMatch(string urlPattern, string url)
        {
            bool result = false;

            if (!urlPattern.Contains("*"))
            {
                result = url.InvariantEquals(urlPattern);
            }
            else
            {
                urlPattern = urlPattern.Replace("*", "([^/]*)");
                Match match = Regex.Match(url, urlPattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    //string matchedValue = match.Groups[1].Value;
                    result = true;
                }
            }
            return result;
        }


        private void SetTemplate(PublishedContentRequest contentRequest, string template, bool forceTemplate, int cacheSeconds)
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

                    // Create cache dependency in order to remove the cached item every time the related content node is modified (saved, deleted, trashed, ...)
                    string nodeCacheId = string.Format(Routing.Constants.Cache.NodeDependencyCacheIdPattern, contentRequest.PublishedContent.Id);
                    Routing.Helpers.CacheHelper.GetExistingOrAddToCache(nodeCacheId, cacheSeconds, System.Web.Caching.CacheItemPriority.NotRemovable,
                        () =>
                        {
                            return 1;
                        },
                        new[] { Routing.Constants.Config.ConfigFilePhysicalPath });

                    // Add the template to the cache in order to retrieve it from the Render MVC controller
                    string cacheId = string.Format(Routing.Constants.Cache.TemplateCacheIdPattern, contentRequest.PublishedContent.Id);
                    Routing.Helpers.CacheHelper.GetExistingOrAddToCacheSlidingExpiration(cacheId, 600, System.Web.Caching.CacheItemPriority.NotRemovable,
                        () =>
                        {
                            return template;
                        },
                        new[] { Routing.Constants.Config.ConfigFilePhysicalPath }, new[] { nodeCacheId });
                }
            }
        }

        private void AddToCacheUrlContentNode(string cacheId, int cacheSeconds, string requestUrl, int nodeId, string template, bool forceTemplate)
        {
            // Create cache dependency in order to remove the cached item every time the related content node is modified (saved, deleted, trashed, ...)
            string nodeCacheId = string.Format(Routing.Constants.Cache.NodeDependencyCacheIdPattern, nodeId);
            Routing.Helpers.CacheHelper.GetExistingOrAddToCache(nodeCacheId, cacheSeconds, System.Web.Caching.CacheItemPriority.NotRemovable,
                 () =>
                 {
                     return 1;
                 },
                 new[] { Routing.Constants.Config.ConfigFilePhysicalPath });

            // Add to the cache
            var urlContentNode = new UrlContentNode() { Url = requestUrl, NodeId = nodeId, Template = template, ForceTemplate = forceTemplate };
            Routing.Helpers.CacheHelper.GetExistingOrAddToCache(cacheId, cacheSeconds, System.Web.Caching.CacheItemPriority.High,
                () =>
                {
                    return urlContentNode;
                },
                new[] { Routing.Constants.Config.ConfigFilePhysicalPath }, new[] { nodeCacheId });

            // Add to the persistent cache
            _PersistentCacheController.Add(urlContentNode);
        }

    }

}