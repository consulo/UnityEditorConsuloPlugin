/*
 * Copyright 2013-2016 consulo.io
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using UnityEditor;
using Consulo.Internal.UnityEditor;

namespace Consulo.Internal.UnityEditor
{
	internal class UnityUtil
	{
		internal static OSFamily OSFamily = FindOSFamily();

		private static OSFamily FindOSFamily()
		{
			var os = Environment.OSVersion;

			if(os.VersionString.Contains("Windows"))
			{
				return OSFamily.Windows;
			}

			// TODO [VISTALL] macOS/Linux check need?
			return OSFamily.Other;
		}

		internal static void RunInMainThread(Action action)
		{
			EditorApplication.CallbackFunction callback = null;
			callback = () =>
			{
				EditorApplication.update = (EditorApplication.CallbackFunction) Delegate.Remove(EditorApplication.update, callback);
				action();
			};
			EditorApplication.update = (EditorApplication.CallbackFunction)Delegate.Combine(EditorApplication.update, callback);
		}

		internal static bool IsDebugEnabled()
		{
#if CONSULO_ACTION_DEBUG
			return true;
#else
			return true;
#endif
		}

		internal static bool IsSocketSearchingEnabled()
		{
#if CONSULO_DISABLE_SOCKET_SEARCHING
			return false;
#endif
			return EditorPrefs.GetBool(PluginConstants.ourSocketSearchingKey, PluginConstants.ourSocketSearchingValue);
		}
	}
}
