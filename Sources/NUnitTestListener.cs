using NUnit.Core;
using UnityEditor;

namespace MustBe.Consulo.Internal
{
	public class NUnitTestListener : EventListener
	{
		public void TestStarted(NUnit.Core.TestName testName)
		{
		}

		public void TestOutput(NUnit.Core.TestOutput testOutput)
		{
		}

		public void RunStarted(string name, int testCount)
		{
		}

		public void SuiteFinished(NUnit.Core.TestResult result)
		{
		}

		public void UnhandledException(System.Exception exception)
		{
		}

		public void TestFinished(NUnit.Core.TestResult result)
		{
		}

		public void SuiteStarted(NUnit.Core.TestName testName)
		{
			EditorUtility.DisplayDialog(PluginConstants.DIALOG_TITLE, "Suite Started: " + testName.Name, "OK");
		}

		public void RunFinished(System.Exception exception)
		{
		}

		public void RunFinished(NUnit.Core.TestResult result)
		{
		}
	}
}