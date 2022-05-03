using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace AdvancedObjectSelector
{
	internal partial class ObjectSelectorEnhanced
	{
		internal class AssetsTab : Tab
		{
			const string elipsis = "...";

			/*
			private static readonly string[] builtinMeshes = new string[]
			{
				"Cube",
				"Capsule",
				"Cylinder",
				"Plane",
				"Sphere",
				"Quad"
			};
			*/

			private static Object[] builtinAssets;

			class Asset
			{
				public string guid;
				public string assetName;
				public string path;
				public bool isSubAsset = false;
				public bool isBuiltinAsset = false;
				private Object obj;
				private Texture2D thumbnail;

				private bool isLoadingThumbnail;

				public bool IsVisible
				{
					get => visible;
					set
					{
						if(value != visible)
						{
							LoadPreviewAsync();
						}
						visible = value;
					}
				}
				private bool visible;

				private Asset() { }

				public static Asset CreateAsset(string guid)
				{
					var path = AssetDatabase.GUIDToAssetPath(guid);
					return new Asset()
					{
						guid = guid,
						path = path,
						assetName = System.IO.Path.GetFileNameWithoutExtension(path)
					};
				}

				public static Asset CreateSubAsset(Object subAsset, string parentGuid)
				{
					var path = AssetDatabase.GUIDToAssetPath(parentGuid);

					return new Asset()
					{
						isSubAsset = true,
						guid = parentGuid,
						assetName = subAsset.name,
						path = AssetDatabase.GUIDToAssetPath(parentGuid),
						obj = subAsset
					};
				}

				public static Asset CreateBuiltinAsset(Object builtinAsset)
				{
					AssetDatabase.TryGetGUIDAndLocalFileIdentifier(builtinAsset, out var guid, out long _);
					return new Asset()
					{
						isBuiltinAsset = true,
						guid = AssetDatabase.GUIDToAssetPath(guid),
						assetName = builtinAsset.name,
						obj = builtinAsset
					};
				}

				public static Asset CreateBuiltinAsset<T>(string name) where T : Object
				{
					var builtinAsset = Resources.GetBuiltinResource<T>(name);
					if (!builtinAsset) throw new System.NullReferenceException();
					AssetDatabase.TryGetGUIDAndLocalFileIdentifier(builtinAsset, out var guid, out long _);
					return new Asset()
					{
						isBuiltinAsset = true,
						guid = AssetDatabase.GUIDToAssetPath(guid),
						assetName = System.IO.Path.GetFileNameWithoutExtension(name),
						obj = builtinAsset
					};
				}

				public Object GetAssetObject()
				{
					if(obj) return obj;
					obj = AssetDatabase.LoadAssetAtPath(path, targetType);
					return obj;
				}

				public Object GetSubAssetObject()
				{
					return obj;
				}

				public bool IsAssetLoaded() => obj != null;

				public Texture2D GetThumbnail()
				{
					if(!thumbnail && !isLoadingThumbnail)
					{
						LoadPreviewAsync();
					}
					return thumbnail;
				}

				void LoadPreviewAsync()
				{
					isLoadingThumbnail = true;
					//var thread = new Thread(() =>
					//{
						obj = GetAssetObject();
						thumbnail = AssetPreview.GetAssetPreview(obj);
					if(thumbnail == null)
					{
						thumbnail = AssetPreview.GetMiniThumbnail(obj);
					}
						//if(!thumbnail) thumbnail = AssetPreview.GetMiniThumbnail(obj);
						isLoadingThumbnail = false;
					//});
					//thread.Start();
				}
			}

			static float elipsisWidth;

			static List<Asset> projectAssets = new List<Asset>();
			static List<Asset> packageAssets = new List<Asset>();

			static Object currentValue;
			static List<Asset> projectAssetsFiltered = new List<Asset>();
			static List<Asset> packageAssetsFiltered = new List<Asset>();

			static bool includeAssets = true;
			static bool includePackageContents = false;

			static int zoomLevel = 0;

			static Rect lastItemRect;

			internal override string TabName => "Assets";

			internal override void Init()
			{
				if(builtinAssets == null)
				{
					List<Object> builtins = new List<Object>();
					builtins.AddRange(AssetDatabase.LoadAllAssetsAtPath("Resources/unity_builtin_extra"));
					builtins.AddRange(AssetDatabase.LoadAllAssetsAtPath("Library/unity default resources"));
					builtinAssets = builtins.ToArray();
				}

				projectAssets.Clear();
				foreach(var g in AssetDatabase.FindAssets("t:" + targetType.Name, new string[] { "Assets" }))
				{
					AddAssetAndSubAssets(g, projectAssets);
				}
				packageAssets.Clear();
				foreach(var g in AssetDatabase.FindAssets("t:" + targetType.Name, new string[] { "Packages" }))
				{
					AddAssetAndSubAssets(g, packageAssets);
				}

				foreach(var b in builtinAssets)
				{
					if(targetType.IsAssignableFrom(b.GetType()))
					{
						projectAssets.Add(Asset.CreateBuiltinAsset(b));
					}
				}
				/*
				if(targetType == typeof(Mesh))
				{
					//Add default meshes
					foreach(var m in builtinMeshes)
					{
						projectAssets.Add(Asset.CreateBuiltinAsset<Mesh>(m + ".fbx"));
					}
				}
				*/
				elipsisWidth = Styles.nameLabel.CalcSize(new GUIContent("...")).x - Styles.nameLabel.padding.horizontal;
			}

			private void AddAssetAndSubAssets(string guid, List<Asset> list)
			{
				if(list.FindIndex(a => a.guid == guid) < 0)
				{
					var asset = Asset.CreateAsset(guid);
					
					var subAssets = AssetDatabase.LoadAllAssetRepresentationsAtPath(asset.path);
					if(subAssets.Length > 0)
					{
						var obj = AssetDatabase.LoadMainAssetAtPath(asset.path);
						if(targetType.IsAssignableFrom(obj.GetType()))
						{
							list.Add(asset);
						}
						foreach (var su in subAssets)
						{
							if(su.GetType() == targetType)
							{
								list.Add(Asset.CreateSubAsset(su, guid));
							}
						}
					}
					else
					{
						list.Add(asset);
					}
				}
			}

			protected override void OnGUI()
			{
				currentValue = GetValue();
				BeginScrollView();
				DrawItem(null);
				bool grid = GetGridItemSize().x > 0;
				if(includeAssets) foreach(var a in projectAssetsFiltered) DrawItem(a);
				if(!grid && includeAssets && includePackageContents) Separator();
				if(includePackageContents) foreach(var a in packageAssetsFiltered) DrawItem(a);
				EndScrollView(lastItemRect.yMax);
			}

			internal override void OnHeader(Rect rect)
			{
				rect.width -= 10;
				rect.SplitHorizontal(rect.width - 160, out _, out rect);
				rect.SplitHorizontal(rect.width - 60, out var sliderRect, out rect);
				rect.SplitHorizontal(rect.width * 0.5f, out var ra, out var rp);

				sliderRect.SplitHorizontal(15, out var sliderLL, out sliderRect);
				sliderRect.SplitHorizontal(sliderRect.width - 15, out sliderRect, out var sliderLR);
				zoomLevel = Mathf.RoundToInt(GUI.HorizontalSlider(sliderRect, zoomLevel, 0, 15));
				GUI.Label(sliderLL, "-");
				GUI.Label(sliderLR, "+");

				includeAssets = GUI.Toggle(ra, includeAssets, "A", EditorStyles.toolbarButton);
				includePackageContents = GUI.Toggle(rp, includePackageContents, "P", EditorStyles.toolbarButton);
			}

			void DrawItem(Asset asset)
			{
				var gridItemSize = GetGridItemSize();
				bool isGrid = gridItemSize.x > 0;
				Rect rect;
				if(isGrid)
				{
					rect = NextGridRect(size.x);
				}
				else
				{
					rect = NextListRect();
				}
								
				if(Event.current.type == EventType.Repaint) lastItemRect = rect;

				bool visible = rect.yMax >= scroll.y || rect.yMin >= scroll.y + size.y + 40;
				if (asset != null && Event.current.type == EventType.Repaint) asset.IsVisible = visible;
				if (!visible)
				{
					//The item is not visible, skip it
					return;
				}

				Texture icon;
				if(asset != null)
				{
					if(isGrid)
					{
						string s = GetShortenedString(asset.assetName, rect.width - 8, Styles.nameLabel, out bool isShortened);
						content.text = s;
						content.tooltip = isShortened ? asset.assetName : "";
						if(EditorGUIUtility.HasObjectThumbnail(targetType))
						{
							icon = asset.GetThumbnail();
						}
						else
						{
							//icon = AssetDatabase.GetCachedIcon(AssetDatabase.GUIDToAssetPath(asset.guid));
							icon = asset.GetThumbnail();
						}
					}
					else
					{
						content.text = $"{asset.assetName}";// {(asset.isSubAsset ? "*" : "")}";
						content.tooltip = "";
						if (asset.isBuiltinAsset)
						{
							icon = asset.GetThumbnail();
						}
						else
						{
							icon = AssetDatabase.GetCachedIcon(AssetDatabase.GUIDToAssetPath(asset.guid));
						}
					}
				}
				else
				{
					content.text = "None";
					icon = null;
				}

				bool isSelected = asset != null ? IsSelected(asset) : currentValue == null;
				if(Event.current.type == EventType.Repaint)
				{
					var style = isSelected ? Styles.listItemSelected : Styles.listItem;

					if(isGrid) rect = rect.Inset(4, 4, 4, 4);

					style.Draw(rect, "", false, false, false, false);

					Rect rIcon, rLabel;

					if(isGrid)
					{
						rect.SplitVertical(rect.height - listItemHeight, out rIcon, out rLabel);
					}
					else
					{
						rect.xMin += GetIndentationOffset() + 5;
						rect.SplitHorizontal(rect.height, out rIcon, out rLabel, 2);
					}

					var nameLabelStyle = isSelected ? Styles.nameLabelSelected : Styles.nameLabel;
					var typeLabelStyle = isSelected ? Styles.typeLabelSelected : Styles.typeLabel;

					if(icon) GUI.DrawTexture(rIcon, icon, ScaleMode.ScaleToFit);

					//EditorGUIUtility.get

					GUI.Label(rLabel, content, nameLabelStyle);

					/*
					if(needsTypeLabel && obj)
					{
						content.text = obj.GetType().Name;
						var w = Styles.typeLabel.CalcSize(content).x;
						rLabel = rLabel.Inset(2);
						rLabel.xMin = rLabel.xMax - w;
						GUI.Label(rLabel, content, typeLabelStyle);
					}
					*/
				}

				if(GUI.Button(rect, "", GUIStyle.none))
				{
					if(isSelected)
					{
						//Very primitive double click detection, might be changed in the future.
						instance.Close();
						return;
					}
					Object obj;
					if(asset != null)
					{
						obj = asset.GetAssetObject();
					}
					else
					{
						obj = null;
					}
					SetValue(obj);
					CurrentTab.OnValueChange(obj);
				}
			}

			Rect NextGridRect(float width)
			{
				var s = GetGridItemSize();
				int h = GetHorizontalItemCount(width);
				int y = (int)(listIndex / (float)h);
				int x = listIndex % h;
				listIndex++;
				return new Rect(x * s.x, y * s.y, s.x, s.y);
			}

			Vector2 GetGridItemSize()
			{
				if(zoomLevel == 0) return Vector2.zero;
				int s = zoomLevel + 4;
				return new Vector2(s * 8, s * 8 + listItemHeight);
			}

			int GetHorizontalItemCount(float width)
			{
				return Mathf.Clamp(Mathf.FloorToInt(width / GetGridItemSize().x), 1, 16);
			}

			bool IsSelected(Asset a)
			{
				if(!currentValue) {
					return a == null;
				}
				else
				{
					if (a.isSubAsset)
					{
						return currentValue == a.GetSubAssetObject();
					}
					else
					{
						return AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(currentValue)) == a.guid;
					}
				}
			}

			internal override void OnSearchChange()
			{
				projectAssetsFiltered.Clear();
				packageAssetsFiltered.Clear();
				if(!string.IsNullOrEmpty(searchString))
				{
					var projectGuids = AssetDatabase.FindAssets(searchString, new string[] { "Assets" }).Distinct().ToArray();
					var packageGuids = AssetDatabase.FindAssets(searchString, new string[] { "Packages" }).Distinct().ToArray();
					foreach(var guid in projectGuids)
					{
						Asset asset = projectAssets.FirstOrDefault(a => a.guid == guid);
						if(asset != null) projectAssetsFiltered.Add(asset);
					}
					foreach(var guid in packageGuids)
					{
						Asset asset = packageAssets.FirstOrDefault(a => a.guid == guid);
						if (asset != null) packageAssetsFiltered.Add(asset);
					}
				}
				else
				{
					projectAssetsFiltered.AddRange(projectAssets);
					packageAssetsFiltered.AddRange(packageAssets);
				}
			}

			private static void ApplySearchFilter()
			{

			}

			private string GetShortenedString(string str, float maxWidth, GUIStyle style, out bool isShortened)
			{
				if(Event.current.type != EventType.Repaint)
				{
					isShortened = false;
					return str;
				}
				isShortened = false;
				var c = new GUIContent(str);
				var w = style.CalcSize(c).x;
				if(w > maxWidth)
				{
					StringBuilder sb = new StringBuilder(str);
					while(sb.Length > 0 && w > maxWidth)
					{
						isShortened = true;
						sb.Remove(sb.Length - 1, 1);
						c.text = sb.ToString() + elipsis;
						w = style.CalcSize(c).x;
					}
					return sb.ToString() + elipsis;
				}
				else
				{
					return str;
				}
			}
		}
	}
}
