using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CabArcEmu
{
    public sealed class PathStringComparer : IComparer<string>
    {
        public int Compare(string a, string b)
        {
            // https://stackoverflow.com/a/39064816
            int r = a.Count(c => c == Path.DirectorySeparatorChar || c == Path.AltDirectorySeparatorChar)
                  - b.Count(c => c == Path.DirectorySeparatorChar || c == Path.AltDirectorySeparatorChar);
            if (r != 0) return r;

            return string.Compare(a, b, true);
        }
    }

    internal class Program
    {
        static int Main(string[] args)
        {
            var arguments = args.ToList();

            var isPreserve = false;
            if (arguments.Count > 0 && arguments[0] == "-p")
            {
                isPreserve = true;

                arguments.RemoveAt(0);
            }

            var option = SearchOption.TopDirectoryOnly;
            if (arguments.Count > 0 && arguments[0] == "-r")
            {
                option = SearchOption.AllDirectories;

                arguments.RemoveAt(0);
            }

            if (arguments.Count < 3 || arguments[0] != "N") return 1;

            var cabFile = arguments[1];

            // http://kiokunohokanko.blogspot.com/2015/07/windowsmakecab-cab-microsoft-makecab.html
            var ddf = new StringBuilder($@".OPTION EXPLICIT
.Set Cabinet=on
.Set Compress=on
.Set CabinetFileCountThreshold=0
.Set FolderFileCountThreshold=0
.Set FolderSizeThreshold=0
.Set MaxCabinetSize=0
.Set MaxDiskFileCount=0
.Set MaxDiskSize=0
.Set InfFileName=NUL
.Set RptFileName=NUL
.Set UniqueFiles=off
");
            ddf.AppendLine($".Set DiskDirectoryTemplate=\"{Path.GetDirectoryName(cabFile)}\"");
            ddf.AppendLine($".Set CabinetNameTemplate=\"{Path.GetFileName(cabFile)}\"");

            int start = 2, step = 1;

            if (arguments.Count == 3 && arguments[2].EndsWith("\\*"))
            {
                // all files in folder
                arguments = Directory.GetFiles(Path.GetDirectoryName(arguments[2]), "*", option).ToList();
                start = 0;
            }
            else
            {
                var r = $@"'{cabFile.Replace(@"\", @"\\")}'\s'('.*)\s'$".Replace("'", "\"");
                var m = new Regex(r).Match(Environment.CommandLine);
                if (m.Success && m.Groups.Count == 2)
                {
                    // multi files
                    arguments = m.Groups[1].Value.Split('"').ToList();
                    start = 1; step = 2;
                }
            }

            var files = new List<string>();
            for (int i = start; i < arguments.Count; i += step)
            {
                files.Add(arguments[i]);
            }
            files.Sort(new PathStringComparer());

            var destDir = "";
            foreach (var file in files)
            {
                if (isPreserve)
                {
                    var _ = GetRelativePath(Environment.CurrentDirectory, file);
                    if (destDir != _)
                    {
                        destDir = _;
                        // https://stackoverflow.com/a/39822229
                        ddf.Append(".Set DestinationDir=\"").Append(destDir).AppendLine("\"");
                    }
                }

                ddf.Append("\"").Append(file).AppendLine("\"");
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

        static string GetRelativePath(string relativeTo, string path)
        {
            var relativePath = Path.GetDirectoryName(path);
            if (relativePath.StartsWith(relativeTo, StringComparison.CurrentCultureIgnoreCase))
            {
                relativePath = relativePath.Substring(relativeTo.Length + 1);
            }
            return relativePath;
        }
    }
}
