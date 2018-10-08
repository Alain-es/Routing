using System;
using System.Web;
using System.Web.Mvc;
using Umbraco.Web.Models;
using Umbraco.Web.Mvc;

namespace Routing.Controllers
{
    public class RoutingRenderMvcController : RenderMvcController
    {
        public override ActionResult Index(RenderModel model)
        {
            // Retrieve the template from the cache
            HttpRequestBase request = null;
            try
            {
                if (HttpContext != null && HttpContext.Request != null)
                {
                    request = HttpContext.Request;
                }
            }
            catch (Exception) { }
            if (request != null)
            {
                var requestUrl = ((RouteDefinition)ControllerContext.RouteData.DataTokens["umbraco-route-def"]).PublishedContentRequest.Uri.OriginalString;
                var cacheId = string.Format(Routing.Constants.Cache.TemplateCacheIdPattern, requestUrl.ToLower().Trim());
                var template = Routing.Helpers.CacheHelper.Get(cacheId) as string;
                if (!string.IsNullOrWhiteSpace(template))
                {
                    var isValidTemplate = System.IO.File.Exists(Server.MapPath(template));
                    if (isValidTemplate)
                {
                    return View(template, model);
                    }
                }
            }

            // Default template
            return base.Index(model);
        }
    }
}