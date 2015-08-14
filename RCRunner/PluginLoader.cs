using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using RCRunner.PluginsStruct;

namespace RCRunner
{
    public class PluginLoader
    {
        /// <summary>
        ///     List of test plugins
        /// </summary>
        public List<TestExecution> TestExecutionPlugiList;

        /// <summary>
        ///     List of all the test runners plugins
        /// </summary>
        public List<TestFrameworkRunner> TestRunnersPluginList;

        /// <summary>
        ///     Basic constructor
        /// </summary>
        public PluginLoader()
        {
            TestRunnersPluginList = new List<TestFrameworkRunner>();
            TestExecutionPlugiList = new List<TestExecution>();
        }

        /// <summary>
        ///     Loads an assembly that contains a test runner
        /// </summary>
        /// <param name="file"></param>
        public void LoadTestRunnerAssembly(string file)
        {
            try
            {
                var assembly = Assembly.LoadFrom(file);

                var classes = from type in assembly.GetTypes()
                    where typeof (TestFrameworkRunner).IsAssignableFrom(type) && type.IsPublic
                    select type;

                foreach (
                    var testRunnerObj in
                        classes.Select(@class => (TestFrameworkRunner) Activator.CreateInstance(@class)))
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
            var resultFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins", "TestRunners");

            if (!Directory.Exists(resultFilePath)) return;

            var filePaths = Directory.GetFiles(resultFilePath, "*.dll");

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
            var resultFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins", "TestExecution");

            if (!Directory.Exists(resultFilePath)) return;

            var filePaths = Directory.GetFiles(resultFilePath, "*.dll");

            foreach (var file in filePaths)
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
                var assembly = Assembly.LoadFrom(file);

                var classes = from type in assembly.GetTypes()
                    where typeof (TestExecution).IsAssignableFrom(type) && type.IsPublic
                    select type;

                foreach (
                    var testExecutionObj in classes.Select(@class => (TestExecution) Activator.CreateInstance(@class)))
                {
                    TestExecutionPlugiList.Add(testExecutionObj);
                }
            }
                // ReSharper disable once EmptyGeneralCatchClause
            catch (Exception)
            {
            }
        }

        public void CallAfterTestExecutionPlugins(string testCase)
        {
            foreach (var testExecution in TestExecutionPlugiList)
            {
                testExecution.AfterTestExecution(testCase);
            }
        }

        public void CallBeforeTestRunPlugins()
        {
            foreach (var testExecution in TestExecutionPlugiList)
            {
                testExecution.BeforeTestRun();
            }
        }

        public void CallAfterTestRunPlugins()
        {
            foreach (var testExecution in TestExecutionPlugiList)
            {
                testExecution.AfterTestRun();
            }
        }
    }
}