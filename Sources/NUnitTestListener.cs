using NUnit.Core;
using UnityEditor;

namespace MustBe.Consulo.Internal
{
	public class NUnitTestListener : EventListener
	{
		private string myUUID;

		public NUnitTestListener(string uuid)
		{
			myUUID = uuid;
		}

		public void TestStarted(NUnit.Core.TestName testName)
		{
			JSONClass jsonClass = new JSONClass();
			jsonClass.Add("name", testName.Name);
			jsonClass.Add("uuid", myUUID);
			jsonClass.Add("state", "true");

			ConsuloIntegration.SendToConsulo("unityTestState", jsonClass);
		}

		public void TestOutput(NUnit.Core.TestOutput testOutput)
		{
		}

		public void RunStarted(string name, int testCount)
		{
		}

		public void SuiteFinished(NUnit.Core.TestResult result)
		{
			JSONClass jsonClass = new JSONClass();
			jsonClass.Add("name", result.Name);
			jsonClass.Add("uuid", myUUID);
			jsonClass.Add("suite", "true");
			jsonClass.Add("state", "false");

			ConsuloIntegration.SendToConsulo("unityTestState", jsonClass);
		}

		public void UnhandledException(System.Exception exception)
		{
		}

		public void TestFinished(NUnit.Core.TestResult result)
		{
			JSONClass jsonClass = new JSONClass();
			jsonClass.Add("name", result.Name);
			jsonClass.Add("uuid", myUUID);
			jsonClass.Add("state", "false");

			ConsuloIntegration.SendToConsulo("unityTestState", jsonClass);
		}

		public void SuiteStarted(NUnit.Core.TestName testName)
		{
			JSONClass jsonClass = new JSONClass();
			jsonClass.Add("name", testName.Name);
			jsonClass.Add("uuid", myUUID);
			jsonClass.Add("suite", "true");
			jsonClass.Add("state", "true");

			ConsuloIntegration.SendToConsulo("unityTestState", jsonClass);
		}

		public void RunFinished(System.Exception exception)
		{
		}

		public void RunFinished(NUnit.Core.TestResult result)
		{
		}
	}
}