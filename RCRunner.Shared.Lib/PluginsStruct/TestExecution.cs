using System.Collections.Generic;

namespace RCRunner.Shared.Lib.PluginsStruct
{
    /// <summary>
    ///     Defines an interface for plugins that needs to be called when a test execution happens
    /// </summary>
    public class TestExecution
    {
        /// <summary>
        ///     Called when a test script finishes its executions
        /// </summary>
        /// <param name="idTestCase">Name of the test case</param>
        public virtual void AfterTestExecution(string idTestCase)
        {
            
        }

        /// <summary>
        /// Called before a test run begins
        /// </summary>
        public virtual void BeforeTestRun(List<TestScript> testCasesList)
        {
            
        }

        /// <summary>
        /// Called after a test run ends
        /// </summary>
        public virtual void AfterTestRun(List<TestScript> testCasesList)
        {
            
        }
    }
}