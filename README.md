# Unity Global Hook

A Unity package providing global keyboard and mouse input detection for Windows, using P/Invoke to interact with the Windows API. It allows your Unity application to receive input events even when it's in the background.

## Features
Captures keyboard and mouse events across the entire Windows system, regardless of whether your Unity application is in focus or running in the background.


## Installation

You can install this package via Git URL.

1.  Open your Unity project.
2.  Navigate to `Window > Package Manager`.
3.  Click the `+` button in the top-left corner of the Package Manager window.
4.  Select `Add package from git URL...`.
5.  Enter the following URL:
    ```
    https://github.com/ec-k/UnityGlobalHook.git?path=/Assets/UnityGlobalHook
    ```
6.  Click `Add`. The package will be downloaded and added to your project.


## Usage

1.  Create an empty GameObject in your Unity scene (e.g., named "Global Input Manager").
2.  Attach the `GlobalInputHook.cs` script to this GameObject.
3.  In any other script where you want to receive global input events, subscribe to the static `GlobalInputHook.OnKeyboardEvent` and `GlobalInputHook.OnMouseEvent` actions.

    ```csharp
    using UnityEngine;
    // Don't forget to unsubscribe in OnDisable to prevent memory leaks!
    public class MyInputHandler : MonoBehaviour
    {
        void OnEnable()
        {
            GlobalInputHook.OnKeyboardEvent += HandleKeyboardInput;
            GlobalInputHook.OnMouseEvent += HandleMouseInput;
        }

        void OnDisable()
        {
            GlobalInputHook.OnKeyboardEvent -= HandleKeyboardInput;
            GlobalInputHook.OnMouseEvent -= HandleMouseInput;
        }

        void HandleKeyboardInput(GlobalKeyboardEventData eventData)
        {
            Debug.Log($"Key: {eventData.KeyCode}, Down: {eventData.IsKeyDown}, Modifiers: {eventData.Modifiers}");
            // Example: Consume Ctrl+F12 to prevent it from reaching other applications
            // if (eventData.KeyCode == GlobalKeyCode.F12 && eventData.Modifiers.HasFlag(ModifierKeysState.Control))
            // {
            //     eventData.IsHandled = true; // Set this flag. Handling in WinApi.HookProc is required for actual consumption.
            // }
        }

        void HandleMouseInput(GlobalMouseEventData eventData)
        {
            if (eventData.IsMove)
            {
                Debug.Log($"Mouse Move: X={eventData.X}, Y={eventData.Y}");
            }
            else if (eventData.IsWheel)
            {
                Debug.Log($"Mouse Wheel: Delta={eventData.DeltaWheel}");
            }
            else // Mouse Down/Up
            {
                Debug.Log($"Mouse Button: {eventData.Button}, Down: {eventData.IsMouseDown}, X={eventData.X}, Y={eventData.Y}");
            }
        }
    }
    ```

### Platform Compatibility

*   **Windows:** Fully supported (Windows 10/11 - 64-bit recommended).
*   **Other Platforms (macOS, Linux, iOS, Android, WebGL etc.):** Not supported. This package relies on Windows-specific API calls (P/Invoke).
