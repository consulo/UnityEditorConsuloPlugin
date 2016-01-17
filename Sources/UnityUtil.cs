using System;
using UnityEditor;

namespace MustBe.Consulo.Internal
{
	internal class UnityUtil
	{
		internal delegate void VoidFunc();

		internal static void RunInMainThread(VoidFunc action)
		{
			EditorApplication.CallbackFunction callback = null;
			callback = () =>
			{
				EditorApplication.update = (EditorApplication.CallbackFunction) Delegate.Remove(EditorApplication.update, callback);
				action();
			};
			EditorApplication.update = (EditorApplication.CallbackFunction)Delegate.Combine(EditorApplication.update, callback);
		}
	}
}
