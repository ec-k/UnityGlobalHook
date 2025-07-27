using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using UnityEngine;

namespace EC2K.UnityGlobalHook
{
    /// <summary>
    /// A Unity component that uses Windows API P/Invoke to detect global mouse and keyboard input.
    /// This functionality works even when the application is in the background.
    /// Detected events are published as public Action events on Unity's main thread.
    /// Note: This script is functional only on Windows platforms.
    /// </summary>
    public class GlobalInputHook : MonoBehaviour
    {
        public static event Action<GlobalKeyboardEventData> OnKeyboardEvent;
        public static event Action<GlobalMouseEventData> OnMouseEvent;

        static IntPtr _keyboardHookId = IntPtr.Zero;
        static IntPtr _mouseHookId = IntPtr.Zero;

        // Keep references to delegates to prevent garbage collection.
        static WinApi.HookProc _keyboardHookProc;
        static WinApi.HookProc _mouseHookProc;

        // Use ConcurrentQueue for thread-safe event queuing, as hook callbacks might be invoked from separate threads.
        static ConcurrentQueue<GlobalKeyboardEventData> _keyboardEventsQueue = new ConcurrentQueue<GlobalKeyboardEventData>();
        static ConcurrentQueue<GlobalMouseEventData> _mouseEventsQueue = new ConcurrentQueue<GlobalMouseEventData>();

        void Start()
        {
            Debug.Log("GlobalInputHook: Initializing...");

            _keyboardHookProc = KeyboardHookCallback;
            _mouseHookProc = MouseHookCallback;

            try
            {
                // Set up the keyboard hook (HookKeyboardLowLevel is a low-level global hook).
                // dwThreadId = 0 (all threads), hMod = IntPtr.Zero (hook procedure is in the executable).
                _keyboardHookId = WinApi.SetWindowsHookEx(WinApi.HookKeyboardLowLevel, _keyboardHookProc, IntPtr.Zero, 0);
                if (_keyboardHookId == IntPtr.Zero)
                {
                    Debug.LogError($"GlobalInputHook: Failed to start keyboard hook. Error code: {Marshal.GetLastWin32Error()}");
                }

                // Set up the mouse hook (HookMouseLowLevel is a low-level global hook).
                _mouseHookId = WinApi.SetWindowsHookEx(WinApi.HookMouseLowLevel, _mouseHookProc, IntPtr.Zero, 0);
                if (_mouseHookId == IntPtr.Zero)
                {
                    Debug.LogError($"GlobalInputHook: Failed to start mouse hook. Error code: {Marshal.GetLastWin32Error()}");
                }

                if (_keyboardHookId != IntPtr.Zero && _mouseHookId != IntPtr.Zero)
                {
                    Debug.Log("GlobalInputHook: Global event hooks started.");
                    Debug.Log("Note: This functionality operates even when the application is in the background.");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"GlobalInputHook: An unexpected error occurred during initialization: {e.Message}");
            }
        }

        void Update()
        {
            while (_keyboardEventsQueue.TryDequeue(out var eventData))
                OnKeyboardEvent?.Invoke(eventData);

            while (_mouseEventsQueue.TryDequeue(out var eventData))
                OnMouseEvent?.Invoke(eventData);
        }

        // CAUTION: This process is CRUCIAL for releasing hooks!
        void OnApplicationQuit()
        {
            Debug.Log("GlobalInputHook: Application quitting. Releasing global event hooks.");
            if (_keyboardHookId != IntPtr.Zero)
            {
                if (!WinApi.UnhookWindowsHookEx(_keyboardHookId))
                {
                    Debug.LogError($"GlobalInputHook: Failed to release keyboard hook. Error code: {Marshal.GetLastWin32Error()}");
                }
                _keyboardHookId = IntPtr.Zero;
            }

            if (_mouseHookId != IntPtr.Zero)
            {
                if (!WinApi.UnhookWindowsHookEx(_mouseHookId))
                {
                    Debug.LogError($"GlobalInputHook: Failed to release mouse hook. Error code: {Marshal.GetLastWin32Error()}");
                }
                _mouseHookId = IntPtr.Zero;
            }
            Debug.Log("GlobalInputHook: Global event hooks released.");
        }

