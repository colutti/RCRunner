using System;
using System.Threading;

namespace RCRunner
{
    /// <summary>
    /// Delegate that defines an event that will be called after a test runs
    /// </summary>
    /// <param name="testcaseScript">Name of the test case</param>
    public delegate void TestRunFinishedDelegate(TestScript testcaseScript);

    /// <summary>
    /// Possible status for the test scripts
    /// </summary>
    public enum TestExecutionStatus
    {
        Failed,
        Passed,
        Active,
        Waiting,
        Running,
    }

    /// <summary>
    /// Class that runs an automated test case in a thread
    /// </summary>
    public class TestScript
    {
        /// <summary>
        /// Name of the class that this test belongs to
        /// </summary>
        public string ClassName;
        /// <summary>
        /// Name of the test method
        /// </summary>
        public string Name;
        /// <summary>
        /// Last error happened when this testes was executed
        /// </summary>
        public string LastExecutionErrorMsg;
        /// <summary>
        /// Status os the test execution
        /// </summary>
        public TestExecutionStatus TestExecutionStatus;
        /// <summary>
        /// Test method description
        /// </summary>
        public string TestDescription;
        /// <summary>
        /// Main object that runs test cases
        /// </summary>
        private ITestFrameworkRunner _testFrameworkRunner;
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
                TestExecutionStatus = TestExecutionStatus.Failed;
                LastExecutionErrorMsg = exception.ToString();
            }
            else
            {
                TestExecutionStatus = TestExecutionStatus.Passed;
                LastExecutionErrorMsg = string.Empty;
            }

            if (TestRunFinished != null) TestRunFinished(this);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public TestScript()
        {
            _pluginLoader = new PluginLoader();
        }

        /// <summary>
        /// Set for the _testFrameworkRunner property
        /// </summary>
        /// <param name="testFrameworkRunner"></param>
        public void SetTestRunner(ITestFrameworkRunner testFrameworkRunner)
        {
            _testFrameworkRunner = testFrameworkRunner;
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
                    //Thread.Sleep(5000);
                    _testFrameworkRunner.RunTest(Name);
                    OnTestRunFinished(null);
                }
                finally
                {
                    _pluginLoader.RunTestExecutionPlugins(Name);
                }
            }
            catch (Exception exception)
            {
                OnTestRunFinished(exception);
            }
        }
    }
}