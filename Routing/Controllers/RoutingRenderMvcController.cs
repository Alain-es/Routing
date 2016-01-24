using System.Web.Caching;
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
            string cacheId = string.Format(Routing.Constants.Cache.TemplateCacheIdPattern, model.Content.Id);
            string template = Routing.Helpers.CacheHelper.Get(cacheId) as string;
            if (template != null)
            {
                return View(template, model);
            }

            // Default template
            return base.Index(model);
        }
    }
}
