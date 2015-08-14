using System;
using System.Collections.Generic;
using System.Linq;
using CommandLine;
using CommandLine.Text;

namespace RCRunner.Console
{
    class Options
    {
        [Option('a', "assembly", Required = true, HelpText = "Full path to the test scripts assembly")]
        public string Assembly { get; set; }

        [Option('r', "runner", Required = true, HelpText = "Test runner assembly full path")]
        public string Runner { get; set; }

        [Option('t', "attr", Required = false, HelpText = "Only run test scripts that contains this attribute")]
        public string Attr { get; set; }

        [Option('c', "class", Required = false, HelpText = "Only run test scripts of the specified class")]
        public string @Class { get; set; }

        [Option('s', "summary", Required = false, DefaultValue = true, HelpText = "Print a summary of the test execution")]
        public bool Summary { get; set; }

        [Option('f', "feedback", Required = false, DefaultValue = false, HelpText = "Print test execution feedback")]
        public bool Feedback { get; set; }

        [ParserState]
        public IParserState LastParserState { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this, current => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }

    class Program
    {
        private static readonly PluginLoader PluginLoader;

        private static readonly RCRunnerAPI RCRunnerAPI;

        private static int _totFailed, _totPassed;

        private static readonly Options Options;

        private static int _totScripts;

        enum ExitCode
        {
            Success = 0,
            Failed = 1,
            Error = 10
        }

        static Program()
        {
            PluginLoader = new PluginLoader();
            RCRunnerAPI = new RCRunnerAPI { MethodStatusChanged = OnMethodStatusChanged };
            Options = new Options();
        }

        private static void PrintSummary()
        {
            var summary = string.Format("Total Scripts: {0}   Failed scripts: {1}   Passed scripts: {2}", _totScripts, _totFailed, _totPassed);
            System.Console.WriteLine("");
            System.Console.WriteLine(summary);
        }

        static int Main(string[] args)
        {
            if (!Parser.Default.ParseArguments(args, Options))
            {
                return (int)ExitCode.Error;
            }

            if (!PrepareforRun(Options.Runner, Options.Assembly)) return (int)ExitCode.Error;

            var testList = FilterTestListByAttr(RCRunnerAPI.TestClassesList, Options.Attr, Options.Class);

            _totScripts = testList.Count();

            RCRunnerAPI.RunTestCases(testList);

            if (_totFailed > 0) return (int)ExitCode.Failed;

            return (int)ExitCode.Success;
        }

        private static void OnMethodStatusChanged(TestScript testcasemethod)
        {
            if (testcasemethod.TestExecutionStatus == TestExecutionStatus.Failed) _totFailed++;

            if (testcasemethod.TestExecutionStatus == TestExecutionStatus.Passed) _totPassed++;

            if (Options.Feedback)
            {
                if (testcasemethod.TestExecutionStatus == TestExecutionStatus.Failed ||
                    testcasemethod.TestExecutionStatus == TestExecutionStatus.Passed)
                {
                    var message = string.Format("{0}.{1} - {2}", testcasemethod.ClassName, testcasemethod.Name,
                        testcasemethod.TestExecutionStatus);
                    System.Console.WriteLine(message);
                }
            }

            if (!RCRunnerAPI.Done()) return;

            if (Options.Summary) PrintSummary();
        }

        private static List<TestScript> FilterTestListByAttr(List<TestScript> testList, string attr, string className)
        {
            var result = testList;

            if (!string.IsNullOrEmpty(attr))
            {
                result = result.Where(x => x.CustomAtributteList.Contains(attr, StringComparer.OrdinalIgnoreCase)).ToList();
            }

            if (!string.IsNullOrEmpty(className))
            {
                result = result.Where(x => x.ClassName.Equals(className, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            return result;
        }

        private static bool PrepareforRun(string runnerName, string assembly)
        {
            PluginLoader.LoadTestExecutionPlugins();
            
            PluginLoader.LoadTestRunnerAssembly(runnerName);

            var testFrameworkRunner = PluginLoader.TestRunnersPluginList.FirstOrDefault();

            if (testFrameworkRunner == null) return false;

            testFrameworkRunner.SetAssemblyPath(assembly);

            RCRunnerAPI.SetTestRunner(testFrameworkRunner);

            RCRunnerAPI.SetPluginLoader(PluginLoader);

            RCRunnerAPI.LoadAssembly();

            return true;
        }

    }
}
