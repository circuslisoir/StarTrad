using Microsoft.Win32;

namespace StarTrad.Helper;

public static class RegeditViewer
{
    public static object? GetRegeditValue(string registryKeyPath, string registryKeyName)
    {
        object registryValue = Registry.GetValue(registryKeyPath, registryKeyName, null);
        if (registryValue is not null)
            return registryValue;
        else
            return null;
    }
}
