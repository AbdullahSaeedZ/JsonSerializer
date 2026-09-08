using Serializer.Attributes;
using System.Reflection;
using System.Text;

namespace Serializer.Core
{
    internal static class CoreDeserializer
    {
        public static async IAsyncEnumerable<string> ReadOneObjectJson(StreamReader reader)
        {
            StringBuilder oneObjString = new StringBuilder();
            int depth = 0;
            bool inQuotes = false;
            char previousChar = '\0';

            // to read 4kb at a time, each char is 2 bytes
            char[] buffer = new char[2048];
            int totalCharsRead;

            while (( totalCharsRead = await reader.ReadAsync(buffer, 0, buffer.Length) ) > 0)
            {
                for (int i = 0; i < totalCharsRead; i++)
                {
                    char currentChar = buffer[i];

                    // toggling inQuotes mode
                    if (currentChar == '"' && previousChar != '\\')
                    {
                        inQuotes = !inQuotes;
                    }

                    if (!inQuotes)
                    {
                        if (currentChar == '{')
                            depth++;
                        else if (currentChar == '}')
                            depth--;
                    }

                    if (depth > 0)
                    {
                        oneObjString.Append(currentChar);
                    }
                    else if (depth == 0 && currentChar == '}')
                    {
                        oneObjString.Append(currentChar);
                        yield return oneObjString.ToString();
                        oneObjString.Clear();
                    }

                    previousChar = currentChar;
                }
            }
        }

        public static T? ConvertJsonToObject<T>(string jsonContent, Type nestedType = null)
        {
            Dictionary<string, string> jsonKeysAndValues = ParseJsonContent(jsonContent);
            Type type;

            if (nestedType != null)
                type = nestedType;
            else
                type = typeof(T);

            ConstructorInfo[] constructors = type.GetConstructors();
            ConstructorInfo? validConstructor = GetValidConstructor(constructors);
            ParameterInfo[]? parameters = validConstructor?.GetParameters();

            T? obj = InvokeInitialObject<T>(validConstructor, parameters);

            if (obj == null)
                return obj;

            MapParsedValuesToObject<T>(obj, type, jsonKeysAndValues);
            return obj;
        }

        private static void MapParsedValuesToObject<T>(T obj, Type type, Dictionary<string, string> jsonKeysAndValues)
        {
            MemberInfo[] members = ReflectionHelper.GetMembers(type);

            foreach (MemberInfo member in members)
            {
                string name = member.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? member.Name;

                if (jsonKeysAndValues.TryGetValue(name, out string? value))
                {
                    if (value == "null")
                        continue;

                    // to handle nested objects
                    if (member is PropertyInfo nestedObject && nestedObject.PropertyType.IsClass && nestedObject.PropertyType != typeof(string))
                    {
                        object? objInstance = ConvertJsonToObject<object>(value, nestedObject.PropertyType);
                        nestedObject.SetValue(obj, objInstance);
                    }
                    else if (member is PropertyInfo property && property.CanWrite) //to make sure the property has a setter before setting its value
                    {
                        property.SetValue(obj, Convert.ChangeType(value, property.PropertyType));
                    }
                    else if (member is FieldInfo field)
                    {
                        field.SetValue(obj, Convert.ChangeType(value, field.FieldType));
                    }
                }
            }
        }

        private static T? InvokeInitialObject<T>(ConstructorInfo? validConstructor, ParameterInfo[]? parameters)
        {
            if (validConstructor == null || parameters == null)
                return default;

            if (parameters.Length == 0)
                return (T?)validConstructor.Invoke(null);
            else
            {
                object?[] readyParameters = new object?[parameters.Length];
                for (int i = 0; i < parameters.Length; i++)
                {
                    readyParameters[i] = parameters[i].ParameterType.IsValueType ?
                        Activator.CreateInstance(parameters[i].ParameterType) : null;
                }

                return (T?)validConstructor.Invoke(readyParameters);
            }
        }

        private static ConstructorInfo? GetValidConstructor(ConstructorInfo[] constructors)
        {
            if (constructors.Length == 0)
                throw new InvalidOperationException("No public constructors found for the type.");

            ConstructorInfo markedConstructor = null;
            ConstructorInfo parameterizedConstructor = null;
            ConstructorInfo parameterlessConstructor = null;
            int jsonConstructorAttribute = 0;
            int parameterizedConstructors = 0;

            foreach (ConstructorInfo constructor in constructors)
            {
                if (constructor.IsDefined(typeof(JsonConstructorAttribute)))
                {
                    jsonConstructorAttribute++;
                    markedConstructor = constructor;
                }

                if (constructor.GetParameters().Length == 0)
                    parameterlessConstructor = constructor;
                else
                {
                    parameterizedConstructors++;
                    parameterizedConstructor = constructor;
                }
            }

            if (jsonConstructorAttribute > 1)
                throw new InvalidOperationException("Multiple constructors with [JsonConstructor] attribute found.");

            if (markedConstructor != null)
                return markedConstructor;

            if (parameterlessConstructor != null)
                return parameterlessConstructor;

            if (parameterizedConstructors > 1)
                throw new InvalidOperationException("Multiple parameterized constructors found.");

            if (parameterizedConstructor != null)
                return parameterizedConstructor;

            return null;
        }

        private static Dictionary<string, string> ParseJsonContent(string jsonContent)
        {
            Dictionary<string, string> propertyPairs = new Dictionary<string, string>();
            List<string> pairs = GetPairs(jsonContent);

            foreach (string pair in pairs)
            {
                string[] key_Value = pair.Split(':', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

                string key = key_Value[0].Trim().Trim('"');
                string value = key_Value[1].Trim().Trim('"');

                propertyPairs[key] = value;
            }
            return propertyPairs;
        }

        private static List<string> GetPairs(string jsonContent)
        {
            List<string> results = new List<string>();
            jsonContent = jsonContent.TrimStart('{').TrimEnd('}');

            int startIndex = 0;
            int depth = 0;
            bool inQuotes = false;

            for (int i = 0; i < jsonContent.Length; i++)
            {
                char c = jsonContent[i];

                if (c == '"' && ( i == 0 || jsonContent[i - 1] != '\\' ))
                {
                    inQuotes = !inQuotes;
                }

                if (!inQuotes)
                {
                    if (c == '{' || c == '[')
                        depth++;
                    else if (c == '}' || c == ']')
                        depth--;
                    else if (c == ',' && depth == 0)
                    {
                        results.Add(jsonContent.Substring(startIndex, i - startIndex));
                        startIndex = i + 1;
                    }
                }
            }

            if (startIndex < jsonContent.Length)
                results.Add(jsonContent.Substring(startIndex));

            return results;
        }
    }
}
