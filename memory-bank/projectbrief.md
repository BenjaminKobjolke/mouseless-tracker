# Project Brief: Mouseless Tracker

## Core Requirements

The Mouseless Tracker is a Windows desktop application designed to encourage keyboard-centric workflows by tracking how long a user hasn't used their mouse.

### Primary Goals

1. **Mouse Usage Tracking**: Monitor when the user last moved their physical mouse
2. **Visual Feedback**: Display real-time tracking via an always-on-top stopwatch window
3. **Device Specificity**: Track only a specific physical mouse device, ignoring virtual mice and other input devices
4. **Idle Detection**: Automatically pause tracking when the system is idle to avoid false positives
5. **Configurability**: Allow users to customize idle thresholds and other settings
6. **Persistence**: Remember user preferences and window positioning between sessions

### Key Features

-   **Real-time Display**: Small, unobtrusive stopwatch showing time since last mouse movement
-   **Device Selection**: One-time setup to choose which physical mouse to monitor
-   **Configurable Idle Threshold**: User-definable system idle time before pausing tracker
-   **Persistent Settings**: INI file-based configuration for all user preferences
-   **Lightweight UI**: Transparent, draggable window with minimal system impact

### Technical Constraints

-   **Platform**: Windows 10/11 only
-   **Framework**: .NET 9.0 with Windows Forms
-   **Input Monitoring**: Must use Raw Input API for precise device tracking
-   **Settings Storage**: INI file format using Windows API
-   **UI Requirements**: Always-on-top, transparent background, keyboard navigation support

### Success Criteria

1. Accurately tracks mouse movement from specific physical device
2. Pauses tracking appropriately during system idle periods
3. Maintains user preferences across application restarts
4. Provides intuitive, non-intrusive user experience
5. Minimal performance impact on system resources

### Scope Boundaries

**In Scope:**

-   Mouse movement tracking for single selected device
-   System idle detection and handling
-   Basic window positioning and font customization
-   INI-based configuration management

**Out of Scope:**

-   Multi-device tracking
-   Advanced analytics or reporting
-   Network connectivity or cloud sync
-   Complex UI themes or customization
-   Integration with other productivity tools
