using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RCRunner
{
    public delegate bool CheckCanceled();
    
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

    public class RCRunnerAPI
    {
        public readonly List<TestMethod> TestClassesList;

        private readonly TestCasesController _testCasesController;

        public event TestRunFinishedDelegate OnTestFinished;

        public event TestRunFinishedDelegate MethodStatusChanged;

        public event Action OnTestExecutionFinished;

        protected virtual void OnOnTestExecutionFinished()
        {
            var handler = OnTestExecutionFinished;
            if (handler != null) handler();
        }

        private ITestFrameworkRunner _testFrameworkRunner;

        private bool _canceled;

        public readonly RunningTestsCount RunningTestsCount;

        private bool CheckTasksCanceled()
        {
            return _canceled;
        }

        public void Cancel()
        {
            _canceled = true;
        }

        protected virtual void OnOnTestFinished(TestMethod testcasemethod)
        {
            RunningTestsCount.Update(testcasemethod);

            if (OnTestFinished != null) OnTestFinished(testcasemethod);

            if (RunningTestsCount.Done()) OnOnTestExecutionFinished();
        }

        public RCRunnerAPI()
        {
            RunningTestsCount = new RunningTestsCount();
            _testCasesController = new TestCasesController();
            TestClassesList = new List<TestMethod>();
            _testCasesController.TestRunFinished += OnTaskTestRunFinishedEvent;
            _testCasesController.TestCaseStatusChanged += MethodStatusChanged;
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
            RunningTestsCount.Reset();
            _canceled = false;
            _testCasesController.DoWork(testCasesList);
        }

        private void OnTaskTestRunFinishedEvent(TestMethod testcaseMethod)
        {
            OnOnTestFinished(testcaseMethod);
        }

    }
}
