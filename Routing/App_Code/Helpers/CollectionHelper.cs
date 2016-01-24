using System.Collections.Generic;
using System.Xml.Linq;


namespace Routing.Helpers
{
    public class CollectionHelper
    {
        public static XDocument SerializeListToXDocument<T>(List<T> list)
        {
            XDocument xDocument = new XDocument();
            System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(list.GetType());
            using (System.Xml.XmlWriter writer = xDocument.CreateWriter())
            {
                serializer.Serialize(writer, list);
                writer.Close();
            }
            return xDocument;
        }

        public static string SerializeListToString<T>(List<T> list)
        {
            return SerializeListToXDocument<T>(list).ToString();
        }

        public static void SerializeListToFile<T>(List<T> list, string filePath)
        {
            // Make sure that the directory exists
            var directory = System.IO.Path.GetDirectoryName(filePath);
            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }

            SerializeListToXDocument<T>(list).Save(filePath);
        }

        public static List<T> DeserializeListFromXDocument<T>(XDocument xDocument)
        {
            List<T> result = new List<T>();
            System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(List<T>));
            using (System.Xml.XmlReader reader = xDocument.CreateReader())
            {
                result = (List<T>)serializer.Deserialize(reader);
                reader.Close();
            }
            return result;
        }

        public static List<T> DeserializeListFromString<T>(string xml)
        {
            List<T> result = null;
            result = DeserializeListFromXDocument<T>(XDocument.Parse(xml));
            return result ?? new List<T>();
        }

        public static List<T> DeserializeListFromFile<T>(string filePath)
        {
            List<T> result = null;
            if (System.IO.File.Exists(filePath))
            {
                result = DeserializeListFromXDocument<T>(XDocument.Load(filePath));
            }
            return result ?? new List<T>();
        }


    }
}