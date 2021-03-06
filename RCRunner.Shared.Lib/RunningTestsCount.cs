using System.Threading;

namespace RCRunner.Shared.Lib
{
    public class RunningTestsCount
    {
        /// <summary>
        ///     Count of failed test scripts
        /// </summary>
        public int TotFailed;

        /// <summary>
        ///     Count of passed test scripts
        /// </summary>
        public int TotPassed;

        /// <summary>
        ///     Count of running test scripts
        /// </summary>
        public int TotRunning;

        /// <summary>
        ///     Count of wating test scripts
        /// </summary>
        public int TotWaiting;

        /// <summary>
        ///     Constructor
        /// </summary>
        public RunningTestsCount()
        {
            Reset();
        }

        /// <summary>
        ///     Resets the count properties
        /// </summary>
        public void Reset()
        {
            TotFailed = 0;
            TotPassed = 0;
            TotRunning = 0;
            TotWaiting = 0;
        }

        /// <summary>
        ///     Update the count properties based on the status of a testmethod
        /// </summary>
        /// <param name="testcasemethod"></param>
        public void Update(TestScript testcasemethod)
        {
            switch (testcasemethod.TestExecutionStatus)
            {
                case TestExecutionStatus.Failed:
                {
                    Interlocked.Decrement(ref TotRunning);
                    Interlocked.Increment(ref TotFailed);
                    break;
                }
                case TestExecutionStatus.Passed:
                {
                    Interlocked.Decrement(ref TotRunning);
                    Interlocked.Increment(ref TotPassed);
                    break;
                }
                case TestExecutionStatus.Running:
                {
                    Interlocked.Increment(ref TotRunning);
                    Interlocked.Decrement(ref TotWaiting);
                    break;
                }
                case TestExecutionStatus.Waiting:
                {
                    Interlocked.Increment(ref TotWaiting);
                    break;
                }
                case TestExecutionStatus.WillRetry:
                {
                    Interlocked.Decrement(ref TotRunning);
                    Interlocked.Increment(ref TotWaiting);
                    break;
                }
            }
        }

        /// <summary>
        ///     Returns if all tests scritps have already ran
        /// </summary>
        /// <returns></returns>
        public bool Done()
        {
            return (TotRunning == 0) && (TotWaiting == 0);
        }
    }
}