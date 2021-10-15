﻿using Luban.Job.Cfg.Datas;
using Luban.Job.Cfg.DataSources.Excel;
using Luban.Job.Cfg.Defs;
using Luban.Job.Cfg.Utils;
using Luban.Job.Common.Defs;
using Luban.Server.Common;
using Microsoft.Office.Interop.Excel;
using Microsoft.Office.Tools.Ribbon;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LubanAssistant
{
    public partial class AssistantTab
    {
        private string RootDefineFile
        {
            get => Properties.Settings.Default.rootDefineFile;
            set
            {
                Properties.Settings.Default.rootDefineFile = value;
                Properties.Settings.Default.Save();
            }
        }

        public string InputDataDir
        {
            get => Properties.Settings.Default.dataDir;
            set
            {
                Properties.Settings.Default.dataDir = value;
                Properties.Settings.Default.Save();
            }
        }

        public TableDataInfo LastLoadTableData { get; private set; }

        private void AssistantTab_Load(object sender, RibbonUIEventArgs e)
        {
        }

        private bool HasSetRootDefineFile()
        {
            return !string.IsNullOrWhiteSpace(RootDefineFile) && File.Exists(RootDefineFile);
        }

        private bool TryChooseRootDefineFile(out string rootDefineFile)
        {
            var dialog = new OpenFileDialog();
            dialog.DefaultExt = "xml";
            dialog.Filter = "root file (*.xml)|*.xml";
            dialog.Title = "Select Root Xml File";
            dialog.CheckFileExists = true;
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                rootDefineFile = dialog.FileName;
                return true;
            }
            rootDefineFile = null;
            return false;
        }

        private bool TryChooseInputDataDir(out string inputDataDir)
        {
            var dialog = new FolderBrowserDialog();
            dialog.Description = "Select Data Dir";
            dialog.RootFolder = Environment.SpecialFolder.MyComputer;
            if (dialog.ShowDialog() == DialogResult.OK && Directory.Exists(dialog.SelectedPath))
            {
                inputDataDir = dialog.SelectedPath;
                return true;
            }
            inputDataDir = null;
            return false;
        }

        private void BtnChooseRootFileClick(object sender, RibbonControlEventArgs e)
        {
            if (TryChooseRootDefineFile(out var rootDefineFile))
            {
                RootDefineFile = rootDefineFile;
            }
        }

        private void BtnChooseDataDirClick(object sender, RibbonControlEventArgs e)
        {
            if (TryChooseInputDataDir(out var dataDir))
            {
                InputDataDir = dataDir;
            }
        }

        private void LoadDataToCurrentDoc()
        {

            Worksheet sheet = Globals.LubanAssistant.Application.ActiveSheet;

            var metaAttrs = ExcelUtil.ParseMetaAttrs(sheet);
            if (!metaAttrs.TryGetValue("table", out var tableName))
            {
                MessageBox.Show($"meta行未指定table名");
                return;
            }

            Task.Run(async () =>
            {
                try
                {
                    var tableDataInfo = LastLoadTableData = await DataLoaderUtil.LoadTableDataAsync(RootDefineFile, InputDataDir, tableName);
                    var title = ExcelUtil.ParseTitles(sheet);
                    ExcelUtil.FillRecords(sheet, metaAttrs, title, tableDataInfo);
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.StackTrace, e.Message);
                }
            });

        }

        private bool PromptIgnoreNotSaveData()
        {
            if (HasNotsaveDataInCurrentWorksapce())
            {
                if (MessageBox.Show("有未保存的数据，确定要覆盖吗？", "警告", MessageBoxButtons.OKCancel) == DialogResult.Cancel)
                {
                    return false;
                }
            }
            return true;
        }

        private bool HasNotsaveDataInCurrentWorksapce()
        {
            return false;
        }

        private void BtnLoadDataClick(object sender, RibbonControlEventArgs e)
        {
            if (!HasSetRootDefineFile())
            {
                MessageBox.Show("未设置root定义文件");
                return;
            }
            if (!Directory.Exists(InputDataDir))
            {
                MessageBox.Show("未设置加载目录");
                return;
            }
            if (PromptIgnoreNotSaveData())
            {
                LoadDataToCurrentDoc();
            }
        }

        private void SaveRecords(Action<Worksheet, Dictionary<string, string>, TableDataInfo, DefTable, TitleInfo> saveTask)
        {
            Worksheet sheet = Globals.LubanAssistant.Application.ActiveSheet;

            var metaAttrs = ExcelUtil.ParseMetaAttrs(sheet);
            if (!metaAttrs.TryGetValue("table", out var tableName))
            {
                MessageBox.Show($"meta行未指定table名");
                return;
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    if (LastLoadTableData == null)
                    {
                        LastLoadTableData = await DataLoaderUtil.LoadTableDataAsync(RootDefineFile, InputDataDir, tableName);
                    }

                    var tableDef = await DataLoaderUtil.LoadTableDefAsync(RootDefineFile, InputDataDir, tableName);
                    var title = ExcelUtil.ParseTitles(sheet);
                    saveTask(sheet, metaAttrs, LastLoadTableData, tableDef, title);
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.StackTrace, e.Message);
                }
            });
        }

        private void BtnSaveAllClick(object sender, RibbonControlEventArgs e)
        {
            SaveRecords((Worksheet sheet, Dictionary<string, string> metaAttrs, TableDataInfo tableDataInfo, DefTable defTable, TitleInfo title) =>
            {
                int usedRowNum = sheet.UsedRange.Rows.Count;
                int firstDataRowNum = int.Parse(metaAttrs["title_rows"]) + 2;
                if (firstDataRowNum <= usedRowNum)
                {
                    var newRecords = ExcelUtil.LoadRecordsInRange(defTable, sheet, title.RootTitle, (sheet.Range[$"A{firstDataRowNum}:A{usedRowNum}"]).EntireRow);
                    ExcelUtil.SaveRecords(InputDataDir, defTable, newRecords);
                    CleanRemovedRecordFiles(LastLoadTableData, newRecords);
                }
                else
                {
                    MessageBox.Show("没有可保存的数据");
                }
            });
        }

        private void CleanRemovedRecordFiles(TableDataInfo lastTableDataInfo, List<Record> newRecords)
        {
            string index = lastTableDataInfo.Table.IndexField.Name;

            var oldRecordDic = lastTableDataInfo.MainRecords.ToDictionary(r => r.Data.GetField(index));
            var newRecordDic = newRecords.ToDictionary(r => r.Data.GetField(index));
            foreach (var rec in lastTableDataInfo.MainRecords)
            {
                DType key = rec.Data.GetField(index);
                if (!newRecordDic.ContainsKey(key))
                {
                    try
                    {
                        File.Delete(Path.Combine(InputDataDir, rec.Source));
                        //MessageBox.Show($"删除 {Path.Combine(InputDataDir, rec.Source)}");
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show($"删除记录:{rec.Source}失败.\n:{e.StackTrace}", "删除失败");
                    }
                }
            }
        }

        private void BtnSaveSelectedClick(object sender, RibbonControlEventArgs e)
        {
            var selectRange = (Range)Globals.LubanAssistant.Application.Selection;
            if (selectRange == null || selectRange.Count == 0)
            {
                MessageBox.Show("没有选中的行");
                return;
            }
            SaveRecords((Worksheet sheet, Dictionary<string, string> metaAttrs, TableDataInfo tableDataInfo, DefTable defTable, TitleInfo title) =>
            {
                int usedRowNum = sheet.UsedRange.Rows.Count;
                if (title.RowNum + 1 < usedRowNum)
                {
                    var newRecords = ExcelUtil.LoadRecordsInRange(defTable, sheet, title.RootTitle, selectRange.EntireRow);
                    ExcelUtil.SaveRecords(InputDataDir, defTable, newRecords);
                }
                else
                {
                    MessageBox.Show("没有可保存的数据");
                }
            });
        }
    }
}