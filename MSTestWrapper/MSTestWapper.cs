using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RCRunner.Shared.Lib;
using RCRunner.Shared.Lib.PluginsStruct;

namespace MSTestWrapper
{

    public class ErrorClassificationSuggestionItem
    {
        [XmlElement("PartialDescription")]
        public string PartialDescription;
        [XmlElement("Suggestion")]
        public string Suggestion;
    }

    /// <summary>
    /// This is a class that will try to suggest error classification based on the analysis of the error. The configuration will be in a XML file,
    /// where you can tell how to find out for specific errors and return suggested classifications
    /// </summary>
    [XmlRoot("ErrorClassificationSuggestionList")]
    public class ErrorClassificationSuggestionList
    {
        [XmlElement("Item")]
        public List<ErrorClassificationSuggestionItem> ErrorClassificationList;

        public ErrorClassificationSuggestionList()
        {
            ErrorClassificationList = new List<ErrorClassificationSuggestionItem>();   
        }
        
        /// <summary>
        /// Given an error message, this method will try to return an error classification suggestion
        /// </summary>
        /// <param name="errorMsg"></param>
        /// <returns></returns>
        public string GetErrorClassificationSugestion(string errorMsg)
        {
            var suggestion = ErrorClassificationList.FirstOrDefault(s => errorMsg.Contains(s.PartialDescription));

            return suggestion != null ? suggestion.Suggestion : string.Empty;
        }

        /// <summary>
        /// Contructs and reads the configuration from a XML file
        /// </summary>
        /// <returns></returns>
        public static ErrorClassificationSuggestionList CreateNew()
        {

            const string errorClassificationSuggestionXmlPath = "errorClassificationSuggestionList.xml";

            var errorClassificationSuggestionList = new ErrorClassificationSuggestionList();

            if (!File.Exists(errorClassificationSuggestionXmlPath)) return errorClassificationSuggestionList;

            var ser = new XmlSerializer(typeof(ErrorClassificationSuggestionList));

            using (var readtext = new StreamReader(errorClassificationSuggestionXmlPath))
            {
                errorClassificationSuggestionList = (ErrorClassificationSuggestionList)ser.Deserialize(readtext);
            }
            
            return errorClassificationSuggestionList;
        }
    }
    
    /// <summary>
    /// A test wrapper that implements the ITestFrameworkRunner as an adapter for the MSTest test framework
    /// </summary>
    public class MSTestWrapper : TestFrameworkRunner
    {
        private string _assemblyPath;
        private string _resultFilePath;
        private readonly ErrorClassificationSuggestionList _errorClassificationSuggestionList;

        public MSTestWrapper()
        {
            _errorClassificationSuggestionList = ErrorClassificationSuggestionList.CreateNew();
        }

        /// <summary>
        /// Returns the assembly that contains the test cases to run
        /// </summary>
        /// <returns>Returns the assembly path</returns>
        public override string GetAssemblyPath()
        {
            return _assemblyPath;
        }

        /// <summary>
        /// Sets the assembly that contains the test cases to run
        /// </summary>
        /// <param name="assemblyPath">The assembly that contains the test cases to run</param>
        public override void SetAssemblyPath(string assemblyPath)
        {
            _assemblyPath = assemblyPath;
        }

        /// <summary>
        /// Retuns the folder which the tests results will be stored
        /// </summary>
        /// <returns>The folder which the tests results will be stored</returns>
        public override string GetTestResultsFolder()
        {
            return _resultFilePath;
        }

        /// <summary>
        /// Sets the folder which the tests results will be stored
        /// </summary>
        /// <param name="folder">The folder which the tests results will be stored</param>
        public override void SetTestResultsFolder(string folder)
        {
            _resultFilePath = folder;
        }

        /// <summary>
        /// Returns the name of the attribute that defines a test method
        /// </summary>
        /// <returns>The name of the attribute that defines a test method</returns>
        public override string GetTestMethodAttribute()
        {
            return typeof(TestMethodAttribute).FullName;
        }

        /// <summary>
        /// Deletes a file wating in case that it is being used by other applications
        /// </summary>
        /// <param name="file"></param>
        private static void SafeDeleteFile(string file)
        {
            try
            {
                File.Delete(file);
            }
            catch (Exception)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                Thread.Sleep(2000);
                File.Delete(file);
            }
        }

        /// <summary>
        /// Check if the error message returned by the test case is a timeout error
        /// </summary>
        /// <param name="testCase">The test cases to run</param>
        /// <param name="errorMsg">The error message</param>
        /// <returns></returns>
        private bool InternalRunTest(string testCase, ref string errorMsg)
        {
            Debug.Assert(_resultFilePath != null, "_resultFilePath != null");
            var resultFilePath = Path.Combine(_resultFilePath, testCase);
            Directory.CreateDirectory(resultFilePath);

            var resultFile = Path.Combine(resultFilePath, testCase + ".trx");

            if (File.Exists(resultFile))
            {
                resultFile = Path.Combine(resultFilePath, testCase + "(2)" + ".trx");
            }

            var msTestPath = Settings.Default.MSTestExeLocation;

            if (!File.Exists(msTestPath))
                throw new FileNotFoundException("MSTest app not found on the specified path", msTestPath);

            if (!File.Exists(_assemblyPath))
                throw new FileNotFoundException("Test Assembly not found on the specified path", _assemblyPath);

            var testContainer = "/testcontainer:" + "\"" + _assemblyPath + "\"";
            var testParam = "/test:" + testCase;

            var resultParam = "/resultsfile:" + "\"" + resultFile + "\"";

            SafeDeleteFile(resultFile);

            try
            {
                var p = new Process
                {
                    StartInfo =
                    {
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        FileName = msTestPath,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        Arguments = testContainer + " " + testParam + " " + resultParam
                    }
                };

                if (!p.Start())
                {
                    throw new Exception("Error starting the MSTest process", new Exception(p.StandardError.ReadToEnd()));
                }

                p.WaitForExit();

                var dummy = string.Empty;

                var testResult = GetTestStatusFromTrxFile(resultFile, ref errorMsg, ref dummy);

                return testResult;
            }

            finally
            {
                CleanUpDirectories(resultFilePath);
            }
        }



