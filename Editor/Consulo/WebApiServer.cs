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
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Consulo.Internal.UnityEditor
{
	[InitializeOnLoad]
	public class WebApiServer
	{
		public static string ourCurrentTestUUID;
		public static string ourCurrentTestName;

		private static string ourDataPath;

		private static MessageSender ourMessageSender;

		static WebApiServer()
		{
			Process currentProcess = Process.GetCurrentProcess();
			int unityConsuloPluginPort = 56000 + currentProcess.Id % 1000 + 2000; // 56000 + 2000

			HTTPServer httpServer = new HTTPServer(unityConsuloPluginPort);

			ourMessageSender = new MessageSender();

			if (UnityUtil.IsDebugEnabled())
			{
				UnityEngine.Debug.Log("Binding port: " + unityConsuloPluginPort);
			}

			Action action = () => HTTPServer.SendSetDefinesToConsulo(null);

			UnityUtil.RunInMainThread(() =>
			{
				ourDataPath = Application.dataPath;

				AppDomain.CurrentDomain.DomainUnload += (sender, e) =>
				{
					EditorUserBuildSettings.activeBuildTargetChanged -= action;

					ourMessageSender.Stop();
					httpServer.Stop();
				};
			});

			EditorUserBuildSettings.activeBuildTargetChanged += action;

			Application.RegisterLogCallback((condition, stackTrace, type) =>
			{
				// we can't use debug and log handler
				if(UnityUtil.IsDebugEnabled() || ourDataPath == null)
				{
					return;
				}

				string testUUID = ourCurrentTestUUID;
				if(testUUID != null)
				{
					JSONClass jsonClass = new JSONClass();
					jsonClass.Add("name", ourCurrentTestName);
					jsonClass.Add("uuid", testUUID);
					jsonClass.Add("type", "TestOutput");
					jsonClass.Add("message", cutTooLogMessage(condition));
					jsonClass.Add("stackTrace", stackTrace);
					jsonClass.Add("messageType", Enum.GetName(typeof(LogType), type));

					Push("unityTestState", jsonClass);
				}
				else
				{
					JSONClass jsonClass = new JSONClass();

					jsonClass.Add("condition", cutTooLogMessage(condition));
					jsonClass.Add("stackTrace", stackTrace);
					jsonClass.Add("projectPath", Path.GetDirectoryName(ourDataPath));
					jsonClass.Add("type", Enum.GetName(typeof(LogType), type));

					Push("unityLog", jsonClass);
				}
			});

			EditorApplication.playmodeStateChanged += delegate
			{
				JSONClass jsonClass = new JSONClass();

				jsonClass.Add("isPlaying", new JSONData(EditorApplication.isPlaying));
				jsonClass.Add("projectPath", Path.GetDirectoryName(ourDataPath));

				Push("unityPlayState", jsonClass);
			};
		}

		private static string cutTooLogMessage(string message)
		{
			if (message.Length > 255)
			{
				return message.Substring(0, 255) + " (message extend 255 chars was cut)";
			}

			return message;
		}

		public static void Push(string url, JSONClass message)
		{
			ourMessageSender.Push(url, message);
		}
	}
}
