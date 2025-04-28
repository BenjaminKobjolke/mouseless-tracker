using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

internal static class SettingsManager
{
    private static readonly string settingsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.ini");
    private const string SettingsSection = "Settings";
    private const string DeviceNameKey = "SelectedDeviceName";
    private const int MaxIniValueSize = 255; // Standard max size for GetPrivateProfileString buffer

    public static void SaveSelectedDevice(string deviceName)
    {
        WriteSetting(DeviceNameKey, deviceName);
    }

    public static string? LoadSelectedDevice()
    {
        string value = ReadSetting(DeviceNameKey);
        return string.IsNullOrEmpty(value) ? null : value;
    }

    private static void WriteSetting(string key, string? value)
    {
        NativeMethods.WritePrivateProfileString(SettingsSection, key, value, settingsFilePath);
        // Consider adding error handling based on the return value if needed
    }

    private static string ReadSetting(string key, string defaultValue = "")
    {
        StringBuilder retVal = new StringBuilder(MaxIniValueSize);
        NativeMethods.GetPrivateProfileString(SettingsSection, key, defaultValue, retVal, MaxIniValueSize, settingsFilePath);
        // Consider adding error handling based on the return value if needed
        return retVal.ToString();
    }
}
