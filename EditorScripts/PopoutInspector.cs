using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace AdvancedObjectSelector
{
	using System;
	using System.Reflection;
	using UnityEditor;
	using UnityEngine;

	public class PopoutInspector : EditorWindow
	{
		private UnityObject asset;
		private Editor assetEditor;

		public static void Open(UnityObject asset)
		{

			var lastSelection = Selection.objects;
			Selection.activeObject = asset;
			var windowType = typeof(Editor).Assembly.GetType("UnityEditor.InspectorWindow");
			//var mi = typeof(EditorWindow).GetMethod("CreateWindow", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static, null, new Type[] { typeof(string), typeof(Type[]) }, null);
			//var inspectorWindow = (EditorWindow)Convert.ChangeType(mi.MakeGenericMethod(windowType).Invoke(null, new object[] { "Popout Inspector", new System.Type[0] }), windowType);
			var inspectorWindow = (EditorWindow)CreateInstance(windowType);
			inspectorWindow.titleContent = new GUIContent(asset.name);
			windowType.GetProperty("isLocked").SetValue(inspectorWindow, true);
			inspectorWindow.ShowUtility();
			Selection.objects = lastSelection;

			/*
			return;
			Debug.Log("open");
			var window = CreateWindow<PopoutInspector>($"{asset.name} | {asset.GetType().Name}");
			window.asset = asset;
			window.assetEditor = Editor.CreateEditor(asset);
			//return window;
			*/
		}

		private void OnGUI()
		{
			GUI.enabled = false;
			asset = EditorGUILayout.ObjectField("Asset", asset, asset.GetType(), false);
			GUI.enabled = true;

			EditorGUILayout.BeginVertical(EditorStyles.helpBox);
			assetEditor.OnInspectorGUI();
			EditorGUILayout.EndVertical();
		}
	}

}
