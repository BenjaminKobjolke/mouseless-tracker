using System;
using System.Collections.Generic;
using System;
using System.Collections.Generic;
using System.Diagnostics; // Added for Stopwatch
using System.Linq;
using System.Threading;
using System.Windows.Forms; // Keep for Application.Run and MessageBox

// Program class is now just the entry point, not a Form
static class Program
{
    // Static fields for selected device details
    private static IntPtr selectedMouseHandle = IntPtr.Zero;
    private static string selectedMouseName = string.Empty;

    [STAThread]
    static void Main()
    {
        // Enable visual styles *before* any UI is created, including the selection form.
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        // --- Device Selection Logic ---
        // This should be the first step. SelectDevice handles finding/prompting.
        SelectDevice();

        // If no device was selected (SelectDevice handles messaging the user), exit.
        if (selectedMouseHandle == IntPtr.Zero)
        {
            // Show a message box as console might not be visible
            MessageBox.Show("No mouse device selected. Application will exit.", "Mouseless Tracker", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // --- Setup and Run Stopwatch Window ---
        var stopwatch = new Stopwatch();
        // Don't start stopwatch here, let the window start it when it's ready

        Console.WriteLine($"Launching Stopwatch Window for '{selectedMouseName}' (Handle: {selectedMouseHandle})...");

        // Visual styles already enabled above.

        // Create and run the StopwatchWindow, passing the stopwatch instance and the selected device handle
        Application.Run(new StopwatchWindow(stopwatch, selectedMouseHandle));

        // Application.Run is blocking, code here will execute after the StopwatchWindow closes.
        Console.WriteLine("Stopwatch window closed. Application exiting.");
    }

    // Helper method to encapsulate device selection logic
    private static void SelectDevice()
    {
        // This method now runs before any UI is created.
        // Console output might not be visible if launched without a console.
        // Consider using MessageBox for critical info or errors.

        Console.WriteLine("Checking for connected mice...");
        var availableMice = InputDeviceManager.GetMouseDevices();

        if (availableMice.Count == 0)
        {
            Console.WriteLine("No mice found.");
            MessageBox.Show("No mouse devices found. Please check connections.", "Mouseless Tracker", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return; // Exit selection process, Main will handle the Zero handle
        }

        string? savedDeviceName = SettingsManager.LoadSelectedDevice();
        bool deviceFoundFromSettings = false;

        if (savedDeviceName != null)
        {
            Console.WriteLine($"Checking for previously selected device: {savedDeviceName}");
            var foundDevice = availableMice.FirstOrDefault(d => d.Name == savedDeviceName);
            if (foundDevice != default && foundDevice.Handle != IntPtr.Zero)
            {
                selectedMouseHandle = foundDevice.Handle;
                selectedMouseName = foundDevice.Name;
                Console.WriteLine("Found previously selected device.");
                deviceFoundFromSettings = true;
            }
            else
            {
                Console.WriteLine("Previously selected device not found or disconnected.");
            }
        }

        if (!deviceFoundFromSettings)
        {
            // Need to prompt user - this is tricky without a console or initial UI.
            // Option 1: Show a simple selection form.
            // Option 2: Temporarily attach/show console (complex).
            // Option 3: Show MessageBox with device list (ugly).
            // Let's implement Option 1: A simple selection form.
            try // Add try block for form creation and display
            {
                using (var selectionForm = CreateSelectionForm(availableMice))
                {
                    // Visual styles should already be enabled by Main. Remove duplicate calls.
                    // Application.EnableVisualStyles();
                    // Application.SetCompatibleTextRenderingDefault(false);

                    DialogResult result = selectionForm.ShowDialog();

                    // Retrieve the selected device tuple stored in the form's Tag property
                    if (result == DialogResult.OK && selectionForm.Tag is ValueTuple<string, IntPtr> selectedDevice)
                    {
                        selectedMouseHandle = selectedDevice.Item2; // Item2 is Handle
                        selectedMouseName = selectedDevice.Item1;   // Item1 is Name
                        Console.WriteLine($"Device selected via form: {selectedMouseName}");
                        SettingsManager.SaveSelectedDevice(selectedMouseName); // Save the new selection
                    }
                    else
                    {
                        Console.WriteLine("No device selected from form.");
                        // selectedMouseHandle remains Zero, Main will handle exit.
                        return; // Exit SelectDevice method
                    }
                } // using selectionForm disposes it here
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error displaying selection form: {ex.Message}");
                MessageBox.Show($"An error occurred while trying to show the device selection window:\n\n{ex.Message}",
                                "Selection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                // Ensure handle is zero so Main exits
                selectedMouseHandle = IntPtr.Zero;
                selectedMouseName = string.Empty;
                return; // Exit SelectDevice method
            }
        }
    } // End of if (!deviceFoundFromSettings) block

    // Helper to create a simple form for device selection
    private static Form CreateSelectionForm(List<(string Name, IntPtr Handle)> devices)
    {
        var form = new Form
        {
            Text = "Select Mouse Device",
            ClientSize = new Size(400, 200),
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterScreen,
            MinimizeBox = false,
            MaximizeBox = false
        };

        var label = new Label
        {
            Text = "Select the physical mouse to monitor:",
            Location = new Point(10, 10),
            AutoSize = true
        };

        var listBox = new ListBox
        {
            Location = new Point(10, 35),
            Size = new Size(380, 120),
            DisplayMember = "Name", // Use the Name property of the anonymous type below
            ValueMember = "Device" // Use the Device property
        };

        // Populate ListBox with an object that holds Name and the Tuple
        listBox.Items.AddRange(devices.Select(d => new { Name = d.Name, Device = d }).ToArray());
        listBox.SelectedIndex = 0; // Default selection

        var okButton = new Button
        {
            Text = "OK",
            Location = new Point(230, 165),
            DialogResult = DialogResult.OK
        };

        var cancelButton = new Button
        {
            Text = "Cancel",
            Location = new Point(315, 165),
            DialogResult = DialogResult.Cancel
        };

        form.Controls.Add(label);
        form.Controls.Add(listBox);
        form.Controls.Add(okButton);
        form.Controls.Add(cancelButton);
        form.AcceptButton = okButton;
        form.CancelButton = cancelButton;

        // Store the selected item when OK is clicked (or selection changes)
        form.FormClosing += (sender, e) =>
        {
            if (form.DialogResult == DialogResult.OK && listBox.SelectedItem != null)
            {
                // Retrieve the anonymous type, then the actual device tuple
                dynamic selectedItemObject = listBox.SelectedItem;
                form.Tag = selectedItemObject.Device; // Store the selected (Name, Handle) tuple in Tag
            }
        };

        return form;
    }

    // The SelectedItem property and FormExtensions class are no longer needed.
}
