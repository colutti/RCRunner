using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using RCRunner.Properties;
using RCRunner.Shared.Lib;
using RCRunner.Shared.Lib.PluginsStruct;

namespace RCRunner
{
    public partial class FrmMain : Form
    {
        private readonly RCRunnerAPI _rcRunner;
        private delegate void SetTextCallback(TestScript testcaseScript);
        private delegate void SetEnabledCallback(bool enabled);

        private readonly PluginLoader _pluginLoader;

        private readonly Color _testActive = Color.FloralWhite;
        private readonly Color _testFailed = Color.Red;
        private readonly Color _testPassed = Color.GreenYellow;
        private readonly Color _testRunning = Color.CornflowerBlue;
        private readonly Color _testWaiting = Color.DarkOrange;
        private TestFrameworkRunner _testFrameworkRunner;

        public FrmMain()
        {
            InitializeComponent();

            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            // ReSharper disable once DoNotCallOverridableMethodsInConstructor
            Text = String.Format("RC Test Script Runner version {0}", version);

            _pluginLoader = new PluginLoader();
            LoadTestRunners();
            _pluginLoader.LoadTestExecutionPlugins();

            _rcRunner = new RCRunnerAPI();
            _rcRunner.MethodStatusChanged += OnMethodStatusChanged;

            lblFailedScripts.ForeColor = _testFailed;
            lblPassedScripts.ForeColor = _testPassed;
            lblRunningScripts.ForeColor = _testRunning;
            lblWatingScripts.ForeColor = _testWaiting;

            cmbxFilter.Items.Clear();
            cmbxFilter.Items.Add("Everything");
            cmbxFilter.Items.Add(TestExecutionStatus.Active);
            cmbxFilter.Items.Add(TestExecutionStatus.Failed);
            cmbxFilter.Items.Add(TestExecutionStatus.Passed);
            cmbxFilter.Items.Add(TestExecutionStatus.Running);
            cmbxFilter.Items.Add(TestExecutionStatus.Waiting);
            cmbxFilter.SelectedIndex = 0;

            ResetTestExecution();

            DisableOrEnableControls(true);
        }

        private void LoadTestRunners()
        {
            _pluginLoader.LoadTestRunnersPlugins();

            foreach (var testFrameworkRunner in _pluginLoader.TestRunnersPluginList)
            {
                cmbTestRunners.Items.Add(testFrameworkRunner.GetDisplayName());
            }

            if (cmbTestRunners.Items.Count <= 0) return;

            cmbTestRunners.SelectedIndex = 0;
            _testFrameworkRunner = _pluginLoader.TestRunnersPluginList[0];

            lblExportExcel.Visible = _testFrameworkRunner.CanExportResultsToExcel();
        }

        private void ResetTestExecution()
        {
            prgrsbrTestProgress.Maximum = 0;
            lblPassedScripts.Text = @"Passed scripts: 0";
            lblFailedScripts.Text = @"Failed scripts: 0";
            lblRunningScripts.Text = @"Running scripts: 0";
            lblWatingScripts.Text = @"Waiting scripts: 0";
            lblTotalScripts.Text = @"Total: 0";
        }

        private void PaintTreeNodeBasedOnTestStatus(TestScript testcaseScript, ListViewItem item)
        {
            switch (testcaseScript.TestExecutionStatus)
            {
                case TestExecutionStatus.Active:
                    item.ForeColor = _testActive;
                    break;
                case TestExecutionStatus.Failed:
                    item.ForeColor = _testFailed;
                    break;
                case TestExecutionStatus.Passed:
                    item.ForeColor = _testPassed;
                    break;
                case TestExecutionStatus.Running:
                    item.ForeColor = _testRunning;
                    break;
                case TestExecutionStatus.Waiting:
                    item.ForeColor = _testWaiting;
                    break;
            }
            item.SubItems[2].Text = testcaseScript.TestExecutionStatus.ToString();
            item.SubItems[4].Text = testcaseScript.LastExecutionErrorMsg;
        }

