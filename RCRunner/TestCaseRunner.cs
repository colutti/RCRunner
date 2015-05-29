using System;
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
        private readonly TestMethod _testCase;
        private readonly ITestFrameworkRunner _testFrameworkRunner;
        private readonly PluginLoader _pluginLoader;
        public event TestRunFinishedDelegate TestRunFinished;

        protected virtual void OnTestRunFinished(Exception exception)
        {
            var handler = TestRunFinished;

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

            if (handler != null) handler(_testCase);
        }

        public TestCaseRunner(TestMethod testCase, ITestFrameworkRunner testFrameworkRunner, PluginLoader pluginLoader)
        {
            _testCase = testCase;
            _testFrameworkRunner = testFrameworkRunner;
            _pluginLoader = pluginLoader;
        }

        private void RunTestExecutionPlugins(string testCase)
        {
            if (_pluginLoader == null) return;

            foreach (var testExecution in _pluginLoader.TestExecutionPlugiList)
            {
                testExecution.AfterTestExecution(testCase);
            }
        }

        public void DoWork()
        {
            var t = new Thread(DoWorkCore);
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
        }

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
                    RunTestExecutionPlugins(_testCase.DisplayName);
                }
            }
            catch (Exception exception)
            {
                OnTestRunFinished(exception);
            }
        }
    }
}