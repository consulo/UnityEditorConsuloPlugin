using System.Threading;
using System.Collections.Concurrent;

namespace Consulo.Internal.UnityEditor
{
	public class MessageSender
	{
		private class Message
		{
			public string url;

			public JSONClass message;

			public Message(string url, JSONClass message)
			{
				this.url = url;
				this.message = message;
			}
		}

		private bool myStop;

		private ConcurrentQueue<Message> myMessages = new ConcurrentQueue<Message>();

		public MessageSender()
		{
			new Thread(Run).Start();
		}

		private void Run()
		{
			while (!myStop)
			{
				try
				{
					Message message;

					if (myMessages.TryDequeue(out message))
					{
						if (UnityUtil.IsDebugEnabled())
						{
							UnityEngine.Debug.Log($"Sending {message.message} to {message.url}");
						}

						UnityUtil.RunInMainThread(() => ConsuloIntegration.SendToConsulo(message.url, message.message));
					}
				}
				finally
				{
					Thread.Sleep(500);
				}
			}
		}

		public void Push(string url, JSONClass message)
		{
			myMessages.Enqueue(new Message(url, message));
		}

		public void Stop()
		{
			myStop = true;
		}
	}
}