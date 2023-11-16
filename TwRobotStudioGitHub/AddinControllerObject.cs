using ABB.Robotics.Controllers;
using ABB.Robotics.Controllers.Configuration;
using ABB.Robotics.Controllers.Discovery;
using ABB.Robotics.RobotStudio.Controllers;
using ABB.Robotics.RobotStudio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Octokit;
using LibGit2Sharp;
using Microsoft.VisualBasic.ApplicationServices;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;
using System.Diagnostics;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ListView;

namespace TwRobotStudioGitHub
{
    public class AddinControllerObject
    {

        #region Robotdata
        public Controller c = null;
        public string DIR_BACKUP_CTRL;
        public string systemName
        {
            get
            {
                if (c.IsVirtual)
                {
                    return $"{System.Environment.MachineName}_{c.SystemName}";
                }
                else
                {
                    return $"{c.SystemName}";
                }
            }
        }
        #endregion
        #region Paths
        public string remoteBackupDir
        {
            get
            {
                c.FileSystem.RemoteDirectory = "";
                return c.GetEnvironmentVariable("TEMP")+"/TempBackup";
            }
        }
        public string systemBackupDir
        {
            get
            {
                return AddinPaths.dirBackups + systemName;
            }
        }
        public string systemRepoDir
        {
            get
            {
                return AddinPaths.dirRepos + systemName;
            }
        }
        #endregion
        #region Githubdata
        public Octokit.User user;
        public string fakeEmail = "aaaaaaaaaaaaaaa@aaaaaaaaaaaa.aaaaaaaaa";
        public GitHubClient client = new GitHubClient(new ProductHeaderValue("TwRobotStudioGitHub"));
        Octokit.Repository Repo = null;
        private UsernamePasswordCredentials _credentials;
        LibGit2Sharp.Signature author;
        LibGit2Sharp.Signature committer;
        CloneOptions co;
        string gitEmail
        {
            get
            {
                if (string.IsNullOrEmpty(AddinDatas.gitDisplayEmail))
                {
                    return fakeEmail;
                }
                else
                {
                    return AddinDatas.gitDisplayEmail;
                }
            }
        }
        string gitUsername
        {
            get
            {
                if (string.IsNullOrEmpty(AddinDatas.gitDisplayName))
                {
                    return Environment.UserName;
                }
                else
                {
                    return AddinDatas.gitDisplayName;
                }
            }
        }
        #endregion
        bool bCompare = false;
        bool bCommit = false;
        void DoCommit()
        {
            bCompare = false;
            bCommit = true;
        }
        void DoCompare()
        {
            bCommit = false;
            bCompare = true;
        }

