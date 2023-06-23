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
			private class Section
			{
				public string title;

				public List<Object> objects = new List<Object>();
				public List<Object> filtered = new List<Object>();
				public bool expand = true;

				public Section(string title)
				{
					this.title = title;
				}

				public void ApplyFilter(List<string> keywords)
				{
					filtered.Clear();
					if(keywords != null)
					{
						filtered.AddRange(objects.Where(o => SearchForWords(o, o.name, o.GetType(), keywords)));
					}
					else
					{
						filtered.AddRange(objects);
					}
				}

				public void Clear()
				{
					objects.Clear();
					filtered.Clear();
				}

				public void Set(IEnumerable<Object> o, Section exclude)
				{
					Clear();
					if(exclude != null)
					{
						objects.AddRange(o.Where(x => !exclude.objects.Contains(x)));
					}
					else
					{
						objects.AddRange(o);
					}
					expand = true;
				}
			}

			enum SceneObjectSection { None, All, Self, Children, Parents, Siblings }

			internal override string TabName => "Scene";

			static bool showRanks = false;

			static SceneObjectSection navSection = SceneObjectSection.None;
			static int navIndex = 0;

			static readonly Section inScene = new Section("In Scene");
			static readonly Section onSelf = new Section("On Self");
			static readonly Section inChildren = new Section("In Children");
			static readonly Section inParents = new Section("In Parents");
			static readonly Section inSiblings = new Section("In Siblings");

			int SelectedIndex
			{
				get
				{
					var obj = GetValue();
					if (!obj) return -1;
					int i = inScene.objects.FindIndex(o => o == obj);
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

			internal override void OnInit()
			{
				if (IsTypeSuitable(targetType))
				{
					var go = ((Component)serializedObject.targetObject).gameObject;
					inScene.Set(FindObjectsOfType(targetType).Where(o => GetGameObject(o).scene == go.scene).OrderBy(o => o.name), null);

					if (targetType == typeof(GameObject))
					{
						onSelf.objects.Add(go);
						inChildren.Set(go.GetComponentsInChildren<Transform>().Select(t => t.gameObject), onSelf);
						inParents.Set(go.GetComponentsInParent<Transform>().Select(t => t.gameObject), onSelf);
						inSiblings.Set(GetComponentsInSiblings(go, typeof(Transform), true).Select(t => t.gameObject), null);
					}
					else
					{
						onSelf.Set(go.GetComponents(targetType), null);
						inChildren.Set(go.GetComponentsInChildren(targetType), onSelf);
						inParents.Set(go.GetComponentsInParent(targetType), onSelf);
						inSiblings.Set(GetComponentsInSiblings(go, targetType, true), null);
					}
				}

				UpdatePosition(true);
			}

			private void UpdatePosition(bool updateScrollingPos)
			{

				int pos = 0;
				var v = GetValue();

				navSection = SceneObjectSection.None;
				navIndex = 0;

				if (v == null || onSelf.objects.Contains(v))
				{
					//Do nothing, it's on (or very near to) the top
					navSection = SceneObjectSection.Self;
					navIndex = onSelf.objects.IndexOf(v);
				}
				else
				{
					int index = inChildren.objects.IndexOf(v);
					if (index >= 0)
					{
						//It's a child object
						navSection = SceneObjectSection.Children;
						navIndex = index;
						pos = 3 + onSelf.objects.Count + index;
						inChildren.expand = true;
					}
					else
					{
						index = inParents.objects.IndexOf(v);
						if (index >= 0)
						{
							//It's a parent object
							navSection = SceneObjectSection.Parents;
							navIndex = index;
							pos = 4 + onSelf.objects.Count + index;
							if (inChildren.expand) pos += inChildren.objects.Count;
							inParents.expand = true;
						}
						else
						{
							index = inSiblings.objects.IndexOf(v);
							if(index >= 0)
							{
								//It's a sibling (or siblings child) object
								navSection = SceneObjectSection.Siblings;
								navIndex = index;
								pos = 5 + onSelf.objects.Count + index;
								if(inChildren.expand) pos += inChildren.objects.Count;
								if(inParents.expand) pos += inParents.objects.Count;
								inSiblings.expand = true;
							}
							else
							{
								//It's neither a child nor parent object
								index = inScene.objects.IndexOf(v);
								navSection = SceneObjectSection.All;
								navIndex = index;
								pos = 7 + onSelf.objects.Count + index;
								if(inChildren.expand) pos += inChildren.objects.Count;
								if(inParents.expand) pos += inParents.objects.Count;
								if(inSiblings.expand) pos += inSiblings.objects.Count;
							}
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

				for(int i = 0; i < onSelf.filtered.Count; i++)
				{
					DrawItem(onSelf.filtered[i], "(Self)");
				}

				DrawSubSection(inChildren, targetTransform);

				DrawSubSection(inParents, targetTransform);

				DrawSubSection(inSiblings, null);

				Separator();

				DrawSubSection(inScene, null);

				GUI.enabled = true;

				EndScrollView(listIndex * listItemHeight);
			}

			private void DrawSubSection(Section section, Transform targetTransform)
			{
				GUI.enabled = section.filtered.Count > 0;
				section.expand = EditorGUI.Foldout(NextListRect(), section.expand, $"{section.title} [{section.filtered.Count}/{section.objects.Count}]");
				if(section.expand)
				{
					indentLevel++;
					for(int i = 0; i < section.filtered.Count; i++)
					{
						string depthStr = null;
						if(targetTransform && showRanks)
						{
							depthStr = GetChildDepth(GetGameObject(section.filtered[i]).transform, targetTransform) + ".";
						}
						DrawItem(section.filtered[i], "", depthStr);
					}
					indentLevel--;
				}
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

				bool isSelected = obj ? IsSelected(obj) : GetValue() == null;
				if(Event.current.type == EventType.Repaint)
				{
					var style = isSelected ? Styles.listItemSelected : Styles.listItem;

					style.Draw(rect, "", false, false, false, false);
					rect.xMin += 4 + GetIndentationOffset();
					rect.SplitHorizontal(16, out var rIcon, out var rLabel, 2);
					rIcon.width = 16;
					rIcon.height = rIcon.width;

					var nameLabelStyle = isSelected ? Styles.nameLabelSelected : Styles.nameLabel;
					var typeLabelStyle = isSelected ? Styles.typeLabelSelected : Styles.typeLabel;

					DrawIcon(rIcon, icon, isSelected, true);
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
				else if (sec == SceneObjectSection.Children) return inChildren.filtered;
				else if (sec == SceneObjectSection.Parents) return inParents.filtered;
				else if (sec == SceneObjectSection.Siblings) return inSiblings.filtered;
				else return inScene.filtered;
			}

			internal override void OnSearchChange()
			{
				List<string> keywords = !string.IsNullOrWhiteSpace(searchString)
					? new List<string>(searchString.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries))
					: null;
				onSelf.ApplyFilter(keywords);
				inChildren.ApplyFilter(keywords);
				inParents.ApplyFilter(keywords);
				inSiblings.ApplyFilter(keywords);
				inScene.ApplyFilter(keywords);
			}

			internal override void OnSelectionChange(Object newSelection)
			{

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

			static Component[] GetComponentsInSiblings(GameObject go, System.Type type, bool includeChildren)
			{
				var t = go.transform;
				if(t.parent != null)
				{
					List<Component> comps = new List<Component>();
					var ownIndex = t.GetSiblingIndex();
					var parent = t.parent;
					for(int i = 0; i < parent.childCount; i++)
					{
						if(i == ownIndex) continue;
						if(includeChildren)
						{
							comps.AddRange(parent.GetChild(i).GetComponentsInChildren(type, true));
						}
						else
						{
							comps.AddRange(parent.GetChild(i).GetComponents(type));
						}
					}
					return comps.ToArray();
				}
				else
				{
					return new Component[0];
				}
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
