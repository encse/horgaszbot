using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using AForge.Imaging;
using AForge.Imaging.Filters;
using CoreAudioApi;
using Point = System.Drawing.Point;

namespace Horgaszbot
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool fStopRequested;
        private bool fFishing;

        private IntPtr hwnd;


        public MainWindow()
        {
            InitializeComponent();
        }



        private HwndSource _source;
        private const int HOTKEY_ID = 9000;

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var helper = new WindowInteropHelper(this);
            _source = HwndSource.FromHwnd(helper.Handle);
            _source.AddHook(HwndHook);
            RegisterHotKey();
        }

        protected override void OnClosed(EventArgs e)
        {
            _source.RemoveHook(HwndHook);
            _source = null;
            UnregisterHotKey();
            base.OnClosed(e);
        }

        private void RegisterHotKey()
        {
            var helper = new WindowInteropHelper(this);
            const uint VK_F10 = 0x79;
            const uint MOD_CTRL = 0x0002;
            if (!User32.RegisterHotKey(helper.Handle, HOTKEY_ID, MOD_CTRL, VK_F10))
            {
                // handle error
            }
        }

        private void UnregisterHotKey()
        {
            var helper = new WindowInteropHelper(this);
            User32.UnregisterHotKey(helper.Handle, HOTKEY_ID);
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;
            switch (msg)
            {
                case WM_HOTKEY:
                    switch (wParam.ToInt32())
                    {
                        case HOTKEY_ID:
                            OnHotKeyPressed();
                            handled = true;
                            break;
                    }
                    break;
            }
            return IntPtr.Zero;
        }

        private void OnHotKeyPressed()
        {
            fStopRequested = true;
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (fFishing)
            {
                fStopRequested = true;
                return;
            }

            fFishing = true;
            Button1.Content = "Stop";
           
            
            var bmpAncor = BmpFromRes("Horgaszbot.ancor.bmp");

            fStopRequested = false;
            while (!fStopRequested)
            {
                DoEvents();

                hwnd = WowLocator.HwndFind();
                if (User32.GetForegroundWindow() != hwnd)
                {
                    Thread.Sleep(100);
                    continue;
                }
                var cursorHandleNotAncor = CursorHandleGet(0, 0);

                var bmp1 = new ScreenCapture().CaptureWindow(hwnd);

                CastFishingLine();
                var bmp2 = new ScreenCapture().CaptureWindow(hwnd);

                var rgrect = RgrectBobberCandidate(bmp1, bmp2);

                var bmp = Tsto(bmp2, rgrect, null);
                MemoryStream ms = new MemoryStream();
                bmp.Save(ms, ImageFormat.Png);
                ms.Position = 0;
                BitmapImage bi = new BitmapImage();
                bi.BeginInit();
                bi.StreamSource = ms;
                bi.EndInit();
                Image1.Source = bi;
                
                foreach (var rect in rgrect)
                {
                    DoEvents();
                    if (fStopRequested)
                        break;

                    var pt = new Point((rect.Left + rect.Right)/2, (rect.Top + rect.Bottom)/2);
                    if (FBobbler(pt, bmpAncor, cursorHandleNotAncor))
                    {
                        if (FWaitForFish())
                            CatchFish(pt);
                        break;
                    }
                }
            }

            fFishing = false;
            Button1.Content = "Start";
        }


        void DoEvents()
        {
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background,
                                                  new Action(delegate { }));
        }

        private bool FBobbler(Point pt, Bitmap bmpAncor, IntPtr? cursorHandleNotAncor)
        {
            var cursorHandle = CursorHandleGet(pt.X, pt.Y);
            if (cursorHandle == cursorHandleNotAncor)
                return false;

            BitmapSource biIcon;
            User32.aICONINFO icInfo;
            var hicon = User32.CopyIcon(cursorHandle.Value);
            if (User32.GetIconInfo(hicon, out icInfo))
            {
                Icon ic = System.Drawing.Icon.FromHandle(hicon);
                var bmpCursor = ic.ToBitmap();
                User32.DestroyIcon(hicon);

                for (int x = 0; x < 32; x++)
                    for (int y = 0; y < 32; y++)
                        if (bmpAncor.GetPixel(x, y).GetHue() != bmpCursor.GetPixel(x, y).GetHue())
                            return false;
            }

            return true;
        }

        private List<Rectangle> RgrectBobberCandidate(Bitmap imgBefore, Bitmap imgAfter)
        {
            var img = XXX(imgBefore, imgAfter);
            var blobCounter = new BlobCounter();
            blobCounter.ProcessImage(img);

            return blobCounter.GetObjectsRectangles().ToList();
        }

        private Bitmap Tsto(Bitmap bmp, IEnumerable<Rectangle> rgrect, Point? ptBobber)
        {
            var bmpDst = new Bitmap(bmp);

            using (var g = Graphics.FromImage(bmpDst))
            {

                foreach (var rect in rgrect)
                    g.DrawRectangle(Pens.White, rect);

                if (ptBobber != null)
                    g.FillEllipse(Brushes.Red, ptBobber.Value.X - 5, ptBobber.Value.Y - 5, 10, 10);
            }

            return bmpDst;
        }

        private Bitmap XXX(Bitmap bmpBefore, Bitmap bmpAfter)
        {
            var filter = new Grayscale(0.2125, 0.7154, 0.0721);
            bmpBefore = filter.Apply(bmpBefore);
            bmpAfter = filter.Apply(bmpAfter);

            // create filters
            var differenceFilter = new Difference();
            IFilter thresholdFilter = new Threshold(15);
            // set backgroud frame as an overlay for difference filter
            differenceFilter.OverlayImage = bmpBefore;
            // apply the filters
            Bitmap tmp1 = differenceFilter.Apply(bmpAfter);
            Bitmap tmp2 = thresholdFilter.Apply(tmp1);
            IFilter erosionFilter = new Erosion();
            // apply the filter 
            Bitmap tmp3 = erosionFilter.Apply(tmp2);

            IFilter pixellateFilter = new Pixellate();
            // apply the filter
            Bitmap tmp4 = pixellateFilter.Apply(tmp3);

            return tmp4;
        }

        public void Jump()
        {
            Console.WriteLine("jump");
            SendKey(" ");
            Thread.Sleep(1000);
        }

        public void CastFishingLine()
        {
            SendKey("1");
            Thread.Sleep(2000);
        }

        public void SendKey(string st)
        {
            User32.PostMessage(hwnd, User32.WM_KEYDOWN, new IntPtr(st[0]), IntPtr.Zero);
            User32.PostMessage(hwnd, User32.WM_KEYUP, new IntPtr(st[0]), IntPtr.Zero);
        }

        public IntPtr? CursorHandleGet(int x, int y)
        {
            var pt = new User32.POINT { x = x, y = y };

            User32.ClientToScreen(hwnd, ref pt);
            User32.SetCursorPos(pt.x, pt.y);
            Thread.Sleep(20);
            var info = new User32.CursorInfo();
            info.Size = Marshal.SizeOf(info.GetType());

            if (User32.GetCursorInfo(out info))
                return info.Handle;

            return null;
        }

        public void CatchFish(Point pt)
        {
            User32.PostMessage(hwnd, User32.WM_RBUTTONDOWN, IntPtr.Zero, new IntPtr((pt.Y << 16) + pt.X));
            Thread.Sleep(50);
            User32.PostMessage(hwnd, User32.WM_RBUTTONUP, IntPtr.Zero, new IntPtr((pt.Y << 16) + pt.X));
            Thread.Sleep(3000);
        }

        private Bitmap BmpFromRes(string resid)
        {
            return new Bitmap(Assembly.GetEntryAssembly().GetManifestResourceStream(resid));

        }


        private bool FWaitForFish()
        {
            var devEnum = new MMDeviceEnumerator();
            var defaultDevice = devEnum.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia);

            var dtStart = DateTime.Now;
            var qMpv = new Queue<float>();

            while ((DateTime.Now - dtStart).TotalSeconds < 30)
            {
                Thread.Sleep(100);
                qMpv.Enqueue(defaultDevice.AudioMeterInformation.MasterPeakValue);
                if (qMpv.Count > 5)
                    qMpv.Dequeue();
                if (qMpv.Average() > 0.15)
                    return true;
                DoEvents();
                if (fStopRequested)
                    break;
            }

            return false;
        }

    }
}
