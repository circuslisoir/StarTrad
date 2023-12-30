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

    public static TEnum GetEnumValueFromString<TEnum>(string value) where TEnum : struct
    {
        if (!Enum.TryParse<TEnum>(value, out TEnum enumValue))
        {
            throw new ArgumentException($"La chaîne \"{value}\" n'est pas une valeur valide pour l'enum {typeof(TEnum)}.");
        }

        return enumValue;
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
