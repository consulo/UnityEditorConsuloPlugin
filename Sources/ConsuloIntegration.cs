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

		private static string EditorScriptApp {
			get
			{
				return EditorPrefs.GetString("kScriptsDefaultApp");
			}
		}

		#if UNITY_5_6
		[MenuItem("Help/Consulo Integration", true)]
		static bool ValidateConsuloPlugin()
		{
			Menu.SetChecked("Help/Consulo Integration", UseConsulo());
			return true;
		}

		[MenuItem("Help/Consulo Integration")]
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

			SendToConsulo("unityOpenFile", jsonClass, true);
			return true;
		}

		/// <summary>
		/// Send request to consulo.
		/// </summary>
		/// <param name="url"></param>
		/// <param name="jsonClass"></param>
		/// <param name="start">Only true if user double click on file</param>
		public static void SendToConsulo(string url, JSONClass jsonClass, bool start = false)
		{
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
						SendRequestToConsulo(url, jsonClass);
					}
					catch(Exception e)
					{
						EditorUtility.DisplayDialog(PluginConstants.ourDialogTitle, "Consulo is not accessible at http://localhost:" + PluginConstants.ourPort + "/" + url + ", message: " + e.Message, "OK");
					}
				}
				else
				{
					if(start)
					{
						StartConsulo(url, jsonClass);
					}
				}
			}
			finally
			{
				ourInProgress = false;
			}
		}

		private static IAsyncResult SendRequestToConsulo(string url, JSONClass jsonClass)
		{
			WebRequest request = WebRequest.Create("http://localhost:" + PluginConstants.ourPort + "/api/" + url);
			request.Timeout = ourTimeout;
			request.Method = "POST";
			request.ContentType = "application/json; charset=utf-8";

			WebRequestState state = new WebRequestState
			{
				Request = request,
				Json = jsonClass.ToString(),
			};

			return request.BeginGetRequestStream(new AsyncCallback(WriteCallback), state);
		}

		private static void WriteCallback(IAsyncResult asynchronousResult)
		{
			WebRequestState state = (WebRequestState) asynchronousResult.AsyncState;

			using (Stream streamResponse = state.Request.EndGetRequestStream(asynchronousResult))
			{
				byte[] bytes = Encoding.UTF8.GetBytes(state.Json);

				streamResponse.Write(bytes, 0, bytes.Length);
			}

			state.Finished = true;
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

			ourLastCheckResult = false;

			try
			{
				using (Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
				{
					sock.ReceiveTimeout = 100;
					sock.SendTimeout = 100;
					sock.Blocking = true;

					sock.Connect("localhost", PluginConstants.ourPort);
					if(sock.Connected)
					{
						return ourLastCheckResult = true;
					}
				}
			}
			catch
			{
			}
			return false;
		}

		private static void StartConsulo(string url, JSONClass jsonClass)
		{
			Process process = new Process();
			string scriptApp = EditorScriptApp;

			if(new FileInfo(scriptApp).Extension == ".app")
			{
				process.StartInfo.FileName = "open";
				process.StartInfo.Arguments = string.Format("-n {0}{1}{0} --args {2}", "\"", "/" + scriptApp, "--no-recent-projects");
			}
			else
			{
				process.StartInfo.FileName = scriptApp;
				process.StartInfo.Arguments = "--no-recent-projects";
				process.StartInfo.WorkingDirectory = Directory.GetParent(scriptApp).FullName;
			}

			process.StartInfo.UseShellExecute = false;
			process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
			process.StartInfo.CreateNoWindow = true;
			process.StartInfo.RedirectStandardOutput = true;
			process.Start();


			Thread thread = new Thread(() =>
			{
				for(int i = 0; i < 60; i++)
				{
					if(!IsConsuloStarted())
					{
						Thread.Sleep(500);
						continue;
					}

					try
					{
						SendRequestToConsulo(url, jsonClass);
						break;
					}
					catch
					{
						Thread.Sleep(500);
					}
				}
			});
			thread.Name = "Sending request to Consulo";
			thread.Start();
		}
	}
}