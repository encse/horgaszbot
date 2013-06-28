using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;

namespace horgaszbot
{
    /// <summary>
    /// Helper class containing User32 API functions
    /// </summary>
    internal class User32
    {
        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();


        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }
        [DllImport("user32.dll")]
        public static extern IntPtr GetDesktopWindow();
        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowDC(IntPtr hWnd);
        [DllImport("user32.dll")]
        public static extern IntPtr ReleaseDC(IntPtr hWnd, IntPtr hDC);
        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowRect(IntPtr hWnd, ref RECT rect);
        [DllImport("user32.dll")]
        public static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("User32.Dll")]
        public static extern bool ClientToScreen(IntPtr hWnd, ref POINT point);

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;
        }

        [DllImport("user32.dll")]
        public static extern bool EnumThreadWindows(int dwThreadId, EnumThreadDelegate lpfn, IntPtr lParam);

        public delegate bool EnumThreadDelegate(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetWindowText(IntPtr hWnd, [Out] StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        public static extern int SetForegroundWindow(IntPtr hWnd);
   

        [StructLayout(LayoutKind.Sequential)]
        public struct CursorInfo
        {
            public int Size;
            public int Flags;
            public IntPtr Handle;
            public Point Position;
        }

        [DllImport("user32.dll")]
        public static extern bool GetCursorInfo(out CursorInfo info);

      

        [DllImport("User32.Dll")]
        public static extern long SetCursorPos(int x, int y);

      
    }
}