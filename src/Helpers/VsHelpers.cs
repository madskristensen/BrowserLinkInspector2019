using EnvDTE;

using EnvDTE80;

using Microsoft;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace BrowserLinkInspector
{
	internal static class VsHelpers
	{
		public static DTE2 DTE => ServiceProvider.GlobalProvider.GetService(typeof(DTE)) as DTE2;

		public static IWpfTextView GetCurentTextView()
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			IComponentModel componentModel = GetComponentModel();
			if (componentModel == null)
			{
				return null;
			}

			IVsEditorAdaptersFactoryService editorAdapter = componentModel.GetService<IVsEditorAdaptersFactoryService>();

			return editorAdapter.GetWpfTextView(GetCurrentNativeTextView());
		}

		public static IVsTextView GetCurrentNativeTextView()
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			IVsTextManager textManager = (IVsTextManager)ServiceProvider.GlobalProvider.GetService(typeof(SVsTextManager));
			Assumes.Present(textManager);

			ErrorHandler.ThrowOnFailure(textManager.GetActiveView(1, null, out IVsTextView activeView));
			return activeView;
		}

		public static IComponentModel GetComponentModel()
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			return (IComponentModel)ServiceProvider.GlobalProvider.GetService(typeof(SComponentModel));
		}

	}
}