        #region PCSDK Code
        public AddinControllerObject(Guid SystemId)
        {
            try
            {
                RobotConnect(SystemId);
            }
            catch (Exception ex)
            {
                Logger.AddMessage("Controller connect failed");
                Logger.AddMessage(new LogMessage(ex.ToString()));
            }
            //
            try
            {
                _ = GithubAuth();
            }
            catch (Exception ex)
            {
                Logger.AddMessage("GitHub authentication failed");
                Logger.AddMessage(new LogMessage(ex.ToString()));
            }
        }
        public void RobotConnect(Guid SystemId)
        {
            try
            {
                if (c != null)
                {
                    if (c.Rapid.IsMaster == null) return;
                    return;
                }
            }
            catch
            {

            }
            c = Controller.Connect(SystemId, ConnectionType.RobotStudio, false);
            c.BackupCompleted += new EventHandler<BackupEventArgs>(c_BackupCompleted);
        }
        public void CreateRemoteBackupAndCommit()
        {
            DoCommit();
            CreateRemoteBackup();
        }
        public void CreateRemoteBackupAndCompare()
        {
            DoCompare();
            CreateRemoteBackup();
        }
        void CreateRemoteBackup()
        {
            if (System.IO.Directory.Exists(systemBackupDir)) ForceDeleteDirectory(systemBackupDir);
            try
            {
                // Start the backup operation on the controller
                // After backup complete, transfer backup to local folder.
                c.Backup(remoteBackupDir);
            }
            catch (Exception ex)
            {
                Logger.AddMessage(new LogMessage("Could not create backup on controller."));
                Logger.AddMessage(ex.ToString());
                return;
            }
        }
        private void c_BackupCompleted(object sender, ABB.Robotics.Controllers.BackupEventArgs e)
        {
            if (!e.Succeeded)
            {
                Logger.AddMessage(new LogMessage("Backup failed"));
                return;
            }
            // Copy the remote backup directory from the controller to the local machine
            ForceDeleteDirectory(systemBackupDir);
            System.IO.Directory.CreateDirectory(systemBackupDir);
            while (!System.IO.Directory.Exists(systemBackupDir)) ;
            try
            {
                c.FileSystem.GetDirectory(remoteBackupDir, systemBackupDir, true);
            }
            catch (Exception ex)
            {
                //Directory.Delete(localDir, true);
                Logger.AddMessage(new LogMessage("Could not copy backup from controller."));
                Logger.AddMessage(new LogMessage(ex.ToString()));
                return;
            }
            // Remove the remote backup directory from controller
            try
            {
                c.FileSystem.RemoveDirectory(remoteBackupDir, true);
            }
            catch (Exception ex)
            {
                Logger.AddMessage(new LogMessage("Could not remove temporary backup from controller."));
                Logger.AddMessage(new LogMessage(ex.ToString()));
                return;
            }
            Logger.AddMessage(new LogMessage($"Successfully saved backup from controller to {systemBackupDir}"));
            if (bCommit) ExecuteCommit();
            if (bCompare) ExecuteCompare();
        }
        #endregion

        #region gitlib2sharp Code
        /// <summary>
        /// Commits all changes.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <exception cref="System.Exception"></exception>
        public void CommitAllChanges(string message)
        {
            var _localFolder = new DirectoryInfo(systemRepoDir);
            using (var repo = new LibGit2Sharp.Repository(_localFolder.FullName))
            {
                var files = _localFolder.GetFiles("*", SearchOption.AllDirectories).Select(f => f.FullName);
                Commands.Stage(repo, files);
                try
                {
                    repo.Commit(message, author, committer);
                }
                catch (EmptyCommitException)
                {
                    //No changes detected
                }
            }
        }
        public void CompareChanges()
        {
            Logger.AddMessage(new LogMessage("Differences between system and repository."));
            var _localFolder = new DirectoryInfo(systemRepoDir);
            using (var repo = new LibGit2Sharp.Repository(_localFolder.FullName))
            {
                var lastCommit = repo.Commits.First();
                var files = _localFolder.GetFiles("*", SearchOption.AllDirectories).Select(f => f.FullName);
                Commands.Stage(repo, files);
                try
                {
                    var dummyCommit = repo.Commit("dummy", author, committer);
                    foreach (var c in repo.Diff.Compare<TreeChanges>(lastCommit.Tree,dummyCommit.Tree))
                    {
                        Logger.AddMessage(new LogMessage($"{c.Status} : {c.Path}"));
                    }
                }
                catch (EmptyCommitException)
                {
                    //No changes detected
                }
            }
        }

        /// <summary>
        /// Pushes all commits.
        /// </summary>
        public void PushCommits()
        {
            var _localFolder = new DirectoryInfo(systemRepoDir);
            using (var repo = new LibGit2Sharp.Repository(_localFolder.FullName))
            {
                var remote = repo.Network.Remotes.FirstOrDefault(r => r.Name == "origin");
                if (remote == null)
                {
                    repo.Network.Remotes.Add("origin", Repo.CloneUrl);
                    remote = repo.Network.Remotes.FirstOrDefault(r => r.Name == "origin");
                }
                var options = new PushOptions
                {
                    CredentialsProvider = (url, usernameFromUrl, types) => _credentials
                };
                string temp = remote.Url;
                repo.Network.Push(remote, repo.Head.CanonicalName, options);
            }
        }



