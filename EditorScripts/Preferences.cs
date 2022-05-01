using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AdvancedObjectSelector
{
	public class Preferences : ScriptableObject
	{
		public enum UsageMode
		{
			Disabled,
			OverrideDefault,
			ExtraButton
		}

		static string Path => System.IO.Path.Combine(Application.dataPath, "../ProjectSettings/AdvancedObjectSelector.asset");

		public static Preferences Instance
		{
			get
			{
				if(preferences == null)
				{
					preferences = CreateInstance<Preferences>();
					if(File.Exists(Path))
					{
						JsonUtility.FromJsonOverwrite(File.ReadAllText(Path), preferences);
					}
					else
					{
						File.WriteAllText(Path, JsonUtility.ToJson(preferences));
					}
				}
				return preferences;
			}
		}
		private static Preferences preferences;

		public UsageMode usage = UsageMode.OverrideDefault;

		internal static SerializedObject GetSerializedSettings()
		{
			return new SerializedObject(Instance);
		}

		internal static void Save()
		{
			File.WriteAllText(Path, JsonUtility.ToJson(preferences));
		}
	} 
}
