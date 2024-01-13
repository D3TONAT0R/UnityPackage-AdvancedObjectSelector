using UnityEditor;
using UnityEngine;

namespace AdvancedObjectSelector
{
	[CustomPropertyDrawer(typeof(Object), true)]
	internal class ObjectPropertyDrawer : PropertyDrawer
	{
		const string packageRoot = "Packages/com.github.d3tonat0r.advancedobjectselector/";

		//int pickerID;

		static GUIStyle extraBtnStyle;
		static Texture plusIcon;
		static Texture arrowUpIcon;
		static Texture arrowDownIcon;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if(fieldInfo == null)
			{
				EditorGUI.PropertyField(position, property, true);
			}
			else
			{
				OnGUI(position, property, label, fieldInfo.FieldType);
			}
		}

		public static void OnGUI(Rect position, SerializedProperty property, GUIContent label, System.Type fieldType)
		{
			if(extraBtnStyle == null)
			{
				extraBtnStyle = "ObjectFieldButton";
				plusIcon = EditorGUIUtility.IconContent("d_Toolbar Plus").image;
				arrowUpIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(packageRoot + "GetParentIcon.psd");
				arrowDownIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(packageRoot + "GetChildrenIcon.psd");
			}

			if(Preferences.Usage == Preferences.UsageMode.Disabled)
			{
				EditorGUI.PropertyField(position, property, true);
				return;
			}

			position.SplitHorizontal(position.width - 18, out _, out var knob);
			knob = knob.Inset(0, 0, 1, 1);
			if (Preferences.Usage == Preferences.UsageMode.ExtraButton) knob.x -= 20;
			Rect knob2 = knob;

			var mods = Event.current.modifiers;

			bool popout = property.objectReferenceValue != null && Event.current.shift;
			bool seekParent = false;
			bool seekChildren = false;

			if(typeof(Component).IsAssignableFrom(fieldType) && property.serializedObject.targetObject is Component comp)
			{
				//seekChildren = Event.current.control;
				//seekParent = Event.current.alt;

				if((seekChildren || seekParent) && GUI.Button(knob2, GUIContent.none))
				{
					if(seekParent)
					{
						property.objectReferenceValue = comp.GetComponentInParent(fieldType);
						if(property.objectReferenceValue) EditorGUIUtility.PingObject(property.objectReferenceValue);
					}
					else if(seekChildren)
					{
						property.objectReferenceValue = comp.GetComponentInChildren(fieldType);
						if(property.objectReferenceValue) EditorGUIUtility.PingObject(property.objectReferenceValue);
					}
				}
			}

			var ctrlID = GUIUtility.GetControlID(FocusType.Passive) + 1;
			bool openObjectPicker = GUI.Button(knob, "", GUIStyle.none);

			var inspectRect = position;
			inspectRect.xMin += EditorGUIUtility.labelWidth;

			var lEnabledState = GUI.enabled;
			GUI.enabled &= popout;
			if(GUI.Button(inspectRect, GUIContent.none, GUIStyle.none))
			{
				PopoutInspector.Open(property.objectReferenceValue);
			}
			GUI.enabled = lEnabledState;

			var lastColor = GUI.backgroundColor;
			if(popout) GUI.backgroundColor *= new Color(0.8f, 0.8f, 1.0f);

			EditorGUI.ObjectField(position, property, label);


			if (Preferences.Usage == Preferences.UsageMode.ExtraButton)
			{
				if (Event.current.type == EventType.Repaint) extraBtnStyle.Draw(knob, GUIContent.none, ctrlID);
				knob.xMin = knob.xMax - 8;
				knob.yMin = knob.yMax - 8;
				GUI.DrawTexture(knob, plusIcon);
			}

			if(seekChildren || seekParent)
			{
				GUI.DrawTexture(knob2, seekParent ? arrowUpIcon : arrowDownIcon, ScaleMode.ScaleToFit);
			}
			GUI.backgroundColor = lastColor;

			if (openObjectPicker)
			{
				//pickerID = GUIUtility.GetControlID(FocusType.Passive) + 100;
				//EditorGUIUtility.ShowObjectPicker<Object>(property.objectReferenceValue, true, "", pickerID);
				ObjectSelectorEnhanced.Open(property);
			}

			/*
			if (Event.current.commandName == "ObjectSelectorUpdated")
			{
				Debug.Log(EditorGUIUtility.GetObjectPickerControlID()+" -> "+pickerID);
				// do a thing relating to the object picker
				if (EditorGUIUtility.GetObjectPickerControlID() == pickerID) property.objectReferenceValue = EditorGUIUtility.GetObjectPickerObject();
			}
			if(Event.current.commandName == "ObjectSelectorClosed")
			{
				pickerID = -1;
			}
			*/
		}
	} 
}
