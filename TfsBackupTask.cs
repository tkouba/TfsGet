using System;
using System.Collections.Generic;
using System.IO;
using CommandLine;
using CommandLine.Text;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace TfsGet
{
    [Verb("backup", HelpText = "Backup TFS repository to local disc.")]
    public class TfsBackupTask : TfsTaskBase
    {
        [Option('v', "ver", HelpText = "TFS version specification.")]
        public string Version { get; set; }

        [Usage]
        public static IEnumerable<Example> Examples
        {
            get
            {
                yield return new Example("copy files from server to local directory",
                    new TfsBackupTask
                    {
                        ServerUrl = @"https://myvso.visualstudio.com/DefaultCollection",
                        ServerProjectPath = @"$/MyProject/ProjectSubfolder",
                        TargetPath = @"""c:\Projects Folder""",
                        Login = @"user@mycompany.com,p@ssw0rd"
                    });
                //yield return new Example("specify bytes", new HeadOptions { FileName = "file.bin", Bytes = 100 });
                //yield return new Example("supress summary", UnParserSettings.WithGroupSwitchesOnly(), new HeadOptions { FileName = "file.bin", Quiet = true });
                //yield return new Example("read more lines", new[] { UnParserSettings.WithGroupSwitchesOnly(), UnParserSettings.WithUseEqualTokenOnly() }, new HeadOptions { FileName = "file.bin", Lines = 10 });
            }
        }

        protected override int Process(TfsTeamProjectCollection tpc)
        {
            if (Verbose)
                Console.WriteLine("Target Path:  {0}", TargetPath);

            var versionControl = tpc.GetService<VersionControlServer>();
            // Listen for the Source Control events.
            versionControl.NonFatalError += OnNonFatalError;

            IEnumerable<Changeset> history = QueryHistory(versionControl);

            foreach (Changeset set in history)
            {
                set.
            }
            var files = versionControl.GetItems(ServerProjectPath, version, RecursionType.Full);
            foreach (Item item in files.Items)
            {
                var localFilePath = GetLocalFilePath(item);

                switch (item.ItemType)
                {
                    case ItemType.Any:
                        throw new ArgumentOutOfRangeException("ItemType.Any - not sure what to do with this");
                    case ItemType.File:
                        if (!Silent)
                            Console.WriteLine("Getting: '{0}'", localFilePath);
                        item.DownloadFile(localFilePath);
                        break;
                    case ItemType.Folder:
                        if (!Silent)
                            Console.WriteLine("Creating Directory: {0}", localFilePath);
                        Directory.CreateDirectory(localFilePath);
                        break;
                }
            }
            return 0;
        }

        private IEnumerable<Changeset> QueryHistory(VersionControlServer versionControl)
        {
            QueryHistoryParameters qhp = new QueryHistoryParameters(ServerProjectPath, RecursionType.Full);
            if (!String.IsNullOrEmpty(Version))
            {
                VersionSpec[] vs = VersionSpec.Parse(Version, null);
                if (vs.Length == 2)
                {
                    qhp.VersionStart = vs[0];
                    qhp.VersionEnd = vs[1];
                }
                if (vs.Length == 1)
                {
                    qhp.VersionStart = vs[0];
                }
            }
            return versionControl.QueryHistory(qhp);
        }

        private string GetLocalFilePath(Item item)
        {
            var projectPath = ServerProjectPath;
            var pathExcludingLastFolder = projectPath.Substring(0, projectPath.LastIndexOf('/') + 1);
            string relativePath = item.ServerItem.Replace(pathExcludingLastFolder, "");
            var localFilePath = Path.Combine(TargetPath, relativePath);
            return localFilePath;
        }

    }
}
