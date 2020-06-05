using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Web.BrowserLink;
using Microsoft.WebTools.Languages.Html.Editor.Document;
using Microsoft.WebTools.Languages.Html.Tree.Nodes;

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.RegularExpressions;

namespace BrowserLinkInspector
{
	[Export(typeof(IBrowserLinkExtensionFactory))]
	public class DesignModeFactory : IBrowserLinkExtensionFactory
	{
		public BrowserLinkExtension CreateExtensionInstance(BrowserLinkConnection connection)
		{
			return new DesignMode();
		}

		public string GetScript()
		{
			using (Stream stream = GetType().Assembly.GetManifestResourceStream("BrowserLinkInspector.BrowserLink.DesignMode.DesignModeBrowserLink.js"))
			using (StreamReader reader = new StreamReader(stream))
			{
				return reader.ReadToEnd();
			}
		}
	}

	public class DesignMode : BrowserLinkExtension
	{
		private BrowserLinkConnection _connection;

		public override IEnumerable<BrowserLinkAction> Actions
		{
			get { yield return new BrowserLinkAction("Design Mode", SetDesignMode); }
		}

		public override void OnConnected(BrowserLinkConnection connection)
		{
			_connection = connection;
		}

		private void SetDesignMode(BrowserLinkAction action)
		{
			Browsers.Client(_connection).Invoke("setDesignMode");
		}

		[SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
		[BrowserLinkCallback]
		public void UpdateSource(string innerHtml, string file, int position)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			VsHelpers.DTE.ItemOperations.OpenFile(file);

			ThreadHelper.JoinableTaskFactory.Run(async () =>
			{
				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
				Microsoft.VisualStudio.Text.Editor.IWpfTextView view = VsHelpers.GetCurentTextView();
				HtmlEditorDocument html = HtmlEditorDocument.TryFromTextView(view);

				if (html == null)
				{
					return;
				}

				view.Selection.Clear();
				html.HtmlEditorTree.GetPositionElement(position + 1, out ElementNode element, out AttributeNode attribute);

				// HTML element
				if (element != null && element.Start == position)
				{
					Span span = new Span(element.InnerRange.Start, element.InnerRange.Length);
					string text = html.TextBuffer.CurrentSnapshot.GetText(span);

					if (text != innerHtml)
					{
						UpdateBuffer(innerHtml, html, span);
					}
				}
				// ActionLink
				else if (element.Start != position)
				{
					//@Html.ActionLink("Application name", "Index", "Home", null, new { @class = "brand" })
					Span span = new Span(position, 100);

					if (position + 100 < html.TextBuffer.CurrentSnapshot.Length)
					{
						string text = html.TextBuffer.CurrentSnapshot.GetText(span);
						string result = Regex.Replace(text, @"^html.actionlink\(""([^""]+)""", "Html.ActionLink(\"" + innerHtml + "\"", RegexOptions.IgnoreCase);

						UpdateBuffer(result, html, span);
					}
				}

			});
		}

		private static void UpdateBuffer(string innerHTML, HtmlEditorDocument html, Span span)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			VsHelpers.DTE.UndoContext.Open("Design Mode changes");

			try
			{
				html.TextBuffer.Replace(span, innerHTML);
				VsHelpers.DTE.ActiveDocument.Save();
			}
			catch
			{
				// Do nothing
			}
			finally
			{
				VsHelpers.DTE.UndoContext.Close();
			}
		}

		[SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
		[BrowserLinkCallback]
		public void Undo()
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			try
			{
				VsHelpers.DTE.ExecuteCommand("Edit.Undo");
				VsHelpers.DTE.ActiveDocument.Save();
			}
			catch
			{
				// Do nothing
			}
		}

		[SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
		[BrowserLinkCallback]
		public void Redo()
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			try
			{
				VsHelpers.DTE.ExecuteCommand("Edit.Redo");
				VsHelpers.DTE.ActiveDocument.Save();
			}
			catch
			{
				// Do nothing
			}
		}

		[SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
		[BrowserLinkCallback]
		public void Save()
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			if (VsHelpers.DTE.ActiveDocument != null && !VsHelpers.DTE.ActiveDocument.Saved)
			{
				VsHelpers.DTE.ActiveDocument.Save();
			}
		}
	}
}