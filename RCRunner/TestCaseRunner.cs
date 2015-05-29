using System;
using System.Diagnostics;
using System.Threading;

namespace RCRunner
{
    /// <summary>
    /// Delegate that defines an event that will be called after a test runs
    /// </summary>
    /// <param name="testcaseMethod">Name of the test case</param>
    public delegate void TestRunFinishedDelegate(TestMethod testcaseMethod);

   /// <summary>
   /// Class that runs an automated test case in a thread
   /// </summary>
    public class TestCaseRunner
    {
        /// <summary>
        /// Name of the test cases to run
        /// </summary>
        private readonly TestMethod _testCase;
        /// <summary>
        /// Main object that runs test cases
        /// </summary>
        private readonly ITestFrameworkRunner _testFrameworkRunner;
        /// <summary>
        /// Class to load plugins to execute events after a test run
        /// </summary>
        private readonly PluginLoader _pluginLoader;
        /// <summary>
        /// Event fired when a test finishes executing
        /// </summary>
        public event TestRunFinishedDelegate TestRunFinished;

        /// <summary>
        /// Method that will be called when a test finishes executing
        /// </summary>
        /// <param name="exception">The object that represents a test cases run error</param>
        protected virtual void OnTestRunFinished(Exception exception)
        {
            if (exception != null)
            {
                _testCase.TestExecutionStatus = TestExecutionStatus.Failed;
                _testCase.LastExecutionErrorMsg = exception.ToString();
            }
            else
            {
                _testCase.TestExecutionStatus = TestExecutionStatus.Passed;
                _testCase.LastExecutionErrorMsg = string.Empty;
            }

            if (TestRunFinished != null) TestRunFinished(_testCase);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="testCase">Name of the test cases to run</param>
        /// <param name="testFrameworkRunner">Main object that runs test cases</param>
        /// <param name="pluginLoader">Class to load plugins to execute events after a test run</param>
        public TestCaseRunner(TestMethod testCase, ITestFrameworkRunner testFrameworkRunner, PluginLoader pluginLoader)
        {
            Debug.Assert(testCase != null);
            Debug.Assert(testFrameworkRunner != null);
            Debug.Assert(pluginLoader != null);
            _testCase = testCase;
            _testFrameworkRunner = testFrameworkRunner;
            _pluginLoader = pluginLoader;
        }
        
        /// <summary>
        /// Main method that will be executed to run a test case
        /// </summary>
        public void DoWork()
        {
            var t = new Thread(DoWorkCore);
            t.Start();
        }

        /// <summary>
        /// Executes a test case
        /// </summary>
        private void DoWorkCore()
        {
            try
            {
                try
                {
                    _testFrameworkRunner.RunTest(_testCase.DisplayName);
                    OnTestRunFinished(null);
                }
                finally
                {
                    _pluginLoader.RunTestExecutionPlugins(_testCase.DisplayName);
                }
            }
            catch (Exception exception)
            {
                OnTestRunFinished(exception);
            }
        }
    }
}