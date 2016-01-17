using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
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
			myListener.Prefixes.Add("http://*:" + myPort.ToString() + "/");
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

				switch(pathAndQuery)
				{
					case "/unityOpenScene":
						context.Response.ContentType = "application/json";
						bool r = false;
						using (Stream stream = context.Request.InputStream)
						{
							using (var streamReader = new StreamReader(stream))
							{
								JSONNode result = JSON.Parse(streamReader.ReadToEnd());
								if(result is JSONClass)
								{
									string fileValue = result["file"].Value;
									if(fileValue != null)
									{
										r = true;
										UnityUtil.RunInMainThread(() =>
										{
											EditorApplication.OpenScene(fileValue);
										});
									}
								}
							}
						}
						String text = "{ \"success\":" + r + " }";

						context.Response.ContentLength64 = text.Length;
						byte[] encodingUTFGetBytes = Encoding.UTF8.GetBytes(text);
						context.Response.OutputStream.Write(encodingUTFGetBytes, 0, encodingUTFGetBytes.Length);
						context.Response.OutputStream.Flush();
						context.Response.StatusCode = (int)HttpStatusCode.OK;
						break;
					default:
						context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
						break;
				}
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
	}
}