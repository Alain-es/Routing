using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Umbraco.Core.Logging;


// Taken from: https://msdn.microsoft.com/en-us/library/system.runtime.serialization.formatters.binary.binaryformatter%28v=vs.100%29.aspx

namespace Routing.Helpers
{
    public class SerializationHelper
    {

        public static string SerializeToString(object obj)
        {
            string result = string.Empty;
            try
            {
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    BinaryFormatter binaryFormatter = new BinaryFormatter();
                    binaryFormatter.Serialize(memoryStream, obj);
                    byte[] data = memoryStream.ToArray();
                    result = Convert.ToBase64String(data);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error<SerializationHelper>("Error in the method SerializeToString()", ex);
            }
            return result;
        }

        public static void SerializeToFile(object obj, string filePath)
        {
            try
            {
                // Make sure that the directory exists
                var directory = System.IO.Path.GetDirectoryName(filePath);
                if (!System.IO.Directory.Exists(directory))
                {
                    System.IO.Directory.CreateDirectory(directory);
                }

                // Serialize
                using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
                {
                    BinaryFormatter binaryFormatter = new BinaryFormatter();
                    binaryFormatter.Serialize(fileStream, obj);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error<SerializationHelper>("Error in the method SerializeToFile()", ex);
            }
        }

        public static object DeserializeFromString(string str)
        {
            object result = null;
            try
            {
                byte[] data = Convert.FromBase64String(str);
                using (MemoryStream memoryStream = new MemoryStream(data))
                {
                    BinaryFormatter binaryFormatter = new BinaryFormatter();
                    result = binaryFormatter.Deserialize(memoryStream);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error<SerializationHelper>("Error in the method DeserializeFromString()", ex);
            }
            return result;
        }

        public static object DeserializeFromFile(string filePath)
        {
            object result = null;
            try
            {
                if (System.IO.File.Exists(filePath))
                {
                    using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        BinaryFormatter formatter = new BinaryFormatter();
                        result = formatter.Deserialize(fileStream);
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error<SerializationHelper>("Error in the method DeserializeFromFile()", ex);
            }
            return result;
        }


    }
}