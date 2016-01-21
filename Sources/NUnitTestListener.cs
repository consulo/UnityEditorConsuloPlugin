using NUnit.Core;
using System;
using UnityEditor;

namespace MustBe.Consulo.Internal
{
	public class NUnitTestListener : EventListener
	{
		private string myUUID;
		private string myRootName;

		public NUnitTestListener(string uuid)
		{
			myUUID = uuid;
		}

		public void TestStarted(NUnit.Core.TestName testName)
		{
			JSONClass jsonClass = new JSONClass();
			jsonClass.Add("name", testName.Name);
			jsonClass.Add("uuid", myUUID);
			jsonClass.Add("type", "TestStarted");


			ConsuloIntegration.SendToConsulo("unityTestState", jsonClass);
		}

		public void TestOutput(NUnit.Core.TestOutput testOutput)
		{
			// something?
		}

		public void RunStarted(string name, int testCount)
		{
			//
		}

		public void SuiteStarted(NUnit.Core.TestName testName)
		{
			if(myRootName == null)
			{
				myRootName = testName.FullName;
				return;
			}

			JSONClass jsonClass = new JSONClass();
			jsonClass.Add("uuid", myUUID);
			jsonClass.Add("name", testName.Name);
			jsonClass.Add("type", "SuiteStarted");

			ConsuloIntegration.SendToConsulo("unityTestState", jsonClass);
		}

		public void SuiteFinished(NUnit.Core.TestResult result)
		{
			if(result.FullName.Equals(myRootName))
			{
				return;
			}

			JSONClass jsonClass = new JSONClass();
			jsonClass.Add("name", result.Name);
			jsonClass.Add("uuid", myUUID);
			jsonClass.Add("type", "SuiteFinished");

			ConsuloIntegration.SendToConsulo("unityTestState", jsonClass);
		}

		public void UnhandledException(System.Exception exception)
		{
			// something?
		}

		public void TestFinished(NUnit.Core.TestResult result)
		{
			JSONClass jsonClass = new JSONClass();
			jsonClass.Add("name", result.Name);
			jsonClass.Add("uuid", myUUID);
			jsonClass.Add("type", result.IsSuccess ? "TestFinished" : "TestFailed");

			ConsuloIntegration.SendToConsulo("unityTestState", jsonClass);
		}

		public void RunFinished(System.Exception exception)
		{
			//
		}

		public void RunFinished(NUnit.Core.TestResult result)
		{
			//
		}
	}
}