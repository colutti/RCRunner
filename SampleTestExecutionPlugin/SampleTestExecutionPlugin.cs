using System.Collections.Concurrent;
using System.Collections.Generic;
using RCRunner.Shared.Lib;
using RCRunner.Shared.Lib.PluginsStruct;

namespace SampleTestExecutionPlugin
{
    public class SampleTestExecutionPlugin : TestExecution
    {
        /// <summary>
        ///     Called when a test script finishes its executions
        /// </summary>
        /// <param name="idTestCase">Name of the test case</param>
        public override void AfterTestExecution(string idTestCase)
        {
           // MessageBox.Show(@"SampleTestExecutionPlugin.AfterTestExecution");
        }

        /// <summary>
        /// Called before a test run begins
        /// </summary>
        public override void BeforeTestRun(List<TestScript> testCasesList)
        {
            //MessageBox.Show(@"SampleTestExecutionPlugin.BeforeTestRun");
        }

        /// <summary>
        /// Called after a test run ends
        /// </summary>
        public override void AfterTestRun(List<TestScript> testCasesList)
        {
            //MessageBox.Show(@"SampleTestExecutionPlugin.AfterTestRun");
        }
    }
}
