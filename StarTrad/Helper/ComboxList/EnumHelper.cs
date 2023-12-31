using System.ComponentModel;
using System.Reflection;

namespace StarTrad.Helper.ComboxList;

public class EnumHelper
{
    public static string GetDescription(Enum value)
    {
        FieldInfo? field = value.GetType().GetField(value.ToString());
        DescriptionAttribute? attribute = (DescriptionAttribute)Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute));

        return attribute == null ? value.ToString() : attribute.Description;
    }

    public static string GetValueFromString<TEnum>(string stringValue) where TEnum : Enum
    {
        foreach (TEnum valeur in Enum.GetValues(typeof(TEnum)))
        {
            if (GetDescription(valeur) == stringValue)
            {
                return valeur.ToString();
            }
        }

        return stringValue;
    }
}
