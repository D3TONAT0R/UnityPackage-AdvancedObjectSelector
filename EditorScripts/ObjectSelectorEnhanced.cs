using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace AdvancedObjectSelector
{
	internal partial class ObjectSelectorEnhanced : EditorWindow
	{
		// Styles used in the object selector
		internal static class Styles
		{
			public static GUIStyle tabButton;
			public static GUIStyle smallStatus = "ObjectPickerSmallStatus";
			public static GUIStyle largeStatus = "ObjectPickerLargeStatus";
			public static GUIStyle tab = "ObjectPickerTab";
			public static GUIStyle bottomBar = "RL DragHandle";
			public static GUIStyle bottomResize = "WindowBottomResize";
			public static GUIStyle previewBackground = "PopupCurveSwatchBackground"; // TODO: Make dedicated style
			public static GUIStyle previewTextureBackground = "ObjectPickerPreviewBackground"; // TODO: Make dedicated style
			public static GUIStyle listItem = "ObjectPickerResultsEven";
			public static GUIStyle listItemSelected = "OL SelectedRow";
			public static GUIStyle nameLabel;
			public static GUIStyle nameLabelSelected;
			public static GUIStyle typeLabel;
			public static GUIStyle typeLabelSelected;
			public static GUIStyle separator = "EyeDropperHorizontalLine";
			public static GUIStyle searchField = "ToolbarSeachTextField";
			public static GUIStyle searchFieldEnd = "ToolbarSeachCancelButton";

			public static readonly GUIContent packagesVisibilityContent = EditorGUIUtility.TrIconContent("SceneViewVisibility", "Number of hidden packages, click to toggle packages visibility");

			public static float listItemHeight;

			static Styles()
			{
				tabButton = new GUIStyle(EditorStyles.toolbarButton)
				{
					font = EditorStyles.boldFont
				};
				listItemHeight = listItem.CalcHeight(new GUIContent("xyz"), 100);
				typeLabel = new GUIStyle(EditorStyles.helpBox)
				{
					fontSize = 8,
					alignment = TextAnchor.MiddleRight,
					wordWrap = false
				};
				typeLabelSelected = new GUIStyle(typeLabel);
				typeLabelSelected.normal.textColor = Color.white;
				nameLabel = new GUIStyle(GUI.skin.label);
				nameLabelSelected = new GUIStyle(nameLabel);
				nameLabelSelected.normal.textColor = Color.white;
				listItemSelected = new GUIStyle(listItemSelected)
				{
					font = nameLabelSelected.font,
					alignment = nameLabelSelected.alignment
				};
			}
		}

		internal abstract class Tab
		{
			public const int listItemHeight = 18;

			protected int listIndex = 0;
			protected Vector2 size;

			internal abstract string TabName { get; }

			internal virtual bool NeedsExtraIndentation => false;

			internal virtual void Init() { }

			internal virtual void HandleInputEvent(Event keyDownEvent) { }

			internal virtual void OnHeader(Rect rect) { }

			internal void DrawGUI(Rect rect)
			{
				listIndex = 0;
				size = rect.size;
				size.x -= GUI.skin.verticalScrollbar.fixedWidth;
				size.y -= 40; //No idea why this is needed, but it is
				OnGUI();
			}

			protected abstract void OnGUI();

			internal virtual void OnSearchChange() { }

			internal virtual void OnSelectionChange(Object newSelection) { }

			internal virtual void OnValueChange(Object newValue) { }

			internal virtual void OnPreviewGUIExtras(Rect rect, Object obj) { }

			protected bool IsRepainting => Event.current.type == EventType.Repaint;

			protected void Separator()
			{
				var r = NextListRect();
				r.SplitVertical(r.height * 0.5f, out _, out r);
				if(IsRepainting) Styles.separator.Draw(r, false, false, false, false);
			}

			protected Rect NextListRect()
			{
				var r = new Rect(0, listIndex * listItemHeight, size.x, listItemHeight);
				listIndex++;
				return r;
			}

			protected void BeginScrollView()
			{
				scroll = EditorGUILayout.BeginScrollView(scroll, false, true, GUIStyle.none, GUI.skin.verticalScrollbar, GUIStyle.none, GUILayout.Height(size.y));
			}

			protected void EndScrollView(float clientHeight)
			{
				//Allocate area rect for the scroll view
				GUILayoutUtility.GetRect(size.x, clientHeight);
				EditorGUILayout.EndScrollView();
			}

			protected float GetIndentationOffset()
			{
				float i = indentLevel * 10f;
				if (NeedsExtraIndentation) i += 16;
				return i;
			}
		}

		static Dictionary<string, System.Type> typeDictionary;
		static ObjectSelectorEnhanced instance;

		static System.Type targetType;
		static SerializedObject serializedObject;
		static string serializedPropPath;
		static Tab[] enabledTabs;

		static string searchString = "";

		static float previewAreaHeight = 150;
		static Vector2 scroll;
		static bool initialized;
		static int tabIndex;
		static bool previewAreaSplitterDrag;

		static Tab sceneTab = new SceneObjectsTab();
		static Tab assetsTab = new AssetsTab();

		static GUIContent content = new GUIContent();
		static int indentLevel = 0;
		static bool needsTypeLabel;

		static Editor editor;
		static System.Type cachedEditorType;

		static Tab CurrentTab => enabledTabs[tabIndex];
		static Tab[] AllTabs => new Tab[] { assetsTab, sceneTab };

		[InitializeOnLoadMethod]
		internal static void Init()
		{
			typeDictionary = new Dictionary<string, System.Type>();
			foreach(var a in GetGameAssembliesIncludingUnity())
			{
				foreach(var t in a.GetTypes().Where(t => t.IsSubclassOf(typeof(Component)) || t.IsInterface))
				{
					string ts = t.Name.ToLower();
					if (!typeDictionary.ContainsKey(ts)) typeDictionary.Add(ts, t);
				}
			}
		}

		internal static Tab[] GetEnabledTabsForType(System.Type type)
		{
			if (type == typeof(GameObject)) return new Tab[] { assetsTab, sceneTab };
			else if (typeof(Component).IsAssignableFrom(type)) return new Tab[] { sceneTab };
			else return new Tab[] { assetsTab };
		}

		private static List<Assembly> GetGameAssembliesIncludingUnity()
		{

			string[] excludePrefixes = new string[]
			{
				"mscorlib",
				"System",
				"Mono.",
				"SyntaxTree",
				"netstandard"
			};
			var list = new List<Assembly>();
			foreach(var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
			{
				bool ignore = false;
				foreach(var prefix in excludePrefixes)
				{
					if(assembly.FullName.StartsWith(prefix))
					{
						ignore = true;
						break;
					}
				}
				if(!ignore)
				{
					list.Add(assembly);
				}
			}
			return list;
		}

		public static void Open(SerializedProperty targetProperty)
		{
			//target = targetProperty;
			serializedObject = targetProperty.serializedObject;
			serializedPropPath = targetProperty.propertyPath;
			targetType = GetType(targetProperty);
			enabledTabs = GetEnabledTabsForType(targetType);
			//Always start with the rightmost tab open
			tabIndex = enabledTabs.Length - 1;
			needsTypeLabel = !(targetType == typeof(Transform) || targetType == typeof(GameObject));
			GetWindow<ObjectSelectorEnhanced>(true, $"Select {targetType.Name} ({targetProperty.displayName})", true);
		}

		private void OnGUI()
		{
			try
			{
				indentLevel = 0;
				if (serializedObject == null)
				{
					GUIUtility.ExitGUI();
					Close();
				}

				HandleInputs();

				DrawSearchBar();
				var toolbarRect = DrawToolbar();

				var tabRect = new Rect(0, toolbarRect.yMax, toolbarRect.width, position.height - toolbarRect.yMax - previewAreaHeight);

				CurrentTab.DrawGUI(tabRect);

				GUILayout.FlexibleSpace();

				DrawPreviewArea();
			}
			catch (System.Exception e)
			{
				Debug.LogException(e);
				//Close();
			}
		}

		void DrawPreviewArea()
		{
			previewAreaHeight = Mathf.Clamp(previewAreaHeight, 50, position.height - 100);
			var bottom = GUILayoutUtility.GetRect(instance.position.width, previewAreaHeight);
			bottom.y += bottom.height - previewAreaHeight;
			bottom.height = previewAreaHeight;

			bottom.SplitVertical(Styles.bottomBar.CalcHeight(GUIContent.none, 100) + 2, out var splitterRect, out bottom);

			if (editor)
			{
				if(Event.current.type == EventType.Repaint) Styles.bottomBar.Draw(splitterRect, false, false, false, false);
				//editor.OnInteractivePreviewGUI(bottom, Styles.previewBackground);
				editor.DrawPreview(bottom);
				bottom = bottom.Inset(2, 2, 2, 2);
				CurrentTab.OnPreviewGUIExtras(bottom, GetValue());

				// Splitter events
				if (Event.current != null)
				{
					switch (Event.current.rawType)
					{
						case EventType.MouseDown:
							if (splitterRect.Contains(Event.current.mousePosition))
							{
								//Debug.Log("Start dragging");
								previewAreaSplitterDrag = true;
							}
							break;
						case EventType.MouseDrag:
							if (previewAreaSplitterDrag)
							{
								//Debug.Log("moving splitter");
								previewAreaHeight -= Event.current.delta.y;
								instance.Repaint();
							}
							break;
						case EventType.MouseUp:
							if (previewAreaSplitterDrag)
							{
								//Debug.Log("Done dragging");
								previewAreaSplitterDrag = false;
							}
							break;
					}
				}
			}
			else
			{
				//GUI.DrawTexture(bottom, Texture2D.normalTexture);
			}
		}

		static Rect DrawToolbar()
		{
			var r = GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.toolbar, GUILayout.ExpandWidth(true));
			var fullRect = r;
			if(Event.current.type == EventType.Repaint) EditorStyles.toolbar.Draw(r, false, false, false, false);
			r.width = 60 * enabledTabs.Length;
			tabIndex = GUI.Toolbar(r, tabIndex, enabledTabs.Select(t => t.TabName).ToArray(), Styles.tab);
			r.x += r.width;
			r.width = fullRect.width - r.x;
			CurrentTab.OnHeader(r);
			return fullRect;
		}

		void HandleInputs()
		{
			var current = Event.current;
			if (current.type == EventType.KeyDown)
			{
				switch (current.keyCode)
				{
					case KeyCode.Escape:
						if (searchString.Length > 0)
						{
							searchString = "";
							UpdateSearch();
						}
						else
						{
							instance.Close();
						}
						break;
					case KeyCode.Return: instance.Close(); break;
					case KeyCode.Home: scroll = Vector2.zero; break;
					case KeyCode.End: scroll.y = float.MaxValue; break;
					case KeyCode.PageUp: scroll.y -= 100; break;
					case KeyCode.PageDown: scroll.y += 100; break;
				}
				CurrentTab.HandleInputEvent(current);
			}
		}

		private void DrawSearchBar()
		{
			var searchRect = GUILayoutUtility.GetRect(GUIContent.none, Styles.searchField, GUILayout.ExpandWidth(true));
			GUI.SetNextControlName("search");
			var newSearchString = GUI.TextField(searchRect, searchString, Styles.searchField);
			if (!initialized)
			{
				GUI.FocusControl("search");
				initialized = true;
			}
			searchRect.xMin = searchRect.xMax - 14;
			if (!string.IsNullOrEmpty(newSearchString))
			{
				if (GUI.Button(searchRect, "", Styles.searchFieldEnd))
				{
					newSearchString = "";
				}
			}
			if (searchString != newSearchString)
			{
				searchString = newSearchString;
				UpdateSearch();
			}
		}

		void UpdateSearch()
		{
			foreach(var tab in AllTabs) tab.OnSearchChange();
			Repaint();
		}

		static bool SearchForWords(Object obj, string name, System.Type type, List<string> keywords)
		{
			string typeName = type != null ? obj.GetType().Name : "";
			for (int i = 0; i < keywords.Count; i++)
			{
				if (!SearchForKeyword(obj, name, typeName, keywords[i])) return false;
			}
			return true;
		}

		static bool SearchForKeyword(Object obj, string name, string typeName, string kw)
		{
			if(kw.StartsWith("c:"))
			{
				var go = GetGameObject(obj);
				if (!go) return false;
				kw = kw.Substring(2);
				//if (typeDictionary.TryGetValue(kw, out var type) && go.GetComponent(type)) return true;
				if (go.GetComponent(kw)) return true;
				return false;
			}

			bool searchName = true;
			bool searchType = true;
			if (kw.StartsWith("t:"))
			{
				searchName = false;
				kw = kw.Substring(2);
			}
			else if (kw.StartsWith("n:"))
			{
				searchType = false;
				kw = kw.Substring(2);
			}

			if (searchName && name.IndexOf(kw, System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
			else if (searchType && typeName.IndexOf(kw, System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
			else return false;
		}

		static bool IsSelected(Object obj)
		{
			return obj == GetValue();
		}

		static void SetValue(Object value)
		{
			serializedObject.FindProperty(serializedPropPath).objectReferenceValue = value;
			serializedObject.ApplyModifiedProperties();
			UpdateSelection(value);
		}

		static Object GetValue()
		{
			return serializedObject.FindProperty(serializedPropPath).objectReferenceValue;
		}

		static void UpdateSelection(Object value)
		{
			CurrentTab.OnSelectionChange(value);
			if (editor) DestroyImmediate(editor);
			var go = GetGameObject(value);
			editor = Editor.CreateEditor(go ? go : value);
			instance.Repaint();
		}

		private void OnEnable()
		{
			instance = this;

			foreach (var tab in AllTabs) tab.Init();

			//Find currently assigned object
			var v = GetValue();

			Object editorDummy = v;
			if (v) editor = Editor.CreateEditor(v);

			UpdateSelection(v);
			searchString = "";
			UpdateSearch();
			initialized = false;
		}

		private void OnLostFocus()
		{
			Close();
		}

		private void OnDestroy()
		{
			if(editor) DestroyImmediate(editor);
		}

		static GameObject GetGameObject(Object obj)
		{
			if (obj is GameObject go) return go;
			else if (obj is Component c) return c.gameObject;
			else return null;
		}

		static System.Type GetType(SerializedProperty property)
		{
			var path = property.propertyPath;
			System.Type parentType = property.serializedObject.targetObject.GetType();
			object obj = property.serializedObject.targetObject;
			while (path.Contains("."))
			{
				string root = path.Split('.')[0];
				if (path.StartsWith("Array.data["))
				{
					path = path.Substring("Array.data[".Length);
					string indexString = "";
					while (path[0] != ']')
					{
						indexString += path[0];
						path = path.Substring(1);
					}
					int index = int.Parse(indexString);
					path = path.Substring(1);
					if (path.Length > 0 && path[0] == '.') path = path.Substring(1);
					if (parentType.IsArray)
					{
						//It's a regular array
						var arr = obj as System.Array;
						obj = arr.GetValue(index);
						parentType = obj.GetType();
					}
					else
					{
						//It's a List
						var indexer = GetIndexer(obj.GetType());
						obj = indexer.GetGetMethod().Invoke(obj, new object[] { index });
						parentType = obj.GetType();
					}
					if (path.Length == 0) return parentType;
				}
				else
				{
					obj = parentType.GetField(root).GetValue(obj);
					parentType = obj.GetType();
					path = path.Substring(root.Length + 1);
				}
			}
			return parentType.GetField(path).FieldType;
		}

		static PropertyInfo GetIndexer(System.Type type)
		{
			foreach (PropertyInfo pi in type.GetProperties())
			{
				if (pi.GetIndexParameters().Length > 0)
				{
					return pi;
				}
			}
			return null;
		}

		// This is the preview area at the bottom of the screen
		void PreviewArea()
		{
			var m_TopSize = position.height - 150;
			var m_PreviewSize = 150;
			var kPreviewExpandedAreaHeight = 100;

			GUI.Box(new Rect(0, m_TopSize, position.width, m_PreviewSize), "", Styles.previewBackground);

			/*
			if (m_ListArea.GetSelection().Length == 0)
				return;
			*/

			/*

			EditorWrapper p = null;
			var selectedObject = GetValue();
			if (m_PreviewSize < kPreviewExpandedAreaHeight)
			{
				// Get info string
				string s;
				if (selectedObject != null)
				{
					p = m_EditorCache[selectedObject];
					string typeName = ObjectNames.NicifyVariableName(selectedObject.GetType().Name);
					if (p != null)
						s = p.name + " (" + typeName + ")";
					else
						s = selectedObject.name + " (" + typeName + ")";

					s += "      " + AssetDatabase.GetAssetPath(selectedObject);
				}
				else
					s = "None";

				LinePreview(s, selectedObject, p);
			}
			else
			{
				if (m_EditorCache == null)
					m_EditorCache = new EditorCache(EditorFeatures.PreviewGUI);

				// Get info string
				string s;
				if (selectedObject != null)
				{
					p = m_EditorCache[selectedObject];
					string typeName = ObjectNames.NicifyVariableName(selectedObject.GetType().Name);
					if (p != null)
					{
						s = p.GetInfoString();
						if (s != "")
							s = p.name + "\n" + typeName + "\n" + s;
						else
							s = p.name + "\n" + typeName;
					}
					else
					{
						s = selectedObject.name + "\n" + typeName;
					}

					s += "\n" + AssetDatabase.GetAssetPath(selectedObject);
				}
				else
					s = "None";

				// Make previews
				if (m_ShowWidePreview.faded != 0.0f)
				{
					GUI.color = new Color(1, 1, 1, m_ShowWidePreview.faded);
					WidePreview(m_PreviewSize, s, selectedObject, p);
				}
				if (m_ShowOverlapPreview.faded != 0.0f)
				{
					GUI.color = new Color(1, 1, 1, m_ShowOverlapPreview.faded);
					OverlapPreview(m_PreviewSize, s, selectedObject, p);
				}
				GUI.color = Color.white;
				m_EditorCache.CleanupUntouchedEditors();
			}
			*/
		}
	} 
}
