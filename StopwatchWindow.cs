using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

public class StopwatchWindow : Form
{
    private readonly Stopwatch stopwatch;
    private System.Windows.Forms.Timer uiUpdateTimer;
    private System.Windows.Forms.Timer idleCheckTimer; // Timer to check system idle state
    private Label timeLabel;
    private readonly IntPtr targetDeviceHandle; // Store the handle of the mouse to monitor
    private const int IdleThresholdSeconds = 10; // Configurable idle time in seconds
    private bool isPausedByIdle = false; // Track if paused specifically due to idle

    public StopwatchWindow(Stopwatch stopwatchInstance, IntPtr deviceHandleToMonitor)
    {
        this.stopwatch = stopwatchInstance;
        this.targetDeviceHandle = deviceHandleToMonitor; // Receive the specific handle
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        // Form Properties
        this.Text = "Mouseless Time";
        this.ClientSize = new Size(160, 50); // Adjusted size for better fit
        this.FormBorderStyle = FormBorderStyle.FixedToolWindow; // Allows dragging, minimal chrome
        this.StartPosition = FormStartPosition.Manual; // Allow setting position later if needed
        // Place it somewhere reasonable initially, e.g., top-right
        this.Location = new Point(Screen.PrimaryScreen.WorkingArea.Width - this.Width - 10, 10);
        this.TopMost = true; // Always on top
        this.ShowInTaskbar = false; // Keep it less intrusive

        // Label Properties
        this.timeLabel = new Label();
        this.timeLabel.Font = new Font("Consolas", 16F, FontStyle.Bold, GraphicsUnit.Point, 0); // Monospaced font, larger size
        this.timeLabel.TextAlign = ContentAlignment.MiddleCenter;
        this.timeLabel.Dock = DockStyle.Fill; // Fill the form
        this.timeLabel.Text = "00:00:00.0"; // Initial text with tenths
        this.Controls.Add(this.timeLabel);

        // Timer Properties
        this.uiUpdateTimer = new System.Windows.Forms.Timer();
        this.uiUpdateTimer.Interval = 100; // Update 10 times per second for tenths
        this.uiUpdateTimer.Tick += UiUpdateTimer_Tick;
        this.uiUpdateTimer.Start();

        // Idle Check Timer Properties
        this.idleCheckTimer = new System.Windows.Forms.Timer();
        this.idleCheckTimer.Interval = 1000; // Check every second
        this.idleCheckTimer.Tick += IdleCheckTimer_Tick;
        this.idleCheckTimer.Start();

        // Ensure stopwatch is running when window is created
        if (!this.stopwatch.IsRunning)
        {
            this.stopwatch.Start();
        }
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        // Explicitly set the window to be topmost using SetWindowPos
        // We use SWP_NOMOVE and SWP_NOSIZE because we only want to affect the Z-order
        bool result = NativeMethods.SetWindowPos(
            this.Handle,
            NativeMethods.HWND_TOPMOST,
            0, 0, 0, 0, // X, Y, cx, cy are ignored due to flags
            NativeMethods.SetWindowPosFlags.SWP_NOMOVE | NativeMethods.SetWindowPosFlags.SWP_NOSIZE);

        if (!result)
        {
            // Log error if SetWindowPos fails
            int error = Marshal.GetLastWin32Error();
            Console.WriteLine($"Warning: SetWindowPos failed to set TopMost. Error code: {error}");
            // The TopMost property might still work, or this provides a clue if it doesn't.
        }
    }


    private void UiUpdateTimer_Tick(object? sender, EventArgs e)
    {
        // Update label text, ensuring UI update is safe (Timer tick is already on UI thread)
        TimeSpan elapsed = this.stopwatch.Elapsed;
        // Format with hours, minutes, seconds, and tenths of a second
        this.timeLabel.Text = $"{elapsed.Hours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}.{(elapsed.Milliseconds / 100)}";
    }

    // Public method to restart the stopwatch (called from WndProc when target mouse moves)
    public void RestartStopwatch()
    {
        Console.WriteLine("RestartStopwatch called."); // Debug
        this.stopwatch.Restart();
        this.isPausedByIdle = false; // Reset idle pause flag
        // Update label immediately and ensure it looks 'active'
        this.timeLabel.ForeColor = SystemColors.ControlText; // Reset color if changed
        UiUpdateTimer_Tick(null, EventArgs.Empty);
    }

