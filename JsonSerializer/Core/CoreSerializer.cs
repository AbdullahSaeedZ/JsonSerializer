using Serializer.Attributes;
using System.Reflection;
using System.Text;

namespace Serializer.Core
{
    internal static class CoreSerializer
    {
        // Total Space Offset = Depth X SpacesPerIndent
        // depth is the level, spaces number or tabs, which increasses with an open bracket and decreasses with a closing bracket

        // removed static depth and totalSpaces fields here to avoid resetting with multiple serializations at same app life time,
        // and to avoid race condition issues
        private static readonly int _spacesPerDepth = 4;

        public static IEnumerable<string> ConvertElementsToJson(IEnumerable<object?>? elements)
        {
            if (elements == null)
            {
                yield return "[]";
                yield break;
            }

            yield return "[\n";
            bool isFirst = true;

            foreach (object? element in elements)
            {
                if (element == null) continue;

                if (!isFirst)
                {
                    yield return ",\n";
                }

                isFirst = false;
                yield return GetOneObjectJson(element, 1); // depth is 1 cuz there is alreasy an array starting bracket
            }

            yield return "\n]";
        }

        // 

        public static string GetOneObjectJson(object obj, int depthLevel, bool isNested = false)
        {
            MemberInfo[] members = ReflectionHelper.GetMembers(obj.GetType());
            if (members.Length == 0) return "{}";

            return ConvertMembersToJson(obj, members, depthLevel, isNested);
        }

        private static string ConvertMembersToJson(object obj, MemberInfo[] members, int depthLevel, bool isNested)
        {
            int _depthLevel = depthLevel;
            string spaces = GetSpaces(_depthLevel);

            StringBuilder oneObjectJson = new StringBuilder(isNested ? "{" : $"{spaces}{{");
            spaces = GetSpaces(++_depthLevel);

            foreach (MemberInfo member in members)
            {
                // preparing the json property section
                string name = member.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? member.Name;
                oneObjectJson.Append($"\n{spaces}\"{name}\": ");

                // to hanle value if a nesdted objcet
                if (HandleNestedObject(obj, member, oneObjectJson, _depthLevel))
                {
                    oneObjectJson.Append(",");
                    continue;
                }

                // preparing the json value section
                AppendMemberValue(obj, member, oneObjectJson);
            }
            oneObjectJson.Length--; // to remove the last comma
            spaces = GetSpaces(--_depthLevel);

            oneObjectJson.Append($"\n{spaces}}}");
            return oneObjectJson.ToString();
        }

        private static string GetSpaces(int newDepthLevel)
        {
            int TotalSpaces = newDepthLevel * _spacesPerDepth;
            return new string(' ', TotalSpaces);
        }

        private static bool HandleNestedObject(object parentObj, MemberInfo member, StringBuilder oneObjectJson, int depthLevel)
        {
            // to check if member is a nested class, other checks are done in HandleRestrictions in ReflectionHelper class
            if (!( member is PropertyInfo nestedClass && !nestedClass.PropertyType.IsPrimitive && nestedClass.PropertyType != typeof(string) && nestedClass.PropertyType != typeof(decimal) ))
                return false;

            object? childObj = nestedClass.GetValue(parentObj);

            if (childObj == null)
                return false;

            oneObjectJson.Append(GetOneObjectJson(childObj, depthLevel, true));
            return true;
        }

        private static void AppendMemberValue(object obj, MemberInfo member, StringBuilder oneObjectJson)
        {
            object? value = member switch
            {
                PropertyInfo p => p.GetValue(obj),
                FieldInfo f => f.GetValue(obj),
                _ => null
            };
            Type? elementType = member switch
            {
                PropertyInfo p => p.PropertyType,
                FieldInfo f => f.FieldType,
                _ => null
            };

            if (value == null)
            {
                oneObjectJson.Append("null,");
            }
            else if (elementType == typeof(string))
            {
                oneObjectJson.Append($"\"{value}\",");
            }
            else if (elementType!.IsPrimitive && elementType != typeof(bool) && elementType != typeof(char) || elementType == typeof(decimal))
            {
                oneObjectJson.Append($"{value},");
            }
            else if (elementType == typeof(bool))
            {
                oneObjectJson.Append($"{value.ToString()!.ToLower()},");
            }
        }




    }
}
