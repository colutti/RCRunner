using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RCRunner
{
    public enum TestExecutionStatus
    {
        Failed,
        Passed,
        Active,
        Waiting,
        Running,
    }
    
    public class TestMethod
    {
        public string ClassName;
        public MethodInfo Method;
        public string DisplayName;
        public string LastExecutionErrorMsg;
        public TestExecutionStatus TestExecutionStatus;
        public string TestDescription;
    }

    public class RunningTestsCount
    {
        public int TotRunning;
        public int TotFailed;
        public int TotPassed;
        public int TotActive;
        public int TotWaiting;

        public void Reset()
        {
            TotActive = 0;
            TotFailed = 0;
            TotPassed = 0;
            TotRunning = 0;
            TotWaiting = 0;
        }

        public RunningTestsCount()
        {
            Reset();
        }
    }
    
    public delegate bool CheckCanceled();

    public class RCRunnerAPI
    {
        public readonly List<TestMethod> TestClassesList;

        private readonly TestCasesController _testCasesController;

        public event TestRunFinishedDelegate OnTestFinished;

        public event TestRunFinishedDelegate MethodStatusChanged;

        private ITestFrameworkRunner _testFrameworkRunner;

        private bool _canceled;

        private readonly RunningTestsCount _runningTestsCount;

        private bool CheckTasksCanceled()
        {
            return _canceled;
        }

        public void Cancel()
        {
            _canceled = true;
        }

        protected virtual void StatusChanged(TestMethod testcasemethod)
        {
            var handler = MethodStatusChanged;
            if (handler != null) handler(testcasemethod);
        }

        protected virtual void OnOnTestFinished(TestMethod testcasemethod)
        {
            var handler = OnTestFinished;
            if (handler != null) handler(testcasemethod);
        }

        protected virtual void OnTestCaseStatusChanged(TestMethod testcasemethod)
        {
            StatusChanged(testcasemethod);
        }

        public RCRunnerAPI()
        {
            _runningTestsCount = new RunningTestsCount();
            _testCasesController = new TestCasesController();
            TestClassesList = new List<TestMethod>();
            _testCasesController.TestRunFinished += OnTaskTestRunFinishedEvent;
            _testCasesController.TestCaseStatusChanged += OnTestCaseStatusChanged;
            _testCasesController.Canceled += CheckTasksCanceled;
            _canceled = false;
        }

         public void SetTestRunner(ITestFrameworkRunner testFrameworkRunner)
        {
            _testFrameworkRunner = testFrameworkRunner;
            _testCasesController.SetTestRunner(testFrameworkRunner);
        }

        private string GetDescriptionAttributeValue(MemberInfo methodInfo)
        {
            var descriptionAttributeName = _testFrameworkRunner.GetTestMethodDescriptionAttribute();

            var descriptionAttr = Attribute.GetCustomAttributes(methodInfo).FirstOrDefault(x => x.GetType().FullName == descriptionAttributeName);

            var description = string.Empty;

            if (descriptionAttr == null) return description;

            var descriptionProperty = descriptionAttr.GetType().GetProperty("Description");

            if (descriptionProperty != null)
            {
                description = descriptionProperty.GetValue(descriptionAttr, null) as string;    
            }

            return description;
        }

        private IList<MethodInfo> GetTestMethodsList(Type classObject)
        {
            var testAttributeName = _testFrameworkRunner.GetTestMethodAttribute();

            var rawMethods = classObject.GetMethods().Where(x => x.GetCustomAttributes().Any(y => y.GetType().FullName == testAttributeName));

            var methodInfos = rawMethods as IList<MethodInfo> ?? rawMethods.ToList();

            return methodInfos;
        }

        public void LoadAssembly()
        {
            var assembly = Assembly.LoadFrom(_testFrameworkRunner.GetAssemblyPath());

            TestClassesList.Clear();

            foreach (var classes in assembly.GetTypes())
            {
                if (!classes.IsClass && !classes.IsPublic) continue;

                var methodInfos = GetTestMethodsList(classes);

                if (!methodInfos.Any()) continue;

                var className = classes.Name;

                foreach (var testMethod in methodInfos.Select(methodInfo => new TestMethod
                {
                    ClassName = className,
                    Method = methodInfo,
                    DisplayName = methodInfo.Name,
                    TestExecutionStatus = TestExecutionStatus.Active,
                    LastExecutionErrorMsg = string.Empty,
                    TestDescription = GetDescriptionAttributeValue(methodInfo)
                }))

                    TestClassesList.Add(testMethod);
            }
        }

        public void RunTestCases(List<TestMethod> testCasesList)
        {
            _runningTestsCount.Reset();
            _canceled = false;
            _testCasesController.DoWork(testCasesList);
        }

        private void OnTaskTestRunFinishedEvent(TestMethod testcaseMethod)
        {
            OnOnTestFinished(testcaseMethod);
        }

    }
}
