using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

internal static class SettingsManager
{
    private static readonly string settingsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.ini");
    private const string SettingsSection = "Settings";
    private const string DeviceNameKey = "SelectedDeviceName";
    private const string WindowPosXKey = "WindowPosX";
    private const string WindowPosYKey = "WindowPosY";
    private const string FontNameKey = "FontName"; // New key for Font Name
    private const string FontSizeKey = "FontSize"; // New key for Font Size
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

    public static void SaveWindowPosition(Point location)
    {
        WriteSetting(WindowPosXKey, location.X.ToString());
        WriteSetting(WindowPosYKey, location.Y.ToString());
        Console.WriteLine($"Saved window position: {location}"); // Debug
    }

    public static Point? LoadWindowPosition()
    {
        string xStr = ReadSetting(WindowPosXKey);
        string yStr = ReadSetting(WindowPosYKey);

        if (int.TryParse(xStr, out int x) && int.TryParse(yStr, out int y))
        {
            Console.WriteLine($"Loaded window position: X={x}, Y={y}"); // Debug
            return new Point(x, y);
        }
        else
        {
            Console.WriteLine("No valid window position found in settings."); // Debug
            return null; // Return null if parsing fails or keys don't exist
        }
    }

    public static void SaveFontSettings(string fontName, float fontSize)
    {
        WriteSetting(FontNameKey, fontName);
        WriteSetting(FontSizeKey, fontSize.ToString(System.Globalization.CultureInfo.InvariantCulture)); // Use invariant culture for float
        Console.WriteLine($"Saved font: {fontName}, Size: {fontSize}"); // Debug
    }

    public static (string? Name, float? Size) LoadFontSettings()
    {
        string nameStr = ReadSetting(FontNameKey);
        string sizeStr = ReadSetting(FontSizeKey);

        string? name = string.IsNullOrEmpty(nameStr) ? null : nameStr;
        float? size = null;

        if (float.TryParse(sizeStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float parsedSize))
        {
            size = parsedSize;
        }

        if (name != null && size.HasValue)
        {
            Console.WriteLine($"Loaded font: {name}, Size: {size.Value}"); // Debug
        }
        else
        {
            Console.WriteLine("No valid font settings found."); // Debug
        }

        return (name, size);
    }
}
