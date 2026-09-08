namespace Serializer.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class JsonIgnoreAttribute : Attribute
    {
    }
}
