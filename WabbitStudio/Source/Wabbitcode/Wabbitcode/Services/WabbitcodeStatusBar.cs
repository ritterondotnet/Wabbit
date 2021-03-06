﻿using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shell;

namespace Revsoft.Wabbitcode.Services
{
	public class WabbitcodeStatusBar : StatusBar
	{
		StatusBarItem statusProgressBarItem = new StatusBarItem();
		ProgressBar statusProgressBar = new ProgressBar();
		StatusBarItem jobNamePanel = new StatusBarItem();

		StatusBarItem txtStatusBarPanel = new StatusBarItem();
		StatusBarItem cursorStatusBarPanel = new StatusBarItem();
		StatusBarItem modeStatusBarPanel = new StatusBarItem();

		public IDockingService DockingService { get; set; }

		public StatusBarItem CursorStatusBarPanel
		{
			get { return cursorStatusBarPanel; }
		}

		public StatusBarItem ModeStatusBarPanel
		{
			get  { return modeStatusBarPanel; }
		}

		public WabbitcodeStatusBar()
		{
			cursorStatusBarPanel.Width = 150;
			modeStatusBarPanel.Width = 25;

			statusProgressBar.Minimum = 0;
			statusProgressBar.Maximum = 1;

			statusProgressBarItem.Visibility = Visibility.Hidden;
			statusProgressBarItem.Width = 100;
			statusProgressBarItem.Content = statusProgressBar;
			statusProgressBarItem.VerticalContentAlignment = VerticalAlignment.Stretch;
			statusProgressBarItem.HorizontalContentAlignment = HorizontalAlignment.Stretch;

			DockPanel.SetDock(modeStatusBarPanel, Dock.Right);
			DockPanel.SetDock(cursorStatusBarPanel, Dock.Right);
			DockPanel.SetDock(statusProgressBarItem, Dock.Right);
			DockPanel.SetDock(jobNamePanel, Dock.Right);

			Items.Add(modeStatusBarPanel);
			Items.Add(cursorStatusBarPanel);
			Items.Add(statusProgressBarItem);
			Items.Add(jobNamePanel);
			Items.Add(txtStatusBarPanel);
		}

		public void SetMessage(string message, bool highlighted)
		{
			Action setMessageAction = delegate
			{
				if (highlighted)
				{
					txtStatusBarPanel.Background = SystemColors.HighlightBrush;
					txtStatusBarPanel.Foreground = SystemColors.HighlightTextBrush;
				}
				else
				{
					txtStatusBarPanel.Background = SystemColors.ControlBrush;
					txtStatusBarPanel.Foreground = SystemColors.ControlTextBrush;
				}
				txtStatusBarPanel.Content = message;
			};
			if (this.Dispatcher.InvokeRequired())
				this.Dispatcher.Invoke(setMessageAction);
			else
				setMessageAction();
		}

		// Displaying progress

		bool statusProgressBarIsVisible;
		string currentTaskName;
		OperationStatus currentStatus;
		SolidColorBrush progressForegroundBrush;

		public void DisplayProgress(string taskName, double workDone, OperationStatus status)
		{
			if (!statusProgressBarIsVisible)
			{
				statusProgressBarItem.Visibility = Visibility.Visible;
				statusProgressBarIsVisible = true;
				StopHideProgress();
			}

			TaskbarItemProgressState taskbarProgressState;
			if (double.IsNaN(workDone))
			{
				statusProgressBar.IsIndeterminate = true;
				status = OperationStatus.Normal; // indeterminate doesn't support foreground color
				taskbarProgressState = TaskbarItemProgressState.Indeterminate;
			}
			else
			{
				statusProgressBar.IsIndeterminate = false;
				statusProgressBar.Value = workDone;

				if (status == OperationStatus.Error)
					taskbarProgressState = TaskbarItemProgressState.Error;
				else
					taskbarProgressState = TaskbarItemProgressState.Normal;
			}

			TaskbarItemInfo taskbar = DockingService.MainWindow.TaskbarItemInfo;
			if (taskbar != null)
			{
				taskbar.ProgressState = taskbarProgressState;
				taskbar.ProgressValue = workDone;
			}

			if (status != currentStatus)
			{
				if (progressForegroundBrush == null)
				{
					SolidColorBrush defaultForeground = statusProgressBar.Foreground as SolidColorBrush;
					progressForegroundBrush = new SolidColorBrush(defaultForeground != null ? defaultForeground.Color : Colors.Blue);
				}

				if (status == OperationStatus.Error)
				{
					statusProgressBar.Foreground = progressForegroundBrush;
					progressForegroundBrush.BeginAnimation(SolidColorBrush.ColorProperty, new ColorAnimation(
						Colors.Red, new Duration(TimeSpan.FromSeconds(0.2)), FillBehavior.HoldEnd));
				}
				else if (status == OperationStatus.Warning)
				{
					statusProgressBar.Foreground = progressForegroundBrush;
					progressForegroundBrush.BeginAnimation(SolidColorBrush.ColorProperty, new ColorAnimation(
						Colors.YellowGreen, new Duration(TimeSpan.FromSeconds(0.2)), FillBehavior.HoldEnd));
				}
				else
				{
					statusProgressBar.ClearValue(ProgressBar.ForegroundProperty);
					progressForegroundBrush = null;
				}
				currentStatus = status;
			}

			if (currentTaskName != taskName)
			{
				currentTaskName = taskName;
				jobNamePanel.Content = taskName;
			}
		}

		public void HideProgress()
		{
			statusProgressBarIsVisible = false;
			// to allow the user to see the red progress bar as a visual clue of a failed 
			// build even if it occurs close to the end of the build, we'll hide the progress bar
			// with a bit of time delay
			System.Threading.Thread.Sleep(TimeSpan.FromMilliseconds(currentStatus == OperationStatus.Error ? 500 : 150));
			this.Dispatcher.Invoke(new Action(DoHideProgress));
		}

		void DoHideProgress()
		{
			if (!statusProgressBarIsVisible)
			{
				// make stuff look nice and delay it a little more by using an animation
				// on the progress bar
				TimeSpan timeSpan = TimeSpan.FromSeconds(0.25);
				var animation = new DoubleAnimation(0, new Duration(timeSpan), FillBehavior.HoldEnd);
				statusProgressBarItem.BeginAnimation(OpacityProperty, animation);
				jobNamePanel.BeginAnimation(OpacityProperty, animation);
				
				Action method = delegate
				{
					if (!statusProgressBarIsVisible)
					{
						statusProgressBarItem.Visibility = Visibility.Collapsed;
						jobNamePanel.Content = currentTaskName = "";
						var taskbar = DockingService.MainWindow.TaskbarItemInfo;
						if (taskbar != null)
							taskbar.ProgressState = TaskbarItemProgressState.None;
						StopHideProgress();
					}
				};
				System.Threading.Thread.Sleep(timeSpan);
				this.Dispatcher.Invoke(method);
			}
		}

		void StopHideProgress()
		{
			statusProgressBarItem.BeginAnimation(OpacityProperty, null);
			jobNamePanel.BeginAnimation(OpacityProperty, null);
		}
	}
	/// <summary>
	/// Represents the status of a operation with progress monitor.
	/// </summary>
	public enum OperationStatus : byte
	{
		/// <summary>
		/// Everything is normal.
		/// </summary>
		Normal,
		/// <summary>
		/// There was at least one warning.
		/// </summary>
		Warning,
		/// <summary>
		/// There was at least one error.
		/// </summary>
		Error
	}
}