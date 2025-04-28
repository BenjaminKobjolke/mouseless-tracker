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

    private static readonly Color TransparentColor = Color.Black; // Color to make transparent
    private bool isDragging = false; // Flag for dragging state
    private Point dragStartPosition; // Point where dragging started
    private bool isWindowActive = false; // Flag to track window focus for border painting

    private void InitializeComponent()
    {
        // Form Properties
        this.Text = "Mouseless Time";
        this.ClientSize = new Size(160, 50); // Adjusted size for better fit
        this.FormBorderStyle = FormBorderStyle.None; // Remove border and title bar
        this.StartPosition = FormStartPosition.Manual; // We will load/save position
        // Default position if none saved (will be set later in OnLoad)
        this.Location = new Point(Screen.PrimaryScreen.WorkingArea.Width - this.Width - 10, 10);
        this.TopMost = true; // Always on top (SetWindowPos will reinforce this)
        this.ShowInTaskbar = false; // Keep it less intrusive
        this.BackColor = TransparentColor; // Set background color
        this.TransparencyKey = TransparentColor; // Make this color transparent

        // Label Properties
        this.timeLabel = new Label();
        LoadAndApplyFontSettings(); // Load font from settings or use default
        this.timeLabel.ForeColor = ColorTranslator.FromHtml("#15fc11"); // Set text color to custom green
        this.timeLabel.BackColor = Color.Transparent; // Ensure label background is transparent
        this.timeLabel.TextAlign = ContentAlignment.MiddleCenter;
        this.timeLabel.Dock = DockStyle.Fill; // Fill the form
        this.timeLabel.Text = "00:00:00.0"; // Initial text with tenths
        // Add mouse event handlers to the label for dragging
        this.timeLabel.MouseDown += TimeLabel_MouseDown;
        this.timeLabel.MouseMove += TimeLabel_MouseMove;
        this.timeLabel.MouseUp += TimeLabel_MouseUp;
        // Add Paint event for custom rendering
        this.timeLabel.Paint += TimeLabel_Paint;
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

        // Add handlers for focus changes
        this.Activated += StopwatchWindow_Activated;
        this.Deactivate += StopwatchWindow_Deactivate;
    }

    protected override void OnLoad(EventArgs e)
    {
        // Load saved position before showing the form
        Point? savedPosition = SettingsManager.LoadWindowPosition();
        if (savedPosition.HasValue)
        {
            // Ensure the loaded position is visible on a screen
            bool positionIsVisible = false;
            foreach (Screen screen in Screen.AllScreens)
            {
                if (screen.WorkingArea.Contains(savedPosition.Value))
                {
                    positionIsVisible = true;
                    break;
                }
            }
            if (positionIsVisible)
            {
                this.Location = savedPosition.Value;
            }
            else
            {
                Console.WriteLine("Saved position is off-screen. Using default.");
                // Keep default position calculated in InitializeComponent
            }
        }
        // else: Keep default position calculated in InitializeComponent

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
        // Format with hours, minutes, and seconds only
        this.timeLabel.Text = $"{elapsed.Hours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}";
    }

    // Public method to restart the stopwatch (called from WndProc when target mouse moves)
    public void RestartStopwatch()
    {
        Console.WriteLine("RestartStopwatch called."); // Debug
        this.stopwatch.Restart();
        this.isPausedByIdle = false; // Reset idle pause flag
        // Update label immediately and ensure it looks 'active'
        this.timeLabel.ForeColor = ColorTranslator.FromHtml("#15fc11"); // Set text color to custom green
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
                    this.timeLabel.ForeColor = ColorTranslator.FromHtml("#15fc11"); // Set text color to custom green
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
        // Save current position before closing
        SettingsManager.SaveWindowPosition(this.Location);
        // Save current font settings
        SettingsManager.SaveFontSettings(this.timeLabel.Font.Name, this.timeLabel.Font.Size);


        uiUpdateTimer?.Stop();
        uiUpdateTimer?.Dispose();
        idleCheckTimer?.Stop(); // Stop the idle timer too
        idleCheckTimer?.Dispose();
        base.OnFormClosing(e);
    }

    // --- Mouse Dragging Event Handlers ---

    private void TimeLabel_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            isDragging = true;
            // Store the starting position of the drag relative to the screen
            dragStartPosition = this.PointToScreen(e.Location);
            // Adjust slightly so the window doesn't jump; use current window location
            // and mouse position relative to the window's top-left corner.
            dragStartPosition.Offset(-this.Left, -this.Top);

            // Attempt to explicitly set focus to the form when clicked
            this.Focus();

            Console.WriteLine("Dragging started. Attempted to set focus."); // Debug
        }
    }

    private void TimeLabel_MouseMove(object? sender, MouseEventArgs e)
    {
        if (isDragging)
        {
            // Calculate new window position
            Point currentScreenPos = this.PointToScreen(e.Location);
            // Subtract the initial offset to get the new top-left corner
            Point newLocation = Point.Subtract(currentScreenPos, new Size(dragStartPosition));

            // Move the form
            this.Location = newLocation;
            // Console.WriteLine($"Dragging to: {this.Location}"); // Debug - can be noisy
        }
    }

    private void TimeLabel_MouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            isDragging = false;
            Console.WriteLine("Dragging stopped."); // Debug
        }
    }

    // --- Keyboard Handling ---

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        const int SlowMoveStep = 1;  // Pixels for slow move (with Shift)
        const int FastMoveStep = 100; // Pixels for fast move (without Shift)

        // Check for modifier keys (Shift)
        bool shiftPressed = (keyData & Keys.Shift) == Keys.Shift;
        int moveStep = shiftPressed ? SlowMoveStep : FastMoveStep;

        // Extract the base key code without modifiers
        Keys keyCode = keyData & Keys.KeyCode;

        switch (keyCode)
        {
            case Keys.Up:
                this.Top -= moveStep;
                Console.WriteLine($"Key Move Up ({moveStep}px) to: {this.Location}"); // Debug
                return true; // Indicate key was processed
            case Keys.Down:
                this.Top += moveStep;
                Console.WriteLine($"Key Move Down ({moveStep}px) to: {this.Location}"); // Debug
                return true;
            case Keys.Left:
                this.Left -= moveStep;
                Console.WriteLine($"Key Move Left ({moveStep}px) to: {this.Location}"); // Debug
                return true;
            case Keys.Right:
                this.Left += moveStep;
                Console.WriteLine($"Key Move Right ({moveStep}px) to: {this.Location}"); // Debug
                return true;
            case Keys.Escape:
                // Escape doesn't usually have modifiers, but check base key code
                Console.WriteLine("Escape key pressed. Closing window."); // Debug
                this.Close();
                return true;
        }

        // Call base class method for any other keys
        return base.ProcessCmdKey(ref msg, keyData);
    }

    // --- Helper Methods ---
    private void LoadAndApplyFontSettings()
    {
        string defaultFontName = "Consolas";
        float defaultFontSize = 16F;
        FontStyle defaultFontStyle = FontStyle.Bold;

        var (loadedName, loadedSize) = SettingsManager.LoadFontSettings();

        string fontNameToUse = loadedName ?? defaultFontName;
        float fontSizeToUse = loadedSize ?? defaultFontSize;

        try
        {
            this.timeLabel.Font = new Font(fontNameToUse, fontSizeToUse, defaultFontStyle);
            Console.WriteLine($"Applied font: {this.timeLabel.Font.Name}, Size: {this.timeLabel.Font.Size}"); // Debug
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error applying font '{fontNameToUse}' at size {fontSizeToUse}: {ex.Message}. Using default.");
            // Fallback to default if loaded font fails
            this.timeLabel.Font = new Font(defaultFontName, defaultFontSize, defaultFontStyle);
        }
    }

    // --- Focus Handling ---
    private void StopwatchWindow_Activated(object? sender, EventArgs e)
    {
        isWindowActive = true; // Set flag
        this.timeLabel.ForeColor = ColorTranslator.FromHtml("#15fc11"); // Set text color to custom green
        // this.timeLabel.BackColor = Color.DarkBlue; // Reverted temporary visual feedback
        this.Invalidate(); // Trigger repaint to draw border
        Console.WriteLine("activated"); // Restore console log
    }

    private void StopwatchWindow_Deactivate(object? sender, EventArgs e)
    {
        isWindowActive = false; // Clear flag
        // this.timeLabel.BackColor = Color.Transparent; // Reverted temporary visual feedback
        this.Invalidate(); // Trigger repaint to remove border
        Console.WriteLine("deactivated"); // Restore console log
    }

    // --- Custom Font Rendering ---
    private void TimeLabel_Paint(object? sender, PaintEventArgs e)
    {
        if (sender is Label label)
        {
            // Clear the background (important for transparency)
            e.Graphics.Clear(TransparentColor);

            // Set rendering quality - try AntiAliasGridFit or SingleBitPerPixelGridFit
            // AntiAliasGridFit often looks better but might cause artifacts on transparent backgrounds.
            // SingleBitPerPixelGridFit is less smooth but might avoid color issues.
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit; // Experiment here

            // Use TextRenderer for better consistency with standard UI text
            TextRenderer.DrawText(
                e.Graphics,
                label.Text,
                label.Font,
                label.ClientRectangle,
                label.ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter); // Center text

            // Draw border *after* text if the window is active
            if (isWindowActive)
            {
                Rectangle borderRect = label.ClientRectangle; // Use label's client rect
                borderRect.Width -= 1;  // Draw on the inside edge
                borderRect.Height -= 1; // Draw on the inside edge
                ControlPaint.DrawBorder(e.Graphics, borderRect, Color.White, ButtonBorderStyle.Solid);
            }
        }
    }

    // --- Custom Border Painting (Moved to TimeLabel_Paint) ---
    // protected override void OnPaint(PaintEventArgs e) { ... } // Removed
}
