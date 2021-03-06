﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;
using dxDD2RenPy.Convert;
using dxDD2RenPy.Helpers;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace dxDD2RenPy
{
	public class MainWindow : Window, ILogLiner
	{
		private TextBox m_InputFolderEdit;
		private TextBox m_Log;

		private Button m_BrowseButton;
		private Button m_ConvertButton;
		private Button m_StopButton;

		private Manager m_ConvertManager;

		public MainWindow()
		{
			InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
			m_ConvertManager = new Manager(this);

			this.Closing += OnClosing;
		}

		private void OnClosing(object source, System.ComponentModel.CancelEventArgs e)
		{
			m_ConvertManager.Dispose();
		}

		private void InitializeComponent()
		{
			AvaloniaXamlLoader.Load(this);

			m_InputFolderEdit = this.FindControl<TextBox>("inputFolder");
			m_Log = this.FindControl<TextBox>("LogText");

			m_BrowseButton = this.FindControl<Button>("browseButton");
			m_ConvertButton = this.FindControl<Button>("convertButton");
			m_StopButton = this.FindControl<Button>("stopButton");

			string version = Assembly.GetEntryAssembly().GetName().Version.ToString();
			this.Title += " " + version;		
		}

		public async void BrowseButton_Click(object sender, RoutedEventArgs e)
		{
			var dialog = new OpenFolderDialog();
			var result = await dialog.ShowAsync(new Window());

			if ((result != null) && (result.Length > 0))
			{
				m_InputFolderEdit.Text = result;
			}
		}

		public void AppendLogLine(string text)
		{
			Dispatcher.UIThread.InvokeAsync(
				new System.Action(() => this.AppendLogLineSynchronous(text))
			);
		}

		public void AppendLogLineSynchronous(string text)
		{		
			m_Log.Text += System.DateTime.Now.ToString("T") + ": " + text + System.Environment.NewLine;

			m_Log.CaretIndex = int.MaxValue;
		}

		public void ConvertButton_Click(object sender, RoutedEventArgs e)
		{
			if (false == Directory.Exists(m_InputFolderEdit.Text))
			{
				AppendLogLine($"Sorry, folder {m_InputFolderEdit.Text} not exists");
				return;
			}

			AppendLogLine($"Entry point: {m_InputFolderEdit.Text}");
			if( m_ConvertManager.StartFolderProcess(m_InputFolderEdit.Text) > 0)
			{
				m_StopButton.IsEnabled = true;
				m_BrowseButton.IsEnabled = false;
				m_ConvertButton.IsEnabled = false;
			}
		}

		public void StopButton_Click(object sender, RoutedEventArgs e)
		{
			m_ConvertManager.StopWatcher();
			m_StopButton.IsEnabled = false;
			m_BrowseButton.IsEnabled = true;
			m_ConvertButton.IsEnabled = true;
		}
	}
}
