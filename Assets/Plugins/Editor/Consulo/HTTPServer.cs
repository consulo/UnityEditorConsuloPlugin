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

#if UNITY_5_6
using NUnit.Framework.Api;
#elif NUNIT
using NUnit.Core;
#endif
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using UnityEditor;
using UnityEngine;

namespace Consulo.Internal.UnityEditor
{
	internal class HTTPServer
	{
		private Thread myListenThread;
		private HttpListener myListener;
		private int myPort;
		private bool myStopped;

		public HTTPServer(int port)
		{
			Initialize(port);
		}

		public void Stop()
		{
			myStopped = true;
			myListenThread.Abort();
			myListener.Stop();
		}

		private void Listen()
		{
			myListener = new HttpListener();
			myListener.Prefixes.Add($"http://*:{myPort}/");
			myListener.Start();
			while(!myStopped)
			{
				try
				{
					HttpListenerContext context = myListener.GetContext();
					Process(context);
				}
				catch(Exception)
				{
				}
			}
		}

		private void Process(HttpListenerContext context)
		{
			var requestHttpMethod = context.Request.HttpMethod;
			if("POST".Equals(requestHttpMethod))
			{
				string pathAndQuery = context.Request.Url.PathAndQuery;

				context.Response.ContentType = "application/json";

				bool resultValue = false;
				HttpStatusCode code = HttpStatusCode.InternalServerError;

				JSONClass jsonClass = null;
				string uuid = null;
				switch(pathAndQuery)
				{
					case "/unityRefresh":
						jsonClass = ReadJSONClass(context);
						uuid = jsonClass == null ? null : jsonClass["uuid"].Value;
						resultValue = uuid != null;
						if(uuid != null)
						{
							UnityUtil.RunInMainThread(() =>
							{
								AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

								JSONClass result = new JSONClass();
								result.Add("uuid", uuid);
								ConsuloIntegration.SendToConsulo("unityRefreshResponse", result);
							});
						}
						code = HttpStatusCode.OK;
						break;
					case "/unityRequestDefines":
					{
						jsonClass = ReadJSONClass(context);
						if(jsonClass != null)
						{
							resultValue = true;
							uuid = jsonClass["uuid"].Value;
							SendSetDefinesToConsulo(uuid);
						}

						code = HttpStatusCode.OK;
						break;
					}
					case "/unityOpenScene":
						jsonClass = ReadJSONClass(context);
						string fileValue = jsonClass == null ? null : jsonClass["file"].Value;
						if(fileValue != null)
						{
							resultValue = true;
							UnityUtil.RunInMainThread(() =>
							{
								EditorApplication.OpenScene(fileValue);
								EditorUtility.FocusProjectWindow();
							});
						}
						code = HttpStatusCode.OK;
						break;
#if NUNIT
					case "/unityRunTest":
						jsonClass = ReadJSONClass(context);
						if(jsonClass != null)
						{
							resultValue = true;
							string type = jsonClass["type"].Value;
							uuid = jsonClass["uuid"].Value;
							UnityUtil.RunInMainThread(() =>
							{
								int undo = Undo.GetCurrentGroup();
								RunNUnitTests(type, uuid);
								Undo.RevertAllDownToGroup(undo);

								JSONClass result = new JSONClass();
								result.Add("uuid", uuid);
								result.Add("type", "RunFinished");
								ConsuloIntegration.SendToConsulo("unityTestState", result);
							});
						}
						code = HttpStatusCode.OK;
						break;
#endif
					default:
						UnityUtil.RunInMainThread(() =>
						{
							EditorUtility.DisplayDialog(PluginConstants.ourDialogTitle, $"Unknown how handle API url {pathAndQuery}, please update UnityEditor plugin for Consulo", "OK");
						});
						code = HttpStatusCode.InternalServerError;
						break;
				}

				string text = "{ \"success\":" + resultValue + " }";

				context.Response.ContentLength64 = text.Length;
				context.Response.StatusCode = (int) code;
				byte[] encodingUTFGetBytes = Encoding.UTF8.GetBytes(text);
				context.Response.OutputStream.Write(encodingUTFGetBytes, 0, encodingUTFGetBytes.Length);
				context.Response.OutputStream.Flush();
			}
			else
			{
				context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
			}

			context.Response.OutputStream.Close();
		}