        /// <summary>
        /// Executes a test case specified by the testcase param
        /// </summary>
        /// <param name="testScript"></param>
        public override void RunTest(TestScript testScript)
        {
            var timer = new Stopwatch();
            timer.Start();
            try
            {
                var errorMsg = string.Empty;

                var testResult = InternalRunTest(testScript.Name, ref errorMsg);

                if (testResult) return;

                // when a test fails, I will try to find out why, based on the configuration XML file that tells me how to search for specific errors
                // and return a error classification sugestion
                testScript.ErrorClassification = _errorClassificationSuggestionList.GetErrorClassificationSugestion(errorMsg);

                throw new Exception(errorMsg);
            }
            finally
            {
                timer.Stop();
                testScript.Duration = timer.Elapsed;
            }
        }

        public override List<TestScript> ReadTestResultsFromFolder(string folder)
        {
            var testScripts = new List<TestScript>();
            var files = Directory.GetFiles(folder, "*.trx", SearchOption.AllDirectories);
            var errorMsg = string.Empty;
            var testName = string.Empty;

            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var file in files)
            {
                var testResult = GetTestStatusFromTrxFile(file, ref errorMsg, ref testName);

                var errorClassification = _errorClassificationSuggestionList.GetErrorClassificationSugestion(errorMsg);
                
                var testScript = new TestScript(null)
                {
                    TestExecutionStatus = testResult ? TestExecutionStatus.Passed : TestExecutionStatus.Failed,
                    ErrorClassification = errorClassification,
                    LastExecutionErrorMsg = errorMsg,
                    Name = testName
                };
                
                testScripts.Add(testScript);
            }
            return testScripts;
        }

        /// <summary>
        /// Gets the result outcome and, in case of a failed test case, returns the test case execution error
        /// </summary>
        /// <param name="fileName">TRX file to read from</param>
        /// <param name="errorMsg">The error message pointer to return the error when the test fails</param>
        /// <param name="testName">Name of the test</param>
        /// <returns>Returns true if the test ran successfuly or false if the test failed</returns>
        static bool GetTestStatusFromTrxFile(string fileName, ref string errorMsg, ref string testName)
        {
            var fileStreamReader = new StreamReader(fileName);
            var xmlSer = new XmlSerializer(typeof(TestRunType));
            var testRunType = (TestRunType)xmlSer.Deserialize(fileStreamReader);

            var resultType = testRunType.Items.OfType<ResultsType>().FirstOrDefault();

            if (resultType == null) throw new Exception("Cannot get the ResultsType from the TRX file");

            var unitTestResultType = resultType.Items.OfType<UnitTestResultType>().FirstOrDefault();

            if (unitTestResultType == null) throw new Exception("Cannot get the UnitTestResultType from the TRX file");

            testName = unitTestResultType.testName;

            var testResult = unitTestResultType.outcome;

            if (!testResult.ToLower().Equals("failed")) return true;

            errorMsg = ((System.Xml.XmlNode[])(((OutputType)(unitTestResultType.Items[0])).ErrorInfo.Message))[0].Value;

            return false;
        }

        /// <summary>
        /// Cleans up all folder directories in the TestResults folder
        /// </summary>
        public void CleanUpDirectories(string resultFilePath)
        {
            try
            {
                var filePaths = Directory.GetDirectories(resultFilePath);

                foreach (var folder in filePaths)
                {
                    CleanDirectory(new DirectoryInfo(folder));
                    Directory.Delete(folder);
                }

            }
            // ReSharper disable once EmptyGeneralCatchClause
            catch
            {

            }
        }

        /// <summary>
        /// Returns the name of the attribute that defines a description for a test method
        /// </summary>
        /// <returns>The name of the attribute that defines description for a test method</returns>
        public override string GetTestMethodDescriptionAttribute()
        {
            return typeof(DescriptionAttribute).FullName;
        }

        /// <summary>
        /// Returns the name of the test runner
        /// </summary>
        /// <returns></returns>
        public override string GetDisplayName()
        {
            return "MSTest";
        }

        /// <summary>
        /// Cleans a single directory content
        /// </summary>
        /// <param name="directory"></param>
        private static void CleanDirectory(DirectoryInfo directory)
        {
            foreach (var file in directory.GetFiles())
            {
                file.Delete();
            }
            foreach (var subDirectory in directory.GetDirectories())
            {
                subDirectory.Delete(true);
            }
        }

    }
}
