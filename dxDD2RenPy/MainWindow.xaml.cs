﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using dxDD2RenPy.Convert;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace dxDD2RenPy
{
	public class MainWindow : Window
	{
		private TextBox m_InputFileEdit;
		private TextBox m_Log;

		public MainWindow()
		{
			InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
		}

		private void InitializeComponent()
		{
			AvaloniaXamlLoader.Load(this);

			m_InputFileEdit = this.FindControl<TextBox>("inputFile");
			m_Log = this.FindControl<TextBox>("LogText");

			string version = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
			this.Title += " " + version;		
		}

		public async void BrowseButton_Click(object sender, RoutedEventArgs e)
		{
			var dialog = new OpenFileDialog();
			dialog.Filters.Add(new FileDialogFilter() { Name = "Json", Extensions = { "json" } });
			var result = await dialog.ShowAsync(new Window());

			if ((result != null) && (result.Length > 0))
			{
				m_InputFileEdit.Text = result.First();
			}
		}

		public void AppendLogLine(string text)
		{
			m_Log.Text += System.DateTime.Now.ToString("T") + ": " + text + System.Environment.NewLine;

			m_Log.CaretIndex = int.MaxValue;
		}

		public void ConvertButton_Click(object sender, RoutedEventArgs e)
		{
			if (false == File.Exists(m_InputFileEdit.Text))
			{
				AppendLogLine($"Sorry, file {m_InputFileEdit.Text} not exists");
				return;
			}

			AppendLogLine($"Entry point: {m_InputFileEdit.Text}");
			new Manager(this).ProcessFile(m_InputFileEdit.Text);
		}
	}
}