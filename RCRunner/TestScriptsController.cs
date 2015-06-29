using System.Collections.Generic;
using System.Threading;
using RCRunner.Properties;

namespace RCRunner
{
    public class TestScriptsController
    {
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
        private ITestFrameworkRunner _testFrameworkRunner;

        /// <summary>
        ///     Total of running test scripts
        /// </summary>
        private int _totRunningScripts;

        /// <summary>
        ///     Check if the test run was canceled by the user
        /// </summary>
        /// <returns></returns>
        protected virtual bool OnCanceled()
        {
            CheckCanceled handler = Canceled;
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
            _totRunningScripts--;
            OnMethodStatusChanged(testcaseScript);
        }

        /// <summary>
        ///     Set for the _testFrameworkRunner property
        /// </summary>
        /// <param name="testFrameworkRunner"></param>
        public void SetTestRunner(ITestFrameworkRunner testFrameworkRunner)
        {
            _testFrameworkRunner = testFrameworkRunner;
        }

        /// <summary>
        ///     Method that will run all the tests listed in _testCasesList
        /// </summary>
        private void DoWorkCore()
        {
            _totRunningScripts = 0;

            foreach (TestScript testMethod in _testCasesList)
            {
                while (_totRunningScripts >= Settings.Default.MaxThreads)
                {
                    if (OnCanceled()) return;
                }

                _totRunningScripts++;
                if (OnCanceled()) return;
                testMethod.SetTestRunner(_testFrameworkRunner);
                testMethod.TestExecutionStatus = TestExecutionStatus.Running;
                OnMethodStatusChanged(testMethod);
                testMethod.TestRunFinished = OnTaskTestRunFinishedEvent;
                testMethod.DoWork();
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