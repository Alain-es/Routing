using Examine;
using Examine.LuceneEngine.SearchCriteria;
using Routing.Controllers;
using Routing.Extensions;
using Routing.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
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
        private static readonly bool IsLoggingPerformanceDataEnabled = (ConfigurationManager.AppSettings["Routing.IsLoggingPerformanceDataEnabled"] ?? string.Empty).Equals("true", StringComparison.CurrentCultureIgnoreCase);

        public bool TryFindContent(PublishedContentRequest contentRequest)
        {
            Stopwatch stopwatch = null;
            string cacheAvailability = string.Empty;
            string requestUrl = VirtualPathUtility.AppendTrailingSlash(contentRequest.Uri.GetAbsolutePathDecoded());

            try
            {
                if (IsLoggingPerformanceDataEnabled)
                {
                    stopwatch = new Stopwatch();
                    stopwatch.Start();
                }

                // Get settings
                Settings settings = _ConfigController.getSettings();

                // Check whether the result is cached
                string cacheId = string.Format(Constants.Cache.RequestUrlCacheIdPattern, requestUrl);
                var urlContentNodeCached = Helpers.CacheHelper.Get(cacheId) as UrlContentNode;
                cacheAvailability = "Route already available in Memory Cache";
                if (urlContentNodeCached == null)
                {
                    // Check whether the result is in the persistent cache
                    urlContentNodeCached = _PersistentCacheController.Get(requestUrl);
                    cacheAvailability = "Route already available in Persistent Cache";
                }
                if (urlContentNodeCached != null)
                {
                    // Check whether the content node id is 0 what means that no content node was found for this url
                    if (urlContentNodeCached.NodeId == 0)
                    {
                        return false;
                    }

                    contentRequest.PublishedContent = UmbracoContext.Current.ContentCache.GetById(urlContentNodeCached.NodeId);

                    // Check whether the published content is not null (it has happened some times)
                    if (contentRequest.PublishedContent != null)
                    {
                        SetTemplate(contentRequest, urlContentNodeCached.Template, urlContentNodeCached.ForceTemplate);

                        // Indicates that a content node was found
                        return true;
                    }
                }

                cacheAvailability = "Route not available in Cache.";

                // Get active routes from the cache or from the config if they are not already cached
                IEnumerable<Route> routes = null;
                routes = Helpers.CacheHelper.GetExistingOrAddToCache(Constants.Cache.ConfigRoutesEnabledCacheId, settings.CacheDurationInHours * 3600, CacheItemPriority.NotRemovable,
                    // Anonymous method that gets the enabled routes
                    () =>
                    {
                        return _ConfigController.getRoutes()
                        .Where(r => r.Enabled)
                        .OrderBy(x => x.Position)
                        .ToList();
                    }
                    , filenamesDependencies: new string[1] { Constants.Config.ConfigFilePhysicalPath }) as IEnumerable<Route>;

                // If there are no routes then exit
                if (!routes.Any())
                {
                    return false;
                }

                // Split the request url into segements
                var requestUrlSegments = contentRequest.Uri.GetAbsolutePathDecoded().Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var route in routes)
                {
                    // Try to find the content node using the property/properties specified
                    if (!string.IsNullOrWhiteSpace(route.PropertyAlias) || !string.IsNullOrWhiteSpace(route.PropertyAliasExactMatch))
                    {
                        // Go through all urlSegments defined for the route that contains the {PropertyValue} identifier
                        foreach (var routeSegmentSettings in route.RouteSegmentSettingsCollection.Where(x => x.PositionOfPropertyValueSegment > 0))
                        {
                            // Check whether the request url contains a segment to compare with the property value
                            var requestUrlPropertyValueSegment = (requestUrlSegments.Length >= routeSegmentSettings.PositionOfPropertyValueSegment) ? requestUrlSegments.Skip(routeSegmentSettings.PositionOfPropertyValueSegment - 1).FirstOrDefault() : string.Empty;
                            if (!string.IsNullOrWhiteSpace(requestUrlPropertyValueSegment))
                            {
                                // Create a test route to compare with the request's route in order to see if the UrlSegments match
                                var testRoute = VirtualPathUtility.AppendTrailingSlash(Regex.Replace(routeSegmentSettings.UrlSegments, @"\{PropertyValue\}", requestUrlPropertyValueSegment));
                                if (UrlMatch(testRoute, requestUrl))
                                {
                                    // Use Examine to find the content node
                                    Examine.SearchCriteria.ISearchCriteria criteria;
                                    Examine.SearchCriteria.IBooleanOperation filter;

                                    // Try to find a content node that contains a property (PropertyAliasExactMatch) whith value matches the PropertyValue url's segment
                                    if (!string.IsNullOrWhiteSpace(route.PropertyAliasExactMatch))
                                    {
                                        IPublishedContent result = null;

                                        // Search
                                        criteria = ExamineManager.Instance.SearchProviderCollection[settings.RoutesExamineSearchProvider].CreateSearchCriteria(UmbracoExamine.IndexTypes.Content);
                                        string searchValue = string.Format(@"""{0}""", requestUrlPropertyValueSegment);
                                        switch (route.PropertyAliasExactMatch.ToLower())
                                        {
                                            case "id":
                                                filter = criteria.Field("id", searchValue);
                                                break;

                                            case "encryptedId":
                                                filter = criteria.Field("id", Helpers.EncryptionHelper.DecryptAES(searchValue, settings.EncryptionKey));
                                                break;

                                            case "name":
                                                filter = criteria.GroupedOr(new List<string>() { "nodeName", "urlName" }, searchValue);
                                                break;

                                            default:
                                                filter = criteria.Field(route.PropertyAliasExactMatch, searchValue);
                                                break;
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
                                            // Check only the PropertyValue segment of the url
                                            else
                                            {
                                                switch (route.PropertyAliasExactMatch.ToLower())
                                                {
                                                    case "id":
                                                    case "encryptedId":
                                                        result = results.FirstOrDefault(x => x.Id.ToString().TrimStart("/").TrimEnd("/").Equals(requestUrlPropertyValueSegment, route.CaseSensitive, route.AccentSensitive));
                                                        break;

                                                    case "name":
                                                        result = results.FirstOrDefault(x => x.Name.TrimStart("/").TrimEnd("/").Equals(requestUrlPropertyValueSegment, route.CaseSensitive, route.AccentSensitive));
                                                        break;

                                                    default:
                                                        result = results.FirstOrDefault(x => x.GetPropertyValue<string>(route.PropertyAliasExactMatch).TrimStart("/").TrimEnd("/").Equals(requestUrlPropertyValueSegment, route.CaseSensitive, route.AccentSensitive));
                                                        break;
                                                }
                                            }

                                            if (result != null)
                                            {
                                                contentRequest.PublishedContent = result;
                                                SetTemplate(contentRequest, route.Template, route.ForceTemplate);
                                                AddToCacheUrlContentNode(cacheId, settings.CacheDurationInHours * 3600, requestUrl, result.Id, route.Template, route.ForceTemplate);
                                                // Indicates that a content node was found
                                                return true;
                                            }
                                        }
                                    }

                                    // Try to find a content node that contains a property (PropertyAlias) whith value converted to url matches the PropertyValue url's segment
                                    if (!string.IsNullOrWhiteSpace(route.PropertyAlias))
                                    {
                                        IPublishedContent result = null;

                                        // Search with wildcards
                                        criteria = ExamineManager.Instance.SearchProviderCollection[settings.RoutesExamineSearchProvider].CreateSearchCriteria(UmbracoExamine.IndexTypes.Content);
                                        string searchValue = Regex.Replace(requestUrlPropertyValueSegment, @"[^A-Za-z0-9]+", "*");
                                        switch (route.PropertyAlias.ToLower())
                                        {
                                            case "id":
                                                filter = criteria.Field("id", searchValue.MultipleCharacterWildcard());
                                                break;

                                            case "encryptedId":
                                                filter = criteria.Field("id", Helpers.EncryptionHelper.DecryptAES(searchValue, settings.EncryptionKey).MultipleCharacterWildcard());
                                                break;

                                            case "name":
                                                filter = criteria.GroupedOr(new List<string>() { "nodeName", "urlName" }, searchValue.MultipleCharacterWildcard());
                                                break;

                                            default:
                                                filter = criteria.Field(route.PropertyAlias, searchValue.MultipleCharacterWildcard());
                                                break;
                                        }
                                        if (!string.IsNullOrWhiteSpace(route.DocumentTypeAlias))
                                        {
                                            filter = filter.And().GroupedOr(new List<string>() { "nodeTypeAlias" }, route.DocumentTypeAlias.Split(','));
                                        }
                                        var results = _UmbracoHelper.TypedSearch(filter.Compile()).ToList();

                                        // Search without wildcards
                                        criteria = ExamineManager.Instance.SearchProviderCollection[settings.RoutesExamineSearchProvider].CreateSearchCriteria(UmbracoExamine.IndexTypes.Content);
                                        searchValue = Regex.Replace(requestUrlPropertyValueSegment, @"[^A-Za-z0-9]+", " ");
                                        switch (route.PropertyAlias.ToLower())
                                        {
                                            case "id":
                                                filter = criteria.Field("id", searchValue);
                                                break;

                                            case "encryptedId":
                                                filter = criteria.Field("id", Helpers.EncryptionHelper.DecryptAES(searchValue, settings.EncryptionKey));
                                                break;

                                            case "name":
                                                filter = criteria.GroupedOr(new List<string>() { "nodeName", "urlName" }, searchValue);
                                                break;

                                            default:
                                                filter = criteria.Field(route.PropertyAlias, searchValue);
                                                break;
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

                                            // Check only the PropertyValue segment of the url
                                            else
                                            {
                                                switch (route.PropertyAlias.ToLower())
                                                {
                                                    case "id":
                                                    case "encryptedId":
                                                        result = results.DistinctBy(x => x.Id).FirstOrDefault(x => x.Id.ToString().ToUrlSegment(contentRequest.Culture).Equals(requestUrlPropertyValueSegment, route.CaseSensitive, route.AccentSensitive));
                                                        break;

                                                    case "name":
                                                        result = results.DistinctBy(x => x.Id).FirstOrDefault(x => x.Name.ToUrlSegment(contentRequest.Culture).Equals(requestUrlPropertyValueSegment, route.CaseSensitive, route.AccentSensitive));
                                                        break;

                                                    default:
                                                        result = results.DistinctBy(x => x.Id).FirstOrDefault(x => x.GetPropertyValue<string>(route.PropertyAlias).ToUrlSegment(contentRequest.Culture).Equals(requestUrlPropertyValueSegment, route.CaseSensitive, route.AccentSensitive));
                                                        break;
                                                }
                                            }

                                            if (result != null)
                                            {
                                                contentRequest.PublishedContent = result;
                                                SetTemplate(contentRequest, route.Template, route.ForceTemplate);
                                                AddToCacheUrlContentNode(cacheId, settings.CacheDurationInHours * 3600, requestUrl, result.Id, route.Template, route.ForceTemplate);
                                                // Indicates that a content node was found
                                                return true;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // Fallback node, what means that no content node was found or that the PropertyAlias/PropertyAliasExactMatch settings are  empty for this route
                    if (!string.IsNullOrWhiteSpace(route.FallbackNodeId))
                    {
                        // Check whether the request url matches with one of the urlSegments defined in the route
                        foreach (var routeSegmentSetting in route.RouteSegmentSettingsCollection.Where(x => requestUrl.InvariantEquals(x.UrlSegments)))
                        {
                            int nodeId;
                            if (int.TryParse(route.FallbackNodeId, out nodeId))
                            {
                                contentRequest.PublishedContent = UmbracoContext.Current.ContentCache.GetById(nodeId);
                                SetTemplate(contentRequest, route.Template, route.ForceTemplate);
                                AddToCacheUrlContentNode(cacheId, settings.CacheDurationInHours * 3600, requestUrl, nodeId, route.Template, route.ForceTemplate);
                                // Indicates that a content node was found
                                return true;
                            }
                        }
                    }
                }

                // No content node was found after processing all rules, but the request url has to be cached in order to avoid processing all the rules again
                // This cached item will be removed if any content node is updated (deleted, saved, created, ...). This is achieved using the nodeId 0.
                // There is also a cache dependency on the config file.
                AddToCacheUrlContentNode(cacheId, settings.CacheDurationInHours * 3600, requestUrl, 0, string.Empty, false);
            }
            finally
            {
                if (IsLoggingPerformanceDataEnabled)
                {
                    stopwatch.Stop();
                    LogHelper.Info<CustomContentFinder>(() => string.Format("Elapsed time: {0} -- {1} -- {2}", stopwatch.Elapsed.ToString(), cacheAvailability, requestUrl));
                }
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
                Match match = Regex.Match(url, urlPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
                if (match.Success)
                {
                    //string matchedValue = match.Groups[1].Value;
                    result = true;
                }
            }
            return result;
        }

        private void SetTemplate(PublishedContentRequest contentRequest, string template, bool forceTemplate)
        {
            if (contentRequest.TemplateAlias == null || string.IsNullOrWhiteSpace(contentRequest.TemplateAlias) || forceTemplate)
            {
                if (!string.IsNullOrWhiteSpace(template))
                {
                    var isValidTemplate = System.IO.File.Exists(HttpContext.Current.Server.MapPath(template));
                    if (!isValidTemplate)
                    {
                        return;
                    }

                    // Check whether it as an alias or a path
                    if (!template.Contains("/"))
                    {
                        contentRequest.TrySetTemplate(template);
                    }
                    else
                    {
                        contentRequest.SetTemplate(new Template(template, template, Guid.NewGuid().ToString().Replace("-", string.Empty)));
                    }

                    var requestUrl = contentRequest.Uri.OriginalString;

                    // Add the template to the cache in order to retrieve it from the Render MVC controller
                    var cacheId = string.Format(Constants.Cache.TemplateCacheIdPattern, requestUrl.ToLower().Trim());
                    string cacheDependencyId = GetNodeCacheDependency(contentRequest.PublishedContent.Id);
                    Helpers.CacheHelper.GetExistingOrAddToCacheSlidingExpiration(cacheId, 300, CacheItemPriority.NotRemovable,
                        () =>
                        {
                            return template;
                        },
                        new[] { Constants.Config.ConfigFilePhysicalPath }, new[] { cacheDependencyId });
                }
            }
        }

        private void AddToCacheUrlContentNode(string cacheId, int cacheSeconds, string requestUrl, int nodeId, string template, bool forceTemplate)
        {
            // Add to the cache
            string cacheDependencyId = GetNodeCacheDependency(nodeId);
            var urlContentNode = new UrlContentNode() { Url = requestUrl, NodeId = nodeId, Template = template, ForceTemplate = forceTemplate };
            Helpers.CacheHelper.GetExistingOrAddToCache(cacheId, cacheSeconds, CacheItemPriority.High,
                () =>
                {
                    return urlContentNode;
                },
                new[] { Constants.Config.ConfigFilePhysicalPath }, new[] { cacheDependencyId });

            // Add to the persistent cache
            _PersistentCacheController.Add(urlContentNode);
        }

        private string GetNodeCacheDependency(int contentNodeId)
        {
            // Create a cache dependency in order to remove cached items where the content node is modified (saved, deleted, trashed, ...)
            // The cache priority is set to NotRemovable because this cache dependency will be associated with items cached with a NotRemovable priority
            Settings settings = _ConfigController.getSettings();
            var cacheDurationSeconds = settings.CacheDurationInHours * 3600;
            string cacheDependencyId = string.Format(Constants.Cache.NodeDependencyCacheIdPattern, contentNodeId);
            Helpers.CacheHelper.GetExistingOrAddToCache(cacheDependencyId, cacheDurationSeconds, CacheItemPriority.NotRemovable,
                () =>
                {
                    return 1;
                },
                new[] { Constants.Config.ConfigFilePhysicalPath });
            return cacheDependencyId;
        }
    }
}