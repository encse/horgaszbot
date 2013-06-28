using System;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

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
                var hwndWow = WowLocator.HwndFind();
              
                var bmpAncor = new Bitmap(System.Reflection.Assembly.GetEntryAssembly().GetManifestResourceStream("horgaszbot.ancor.bmp"));
                var actor = new Actor(hwndWow, bmpAncor, DgSendKeys);

                fishermanLooper = new FishermanLooper(new Fisherman(actor, RefreshTsto));
                fishermanLooper.Start();
            }
            catch (Exception er)
            {
                Console.WriteLine("cannot start fishing\n" + er.Message);
            }
            Console.WriteLine("end start");
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
    }
}
