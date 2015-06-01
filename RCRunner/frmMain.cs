﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace RCRunner
{
    public partial class FrmMain : Form
    {
        private readonly RCRunnerAPI _rcRunner;
        private delegate void SetTextCallback(TestScript testcaseScript);
        private delegate void SetEnabledCallback(bool enabled);

        private PluginLoader _pluginLoader;

        private readonly Color _testActive = Color.FloralWhite;
        private readonly Color _testFailed = Color.Red;
        private readonly Color _testPassed = Color.GreenYellow;
        private readonly Color _testRunning = Color.Blue;
        private readonly Color _testWaiting = Color.DarkOrange;
        private ITestFrameworkRunner _testFrameworkRunner;

        public FrmMain()
        {
            InitializeComponent();

            LoadTestRunners();

            _rcRunner = new RCRunnerAPI();
            _rcRunner.OnTestFinished += OnTaskFinishedEvent;
            _rcRunner.MethodStatusChanged += OnMethodStatusChanged;
            _rcRunner.OnTestExecutionFinished += OnTestExecutionFinished;

            trvTestCases.CheckBoxes = true;

            lblFailedScripts.ForeColor = _testFailed;
            lblPassedScripts.ForeColor = _testPassed;
            lblRunningScripts.ForeColor = _testRunning;
            lblWatingScripts.ForeColor = _testWaiting;

            ResetTestExecution();

            DisableOrEnableControls(true);
        }

        private void LoadTestRunners()
        {
            _pluginLoader = new PluginLoader();
            _pluginLoader.LoadTestRunnersPlugins();

            foreach (var testFrameworkRunner in _pluginLoader.TestRunnersPluginList)
            {
                cmbTestRunners.Items.Add(testFrameworkRunner.GetDisplayName());
            }

            if (cmbTestRunners.Items.Count <= 0) return;

            cmbTestRunners.SelectedIndex = 0;
            _testFrameworkRunner = _pluginLoader.TestRunnersPluginList[0];
        }

        private void ResetTestExecution()
        {
            prgrsbrTestProgress.Maximum = 0;
            lblPassedScripts.Text = @"Passed scripts: 0";
            lblFailedScripts.Text = @"Failed scripts: 0";
            lblRunningScripts.Text = @"Running scripts: 0";
            lblWatingScripts.Text = @"Waiting scripts: 0";
        }

        private static void CheckTreeViewNode(TreeNode node, Boolean isChecked)
        {
            foreach (TreeNode item in node.Nodes)
            {
                item.Checked = isChecked;

                if (item.Nodes.Count > 0)
                {
                    CheckTreeViewNode(item, isChecked);
                }
            }
        }

        private void PaintTreeNodeBasedOnTestStatus(TestScript testcaseScript, TreeNode node)
        {
            switch (testcaseScript.TestExecutionStatus)
            {
                case TestExecutionStatus.Active:
                    node.ForeColor = _testActive;
                    break;
                case TestExecutionStatus.Failed:
                    node.ForeColor = _testFailed;
                    break;
                case TestExecutionStatus.Passed:
                    node.ForeColor = _testPassed;
                    break;
                case TestExecutionStatus.Running:
                    node.ForeColor = _testRunning;
                    break;
                case TestExecutionStatus.Waiting:
                    node.ForeColor = _testWaiting;
                    break;
            }
        }

        private TreeNode FindNodebyTest(TestScript testcaseScript)
        {
            TreeNode nodeFound = null;

            foreach (TreeNode node in trvTestCases.Nodes)
            {
                foreach (var child in node.Nodes.Cast<TreeNode>().Where(child => child.Tag == testcaseScript))
                {
                    nodeFound = child;
                    break;
                }
            }
            return nodeFound;
        }

        private TreeNode FindNodebyClassName(TestScript testcaseScript)
        {
            return trvTestCases.Nodes.Cast<TreeNode>().FirstOrDefault(node => node.Text.Equals(testcaseScript.ClassName));
        }

        private void OnMethodStatusChanged(TestScript testcasemethod)
        {
            OnTaskFinishedEvent(testcasemethod);
        }

        private void UpdateLblTestScripts(TestScript testcaseScript)
        {
            if (lblPassedScripts.InvokeRequired)
            {
                var d = new SetTextCallback(UpdateLblTestScripts);
                Invoke(d, new object[] { testcaseScript });
            }
            else
            {
                lblPassedScripts.Text = @"Passed scripts: " + _rcRunner.RunningTestsCount.TotPassed;
                lblFailedScripts.Text = @"Failed scripts: " + _rcRunner.RunningTestsCount.TotFailed;
                lblRunningScripts.Text = @"Running scripts: " + _rcRunner.RunningTestsCount.TotRunning;
                lblWatingScripts.Text = @"Waiting scripts: " + _rcRunner.RunningTestsCount.TotWaiting;
            }
        }

        private void UpdateTreeview(TestScript testcaseScript)
        {
            if (trvTestCases.InvokeRequired)
            {
                var d = new SetTextCallback(UpdateTreeview);
                Invoke(d, new object[] { testcaseScript });
            }
            else
            {
                var nodeFound = FindNodebyTest(testcaseScript);
                if (nodeFound == null) return;
                PaintTreeNodeBasedOnTestStatus(testcaseScript, nodeFound);
                if (trvTestCases.SelectedNode != null)
                {
                    trvTestCases_AfterSelect(trvTestCases.SelectedNode, null);
                }
            }
        }

        private void OnTestExecutionFinished()
        {
            DisableOrEnableControls(true); 
        }

        private void OnTaskFinishedEvent(TestScript testcaseScript)
        {
            UpdateTreeview(testcaseScript);

            var status = testcaseScript.TestExecutionStatus;

            if (status == TestExecutionStatus.Failed || status == TestExecutionStatus.Passed)
            {
                if (prgrsbrTestProgress.InvokeRequired)
                {
                    Action d = prgrsbrTestProgress.PerformStep;
                    Invoke(d);
                }
                else
                {
                    prgrsbrTestProgress.PerformStep();
                }
            }

            UpdateLblTestScripts(testcaseScript);
        }

        private static string CreateTestResultsFolder()
        {
            var folderName = DateTime.Now.ToString(CultureInfo.InvariantCulture).Replace("/", "").Replace(":", "");
            var resultFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestResults", folderName);
            Directory.CreateDirectory(resultFilePath);
            return resultFilePath;
        }

        private void trvTestCases_AfterCheck(object sender, TreeViewEventArgs e)
        {
            CheckTreeViewNode(e.Node, e.Node.Checked);
        }

        private void trvTestCases_AfterSelect(object sender, TreeViewEventArgs e)
        {
            var node = trvTestCases.SelectedNode;
            if (node == null) return;
            if (!(node.Tag is TestScript)) return;

            var testMethod = node.Tag as TestScript;
            txtbxTestDescription.Text = testMethod.TestDescription;
            lblTestStatus.Text = @"Test Status: " + testMethod.TestExecutionStatus;
            switch (testMethod.TestExecutionStatus)
            {
                case TestExecutionStatus.Active:
                    lblTestStatus.ForeColor = _testActive;
                    break;
                case TestExecutionStatus.Failed:
                    lblTestStatus.ForeColor = _testFailed;
                    break;
                case TestExecutionStatus.Passed:
                    lblTestStatus.ForeColor = _testPassed;
                    break;
                case TestExecutionStatus.Running:
                    lblTestStatus.ForeColor = _testRunning;
                    break;
                case TestExecutionStatus.Waiting:
                    lblTestStatus.ForeColor = _testWaiting;
                    break;
            }

            txtbxTestError.Text = testMethod.LastExecutionErrorMsg;
        }

        private void LoadTreeView(IEnumerable<TestScript> testClassesList)
        {
            trvTestCases.BeginUpdate();
            trvTestCases.Nodes.Clear();
            try
            {
                foreach (var testMethod in testClassesList)
                {
                    var classNode = FindNodebyClassName(testMethod) ?? trvTestCases.Nodes.Add(testMethod.ClassName);
                    var methodNode = classNode.Nodes.Add(testMethod.Name);
                    methodNode.Tag = testMethod;
                    PaintTreeNodeBasedOnTestStatus(testMethod, methodNode);
                }
            }
            finally
            {
                trvTestCases.EndUpdate();  
            }
        }

        private void lblLoadAssembly_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            ResetTestExecution();
            DisableOrEnableControls(false);
            try
            {
                cmbxFilter.SelectedIndex = 0;

                var fileDialog = new OpenFileDialog { DefaultExt = ".dll", CheckFileExists = true, Multiselect = false };

                var foundAssembly = fileDialog.ShowDialog();

                if (foundAssembly != DialogResult.OK) return;

                var assemblyFile = fileDialog.FileName;

                _testFrameworkRunner.SetAssemblyPath(assemblyFile);

                _rcRunner.SetTestRunner(_testFrameworkRunner);

                _rcRunner.LoadAssembly();

                LoadTreeView(_rcRunner.TestClassesList);
            }
            finally
            {
                DisableOrEnableControls(true);
            }
        }

        private static int TreeviewCountCheckedNodes(IEnumerable treeNodeCollection)
        {
            var countchecked = 0;

            if (treeNodeCollection == null) return countchecked;

            foreach (TreeNode node in treeNodeCollection)
            {
                if (node.Nodes.Count == 0 && node.Checked)
                {
                    countchecked++;
                }
                else if (node.Nodes.Count > 0)
                {
                    countchecked += TreeviewCountCheckedNodes(node.Nodes);
                }
            }

            return countchecked;
        }

        private void lblExecuteTestScripts_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            ResetTestExecution();

            _rcRunner.SetTestRunner(_testFrameworkRunner);

            DisableOrEnableControls(false);

            prgrsbrTestProgress.Maximum = TreeviewCountCheckedNodes(trvTestCases.Nodes);

            var testCasesList = new List<TestScript>();

            foreach (TreeNode node in trvTestCases.Nodes)
            {
                testCasesList.AddRange(from TreeNode child in node.Nodes where child.Checked where child.Tag is TestScript select child.Tag as TestScript);
            }

            if (testCasesList.Any())
            {
                var testResultsFolder = CreateTestResultsFolder();
                _testFrameworkRunner.SetTestResultsFolder(testResultsFolder);
                _rcRunner.RunTestCases(testCasesList);
            }
            else
            {
                DisableOrEnableControls(true);
            }

        }

        private void lblCancel_Click(object sender, EventArgs e)
        {
            _rcRunner.Cancel();
        }

        private void DisableOrEnableControls(bool enable)
        {
            if (lblLoadAssembly.InvokeRequired)
            {
                SetEnabledCallback d = DisableOrEnableControls;
                Invoke(d, new object[] { enable });
            }
            else
            {
                lblCancel.Enabled = !enable;
                lblLoadAssembly.Enabled = enable;
                lblExecuteTestScripts.Enabled = enable;
            }
        }

        private void cmbxFilter_SelectionChangeCommitted(object sender, EventArgs e)
        {
            IEnumerable<TestScript> testClassesList;
            switch (cmbxFilter.SelectedIndex)
            {
                case 1:
                    testClassesList = _rcRunner.TestClassesList.Where(x => x.TestExecutionStatus == TestExecutionStatus.Running);
                    break;

                case 2:
                    testClassesList = _rcRunner.TestClassesList.Where(x => x.TestExecutionStatus == TestExecutionStatus.Waiting);
                    break;

                case 3:
                    testClassesList = _rcRunner.TestClassesList.Where(x => x.TestExecutionStatus == TestExecutionStatus.Failed);
                    break;

                case 4:
                    testClassesList = _rcRunner.TestClassesList.Where(x => x.TestExecutionStatus == TestExecutionStatus.Passed);
                    break;

                case 5:
                    testClassesList = _rcRunner.TestClassesList.Where(x => x.TestExecutionStatus == TestExecutionStatus.Active);
                    break;

                default:
                    testClassesList = _rcRunner.TestClassesList;
                    break;
            }
            LoadTreeView(testClassesList);
        }
    }

}
