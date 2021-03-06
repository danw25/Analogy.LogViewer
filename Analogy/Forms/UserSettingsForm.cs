﻿using Analogy.Interfaces;
using Analogy.Managers;
using Analogy.Properties;
using Analogy.Types;
using DevExpress.Utils;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Views.Grid.ViewInfo;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace Analogy
{

    public partial class UserSettingsForm : XtraForm
    {
        private struct FactoryCheckItem
        {
            public string Name;
            public Guid ID;

            public FactoryCheckItem(string name, Guid id)
            {
                Name = name;
                ID = id;
            }

            public override string ToString() => $"{Name} ({ID})";
        }

        private DataTable messageData;
        private UserSettingsManager Settings { get; } = UserSettingsManager.UserSettings;
        private int InitialSelection = -1;

        public UserSettingsForm()
        {
            InitializeComponent();
            SetupEventsHandlers();


        }

        private void SetupExampleMessage(string text)
        {
            DataRow dtr = messageData.NewRow();
            dtr.BeginEdit();
            dtr["Date"] = DateTime.Now;
            dtr["Text"] = text;
            dtr["Source"] = "Analogy";
            dtr["Level"] = AnalogyLogLevel.Event.ToString();
            dtr["Class"] = AnalogyLogClass.General.ToString();
            dtr["Category"] = "None";
            dtr["User"] = "None";
            dtr["Module"] = "Analogy";
            dtr["ProcessID"] = Process.GetCurrentProcess().Id;
            dtr["ThreadID"] = Thread.CurrentThread.ManagedThreadId;
            dtr["DataProvider"] = string.Empty;
            dtr["MachineName"] = "None";
            dtr.EndEdit();
            messageData.Rows.Add(dtr);
            messageData.AcceptChanges();
        }

        public UserSettingsForm(int tabIndex) : this()
        {
            InitialSelection = tabIndex;
        }

        private void UserSettingsForm_Load(object sender, EventArgs e)
        {
            ShowIcon = true;
            logGrid.MouseDown += logGrid_MouseDown;
            Icon = UserSettingsManager.UserSettings.GetIcon();
            LoadSettings();
            if (InitialSelection >= 0)
                tabControlMain.SelectedTabPageIndex = InitialSelection;
            if (File.Exists(Settings.LogGridFileName))
            {
                gridControl.MainView.RestoreLayoutFromXml(Settings.LogGridFileName);
            }
            messageData = Utils.DataTableConstructor();
            gridControl.DataSource = messageData.DefaultView;
            SetupExampleMessage("Test 1");
            SetupExampleMessage("Test 2");

        }
        void logGrid_MouseDown(object sender, MouseEventArgs e)
        {
            GridHitInfo info = logGrid.CalcHitInfo(e.Location);
            if (info.InColumnPanel)
            {
                teHeader.Tag = info.Column;
                teHeader.Text = info.Column.Caption;
            }
        }
        private void SetupEventsHandlers()
        {
            tsAutoComplete.IsOnChanged += (s, e) => { Settings.RememberLastSearches = tsAutoComplete.IsOn; };
            nudAutoCompleteCount.ValueChanged += (s, e) =>
            {
                Settings.NumberOfLastSearches = (int)nudAutoCompleteCount.Value;
            };
        }
        private void LoadSettings()
        {
            tsRememberLastPositionAndState.IsOn = Settings.AnalogyPosition.RememberLastPosition;
            logGrid.Columns["Date"].DisplayFormat.FormatType = FormatType.DateTime;
            logGrid.Columns["Date"].DisplayFormat.FormatString = Settings.DateTimePattern;
            tsHistory.IsOn = Settings.ShowHistoryOfClearedMessages;
            teDateTimeFormat.Text = Settings.DateTimePattern;
            tsFilteringExclude.IsOn = Settings.SaveSearchFilters;
            listBoxFoldersProbing.Items.AddRange(Settings.AdditionalProbingLocations.ToArray());
            tsAutoComplete.IsOn = Settings.RememberLastSearches;
            nudRecentFiles.Value = Settings.RecentFilesCount;
            nudRecentFolders.Value = Settings.RecentFoldersCount;
            tsUserStatistics.IsOn = Settings.EnableUserStatistics;
            //tsSimpleMode.IsOn = Settings.SimpleMode;
            tsFileCaching.IsOn = Settings.EnableFileCaching;
            tswitchExtensionsStartup.IsOn = Settings.LoadExtensionsOnStartup;
            tsStartupRibbonMinimized.IsOn = Settings.StartupRibbonMinimized;
            tsErrorLevelAsDefault.IsOn = Settings.StartupErrorLogLevel;
            chkEditPaging.Checked = Settings.PagingEnabled;
            if (Settings.PagingEnabled)
            {
                nudPageLength.Value = Settings.PagingSize;
            }
            else
            {
                nudPageLength.Enabled = false;
            }

            checkEditSearchAlsoInSourceAndModule.Checked = Settings.SearchAlsoInSourceAndModule;
            toggleSwitchIdleMode.IsOn = Settings.IdleMode;
            nudIdleTime.Value = Settings.IdleTimeMinutes;
            tsDataTimeAscendDescend.IsOn = Settings.DefaultDescendOrder;
            var manager = ExtensionsManager.Instance;
            var extensions = manager.GetExtensions().ToList();
            foreach (var extension in extensions)
            {

                chklItems.Items.Add(extension, Settings.StartupExtensions.Contains(extension.ID));
                chklItems.DisplayMember = "DisplayName";

            }

            var startup = Settings.AutoStartDataProviders;
            var loaded = FactoriesManager.Instance.GetRealTimeDataSourcesNamesAndIds();
            foreach (var realTime in loaded)
            {
                FactoryCheckItem itm = new FactoryCheckItem(realTime.Name, realTime.ID);
                chkLstItemRealTimeDataSources.Items.Add(itm, startup.Contains(itm.ID));
            }



            foreach (var setting in Settings.FactoriesOrder)
            {
                FactorySettings factory = Settings.GetFactorySetting(setting);
                if (factory == null) continue;
                FactoryCheckItem itm = new FactoryCheckItem(factory.FactoryName, factory.FactoryId);
                chkLstDataProviderStatus.Items.Add(itm, factory.Status == DataProviderFactoryStatus.Enabled);
            }
            //add missing:
            foreach (var factory in Settings.FactoriesSettings.Where(itm => !Settings.FactoriesOrder.Contains(itm.FactoryId)))
            {

                FactoryCheckItem itm = new FactoryCheckItem(factory.FactoryName, factory.FactoryId);
                chkLstDataProviderStatus.Items.Add(itm, factory.Status != DataProviderFactoryStatus.Disabled);
            }

            //file associations:
            cbDataProviderAssociation.DataSource = Settings.FactoriesSettings;
            cbDataProviderAssociation.DisplayMember = "FactoryName";
            tsRememberLastOpenedDataProvider.IsOn = Settings.RememberLastOpenedDataProvider;
            lboxHighlightItems.DataSource = Settings.PreDefinedQueries.Highlights;
            lboxAlerts.DataSource = Settings.PreDefinedQueries.Alerts;
            lboxFilters.DataSource = Settings.PreDefinedQueries.Filters;
            nudAutoCompleteCount.Value = Settings.NumberOfLastSearches;
            tsSingleInstance.IsOn = Settings.SingleInstance;
            if (Settings.AnalogyIcon == "Light")
            {
                rbtnLightIconColor.Checked = true;
            }
            else
            {
                rbtnDarkIconColor.Checked = true;
            }
            LoadColorSettings();
            cbUpdates.Properties.Items.AddRange(typeof(UpdateMode).GetDisplayValues().Values);
            cbUpdates.SelectedItem = UpdateManager.Instance.UpdateMode.GetDisplay();
            tsTraybar.IsOn = Settings.MinimizedToTrayBar;
            tsCheckAdditionalInformation.IsOn = Settings.CheckAdditionalInformation;
        }

        private void SaveSetting()
        {
            Settings.ColorSettings.SetColorForLogLevel(AnalogyLogLevel.Unknown, cpeLogLevelUnknown.Color);
            Settings.ColorSettings.SetColorForLogLevel(AnalogyLogLevel.Disabled, cpeLogLevelDisabled.Color);
            Settings.ColorSettings.SetColorForLogLevel(AnalogyLogLevel.Trace, cpeLogLevelTrace.Color);
            Settings.ColorSettings.SetColorForLogLevel(AnalogyLogLevel.Verbose, cpeLogLevelVerbose.Color);
            Settings.ColorSettings.SetColorForLogLevel(AnalogyLogLevel.Debug, cpeLogLevelDebug.Color);
            Settings.ColorSettings.SetColorForLogLevel(AnalogyLogLevel.Event, cpeLogLevelEvent.Color);
            Settings.ColorSettings.SetColorForLogLevel(AnalogyLogLevel.Warning, cpeLogLevelWarning.Color);
            Settings.ColorSettings.SetColorForLogLevel(AnalogyLogLevel.Error, cpeLogLevelError.Color);
            Settings.ColorSettings.SetColorForLogLevel(AnalogyLogLevel.Critical, cpeLogLevelCritical.Color);
            Settings.ColorSettings.SetColorForLogLevel(AnalogyLogLevel.AnalogyInformation,
                cpeLogLevelAnalogyInformation.Color);
            Settings.ColorSettings.SetHighlightColor(cpeHighlightColor.Color);
            Settings.ColorSettings.SetNewMessagesColor(cpeNewMessagesColor.Color);
            Settings.ColorSettings.EnableNewMessagesColor = ceNewMessagesColor.Checked;
            Settings.ColorSettings.OverrideLogLevelColor = ceOverrideLogLevelColor.Checked;
            Settings.RecentFilesCount = (int) nudRecentFiles.Value;
            Settings.RecentFoldersCount = (int) nudRecentFolders.Value;
            List<Guid> order = (from FactoryCheckItem itm in chkLstDataProviderStatus.Items select (itm.ID)).ToList();
            var checkedItem = chkLstDataProviderStatus.CheckedItems.Cast<FactoryCheckItem>().ToList();
            foreach (Guid guid in order)
            {
                var factory = Settings.FactoriesSettings.SingleOrDefault(f => f.FactoryId == guid);
                if (factory != null)
                {
                    factory.Status = checkedItem.Exists(f => f.ID == guid)
                        ? DataProviderFactoryStatus.Enabled
                        : DataProviderFactoryStatus.Disabled;
                }
            }

            Settings.RememberLastOpenedDataProvider = tsRememberLastOpenedDataProvider.IsOn;
            Settings.RememberLastSearches = tsAutoComplete.IsOn;
            Settings.UpdateOrder(order);
            Settings.AdditionalProbingLocations = listBoxFoldersProbing.Items.Cast<string>().ToList();
            Settings.SingleInstance = tsSingleInstance.IsOn;
            Settings.AnalogyIcon = rbtnLightIconColor.Checked ? "Light" : "Dark";
            var options = typeof(UpdateMode).GetDisplayValues();
            UpdateManager.Instance.UpdateMode = (UpdateMode) Enum.Parse(typeof(UpdateMode),
                options.Single(k => k.Value == cbUpdates.SelectedItem.ToString()).Key, true);
            Settings.MinimizedToTrayBar = tsTraybar.IsOn;
            Settings.CheckAdditionalInformation = tsCheckAdditionalInformation.IsOn;
            Settings.AnalogyPosition.RememberLastPosition = tsRememberLastPositionAndState.IsOn;
            Settings.Save();
        }

        private void LoadColorSettings()
        {
            cpeLogLevelUnknown.Color = Settings.ColorSettings.GetColorForLogLevel(AnalogyLogLevel.Unknown);
            cpeLogLevelDisabled.Color = Settings.ColorSettings.GetColorForLogLevel(AnalogyLogLevel.Disabled);
            cpeLogLevelTrace.Color = Settings.ColorSettings.GetColorForLogLevel(AnalogyLogLevel.Trace);
            cpeLogLevelVerbose.Color = Settings.ColorSettings.GetColorForLogLevel(AnalogyLogLevel.Verbose);
            cpeLogLevelDebug.Color = Settings.ColorSettings.GetColorForLogLevel(AnalogyLogLevel.Debug);
            cpeLogLevelEvent.Color = Settings.ColorSettings.GetColorForLogLevel(AnalogyLogLevel.Event);
            cpeLogLevelWarning.Color = Settings.ColorSettings.GetColorForLogLevel(AnalogyLogLevel.Warning);
            cpeLogLevelError.Color = Settings.ColorSettings.GetColorForLogLevel(AnalogyLogLevel.Error);
            cpeLogLevelCritical.Color = Settings.ColorSettings.GetColorForLogLevel(AnalogyLogLevel.Critical);
            cpeLogLevelAnalogyInformation.Color = Settings.ColorSettings.GetColorForLogLevel(AnalogyLogLevel.AnalogyInformation);
            cpeHighlightColor.Color = Settings.ColorSettings.GetHighlightColor();
            cpeNewMessagesColor.Color = Settings.ColorSettings.GetNewMessagesColor();
            ceNewMessagesColor.Checked = Settings.ColorSettings.EnableNewMessagesColor;
            ceOverrideLogLevelColor.Checked = Settings.ColorSettings.OverrideLogLevelColor;
        }


        private void tsFilteringExclude_Toggled(object sender, EventArgs e)
        {
            Settings.SaveSearchFilters = tsFilteringExclude.IsOn;

        }

        private void tsHistory_Toggled(object sender, EventArgs e)
        {
            Settings.ShowHistoryOfClearedMessages = tsHistory.IsOn;
        }


        private void tsUserStatistics_Toggled(object sender, EventArgs e)
        {
            EnableDisableUserStatistics(tsUserStatistics.IsOn);
            Settings.EnableUserStatistics = tsUserStatistics.IsOn;

        }

        private void EnableDisableUserStatistics(bool isOn)
        {
            if (isOn)
            {
                lblLaunchCount.Text = $"Number of Analogy Launches: {Settings.AnalogyLaunches}";
                lblRunningTime.Text = $"Running Time: {Settings.DisplayRunningTime}";
                lblOpenedFiles.Text = $"Number Of Opened Files: {Settings.AnalogyOpenedFiles}";
            }
            else
            {
                lblLaunchCount.Text = $"Number of Analogy Launches: 0";
                lblRunningTime.Text = $"Running Time: 0";
                lblOpenedFiles.Text = $"Number Of Opened Files: N/A";
            }
        }

        private void btnClearStatistics_Click(object sender, EventArgs e)
        {
            XtraMessageBox.AllowCustomLookAndFeel = true;
            var result = XtraMessageBox.Show("Clear statistics?", "Confirmation Required", MessageBoxButtons.YesNo);
            if (result == DialogResult.Yes)
            {
                Settings.ClearStatistics();
            }

        }

        private void tsSimpleMode_Toggled(object sender, EventArgs e)
        {
            //Settings.SimpleMode = tsSimpleMode.IsOn;
        }

        private void tsFileCaching_Toggled(object sender, EventArgs e)
        {
            Settings.EnableFileCaching = tsFileCaching.IsOn;
        }

        private void tswitchExtensionsStartup_Toggled(object sender, EventArgs e)
        {
            Settings.LoadExtensionsOnStartup = tswitchExtensionsStartup.IsOn;
            chklItems.Enabled = tswitchExtensionsStartup.IsOn;
        }

        private void chklItems_SelectedIndexChanged(object sender, EventArgs e)
        {
            Settings.StartupExtensions =
                chklItems.CheckedItems.Cast<IAnalogyExtension>().Select(ex => ex.ID).ToList();


        }

        private void tsStartupRibbonMinimized_Toggled(object sender, EventArgs e)
        {
            Settings.StartupRibbonMinimized = tsStartupRibbonMinimized.IsOn;
        }

        private void tsErrorLevelAsDefault_Toggled(object sender, EventArgs e)
        {
            Settings.StartupErrorLogLevel = tsErrorLevelAsDefault.IsOn;
        }

        private void chkEditPaging_CheckedChanged(object sender, EventArgs e)
        {
            Settings.PagingEnabled = chkEditPaging.Checked;
            nudPageLength.Enabled = Settings.PagingEnabled;
        }

        private void nudPageLength_ValueChanged(object sender, EventArgs e)
        {
            Settings.PagingSize = (int)nudPageLength.Value;
        }

        private void checkEditSearchAlsoInSourceAndModule_CheckedChanged(object sender, EventArgs e)
        {
            Settings.SearchAlsoInSourceAndModule = checkEditSearchAlsoInSourceAndModule.Checked;
        }

        private void ToggleSwitchIdleMode_Toggled(object sender, EventArgs e)
        {
            Settings.IdleMode = toggleSwitchIdleMode.IsOn;

        }

        private void NudIdleTime_ValueChanged(object sender, EventArgs e)
        {
            Settings.IdleTimeMinutes = (int)nudIdleTime.Value;

        }

        private void ChkLstItemRealTimeDataSources_SelectedIndexChanged(object sender, EventArgs e)
        {
            Settings.AutoStartDataProviders =
                chkLstItemRealTimeDataSources.CheckedItems.Cast<FactoryCheckItem>().Select(r => r.ID).ToList();
        }

        private void UserSettingsForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveSetting();
        }



        private void tsDataTimeAscendDescend_Toggled(object sender, EventArgs e)
        {
            Settings.DefaultDescendOrder = tsDataTimeAscendDescend.IsOn;

        }

        private void sBtnExportColors_Click(object sender, EventArgs e)
        {
            SaveSetting();
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Analogy Color Settings (*.json)|*.json";
            saveFileDialog.Title = @"Export Analogy Color settings";

            if (saveFileDialog.ShowDialog(this) == DialogResult.OK)
            {

                try
                {
                    File.WriteAllText(saveFileDialog.FileName, Settings.ColorSettings.AsJson());
                    XtraMessageBox.Show("File Saved", @"Export settings", MessageBoxButtons.OK,
                        MessageBoxIcon.Information);

                }
                catch (Exception ex)
                {
                    AnalogyLogManager.Instance.LogError("Error during save to file: " + e, nameof(sBtnExportColors_Click));
                    XtraMessageBox.Show("Error Export: " + ex.Message, @"Error Saving file", MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }

            }
        }

        private void sBtnImportColors_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Filter = "Analogy Color Settings (*.json)|*.json";
            openFileDialog1.Title = @"Import NLog settings";
            openFileDialog1.Multiselect = true;
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    var json = File.ReadAllText(openFileDialog1.FileName);
                    ColorSettings color = ColorSettings.FromJson(json);
                    Settings.ColorSettings = color;
                    LoadColorSettings();
                    XtraMessageBox.Show("File Imported. Save settings if desired", @"Import settings",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);

                }
                catch (Exception ex)
                {
                    AnalogyLogManager.Instance.LogError("Error during import data: " + e, nameof(sBtnImportColors_Click));
                    XtraMessageBox.Show("Error Import: " + ex.Message, @"Error Import file", MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }

        private void btnDataProviderCustomSettings_Click(object sender, EventArgs e)
        {
            UserSettingsDataProvidersForm user = new UserSettingsDataProvidersForm();
            user.ShowDialog(this);
        }

        private void sBtnMoveUp_Click(object sender, EventArgs e)
        {
            if (chkLstDataProviderStatus.SelectedIndex <= 0) return;
            var selectedIndex = chkLstDataProviderStatus.SelectedIndex;
            var currentValue = chkLstDataProviderStatus.Items[selectedIndex];
            chkLstDataProviderStatus.Items[selectedIndex] = chkLstDataProviderStatus.Items[selectedIndex - 1];
            chkLstDataProviderStatus.Items[selectedIndex - 1] = currentValue;
            chkLstDataProviderStatus.SelectedIndex = chkLstDataProviderStatus.SelectedIndex - 1;
        }

        private void sBtnMoveDown_Click(object sender, EventArgs e)
        {
            if (chkLstDataProviderStatus.SelectedIndex == chkLstDataProviderStatus.Items.Count - 1) return;
            var selectedIndex = chkLstDataProviderStatus.SelectedIndex;
            var currentValue = chkLstDataProviderStatus.Items[selectedIndex + 1];
            chkLstDataProviderStatus.Items[selectedIndex + 1] = chkLstDataProviderStatus.Items[selectedIndex];
            chkLstDataProviderStatus.Items[selectedIndex] = currentValue;
            chkLstDataProviderStatus.SelectedIndex = chkLstDataProviderStatus.SelectedIndex + 1;
        }

        private void cbDataProviderAssociation_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbDataProviderAssociation.SelectedItem is FactorySettings setting && setting.UserSettingFileAssociations.Any())
                txtbDataProviderAssociation.Text = string.Join(",", setting.UserSettingFileAssociations);
            else
                txtbDataProviderAssociation.Text = string.Empty;

        }

        private void btnSetFileAssociation_Click(object sender, EventArgs e)
        {
            if (cbDataProviderAssociation.SelectedItem is FactorySettings setting)
                setting.UserSettingFileAssociations = txtbDataProviderAssociation.Text
                    .Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        private void rbtnHighlightContains_CheckedChanged(object sender, EventArgs e)
        {
            teHighlightContains.Enabled = rbtnHighlightContains.Checked;
            teHighlightEquals.Enabled = rbtnHighlightEquals.Checked;
        }

        private void rbtnHighlightEquals_CheckedChanged(object sender, EventArgs e)
        {
            teHighlightContains.Enabled = rbtnHighlightContains.Checked;
            teHighlightEquals.Enabled = rbtnHighlightEquals.Checked;
        }

        private void sbtnAddHighlight_Click(object sender, EventArgs e)
        {
            if (rbtnHighlightContains.Checked)
            {
                Settings.PreDefinedQueries.AddHighlight(teHighlightContains.Text, PreDefinedQueryType.Contains, cpeHighlightPreDefined.Color);
                lboxHighlightItems.DataSource = Settings.PreDefinedQueries.Highlights;
                lboxHighlightItems.Refresh();
            }

            if (rbtnHighlightEquals.Checked)
            {
                Settings.PreDefinedQueries.AddHighlight(teHighlightEquals.Text, PreDefinedQueryType.Equals, cpeHighlightPreDefined.Color);
                lboxHighlightItems.DataSource = Settings.PreDefinedQueries.Highlights;
                lboxHighlightItems.Refresh();
            }
        }

        private void sbtnDeleteHighlight_Click(object sender, EventArgs e)
        {
            if (lboxHighlightItems.SelectedItem is PreDefineHighlight highlight)
            {
                Settings.PreDefinedQueries.RemoveHighlight(highlight);
                lboxHighlightItems.DataSource = Settings.PreDefinedQueries.Highlights;
                lboxHighlightItems.Refresh();
            }
        }

        private void sbtnAddFilter_Click(object sender, EventArgs e)
        {
            Settings.PreDefinedQueries.AddFilter(txtbIncludeTextFilter.Text, txtbExcludeFilter.Text, txtbSourcesFilter.Text, txtbModulesFilter.Text);
            lboxFilters.DataSource = Settings.PreDefinedQueries.Filters;
            lboxFilters.Refresh();
        }

        private void sbtnDeleteFilter_Click(object sender, EventArgs e)
        {
            if (lboxFilters.SelectedItem is PreDefineFilter filter)
            {
                Settings.PreDefinedQueries.RemoveFilter(filter);
                lboxFilters.DataSource = Settings.PreDefinedQueries.Filters;
                lboxFilters.Refresh();
            }
        }

        private void sbtnAddAlerts_Click(object sender, EventArgs e)
        {
            Settings.PreDefinedQueries.AddAlert(txtbIncludeTextAlert.Text, txtbExcludeAlert.Text, txtbSourcesAlert.Text, txtbModulesAlert.Text);
            lboxAlerts.DataSource = Settings.PreDefinedQueries.Alerts;
            lboxAlerts.Refresh();
        }

        private void sbtnDeleteAlerts_Click(object sender, EventArgs e)
        {
            if (lboxAlerts.SelectedItem is PreDefineAlert alert)
            {
                Settings.PreDefinedQueries.RemoveAlert(alert);
                lboxAlerts.DataSource = Settings.PreDefinedQueries.Alerts;
                lboxAlerts.Refresh();
            }
        }

        private void sbtnFolderProbingBrowse_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog folderDlg = new FolderBrowserDialog
            {
                ShowNewFolderButton = false
            })
            {
                // Show the FolderBrowserDialog.  
                DialogResult result = folderDlg.ShowDialog();
                if (result == DialogResult.OK)
                {
                    teFoldersProbing.Text = folderDlg.SelectedPath;
                }
            }
        }

        private void sbtnFolderProbingAdd_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(teFoldersProbing.Text)) return;
            listBoxFoldersProbing.Items.Add(teFoldersProbing.Text);
        }

        private void sbtnDeleteFolderProbing_Click(object sender, EventArgs e)
        {
            if (listBoxFoldersProbing.SelectedItem != null)
                listBoxFoldersProbing.Items.Remove(listBoxFoldersProbing.SelectedItem);
        }

        private void rbtnDarkIconColor_CheckedChanged(object sender, EventArgs e)
        {
            if (rbtnDarkIconColor.Checked)
            {
                peAnalogy.Image = Resources.AnalogyDark;
            }
        }

        private void rbtnLightIconColor_CheckedChanged(object sender, EventArgs e)
        {
            if (rbtnLightIconColor.Checked)
            {
                peAnalogy.Image = Resources.AnalogyLight;
            }
        }

        private void sbtnHeaderSet_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(teHeader.Text) && teHeader.Tag is DevExpress.XtraGrid.Columns.GridColumn column)
            {
                column.Caption = teHeader.Text;
                SaveGridLayout();
            }
        }
        private void SaveGridLayout()
        {
            try
            {
                gridControl.MainView.SaveLayoutToXml(Settings.LogGridFileName);
            }
            catch (Exception e)
            {
                AnalogyLogger.Instance.LogException(e, "Analogy", $"Error saving setting: {e.Message}");
                XtraMessageBox.Show(e.Message, $"Error Saving layout file: {e.Message}", MessageBoxButtons.OK, MessageBoxIcon.Error);

            }
        }

        private void sbtnDateTimeFormat_Click(object sender, EventArgs e)
        {

            logGrid.Columns["Date"].DisplayFormat.FormatType = FormatType.DateTime;
            logGrid.Columns["Date"].DisplayFormat.FormatString = teDateTimeFormat.Text;
            Settings.DateTimePattern = teDateTimeFormat.Text;
        }

        private void ceNewMessagesColor_CheckedChanged(object sender, EventArgs e)
        {
            cpeNewMessagesColor.Enabled = ceNewMessagesColor.Checked;

        }
    }
}

