# Technical Context: Mouseless Tracker

## Technologies Used

### Core Framework

-   **.NET 9.0**: Latest LTS version with Windows Forms support
-   **Windows Forms**: Native Windows UI framework for desktop applications
-   **C# 12**: Latest language features with nullable reference types enabled

### Windows APIs

-   **Raw Input API**: Device-specific input monitoring
-   **GetLastInputInfo**: System idle time detection
-   **SetWindowPos**: Window positioning and topmost behavior
-   **INI File APIs**: Configuration persistence (GetPrivateProfileString, WritePrivateProfileString)

### Development Tools

-   **Visual Studio Code**: Primary development environment
-   **.NET CLI**: Build and project management
-   **MSBuild**: Compilation and packaging

## Development Setup

### Project Structure

```
mouseless-tracker/
├── Program.cs                 # Application entry point
├── StopwatchWindow.cs         # Main UI window
├── SettingsManager.cs         # Configuration management
├── InputDeviceManager.cs      # Device enumeration
├── NativeMethods.cs          # Windows API interop
├── MouselessTracker.csproj   # Project configuration
├── mouseless-tracker.sln     # Solution file
├── settings.ini              # User configuration
└── memory-bank/              # Documentation
```

### Build Configuration

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net9.0-windows</TargetFramework>
    <UseWindowsForms>true</UseWindowsForms>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
```

### Build Commands

-   **Debug Build**: `dotnet build mouseless-tracker.sln`
-   **Release Build**: `dotnet build mouseless-tracker.sln --configuration Release`
-   **Run**: `dotnet run --project MouselessTracker.csproj`

## Technical Constraints

### Platform Requirements

-   **Operating System**: Windows 10/11 (Raw Input API requirements)
-   **Runtime**: .NET 9.0 Desktop Runtime
-   **Architecture**: x64 (primary), x86 (compatible)

### Performance Constraints

-   **Memory Usage**: < 50MB typical working set
-   **CPU Usage**: < 1% during normal operation
-   **Startup Time**: < 2 seconds from launch to functional
-   **Response Time**: < 100ms for mouse movement detection

### System Integration

-   **Raw Input Registration**: Must handle device enumeration and registration
-   **Window Management**: Always-on-top behavior without interfering with other applications
-   **Settings Persistence**: INI file must be writable in application directory

## Dependencies

### Runtime Dependencies

-   **.NET 9.0 Desktop Runtime**: Required for Windows Forms applications
-   **Windows API**: Native system calls through P/Invoke

### Development Dependencies

-   **.NET 9.0 SDK**: For building and development
-   **Windows SDK**: For Windows API definitions (implicit)

### No External NuGet Packages

The application intentionally avoids external dependencies to:

-   Minimize deployment complexity
-   Reduce security surface area
-   Ensure long-term compatibility
-   Simplify troubleshooting

## Tool Usage Patterns

### Development Workflow

1. **Code Changes**: Edit source files in VS Code
2. **Build Verification**: `dotnet build` to check compilation
3. **Testing**: `dotnet run` for functional testing
4. **Release**: `dotnet build --configuration Release` for distribution

### Debugging Approach

-   **Console Output**: Debug information written to console (visible in development)
-   **Exception Handling**: Detailed error messages with context
-   **Settings Validation**: Automatic fallback to defaults on corruption

### Configuration Management

-   **INI File Format**: Human-readable configuration
-   **Automatic Defaults**: Missing settings use sensible defaults
-   **Type Safety**: Strongly-typed accessors with validation

## API Integration Patterns

### Windows Raw Input

```csharp
// Registration pattern
var rid = new RAWINPUTDEVICE[1];
rid[0].UsagePage = 0x01; // HID_USAGE_PAGE_GENERIC
rid[0].Usage = 0x02;     // HID_USAGE_GENERIC_MOUSE
rid[0].Flags = RIDEV_INPUTSINK;
rid[0].Target = windowHandle;
RegisterRawInputDevices(rid, ...);

// Processing pattern
protected override void WndProc(ref Message m)
{
    if (m.Msg == WM_INPUT)
    {
        // Extract and process raw input data
    }
}
```

### Settings Persistence

```csharp
// Write pattern
WritePrivateProfileString(section, key, value, filePath);

// Read pattern
GetPrivateProfileString(section, key, defaultValue, buffer, bufferSize, filePath);
```

### Idle Detection

```csharp
// System idle time pattern
var lastInputInfo = new LASTINPUTINFO();
lastInputInfo.cbSize = Marshal.SizeOf(lastInputInfo);
GetLastInputInfo(ref lastInputInfo);
var idleTime = Environment.TickCount - lastInputInfo.dwTime;
```

## Deployment Considerations

### Distribution Format

-   **Self-Contained**: Include .NET runtime for standalone deployment
-   **Framework-Dependent**: Smaller size, requires .NET runtime installation
-   **Single File**: Optional packaging for simplified distribution

### Installation Requirements

-   **No Registry Changes**: Application is portable
-   **File System Access**: Write access to application directory for settings
-   **Windows API Access**: Standard user permissions sufficient

### Compatibility Strategy

-   **Windows Version**: Target Windows 10 1903+ for Raw Input API stability
-   **.NET Version**: Use LTS version for long-term support
-   **Architecture**: Build for x64 primarily, x86 for compatibility

## Security Considerations

### Input Monitoring

-   **Device-Specific**: Only monitors selected mouse device
-   **No Keystroke Logging**: Does not capture keyboard input
-   **Local Processing**: No network communication or data transmission

### File System Access

-   **Application Directory**: Only writes to settings.ini in app directory
-   **No System Modification**: Does not modify registry or system files
-   **User Permissions**: Runs with standard user privileges

### Privacy

-   **No Data Collection**: Does not store or transmit usage data
-   **Local Configuration**: All settings stored locally
-   **Minimal Footprint**: Only necessary Windows API access
