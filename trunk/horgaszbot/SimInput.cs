using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace horgaszbot
{
    public static class SimInput
    {
        [DllImport("user32.dll")]
        static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

        [Flags]
        public enum MouseEventFlags : uint
        {
            Move = 0x0001,
            LeftDown = 0x0002,
            LeftUp = 0x0004,
            RightDown = 0x0008,
            RightUp = 0x0010,
            MiddleDown = 0x0020,
            MiddleUp = 0x0040,
            Absolute = 0x8000
        }

        public static void MouseEvent(MouseEventFlags e, uint x, uint y)
        {
            mouse_event((uint)e, x, y, 0, UIntPtr.Zero);
        }
        public static void RightClick(int x, int y)
        {
            var scr = Screen.PrimaryScreen.Bounds;
            MouseEvent(MouseEventFlags.RightDown | MouseEventFlags.RightUp | MouseEventFlags.Move | MouseEventFlags.Absolute,
                       (uint)(x * 65535 / scr.Width),
                       (uint)(y * 65535 / scr.Height));
        }


    }
}