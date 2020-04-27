using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using dxDD2RenPy.Helpers;

namespace dxDD2RenPy.Convert
{
	public class Manager
	{
		private ILogLiner m_Log;
		private HashSet<string> m_ProcessedFiles = new HashSet<string>();
		private HashSet<string> m_ProcessedCharacters = new HashSet<string>();
		private Dictionary<string, DDVariable> m_ProcessedVariables = new Dictionary<string, DDVariable>();

		public Manager(ILogLiner logger)
		{
			m_Log = logger;
		}

		public void ProcessAll(string path)
		{
			ProcessFile(path);

			WriteVariables(path);
		}

		public void ProcessCharacter(string name)
		{
			if (m_ProcessedCharacters.Contains(name))
			{
				return;
			}

			m_ProcessedCharacters.Add(name);
		}

		public void ProcessVariable(string name, DDVariable value)
		{
			if (m_ProcessedVariables.ContainsKey(name))
			{
				return;
			}

			m_ProcessedVariables[name] = value;
		}

		public void ProcessFile(string path)
		{
			if (m_ProcessedFiles.Contains(path))
			{
				return;
			}

			m_ProcessedFiles.Add(path);
			ConvertFile(path);
		}

		private void ConvertFile(string path)
		{
			Converter conv = new Converter(this);

			m_Log.AppendLogLine($"Start {Path.GetFileNameWithoutExtension(path)}");

			try
			{
				if (0 == conv.Load(path))
				{
					int charsCount = conv.Convert();

					m_Log.AppendLogLine($"\"{conv.Filename}\" finished. Size: {charsCount}");
				}
				else
				{
					m_Log.AppendLogLine($"Unable to load file: {path}");
				}
			}
			catch (System.Exception ex)
			{
				m_Log.AppendLogLine($"Convertation failed: {ex.Message}");
			}
		}

		private void WriteVariables(string bornPath)
		{
			if(m_ProcessedVariables.Keys.Count < 1)
			{
				return;
			}

			string path = bornPath.Replace(".json", "") + "-all-variables.rpy";

			m_Log.AppendLogLine($"Writing all variables to {Path.GetFileName(path)}");

			using (var writer = new StatStreamWriter(path))
			{
				var names = new List<string>(m_ProcessedVariables.Keys);

				names.Sort();
				writer.WriteLine("init python:");

				foreach (var varName in names)
				{
					var varValue = m_ProcessedVariables[varName];

					writer.Write($"    {varName} = ");

					switch (varValue.type)
					{
						case DDVarType.String: writer.WriteLine($"\"{varValue.value}\""); break;
						case DDVarType.Integer: writer.WriteLine($"{varValue.value}"); break;
						case DDVarType.Boolean: writer.WriteLine(string.Format("{0}", varValue.Equals("true") ? "True" : "False")); break;
					}
				}

				m_Log.AppendLogLine($"Variables finished. Size: {writer.TotalWritten}");
			}
		}
	}
}
