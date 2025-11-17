using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace HRead
{
    public class HotkeyManager : IDisposable
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        private const int WM_HOTKEY = 0x0312;
        private const uint MOD_CONTROL = 0x0002;

        private const byte VK_CONTROL = 0x11;
        private const byte VK_C = 0x43;
        private const uint KEYEVENTF_KEYUP = 0x0002;

        public event Action<int> HotkeyPressed;

        private readonly IntPtr _handle;
        private bool _disposed = false;

        public HotkeyManager(IntPtr formHandle)
        {
            _handle = formHandle;
        }

        public bool RegisterHotkey(int id, Keys key)
        {
            return RegisterHotKey(_handle, id, MOD_CONTROL, (uint)key);
        }

        public bool UnregisterHotkey(int id)
        {
            return UnregisterHotKey(_handle, id);
        }

        public void ProcessMessage(Message m)
        {
            if (m.Msg == WM_HOTKEY)
            {
                HotkeyPressed?.Invoke(m.WParam.ToInt32());
            }
        }

        public void SimulateCopy()
        {
            keybd_event(VK_CONTROL, 0, 0, UIntPtr.Zero);
            keybd_event(VK_C, 0, 0, UIntPtr.Zero);
            keybd_event(VK_C, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                // Unregister all hotkeys here if needed
                _disposed = true;
            }
        }
    }
}