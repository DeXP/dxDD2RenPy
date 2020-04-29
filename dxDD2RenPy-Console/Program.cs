using System;
using System.IO;
using System.Reflection;
using dxDD2RenPy.Convert;
using dxDD2RenPy.Helpers;

namespace dxDD2RenPy_Console
{
	class Program
	{
		class ConsoleLogger: ILogLiner
		{
			public void AppendLogLine(string text)
			{
				Console.WriteLine(DateTime.Now.ToString("T") + ": " + text);
			}
		}

		static void Main(string[] args)
		{
			Console.WriteLine("dxDD2RenPy-Console {0}", Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion);

			if (args.Length < 1)
			{
				Console.WriteLine("Console version of dxDD2RenPy converter - the tool to convert Dialogue Designer JSON files into Ren'Py code.");
				Console.WriteLine("Arguments:");
				Console.WriteLine("  [-m] - Do file system monitoring (actual only for folder)");
				Console.WriteLine("  path - Path to the file or folder to convert");
			}
			else
			{
				bool startWatcher = false;

				using (var manager = new Manager(new ConsoleLogger()))
				{
					foreach (var arg in args)
					{
						if (arg.StartsWith("-m"))
						{
							startWatcher = true;
						}
						else
						{
							string path = arg;

							if (File.Exists(path))
							{
								manager.ProcessFile(path);
							}
							else
							{
								manager.ProcessAll(path, startWatcher);

								if (true == startWatcher)
								{
									Console.WriteLine("Press 'q' to quit the application.");
									while (Console.Read() != 'q') ;
								}
							}
						}
					}
				}
			}
		}
		
	}
}
