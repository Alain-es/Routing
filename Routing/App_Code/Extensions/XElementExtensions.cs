using System.Linq;
using System.Xml.Linq;

namespace Routing.Extensions
{

    public static class XElementExtensions
    {

        public static XElement ElementCaseInsensitive(this XElement self, XName name)
        {
            return self.Elements().FirstOrDefault(x => string.Compare(x.Name.LocalName, name.LocalName, true) == 0);
        }

        public static XAttribute AttributeCaseInsensitive(this XElement self, XName name)
        {
            return self.Attributes().FirstOrDefault(x => string.Compare(x.Name.LocalName, name.LocalName, true) == 0);
        }

        public static void SetAttributeValueCaseInsensitive(this XElement self, XName name, object value)
        {
            var attribute = self.Attributes().FirstOrDefault(x => string.Compare(x.Name.LocalName, name.LocalName, true) == 0);
            if (attribute != null)
            {
                self.SetAttributeValue(attribute.Name.LocalName, value);
            }
            else
            {
                self.SetAttributeValue(name, value);
            }
        }

    }

}