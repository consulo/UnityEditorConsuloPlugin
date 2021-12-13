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
#pragma warning disable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using Consulo.Internal.UnityEditor;

namespace Consulo.Internal.UnityEditor
{
	/// <summary>
	/// UnityEditor.Menu class is not exists in Unity 4.6 we need add some hack
	/// </summary>
	public class ConsuloIntegration
	{
		private static readonly List<string> ourSupportedContentTypes = new List<string>(new string[]{"UnityEditor.MonoScript", "UnityEngine.Shader"});
		private static bool ourInProgress;
		private static int ourTimeout = 1000;

		private static int ourLastCheckCacheTime = 5000;
		private static int ourLastCheck = -1;
		private static bool ourLastCheckResult;
		private static int ourLastCheckedProcessId = -1;

		private static string EditorScriptApp {
			get
			{
				return EditorPrefs.GetString("kScriptsDefaultApp");
			}
		}

		#if UNITY_5_6_OR_NEWER
		[MenuItem("Consulo/Socket searching", true)]
		static bool EnableSocketSearching()
		{
			bool value = EditorPrefs.GetBool(PluginConstants.ourSocketSearchingKey, PluginConstants.ourSocketSearchingValue);
			Menu.SetChecked("Consulo/Socket searching", value);
			return true;
		}

		[MenuItem("Consulo/Socket searching")]
		static void ClickEnableSocketSearching()
		{
			bool oldValue = EditorPrefs.GetBool(PluginConstants.ourSocketSearchingKey, PluginConstants.ourSocketSearchingValue);
			EditorPrefs.SetBool(PluginConstants.ourSocketSearchingKey, !oldValue);
		}

		[MenuItem("Consulo/Integration", true)]
		static bool ValidateConsuloPlugin()
		{
			Menu.SetChecked("Consulo/Integration", UseConsulo());
			return true;
		}

		[MenuItem("Consulo/Integration")]
		static void ClickConsuloPlugin()
		{
			if(UseConsulo())
			{
				EditorUtility.DisplayDialog(PluginConstants.ourDialogTitle, "For disabling Consulo integration - reset 'External Editor'", "OK");
			}
			else
			{
				EditorUtility.DisplayDialog(PluginConstants.ourDialogTitle, "For enabling Consulo integration - set Consulo as 'External Editor'", "OK");
			}
		}
		#endif

		internal static bool UseConsulo()
		{
			string scriptApp = EditorScriptApp;
			// ignore case
			return !string.IsNullOrEmpty(scriptApp) && scriptApp.IndexOf("consulo", StringComparison.OrdinalIgnoreCase) != -1;
		}

