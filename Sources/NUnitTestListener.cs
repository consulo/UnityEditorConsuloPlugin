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
using System;

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
			WebApiServer.ourCurrentTestName = testName.Name;

			JSONClass jsonClass = new JSONClass();
			jsonClass.Add("name", testName.Name);
			jsonClass.Add("uuid", myUUID);
			jsonClass.Add("type", "TestStarted");


			ConsuloIntegration.SendToConsulo("unityTestState", jsonClass);
		}

		public void TestOutput(NUnit.Core.TestOutput testOutput)
		{
			JSONClass jsonClass = new JSONClass();
			jsonClass.Add("name", Enum.GetName(typeof(TestOutputType), testOutput.Type));
			jsonClass.Add("uuid", myUUID);
			jsonClass.Add("type", "TestOutput");
			jsonClass.Add("message", testOutput.ToString());

			ConsuloIntegration.SendToConsulo("unityTestState", jsonClass);
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
			String typeText;
			switch(result.ResultState)
			{
				case ResultState.Ignored:
					typeText = "TestIgnored";
					break;
				case ResultState.Success:
					typeText = "TestFinished";
					break;
				default:
					typeText = "TestFailed";
					break;
			}

			jsonClass.Add("type", typeText);
			string message = result.Message;
			if(message != null)
			{
				jsonClass.Add("message", message);
			}
			string trace = result.StackTrace;
			if(trace != null)
			{
				jsonClass.Add("stackTrace", trace);
			}
			jsonClass.Add("time", result.Time.ToString());

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
#endif