using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using OfficeOpenXml;
using RCRunner.Shared.Lib.PluginsStruct;

namespace RCRunner.Shared.Lib
{
    public delegate bool CheckCanceled();

    public class RCRunnerAPI
    {
        public readonly List<TestScript> TestClassesList;

        private List<TestScript> _runningTestCases; 

        private readonly TestScriptsController _testCasesController;

        public TestRunFinishedDelegate MethodStatusChanged;

        public List<string> CustomAttributesList;

        private PluginLoader _pluginLoader;

        public void OnMethodStatusChanged(TestScript testcaseScript)
        {
            RunningTestsCount.Update(testcaseScript);
            if (MethodStatusChanged != null) MethodStatusChanged(testcaseScript);

            if (Done())
            {
                _pluginLoader.CallAfterTestRunPlugins(TestClassesList);
            }
        }

        private TestFrameworkRunner _testFrameworkRunner;

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

        public RCRunnerAPI()
        {
            RunningTestsCount = new RunningTestsCount();
            _testCasesController = new TestScriptsController(RunningTestsCount);
            TestClassesList = new List<TestScript>();
            CustomAttributesList = new List<string>();
            _testCasesController.TestCaseStatusChanged = OnMethodStatusChanged;
            _testCasesController.Canceled = CheckTasksCanceled;
            _canceled = false;
        }

        public void SetTestRunner(TestFrameworkRunner testFrameworkRunner)
        {
            _testFrameworkRunner = testFrameworkRunner;
            _testCasesController.SetTestRunner(testFrameworkRunner);
        }

        public void SetPluginLoader(PluginLoader pluginLoader)
        {
            _pluginLoader = pluginLoader;
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

        private List<string> GetCustomAttributes(MethodInfo method)
        {
            if (method == null) return null;

            var testAttributeName = _testFrameworkRunner.GetTestMethodAttribute();

            var descriptionAttributeName = _testFrameworkRunner.GetTestMethodDescriptionAttribute();

            var attributesList = method.CustomAttributes.Where(x => x.AttributeType.FullName != testAttributeName && x.AttributeType.FullName != descriptionAttributeName).Select(x => x.AttributeType.Name.Replace("Attribute", "")).ToList();

            var tempList = attributesList.Except(CustomAttributesList).ToList();

            CustomAttributesList.AddRange(tempList);

            return attributesList;
        }

        public void LoadAssembly()
        {
            var assembly = Assembly.LoadFrom(_testFrameworkRunner.GetAssemblyPath());

            TestClassesList.Clear();

            CustomAttributesList.Clear();

            foreach (var classes in assembly.GetTypes())
            {
                if (!classes.IsClass && !classes.IsPublic) continue;

                var methodInfos = GetTestMethodsList(classes);

                if (!methodInfos.Any()) continue;

                var className = classes.Name;

                foreach (var testMethod in methodInfos)
                {
                    var testScript = new TestScript(_pluginLoader)
                    {
                        ClassName = className,
                        Name = testMethod.Name,
                        TestExecutionStatus = TestExecutionStatus.Active,
                        LastExecutionErrorMsg = string.Empty,
                        TestDescription = GetDescriptionAttributeValue(testMethod),
                        CustomAtributteList = GetCustomAttributes(testMethod)
                    };

                    TestClassesList.Add(testScript);
                }
            }
        }

        public void RunTestCases(List<TestScript> testCasesList)
        {
            RunningTestsCount.Reset();
            _canceled = false;
            _runningTestCases = testCasesList;

            _pluginLoader.CallBeforeTestRunPlugins(testCasesList);

            foreach (var testMethod in testCasesList)
            {
                testMethod.RetryCount = 0;
                testMethod.TestExecutionStatus = TestExecutionStatus.Waiting;
                testMethod.LastExecutionErrorMsg = string.Empty;
                OnMethodStatusChanged(testMethod);
            }
            _testCasesController.DoWork(testCasesList);
        }

        public bool Done()
        {
            return RunningTestsCount.Done();
        }


        /// <summary>
        /// Exports a test run to excel
        /// </summary>
        /// <param name="excelFilepath"></param>
        /// <param name="testCasesList"></param>
        public static void ExportToExcel(string excelFilepath, List<TestScript> testCasesList)
        {
            if (!testCasesList.Any()) return;

            File.Delete(excelFilepath);

            var newFile = new FileInfo(excelFilepath);
            var oXl = new ExcelPackage(newFile);

            // Create a workbook and add sheet
            var oSheet = oXl.Workbook.Worksheets.Add("Report");

            oSheet.Name = "Report";

            // Write the column names to the work sheet
            oSheet.Cells[1, 1].Value = "Class Name";
            oSheet.Cells[1, 2].Value = "Test Script";
            oSheet.Cells[1, 3].Value = "Status";
            oSheet.Cells[1, 4].Value = "Error Classification";
            oSheet.Cells[1, 5].Value = "Description";
            oSheet.Cells[1, 6].Value = "Duration";
            oSheet.Cells[1, 7].Value = "Last Error";

            var row = 2;

            foreach (var item in testCasesList)
            {

                oSheet.Cells[row, 1].Value = item.ClassName;
                oSheet.Cells[row, 2].Value = item.Name;
                oSheet.Cells[row, 3].Value = item.TestExecutionStatus.ToString();
                oSheet.Cells[row, 4].Value = item.ErrorClassification;
                oSheet.Cells[row, 5].Value = item.TestDescription;
                oSheet.Cells[row, 6].Value = item.Duration.ToString();
                oSheet.Cells[row, 7].Value = item.LastExecutionErrorMsg;

                row++;
            }

            row += 2;

            // Add summmary
            oSheet.Cells[row++, 1].Value = "Testcases Passed = " + testCasesList.Count(x => x.TestExecutionStatus == TestExecutionStatus.Passed);
            oSheet.Cells[row++, 1].Value = "Testcases Failed = " + testCasesList.Count(x => x.TestExecutionStatus == TestExecutionStatus.Failed);

            // Autoformat the sheet
            oSheet.Cells[1, 1, row, 7].AutoFitColumns();

            oXl.Save();
        }

    }
}