		private void Initialize(int port)
		{
			myPort = port;
			myListenThread = new Thread(Listen);
			myListenThread.Start();
		}

#if NUNIT
		private static void RunNUnitTests(string type, string uuid)
		{
			#if UNITY_5_6
			/*Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			List<Assembly> loadAssemblies = new List<Assembly>();
			foreach (Assembly t in assemblies)
			{
				string fullName = t.FullName;
				if(fullName.Contains("Assembly-CSharp-Editor") || fullName.Contains("Assembly-UnityScript-Editor"))
				{
					loadAssemblies.Add(t);
				}
			}

			NUnitTestAssemblyRunner runner = new NUnitTestAssemblyRunner(new DefaultTestAssemblyBuilder());

			foreach (Assembly assembly in loadAssemblies)
			{
				ITest test = runner.Load(assembly, new Dictionary<string, object>());
				if(test == null)
				{
					continue;
				}

				WebApiServer.ourCurrentTestUUID = uuid;

				runner.Run(new NUnitTestListener(uuid), TestFilter.Empty);

				WebApiServer.ourCurrentTestUUID = null;
				WebApiServer.ourCurrentTestName = null;
			} */

			#else
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			List<string> assemblyLocations = new List<string>();
			foreach (Assembly t in assemblies)
			{
				string fullName = t.FullName;
				if(fullName.Contains("Assembly-CSharp-Editor") || fullName.Contains("Assembly-UnityScript-Editor"))
				{
					assemblyLocations.Add(t.Location);
				}
			}

			CoreExtensions.Host.InitializeService(); // need initialize service

			TestPackage testPackage = new TestPackage(PlayerSettings.productName, assemblyLocations);

			TestExecutionContext.CurrentContext.TestPackage = testPackage;

			TestSuiteBuilder builder = new TestSuiteBuilder();

			TestSuite testSuite = builder.Build(testPackage);

			if(testSuite == null)
			{
				EditorUtility.DisplayDialog(PluginConstants.ourDialogTitle, "Suite is null", "OK");
				return;
			}

			WebApiServer.ourCurrentTestUUID = uuid;

			testSuite.Run(new NUnitTestListener(uuid), TestFilter.Empty);

			WebApiServer.ourCurrentTestUUID = null;
			WebApiServer.ourCurrentTestName = null;

			TestExecutionContext.CurrentContext.TestPackage = null;
			#endif
		}
#endif

		private static JSONClass ReadJSONClass(HttpListenerContext context)
		{
			using (Stream stream = context.Request.InputStream)
			{
				using (var streamReader = new StreamReader(stream))
				{
					JSONNode result = JSON.Parse(streamReader.ReadToEnd());
					if(result is JSONClass)
					{
						return (JSONClass) result;
					}
				}
			}
			return null;
		}

		public static void SendSetDefinesToConsulo(string uuid)
		{
			UnityUtil.RunInMainThread(() =>
			{
				if(!ConsuloIntegration.UseConsulo())
				{
					return;
				}

				string projectPath = Path.GetDirectoryName(Application.dataPath);
				projectPath = projectPath.Replace('\\', '/');

				JSONClass result = new JSONClass();
				result.Add("projectPath", projectPath);
				if(uuid != null)
				{
					result.Add("uuid", uuid);
				}
				JSONArray array = new JSONArray();
				foreach (string define in EditorUserBuildSettings.activeScriptCompilationDefines)
				{
					array.Add(define);
				}

				result.Add("defines", array);

				ConsuloIntegration.SendToConsulo("unitySetDefines", result);
			});
		}
	}
}