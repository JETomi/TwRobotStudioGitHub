using ABB.Robotics.Controllers;
using ABB.Robotics.RobotStudio.Controllers;
using ABB.Robotics.RobotStudio.Environment;
using ABB.Robotics.RobotStudio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Web.UI.WebControls;
using System.Windows.Forms.Integration;

namespace TwRobotStudioGitHub
{
    public static class AddinRsUi
    {

        #region ExecuteCommand
        private static void buttonCommit_ExecuteCommand(object sender, ExecuteCommandEventArgs e)
        {
            try
            {
                AddinDatas.getSelectedControllerObject().CreateRemoteBackupAndCommit();
            }
            catch (Exception ex)
            {
                Logger.AddMessage(new ExceptionLogMessage("buttonCommit_ExecuteCommand encountered an exception", ex));
            }
        }
        private static async void buttonRepo_ExecuteCommand(object sender, ExecuteCommandEventArgs e)
        {
            try
            {
                await AddinDatas.getSelectedControllerObject().OpenGithubRepoInBrowser();
            }
            catch (Exception ex)
            {
                Logger.AddMessage(new ExceptionLogMessage("buttonRepo_ExecuteCommand encountered an exception", ex));
            }
        }
        private static void buttonCompareRepo_ExecuteCommand(object sender, ExecuteCommandEventArgs e)
        {
            try
            {
                AddinDatas.getSelectedControllerObject().CreateRemoteBackupAndCompare();
            }
            catch (Exception ex)
            {
                Logger.AddMessage(new ExceptionLogMessage("buttonCompareRepo_ExecuteCommand encountered an exception", ex));
            }
        }
        private static void buttonCompareFiles_ExecuteCommand(object sender, ExecuteCommandEventArgs e)
        {
            try
            {
                // Create a new DocumentWindow with a WPF control
                var wpfHost = new ElementHost();
                wpfHost.Child = new CompareFiles(AddinDatas.getSelectedControllerObject());
                var _wpfWindow = new DocumentWindow(null, wpfHost, "Git - compare");
                _wpfWindow.Closed += (s, e1) =>
                {
                    _wpfWindow = null;
                };
                UIEnvironment.Windows.Add(_wpfWindow);
            }
            catch (Exception ex)
            {
                Logger.AddMessage(new ExceptionLogMessage("buttonCompareFiles_ExecuteCommand encountered an exception", ex));
            }
        }
        private static void buttonSetupToken_ExecuteCommand(object sender, ExecuteCommandEventArgs e)
        {
            try
            {
                var token = Microsoft.VisualBasic.Interaction.InputBox("Github token", "Enter github token", AddinDatas.gitToken);
                if (token != null)
                {
                    if (token == "") return;
                    AddinDatas.UpdateToken(token);
                }
            }
            catch
            {

            }
        }
        private static void buttonSetupName_ExecuteCommand(object sender, ExecuteCommandEventArgs e)
        {
            try
            {
                var name = Microsoft.VisualBasic.Interaction.InputBox("Github displayname", "Enter name displayed in commits", AddinDatas.gitDisplayName);
                if (name != null)
                {
                    AddinDatas.UpdateDisplayName(name);
                }
            }
            catch
            {

            }
        }
        private static void buttonSetupEmail_ExecuteCommand(object sender, ExecuteCommandEventArgs e)
        {
            try
            {
                var email = Microsoft.VisualBasic.Interaction.InputBox("Github account email", "Enter email for account displayed in commits", AddinDatas.gitDisplayEmail);
                if (email != null)
                {
                    AddinDatas.UpdateDisplayEmail(email);
                }
            }
            catch
            {

            }
        }
        #endregion
        #region UpdateCommandUI
        private static void ControllerAndTokenAvailable_UpdateCommandUI(object sender, UpdateCommandUIEventArgs e)
        {
            // This enables the button if controller is selected and github token exists.
            if (ControllerManager.SelectedControllerObject == null)
            {
                e.Enabled = false;
                return;
            }
            if (AddinDatas.gitToken == "")
            {
                e.Enabled = false;
                return;
            }
            e.Enabled = true;
        }
        #endregion

