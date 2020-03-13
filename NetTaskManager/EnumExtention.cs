using System;
using System.ComponentModel;
using System.Reflection;

namespace NetTaskManager
{
    static class EnumExtention
    {
        public static string GetDescription(this Enum value)
        {
            Type type = value.GetType();
            string name = Enum.GetName(type, value);
            if (name == null)
                return null;
            FieldInfo field = type.GetField(name);
            DescriptionAttribute attribute = Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) as DescriptionAttribute;
            return attribute == null ? name : attribute.Description;
        }
    }
}
