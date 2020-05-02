﻿using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using dxDD2RenPy.Helpers;

namespace dxDD2RenPy.Convert
{
	public class Manager: IDisposable
	{
		private ILogLiner m_Log;
		private HashSet<string> m_ProcessedFiles = new HashSet<string>();
		private HashSet<string> m_ProcessedCharacters = new HashSet<string>();
		private Dictionary<string, DDVariable> m_ProcessedVariables = new Dictionary<string, DDVariable>();
		private bool m_IsVariablesChanged = false;
		private string m_RootPath = string.Empty;

		private FileSystemWatcher m_FSWatcher;

		public Manager(ILogLiner logger)
		{
			m_Log = logger;

			m_FSWatcher = new FileSystemWatcher();
		}

		public int StartFolderProcess(string path, bool startWatcher = true)
		{
			if (path.EndsWith(Path.DirectorySeparatorChar))
			{
				path = Path.GetDirectoryName(path);
			}

			m_RootPath = path;

			ProcessFolder(path);
			WriteVariables(path);

			if (true == startWatcher)
			{
				m_FSWatcher.Path = path;
				m_FSWatcher.Filter = "*.json";
				m_FSWatcher.IncludeSubdirectories = true;

				m_FSWatcher.NotifyFilter = NotifyFilters.LastWrite
								 | NotifyFilters.FileName
								 | NotifyFilters.DirectoryName;

				m_FSWatcher.Changed += OnChanged;
				m_FSWatcher.Created += OnChanged;
				m_FSWatcher.Deleted += OnDeleted;
				m_FSWatcher.Renamed += OnRenamed;

				m_Log.AppendLogLine("Your changes are now watched in real time");
				m_FSWatcher.EnableRaisingEvents = true;

				return 1;
			}

			return 0;
		}

		public void StopWatcher()
		{
			m_FSWatcher.EnableRaisingEvents = false;

			m_ProcessedFiles.Clear();
			m_ProcessedCharacters.Clear();
			m_ProcessedVariables.Clear();

			m_Log.AppendLogLine("File system watcher is stopped");
		}

		private bool IsGoodLocation(string path)
		{
			string topFolder = Path.GetFileName(Path.GetDirectoryName(path));
			return (false == topFolder.Equals("saves"));
		}

		private DateTime m_LastChangedTime = DateTime.MinValue;
		private TimeSpan m_MinChangeDelta = TimeSpan.FromMilliseconds(500);

		private void OnChanged(object source, FileSystemEventArgs e)
		{	
			if (IsGoodLocation(e.FullPath))
			{
				DateTime lastWriteTime = File.GetLastWriteTime(e.FullPath);

				if (lastWriteTime - m_LastChangedTime > m_MinChangeDelta)
				{
					ConvertFile(e.FullPath);
					WriteVariables(e.FullPath);
					m_LastChangedTime = lastWriteTime;
				}
			}
		}

		private void OnRenamed(object source, RenamedEventArgs e)
		{
			if (IsGoodLocation(e.OldFullPath))
			{
				DeleteRpyFile(e.OldFullPath);
			}

			OnChanged(source, e);
		}

		private void OnDeleted(object source, FileSystemEventArgs e)
		{
			if (IsGoodLocation(e.FullPath))
			{
				DeleteRpyFile(e.FullPath);
			}
		}

		private void DeleteRpyFile(string jsonPath)
		{
			string path = Converter.GetRpyName(jsonPath);

			m_Log.AppendLogLine($"Deleting {Path.GetFileName(path)}");

			if (File.Exists(path))
			{
				File.Delete(path);
			}
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

			m_IsVariablesChanged = true;
			m_ProcessedVariables[name] = value;
		}

		public void ProcessFolder(string path)
		{
			var jsonFiles = Directory.GetFiles(path, "*.json", SearchOption.AllDirectories);

			foreach(var file in jsonFiles)
			{
				if (IsGoodLocation(file))
				{
					ProcessFile(file);
				}
			}
		}

		public void StartFileProcess(string path)
		{
			m_RootPath = Path.GetDirectoryName(path);
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
			catch (Exception ex)
			{
				m_Log.AppendLogLine($"Convertation failed: {ex.Message}");
			}
		}

		private void WriteVariables(string bornPath)
		{
			if ((m_ProcessedVariables.Keys.Count < 1) || (false == m_IsVariablesChanged))
			{
				return;
			}

			m_IsVariablesChanged = false;

			string path = m_RootPath + Path.DirectorySeparatorChar + "all-game-variables.rpy";

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

		public void Dispose()
		{
			m_FSWatcher.Dispose();
		}
	}
}
