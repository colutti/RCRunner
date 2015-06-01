namespace RCRunner
{
    public class RunningTestsCount
    {
        /// <summary>
        /// Count of running test scripts
        /// </summary>
        public int TotRunning;
        /// <summary>
        /// Count of failed test scripts
        /// </summary>
        public int TotFailed;
        /// <summary>
        /// Count of passed test scripts
        /// </summary>
        public int TotPassed;
        /// <summary>
        /// Count of wating test scripts
        /// </summary>
        public int TotWaiting;

        /// <summary>
        /// Resets the count properties
        /// </summary>
        public void Reset()
        {
            TotFailed = 0;
            TotPassed = 0;
            TotRunning = 0;
            TotWaiting = 0;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public RunningTestsCount()
        {
            Reset();
        }

        /// <summary>
        /// Update the count properties based on the status of a testmethod
        /// </summary>
        /// <param name="testcasemethod"></param>
        public void Update(TestMethod testcasemethod)
        {
            switch (testcasemethod.TestExecutionStatus)
            {
                case TestExecutionStatus.Failed:
                {
                    TotRunning--;
                    TotFailed++;
                    break;
                }
                case TestExecutionStatus.Passed:
                {
                    TotRunning--;
                    TotPassed++;
                    break;
                }
                case TestExecutionStatus.Running:
                {
                    TotRunning++;
                    TotWaiting--;
                    break;
                }
                case TestExecutionStatus.Waiting:
                {
                    TotWaiting++;
                    break;
                }
            }
        }

        /// <summary>
        /// Returns if all tests scritps have already ran
        /// </summary>
        /// <returns></returns>
        public bool Done()
        {
            return (TotRunning == 0) && (TotWaiting == 0);
        }

    }
}