        public static void CreateButton()
        {
            //Begin UndoStep
            ABB.Robotics.RobotStudio.Project.UndoContext.BeginUndoStep("Add Buttons");

            try
            {
                //Splitbutton
                CommandBarGalleryPopup buttonSplitBehaviour = new CommandBarGalleryPopup("twRobotStudioGitHub-SplitButton", "Git version control");
                buttonSplitBehaviour.Image = Properties.Resources.git_logo;
                buttonSplitBehaviour.Enabled = CommandBarPopupEnableMode.Enabled;
                buttonSplitBehaviour.HelpText = "tw GitHub integration";

                //Commit button
                CommandBarButton buttonCommit = new CommandBarButton("twRobotStudioGitHub-commit", "Commit changes");
                buttonCommit.HelpText = "Uploads backup to repository.";
                buttonCommit.DefaultEnabled = true;
                buttonCommit.Image = Properties.Resources.commit_32;
                buttonCommit.LargeImage = Properties.Resources.commit_32;
                buttonCommit.UpdateCommandUI += new UpdateCommandUIEventHandler(ControllerAndTokenAvailable_UpdateCommandUI);
                buttonCommit.ExecuteCommand += new ExecuteCommandEventHandler(buttonCommit_ExecuteCommand);

                //Open button
                CommandBarButton buttonRepo = new CommandBarButton("twRobotStudioGitHub-open", "Open repository");
                buttonRepo.HelpText = "Opens repository in browser";
                buttonRepo.DefaultEnabled = true;
                buttonRepo.Image = Properties.Resources.github_32;
                buttonRepo.LargeImage = Properties.Resources.github_32;
                buttonRepo.UpdateCommandUI += new UpdateCommandUIEventHandler(ControllerAndTokenAvailable_UpdateCommandUI);
                buttonRepo.ExecuteCommand += new ExecuteCommandEventHandler(buttonRepo_ExecuteCommand);

                //Compare system button
                CommandBarButton buttonCompareRepo = new CommandBarButton("twRobotStudioGitHub-compare-repo", "Compare system");
                buttonCompareRepo.HelpText = "Compares repository with system";
                buttonCompareRepo.DefaultEnabled = false;
                buttonCompareRepo.Image = Properties.Resources.code_branch_32;
                buttonCompareRepo.LargeImage = Properties.Resources.code_branch_32;
                buttonCompareRepo.UpdateCommandUI += new UpdateCommandUIEventHandler(ControllerAndTokenAvailable_UpdateCommandUI);
                buttonCompareRepo.ExecuteCommand += new ExecuteCommandEventHandler(buttonCompareRepo_ExecuteCommand);

                //Compare files button
                CommandBarButton buttonCompareFile = new CommandBarButton("twRobotStudioGitHub-compare-file", "Compare modules");
                buttonCompareFile.HelpText = "Compares current modules with commits";
                buttonCompareFile.DefaultEnabled = false;
                buttonCompareFile.Image = Properties.Resources.compare_files_32;
                buttonCompareFile.LargeImage = Properties.Resources.compare_files_32;
                buttonCompareFile.UpdateCommandUI += new UpdateCommandUIEventHandler(ControllerAndTokenAvailable_UpdateCommandUI);
                buttonCompareFile.ExecuteCommand += new ExecuteCommandEventHandler(buttonCompareFiles_ExecuteCommand);


                //Settings button
                CommandBarPopup popupSettings = new CommandBarPopup("twRobotStudioGitHub-settings", "Settings");
                popupSettings.Enabled = CommandBarPopupEnableMode.Enabled;
                popupSettings.Image = Properties.Resources.setting_32;
                popupSettings.HelpText = "Addin settings";


                //Setup token
                CommandBarButton buttonSetupToken = new CommandBarButton("twRobotStudioGitHub-setup-token", "Set github token");
                buttonSetupToken.HelpText = "Enter GitHub token";
                buttonSetupToken.DefaultEnabled = true;
                buttonSetupToken.Image = Properties.Resources.token_32;
                buttonSetupToken.LargeImage = Properties.Resources.token_32;
                buttonSetupToken.ExecuteCommand += new ExecuteCommandEventHandler(buttonSetupToken_ExecuteCommand);
                //Setup github displayname
                CommandBarButton buttonSetupDisplayname = new CommandBarButton("twRobotStudioGitHub-setup-displayname", "Change author name");
                buttonSetupDisplayname.HelpText = "Name showed in commits";
                buttonSetupDisplayname.DefaultEnabled = true;
                buttonSetupDisplayname.Image = Properties.Resources.username_32;
                buttonSetupDisplayname.LargeImage = Properties.Resources.username_32;
                buttonSetupDisplayname.ExecuteCommand += new ExecuteCommandEventHandler(buttonSetupName_ExecuteCommand);
                //Setup github displayemail
                CommandBarButton buttonSetupDisplayemail = new CommandBarButton("twRobotStudioGitHub-setup-displayemail", "Change author email");
                buttonSetupDisplayemail.HelpText = "Email links GitHub account to commits";
                buttonSetupDisplayemail.DefaultEnabled = true;
                buttonSetupDisplayemail.Image = Properties.Resources.email_32;
                buttonSetupDisplayemail.LargeImage = Properties.Resources.email_32;
                buttonSetupDisplayemail.ExecuteCommand += new ExecuteCommandEventHandler(buttonSetupEmail_ExecuteCommand);
                //Add settings menubuttons
                popupSettings.Controls.Add(buttonSetupToken);
                popupSettings.Controls.Add(buttonSetupDisplayname);
                popupSettings.Controls.Add(buttonSetupDisplayemail);


                //Setup splitbutton
                buttonSplitBehaviour.ClickButton = buttonCommit;
                buttonSplitBehaviour.GalleryTextPosition = GalleryTextPosition.Right;
                buttonSplitBehaviour.GalleryItemSize = new System.Drawing.Size(200, 32);
                //Add menubuttons
                buttonSplitBehaviour.GalleryControls.Add(buttonCommit);
                buttonSplitBehaviour.GalleryControls.Add(buttonRepo);
                buttonSplitBehaviour.GalleryControls.Add(buttonCompareRepo);
                buttonSplitBehaviour.GalleryControls.Add(buttonCompareFile);
                buttonSplitBehaviour.GalleryControls.Add(popupSettings);
                //Add to ribbontab
                UIEnvironment.RibbonTabs[3].Groups[1].Controls.Add(buttonSplitBehaviour);

            }
            catch (Exception ex)
            {
                ABB.Robotics.RobotStudio.Project.UndoContext.CancelUndoStep(CancelUndoStepType.Rollback);
                Logger.AddMessage(new LogMessage(ex.Message.ToString()));
            }
            finally
            {
                ABB.Robotics.RobotStudio.Project.UndoContext.EndUndoStep();
            }
        }


    }
}
