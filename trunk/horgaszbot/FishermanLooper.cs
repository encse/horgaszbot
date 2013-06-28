using System;
using System.Threading;

namespace horgaszbot
{
    class FishermanLooper
    {
        private readonly Fisherman fisherman;

        private Thread threadWorker;
        public object objLock = new object();

        public FishermanLooper(Fisherman fisherman)
        {
            this.fisherman = fisherman;
        }

        private readonly ManualResetEvent aresmStop = new ManualResetEvent(false);

        public void Start()
        {
            lock(objLock)
            {
                if (threadWorker == null)
                {
                    threadWorker = new Thread(Loop);
                    threadWorker.Start();
                }
            }
        }

        public void Stop()
        {
            lock(objLock)
            {
                if (threadWorker != null)
                {
                    aresmStop.Set();
                    threadWorker.Join();
                    threadWorker = null;
                }
            }
        }

        private void Loop()
        {
            while(!FStopReq())
            {
                try
                {
                    Console.WriteLine("CatchAFish  start");
                    fisherman.CatchAFish(FStopReq);
                    Console.WriteLine("CatchAFish  end");
                }
                catch(Actorer er)
                {
                    Console.Write("x");
                    Thread.Sleep(1000);
                }
                catch(Exception er)
                {
                    Console.WriteLine(er);
                    Thread.Sleep(1000);
                }
            }
            aresmStop.Reset();
        }
           
        private bool FStopReq()
        {
            return aresmStop.WaitOne(1);
        }
    }
}