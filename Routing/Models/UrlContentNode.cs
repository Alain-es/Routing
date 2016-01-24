using System;


namespace Routing.Models
{
    [Serializable]
    public class UrlContentNode
    {
        public string Url { get; set; }
        public int NodeId { get; set; }
        public string Template { get; set; }
        public bool ForceTemplate { get; set; }
    }
}