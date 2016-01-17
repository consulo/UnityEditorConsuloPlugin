using System;
using System.Diagnostics;
using UnityEditor;

namespace MustBe.Consulo.Internal
{
	internal delegate void Action();

	[InitializeOnLoad]
	public class WebApiServer
	{
		static WebApiServer()
		{
			Process currentProcess = Process.GetCurrentProcess();
			int unityConsuloPluginPort = 56000 + currentProcess.Id % 1000 + 2000; // 56000 + 2000

			HTTPServer httpServer = new HTTPServer(unityConsuloPluginPort);

			UnityUtil.RunInMainThread(() =>
			{
				AppDomain.CurrentDomain.DomainUnload += (sender, e) =>
				{
					httpServer.Stop();
				};
			});
		}
	}
}
