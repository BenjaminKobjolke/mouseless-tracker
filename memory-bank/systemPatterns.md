# System Patterns: Mouseless Tracker

## Architecture Overview

The Mouseless Tracker follows a simple Windows Forms application architecture with clear separation of concerns:

```
Program (Entry Point)
├── Device Selection Logic
├── StopwatchWindow (Main UI)
│   ├── Raw Input Handling
│   ├── Idle Detection
│   ├── UI Rendering
│   └── Event Management
├── SettingsManager (Configuration)
├── InputDeviceManager (Device Enumeration)
└── NativeMethods (Windows API Interop)
```

## Key Technical Decisions

### 1. Raw Input API for Mouse Tracking

**Decision**: Use Windows Raw Input API instead of global mouse hooks or cursor position polling.

**Rationale**:

-   Device-specific tracking: Can distinguish between different physical mice
-   Lower system impact than global hooks
-   More reliable than polling cursor position
-   Handles multiple mice correctly

**Implementation Pattern**:

```csharp
// Register for raw input in window handle creation
protected override void OnHandleCreated(EventArgs e)
{
    RegisterForRawInput();
}

// Process raw input messages in WndProc
protected override void WndProc(ref Message m)
{
    if (m.Msg == WM_INPUT)
    {
        // Extract device handle and movement data
        // Only process if matches target device
    }
}
```

### 2. INI File Configuration

**Decision**: Use Windows INI file API instead of JSON, XML, or registry.

**Rationale**:

-   Simple key-value structure matches needs
-   Human-readable and editable
-   Native Windows API support
-   Automatic type conversion handling
-   No external dependencies

**Pattern**:

```csharp
// Centralized settings management
internal static class SettingsManager
{
    private static void WriteSetting(string key, string value)
    private static string ReadSetting(string key, string defaultValue = "")
    // Typed accessors for each setting
}
```

### 3. Idle Detection Strategy

**Decision**: Use GetLastInputInfo API with configurable threshold.

**Rationale**:

-   System-wide idle detection (not just application)
-   Handles all input types (mouse, keyboard, touch)
-   Configurable threshold allows user customization
-   Reliable and lightweight

**Pattern**:

```csharp
// Timer-based idle checking
private void IdleCheckTimer_Tick(object sender, EventArgs e)
{
    var idleTime = GetSystemIdleTime();
    if (idleTime >= configuredThreshold)
        PauseTracking();
    else if (wasPausedByIdle)
        ResumeTracking();
}
```

## Design Patterns in Use

### 1. Settings Management Pattern

**Centralized Configuration**: All settings access goes through SettingsManager static class.

-   Consistent API for all setting types
-   Automatic default value handling
-   Debug logging for troubleshooting
-   Type-safe accessors

### 2. Event-Driven UI Updates

**Timer-Based Updates**: UI updates driven by Windows Forms Timer events.

-   Separates timing logic from display logic
-   Consistent update intervals
-   Thread-safe UI updates

### 3. Resource Management Pattern

**Deterministic Cleanup**: Proper disposal of timers and resources.

```csharp
protected override void OnFormClosing(FormClosingEventArgs e)
{
    // Save state before cleanup
    SettingsManager.SaveWindowPosition(this.Location);

    // Dispose resources
    uiUpdateTimer?.Stop();
    uiUpdateTimer?.Dispose();
}
```

## Component Relationships

### Core Dependencies

1. **Program → InputDeviceManager**: Device enumeration and selection
2. **Program → SettingsManager**: Load saved device preference
3. **Program → StopwatchWindow**: Main application window
4. **StopwatchWindow → SettingsManager**: Load/save all user preferences
5. **StopwatchWindow → NativeMethods**: Windows API calls
6. **SettingsManager → NativeMethods**: INI file operations

### Data Flow

1. **Startup**: Program → InputDeviceManager → SettingsManager → StopwatchWindow
2. **Input Processing**: Raw Input → StopwatchWindow → Stopwatch Logic
3. **Idle Detection**: Timer → GetLastInputInfo → Pause/Resume Logic
4. **Settings Persistence**: User Actions → SettingsManager → INI File

## Critical Implementation Paths

### 1. Device Selection Flow

```
Application Start
├── Enumerate Available Mice (InputDeviceManager)
├── Check Saved Device (SettingsManager)
├── If Found: Use Saved Device
└── If Not Found: Show Selection Dialog
    ├── User Selects Device
    ├── Save Selection (SettingsManager)
    └── Continue with Selected Device
```

### 2. Mouse Movement Detection

```
Raw Input Message (WM_INPUT)
├── Extract Device Handle
├── Check if Matches Target Device
├── If Match: Extract Movement Data
├── If Movement > 0: Reset Stopwatch
└── Update UI
```

### 3. Idle State Management

```
Idle Check Timer (1 second interval)
├── Get System Idle Time
├── Compare to Configured Threshold
├── If Idle: Pause Stopwatch (if running)
├── If Active: Resume Stopwatch (if paused by idle)
└── Update Visual State
```

## Error Handling Strategy

### 1. Graceful Degradation

-   If device selection fails: Show error and exit cleanly
-   If settings file is corrupted: Use defaults and recreate
-   If Raw Input registration fails: Throw with detailed error

### 2. User Communication

-   Console output for debugging (visible in development)
-   MessageBox for critical errors that require user attention
-   Silent handling for recoverable issues

### 3. State Recovery

-   Window position validation (ensure on-screen)
-   Font loading with fallback to defaults
-   Device handle validation on startup
