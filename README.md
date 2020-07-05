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
'taskDelayDelay' is used to specify how early we should return before 'Task.Yield()'.
'taskYieldDelay' is used to specify how early we should return before staring 'Sleep(0)' or 'SpinWait(1)' (if sleep disabled).
'USE_SLEEP0' can be used to remove the 'sleep(0)' code so spinwait is used. Mixing sleep and yeld are not a best practice but do yield better results.

'''
        long taskDelayDelay = 16;
        long taskYieldDelay = 8 * TimeSpan.TicksPerMillisecond;
        bool USE_SLEEP0 = false;   <---- This one can be turned off if Sleep(0) does not want to be used (or lines can be deleted).
'''

## Examples
### Without a CPU Load
On a threadripper CPU, release mode, .net core 3, no debugger attached
Options: taskDelayDelay = 16; taskYieldDelay = 8; USE_SLEEP0 = true;
Resusults are very good - only off by 
|loop#   | Target Time| Late by(ms)| Yield | Yield Count | Sleep Count | Spin Count| 
|--------|------------|------------|-------|-------|-------|-----|
| 1      | 54.750000  | 0.000      | Yes   | 2     | 10562 | 433 |
| 2      | 55.000000  | 0.002      | Yes   | 2690  | 10641 | 437 |
| 3      | 55.250000  | 0.002      | Yes   | 18    | 11051 | 442 |
| 4      | 55.500000  | 0.001      | Yes   | 3145  | 11240 | 431 |
| 5      | 55.750000  | 0.001      | Yes   | 0     | 7422  | 440 |
| 6      | 56.000000  | 0.002      | Yes   | 3778  | 11403 | 440 |
| 7      | 56.250000  | 0.000      | Yes   | 4756  | 11435 | 435 |
| 8      | 56.500000  | 0.002      | Yes   | 5523  | 11357 | 438 |
| 9      | 56.750000  | 0.001      | Yes   | 5553  | 10704 | 421 |
| 10     | 57.000000  | 0.000      | Yes   | 4520  | 11350 | 437 |
| 11     | 57.250000  | 0.000      | Yes   | 5265  | 11400 | 441 |
| 12     | 57.500000  | 0.002      | Yes   | 4517  | 11420 | 441 |
| 13     | 57.750000  | 0.001      | Yes   | 6886  | 11307 | 441 |
| 14     | 58.000000  | 0.000      | Yes   | 5494  | 11415 | 442 |
| 15     | 58.250000  | 0.000      | Yes   | 5370  | 11376 | 440 |
| 16     | 58.500000  | 0.000      | Yes   | 4123  | 11399 | 441 |
| 17     | 58.750000  | 0.002      | Yes   | 5518  | 11230 | 440 |
| 18     | 59.000000  | 0.001      | Yes   | 5820  | 11256 | 436 |
| 19     | 59.250000  | 0.002      | Yes   | 4689  | 11561 | 443 |
| 20     | 59.500000  | 0.001      | Yes   | 5920  | 11029 | 441 |
| 21     | 59.750000  | 0.000      | Yes   | 4876  | 10646 | 438 |
| 22     | 00.000000  | 0.002      | Yes   | 4841  | 11168 | 440 |
| 23     | 00.250000  | 0.001      | Yes   | 5261  | 11354 | 443 |
| 24     | 00.500000  | 0.001      | Yes   | 5440  | 10686 | 442 |
| 25     | 00.750000  | 0.001      | Yes   | 4330  | 10739 | 441 |
| 26     | 01.000000  | 0.002      | Yes   | 4800  | 11310 | 442 |
| 27     | 01.250000  | 0.000      | Yes   | 3910  | 10849 | 440 |
| 28     | 01.500000  | 0.002      | Yes   | 4956  | 11250 | 439 |
| 29     | 01.750000  | 0.000      | Yes   | 4688  | 11336 | 439 |
| 30     | 02.000000  | 0.000      | Yes   | 5259  | 11416 | 438 |
| 31     | 02.250000  | 0.000      | Yes   | 4499  | 11131 | 432 |
| 32     | 02.500000  | 0.000      | Yes   | 4432  | 11257 | 439 |
| 33     | 02.750000  | 0.000      | Yes   | 0     | 5796  | 439 |
| 34     | 03.000000  | 0.002      | Yes   | 5934  | 11284 | 441 |
| 35     | 03.250000  | 0.001      | Yes   | 6880  | 11314 | 442 |
| 36     | 03.500000  | 0.001      | Yes   | 5677  | 11414 | 437 |
| 37     | 03.750000  | 0.000      | Yes   | 5498  | 11378 | 441 |
| 38     | 04.000000  | 0.001      | Yes   | 5636  | 11341 | 440 |
| 39     | 04.250000  | 0.000      | Yes   | 5013  | 11446 | 415 |

