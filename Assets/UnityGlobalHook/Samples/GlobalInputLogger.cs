using UnityEngine;

namespace EC2K.UnityGlobalHook.Samples
{
    /// <summary>
    /// A sample script that subscribes to and logs events published by GlobalInputHook.
    /// Used for verifying the package's functionality and as an example of usage.
    /// </summary>
    [RequireComponent(typeof(GlobalInputHook))]
    public class GlobalInputLogger : MonoBehaviour
    {
        // Subscribe to GlobalInputHook events when this component is enabled.
        void OnEnable()
        {
            GlobalInputHook.OnKeyboardEvent += HandleKeyboardEvent;
            GlobalInputHook.OnMouseEvent += HandleMouseEvent;
            Debug.Log("GlobalInputLogger: Subscribed to events.");
        }

        // Unsubscribe from GlobalInputHook events when this component is disabled or destroyed.
        // This is crucial to prevent memory leaks.
        void OnDisable()
        {
            GlobalInputHook.OnKeyboardEvent -= HandleKeyboardEvent;
            GlobalInputHook.OnMouseEvent -= HandleMouseEvent;
            Debug.Log("GlobalInputLogger: Unsubscribed from events.");
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