        // Invoked by the OS, possibly on a non-Unity main thread
        private static IntPtr KeyboardHookCallback(int hookCode, IntPtr messageType, IntPtr dataPointer)
        {
            if (hookCode >= 0)
            {
                var keyboardHookStruct = Marshal.PtrToStructure<WinApi.KeyboardHookStruct>(dataPointer);

                var eventData = new GlobalKeyboardEventData
                {
                    KeyCode = (GlobalKeyCode)keyboardHookStruct.VirtualKeyCode,
                    IsKeyDown = (messageType == (IntPtr)WinApi.MessageKeydown || messageType == (IntPtr)WinApi.MessageSystemKeydown)
                };

                // Determine modifier keys (Shift, Ctrl, Alt) state using GetKeyState.
                if ((WinApi.GetKeyState((int)GlobalKeyCode.LShift) & 0x8000) != 0 || (WinApi.GetKeyState((int)GlobalKeyCode.RShift) & 0x8000) != 0)
                    eventData.Modifiers |= ModifierKeysState.Shift;
                if ((WinApi.GetKeyState((int)GlobalKeyCode.LControl) & 0x8000) != 0 || (WinApi.GetKeyState((int)GlobalKeyCode.RControl) & 0x8000) != 0)
                    eventData.Modifiers |= ModifierKeysState.Control;
                if ((WinApi.GetKeyState((int)GlobalKeyCode.LMenu) & 0x8000) != 0 || (WinApi.GetKeyState((int)GlobalKeyCode.RMenu) & 0x8000) != 0)
                    eventData.Modifiers |= ModifierKeysState.Alt;

                _keyboardEventsQueue.Enqueue(eventData);

                // If eventData.IsHandled is true (set by a main thread subscriber),
                // you could return (IntPtr)1 here to consume the event and prevent it from propagating.
                // Example: if (eventData.IsHandled) return (IntPtr)1;
                // Current behavior: Always passes to the next hook.
            }

            // Pass the event to the next hook procedure.
            return WinApi.CallNextHookEx(_keyboardHookId, hookCode, messageType, dataPointer);
        }

        private static IntPtr MouseHookCallback(int hookCode, IntPtr messageType, IntPtr dataPointer)
        {
            if (hookCode >= 0)
            {
                var mouseHookStruct = Marshal.PtrToStructure<WinApi.MouseHookStruct>(dataPointer);

                var eventData = new GlobalMouseEventData
                {
                    X = mouseHookStruct.Pt.X,
                    Y = mouseHookStruct.Pt.Y,
                    IsMove = (messageType == (IntPtr)WinApi.MessageMousemove),
                    IsWheel = (messageType == (IntPtr)WinApi.MessageMousewheel)
                };

                switch ((uint)messageType)
                {
                    case WinApi.MessageLButtonDown:
                        eventData.Button = GlobalMouseButton.Left;
                        eventData.IsMouseDown = true;
                        break;
                    case WinApi.MessageLButtonUp:
                        eventData.Button = GlobalMouseButton.Left;
                        eventData.IsMouseDown = false;
                        break;
                    case WinApi.MessageRButtonDown:
                        eventData.Button = GlobalMouseButton.Right;
                        eventData.IsMouseDown = true;
                        break;
                    case WinApi.MessageRButtonUp:
                        eventData.Button = GlobalMouseButton.Right;
                        eventData.IsMouseDown = false;
                        break;
                    case WinApi.MessageMButtonDown:
                        eventData.Button = GlobalMouseButton.Middle;
                        eventData.IsMouseDown = true;
                        break;
                    case WinApi.MessageMButtonUp:
                        eventData.Button = GlobalMouseButton.Middle;
                        eventData.IsMouseDown = false;
                        break;
                    case WinApi.MessageXButtonDown:
                        eventData.IsMouseDown = true;
                        eventData.Button = ((mouseHookStruct.MouseData >> 16) == 1) ? GlobalMouseButton.XButton1 : GlobalMouseButton.XButton2;
                        break;
                    case WinApi.MessageXButtonUp:
                        eventData.IsMouseDown = false;
                        eventData.Button = ((mouseHookStruct.MouseData >> 16) == 1) ? GlobalMouseButton.XButton1 : GlobalMouseButton.XButton2;
                        break;
                    case WinApi.MessageMousewheel:
                        eventData.DeltaWheel = (short)(mouseHookStruct.MouseData >> 16);
                        break;
                }

                _mouseEventsQueue.Enqueue(eventData);

                // If eventData.IsHandled is true, you could return (IntPtr)1 here to consume the event.
                // Example: if (eventData.IsHandled) return (IntPtr)1;
                // Current behavior: Always passes to the next hook.
            }

            // Pass the event to the next hook procedure.
            return WinApi.CallNextHookEx(_mouseHookId, hookCode, messageType, dataPointer);
        }
    }
}