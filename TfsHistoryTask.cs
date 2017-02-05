using System;
using System.Collections.Generic;
using System.Linq;
using CommandLine;
using CommandLine.Text;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace TfsGet
{

    [Verb("history", HelpText = "Displays the revision history of project or folder.")]
    public class TfsHistoryTask : TfsTaskBase
    {
        [Option('f', "format",
            Default = HistoryFormat.Brief,
            HelpText = "Output format (Brief, Detailed, Csv, Md)")]
        public HistoryFormat Format { get; set; }

        [Option("comments", HelpText = "Use comments, when changeset doesn't contain associated work item. (Only for MD format)")]
        public bool UseComments { get; set; }

        [Option('v', "ver", HelpText = "TFS version specification.")]
        public string Version { get; set; }

        protected override int Process(TfsTeamProjectCollection tpc)
        {
            VersionControlServer versionControl = tpc.GetService<VersionControlServer>();
            // Listen for the Source Control events.
            versionControl.NonFatalError += OnNonFatalError;

            IEnumerable<Changeset> history = QueryHistory(versionControl);
            switch (Format)
            {
                case HistoryFormat.Brief:
                    WriteBrief(versionControl, history);
                    break;
                case HistoryFormat.Detailed:
                    WriteDetailed(versionControl, history);
                    break;
                case HistoryFormat.Csv:
                    WriteCsv(versionControl, history);
                    break;
                case HistoryFormat.Md:
                    WriteMd(versionControl, history);
                    break;
                default:
                    break;
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

        private void WriteBrief(VersionControlServer versionControl, IEnumerable<Changeset> history)
        {
            if (!Silent)
            {
                Console.WriteLine("Changeset Change                     User              Date       Comment");
                Console.WriteLine("--------- -------------------------- ----------------- ---------- --------");
            }
            foreach (Changeset item in history)
            {
                //Console.WriteLine("C{0}:{1}, {2} ({3}): {4}", item.ChangesetId, item.CreationDate,
                //    item.Committer, item.CommitterDisplayName,
                //    item.Comment);
                ChangeType changes = versionControl.GetChangesForChangeset(item.ChangesetId, false, Int32.MaxValue, null)
                        .Select(o => o.ChangeType).Aggregate((x, y) => x | y);
                Console.WriteLine("{0,-9:0} {1,-26} {2,-17} {3,10:d} {4}",
                    item.ChangesetId,
                    changes,
                    item.CommitterDisplayName,
                    item.CreationDate,
                    item.Comment);
            }
        }

        private void WriteDetailed(VersionControlServer versionControl, IEnumerable<Changeset> history)
        {
            int i = 0;
            foreach (Changeset item in history)
            {
                if (i > 0)
                {
                    Console.WriteLine();
                    Console.WriteLine("-------------------------------------------------------------------------------");
                }
                i++;
                Console.WriteLine("Changeset: {0}", item.ChangesetId);
                Console.WriteLine("User: {0} ({1})", item.OwnerDisplayName, item.Owner);
                Console.WriteLine("Date: {0:G}", item.CreationDate);
                Console.WriteLine();
                Console.WriteLine("Comment:");
                Console.WriteLine("  {0}", item.Comment);
                var changes = versionControl.GetChangesForChangeset(item.ChangesetId, false, Int32.MaxValue, null);
                if (changes.Any())
                {
                    Console.WriteLine();
                    Console.WriteLine("Items:");
                    foreach (var change in changes)
                    {
                        Console.WriteLine("  {0,-26} {1}", change.ChangeType, change.Item.ServerItem);
                    }
                }
                if (item.CheckinNote != null && item.CheckinNote.Values != null && item.CheckinNote.Values.Length > 0)
                {
                    Console.WriteLine();
                    Console.WriteLine("Check-in Notes:");
                    foreach (var note in item.CheckinNote.Values)
                    {
                        Console.WriteLine("  {0}:", note.Name);
                        Console.WriteLine("    {0}", note.Value);
                    }
                }
                if (item.PolicyOverride != null && item.PolicyOverride.PolicyFailures != null && item.PolicyOverride.PolicyFailures.Length > 0)
                {
                    Console.WriteLine();
                    Console.WriteLine("Policy Warnings:");
                    Console.WriteLine("  Override Reason:");
                    Console.WriteLine("    {0}:", item.PolicyOverride.Comment);
                    foreach (var fail in item.PolicyOverride.PolicyFailures)
                    {
                        Console.WriteLine("  Messages:");
                        //Console.WriteLine("  {0}:", fail.PolicyName);
                        Console.WriteLine("    {0}", fail.Message);
                    }
                }
                if (item.AssociatedWorkItems != null && item.AssociatedWorkItems.Length > 0)
                {
                    Console.WriteLine();
                    Console.WriteLine("Associated Work Items:");
                    foreach (var wk in item.AssociatedWorkItems)
                    {
                        Console.WriteLine("  {0}: {1}", wk.Id, wk.Title);
                    }
                }
            }
        }

        private void WriteCsv(VersionControlServer versionControl, IEnumerable<Changeset> history)
        {
            string separator = System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ListSeparator;
            if (!Silent)
            {
                Console.WriteLine("Changeset{0}Change{0}User{0}\"User display name\"{0}\"Date/time\"{0}Comment", separator);
            }
            foreach (Changeset item in history)
            {
                //Console.WriteLine("C{0}:{1}, {2} ({3}): {4}", item.ChangesetId, item.CreationDate,
                //    item.Committer, item.CommitterDisplayName,
                //    item.Comment);
                ChangeType changes = versionControl.GetChangesForChangeset(item.ChangesetId, false, Int32.MaxValue, null)
                        .Select(o => o.ChangeType).Aggregate((x, y) => x | y);
                Console.WriteLine("{1}{0}\"{2}\"{0}\"{3}\"{0}\"{4}\"{0}\"{5:G}\"{0}\"{6}\"",
                    separator,
                    item.ChangesetId,
                    changes,
                    item.Committer.Replace("\"", "\"\""),
                    item.CommitterDisplayName.Replace("\"", "\"\""),
                    item.CreationDate,
                    item.Comment.Replace("\"", "\"\""));
            }
        }

        private void WriteMd(VersionControlServer versionControl, IEnumerable<Changeset> history)
        {
            if (!Silent)
            {
                Console.WriteLine("#Changelog");
            }
            foreach (Changeset item in history)
            {
                if (item.AssociatedWorkItems != null && item.AssociatedWorkItems.Length > 0)
                {
                    foreach (var wk in item.AssociatedWorkItems)
                    {
                        Console.WriteLine("* **#{0}** {1}", wk.Id, wk.Title);
                    }
                }
                else
                {
                    if (UseComments)
                        Console.WriteLine("* {0}", item.Comment);
                }
            }
        }

        [Usage]
        public static IEnumerable<Example> Examples
        {
            get
            {
                yield return new Example("Show history in Brief format",
                    new TfsHistoryTask
                    {
                        ServerUrl = @"https://myvso.visualstudio.com/DefaultCollection",
                        ServerProjectPath = @"$/MyProject/ProjectSubfolder",
                        TargetPath = @"""c:\Projects Folder""",
                        Login = @"user@mycompany.com,p@ssw0rd"
                    });
            }
        }

    }
}
