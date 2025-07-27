using UnityEngine;

namespace UnityGlobalHook.Samples
{
    /// <summary>
    /// A sample script that subscribes to and logs events published by GlobalInputHook.
    /// Used for verifying the package's functionality and as an example of usage.
    /// </summary>
    public class GlobalInputLogger : MonoBehaviour
    {
        void Start()
        {
            GlobalInputHook.StartHooks();
            GlobalInputHook.OnKeyboardEvent += HandleKeyboardEvent;
            GlobalInputHook.OnMouseEvent += HandleMouseEvent;
        }

        void OnDestroy()
        {
            GlobalInputHook.OnKeyboardEvent -= HandleKeyboardEvent;
            GlobalInputHook.OnMouseEvent -= HandleMouseEvent;
            Debug.Log("GlobalInputLogger: Unsubscribed from events.");

            // Call StopHooks if global hooks are no longer needed for the entire application.
            // (Typically, the internal HookDispatcher automatically calls it when the application quits,
            // so explicit calls are usually not necessary. However, you might call it
            // in specific situations where you want to stop the hooks earlier.)
            // EC2K.UnityGlobalHook.GlobalInputHook.StopHooks();
        }

        // Handles incoming global keyboard events.
        void HandleKeyboardEvent(GlobalKeyboardEventData eventData)
        {
            if (eventData.IsKeyDown)
                Debug.Log($"[Global KeyDown] Key: {eventData.KeyCode}, Modifiers: {eventData.Modifiers}");
            else
                Debug.Log($"[Global KeyUp] Key: {eventData.KeyCode}");

            // Example: Consuming an event for a specific key combination (commented out).
            // This script itself does not consume events, but demonstrates the possibility.
            // if (eventData.KeyCode == GlobalKeyCode.F12 && eventData.Modifiers.HasFlag(ModifierKeysState.Control))
            // {
            //     eventData.IsHandled = true; // Set to true if you want to consume the event, requires handling in the callback.
            // }
        }

        void HandleMouseEvent(GlobalMouseEventData eventData)
        {
            if (eventData.IsMove)
                Debug.Log($"[Global MouseMove] X: {eventData.X}, Y: {eventData.Y}");
            else if (eventData.IsWheel)
                Debug.Log($"[Global MouseWheel] Scroll: {eventData.DeltaWheel}, X: {eventData.X}, Y: {eventData.Y}");
            else if (eventData.IsMouseDown)
                Debug.Log($"[Global MouseDown] Button: {eventData.Button}, X: {eventData.X}, Y: {eventData.Y}");
            else
                Debug.Log($"[Global MouseUp] Button: {eventData.Button}, X: {eventData.X}, Y: {eventData.Y}");
        }
    }
}
