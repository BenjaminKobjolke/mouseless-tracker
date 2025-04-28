using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

internal static class InputDeviceManager
{
    public static List<(string Name, IntPtr Handle)> GetMouseDevices()
    {
        var mouseDevices = new List<(string Name, IntPtr Handle)>();
        uint deviceCount = 0;

        // First call to get the number of devices
        uint result = NativeMethods.GetRawInputDeviceList(null, ref deviceCount, (uint)Marshal.SizeOf<NativeMethods.RAWINPUTDEVICELIST>());

        if (result == unchecked((uint)-1) || deviceCount == 0)
        {
            // Handle error or no devices found
            Console.WriteLine("Error getting device list or no devices found.");
            return mouseDevices; // Return empty list
        }

        var devices = new NativeMethods.RAWINPUTDEVICELIST[deviceCount];
        // Second call to get the actual device list
        result = NativeMethods.GetRawInputDeviceList(devices, ref deviceCount, (uint)Marshal.SizeOf<NativeMethods.RAWINPUTDEVICELIST>());

        if (result == unchecked((uint)-1))
        {
            // Handle error
            Console.WriteLine("Error getting device list details.");
            return mouseDevices; // Return empty list
        }

        foreach (var device in devices)
        {
            if (device.dwType == NativeMethods.RIM_TYPEMOUSE) // Only Mice
            {
                uint size = 0;
                // First call to get the size of the device name
                NativeMethods.GetRawInputDeviceInfo(device.hDevice, NativeMethods.RIDI_DEVICENAME, IntPtr.Zero, ref size);

                if (size > 0)
                {
                    IntPtr pData = Marshal.AllocHGlobal((int)size);
                    try
                    {
                        // Second call to get the actual device name
                        if (NativeMethods.GetRawInputDeviceInfo(device.hDevice, NativeMethods.RIDI_DEVICENAME, pData, ref size) > 0)
                        {
                            // Marshal the ANSI string from the pointer
                            string deviceName = Marshal.PtrToStringAnsi(pData) ?? "Unknown Device"; // Provide fallback
                            if (!string.IsNullOrEmpty(deviceName))
                            {
                                mouseDevices.Add((deviceName, device.hDevice));
                            }
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(pData); // Ensure memory is freed
                    }
                }
            }
        }
        return mouseDevices;
    }
}
