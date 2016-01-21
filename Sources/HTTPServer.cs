using NUnit.Core;
using NUnit.Core.Filters;
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
				switch(pathAndQuery)
				{
					case "/unityOpenScene":
						jsonClass = ReadJSONClass(context);
						string fileValue = jsonClass == null ? null : jsonClass["file"].Value;
						if(fileValue != null)
						{
							resultValue = true;
							UnityUtil.RunInMainThread(() =>
							{
								EditorApplication.OpenScene(fileValue);
							});
						}
						code = HttpStatusCode.OK;
						break;
					case "/unityRunTest":
						jsonClass = ReadJSONClass(context);
						if(jsonClass != null)
						{
							resultValue = true;
							string type = jsonClass["type"].Value;
							string uuid = jsonClass["uuid"].Value;
							UnityUtil.RunInMainThread(() =>
							{
								int undo = Undo.GetCurrentGroup();
								RunNUnitTests(type, uuid);
								Undo.RevertAllDownToGroup(undo);
							});
						}
						code = HttpStatusCode.OK;
						break;
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