using System.ComponentModel;
using System.Reflection;

namespace StarTrad.Helper.ComboxList;

public class EnumHelper
{
    public static string? GetDescription(Enum value)
    {
        FieldInfo? field = value.GetType().GetField(value.ToString());

        if (field == null) {
            return null;
        }

        Attribute? attribute = Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute));

        if (attribute == null) {
            return value.ToString();
        }

        return ((DescriptionAttribute)attribute).Description;
    }
        
    public static string GetValueFromDescription<TEnum>(string description) where TEnum : Enum
    {
        foreach (TEnum valeur in Enum.GetValues(typeof(TEnum)))
        {
            if (GetDescription(valeur) == description)
            {
                return valeur.ToString();
            }
        }

        return description;
    }
}
