using System.Collections.Generic;
using System.Threading;

namespace RCRunner
{
    public class TestCasesController
    {
        public event TestRunFinishedDelegate TestRunFinished;

        public event TestRunFinishedDelegate TestCaseStatusChanged;

        public event CheckCanceled Canceled;

        private List<TestMethod> _testCasesList;

        private ITestFrameworkRunner _testFrameworkRunner;

        private int _totRunningScripts;

        private readonly PluginLoader _pluginLoader;

        protected virtual bool OnCanceled()
        {
            var handler = Canceled;
            return handler != null && handler();
        }

        public void SetTestRunner(ITestFrameworkRunner testFrameworkRunner)
        {
            _testFrameworkRunner = testFrameworkRunner;
        }

        protected virtual void OnMethodStatusChanged(TestMethod testcasemethod)
        {
            var handler = TestCaseStatusChanged;
            if (handler != null) handler(testcasemethod);
        }

        private void OnTaskTestRunFinishedEvent(TestMethod testcaseMethod)
        {
            _totRunningScripts--;
        }

        public TestCasesController()
        {
            _pluginLoader = new PluginLoader();
            _pluginLoader.LoadTestExecutionPlugins();
        }

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

        public void DoWork(List<TestMethod> testCasesList)
        {
            _testCasesList = testCasesList;
            var t = new Thread(DoWorkCore);
            t.Start();
        }
    }
}