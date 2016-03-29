using System.Collections.Generic;

namespace RCRunner.Shared.Lib.PluginsStruct
{
    /// <summary>
    ///     Defines an Interface to comunicate with different test frameworks, such as mstest, nunit, etc
    /// </summary>
    public class TestFrameworkRunner
    {
        public virtual List<TestScript> ReadTestResultsFromFolder(string folder)
        {
            return null;
        }

        /// <summary>
        ///     Executes a test case specified by the testcase param
        /// </summary>
        /// <param name="testScript"></param>
        public virtual void  RunTest(TestScript testScript) {}

        /// <summary>
        ///     Returns the assembly that contains the test cases to run
        /// </summary>
        /// <returns>Returns the assembly path</returns>
        public virtual string GetAssemblyPath()
        {
            return string.Empty;
        }

        /// <summary>
        ///     Sets the assembly that contains the test cases to run
        /// </summary>
        /// <param name="assemblyPath">The assembly that contains the test cases to run</param>
        public virtual void  SetAssemblyPath(string assemblyPath){}

        /// <summary>
        ///     Retuns the folder which the tests results will be stored
        /// </summary>
        /// <returns>The folder which the tests results will be stored</returns>
        public virtual string GetTestResultsFolder()
        {
            return null;
        }

        /// <summary>
        ///     Sets the folder which the tests results will be stored
        /// </summary>
        /// <param name="folder">The folder which the tests results will be stored</param>
        public virtual void  SetTestResultsFolder(string folder){}

        /// <summary>
        ///     Returns the name of the attribute that defines a test method
        /// </summary>
        /// <returns>The name of the attribute that defines a test method</returns>
        public virtual string GetTestMethodAttribute()
        {
            return null;
        }

        /// <summary>
        ///     Returns the name of the attribute that defines a description for a test method
        /// </summary>
        /// <returns>The name of the attribute that defines description for a test method</returns>
        public virtual string GetTestMethodDescriptionAttribute()
        {
            return string.Empty;
        }

        /// <summary>
        ///     Returns the name of the test runner
        /// </summary>
        /// <returns></returns>
        public virtual string GetDisplayName()
        {
            return string.Empty;
        }
       
    }
}