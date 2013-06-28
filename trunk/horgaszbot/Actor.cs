using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace horgaszbot
{
    class Actorer : Exception
    {
        
    }

    class Actor
    {
        private readonly IntPtr hwnd;
        private readonly Bitmap bmpAncor;
        private readonly Action<string> dgSendKeys;
        private readonly Cursor cursorNoFish;

        public Actor(IntPtr hwnd, Bitmap bmpAncor, Action<string> dgSendKeys)
        {
            this.hwnd = hwnd;
            this.bmpAncor = bmpAncor;
            this.dgSendKeys = dgSendKeys;

            cursorNoFish = CursorGet(0, 0);
        }

        public void Jump()
        {
            CheckActiveness();
            Console.WriteLine("jump");
            dgSendKeys(" ");
            Thread.Sleep(1000);
        }

        private void CheckActiveness()
        {
            if (User32.GetForegroundWindow() != hwnd)
                throw new Actorer();
        }

        public void CastFishingLine()
        {
            CheckActiveness();
            dgSendKeys("1");
            Thread.Sleep(2000);
        }

        public void CatchFish(Point pt)
        {
            CheckActiveness();

            var point = new User32.POINT { x = pt.X, y = pt.Y };
            User32.ClientToScreen(hwnd, ref point);
           
                SimInput.RightClick(point.x, point.y);
            Thread.Sleep(3000);
        }

        public Bitmap Watch()
        {
            return new Bitmap(new ScreenCapture().CaptureWindow(hwnd));
        }

        public bool FBobber(Point pt)
        {
            CheckActiveness();

            var cursor = CursorGet(pt.X, pt.Y);

            if (cursorNoFish.Handle == cursor.Handle)
                return false;

            var bmp = new Bitmap(32, 32);
            using (var g = Graphics.FromImage(bmp))
                cursor.Draw(g, new Rectangle(0, 0, 32, 32));
            for (int x = 0; x < 32; x++)
                for (int y = 0; y < 32; y++)
                    if (bmpAncor.GetPixel(x, y).GetHue() != bmp.GetPixel(x, y).GetHue())
                        return false;
            return true;
        }

        public Cursor CursorGet(int x, int y)
        {

            var pt = new User32.POINT { x = x, y = y };

            User32.ClientToScreen(hwnd, ref pt);
            User32.SetCursorPos(pt.x, pt.y);

            Thread.Sleep(50);
            var info = new User32.CursorInfo();
            info.Size = Marshal.SizeOf(info.GetType());


            if (User32.GetCursorInfo(out info))
                return new Cursor(info.Handle);

            return null;
        }
    }
}