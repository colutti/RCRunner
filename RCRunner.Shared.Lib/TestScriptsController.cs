using System.Collections.Generic;
using System.Linq;
using System.Threading;
using RCRunner.Shared.Lib.PluginsStruct;

namespace RCRunner.Shared.Lib
{
    public class TestScriptsController
    {
        private readonly RunningTestsCount _runningTestsCount;
        
        private readonly object _obj;
        
        /// <summary>
        ///     Event to check if the test run was canceled
        /// </summary>
        public CheckCanceled Canceled;

        /// <summary>
        ///     Event fired when a test run changes its status
        /// </summary>
        public TestRunFinishedDelegate TestCaseStatusChanged;

        /// <summary>
        ///     List of the test cases to run
        /// </summary>
        private List<TestScript> _testCasesList;

        /// <summary>
        ///     Main object that runs test cases
        /// </summary>
        private TestFrameworkRunner _testFrameworkRunner;

        public TestScriptsController(RunningTestsCount runningTestsCount)
        {
            _runningTestsCount = runningTestsCount;
            _obj = new object();
        }

        /// <summary>
        ///     Check if the test run was canceled by the user
        /// </summary>
        /// <returns></returns>
        protected virtual bool OnCanceled()
        {
            var handler = Canceled;
            return handler != null && handler();
        }

        /// <summary>
        ///     Method to call the TestCaseStatusChanged event when a test changes its status (running, waiting, etc)
        /// </summary>
        /// <param name="testcasemethod">The method whom status have changed</param>
        protected virtual void OnMethodStatusChanged(TestScript testcasemethod)
        {
            TestRunFinishedDelegate handler = TestCaseStatusChanged;
            if (handler != null) handler(testcasemethod);
        }

        /// <summary>
        ///     Method called by the TestCaseRunner when a test finishes
        /// </summary>
        /// <param name="testcaseScript"></param>
        private void OnTaskTestRunFinishedEvent(TestScript testcaseScript)
        {
            if (testcaseScript.TestExecutionStatus == TestExecutionStatus.Failed && ShouldRetry(testcaseScript.LastExecutionErrorMsg))
            {
                if (testcaseScript.RetryCount < 1)
                {
                    RemoveItemThreadSafe(testcaseScript);
                    testcaseScript.TestExecutionStatus = TestExecutionStatus.WillRetry;
                    testcaseScript.RetryCount++;
                    AddItemThreadSafe(testcaseScript);
                }
            }
            OnMethodStatusChanged(testcaseScript);
        }

        /// <summary>
        ///     Set for the _testFrameworkRunner property
        /// </summary>
        /// <param name="testFrameworkRunner"></param>
        public void SetTestRunner(TestFrameworkRunner testFrameworkRunner)
        {
            _testFrameworkRunner = testFrameworkRunner;
        }

        private static bool ShouldRetry(string errorMsg)
        {
            return errorMsg.ToLower().Contains("timed out after") || errorMsg.ToLower().Contains("unable to connect to the remote server") ||
                errorMsg.ToLower().Contains("element is not clickable") || errorMsg.ToLower().Contains("failed to start up socket") ||
                errorMsg.ToLower().Contains("a exception with a null response was") || errorMsg.ToLower().Contains("not available and is not among the last");
        }

        private void AddItemThreadSafe(TestScript testScript)
        {
            lock (_obj)
            {
                _testCasesList.Add(testScript);
            }

        }

        private TestScript GetItemThreadSafe()
        {
            lock (_obj)
            {
                if (_testCasesList.Count <= 0) return null;

                var testScript = _testCasesList.First();
                _testCasesList.Remove(testScript);
                return testScript;
            }
        }

        private void RemoveItemThreadSafe(TestScript testScript)
        {
            lock (_obj)
            {
                _testCasesList.Remove(testScript);
            }
        }


        /// <summary>
        ///     Method that will run all the tests listed in _testCasesList
        /// </summary>
        private void DoWorkCore()
        {
            _runningTestsCount.TotRunning = 0;

            var testScript = GetItemThreadSafe();

            while (testScript != null) 
            {
                var threads = SeleniumGridApi.GetNumberAvailableBrownsers();

                while (_runningTestsCount.TotRunning >= threads)
                {
                    if (OnCanceled()) return;
                    threads = SeleniumGridApi.GetNumberAvailableBrownsers();
                }

                if (OnCanceled()) return;

                testScript.SetTestRunner(_testFrameworkRunner);
                testScript.TestExecutionStatus = TestExecutionStatus.Running;
                testScript.TestRunFinished = OnTaskTestRunFinishedEvent;

                OnMethodStatusChanged(testScript);

                testScript.DoWork();
                
                testScript = GetItemThreadSafe();

                while (testScript == null && _runningTestsCount.TotRunning > 0)
                {
                    testScript = GetItemThreadSafe();
                }
            }
        }

        /// <summary>
        ///     Method that will call DoWorkCore to run all the tests in testCasesList in a thread
        /// </summary>
        /// <param name="testCasesList">The list of test cases to run</param>
        public void DoWork(List<TestScript> testCasesList)
        {
            _testCasesList = testCasesList;
            var t = new Thread(DoWorkCore);
            t.Start();
        }
    }
}