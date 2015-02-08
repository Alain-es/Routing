using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Caching;
using System.Text.RegularExpressions;

using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Web;
using Umbraco.Web.Routing;
using Examine;

using Routing.Helpers;
using Routing.Models;


namespace Routing.ContentFinders
{
    public class CustomContentFinder : IContentFinder
    {

        public bool TryFindContent(PublishedContentRequest contentRequest)
        {
            // TODO: cache the results to improve the performance

            List<Route> routes = new List<Route>();
            UmbracoHelper umbracoHelper = new UmbracoHelper(contentRequest.RoutingContext.UmbracoContext);

            // Load routes
            routes = ConfigFileHelper.getRoutes().ToList<Route>();

            // Split the request url into segements
            string requestUrl = string.Concat(contentRequest.Uri.GetAbsolutePathDecoded(), "/");
            var requestUrlSegments = contentRequest.Uri.GetAbsolutePathDecoded().Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            var requestLastSegment = (requestUrlSegments.Length > 0) ? requestUrlSegments.Last() : string.Empty;

            foreach (var route in routes)
            {
                if (!route.Enabled)
                    continue;

                // Create a test route to compare with the request's route in order to see if the UrlSegments match
                var testRoute = string.Concat(route.UrlSegments, requestLastSegment, "/");
                if (requestUrl.InvariantEquals(testRoute))
                {
                    // Use Examine to find the content node
                    var criteria = ExamineManager.Instance.DefaultSearchProvider.CreateSearchCriteria("content");
                    string searchValue = Regex.Replace(requestLastSegment, @"[^A-Za-z0-9]+", "*");
                    Examine.SearchCriteria.IBooleanOperation filter;
                    if (string.IsNullOrWhiteSpace(route.PropertyAlias) || route.PropertyAlias.InvariantEquals("name"))
                    {

                        filter = criteria.NodeName(searchValue);
                    }
                    else
                    {
                        filter = criteria.Field(route.PropertyAlias, searchValue);
                    }
                    if (!string.IsNullOrWhiteSpace(route.DocumentTypeAlias))
                    {
                        filter = filter.And().NodeTypeAlias(route.DocumentTypeAlias);
                    }
                    var results = umbracoHelper.TypedSearch(filter.Compile()).ToArray();
                    bool found = false;
                    foreach (var result in results)
                    {
                        if (string.IsNullOrWhiteSpace(route.PropertyAlias) || route.PropertyAlias.InvariantEquals("name"))
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
                            // Needs to set a fake template in case the content doesn't have one in order to avoid an exception
                            SetTemplate(contentRequest, route.Template, route.ForceTemplate);
                            // Indicate that a content was found 
                            return true;
                        }

                    }
                }
                // Fallback node 
                if (requestUrl.InvariantEquals(route.UrlSegments) && !string.IsNullOrWhiteSpace(route.FallbackNodeId))
                {
                    int nodeId;
                    if (int.TryParse(route.FallbackNodeId, out nodeId))
                    {
                        contentRequest.PublishedContent = UmbracoContext.Current.ContentCache.GetById(nodeId);
                        SetTemplate(contentRequest, route.Template, route.ForceTemplate);
                        // Indicate that a content was found 
                        return true;
                    }
                }
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
                    string cacheId = string.Format(Routing.Constants.Cache.TemplateCacheIdPattern, contentRequest.PublishedContent.Id);
                    HttpContext.Current.Cache.Add(cacheId, template, new CacheDependency(Routing.Constants.Config.ConfigFilePhysicalPath), DateTime.Now.AddMinutes(5), Cache.NoSlidingExpiration, System.Web.Caching.CacheItemPriority.NotRemovable, null);
                }
            }

        }

    }

}