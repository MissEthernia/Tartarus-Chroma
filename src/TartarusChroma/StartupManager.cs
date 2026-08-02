using Microsoft.Win32;

namespace TartarusChroma;

internal static class StartupManager
{
    private const string RunKey =
        @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "TartarusChroma";

    public static void SetEnabled(bool enabled)
    {
        using RegistryKey key =
            Registry.CurrentUser.CreateSubKey(RunKey, writable: true);

        if (enabled)
        {
            string executable = Application.ExecutablePath;
            key.SetValue(ValueName, $"\"{executable}\" --minimized");
        }
        else
        {
            key.DeleteValue(ValueName, throwOnMissingValue: false);
        }
    }
}
