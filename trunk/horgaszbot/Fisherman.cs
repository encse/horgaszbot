using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using AForge.Imaging;
using AForge.Imaging.Filters;
using CoreAudioApi;

namespace horgaszbot
{
    class Fisherman
    {
        private Actor actor;
        private readonly Action<Bitmap> dgTsto;

        public Fisherman(Actor actor, Action<Bitmap> dgTsto)
        {
            this.actor = actor;
            this.dgTsto = dgTsto;
        }

        private Random r = new Random();

        public void CatchAFish(Func<bool> dgFStopReq)
        {
            if (r.Next(5) == 0)
                actor.Jump();

            var imageBefore = actor.Watch();

            actor.CastFishingLine();

            var imageAfter = actor.Watch();

            var rgrectBobber = RgrectBobberCandidate(imageBefore, imageAfter);

            var ptBobber = PtBobberFromCandidates(actor, rgrectBobber);

            Tsto(imageAfter, rgrectBobber, ptBobber);

            if (ptBobber == null)
            {
                Console.WriteLine("can't find bobbler");
                return;
            }

            if(FWaitForFish(dgFStopReq))
            {
                Console.WriteLine("I hear a fish");
                actor.CatchFish(ptBobber.Value);
            }
            else
            {
                actor.Jump();
            }
        }

        private bool FWaitForFish(Func<bool> dgFStopReq)
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
                Console.Write(".");

                if (dgFStopReq())
                    break;
            }
            
            return false;
        }

        private Point? PtBobberFromCandidates(Actor actor, List<Rectangle> rgrectBobber)
        {
            foreach (var rect in rgrectBobber)
            {
                var pt = new Point((rect.Left + rect.Right) / 2, (rect.Top + rect.Bottom) / 2);

                if (actor.FBobber(pt))
                {
                    //   var cursor = actor.CursorGet(pt.X, pt.Y);

                    //pictureBox2.Image = new Bitmap(200, 200);
                    //using (var g = Graphics.FromImage(pictureBox2.Image))
                    //{
                    //    cursor.Draw(g, new Rectangle(0, 0, 100, 100));
                    //}
                    //pictureBox2.Image.Save("x.bmp", ImageFormat.Bmp);

                    return pt;
                }
            }
            return null;
        }

        private void Tsto(Bitmap bmp, IEnumerable<Rectangle> rgrect, Point? ptBobber)
        {
            var bmpDst = new Bitmap(bmp);

            using (var g = Graphics.FromImage(bmpDst))
            {

                foreach (var rect in rgrect)
                    g.DrawRectangle(Pens.White, rect);

                if (ptBobber != null)
                    g.FillEllipse(Brushes.Red, ptBobber.Value.X - 5, ptBobber.Value.Y - 5, 10, 10);
            }
            dgTsto(bmpDst);
        }
     
        private List<Rectangle> RgrectBobberCandidate(Bitmap imgBefore, Bitmap imgAfter)
        {
            var img = XXX(imgBefore, imgAfter);
            var blobCounter = new BlobCounter();
            blobCounter.ProcessImage(img);

            return blobCounter.GetObjectsRectangles().ToList();
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
    }
}