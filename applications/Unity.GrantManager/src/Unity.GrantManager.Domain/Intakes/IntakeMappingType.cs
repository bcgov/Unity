using System;

namespace Unity.GrantManager.Intakes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class MapFieldTypeAttribute : Attribute
    {
        public MapFieldTypeAttribute(string type)
        {
            Type = type;
        }

        public string Type { get; }
    }
}
