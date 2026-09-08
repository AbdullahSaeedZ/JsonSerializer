using System.Reflection;
using Serializer.Attributes;
using System.Runtime.CompilerServices;

namespace Serializer.Core
{
    internal static class ReflectionHelper
    {
        // this is used in both CoreSerializer and CoreDeserializer to get the members of a type,
        // was put here to avoid code duplication and to have a single place to change the logic and default behavior if needed
        public static MemberInfo[] GetMembers(Type type)
        {
            // this will pick up both public and private fields and properties 
            return type.FindMembers(MemberTypes.Property | MemberTypes.Field,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                HandleRestrictions, null);
        }


        // this is a filtering method for FindMembers, it will be called for each member of the type so that only members that pass the filter will be returned
        private static bool HandleRestrictions(MemberInfo member, object? criteria)
        {
            // to exclude backing fields of properties (which are marked CompilerGeneratedAttribute by compiler)
            if (member.IsDefined(typeof(CompilerGeneratedAttribute)) || member.IsDefined(typeof(JsonIgnoreAttribute)))
                return false;

            if (member.IsDefined(typeof(JsonIncludeAttribute)))
                return true;

            // to skip private fields and properties that are not marked with either
            if (member is FieldInfo field && !field.IsPublic)
                return false;
            if (member is PropertyInfo prop && prop.CanRead && prop.GetMethod?.IsPublic == false)
                return false;

            return true;
        }
    }
}
