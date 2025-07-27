using System;

namespace EC2K.UnityGlobalHook
{
    [Flags]
    public enum ModifierKeysState : byte
    {
        None = 0,
        Alt = 1,
        Control = 2,
        Shift = 4,
    }

    // Global virtual key codes (based on Windows VK_CODE).
    // Add other virtual key codes as needed.
    public enum GlobalKeyCode : uint
    {
        LButton = 0x01, RButton = 0x02, MButton = 0x04,
        XButton1 = 0x05, XButton2 = 0x06,
        Back = 0x08, Tab = 0x09, Clear = 0x0C, Return = 0x0D,
        Shift = 0x10, Control = 0x11, Menu = 0x12, Pause = 0x13, CapsLock = 0x14,
        Escape = 0x1B, Space = 0x20,
        Prior = 0x21, Next = 0x22, End = 0x23, Home = 0x24, Left = 0x25, Up = 0x26, Right = 0x27, Down = 0x28,
        D0 = 0x30, D1 = 0x31, D2 = 0x32, D3 = 0x33, D4 = 0x34, D5 = 0x35, D6 = 0x36, D7 = 0x37, D8 = 0x38, D9 = 0x39,
        A = 0x41, B = 0x42, C = 0x43, D = 0x44, E = 0x45, F = 0x46, G = 0x47, H = 0x48, I = 0x49, J = 0x4A, K = 0x4B, L = 0x4C, M = 0x4D, N = 0x4E, O = 0x4F,
        P = 0x50, Q = 0x51, R = 0x52, S = 0x53, T = 0x54, U = 0x55, V = 0x56, W = 0x57, X = 0x58, Y = 0x59, Z = 0x5A,
        F1 = 0x70, F2 = 0x71, F3 = 0x72, F4 = 0x73, F5 = 0x74, F6 = 0x75, F7 = 0x76, F8 = 0x77, F9 = 0x78, F10 = 0x79, F11 = 0x7A, F12 = 0x7B,
        LShift = 0xA0, RShift = 0xA1, LControl = 0xA2, RControl = 0xA3, LMenu = 0xA4, RMenu = 0xA5,
    }

    public enum GlobalMouseButton
    {
        None,
        Left,
        Right,
        Middle,
        XButton1,
        XButton2
    }

    public class GlobalKeyboardEventData
    {
        public GlobalKeyCode KeyCode { get; internal set; }
        public ModifierKeysState Modifiers { get; internal set; }
        public bool IsKeyDown { get; internal set; }
        public bool IsHandled { get; set; } = false; // Indicates if the event should be consumed by the system. (Can be set by event subscribers)
    }

    public class GlobalMouseEventData
    {
        public GlobalMouseButton Button { get; internal set; }
        public int X { get; internal set; }
        public int Y { get; internal set; }
        public int DeltaWheel { get; internal set; }
        public bool IsMouseDown { get; internal set; }
        public bool IsMove { get; internal set; }
        public bool IsWheel { get; internal set; }
        public bool IsHandled { get; set; } = false; // Indicates if the event should be consumed by the system. (Can be set by event subscribers)
    }
}