		[OnOpenAsset]
		static bool OnOpenedAssetCallback(int instanceID, int line)
		{
			if(!UseConsulo())
			{
				if(UnityUtil.IsDebugEnabled())
				{
					UnityEngine.Debug.Log("UseConsulo() = false");
				}
				return false;
			}

			UnityEngine.Object selected = EditorUtility.InstanceIDToObject(instanceID);
			string contentType = selected.GetType().ToString();
			if(!ourSupportedContentTypes.Contains(contentType))
			{
				if(UnityUtil.IsDebugEnabled())
				{
					UnityEngine.Debug.Log("Not supported type " + contentType);
				}
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

			SendToConsulo("unityOpenFile", jsonClass, true, true);
			return true;
		}

		/// <summary>
		/// Send request to consulo.
		/// </summary>
		/// <param name="url"></param>
		/// <param name="jsonClass"></param>
		/// <param name="focus">Focus app</param>
		/// <param name="start">Only true if user double click on file</param>
		public static void SendToConsulo(string url, JSONClass jsonClass, bool focus = false, bool start = false)
		{
			if(UnityUtil.IsDebugEnabled())
			{
				UnityEngine.Debug.Log("Sending json to consulo " + jsonClass);
			}

			if(!UseConsulo())
			{
				return;
			}

			if(ourInProgress)
			{
				return;
			}

			try
			{
				ourInProgress = true;

				if(IsConsuloStarted(!start))
				{
					try
					{
						SendRequestToConsulo(url, jsonClass, focus);
					}
					catch(Exception e)
					{
						UnityEngine.Debug.LogError(e);
						EditorUtility.DisplayDialog(PluginConstants.ourDialogTitle, "Consulo is not accessible at http://localhost:" + PluginConstants.ourPort + "/" + url + ", message: " + e.Message, "OK");
					}
				}
				else
				{
					if(start)
					{
						StartConsulo(url, jsonClass, focus);
					}
				}
			}
			finally
			{
				ourInProgress = false;
			}
		}

		private static void SendRequestToConsulo(string url, JSONClass jsonClass, bool focus)
		{
			System.Collections.IEnumerator e = SendRequestToConsuloImpl(url, jsonClass);
			while(e.MoveNext())
			{
			}

			if(focus)
			{
				switch(UnityUtil.OSFamily)
				{
					case OSFamily.Windows:
						if(ourLastCheckedProcessId != -1)
						{
							User32Dll.AllowSetForegroundWindow(ourLastCheckedProcessId);
						}
						break;
				}
			}
		}

		private static System.Collections.IEnumerator SendRequestToConsuloImpl(string url, JSONClass jsonClass)
		{
			string fullUrl = "http://localhost:" + PluginConstants.ourPort + "/api/" + url;

			using(UnityEngine.Networking.UnityWebRequest post = new UnityEngine.Networking.UnityWebRequest(fullUrl, UnityEngine.Networking.UnityWebRequest.kHttpVerbPOST))
			{
				byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(jsonClass.ToString());
				post.uploadHandler = new UnityEngine.Networking.UploadHandlerRaw(jsonToSend);
				post.SetRequestHeader("Content-Type", "application/json");
				post.timeout = ourTimeout;

				yield return post.SendWebRequest();

				if (post.isHttpError || post.isNetworkError)
				{
					throw new Exception(post.error);
				}
			}
		}

		private static bool IsConsuloStarted(bool useTimeCheck = false)
		{
			int tickCount = Environment.TickCount;

			if(useTimeCheck)
			{
				if(ourLastCheck > 0 && (tickCount - ourLastCheck) < ourLastCheckCacheTime)
				{
					return ourLastCheckResult;
				}

				ourLastCheck = tickCount;
			}

			ourLastCheckedProcessId = -1;
			ourLastCheckResult = false;

			try
			{
				Process[] processes = Process.GetProcesses();
				foreach (Process process in processes)
				{
					string processName = null;

					try
					{
						// System.InvalidOperationException: Process has exited or is inaccessible, so the requested information is not available.
						processName = process.ProcessName.ToLowerInvariant();
					}
					catch
					{
					}

					if(processName != null && processName.Contains("consulo"))
					{
						ourLastCheckedProcessId = process.Id;

						return ourLastCheckResult = true;
					}
				}
			}
			catch (Exception e)
			{
				UnityEngine.Debug.LogError(e);
			}

			try
			{
				if (UnityUtil.IsSocketSearchingEnabled())
				{
					using (Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)) {
						sock.ReceiveTimeout = 100;
						sock.SendTimeout = 100;
						sock.Blocking = true;

						sock.Connect("localhost", PluginConstants.ourPort);
						if (sock.Connected)
						{
							return ourLastCheckResult = true;
						}
					}
				}
			}
			catch
			{
			}
			return false;
		}

		private static void StartConsulo(string url, JSONClass jsonClass, bool focus)
		{
			JSONClass apiCall = new JSONClass();
			apiCall.Add("url", "/api/" + url);
			apiCall.Add("body", jsonClass);

			string jsonFilePath = Path.GetTempPath() + Guid.NewGuid().ToString() + ".json";

			File.WriteAllText(jsonFilePath, apiCall.ToString());

			Process process = new Process();
			string scriptApp = EditorScriptApp;

			if(new FileInfo(scriptApp).Extension == ".app")
			{
				process.StartInfo.FileName = "open";
				process.StartInfo.Arguments = string.Format("-n {0}{1}{0} --args {2}", "\"", "/" + scriptApp, "--no-recent-projects --json " + jsonFilePath);
			}
			else
			{
				process.StartInfo.FileName = scriptApp;
				process.StartInfo.Arguments = "--no-recent-projects --json " + jsonFilePath;
				process.StartInfo.WorkingDirectory = Directory.GetParent(scriptApp).FullName;
			}

			process.StartInfo.UseShellExecute = false;
			process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
			process.StartInfo.CreateNoWindow = true;
			process.StartInfo.RedirectStandardOutput = true;
			process.Start();
		}
	}
}