    private void IdleCheckTimer_Tick(object? sender, EventArgs e)
    {
        var lastInputInfo = new NativeMethods.LASTINPUTINFO();
        lastInputInfo.cbSize = (uint)Marshal.SizeOf(lastInputInfo);

        if (NativeMethods.GetLastInputInfo(ref lastInputInfo))
        {
            // Calculate idle time in milliseconds
            // Handle potential Environment.TickCount wrapping by using unchecked arithmetic
            uint currentTimeTicks = unchecked((uint)Environment.TickCount);
            uint lastInputTicks = lastInputInfo.dwTime;
            uint idleTimeMs = unchecked(currentTimeTicks - lastInputTicks);

            // Console.WriteLine($"Idle Time (ms): {idleTimeMs}"); // Debug output

            if (idleTimeMs >= IdleThresholdSeconds * 1000)
            {
                // Idle threshold reached - PAUSE if running
                if (this.stopwatch.IsRunning)
                {
                    Console.WriteLine($"System idle detected ({idleTimeMs}ms). Pausing stopwatch."); // Debug
                    this.stopwatch.Stop();
                    this.isPausedByIdle = true;
                    // Optional: Indicate paused state visually
                    this.timeLabel.ForeColor = SystemColors.GrayText;
                }
            }
            else
            {
                // System is active - RESUME if paused *by idle*
                if (!this.stopwatch.IsRunning && this.isPausedByIdle)
                {
                    Console.WriteLine($"System active again ({idleTimeMs}ms). Resuming stopwatch."); // Debug
                    this.stopwatch.Start();
                    this.isPausedByIdle = false;
                    // Optional: Restore active visual state
                    this.timeLabel.ForeColor = SystemColors.ControlText;
                }
            }
        }
        else
        {
            // Failed to get last input info - log warning?
            Console.WriteLine("Warning: GetLastInputInfo failed.");
        }
    }


    // --- Raw Input Handling ---

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        RegisterForRawInput();
    }

    private void RegisterForRawInput()
    {
        var rid = new NativeMethods.RAWINPUTDEVICE[1];
        rid[0].UsagePage = 0x01; // HID_USAGE_PAGE_GENERIC
        rid[0].Usage = 0x02;      // HID_USAGE_GENERIC_MOUSE
        // RIDEV_INPUTSINK allows receiving input even when the window is not in the foreground.
        // RIDEV_DEVNOTIFY allows receiving device arrival/removal messages (optional).
        rid[0].Flags = NativeMethods.RIDEV_INPUTSINK;
        rid[0].Target = this.Handle; // Target this window handle

        Console.WriteLine($"Registering raw input for window handle: {this.Handle}");
        if (!NativeMethods.RegisterRawInputDevices(rid, (uint)rid.Length, (uint)Marshal.SizeOf<NativeMethods.RAWINPUTDEVICE>()))
        {
            int error = Marshal.GetLastWin32Error();
            Console.WriteLine($"Failed to register raw input device. Error code: {error}");
            // Throw detailed exception or handle gracefully
            throw new System.ComponentModel.Win32Exception(error, "Failed to register raw input device.");
        }
        Console.WriteLine("Raw input device registered successfully for the window.");
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == NativeMethods.WM_INPUT)
        {
            uint dwSize = 0;
            // Get the size of the input data.
            NativeMethods.GetRawInputData(m.LParam, NativeMethods.RID_INPUT, IntPtr.Zero, ref dwSize, (uint)Marshal.SizeOf<NativeMethods.RAWINPUTHEADER>());

            if (dwSize == 0)
            {
                base.WndProc(ref m);
                return;
            }

            IntPtr buffer = Marshal.AllocHGlobal((int)dwSize);
            try
            {
                // Read the raw input data.
                if (NativeMethods.GetRawInputData(m.LParam, NativeMethods.RID_INPUT, buffer, ref dwSize, (uint)Marshal.SizeOf<NativeMethods.RAWINPUTHEADER>()) == dwSize)
                {
                    NativeMethods.RAWINPUT raw = Marshal.PtrToStructure<NativeMethods.RAWINPUT>(buffer);

                    // Check if it's mouse input AND matches the specific device handle we are monitoring
                    if (raw.header.dwType == NativeMethods.RIM_TYPEMOUSE && raw.header.hDevice == this.targetDeviceHandle)
                    {
                        // Check for actual movement (lLastX/lLastY are relative movements)
                        if (raw.mouse.lLastX != 0 || raw.mouse.lLastY != 0)
                        {
                            // Physical mouse moved - *only* restart the stopwatch now.
                            // Resuming from idle is handled by the idleCheckTimer.
                            Console.WriteLine($"[PHYSICAL] Target mouse moved ({raw.header.hDevice}) - Restarting stopwatch."); // Debug output
                            RestartStopwatch();
                        }
                        // Optionally log button presses etc.
                        // if (raw.mouse.usButtonFlags != 0) { ... }
                    }
                    // else { Console.WriteLine($"Ignoring input from device: {raw.header.hDevice}"); } // Debugging
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing raw input: {ex.Message}");
                // Consider logging the exception details
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        // Pass message to base class for default processing
        base.WndProc(ref m);
    }

    // Optional: Override OnFormClosing to stop the timer
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        uiUpdateTimer?.Stop();
        uiUpdateTimer?.Dispose();
        idleCheckTimer?.Stop(); // Stop the idle timer too
        idleCheckTimer?.Dispose();
        base.OnFormClosing(e);
    }
}
