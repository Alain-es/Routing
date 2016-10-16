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
                string requestUrl = VirtualPathUtility.AppendTrailingSlash(request.Url.AbsolutePath);
                string cacheId = string.Format(Routing.Constants.Cache.TemplateCacheIdPattern, requestUrl.ToLower());
                string template = Routing.Helpers.CacheHelper.Get(cacheId) as string;
                if (template != null)
                {
                    return View(template, model);
                }
            }

            // Default template
            return base.Index(model);
        }
    }
}
