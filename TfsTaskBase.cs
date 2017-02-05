using System;
using System.Collections.Generic;
using System.Net;
using CommandLine;
using CommandLine.Text;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.VisualStudio.Services.Common;

namespace TfsGet
{

    public abstract class TfsTaskBase
    {

        [Value(0, Required = true, MetaName = "ServerUrl", HelpText = "TFS Server URL")]
        public string ServerUrl { get; set; }
        [Value(1, Required = true, MetaName = "ProjectPath", HelpText = "Project path")]
        public string ServerProjectPath { get; set; }
        [Value(2, MetaName = "TargetPath", HelpText = "Target local directory")]
        public string TargetPath { get; set; }

        [Option('y', "login", HelpText = "Specifies the user account to run the command.")]
        public string Login { get; set; }

        [Option("verbose", HelpText = "Show verbose output.")]
        public bool Verbose { get; set; }

        [Option("silent", HelpText ="Supress any messages.")]
        public bool Silent { get; set; }

        private VssCredentials credentials = null;
        public VssCredentials Credentials
        {
            get
            {
                if (credentials == null)
                {
                    if (!String.IsNullOrEmpty(Login))
                    {
                        int separatorPosition = Login.IndexOf(',');
                        string user = separatorPosition >= 0 ?
                            Login.Substring(0, separatorPosition) :
                            Login;
                        string password = separatorPosition >= 0 ?
                            Login.Substring(separatorPosition + 1) :
                            null;
                        var networkCreds = new NetworkCredential(user, password);
                        var creds = new VssBasicCredential(networkCreds);
                        credentials = new VssCredentials(creds)
                        {
                            PromptType = password == null ? CredentialPromptType.PromptIfNeeded : CredentialPromptType.DoNotPrompt
                        };
                    }
                    //else
                    //    credentials = new WindowsCredential();
                }
                return credentials;
            }
        }

        public virtual int Execute()
        {
            int retVal = 0;
            using (var tpc = new TfsTeamProjectCollection(new Uri(ServerUrl), Credentials))
            {
                retVal = CheckAccess(tpc);
                if (retVal == 0)
                    retVal = Process(tpc);
            }
            return retVal;
        }

        protected int CheckAccess(TfsTeamProjectCollection tpc)
        {
            try
            {
                if (Verbose)
                {
                    Console.WriteLine("Server Url:   {0}", ServerUrl);
                    Console.WriteLine("Project Path: {0}", ServerProjectPath);
                }
                tpc.Authenticate();
            }
            catch (Exception ex)
            {
                Console.WriteLine("TFS Authentication Failed: {0}", ex.Message);
                return 1;
            }
            return 0;
        }

        protected abstract int Process(TfsTeamProjectCollection tpc);

        protected virtual void OnNonFatalError(Object sender, ExceptionEventArgs e)
        {
            var message = e.Exception != null ? e.Exception.Message : e.Failure.Message;
            Console.Error.WriteLine("Exception: " + message);
        }


        //public static TfsParams Create(IList<string> args)
        //{
        //    if (args.Count < 5)
        //    {
        //        Console.WriteLine("Please supply 5 or 6 parameters: tfsServerUrl serverProjectPath targetPath userName password [silent]");
        //        Console.WriteLine("The optional 6th 'silent' parameter will suppress listing each file downloaded");
        //        Console.WriteLine(@"Ex: tfsget ""https://myvso.visualstudio.com/DefaultCollection"" ""$/MyProject/ProjectSubfolder"" ""c:\Projects Folder"", user, password ");

        //        Environment.Exit(1);
        //    }

        //    var tfsServerUrl = args[0]; //"https://myvso.visualstudio.com/DefaultCollection";
        //    var serverProjectPath = args[1]; // "$/MyProject/Folder Path";
        //    var targetPath = args[2]; // @"c:\Projects\";
        //    var userName = args[3]; //"login";
        //    var password = args[4]; //"passsword";
        //    var silentFlag = args.Count >= 6 && (args[5].ToLower() == "silent"); //"silent";
        //    var tfsCredentials = GetTfsCredentials(userName, password);

        //    var tfsParams = new TfsParams
        //    {
        //        ServerUrl = tfsServerUrl,
        //        ServerProjectPath = serverProjectPath,
        //        TargetPath = targetPath,
        //        Credentials = tfsCredentials,
        //        Silent = silentFlag,
        //    };
        //    return tfsParams;
        //}

    }
}
