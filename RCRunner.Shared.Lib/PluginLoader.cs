using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using RCRunner.Shared.Lib.PluginsStruct;

namespace RCRunner.Shared.Lib
{
    public class PluginLoader
    {
        /// <summary>
        ///     List of test plugins
        /// </summary>
        public List<TestExecution> TestExecutionPlugiList;

        private readonly object _lockObj;

        /// <summary>
        ///     List of all the test runners plugins
        /// </summary>
        public List<TestFrameworkRunner> TestRunnersPluginList;

        /// <summary>
        ///     Basic constructor
        /// </summary>
        public PluginLoader()
        {
            _lockObj = new object();
            TestRunnersPluginList = new List<TestFrameworkRunner>();
            TestExecutionPlugiList = new List<TestExecution>();
        }

        /// <summary>
        ///     Loads an assembly that contains a test runner
        /// </summary>
        /// <param name="file"></param>
        public void LoadTestRunnerAssembly(string file)
        {
            lock (_lockObj)
            {
                try
                {
                    var assembly = Assembly.LoadFrom(file);

                    var classes = from type in assembly.GetTypes()
                        where type.IsSubclassOf(typeof (TestFrameworkRunner)) && type.IsPublic
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
        }

        /// <summary>
        ///     Loads all test runners plugins from a folder
        /// </summary>
        public void LoadTestRunnersPlugins()
        {
            lock (_lockObj)
            {
                var resultFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins", "TestRunners");

                if (!Directory.Exists(resultFilePath)) return;

                var filePaths = Directory.GetFiles(resultFilePath, "*.dll");

                foreach (string file in filePaths)
                {
                    LoadTestRunnerAssembly(file);
                }
            }
        }

        /// <summary>
        ///     Loadas all test plugins from a folder
        /// </summary>
        public void LoadTestExecutionPlugins()
        {
            lock (_lockObj)
            {
                var resultFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins", "TestExecution");

                if (!Directory.Exists(resultFilePath)) return;

                var filePaths = Directory.GetFiles(resultFilePath, "*.dll");

                foreach (var file in filePaths)
                {
                    LoadTestExecutionAssembly(file);
                }
            }
        }

        /// <summary>
        ///     Loads an assembly that contatis a test plugin
        /// </summary>
        /// <param name="file"></param>
        private void LoadTestExecutionAssembly(string file)
        {
            try
            {
                var assembly = Assembly.LoadFrom(file);

                var classes = from type in assembly.GetTypes()
                    where type.IsSubclassOf(typeof (TestExecution)) && type.IsPublic
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

        /// <summary>
        /// Call all the plugins notifying that a test finished to run
        /// </summary>
        /// <param name="testCase"></param>
        public void CallAfterTestExecutionPlugins(string testCase)
        {
            lock (_lockObj)
            {
                foreach (var testExecution in TestExecutionPlugiList)
                {
                    testExecution.AfterTestExecution(testCase);
                }
            }
        }

        /// <summary>
        /// Call all the plugins with "before all tests execution" notification
        /// </summary>
        public void CallBeforeTestRunPlugins(List<TestScript> testCasesList)
        {
            lock (_lockObj)
            {
                foreach (var testExecution in TestExecutionPlugiList)
                {
                    testExecution.BeforeTestRun(testCasesList);
                }
            }
        }

        /// <summary>
        /// Call all the plugins with "after all tests execution" notification
        /// </summary>
        public void CallAfterTestRunPlugins(List<TestScript> testCasesList)
        {
            lock (_lockObj)
            {
                foreach (var testExecution in TestExecutionPlugiList)
                {
                    testExecution.AfterTestRun(testCasesList);
                }
            }
        }
    }
}