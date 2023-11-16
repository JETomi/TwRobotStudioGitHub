using ABB.Robotics.Controllers;
using ABB.Robotics.Math;
using ABB.Robotics.RobotStudio;
using ABB.Robotics.RobotStudio.Controllers;
using ABB.Robotics.RobotStudio.Environment;
using Octokit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TwRobotStudioGitHub
{
    public class Class1 : HostedAddInBase
    {

        // This is the entry point which will be called when the Add-in is loaded
        public static void AddinMain()
        {
            if (Directory.Exists(AddinPaths.dirTemp))
            {
                AddinControllerObject.ForceDeleteDirectory(AddinPaths.dirTemp);
            }
            Directory.CreateDirectory(AddinPaths.dirTemp);
            //
            AddinRsUi.CreateButton();
        }
        public override void OnLoad()
        {
            ABB.Robotics.RobotStudio.Project.ActiveProject.Closed += new EventHandler(unloadCallback);
            base.OnLoad();
        }


        public static void unloadCallback(object o, EventArgs e)
        {
            // This is the entry point which will be called when the Add-in is unloaded
            if (Directory.Exists(AddinPaths.dirTemp))
            {
                AddinControllerObject.ForceDeleteDirectory(AddinPaths.dirTemp);
            }
            //
        }
    }
}