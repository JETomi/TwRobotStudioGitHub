using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ABB.Robotics.Controllers;
using ABB.Robotics.Controllers.RapidDomain;
using ABB.Robotics.RobotStudio;
using ABB.Robotics.RobotStudio.Controllers;
using DiffPlex.Wpf.Controls;
using Octokit;

namespace TwRobotStudioGitHub
{
    /// <summary>
    /// Interaction logic for CompareFiles.xaml
    /// </summary>
    public partial class CompareFiles : UserControl
    {
        AddinControllerObject aco;
        public CompareFiles(AddinControllerObject _aco)
        {
            InitializeComponent();
            aco = _aco;
            this.Loaded += CompareFiles_Loaded;
            cbCommits.SelectionChanged += CbCommits_SelectionChanged;
            cbTasks.SelectionChanged += CbTasks_SelectionChanged;
            cbModules.SelectionChanged += CbModules_SelectionChanged;
        }

        private void CbModules_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                UpdateView();
            }
            catch (Exception ex)
            {
                Logger.AddMessage(new LogMessage(ex.ToString()));
            }
        }

        private void CbTasks_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                if (e.AddedItems[0] is ABB.Robotics.Controllers.RapidDomain.Task task)
                {
                    var modules = task.GetModules();
                    cbModules.ItemsSource = null;
                    cbModules.ItemsSource = modules;
                }
                else if (cbTasks.SelectedItem is ABB.Robotics.Controllers.RapidDomain.Task task2)
                {
                    var modules = task2.GetModules();
                    cbModules.ItemsSource = null;
                    cbModules.ItemsSource = modules;
                }
            }
        }

        public void UpdateView()
        {
            if (cbCommits.SelectedItem == null) return;
            if (cbModules.SelectedItem == null) return;
            //
            if (!System.IO.Directory.Exists(AddinPaths.dirTemp)) System.IO.Directory.CreateDirectory(AddinPaths.dirTemp);
            //Get controller module
            var mod = cbModules.SelectedItem as Module;
            string ctrlpath = "";
            //if (mod.Controller.IsRobotWare7)
            //{
            //    ctrlpath = "TEMP/";
            //    mod.Controller.FileSystem.RemoteDirectory = "";
            //}
            //else
            //{
                ctrlpath = aco.c.GetEnvironmentVariable("TEMP")+"/";
                mod.Controller.FileSystem.RemoteDirectory = "";
            //}
            mod.SaveToFile(ctrlpath);
            string modfile = $"{mod.Name}{getModuleExtension(mod)}";
            string localpath = $"{AddinPaths.dirTemp}{modfile}";
            mod.Controller.FileSystem.GetFile($"{ctrlpath}{modfile}", localpath, true);
            if (aco.c.IsRobotWare7)
            {
                DiffView.NewText = System.IO.File.ReadAllText(localpath);
            }
            else
            {
                DiffView.NewText = System.IO.File.ReadAllText(localpath, Encoding.Default);
            }
            //Get repo module
            string modDirPath = "";
            if (mod.IsSystem)
            {
                modDirPath = "SYSMOD";
            }
            else
            {
                modDirPath = "PROGMOD";
            }
            string taskNo = TaskNameToTaskNumber((cbTasks.SelectedItem as ABB.Robotics.Controllers.RapidDomain.Task).Name);
            if (taskNo == "")
            {
                Logger.AddMessage(new LogMessage($"Unable to determine task-number by name."));
            }
            else
            {
                string filePath = $"RAPID/TASK{taskNo}/{modDirPath}/{modfile}";
                DiffView.OldText = aco.GetFileContentFromCommit(cbCommits.SelectedItem as GitHubCommit, filePath);
            }
        }

        string TaskNameToTaskNumber(string TaskName)
        {
            try
            {
                //Don't know of a better way to do this..
                var backinfo = System.IO.File.ReadAllLines($"{aco.systemBackupDir}/BACKINFO/backinfo.txt");

                //Get row from backinfo file that contains task number.
                var row = backinfo.Last(r => r.Contains(">>TASK") && r.Contains(TaskName));

                //Get number of task from row like this ">>TASK2: (T_ROB1,,)"
                int pFrom = row.IndexOf(">>TASK") + ">>TASK".Length;
                int pTo = row.IndexOf(": ");
                string ret = row.Substring(pFrom, pTo - pFrom);
                //
                return ret;
            }
            catch (DirectoryNotFoundException)
            {

                Logger.AddMessage(new LogMessage($"The file {aco.systemBackupDir}/BACKINFO/backinfo.txt was not found. Try making a commit or system compare and then try again."));
            }
            return "";
        }

        string getModuleExtension(Module mod)
        {

            if (mod.Controller.IsRobotWare7)
            {
                if (mod.IsSystem)
                {
                    return ".sysx";
                }
                else
                {
                    return ".modx";
                }
            }
            else
            {
                if (mod.IsSystem)
                {
                    return ".sys";
                }
                else
                {
                    return ".mod";
                }
            }
        }

        private void CbCommits_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                UpdateView();
            }
            catch (Exception ex)
            {
                Logger.AddMessage(new LogMessage(ex.ToString()));
            }
        }

        private async void CompareFiles_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var commitlist = await aco.GetCommitList();
                cbCommits.ItemsSource = null;
                cbCommits.ItemsSource = commitlist;
            }
            catch (Exception ex)
            {
                Logger.AddMessage(new LogMessage(ex.ToString()));
            }
            try
            {
                var tasks = aco.c.Rapid.GetTasks();
                cbTasks.ItemsSource = null;
                cbTasks.ItemsSource = tasks;
            }
            catch (Exception ex)
            {
                Logger.AddMessage(new LogMessage(ex.ToString()));
            }
        }
    }
}
