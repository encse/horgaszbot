using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using AForge;
using AForge.Imaging;
using AForge.Imaging.Filters;

namespace horgaszbot
{
    public partial class Form1 : Form
    {
        private object objLock = new object();
        KeyboardHook hook = new KeyboardHook();
        private FishermanLooper fishermanLooper;

        public Form1()
        {
            InitializeComponent();
        }


        void Start()
        {
            Console.WriteLine("begin start");
            try
            {
                var actor = ActorCreate();

                fishermanLooper = new FishermanLooper(new Fisherman(actor, RefreshTsto));
                fishermanLooper.Start();
            }
            catch (Exception er)
            {
                Console.WriteLine("cannot start fishing\n" + er.Message);
            }
            Console.WriteLine("end start");
        }

        private Actor ActorCreate()
        {
            var hwndWow = WowLocator.HwndFind();

            var bmpAncor = BmpFromRes("horgaszbot.ancor.bmp");
            var actor = new Actor(hwndWow, bmpAncor, DgSendKeys);
            return actor;
        }

        private Bitmap BmpFromRes(string resid)
        {
            return new Bitmap(System.Reflection.Assembly.GetEntryAssembly().GetManifestResourceStream(resid));
            
        }

        private void DgSendKeys(string st)
        {
            if(InvokeRequired)
            {    
                BeginInvoke(new Action(() => DgSendKeys(st)));
                return;
            }

            
            SendKeys.Send(st);
        }


        void Stop()
        {
            Console.WriteLine("begin stop");
            fishermanLooper.Stop();
            fishermanLooper = null;
            Console.WriteLine("end stop");
        }

        private void RefreshTsto(Bitmap bmp)
        {
            if(InvokeRequired)
            {
                Invoke(new Action(() => RefreshTsto(bmp)), null);
                return;
            }
            pictureBox1.Image = bmp;
            Application.DoEvents();
        }

        private void button1_Click(object sender, EventArgs e)
        {
             lock(objLock)
            {
                if(fishermanLooper == null)
                    Start();
                else
                    Stop();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            
            var f = new Fisherman(ActorCreate(), RefreshTsto);
            User32.SetForegroundWindow(WowLocator.HwndFind());
            f.Boo();
        }
    }
}
