using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace CabArcEmu
{
    internal class Program
    {
        static int Main(string[] args)
        {
            if (args.Length < 3 || args[0] != "N") return 1;

            var cabFile = args[1];

            // http://kiokunohokanko.blogspot.com/2015/07/windowsmakecab-cab-microsoft-makecab.html
            var ddf = new StringBuilder($@".Set Compress=on
.Set CabinetFileCountThreshold=0
.Set FolderFileCountThreshold=0
.Set FolderSizeThreshold=0
.Set MaxCabinetSize=0
.Set MaxDiskFileCount=0
.Set MaxDiskSize=0
.Set DiskDirectoryTemplate={Path.GetDirectoryName(cabFile)}
.Set CabinetNameTemplate={Path.GetFileName(cabFile)}
.Set InfFileName=NUL
.Set RptFileName=NUL
");

            var arguments = args;
            int start = 2, step = 1;

            if (args.Length == 3 && args[2].EndsWith("\\*"))
            {
                // all files in folder
                arguments = Directory.GetFiles(Path.GetDirectoryName(args[2]), "*");
                start = 0;
            }
            else
            {
                var r = $@"'{cabFile.Replace(@"\", @"\\")}'\s'('.*)\s'$".Replace("'", "\"");
                var m = new Regex(r).Match(Environment.CommandLine);
                if (m.Success && m.Groups.Count == 2)
                {
                    // multi files
                    arguments = m.Groups[1].Value.Split('"');
                    start = 1; step = 2;
                }
            }

            for (int i = start; i < arguments.Length; i += step)
            {
                ddf.AppendLine(arguments[i]);
            }

            var ddfFile = Path.GetTempFileName();
            int rc = -1;
            try
            {
                File.WriteAllText(ddfFile, ddf.ToString(), Encoding.Default);

                var pi = new ProcessStartInfo()
                {
                    FileName = "makecab",
                    Arguments = $"/f {ddfFile}",
                    UseShellExecute = false
                };

                var p = Process.Start(pi);
                p.WaitForExit();
                rc = p.ExitCode;
                if (rc == 0)
                {
                    //XXX - DO NOT DELETE and MODIFY
                    Console.WriteLine("Completed successfully");
                }
            }
            finally
            {
#if !DEBUG
                File.Delete(ddfFile);
#endif
            }

            return rc;
        }
    }
}
