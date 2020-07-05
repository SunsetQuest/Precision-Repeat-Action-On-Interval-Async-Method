# A Precision Repeating Timer
What is it?
This is an accurate auto-reset timer that can be used in services or applications. 
The function is setup to not be CPU wasteful but still doing its best to have an accurate (< 1ms) most of the time. Since windows is not generally a realtime OS, its not possible for it to ask for CPU cycles at an exact moment. Windows will hand out chunks of CPU time to apps/services and this is what we have to work with. This function tries to work around this by doing ever tighter timed functions. 
1. It will first do a Task.Delay() this is the least CPU intensive to get us near the time we are looking for.
1. It will then loop at Task.Yield(). This will share CPU cycles with the app itself and with windows.
1. As it gets even closer, it will then do a Thread.Sleep(0). Thread.Sleep(0) even has a faster turnout because of some technical details. Using Thread.Sleep(0) is optional however as it may not be considered good programming practice to mix Task.Yeld() and Thread.Sleep(0). 
1. Finally, it will loop at the CPU intensive Thread.SpinWait(1) until the DateTime is hit. This is usually less than 1ms.

## Timer alignment.
The timer will align itself.  I used some of my math skills to come up with:
```
X = NowInTicks + requestedIntervalInTicks
Next_Targe = X - (X % requestedIntervalInTicks)
```
...
This results in a value aligned to the current 0 second. The best way to show this is by examples...
Examples: (say the time at load is 1:23:45.111111)
'''
| Interval | 1st fire time| 2nd fire time|
|----------|--------------|--------------|
|   1 ms   | 1:23:45.112  | 1:23:45.113  |
|   2 ms   | 1:23:45.112  | 1:23:45.114  |
|   3 ms   | 1:23:45.114  | 1:23:45.117  |
|   4 ms   | 1:23:45.112  | 1:23:45.116  |
|   5 ms   | 1:23:45.115  | 1:23:45.120  |
|   6 ms   | 1:23:45.112  | 1:23:45.118  |
|  10 ms   | 1:23:45.120  | 1:23:45.130  |
|  25 ms   | 1:23:45.125  | 1:23:45.150  |
| 100 ms   | 1:23:45.200  | 1:23:45.300  |
| 250 ms   | 1:23:45.250  | 1:23:45.500  |

## Configuration
There are a few lines you use to tune.
'''
        long stage1Delay = 20;
        long stage2Delay = 5 * TimeSpan.TicksPerMillisecond;
        bool USE_SLEEP0 = false;   <---- This one can be turned off if Sleep(0) does not want to be used (or lines can be deleted).
'''
## License
MIT License (MIT)

## Sources of ideas/code
* framework: https://stackoverflow.com/a/22453097/2352507 (Matthew Watson, Mar 17 '14)
* yield use: https://stackoverflow.com/a/33407181/2352507 (Ned Stoyanov, Oct 29 '15)
* timers without losing time: https://stackoverflow.com/questions/9228313/most-accurate-timer-in-net (Matt Thomas, Jan 7 '16)

## Author
Created by Ryan S. White on May 2020.
