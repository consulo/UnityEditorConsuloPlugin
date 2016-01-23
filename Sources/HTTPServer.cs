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

#if NUNIT
using NUnit.Core;
using NUnit.Core.Filters;
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using UnityEditor;

namespace MustBe.Consulo.Internal
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
			myListener.Prefixes.Add($"http://*:{myPort.ToString()}/");
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
				String uuid = null;
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
							EditorUtility.DisplayDialog(PluginConstants.DIALOG_TITLE, $"Unknown how handle API url {pathAndQuery}, please update UnityEditor plugin for Consulo", "OK");
						});
						code = HttpStatusCode.InternalServerError;
						break;
				}

				String text = "{ \"success\":" + resultValue + " }";

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
				EditorUtility.DisplayDialog(PluginConstants.DIALOG_TITLE, "Suite is null", "OK");
				return;
			}

			testSuite.Run(new NUnitTestListener(uuid), TestFilter.Empty);

			TestExecutionContext.CurrentContext.TestPackage = null;
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
	}
}