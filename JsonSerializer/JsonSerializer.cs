using Serializer.Core;


namespace Serializer
{
    public static class JsonSerializer
    {
        /// <summary>
        /// Serializes a collection of objects to a JSON file.
        /// </summary>
        /// <param name="elements"></param>
        /// <param name="filePath"></param>
        public static async Task SerializeAsync(IEnumerable<object?>? elements, string filePath)
        {
            using StreamWriter writer = new StreamWriter(filePath, false);
            IEnumerable<string> elemnts = CoreSerializer.ConvertElementsToJson(elements);

            foreach (string element in elemnts)
            {
                if (element == null) continue;
                await writer.WriteAsync(element);
            }
        }


        /// <summary>
        /// Serializes a single object to a JSON file.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="filePath"></param>
        public static async Task SerializeAsync(object obj, string filePath)
        {
            if (obj == null) return;

            using StreamWriter writer = new StreamWriter(filePath, false);
            string jsonObject = CoreSerializer.GetOneObjectJson(obj, 0);
            await writer.WriteAsync(jsonObject);
        }


        //--------------


        /// <summary>
        /// Deserializes a JSON file into a list of objects of type T.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static async Task<List<T>?> DeserializeListAsync<T>(string filePath)
        {
            if (!File.Exists(filePath))
                return default;

            using StreamReader reader = new StreamReader(filePath);
            List<T> list = new List<T>();

            IAsyncEnumerable<string> jsonObjects = CoreDeserializer.ReadOneObjectJson(reader);

            await foreach (string jsonObject in jsonObjects)
            {
                T? obj = CoreDeserializer.ConvertJsonToObject<T>(jsonObject);
                if (obj != null)
                    list.Add(obj);
            }
            return list;
        }


        /// <summary>
        /// Deserializes a JSON file into an object of type T.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static async Task<T?> DeserializeAsync<T>(string filePath)
        {
            if (!File.Exists(filePath))
                return default;

            using StreamReader reader = new StreamReader(filePath);

            char firstChar = (char)reader.Peek();
            if (firstChar == '[' || firstChar == -1)
                return default;

            string jsonContent = await reader.ReadToEndAsync();
            return CoreDeserializer.ConvertJsonToObject<T>(jsonContent);
        }
    }
}




