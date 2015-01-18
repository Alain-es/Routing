using System;
using System.Web;
using System.Web.Mvc;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Dynamic;
using System.ComponentModel;
using System.Text;
using System.IO;

using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Xml.XPath;

using System.Web.Http;
using System.Net.Http;
using System.Web.Hosting;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

using Umbraco.Web.Editors;
using Umbraco.Web.Mvc;
using Umbraco.Web.WebApi;
using Umbraco.Core.Logging;

using Routing.Helpers;

namespace Routing.Controllers
{
    [PluginController("Routing")]
    [IsBackOffice]
    public class RoutingApiController : UmbracoAuthorizedJsonController
    {
        [System.Web.Http.HttpGet]
        public string GetRoutes()
        {
            string result = string.Empty;

            // Load
            XDocument xDocument = ConfigFileHelper.LoadConfig();

            try
            {
                // Convert document attributes into elements
                foreach (XElement xElement in xDocument.Descendants())
                {
                    foreach (var attribute in xElement.Attributes())
                    {
                        xElement.SetElementValue(attribute.Name, attribute.Value);
                    }
                    xElement.RemoveAttributes();
                }
                // Serialize
                result = JsonConvert.SerializeXNode(xDocument.XPathSelectElement("//Routes"));

            }
            catch (Exception ex)
            {
                LogHelper.Error(typeof(RoutingApiController), "Error serializing routes.", ex);
            }

            return result;
        }

        [System.Web.Http.HttpPost]
        public string SaveRoutes([FromBody] string paramValues)
        {
            string result = "Unexpected error.";

            // Check whether parameters have a value
            if (paramValues == null)
            {
                result = "Parameters are null.";
                return result;
            }

            // Get the values 
            try
            {
                // Deserialize Xml
                var xDocument = XDocument.Parse(JsonConvert.DeserializeXmlNode(paramValues, "Routes").OuterXml);

                // Convert document elements into attributes
                IEnumerable<XElement> routes = from element in xDocument.Descendants("Route")
                                               select element;
                foreach (XElement route in routes)
                {
                    foreach (XElement element in route.Descendants())
                    {
                        route.SetAttributeValue(element.Name, element.Value);
                    }
                    route.RemoveNodes();
                }

                // Save 
                result = ConfigFileHelper.SaveConfig(xDocument);
            }
            catch (Exception ex)
            {
                LogHelper.Error(typeof(RoutingApiController), "Error deserializing routes.", ex);
                result = string.Format("Error deserializing routes: {0}", ex.Message);
                return result;
            }

            return result;
        }

    }
}


