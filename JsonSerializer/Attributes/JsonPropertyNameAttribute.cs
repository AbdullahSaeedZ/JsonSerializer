namespace Serializer.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class JsonPropertyNameAttribute : Attribute
    {
        public string Name { get; set; }
        public JsonPropertyNameAttribute(string name)
        {
            Name = name;
        }
    }
}
