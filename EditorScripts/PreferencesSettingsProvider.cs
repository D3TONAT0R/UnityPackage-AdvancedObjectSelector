using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AdvancedObjectSelector
{
	internal static class PreferencesSettingsProvider
	{
		[SettingsProvider]
		public static SettingsProvider CreateSettingsProvider()
		{
			var sp = new SettingsProvider("Preferences/Advanced Object Selector", SettingsScope.User)
			{
				guiHandler = DrawGUI
			};
			return sp;
		}

		static void DrawGUI(string searchContext)
		{
			EditorGUIUtility.labelWidth = 200;
			Preferences.Usage = (Preferences.UsageMode)EditorGUILayout.EnumPopup("Usage", Preferences.Usage);
			Preferences.SeekModeEnabled = EditorGUILayout.Toggle("Seek mode enabled (Ctrl / Alt)", Preferences.SeekModeEnabled);
			Preferences.FullGameObjectPopout = EditorGUILayout.Toggle("Show full game object in poout window", Preferences.FullGameObjectPopout);
		}
	}

}