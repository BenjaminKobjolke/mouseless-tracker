# Mouseless Tracker

A Windows application that tracks how long you haven't used your mouse, encouraging mouseless workflows and keyboard-centric productivity.

## Features

-   **Real-time Tracking**: Shows a small, always-on-top stopwatch displaying time since last mouse movement
-   **Device-Specific Monitoring**: Tracks a specific physical mouse device, ignoring virtual mice and other input devices
-   **Configurable Idle Detection**: Automatically pauses tracking when the system is idle (configurable threshold)
-   **Session Logging**: Optional SQLite database logging of mouseless sessions with application context
-   **Persistent Settings**: Remembers window position, font preferences, and selected mouse device
-   **Lightweight & Unobtrusive**: Minimal system resources, transparent background, draggable window

## Configuration

The application uses a `settings.ini` file for configuration:

```ini
[Settings]
; Idle threshold in seconds - how long the system must be idle before pausing the tracker
; Default: 10 seconds
IdleThresholdSeconds=10

; Other settings are automatically managed by the application
SelectedDeviceName=
WindowPosX=
WindowPosY=
FontName=
FontSize=
```

### Customizing Idle Threshold

To change when the tracker pauses due to system inactivity:

1. Close the application
2. Edit `settings.ini` in the application directory
3. Change `IdleThresholdSeconds` to your desired value (in seconds)
4. Restart the application

### Session Logging

The application can optionally log mouseless sessions to a SQLite database (`database.db`) for analysis:

```ini
[Settings]
; Session logging to SQLite database (disabled by default)
LoggingEnabled=true
```

**Logged Data:**

-   Session duration (in seconds)
-   Timestamp of when mouse was moved
-   Active application executable name
-   Active window title

**Requirements:**

-   Sessions must be at least 10 seconds long to be logged
-   Logging is disabled by default for privacy
-   Database file is created automatically when first enabled

**Privacy Notes:**

-   All data is stored locally in `database.db`
-   No network communication or data transmission
-   Can be disabled at any time by setting `LoggingEnabled=false`

## Building

### Debug Build

```bash
dotnet build mouseless-tracker.sln
```

### Release Build

```bash
dotnet build mouseless-tracker.sln --configuration Release
```

### Running

```bash
dotnet run --project MouselessTracker.csproj
```

## Usage

1. Launch the application
2. Select your physical mouse device from the list (first time only)
3. The tracker window appears in the top-right corner
4. Move the window by dragging it
5. Use keyboard shortcuts:
    - **Arrow Keys**: Move window (hold Shift for fine movement)
    - **Escape**: Close application

## Technical Details

-   **Framework**: .NET 9.0 (Windows Forms)
-   **Input Monitoring**: Raw Input API for precise device-specific tracking
-   **Idle Detection**: Windows GetLastInputInfo API
-   **Settings**: INI file format using Windows API
-   **UI**: Transparent, borderless window with custom rendering

## Requirements

-   Windows 10/11
-   .NET 9.0 Runtime
-   At least one connected mouse device
