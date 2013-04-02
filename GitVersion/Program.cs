using System;
using System.Diagnostics;
using System.IO;

namespace GitVersion
{
	class Program
	{
		static int Main(string[] args)
		{
			try
			{
				string pwd = args[0].Trim('"');
				Directory.SetCurrentDirectory(pwd);

				string rev = "unknown";
				bool dirty = true;
				try
				{
					ProcessStartInfo git = new ProcessStartInfo("git", "rev-parse --short HEAD")
					{
						RedirectStandardOutput = true,
						UseShellExecute = false,
						CreateNoWindow = true
					};

					Process proc = new Process() { StartInfo = git };
					proc.Start();
					proc.WaitForExit();

					rev = proc.StandardOutput.ReadToEnd().Trim();

					git = new ProcessStartInfo("git", "status --short")
					{
						RedirectStandardOutput = true,
						UseShellExecute = false,
						CreateNoWindow = true
					};

					proc = new Process() { StartInfo = git };
					proc.Start();
					proc.WaitForExit();

					dirty = proc.StandardOutput.ReadLine() != null;
				}
				catch (Exception)
				{
					Console.WriteLine("Could not launch git.");
				}

				StreamWriter sw = new StreamWriter("git.cs", false);
				sw.WriteLine("using Common;\n");
				sw.WriteLine("[assembly: GitRevision(\"{0}\", {1})]", rev, dirty.ToString().ToLower());
				sw.Close();

				return 0;
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine(ex);
				return 1;
			}
		}
	}
}
