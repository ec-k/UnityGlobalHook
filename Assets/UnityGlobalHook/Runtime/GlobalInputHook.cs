using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using UnityEditor.Search;
using UnityEngine;

namespace EC2K.UnityGlobalHook
{
    /// <summary>
    /// A static utility class for detecting global mouse and keyboard input.
    /// This functionality works even when the application is in the background.
    /// Detected events are published as static Action events on Unity's main thread.
    /// Note: This functionality is available only on Windows platforms.
    /// </summary>
    public class GlobalInputHook
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

        static bool _isHookActive = false;

        /// <summary>
        /// A hidden MonoBehaviour to process queued events on Unity's main thread
        /// and ensure hook release upon application quit.
        /// This class is automatically generated and managed by GlobalInputHook.StartHooks().
        /// </summary>
        class HookDispathcer : MonoBehaviour
        {
            static HookDispathcer _instance; // singleton instance to prevent memory leak
            public static HookDispathcer Instance
            {
                get
                {
                    if(_instance is null)
                    {
                        var dispathcerGO = new GameObject("GlobalInputHookDispacher");
                        dispathcerGO.hideFlags = HideFlags.HideInHierarchy;
                        DontDestroyOnLoad(dispathcerGO);
                        _instance = dispathcerGO.AddComponent<HookDispathcer>();
                    }
                    return _instance;
                }
            }

            void Awake()
            {
                // Prevent duplicate dispatchers
                if (_instance != null && _instance != this)
                {
                    Destroy(this.gameObject);
                    return;
                }
                _instance = this;
            }

            void Update()
            {
                GlobalInputHook.ProcessQueuedEvents();
            }

            void OnApplicationQuit()
            {
                GlobalInputHook.StopHooks();
            }

            void OnDestroy()
            {
                GlobalInputHook.StopHooks();
                _instance = null;
            }
        }

        /// <summary>
        /// Initializes and starts global mouse and keyboard hooks.
        /// This method should be called once at application startup (e.g., from Start() of any MonoBehaviour in the scene).
        /// </summary>
        public static void StartHooks()
        {
            if (_isHookActive)
            {
                Debug.LogWarning("GlobalInputHook: Global hooks are already active. No need to start again.");
                return;
            }

            var _ = HookDispathcer.Instance;
            Debug.Log("GlobalInputHook: Starting global hook initialization.");
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
                    _isHookActive = true;
                    Debug.Log("GlobalInputHook: Global event hooks started.");
                    Debug.Log("Note: This functionality operates even when the application is in the background.");
                }
                else
                {
                    Debug.LogError("GlobalInputHook: Neither global hook could be started.");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"GlobalInputHook: An unexpected error occurred during initialization: {e.Message}");
                StopHooks();
            }
        }

        /// <summary>
        /// Releases global mouse and keyboard hooks.
        /// Typically called when the application quits or when global input is no longer needed.
        /// </summary>
        public static void StopHooks()
        {
            // If hooks are not active or already released
            if (!_isHookActive && _keyboardHookId == IntPtr.Zero && _mouseHookId == IntPtr.Zero)
            {
                Debug.Log("GlobalInputHook: Global hooks are not active. No stop operation needed.");
                return;
            }

            Debug.Log("GlobalInputHook: Releasing global event hooks...");

            // Release keyboard hook
            if (_keyboardHookId != IntPtr.Zero)
            {
                if (!WinApi.UnhookWindowsHookEx(_keyboardHookId))
                {
                    Debug.LogError($"GlobalInputHook: Failed to release keyboard hook. Error code: {Marshal.GetLastWin32Error()}");
                }
                _keyboardHookId = IntPtr.Zero; // Clear ID
            }

            // Release mouse hook
            if (_mouseHookId != IntPtr.Zero)
            {
                if (!WinApi.UnhookWindowsHookEx(_mouseHookId))
                {
                    Debug.LogError($"GlobalInputHook: Failed to release mouse hook. Error code: {Marshal.GetLastWin32Error()}");
                }
                _mouseHookId = IntPtr.Zero; // Clear ID
            }

            _isHookActive = false;
            Debug.Log("GlobalInputHook: Global event hooks released.");
        }

        /// <summary>
        /// Processes events stored in the queue and fires corresponding Action events.
        /// This method is called from HookDispatcher's Update() on Unity's main thread.
        /// </summary>
        static void ProcessQueuedEvents()
        {
            while (_keyboardEventsQueue.TryDequeue(out var eventData))
                OnKeyboardEvent?.Invoke(eventData);

            while (_mouseEventsQueue.TryDequeue(out var eventData))
                OnMouseEvent?.Invoke(eventData);
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