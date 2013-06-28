using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Threading;
using AForge;
using AForge.Imaging;
using AForge.Imaging.Filters;
using CoreAudioApi;
using Point = System.Drawing.Point;

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

            var bobberTracker = new BobberTracker(actor.Watch(), ptBobber.Value, actor, dgTsto);
            try
            {
                bobberTracker.Run();

                if(FWaitForFish(dgFStopReq))
                {
                    Console.WriteLine("I hear a fish");
                    actor.CatchFish(bobberTracker.PtBobber);
                }
                else
                {
                    actor.Jump();
                }
            }
            finally
            {
                bobberTracker.Stop();
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
                //Thread.Sleep(100);
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
                    return pt;
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


        public void Boo()
        {
            
        }
    }

    class BobberTracker
    {
        private Bitmap bmpLast;
        private Point? optBobber;
        private Actor actor;
        private readonly Action<Bitmap> dgTsto;
        public Point PtBobber { get { return optBobber.Value; } }

        public BobberTracker(Bitmap bmp, Point ptBobber, Actor actor, Action<Bitmap> dgTsto)
        {
            bmpLast = Filter(bmp);
            optBobber = ptBobber;
            this.actor = actor;
            this.dgTsto = dgTsto;
        }

        public Point? Refresh()
        {
            const int block = 30;

            if (optBobber == null)
                return null;

            var points = new List<IntPoint> {new IntPoint(optBobber.Value.X, optBobber.Value.Y)};

            var bmpCurrent = Filter(actor.Watch());
            var bm = new ExhaustiveBlockMatching(block, 100);

            var rgmatch = bm.ProcessImage(bmpLast, points, bmpCurrent).OrderBy(s => s.Similarity);
              

            var optBobberNew = rgmatch.Any()
                                   ? new Point((int) Math.Round(rgmatch.Average(match => match.MatchPoint.X)),
                                               (int) Math.Round(rgmatch.Average(match => match.MatchPoint.Y)))
                                   : (Point?)null;

            var tstoImage = new Bitmap(bmpCurrent);

            if (optBobberNew != null)
            { 
                optBobber = optBobberNew;

                using (var g = Graphics.FromImage(tstoImage))
                {
                    foreach (var match in rgmatch)
                    {
                        g.DrawRectangle(Pens.Yellow, match.MatchPoint.X - block / 2, match.MatchPoint.Y - block / 2, block, block);
                        g.DrawLine(Pens.Red, new Point(match.SourcePoint.X, match.SourcePoint.Y), new Point(match.MatchPoint.X, match.MatchPoint.Y));
                    }
                }
            }
            else
                optBobber = null;

            bmpLast = bmpCurrent;
            dgTsto(tstoImage);
            return optBobber;
        }
            
        Bitmap Filter(Bitmap bmp)
        {
            return ApplyAll(bmp, new Grayscale(0, 0, 0.5), new Threshold(20), new HomogenityEdgeDetector());
        }

        Bitmap ApplyAll(Bitmap bmp, params IFilter[] rgfilter)
        {
            return rgfilter.Aggregate(bmp, (current, filter) => filter.Apply(current));
        }


        private Thread thread;
        private AutoResetEvent aresmStop = new AutoResetEvent(false);
        public void Run()
        {
            thread = new Thread(() =>
                                    {
                                        while (!aresmStop.WaitOne(1))
                                            Refresh();
                                    });
            thread.Start();
        }

        public void Stop()
        {
            aresmStop.Set();
            thread.Join();
        }
    }
}