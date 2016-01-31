using System.Collections.Generic;

namespace Routing.Models
{
    public class Route
    {
        public int Position { get; set; }

        // [Optional]
        // Admits comma separated lists of UrlSegments strings
        public string RouteSegmentSettings { get; set; }

        // [Optional]
        // Deafault value: true
        public bool Enabled { get; set; }

        // [Optional]
        // If left empty it will match any document type
        public string DocumentTypeAlias { get; set; }

        // [Optional]
        // Try to match the specified segment of the current browser's url with the value stored in the node's property speficied (will ignore some url valid characters like spaces, dashes, ...)
        // If left empty this feature is ignored
        public string PropertyAlias { get; set; }

        // [Optional]
        // Try to match the speficied segment of the current browser's url with the value stored in the node's property speficied (exact match)
        // If left empty this feature is ignored
        public string PropertyAliasExactMatch { get; set; }

        // [Optional]
        // Deafault value: false
        public bool MatchNodeFullUrl { get; set; }

        // [Optional]
        public string Template { get; set; }

        // [Optional]
        // Deafault value: false
        public bool ForceTemplate { get; set; }

        // [Optional]
        // If left empty this feature is ignored
        public string FallbackNodeId { get; set; }

        // [Optional]
        // Deafault value: false
        public bool CaseSensitive { get; set; }

        // [Optional]
        // Deafault value: true
        public bool AccentSensitive { get; set; }

        // [Optional]
        public string Description { get; set; }

        // [Internal]
        // All the UrlSegmentSettings splited into a collection
        public List<RouteSegmentSettings> RouteSegmentSettingsCollection { get; set; }
    }
}
