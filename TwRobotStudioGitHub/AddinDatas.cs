// Ignore Spelling: Addin Tw

using ABB.Robotics.Controllers;
using ABB.Robotics.Controllers.RapidDomain;
using ABB.Robotics.RobotStudio;
using ABB.Robotics.RobotStudio.Controllers;
using Microsoft.VisualBasic.ApplicationServices;
using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwRobotStudioGitHub
{
    public static class AddinPaths
    {
        //Dir's
        public static string dirBackups = "C:\\ProgramData\\ABB Industrial IT\\Robotics IT\\DistributionPackages\\TwRobotstudioGithub-0.1\\data\\Backups\\";
        public static string dirRepos = "C:\\ProgramData\\ABB Industrial IT\\Robotics IT\\DistributionPackages\\TwRobotstudioGithub-0.1\\data\\Repos\\";
        public static string dirTemp = "C:\\ProgramData\\ABB Industrial IT\\Robotics IT\\DistributionPackages\\TwRobotstudioGithub-0.1\\data\\Temp\\";
        //Files
        public static string fileToken = "C:\\ProgramData\\ABB Industrial IT\\Robotics IT\\DistributionPackages\\TwRobotstudioGithub-0.1\\data\\Token.txt";
        public static string fileDisplayName = "C:\\ProgramData\\ABB Industrial IT\\Robotics IT\\DistributionPackages\\TwRobotstudioGithub-0.1\\data\\Displayname.txt";
        public static string fileDisplayEmail = "C:\\ProgramData\\ABB Industrial IT\\Robotics IT\\DistributionPackages\\TwRobotstudioGithub-0.1\\data\\Displayemail.txt";
    }
    public static class AddinDatas
    {
        #region Github token
        static string _gitToken = "";
        static string _gitDisplayName = "";
        static string _gitDisplayEmail = "";
        public static string gitToken
        {
            get
            {
                if (_gitToken == "")
                {
                    if (System.IO.File.Exists(AddinPaths.fileToken))
                    {
                        _gitToken = System.IO.File.ReadAllText(AddinPaths.fileToken);
                    }
                }
                return _gitToken;
            }
        }
        public static string gitDisplayName
        {
            get
            {
                if (_gitDisplayName == "")
                {
                    if (System.IO.File.Exists(AddinPaths.fileDisplayName))
                    {
                        _gitDisplayName = System.IO.File.ReadAllText(AddinPaths.fileDisplayName);
                    }
                }
                return _gitDisplayName;
            }
        }
        public static string gitDisplayEmail
        {
            get
            {
                if (_gitDisplayEmail == "")
                {
                    if (System.IO.File.Exists(AddinPaths.fileDisplayEmail))
                    {
                        _gitDisplayEmail = System.IO.File.ReadAllText(AddinPaths.fileDisplayEmail);
                    }
                }
                return _gitDisplayEmail;
            }
        }
        public static void UpdateToken(string token)
        {
            System.IO.File.WriteAllText(AddinPaths.fileToken, token);
            _gitToken = token;
        }
        public static void UpdateDisplayName(string name)
        {
            System.IO.File.WriteAllText(AddinPaths.fileDisplayName, name);
            _gitDisplayName = name;
        }
        public static void UpdateDisplayEmail(string email)
        {
            System.IO.File.WriteAllText(AddinPaths.fileDisplayEmail, email);
            _gitDisplayEmail = email;
        }
        #endregion
        #region ControllerObjectHandler
        static Dictionary<string, AddinControllerObject> controllerObjects = new Dictionary<string, AddinControllerObject>();
        private static AddinControllerObject getControllerFromSystemId(Guid systemId)
        {
            AddinControllerObject ret = null;
            try
            {
                ret = controllerObjects[systemId.ToString()];
                ret.RobotConnect(systemId);
            }
            catch { }
            if (ret == null)
            {
                ret = new AddinControllerObject(systemId);
                controllerObjects[systemId.ToString()] = ret;
            }
            return ret;
        }
        public static AddinControllerObject getSelectedControllerObject()
        {
            var selectedController = ControllerManager.SelectedControllerObject.Root;
            return getControllerFromSystemId(selectedController.SystemId);
        }
        /*public static AddinControllerObject getControllerObject(Guid SystemId)
        {
            return getControllerFromSystemId(SystemId);
        }*/
        #endregion
    }
}
