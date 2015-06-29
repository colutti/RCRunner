using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace RCRunner
{
    public class PluginLoader
    {
        /// <summary>
        ///     List of test plugins
        /// </summary>
        public List<ITestExecution> TestExecutionPlugiList;

        /// <summary>
        ///     List of all the test runners plugins
        /// </summary>
        public List<ITestFrameworkRunner> TestRunnersPluginList;

        /// <summary>
        ///     Basic constructor
        /// </summary>
        public PluginLoader()
        {
            TestRunnersPluginList = new List<ITestFrameworkRunner>();
            TestExecutionPlugiList = new List<ITestExecution>();
        }

        /// <summary>
        ///     Loads an assembly that contains a test runner
        /// </summary>
        /// <param name="file"></param>
        public void LoadTestRunnerAssembly(string file)
        {
            try
            {
                Assembly assembly = Assembly.LoadFrom(file);

                IEnumerable<Type> classes = from type in assembly.GetTypes()
                    where typeof (ITestFrameworkRunner).IsAssignableFrom(type) && type.IsPublic
                    select type;

                foreach (
                    ITestFrameworkRunner testRunnerObj in
                        classes.Select(@class => (ITestFrameworkRunner) Activator.CreateInstance(@class)))
                {
                    TestRunnersPluginList.Add(testRunnerObj);
                }
            }
                // ReSharper disable once EmptyGeneralCatchClause
            catch (Exception)
            {
            }
        }

        /// <summary>
        ///     Loads all test runners plugins from a folder
        /// </summary>
        public void LoadTestRunnersPlugins()
        {
            string resultFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins", "TestRunners");

            if (!Directory.Exists(resultFilePath)) return;

            string[] filePaths = Directory.GetFiles(resultFilePath, "*.dll");

            foreach (string file in filePaths)
            {
                LoadTestRunnerAssembly(file);
            }
        }

        /// <summary>
        ///     Loadas all test plugins from a folder
        /// </summary>
        public void LoadTestExecutionPlugins()
        {
            string resultFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins", "TestExecution");

            if (!Directory.Exists(resultFilePath)) return;

            string[] filePaths = Directory.GetFiles(resultFilePath, "*.dll");

            foreach (string file in filePaths)
            {
                LoadTestExecutionAssembly(file);
            }
        }

        /// <summary>
        ///     Loadas an assembly that contatis a test plugin
        /// </summary>
        /// <param name="file"></param>
        private void LoadTestExecutionAssembly(string file)
        {
            try
            {
                Assembly assembly = Assembly.LoadFrom(file);

                IEnumerable<Type> classes = from type in assembly.GetTypes()
                    where typeof (ITestExecution).IsAssignableFrom(type) && type.IsPublic
                    select type;

                foreach (
                    ITestExecution testExecutionObj in
                        classes.Select(@class => (ITestExecution) Activator.CreateInstance(@class)))
                {
                    TestExecutionPlugiList.Add(testExecutionObj);
                }
            }
                // ReSharper disable once EmptyGeneralCatchClause
            catch (Exception)
            {
            }
        }

        public void RunTestExecutionPlugins(string testCase)
        {
            foreach (ITestExecution testExecution in TestExecutionPlugiList)
            {
                testExecution.AfterTestExecution(testCase);
            }
        }
    }
}