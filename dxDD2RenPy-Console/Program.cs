using System;
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
				Console.WriteLine("  Path to the file to convert");
			}
			else
			{
				new Manager(new ConsoleLogger()).ProcessAll(args[0]);
			}
		}

		
	}
}
