using System;
using System.Runtime.InteropServices;
using System.Text;

internal static class NativeMethods
{
    // --- Raw Input Structures ---

    [StructLayout(LayoutKind.Sequential)]
    internal struct RAWINPUTDEVICELIST
    {
        public IntPtr hDevice;
        public uint dwType;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct RAWINPUTDEVICE
    {
        public ushort UsagePage;
        public ushort Usage;
        public uint Flags;
        public IntPtr Target;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct RAWINPUTHEADER
    {
        public uint dwType;
        public uint dwSize;
        public IntPtr hDevice;
        public IntPtr wParam;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct RAWMOUSE
    {
        public ushort usFlags;
        public uint ulButtons; // Changed from ulong to uint based on common definitions
        public ushort usButtonFlags;
        public ushort usButtonData;
        public uint ulRawButtons;
        public int lLastX;
        public int lLastY;
        public uint ulExtraInformation;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct RAWINPUT
    {
        [FieldOffset(0)]
        public RAWINPUTHEADER header;
        [FieldOffset(16)] // Adjust offset if RAWMOUSE size changes (should be sizeof(RAWINPUTHEADER))
        public RAWMOUSE mouse;
        // Add other potential raw input types like keyboard if needed later
        // [FieldOffset(16)]
        // public RAWKEYBOARD keyboard;
        // [FieldOffset(16)]
        // public RAWHID hid;
    }

    // --- Raw Input Functions ---

    // Constants for GetRawInputDeviceInfo
    internal const uint RIDI_DEVICENAME = 0x20000007;
    // Constants for RAWINPUTDEVICE Flags
    internal const uint RIDEV_INPUTSINK = 0x00000100;
    // Constants for GetRawInputData uiCommand
    internal const uint RID_INPUT = 0x10000003;
    // Constants for RAWINPUTHEADER dwType
    internal const uint RIM_TYPEMOUSE = 0;
    // Constants for Window Messages
    internal const int WM_INPUT = 0x00FF;


    [DllImport("user32.dll", SetLastError = true)]
    internal static extern uint GetRawInputDeviceList(
        [Out] RAWINPUTDEVICELIST[]? pRawInputDeviceList, // Nullable for querying count
        ref uint puiNumDevices,
        uint cbSize);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Ansi)] // Use Ansi for device name
    internal static extern uint GetRawInputDeviceInfo(
        IntPtr hDevice,
        uint uiCommand,
        IntPtr pData, // Can be IntPtr.Zero for querying size
        ref uint pcbSize);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool RegisterRawInputDevices(
        RAWINPUTDEVICE[] pRawInputDevices,
        uint uiNumDevices,
        uint cbSize);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern uint GetRawInputData(
        IntPtr hRawInput,
        uint uiCommand,
        IntPtr pData, // Can be IntPtr.Zero for querying size
        ref uint pcbSize,
        uint cbSizeHeader);


    // --- INI File Functions ---

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern long WritePrivateProfileString(
        string section,
        string key,
        string? value, // Allow null to delete key
        string filePath);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern int GetPrivateProfileString(
        string section,
        string key,
        string defaultValue,
        StringBuilder retVal,
        int size,
        string filePath);

    // --- Window Positioning Functions ---

    // Special window handles for SetWindowPos
    internal static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    internal static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
    internal static readonly IntPtr HWND_TOP = new IntPtr(0);
    internal static readonly IntPtr HWND_BOTTOM = new IntPtr(1);

    // SetWindowPos Flags
    [Flags]
    internal enum SetWindowPosFlags : uint
    {
        SWP_NOSIZE = 0x0001,
        SWP_NOMOVE = 0x0002,
        SWP_NOZORDER = 0x0004,
        SWP_NOREDRAW = 0x0008,
        SWP_NOACTIVATE = 0x0010,
        SWP_FRAMECHANGED = 0x0020, // The frame changed: send WM_NCCALCSIZE
        SWP_SHOWWINDOW = 0x0040,
        SWP_HIDEWINDOW = 0x0080,
        SWP_NOCOPYBITS = 0x0100,
        SWP_NOOWNERZORDER = 0x0200, // Don't do owner Z ordering
        SWP_NOSENDCHANGING = 0x0400, // Don't send WM_WINDOWPOSCHANGING
        SWP_DRAWFRAME = SWP_FRAMECHANGED,
        SWP_NOREPOSITION = SWP_NOOWNERZORDER,
        SWP_DEFERERASE = 0x2000,
        SWP_ASYNCWINDOWPOS = 0x4000
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int X,
        int Y,
        int cx,
        int cy,
        SetWindowPosFlags uFlags);
}
