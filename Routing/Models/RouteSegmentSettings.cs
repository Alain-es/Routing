using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Umbraco.Core;


namespace Routing.Models
{
    public class RouteSegmentSettings
    {
        public string UrlSegments { get; set; }

        // Collection of segments
        public List<string> UrlSegmentsCollection { get; set; }

        // Position of the segment that will be replaced by the property value in order to match the url
        // This segment is identified by the identifier: {PropertyValue}
        public int PositionOfPropertyValueSegment { get; set; }

        // Constructor
        public RouteSegmentSettings(string urlSegments)
        {
            UrlSegments = VirtualPathUtility.AppendTrailingSlash(urlSegments.Trim());

            // Converts the url segments string  into a collection
            UrlSegmentsCollection = UrlSegments.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries).ToList();

            // Position of the segment to compare with the property value
            PositionOfPropertyValueSegment = UrlSegmentsCollection.Select((value, index) => new { value, index })
                  .Where(x => x.value.InvariantEquals("{PropertyValue}"))
                  .Select(x => x.index + 1)
                  .FirstOrDefault();
        }
    }
}
