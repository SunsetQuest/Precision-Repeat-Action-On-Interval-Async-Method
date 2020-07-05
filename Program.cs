// Precision Repeat Action On Interval Async Method
// Created by Ryan Scott White (sunsetquest) on 4/21/2020
// Shared under the MIT License 
// Goals: 
//   (1) simple
//   (2) accurate timer interval
//   (3) not cpu wasteful (without using SpinWait too much)
//   (4) The timer will aline itself. 
//
// Sources of ideas/code
//   framework: https://stackoverflow.com/a/22453097/2352507 (Matthew Watson, Mar 17 '14)
//   yield use: https://stackoverflow.com/a/33407181/2352507 (Ned Stoyanov, Oct 29 '15)
//   timers without loosing time: https://stackoverflow.com/questions/9228313/most-accurate-timer-in-net (Matt Thomas, Jan 7 '16)

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    static void Main()
    {
        CancellationTokenSource cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(300));
        Console.WriteLine("Start time is " + DateTime.Now.ToString("hh.mm.ss.ffffff"));
        PrecisionRepeatActionOnIntervalAsync(SayHello(), TimeSpan.FromMilliseconds(100), cancellation.Token).Wait();
        Console.WriteLine("Ending Time is " + DateTime.Now.ToString("hh.mm.ss.ffffff"));
        Console.ReadKey();
    }

    // Some Function
    public static Action SayHello() => () => Console.WriteLine(DateTime.Now.ToString("ss.ffffff"));
        
    /// <summary>
    /// A timer that will fire an action at a regular interval. The timer will aline itself.
    /// </summary>
    /// <param name="action">The action to run Asyncrinasally</param>
    /// <param name="interval">The interval to fire at.</param>
    /// <param name="ct">(optional)A CancellationToken to cancel.</param>
    /// <returns>The Task.</returns>
    public static async Task PrecisionRepeatActionOnIntervalAsync(Action action, TimeSpan interval, CancellationToken? ct = null)
    {
        long stage1Delay = 16 ;
        long stage2Delay = 8 * TimeSpan.TicksPerMillisecond;
        bool USE_SLEEP0 = false;

        DateTime target = DateTime.Now + new TimeSpan(0, 0, 0, 0, (int)stage1Delay + 2);
        bool warmup = true;
        while (true)
        {
            // Getting closer to 'target' - Lets do the less precise but least cpu intensive wait
            var timeLeft = target - DateTime.Now;
            if (timeLeft.TotalMilliseconds >= stage1Delay)
            {
                try
                {
                    await Task.Delay((int)(timeLeft.TotalMilliseconds - stage1Delay), ct ?? CancellationToken.None);
                }
                catch (TaskCanceledException) when (ct != null)
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

            // Extreamlly close to 'target' - Lets do the most precise but very cpu/battery intensive 
            while (DateTime.Now < target)
            {
                Thread.SpinWait(64);
            }

            if (!warmup)
            {
                await Task.Run(action); // or your code here
                target += interval;
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

    /// <summary>
    /// A timer that will fire an action at a regular interval. The timer will aline itself.
    /// </summary>
    /// <param name="action">The action to run Asyncrinasally</param>
    /// <param name="interval">The interval to fire at.</param>
    /// <param name="ct">(optional)A CancellationToken to cancel.</param>
    /// <returns>The Task.</returns>
    public static async Task PrecisionRepeatActionOnIntervalAsync_DEBUG(Action task, TimeSpan interval, CancellationToken? ct = null)
    {
        StringBuilder log = new StringBuilder();

        const long taskDelayDelay = 16;
        const long taskYieldDelay = 8 * TimeSpan.TicksPerMillisecond;
        bool USE_SLEEP0 = true;

        int loops = 0;
        DateTime target = DateTime.Now + new TimeSpan(0, 0, 0, 0, (int)taskDelayDelay + 2);
        bool warmup = true;
        int misses = 0;
        while (true)
        {
            // Getting closer to 'target' - Lets do the less precise but least cpu intensive wait
            bool taskDelayed = false;
            var timeLeft = target - DateTime.Now;
            if (timeLeft.TotalMilliseconds >= taskDelayDelay)
            {
                taskDelayed = true;
                try
                {
                    await Task.Delay((int)(timeLeft.TotalMilliseconds - taskDelayDelay), ct ?? CancellationToken.None);
                }
                catch (TaskCanceledException) when (ct != null)
                {
                    Console.WriteLine(log);
                    Console.WriteLine("misses: " + misses);
                    return;
                }
            }

            // Getting closer to 'target' - Lets do the semi-precise but mild cpu intensive wait - Task.Yield()
            int YieldCount = 0;
            while (DateTime.Now < target - new TimeSpan(taskYieldDelay))
            {
                YieldCount++;
                await Task.Yield();
            }

            // Getting closer to 'target' - Lets do the semi-precise but mild cpu intensive wait - Thread.Sleep(0)
            // Note: Thread.Sleep(0) is removed below because it is sometimes looked down on and also said not good to mix 'Thread.Sleep(0)' with Tasks.
            //       However, Thread.Sleep(0) does have a quicker and more reliable turn around time then Task.Yield() so to 
            //       make up for this a longer (and more expensive) Thread.SpinWait(1) would be needed.
            int sleep0Count = 0;
            if (USE_SLEEP0)
                while (DateTime.Now < target - new TimeSpan(taskYieldDelay / 8))
                {
                    sleep0Count++;
                    Thread.Sleep(0);
                }

            // Extreamlly close to 'target' - Lets do the most precise but very cpu/battery intensive 
            int spinCount = 0;
            while (DateTime.Now < target)
            {
                spinCount++;
                Thread.SpinWait(64);
            }

            DateTime finish = DateTime.Now;

            if (finish.Subtract(target).Ticks >= (new TimeSpan(0, 0, 0, 0, 1)).Ticks)
                misses++;

            if (!warmup)
            {
                await Task.Run(task); // or your code here
                log.AppendLine("| " + loops + "\t| " + target.ToString("ss.ffffff") + "\t| "
                    + finish.Subtract(target).ToString(@"s\.fffffff") + (taskDelayed ? "\t| Delayed" : "\t| Skipped")
                    + "\t| " + YieldCount + "\t| " + sleep0Count + "\t| " + spinCount + "\t|");
                target += interval;
            }
            else
            {
                long start1 = DateTime.Now.Ticks + ((long)interval.TotalMilliseconds * TimeSpan.TicksPerMillisecond);
                long alignVal = start1 - (start1 % ((long)interval.TotalMilliseconds * TimeSpan.TicksPerMillisecond));
                target = new DateTime(alignVal);
                warmup = false;
                log.AppendLine("|     \t| Actual     \t|        \t| Task.\t| Yield\t| Sleep\t| Spin  |");
                log.AppendLine("|loop#\t| Target Time\t| Late by\t| Yield\t| Count\t| Count\t| Count |");
                log.AppendLine("|-------|---------------|---------------|-------|-------|-------|-------|");
            }
            loops++;
        }
    }
}

