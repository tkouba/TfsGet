using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.VisualStudio.Services.Common;
using CommandLine;

namespace TfsGet
{
    class Program
    {
        static int Main(string[] args)
        {
            using (Parser parser = new Parser((o) =>
                {
                    o.HelpWriter = Console.Error;
                    o.CaseInsensitiveEnumValues = true;
                    o.CaseSensitive = false;
                }))
            {
                return parser.ParseArguments<TfsCopyTask, TfsHistoryTask>(args)
                    .MapResult(
                      (TfsCopyTask opts) => opts.Execute(),
                      (TfsHistoryTask opts) => opts.Execute(),
                      //(CommitOptions opts) => RunCommitAndReturnExitCode(opts),
                      //(CloneOptions opts) => RunCloneAndReturnExitCode(opts),
                      errs => 1);
            }
        }

    }
}
