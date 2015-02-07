using System.Web.Mvc;
using System.Web.Caching;

using Umbraco.Web.Models;
using Umbraco.Web.Mvc;


namespace Routing.Controllers
{

    public class RoutingController : RenderMvcController
    {

        public override ActionResult Index(RenderModel model)
        {
            // Retrieve the template from the cache
            string cacheId = string.Format(Routing.Constants.Cache.TemplateCacheIdPattern, model.Content.Id);
            string template = Request.RequestContext.HttpContext.Cache.Get(cacheId) as string;
            if (template != null)
            {
                return View(template, model);
            }

            // Default template
            return base.Index(model);
        }

    }
}
