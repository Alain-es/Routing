using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Routing.Models
{
    public class Route
    {
        public string UrlSegments { get; set; }
        public bool Enabled { get; set; }
    }
}
