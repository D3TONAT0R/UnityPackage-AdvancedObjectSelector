using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AdvancedObjectSelector
{
	public class Preferences : ScriptableObject
	{
		private const string usageKey = "AdvancedObjectSelector_Usage";
		private const string enableSeekingKey = "AdvancedObjectSelector_EnableSeeking";
		private const string fullPopoutKey = "AdvancedObjectSelector_FullPopout";

		public enum UsageMode
		{
			Disabled = 0,
			OverrideDefault = 1,
			ExtraButton = 2
		}

		public static UsageMode Usage
		{
			get => (UsageMode)EditorPrefs.GetInt(usageKey, 2);
			set => EditorPrefs.SetInt(usageKey, (int)value);
		}

		public static bool SeekModeEnabled
		{
			get => EditorPrefs.GetBool(enableSeekingKey, true);
			set => EditorPrefs.SetBool(enableSeekingKey, value);
		}

		public static bool FullGameObjectPopout
		{
			get => EditorPrefs.GetBool(fullPopoutKey, true);
			set => EditorPrefs.SetBool(fullPopoutKey, value);
		}
	} 
}
