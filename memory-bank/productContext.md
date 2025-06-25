# Product Context: Mouseless Tracker

## Why This Project Exists

The Mouseless Tracker addresses a specific productivity challenge: helping users develop and maintain keyboard-centric workflows by providing awareness of their mouse usage patterns.

### Problem Statement

Many productivity-focused users and developers want to minimize mouse usage to:

-   Increase typing speed and workflow efficiency
-   Reduce repetitive strain injuries from mouse movements
-   Develop muscle memory for keyboard shortcuts
-   Maintain focus without hand movement between keyboard and mouse

However, users often unconsciously reach for the mouse without realizing how frequently they break their keyboard-focused flow.

### Solution Approach

The Mouseless Tracker provides **passive awareness** through:

-   Real-time visual feedback showing time since last mouse movement
-   Unobtrusive display that doesn't interrupt workflow
-   Device-specific tracking to avoid false positives from virtual mice
-   Smart idle detection to pause during breaks

## How It Should Work

### User Experience Flow

1. **Initial Setup**

    - User launches application for first time
    - System presents list of connected mouse devices
    - User selects their primary physical mouse
    - Tracker window appears and begins monitoring

2. **Daily Usage**

    - Small stopwatch displays in corner of screen
    - Timer resets to 00:00:00 when user moves selected mouse
    - Timer continues counting when user uses keyboard only
    - Timer pauses during system idle periods (configurable)

3. **Customization**
    - User can drag window to preferred position
    - Position is remembered between sessions
    - Idle threshold can be adjusted via settings file
    - Font preferences are preserved

### Behavioral Goals

**Encourage Keyboard Usage**: By showing real-time feedback, users become more conscious of when they reach for the mouse and may choose keyboard alternatives.

**Avoid Guilt/Pressure**: The tracker is informational, not judgmental. It doesn't set goals or provide negative feedback.

**Maintain Workflow**: The display is designed to be visible but not distracting, allowing users to glance at it without losing focus.

## User Experience Goals

### Primary Experience Principles

1. **Unobtrusive Awareness**: Information is available at a glance without demanding attention
2. **Accurate Tracking**: Only counts actual mouse usage, not system artifacts
3. **Persistent Behavior**: Works consistently across sessions and system states
4. **Minimal Configuration**: Works well out-of-the-box with optional customization

### Success Metrics

-   Users report increased awareness of mouse usage patterns
-   Application runs reliably without crashes or performance issues
-   Settings and preferences persist correctly across sessions
-   Idle detection works accurately without false pauses or missed activity

### Anti-Goals

-   **Not a Productivity Timer**: This isn't about measuring work time or productivity
-   **Not Gamification**: No scores, achievements, or competitive elements
-   **Not Restrictive**: Doesn't block or limit mouse usage in any way
-   **Not Complex**: Avoids advanced features that complicate the core experience
