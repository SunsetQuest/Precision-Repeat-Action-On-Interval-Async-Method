using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

// Source for framework: https://stackoverflow.com/a/22453097/2352507
// Source for Yield information: https://stackoverflow.com/a/33407181/2352507
// Matt Thomas's overall message: https://stackoverflow.com/questions/9228313/most-accurate-timer-in-net

class Program
{

    static void Main()
    {
        CancellationTokenSource cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(1000));
        Console.WriteLine("Begin: " + DateTime.Now.ToString("hh.mm.ss.ffffff"));
        RepeatActionOnIntervalAsync(SayHelloAsync(), TimeSpan.FromMilliseconds(100), cancellation.Token).Wait();
        Console.WriteLine("Finish: " + DateTime.Now.ToString("hh.mm.ss.ffffff"));
        Console.ReadKey();
    }

    public static Action SayHelloAsync() => () => Console.WriteLine("Fired at " + DateTime.Now.ToString("hh.mm.ss.ffffff"));
        
    public static async Task RepeatActionOnIntervalAsync(Action task, TimeSpan interval, CancellationToken? ct = null)
    {
        long stage1Delay = 20 ;
        long stage2Delay = 5 * TimeSpan.TicksPerMillisecond;
        bool USE_SLEEP0 = false;

        DateTime target = DateTime.Now + new TimeSpan(0, 0, 0, 0, (int)stage1Delay + 2);
        bool warmup = true;
        while (true)
        {
            // Getting closer to 'target' - Lets do the less precisces but least cpu intesive wait
            var timeLeft = target - DateTime.Now;
            if (timeLeft.TotalMilliseconds >= stage1Delay)
            {
                try
                {
                    await Task.Delay((int)(timeLeft.TotalMilliseconds - stage1Delay), ct ?? CancellationToken.None);
                }
                catch (TaskCanceledException)
                {
                    return;
                }
            }

            // Getting closer to 'target' - Lets do the semi-precise but mild cpu intesive wait - Task.Yield()
            while (DateTime.Now < target - new TimeSpan(stage2Delay))
            {
                await Task.Yield();
            }

            // Getting closer to 'target' - Lets do the semi-precise but mild cpu intesive wait - Thread.Sleep(0)
            // Note: Thread.Sleep(0) is removed below because it is sometimes looked down on and also said not good to mix 'Thread.Sleep(0)' with Tasks.
            //       However, Thread.Sleep(0) does have a quicker and more reliable turn around time then Task.Yield() so to 
            //       make up for this a longer (and more expensive) Thread.SpinWait(1) would be needed.
            if (USE_SLEEP0)
            {
                while (DateTime.Now < target - new TimeSpan(stage2Delay / 8))
                {
                    Thread.Sleep(0);
                }
            }

            // Extreamlly close to 'target' - Lets do the most precise but very cpu/battery intesive 
            while (DateTime.Now < target)
            {
                Thread.SpinWait(64);
            }

            if (!warmup)
            {
                target += interval;
                await Task.Run(task); // or your code here
            }
            else
            {
                long start1 = DateTime.Now.Ticks + ((long)interval.TotalMilliseconds * TimeSpan.TicksPerMillisecond);
                long alignVal = start1 - (start1 % ((long)interval.TotalMilliseconds * TimeSpan.TicksPerMillisecond));
                target = new DateTime(alignVal);
                warmup = false;
            }
        }
    }


    public static async Task RepeatActionOnIntervalAsync_DEBUG(Action task, TimeSpan interval, CancellationToken? ct = null)
    {
        StringBuilder log = new StringBuilder();

        long stage1Delay = 20;
        long stage2Delay = 5 * TimeSpan.TicksPerMillisecond;
        bool USE_SLEEP0 = false;

        int loops = 0;
        DateTime target = DateTime.Now + new TimeSpan(0, 0, 0, 0, (int)stage1Delay + 2);
        bool warmup = true;
        int misses = 0;
        while (true)
        {
            if (loops == 100)
                break;

            // Getting closer to 'target' - Lets do the less precisces but least cpu intesive wait
            bool taskDelayed = false;
            var timeLeft = target - DateTime.Now;
            if (timeLeft.TotalMilliseconds >= stage1Delay)
            {
                taskDelayed = true;
                try
                {
                    await Task.Delay((int)(timeLeft.TotalMilliseconds - stage1Delay), ct ?? CancellationToken.None);
                }
                catch (TaskCanceledException)
                {
                    return;
                }
            }

            // Getting closer to 'target' - Lets do the semi-precise but mild cpu intesive wait - Task.Yield()
            int delay0Count = 0;
            while (DateTime.Now < target - new TimeSpan(stage2Delay))
            {
                delay0Count++;
                await Task.Yield();
            }

            // Getting closer to 'target' - Lets do the semi-precise but mild cpu intesive wait - Thread.Sleep(0)
            // Note: Thread.Sleep(0) is removed below because it is sometimes looked down on and also said not good to mix 'Thread.Sleep(0)' with Tasks.
            //       However, Thread.Sleep(0) does have a quicker and more reliable turn around time then Task.Yield() so to 
            //       make up for this a longer (and more expensive) Thread.SpinWait(1) would be needed.
            int sleep0Count = 0;
            if (USE_SLEEP0)
                while (DateTime.Now < target - new TimeSpan(stage2Delay / 8))
                {
                    sleep0Count++;
                    Thread.Sleep(0);
                }

            // Extreamlly close to 'target' - Lets do the most precise but very cpu/battery intesive 
            int spinCount = 0;
            while (DateTime.Now < target)
            {
                spinCount++;
                Thread.SpinWait(64);
            }

            DateTime finish = DateTime.Now;

            if (finish.Subtract(target).Ticks > (new TimeSpan(0, 0, 0, 0, 1)).Ticks)
                misses++;

            log.AppendLine((warmup ? "WARMUP " : "") + "loop:" + loops + "\tnext: " + target.ToString("ss.ffffff")
            + " \t" + finish.Subtract(target).ToString(@"s\.ffffff") + "\tTaskDelayed:" + (taskDelayed ? "Y" : "N")
            + "  Counts->\tYield():" + delay0Count + "\tSleep0:" + sleep0Count + "\tSpin:" + spinCount);


            if (!warmup)
            {
                target += interval;

                await Task.Run(task); // or your code here
            }
            else
            {
                long start1 = DateTime.Now.Ticks + ((long)interval.TotalMilliseconds * TimeSpan.TicksPerMillisecond);
                long alignVal = start1 - (start1 % ((long)interval.TotalMilliseconds * TimeSpan.TicksPerMillisecond));
                target = new DateTime(alignVal);
                warmup = false;
            }
            loops++;
        }
        Console.WriteLine(log);
        Console.WriteLine("misses: " + misses);
    }


}

