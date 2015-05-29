using System.Collections.Generic;
using System.Threading;

namespace RCRunner
{
    public class TestCasesController
    {
        /// <summary>
        /// Event fired when a test finishes executing
        /// </summary>
        public event TestRunFinishedDelegate TestRunFinished;
        /// <summary>
        /// Event fired when a test run changes its status
        /// </summary>
        public event TestRunFinishedDelegate TestCaseStatusChanged;
        /// <summary>
        /// Event to check if the test run was canceled
        /// </summary>
        public event CheckCanceled Canceled;
        /// <summary>
        /// List of the test cases to run
        /// </summary>
        private List<TestMethod> _testCasesList;
        /// <summary>
        /// Main object that runs test cases
        /// </summary>
        private ITestFrameworkRunner _testFrameworkRunner;
        /// <summary>
        /// Total of running test scripts
        /// </summary>
        private int _totRunningScripts;
        /// <summary>
        /// Class to load plugins to execute events after a test run
        /// </summary>
        private readonly PluginLoader _pluginLoader;

        /// <summary>
        /// Check if the test run was canceled by the user
        /// </summary>
        /// <returns></returns>
        protected virtual bool OnCanceled()
        {
            var handler = Canceled;
            return handler != null && handler();
        }

        /// <summary>
        /// Set for the _testFrameworkRunner field
        /// </summary>
        /// <param name="testFrameworkRunner"></param>
        public void SetTestRunner(ITestFrameworkRunner testFrameworkRunner)
        {
            _testFrameworkRunner = testFrameworkRunner;
        }

        /// <summary>
        /// Method to call the TestCaseStatusChanged event when a test changes its status (running, waiting, etc) 
        /// </summary>
        /// <param name="testcasemethod">The method whom status have changed</param>
        protected virtual void OnMethodStatusChanged(TestMethod testcasemethod)
        {
            var handler = TestCaseStatusChanged;
            if (handler != null) handler(testcasemethod);
        }

        /// <summary>
        /// Method called by the TestCaseRunner when a test finishes
        /// </summary>
        /// <param name="testcaseMethod"></param>
        private void OnTaskTestRunFinishedEvent(TestMethod testcaseMethod)
        {
            _totRunningScripts--;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public TestCasesController()
        {
            _pluginLoader = new PluginLoader();
            _pluginLoader.LoadTestExecutionPlugins();
        }

        /// <summary>
        /// Method that will run all the tests listed in _testCasesList
        /// </summary>
        private void DoWorkCore()
        {
            _totRunningScripts = 0;

            foreach (var testMethod in _testCasesList)
            {
                testMethod.TestExecutionStatus = TestExecutionStatus.Waiting;
                testMethod.LastExecutionErrorMsg = string.Empty;
                OnMethodStatusChanged(testMethod);
            }

            foreach (var testMethod in _testCasesList)
            {
                while (_totRunningScripts >= Properties.Settings.Default.MaxThreads)
                {
                    if (OnCanceled()) return;
                }

                _totRunningScripts++;
                if (OnCanceled()) return;
                testMethod.TestExecutionStatus = TestExecutionStatus.Running;
                OnMethodStatusChanged(testMethod);
                var task = new TestCaseRunner(testMethod, _testFrameworkRunner, _pluginLoader);
                task.TestRunFinished += OnTaskTestRunFinishedEvent;
                task.TestRunFinished += TestRunFinished;
                task.DoWork();
            }
        }

        /// <summary>
        /// Method that will call DoWorkCore to run all the tests in testCasesList in a thread
        /// </summary>
        /// <param name="testCasesList">The list of test cases to run</param>
        public void DoWork(List<TestMethod> testCasesList)
        {
            _testCasesList = testCasesList;
            var t = new Thread(DoWorkCore);
            t.Start();
        }
    }
}