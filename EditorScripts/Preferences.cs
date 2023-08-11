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
	} 
}
