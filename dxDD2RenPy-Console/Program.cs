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
			var assembly = Assembly.GetEntryAssembly();

			Console.WriteLine("{0} {1}", assembly.GetName().Name, assembly.GetName().Version);

			if (args.Length < 1)
			{
				Console.WriteLine(assembly.GetCustomAttribute<AssemblyDescriptionAttribute>().Description);
				Console.WriteLine("Arguments:");
				Console.WriteLine("  [-m] - Do file system monitoring (actual only for folder)");
				Console.WriteLine("  path - Path to the file or folder to convert");
				Console.WriteLine("More info on: {0}", assembly.GetCustomAttribute<AssemblyCopyrightAttribute>().Copyright);
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
								manager.StartFileProcess(path);
							}
							else
							{
								manager.StartFolderProcess(path, startWatcher);

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
