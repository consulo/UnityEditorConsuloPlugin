using System;
using System.IO;
using System.Net;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace MustBe.Consulo.Internal
{
	public class ConsuloPlugin : MonoBehaviour
	{
		private const int ourPort = 62242;
		//private const int ourPort = 55333; // dev port
		private const String ourEditorPrefsKey = "UseConsuloAsExternalEditor";

		[MenuItem("Edit/Use Consulo as External Editor", true)]
		static bool ValidateUncheckConsulo()
		{
			Menu.SetChecked("Edit/Use Consulo as External Editor", UseConsulo());
			return true;
		}

		[MenuItem("Edit/Use Consulo as External Editor")]
		static void UncheckConsulo()
		{
			var state = UseConsulo();
			Menu.SetChecked("Edit/Use Consulo as External Editor", !state);
			EditorPrefs.SetBool(ourEditorPrefsKey, !state);
		}

		/*[MenuItem("File/Open Project in Consulo")]
		static void OpenProjectInConsulo()
		{
			//TODO [VISTALL] open project
		}*/

		static bool UseConsulo()
		{
			if (!EditorPrefs.HasKey(ourEditorPrefsKey))
			{
				EditorPrefs.SetBool(ourEditorPrefsKey, false);
				return false;
			}

			return EditorPrefs.GetBool(ourEditorPrefsKey);
		}

		[UnityEditor.Callbacks.OnOpenAssetAttribute()]
		static bool OnOpenedAssetCallback(int instanceID, int line)
		{
			if (!UseConsulo())
			{
				return false;
			}

			var projectPath = ProjectPath();

			var selected = EditorUtility.InstanceIDToObject(instanceID);

			var filePath = projectPath + "/" + AssetDatabase.GetAssetPath(selected);

			var request = WebRequest.Create("http://localhost:" + ourPort + "/api/unityOpenFile");
			request.Timeout = 5000;
			request.Method = "POST";

			projectPath.Replace('\\', '/');
			String json = "{" +
			"projectPath: \"" + projectPath + "\"," +
			"filePath: \"" + filePath + "\"," +
			"contentType: \"" + selected.GetType().ToString() + "\"," +
			"line: " + line +
			"}";
			request.ContentType = "application/json; charset=utf-8";
			var bytes = Encoding.UTF8.GetBytes(json);
			request.ContentLength = bytes.Length;

			using (var stream = request.GetRequestStream())
			{
				stream.Write(bytes, 0, bytes.Length);
			}

			using(var requestGetResponse = request.GetResponse())
			{
				using (var responseStream = requestGetResponse.GetResponseStream())
				{
					using (var streamReader = new StreamReader(responseStream))
					{
						var result = streamReader.ReadToEnd();
						if (result.Contains("unsupported-content-type"))
						{
							return false;
						}
					}
				}
			}
			return true;
		}

		static string ProjectPath()
		{
			return Path.GetDirectoryName(Application.dataPath);
		}
	}
}