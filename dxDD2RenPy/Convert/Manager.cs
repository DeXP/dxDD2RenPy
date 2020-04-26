using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace dxDD2RenPy.Convert
{
	public class Manager
	{
		private HashSet<string> m_ProcessedFiles = new HashSet<string>();
		private MainWindow m_LogWindow;

		public Manager(MainWindow window)
		{
			m_LogWindow = window;
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

			m_LogWindow.AppendLogLine($"Start {Path.GetFileNameWithoutExtension(path)}");

			try
			{
				if (0 == conv.Load(path))
				{
					int charsCount = conv.Convert();

					m_LogWindow.AppendLogLine($"\"{conv.Filename}\" finished. Size: {charsCount}");
				}
				else
				{
					m_LogWindow.AppendLogLine($"Unable to load file: {path}");
				}
			}
			catch (System.Exception ex)
			{
				m_LogWindow.AppendLogLine($"Convertation failed: {ex.Message}");
			}
		}
	}
}
