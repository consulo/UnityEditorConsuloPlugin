/*
 * Copyright 2013-2016 must-be.org
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
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace MustBe.Consulo.Internal
{
	/// <summary>
	/// UnityEditor.Menu class is not exists in Unity 4.6 we need add some hack
	/// </summary>
	public class ConsuloIntegration
	{
		private static readonly List<string> ourSupportedContentTypes = new List<string>(new string[]{"UnityEditor.MonoScript", "UnityEngine.Shader"});

        [MenuItem("Edit/Send Scripting Defines to Consulo")]
        static void UpdateScriptingDefines()
        {
            WebApiServer.UpdateScriptingDefines();
        }

#if UNITY_BEFORE_5

		[MenuItem("Edit/Use Consulo as External Editor", true)]
		static bool UseConsuloAsExternalEditorValidator()
		{
			return !UseConsulo();
		}

		[MenuItem("Edit/Disable Consulo as External Editor", true)]
		static bool DisableConsuloAsExternalEditorValidator()
		{
			return UseConsulo();
		}

		[MenuItem("Edit/Use Consulo as External Editor")]
		static void UseConsuloAsExternalEditor()
		{
			EditorPrefs.SetBool(PluginConstants.ourEditorPrefsKey, true);
		}

		[MenuItem("Edit/Disable Consulo as External Editor")]
		static void DisableConsuloAsExternalEditor()
		{
			EditorPrefs.SetBool(PluginConstants.ourEditorPrefsKey, false);
		}

#else

		[MenuItem("Edit/Use Consulo as External Editor", true)]
		static bool ValidateUncheckConsulo()
		{
			Menu.SetChecked("Edit/Use Consulo as External Editor", UseConsulo());
			return true;
		}

		[MenuItem("Edit/Use Consulo as External Editor")]
		static void UncheckConsulo()
		{
			var state = UseConsulo();
			Menu.SetChecked("Edit/Use Consulo as External Editor", !state);
			EditorPrefs.SetBool(PluginConstants.ourEditorPrefsKey, !state);
		}
#endif

		static bool UseConsulo()
		{
			if(!EditorPrefs.HasKey(PluginConstants.ourEditorPrefsKey))
			{
				EditorPrefs.SetBool(PluginConstants.ourEditorPrefsKey, false);
				return false;
			}

			return EditorPrefs.GetBool(PluginConstants.ourEditorPrefsKey);
		}

		[OnOpenAsset]
		static bool OnOpenedAssetCallback(int instanceID, int line)
		{
			if(!UseConsulo())
			{
				return false;
			}

			UnityEngine.Object selected = EditorUtility.InstanceIDToObject(instanceID);
			string contentType = selected.GetType().ToString();
			if(!ourSupportedContentTypes.Contains(contentType))
			{
				return false;
			}

			string projectPath = Path.GetDirectoryName(Application.dataPath);
			string filePath = projectPath + "/" + AssetDatabase.GetAssetPath(selected);
			projectPath = projectPath.Replace('\\', '/');
			filePath = filePath.Replace('\\', '/');

			JSONClass jsonClass = new JSONClass();
			jsonClass.Add("projectPath", new JSONData(projectPath));
			jsonClass.Add("filePath", new JSONData(filePath));
			jsonClass.Add("editorPath", new JSONData(EditorApplication.applicationPath));
			jsonClass.Add("contentType", new JSONData(selected.GetType().ToString()));
			jsonClass.Add("line", new JSONData(line));

			SendToConsulo("unityOpenFile", jsonClass);
			return true;
		}

		public static void SendToConsulo(string url, JSONClass jsonClass)
		{
			if(!UseConsulo())
			{
				return;
			}

			try
			{
				WebRequest request = WebRequest.Create("http://localhost:" + PluginConstants.ourPort + "/api/" + url);
				request.Timeout = 10000;
				request.Method = "POST";
				request.ContentType = "application/json; charset=utf-8";

				WebRequestState state = new WebRequestState
				{
					Request = request,
					Json = jsonClass
				};

				request.BeginGetRequestStream(new AsyncCallback(WriteCallback), state);
			}
			catch(Exception e)
			{
				EditorUtility.DisplayDialog(PluginConstants.ourDialogTitle, "Consulo is not accessible at http://localhost:" + PluginConstants.ourPort + "/" + url + ", message: " + e.Message, "OK");
			}
		}

		private static void WriteCallback(IAsyncResult asynchronousResult)
		{
			WebRequestState state = (WebRequestState) asynchronousResult.AsyncState;

			using (Stream streamResponse = state.Request.EndGetRequestStream(asynchronousResult))
			{
				string jsonClassToString = state.Json.ToString();
				byte[] bytes = Encoding.UTF8.GetBytes(jsonClassToString);

				streamResponse.Write(bytes, 0, bytes.Length);
			}
		}
	}
}