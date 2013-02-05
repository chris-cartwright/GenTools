using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

				ProcessStartInfo git = new ProcessStartInfo("git", "rev-parse --short HEAD");
				git.RedirectStandardOutput = true;
				git.UseShellExecute = false;
				git.CreateNoWindow = true;

				Process proc = new Process();
				proc.StartInfo = git;
				proc.Start();
				proc.WaitForExit();

				string rev = proc.StandardOutput.ReadToEnd().Trim();
				StreamWriter sw = new StreamWriter("git.cs", false);
				sw.WriteLine(String.Format("[assembly: GitRevision(\"{0}\")]", rev));
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