        private void OnMethodStatusChanged(TestScript testcasemethod)
        {
            UpdateTreeview(testcasemethod);

            var status = testcasemethod.TestExecutionStatus;

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

            UpdateLblTestScripts(testcasemethod);

            if (_rcRunner.Done()) DisableOrEnableControls(true);
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
            if (listViewTestScripts.InvokeRequired)
            {
                var d = new SetTextCallback(UpdateTreeview);
                Invoke(d, new object[] { testcaseScript });
            }
            else
            {
                var item = listViewTestScripts.FindItemWithText(testcaseScript.Name, true, 0);
                if (item == null) return;
                PaintTreeNodeBasedOnTestStatus(testcaseScript, item);
            }
        }

        private static string CreateTestResultsFolder()
        {
            var folderName = DateTime.Now.ToString(CultureInfo.InvariantCulture).Replace("/", "").Replace(":", "");
            var resultFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestResults", folderName);
            Directory.CreateDirectory(resultFilePath);
            return resultFilePath;
        }

        private void LoadTestScriptListToView(IEnumerable<TestScript> testClassesList)
        {
            listViewTestScripts.ListViewItemSorter = null;
            listViewTestScripts.BeginUpdate();
            listViewTestScripts.Items.Clear();
            try
            {
                foreach (var testMethod in testClassesList)
                {
                    var item = listViewTestScripts.Items.Add(testMethod.ClassName);
                    item.SubItems.Add(testMethod.Name);
                    item.SubItems.Add(testMethod.TestExecutionStatus.ToString());
                    item.SubItems.Add(testMethod.TestDescription);
                    item.SubItems.Add(testMethod.LastExecutionErrorMsg);
                    item.Tag = testMethod;
                    PaintTreeNodeBasedOnTestStatus(testMethod, item );
                }
            }
            finally
            {
                listViewTestScripts.ListViewItemSorter = new ListViewItemComparer(0); 
                listViewTestScripts.EndUpdate();
            }
        }

        private void LoadClassList(IEnumerable<TestScript> testClassesList)
        {
            foreach (var testScript in testClassesList.Where(testScript => !cmbxClasses.Items.Contains(testScript.ClassName)))
            {
               cmbxClasses.Items.Add(testScript.ClassName, true);
            }
            cmbxClasses.Sorted = true;
        }

        private void lblLoadAssembly_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (_pluginLoader.TestRunnersPluginList.Count <= 0)
            {
                MessageBox.Show(string.Format(@"No test framework plugins found. Make sure you have a test framework plugin at {0} before opening the runner", 
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Plugins\TestRunners")), @"Missing plugin", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
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

                _rcRunner.SetPluginLoader(_pluginLoader);

                _rcRunner.LoadAssembly();

                cmbxAttributes.Items.Clear();

                foreach (var attributes in _rcRunner.CustomAttributesList)
                {
                    cmbxAttributes.Items.Add(attributes);

                }

                for (var i = 0; i < cmbxAttributes.Items.Count; i++) cmbxAttributes.SetItemChecked(i, true);

                LoadTestScriptListToView(_rcRunner.TestClassesList);

                LoadClassList(_rcRunner.TestClassesList);

                lblTotalScripts.Text = @"Total: " + _rcRunner.TestClassesList.Count;
            }
            finally
            {
                DisableOrEnableControls(true);
            }
        }

        private void lblExecuteTestScripts_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            ResetTestExecution();

            _rcRunner.SetTestRunner(_testFrameworkRunner);

            _rcRunner.SetPluginLoader(_pluginLoader);

            DisableOrEnableControls(false);

            prgrsbrTestProgress.Maximum = listViewTestScripts.CheckedItems.Count;

            var testCasesList = new List<TestScript>();

            testCasesList.AddRange(from ListViewItem item in listViewTestScripts.CheckedItems select item.Tag as TestScript);

            if (testCasesList.Any())
            {
                var testResultsFolder = CreateTestResultsFolder();
                _testFrameworkRunner.SetTestResultsFolder(testResultsFolder);
                _rcRunner.RunTestCases(testCasesList, Settings.Default.MaxThreads);
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
                cmbxAttributes.Enabled = enable;
                cmbxFilter.Enabled = enable;
                lblExportExcel.Enabled = enable;
            }
        }

