using UnityEditor;
using UnityEngine;

namespace AdvancedObjectSelector
{
	[CustomPropertyDrawer(typeof(Object), true)]
	internal class ObjectPropertyDrawer : PropertyDrawer
	{
		//int pickerID;

		static GUIStyle extraBtnStyle;
		static Texture plusIcon;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			OnGUI(position, property, label, fieldInfo.FieldType);
		}

		public static void OnGUI(Rect position, SerializedProperty property, GUIContent label, System.Type fieldType)
		{
			var usage = Preferences.Instance.usage;
			if (usage == Preferences.UsageMode.Disabled) return;

			var content = EditorGUIUtility.ObjectContent(property.objectReferenceValue, fieldType);

			position.SplitHorizontal(position.width - 18, out _, out var knob);
			knob = knob.Inset(0, 0, 1, 1);
			if (usage == Preferences.UsageMode.ExtraButton) knob.x -= 20;
			var ctrlID = GUIUtility.GetControlID(FocusType.Passive) + 1;
			bool openObjectPicker = GUI.Button(knob, "", GUIStyle.none);

			var inspectRect = position;
			inspectRect.xMin += EditorGUIUtility.labelWidth;
			if(property.objectReferenceValue != null && Event.current.shift && GUI.Button(inspectRect, GUIContent.none, GUIStyle.none))
			{
				PopoutInspector.Open(property.objectReferenceValue);
			}

			EditorGUI.ObjectField(position, property, label);
			if (usage == Preferences.UsageMode.ExtraButton)
			{
				if (extraBtnStyle == null)
				{
					extraBtnStyle = "ObjectFieldButton";
					plusIcon = EditorGUIUtility.IconContent("d_Toolbar Plus").image;
				}
				if (Event.current.type == EventType.Repaint) extraBtnStyle.Draw(knob, GUIContent.none, ctrlID);
				knob.xMin = knob.xMax - 8;
				knob.yMin = knob.yMax - 8;
				GUI.DrawTexture(knob, plusIcon);
			}
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