### With Load a high CPU load (CPU stress test)
On a threadripper CPU, release mode, .net core 3, no debugger attached.
Options: taskDelayDelay = 16; taskYieldDelay = 8; USE_SLEEP0 = true;                                                                 
|loop#   | Target Time| Late by(ms)| Yield | Yield Count | Sleep Count | Spin Count| 
|--------|------------|------------|-------|-------|-------|------|
| 1      | 32.500000  |  2.437    | Yes   | 0     | 0     | 0    |
| 2      | 32.750000  | 41.330    | Yes   | 9     | 1     | 0    |
| 3      | 33.000000  | 15.380    | Yes   | 0     | 1     | 0    |
| 4      | 33.250000  |  5.448    | Yes   | 0     | 0     | 0    |
| 5      | 33.500000  |  5.248    | Yes   | 0     | 13    | 0    |
| 6      | 33.750000  |  2.389    | Yes   | 0     | 0     | 0    |
| 7      | 34.000000  |  2.915    | Yes   | 0     | 0     | 0    |
| 8      | 34.250000  |  0.001    | Yes   | 0     | 1     | 213  |
| 9      | 34.500000  |  0.000    | Yes   | 5882  | 3999  | 84   |
| 10     | 34.750000  |  2.851    | Yes   | 0     | 1     | 0    |
| 11     | 35.000000  | 16.373    | Yes   | 0     | 0     | 0    |
| 12     | 35.250000  |  9.303    | Yes   | 0     | 2     | 0    |
| 13     | 35.500000  |  0.003    | Yes   | 0     | 0     | 185  |
| 14     | 35.750000  |  3.383    | Yes   | 0     | 0     | 0    |
| 15     | 36.000000  |  1.318    | Yes   | 6840  | 4378  | 0    |
| 16     | 36.250000  |  8.555    | Yes   | 0     | 0     | 0    |
| 17     | 36.500000  |  0.001    | Yes   | 1479  | 8905  | 304  |
| 18     | 36.750000  |  2.601    | Yes   | 0     | 1     | 0    |
| 19     | 37.000000  |  0.001    | Yes   | 2913  | 1045  | 302  |
| 20     | 37.250000  |  0.000    | Yes   | 0     | 1     | 213  |
| 21     | 37.500000  | 14.822    | Yes   | 3702  | 1     | 0    |
| 22     | 37.750000  |  6.647    | Yes   | 0     | 2     | 0    |
| 23     | 38.000000  |  4.333    | Yes   | 840   | 655   | 0    |
| 24     | 38.250000  |  2.619    | Yes   | 0     | 2     | 0    |
| 25     | 38.500000  | 31.180    | Yes   | 4299  | 1     | 0    |
| 26     | 38.750000  |  0.000    | Yes   | 2179  | 6017  | 304  |
| 27     | 39.000000  |  0.002    | Yes   | 6623  | 8230  | 300  |
| 28     | 39.250000  |  0.003    | Yes   | 6598  | 7434  | 300  |
| 29     | 39.500000  |  0.003    | Yes   | 0     | 763   | 330  |
| 30     | 39.750000  | 11.152    | Yes   | 0     | 0     | 0    |
| 31     | 40.000000  |  0.003    | Yes   | 653   | 8864  | 302  |
| 32     | 40.250000  |  9.336    | Yes   | 0     | 0     | 0    |
| 33     | 40.500000  |  7.701    | Yes   | 0     | 0     | 0    |
| 34     | 40.750000  |  0.314    | Yes   | 0     | 1     | 0    |
| 35     | 41.000000  |  1.341    | Yes   | 0     | 0     | 0    |
| 36     | 41.250000  |  0.002    | Yes   | 0     | 0     | 236  |
| 37     | 41.500000  |  0.001    | Yes   | 0     | 3809  | 298  |
| 38     | 41.750000  |  1.307    | Yes   | 0     | 521   | 0    |
| 39     | 42.000000  |  0.000    | Yes   | 2886  | 6773  | 296  |
| 40     | 42.250000  |  9.110    | Yes   | 882   | 1     | 0    |


## License
MIT License (MIT)

## Sources of ideas/code
* framework: https://stackoverflow.com/a/22453097/2352507 (Matthew Watson, Mar 17 '14)
* yield use: https://stackoverflow.com/a/33407181/2352507 (Ned Stoyanov, Oct 29 '15)
* timers without losing time: https://stackoverflow.com/questions/9228313/most-accurate-timer-in-net (Matt Thomas, Jan 7 '16)

## Author
Created by Ryan S. White on May 2020.
