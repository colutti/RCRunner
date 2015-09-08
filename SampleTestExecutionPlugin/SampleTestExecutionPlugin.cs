using System.Windows.Forms;
using RCRunner.PluginsStruct;

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
        public override void BeforeTestRun()
        {
            //MessageBox.Show(@"SampleTestExecutionPlugin.BeforeTestRun");
        }

        /// <summary>
        /// Called after a test run ends
        /// </summary>
        public override void AfterTestRun()
        {
            //MessageBox.Show(@"SampleTestExecutionPlugin.AfterTestRun");
        }
    }
}