        //Compare
        public string GetFileContentFromCommit(GitHubCommit selectedCommit, string filePath)
        {
            //2. Create local project folder
            if (!LibGit2Sharp.Repository.IsValid(systemRepoDir))
            {
                ForceDeleteDirectory(systemRepoDir);
                CreateLocalControllerFolder(systemRepoDir);
                LibGit2Sharp.Repository.Clone(Repo.CloneUrl, systemRepoDir, co);
            }
            //3. Clone remote repo to local pc
            try
            {
                var _localFolder = new DirectoryInfo(systemRepoDir);
                using (var repo = new LibGit2Sharp.Repository(_localFolder.FullName))
                {
                    var commit = repo.Lookup<LibGit2Sharp.Commit>(selectedCommit.Sha);
                    var treeEntry = commit[filePath];
                    //Debug.Assert(treeEntry.TargetType == TreeEntryTargetType.Blob);
                    var blob = (LibGit2Sharp.Blob)treeEntry.Target;
                    var contentStream = blob.GetContentStream();
                    if (c.IsRobotWare7)
                    {
                        using (var tr = new StreamReader(contentStream))
                        {
                            string content = tr.ReadToEnd();
                            return content;
                        }
                    }
                    else
                    {
                        using (var tr = new StreamReader(contentStream, Encoding.Default))
                        {
                            string content = tr.ReadToEnd();
                            return content;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.AddMessage(new LogMessage("Module doesn't exist in commit"));
                //Logger.AddMessage(new LogMessage(ex.ToString()));
            }
            return "Module doesn't exist in commit";
        }
        #endregion

        #region octokit Code
        public async Task GithubAuth()
        {
            if (user != null) return;
            var tokenAuth = new Octokit.Credentials(AddinDatas.gitToken); // NOTE: not real token
            client.Credentials = tokenAuth;
            user = await client.User.Current();

            //Setup libgit2sharp data
            _credentials = new UsernamePasswordCredentials
            {
                Username = user.Login,
                Password = AddinDatas.gitToken
            };
            author = new LibGit2Sharp.Signature(gitUsername, gitEmail, DateTime.Now);
            committer = author;

            co = new CloneOptions();
            co.CredentialsProvider = (_url, _user, _cred) => new UsernamePasswordCredentials { Username = user.Login, Password = AddinDatas.gitToken };
        }
        async Task<Octokit.Repository> getRepo()
        {
            if (Repo != null) return Repo;
            IReadOnlyList<Octokit.Repository> repos;
            repos = await client.Repository.GetAllForUser(user.Login);
            //Check if repo exists
            foreach (var item in repos)
            {
                if (item.Name == systemName)
                {
                    //Existing repo found
                    return item;
                }
            }
            //Creating new repo
            var newRepo = new NewRepository(systemName)
            {
                AutoInit = true,
                Private = true
            };
            try
            {
                return await client.Repository.Create(newRepo);
            }
            catch (RepositoryExistsException)
            {
                //Existing repo found
                return await client.Repository.Get(user.Login, systemName);
            }
            catch (Exception ex)
            {
                Logger.AddMessage("Failed to create a new repository.");
                Logger.AddMessage(new LogMessage(ex.ToString()));
                return null;
            }
        }
        public async void ExecuteCommit()
        {
            try
            {
                // Login @ github
                await GithubAuth();
                if (user == null)
                {
                    Logger.AddMessage(new LogMessage("Github authentication failed."));
                    return;
                }
                //1. Get or create repo.
                Repo = await getRepo();
                if (System.IO.Directory.Exists(systemRepoDir)) ForceDeleteDirectory(systemRepoDir);
                //2. Create local project folder
                CreateLocalControllerFolder(systemRepoDir);
                //3. Clone remote repo to local pc
                LibGit2Sharp.Repository.Clone(Repo.CloneUrl, systemRepoDir, co);
                //4. Move robot backup to local repo folder
                CopyAll(systemBackupDir, systemRepoDir);
                try
                {
                    //Get commit message from user
                    var commitmsg = Microsoft.VisualBasic.Interaction.InputBox("Github commit", "Enter commit message.", "");
                    if (string.IsNullOrEmpty(commitmsg)) { 
                        Logger.AddMessage(new LogMessage("Commit aborted, no message."));
                        return; 
                    }
                    // Create commit
                    CommitAllChanges(commitmsg);
                }
                catch (Exception ex)
                {
                    Logger.AddMessage(new LogMessage(ex.ToString()));
                    return;
                }
                // Upload commit
                PushCommits();
                //
                await OpenGithubRepoInBrowser();
            }
            catch (Exception ex)
            {
                Logger.AddMessage(new LogMessage(ex.ToString()));
                return;
            }
        }
        public async void ExecuteCompare()
        {
            try
            {
                // Login @ github
                await GithubAuth();
                if (user == null)
                {
                    Logger.AddMessage(new LogMessage("Github authentication failed."));
                    return;
                }
                //1. Get or create repo.
                Repo = await getRepo();
                if (System.IO.Directory.Exists(systemRepoDir)) ForceDeleteDirectory(systemRepoDir);
                //2. Create local project folder
                CreateLocalControllerFolder(systemRepoDir);
                //3. Clone remote repo to local pc
                LibGit2Sharp.Repository.Clone(Repo.CloneUrl, systemRepoDir, co);
                //4. Move robot backup to local repo folder
                CopyAll(systemBackupDir, systemRepoDir);
                try
                {
                    CompareChanges();
                }
                catch (Exception ex)
                {
                    Logger.AddMessage(new LogMessage(ex.ToString()));
                    return;
                }
            }
            catch (Exception ex)
            {
                Logger.AddMessage(new LogMessage(ex.ToString()));
                return;
            }
        }

        public async Task OpenGithubRepoInBrowser()
        {
            // Login @ github
            await GithubAuth();
            if (user == null)
            {
                Logger.AddMessage(new LogMessage("Github authentication failed."));
                return;
            }
            //1. Get or create repo.
            Repo = await getRepo();
            if (Repo == null)
            {
                Logger.AddMessage(new LogMessage("No repository found, can't open browser."));
                return;
            }
            // Open github repo in browser
            System.Diagnostics.Process.Start(Repo.HtmlUrl);
        }

        public async Task<IReadOnlyList<GitHubCommit>> GetCommitList()
        {
            // Login @ github
            await GithubAuth();
            if (user == null)
            {
                Logger.AddMessage(new LogMessage("Github authentication failed."));
                return null;
            }
            //1. Get or create repo.
            Repo = await getRepo();
            //
            var Commits = await client.Repository.Commit.GetAll(Repo.Id);

            return Commits;
            //
            //if (System.IO.Directory.Exists(systemRepoDir)) ForceDeleteDirectory(systemRepoDir);
            ////2. Create local project folder
            //CreateLocalControllerFolder(systemRepoDir);
            ////
            //LibGit2Sharp.Repository.Clone(Repo.CloneUrl, systemRepoDir, co);
            ////
            //var _localFolder = new DirectoryInfo(systemRepoDir);
            //using (var repo = new LibGit2Sharp.Repository(_localFolder.FullName))
            //{
            //    return new List<LibGit2Sharp.Commit>(repo.Commits);
            //}
        }
        #endregion
        #region Filesystem methods
        public static void ForceDeleteDirectory(string path)
        {
            if (!System.IO.Directory.Exists(path)) return;
            var directory = new DirectoryInfo(path) { Attributes = FileAttributes.Normal };

            foreach (var info in directory.GetFileSystemInfos("*", SearchOption.AllDirectories))
            {
                info.Attributes = FileAttributes.Normal;
            }

            directory.Delete(true);
        }
        private static void CreateLocalControllerFolder(string projectPath)
        {
            System.IO.Directory.CreateDirectory(projectPath);
        }
        public static void CopyAll(string _source, string _target)
        {
            DirectoryInfo source = new DirectoryInfo(_source);
            DirectoryInfo target = new DirectoryInfo(_target);

            Directory.CreateDirectory(target.FullName);

            // Copy each file into the new directory.
            foreach (FileInfo fi in source.GetFiles())
            {
                Console.WriteLine(@"Copying {0}\{1}", target.FullName, fi.Name);
                fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
            }

            // Copy each subdirectory using recursion.
            foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
            {
                DirectoryInfo nextTargetSubDir =
                    target.CreateSubdirectory(diSourceSubDir.Name);
                CopyAll(diSourceSubDir.FullName, nextTargetSubDir.FullName);
            }

        }
        #endregion
    }
}