        private void ApplyFilter()
        {
            TestExecutionStatus testExecutionStatus;

            if (!Enum.TryParse(cmbxFilter.Text, out testExecutionStatus)) testExecutionStatus = TestExecutionStatus.Active;

            IEnumerable<TestScript> testScriptsListAttributes;

            if (cmbxAttributes.CheckedItems.Count == 0 || cmbxAttributes.CheckedItems.Count == cmbxAttributes.Items.Count)
            {
                testScriptsListAttributes = _rcRunner.TestClassesList;
            }
            else
            {
                var selectedAttributes = cmbxAttributes.CheckedItems.Cast<string>().ToList();

                testScriptsListAttributes = from t in _rcRunner.TestClassesList
                                            where t.CustomAtributteList.Join(selectedAttributes, x => x, g => g, (a, b) => 1).Any()
                                            select t;
            }

            var classFilter = testScriptsListAttributes;

            if (cmbxClasses.CheckedItems.Count > 0 && cmbxClasses.CheckedItems.Count != cmbxClasses.Items.Count)
            {
                var selectedClasses = cmbxClasses.CheckedItems.Cast<string>().ToList();

                classFilter = classFilter.Where(x => selectedClasses.Contains(x.ClassName));
            }

            var finalFilter = cmbxFilter.SelectedIndex == 0 ? classFilter : classFilter.Where(x => x.TestExecutionStatus == testExecutionStatus).ToList();

            var testClassesList = finalFilter as IList<TestScript> ?? finalFilter.ToList();

            lblTotalScripts.Text = @"Total: " + testClassesList.Count();

            LoadTestScriptListToView(testClassesList);
        }

        private void resultsListView_KeyUp(object sender, KeyEventArgs e)
        {
            if (sender != listViewTestScripts) return;

            if (e.Control && e.KeyCode == Keys.C)
                CopySelectedValuesToClipboard();
        }

        private void CopySelectedValuesToClipboard()
        {
            var builder = new StringBuilder();
            foreach (ListViewItem item in listViewTestScripts.SelectedItems)
                builder.AppendLine(item.SubItems[1].Text);

            Clipboard.SetText(builder.ToString());
        }

        private void lblExportExcel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (!_testFrameworkRunner.CanExportResultsToExcel()) return;

            var folderDialog = new FolderBrowserDialog
            {
                Description = @"Choose the folder that contains the test results"
            };

            var result = folderDialog.ShowDialog();

            if (result != DialogResult.OK) return;

            var folder = folderDialog.SelectedPath;

            var excelpath = Path.Combine(folder, "result.xlsx");

            _testFrameworkRunner.ExportResultsToExcel(folder, excelpath);

            MessageBox.Show(@"Results exported to " + excelpath);
        }

        private void cmbTestRunners_SelectionChangeCommitted(object sender, EventArgs e)
        {
            _testFrameworkRunner = _pluginLoader.TestRunnersPluginList[cmbTestRunners.SelectedIndex];
            lblExportExcel.Visible = _testFrameworkRunner.CanExportResultsToExcel();
        }

        private void lblApplyFilter_Click(object sender, EventArgs e)
        {
            ApplyFilter();
        }

        private void lblCheckAll_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            foreach (ListViewItem item in listViewTestScripts.Items)
            {
                item.Checked = true;
            }
        }

        private void lblUncheckAll_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            foreach (ListViewItem item in listViewTestScripts.Items)
            {
                item.Checked = false;
            }
        }

        private void listViewTestScripts_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            listViewTestScripts.ListViewItemSorter = new ListViewItemComparer(e.Column);
        }

    }

    class ListViewItemComparer : IComparer
    {
        private readonly int _col;

        public ListViewItemComparer(int column)
        {
            _col = column;
        }
        public int Compare(object x, object y)
        {
            return String.CompareOrdinal(((ListViewItem)x).SubItems[_col].Text, ((ListViewItem)y).SubItems[_col].Text);
        }
    }


}
