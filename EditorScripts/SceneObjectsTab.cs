using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AdvancedObjectSelector
{
	internal partial class ObjectSelectorEnhanced
	{
		internal class SceneObjectsTab : Tab
		{
			enum SceneObjectSection { None, All, Self, Children, Parents }

			internal override string TabName => "Scene";

			static bool expandInChildren = false;
			static bool expandInParents = false;
			static bool expandAll = true;
			static bool showRanks = false;

			static Texture2D selectedObjectIcon;
			static SceneObjectSection navSection = SceneObjectSection.None;
			static int navIndex = 0;

			static List<Object> allObjects = new List<Object>();
			static List<Object> inChildren = new List<Object>();
			static List<Object> inParents = new List<Object>();
			static List<Object> onSelf = new List<Object>();

			static List<Object> allObjectsFiltereed = new List<Object>();
			static List<Object> inChildrenFiltered = new List<Object>();
			static List<Object> inParentsFiltered = new List<Object>();
			static List<Object> onSelfFiltered = new List<Object>();

			int SelectedIndex
			{
				get
				{
					var obj = GetValue();
					if (!obj) return -1;
					int i = allObjects.FindIndex(o => o == obj);
					if (i >= 0)
					{
						return i;
					}
					else
					{
						//This shouldn't happen usually
						return int.MaxValue;
					}
				}
			}

			internal override bool NeedsExtraIndentation => showRanks;

			public bool IsTypeSuitable(System.Type type)
			{
				return type.IsSubclassOf(typeof(Component)) || type == typeof(Component) || type == typeof(GameObject);
			}

			internal override void Init()
			{
				allObjects.Clear();
				onSelf.Clear();
				inChildren.Clear();
				inParents.Clear();
				if (IsTypeSuitable(targetType))
				{
					var go = ((Component)serializedObject.targetObject).gameObject;
					allObjects.AddRange(FindObjectsOfType(targetType).Where(o => GetGameObject(o).scene == go.scene));

					//if (serializedObject.targetObject)
					//{
					if (targetType == typeof(GameObject))
					{
						onSelf.Add(go);
						inChildren.AddRange(go.GetComponentsInChildren<Transform>().Select(t => t.gameObject));
						inParents.AddRange(go.GetComponentsInParent<Transform>().Select(t => t.gameObject));
					}
					else
					{
						onSelf.AddRange(go.GetComponents(targetType));
						inChildren.AddRange(go.GetComponentsInChildren(targetType));
						inParents.AddRange(go.GetComponentsInParent(targetType));
					}
					inChildren.RemoveAll(c => onSelf.Contains(c));
					inParents.RemoveAll(c => onSelf.Contains(c));
					//}
				}
				expandInChildren = false;
				expandInParents = false;
				expandAll = true;

				UpdatePosition(true);
			}

			private void UpdatePosition(bool updateScrollingPos)
			{

				int pos = 0;
				var v = GetValue();

				navSection = SceneObjectSection.None;
				navIndex = 0;

				if (v == null || onSelf.Contains(v))
				{
					//Do nothing, it's on (or very near to) the top
					navSection = SceneObjectSection.Self;
					navIndex = onSelf.IndexOf(v);
				}
				else
				{
					int index = inChildren.IndexOf(v);
					if (index >= 0)
					{
						//It's a child object
						navSection = SceneObjectSection.Children;
						navIndex = index;
						pos = 3 + onSelf.Count + index;
						expandInChildren = true;
					}
					else
					{
						index = inParents.IndexOf(v);
						if (index >= 0)
						{
							//It's a parent object
							navSection = SceneObjectSection.Parents;
							navIndex = index;
							pos = 4 + onSelf.Count + index;
							if (expandInChildren) pos += inChildren.Count;
							expandInParents = true;
						}
						else
						{
							//It's neither a child nor parent object
							index = allObjects.IndexOf(v);
							navSection = SceneObjectSection.All;
							navIndex = index;
							pos = 7 + onSelf.Count + index;
							if (expandInChildren) pos += inChildren.Count;
							if (expandInParents) pos += inParents.Count;
						}
					}
					if(updateScrollingPos) scroll.y = pos * listItemHeight - (instance.position.height - previewAreaHeight - 40) * 0.5f;
				}
			}

			protected override void OnGUI()
			{
				BeginScrollView();

				Separator();

				DrawItem(null);

				Separator();

				var targetTransform = GetGameObject(serializedObject.targetObject).transform;

				for (int i = 0; i < onSelfFiltered.Count; i++)
				{
					DrawItem(onSelfFiltered[i], "(Self)");
				}

				GUI.enabled = inChildrenFiltered.Count > 0;
				expandInChildren = EditorGUI.Foldout(NextListRect(), expandInChildren, $"In Children [{inChildrenFiltered.Count}/{inChildren.Count}]");
				if (expandInChildren)
				{
					indentLevel++;
					for (int i = 0; i < inChildrenFiltered.Count; i++)
					{
						string depthStr = null;
						if (showRanks)
						{
							depthStr = GetChildDepth(GetGameObject(inChildrenFiltered[i]).transform, targetTransform) + ".";
						}
						DrawItem(inChildrenFiltered[i], "", depthStr);
					}
					indentLevel--;
				}

				GUI.enabled = inParentsFiltered.Count > 0;
				expandInParents = EditorGUI.Foldout(NextListRect(), expandInParents, $"In Parents [{inParentsFiltered.Count}/{inParents.Count}]");
				if (expandInParents)
				{
					indentLevel++;
					for (int i = 0; i < inParentsFiltered.Count; i++)
					{
						string depthStr = null;
						if (showRanks)
						{
							depthStr = GetParentDepth(GetGameObject(inParentsFiltered[i]).transform, targetTransform) + ".";
						}
						DrawItem(inParentsFiltered[i], "", depthStr);
					}
					indentLevel--;
				}

				Separator();

				GUI.enabled = allObjectsFiltereed.Count > 0;
				expandAll = EditorGUI.Foldout(NextListRect(), expandAll, $"In Scene [{allObjectsFiltereed.Count}/{allObjects.Count}]");
				if (expandAll)
				{
					indentLevel++;
					for (int i = 0; i < allObjectsFiltereed.Count; i++)
					{
						string hierarchicalDepthStr = null;
						if (showRanks)
						{
							hierarchicalDepthStr = GetHierarchicalDepth(GetGameObject(allObjectsFiltereed[i]).transform) + ".";
						}
						DrawItem(allObjectsFiltereed[i], "", hierarchicalDepthStr);
					}
					indentLevel--;
				}
				GUI.enabled = true;

				EndScrollView(listIndex * listItemHeight);
			}

			void DrawItem(Object obj, string prefix = "", string frontLabel = null)
			{
				var rect = NextListRect();

				if(rect.yMax < scroll.y || rect.yMin > scroll.y + size.y + 40)
				{
					//The item is not visible, skip it
					return;
				}

				Texture icon;
				if(obj)
				{
					content.text = $"{obj.name}";
					icon = EditorGUIUtility.ObjectContent(obj, obj.GetType()).image;
				}
				else
				{
					content.text = "None";
					icon = null;
				}
				if(!string.IsNullOrEmpty(prefix)) content.text = prefix + " " + content.text;
				//var rect = GUILayoutUtility.GetRect(content, Styles.listItem, GUILayout.ExpandWidth(true), GUILayout.Height(EditorStyles.label.CalcHeight(content, 1000) + 2));

				bool isSelected = obj ? IsSelected(obj) : GetValue() == null;
				if(Event.current.type == EventType.Repaint)
				{
					var style = isSelected ? Styles.listItemSelected : Styles.listItem;

					style.Draw(rect, "", false, false, false, false);
					rect.xMin += GetIndentationOffset();
					rect.SplitHorizontal(rect.height, out var rIcon, out var rLabel, 2);

					if(isSelected && icon) icon = selectedObjectIcon;
					var nameLabelStyle = isSelected ? Styles.nameLabelSelected : Styles.nameLabel;
					var typeLabelStyle = isSelected ? Styles.typeLabelSelected : Styles.typeLabel;

					if(icon) GUI.DrawTexture(rIcon, icon, ScaleMode.ScaleToFit);
					GUI.Label(rLabel, content, nameLabelStyle);

					if(needsTypeLabel && obj)
					{
						content.text = obj.GetType().Name;
						var w = Styles.typeLabel.CalcSize(content).x;
						rLabel = rLabel.Inset(2, 2, 2, 2);
						rLabel.xMin = rLabel.xMax - w;
						GUI.Label(rLabel, content, typeLabelStyle);
					}

					if(!string.IsNullOrEmpty(frontLabel))
					{
						var r = rect.Inset(2, 2, 2, 2);
						r.x -= GetIndentationOffset();
						r.width = r.height;
						GUI.Label(r, frontLabel, typeLabelStyle);
					}
				}

				if(GUI.Button(rect, "", GUIStyle.none))
				{
					if(isSelected)
					{
						//Very primitive double click detection, might be changed in the future.
						instance.Close();
						return;
					}
					SetValue(obj);
					CurrentTab.OnValueChange(obj);
				}
			}

			internal override void HandleInputEvent(Event keyDownEvent)
			{
				if (navSection != SceneObjectSection.None)
				{
					var list = GetFilteredListByType(navSection);
					if (Event.current.keyCode == KeyCode.DownArrow)
					{
						navIndex++;
						navIndex %= list.Count;
						SetValue(list[navIndex]);
					}
					else if (Event.current.keyCode == KeyCode.UpArrow)
					{
						navIndex--;
						if (navIndex < 0) navIndex += list.Count;
						SetValue(list[navIndex]);
					}
				}
			}

			List<Object> GetFilteredListByType(SceneObjectSection sec)
			{
				if (sec == SceneObjectSection.None) return null;
				else if (sec == SceneObjectSection.Children) return inChildrenFiltered;
				else if (sec == SceneObjectSection.Parents) return inParentsFiltered;
				else return allObjectsFiltereed;
			}

			internal override void OnSearchChange()
			{
				allObjectsFiltereed.Clear();
				inChildrenFiltered.Clear();
				inParentsFiltered.Clear();
				onSelfFiltered.Clear();
				if (!string.IsNullOrEmpty(searchString))
				{
					var keywords = new List<string>(searchString.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries));
					allObjectsFiltereed.AddRange(allObjects.Where(	o => SearchForWords(o, o.name, o.GetType(), keywords)));
					inChildrenFiltered.AddRange(inChildren.Where(	o => SearchForWords(o, o.name, o.GetType(), keywords)));
					inParentsFiltered.AddRange(inParents.Where(		o => SearchForWords(o, o.name, o.GetType(), keywords)));
					onSelfFiltered.AddRange(onSelf.Where(			o => SearchForWords(o, o.name, o.GetType(), keywords)));
				}
				else
				{
					allObjectsFiltereed.AddRange(allObjects);
					inChildrenFiltered.AddRange(inChildren);
					inParentsFiltered.AddRange(inParents);
					onSelfFiltered.AddRange(onSelf);
				}
			}

			internal override void OnSelectionChange(Object newSelection)
			{
				if(newSelection != null)
				{
					var src = EditorGUIUtility.ObjectContent(newSelection, newSelection.GetType()).image as Texture2D;
					if(src)
					{
						
						selectedObjectIcon = new Texture2D(src.width, src.height, src.format, src.mipmapCount, false);
						Graphics.CopyTexture(src, 0, 0, selectedObjectIcon, 0, 0);
						var pixels = selectedObjectIcon.GetPixels32();
						for(int i = 0; i < pixels.Length; i++)
						{
							pixels[i].r = 255;
							pixels[i].g = 255;
							pixels[i].b = 255;
						}
						selectedObjectIcon.SetPixels32(pixels);
						selectedObjectIcon.Apply();
					}
				}
			}

			internal override void OnValueChange(Object newValue)
			{
				UpdatePosition(false);
			}

			internal override void OnPreviewGUIExtras(Rect rect, Object obj)
			{
				rect.SplitVertical(rect.height - 20, out _, out rect);
				Transform t = null;
				if (obj is Transform)
				{
					t = obj as Transform;
				}
				else if (obj is GameObject go)
				{
					t = go.transform;
				}
				else if (obj is Component comp)
				{
					t = comp.transform;
				}
				if (t) GUI.Label(rect, t.GetHierarchyPathString(), EditorStyles.whiteMiniLabel);
			}

			internal override void OnHeader(Rect rect)
			{
				rect.SplitHorizontal(rect.width - 50, out _, out var rankRect);
				rankRect.x -= 10;
				showRanks = GUI.Toggle(rankRect, showRanks, "Ranks", EditorStyles.toolbarButton);
			}

			static int GetChildDepth(Transform child, Transform target)
			{
				int i = 0;
				while (child != target)
				{
					i++;
					child = child.parent;
					if (child == null) throw new System.ArgumentException("Transform was not a child of target object.");
				}
				return i;
			}

			static int GetParentDepth(Transform parent, Transform target)
			{
				int i = 0;
				while (target != parent)
				{
					i++;
					target = target.parent;
					if (target == null) throw new System.ArgumentException("Transform was not a parent of target object.");
				}
				return i;
			}

			static int GetHierarchicalDepth(Transform t)
			{
				int i = 0;
				while(t != null)
				{
					i++;
					t = t.parent;
				}
				return i;
			}
		}
	}
}
