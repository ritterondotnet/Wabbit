﻿// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

using Revsoft.Wabbitcode.AvalonEditExtension.Snippets;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit;


namespace Revsoft.Wabbitcode.AvalonEditExtension
{
	/// <summary>
	/// The text editor used in SharpDevelop.
	/// Serves both as base class for CodeEditorView and as text editor control
	/// for editors used in other parts of SharpDevelop (e.g. all ConsolePad-based controls)
	/// </summary>
	public class WabbitcodeTextEditor : TextEditor
	{
		static WabbitcodeTextEditor()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(WabbitcodeTextEditor),
			                                         new FrameworkPropertyMetadata(typeof(WabbitcodeTextEditor)));
		}
				
		public WabbitcodeTextEditor()
		{			
			this.CommandBindings.Add(new CommandBinding(ApplicationCommands.Print, OnPrint));
			this.CommandBindings.Add(new CommandBinding(ApplicationCommands.PrintPreview, OnPrintPreview));
		}
		
		protected virtual string FileName {
			get { return "untitled"; }
		}
		
		WabbitcodeCompletionWindow completionWindow;
		WabbitcodeInsightWindow insightWindow;
		
		void CloseExistingCompletionWindow()
		{
			if (completionWindow != null) {
				completionWindow.Close();
			}
		}
		
		void CloseExistingInsightWindow()
		{
			if (insightWindow != null) {
				insightWindow.Close();
			}
		}
		
		public WabbitcodeCompletionWindow ActiveCompletionWindow {
			get { return completionWindow; }
		}
		
		public WabbitcodeInsightWindow ActiveInsightWindow {
			get { return insightWindow; }
		}
		
		internal void ShowCompletionWindow(WabbitcodeCompletionWindow window)
		{
			CloseExistingCompletionWindow();
			completionWindow = window;
			window.Closed += delegate {
				completionWindow = null;
			};
			Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(
				delegate {
					if (completionWindow == window) {
						window.Show();
					}
				}
			));
		}
		
		internal void ShowInsightWindow(WabbitcodeInsightWindow window)
		{
			CloseExistingInsightWindow();
			insightWindow = window;
			window.Closed += delegate {
				insightWindow = null;
			};
			Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(
				delegate {
					if (insightWindow == window) {
						window.Show();
					}
				}
			));
		}
		
		#region Printing
		void OnPrint(object sender, ExecutedRoutedEventArgs e)
		{
			PrintDialog printDialog = PrintPreviewViewContent.PrintDialog;
			if (printDialog.ShowDialog() == true) {
				FlowDocument fd = DocumentPrinter.CreateFlowDocumentForEditor(this);
				fd.ColumnGap = 0;
				fd.ColumnWidth = printDialog.PrintableAreaWidth;
				fd.PageHeight = printDialog.PrintableAreaHeight;
				fd.PageWidth = printDialog.PrintableAreaWidth;
				IDocumentPaginatorSource doc = fd;
				printDialog.PrintDocument(doc.DocumentPaginator, Path.GetFileName(this.FileName));
			}
		}
		
		void OnPrintPreview(object sender, ExecutedRoutedEventArgs e)
		{
			PrintDialog printDialog = PrintPreviewViewContent.PrintDialog;
			FlowDocument fd = DocumentPrinter.CreateFlowDocumentForEditor(this);
			fd.ColumnGap = 0;
			fd.ColumnWidth = printDialog.PrintableAreaWidth;
			fd.PageHeight = printDialog.PrintableAreaHeight;
			fd.PageWidth = printDialog.PrintableAreaWidth;
			PrintPreviewViewContent.ShowDocument(fd, Path.GetFileName(this.FileName));
		}
		#endregion
	}
	
	sealed class ZoomLevelToTextFormattingModeConverter : IValueConverter
	{
		public static readonly ZoomLevelToTextFormattingModeConverter Instance = new ZoomLevelToTextFormattingModeConverter();
		
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (((double)value) == 1.0)
				return TextFormattingMode.Display;
			else
				return TextFormattingMode.Ideal;
		}
		
		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}
