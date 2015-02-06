using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Routing.Models
{
    public class Route
    {
        public int Position { get; set; }

        public string UrlSegments { get; set; }

        public bool Enabled { get; set; }

        public string DocumentTypeAlias { get; set; }

        public string PropertyAlias { get; set; }

        public string Template { get; set; }
        public bool ForceTemplate { get; set; }

        public string FallbackNodeId { get; set; }

        public string Description { get; set; }
    }
}
