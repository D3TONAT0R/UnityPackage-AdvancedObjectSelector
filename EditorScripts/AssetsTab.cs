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

			class Asset
			{
				public readonly string guid;
				public readonly string assetName;
				private Object obj;
				private string path;
				private Texture2D thumbnail;

				private bool isLoadingThumbnail;

				public Asset(string guid)
				{
					this.guid = guid;
					path = AssetDatabase.GUIDToAssetPath(guid);
					assetName = System.IO.Path.GetFileNameWithoutExtension(path);
				}

				public Object GetAssetObject()
				{
					if(obj) return obj;
					obj = AssetDatabase.LoadAssetAtPath(path, targetType);
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
				projectAssets.Clear();
				foreach(var g in AssetDatabase.FindAssets("t:" + targetType.Name, new string[] { "Assets" }))
				{
					projectAssets.Add(new Asset(g));
				}
				packageAssets.Clear();
				foreach(var g in AssetDatabase.FindAssets("t:" + targetType.Name, new string[] { "Packages" }))
				{
					packageAssets.Add(new Asset(g));
				}
				elipsisWidth = Styles.nameLabel.CalcSize(new GUIContent("...")).x - Styles.nameLabel.padding.horizontal;
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
				var gs = GetGridItemSize();
				Rect rect;
				if(gs.x > 0)
				{
					rect = NextGridRect(size.x);
				}
				else
				{
					rect = NextListRect();
				}
								
				if(Event.current.type == EventType.Repaint) lastItemRect = rect;

				if(rect.yMax < scroll.y || rect.yMin > scroll.y + size.y + 40)
				{
					//The item is not visible, skip it
					return;
				}

				Texture icon;
				if(asset != null)
				{
					if(gs.x > 0)
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
						content.text = $"{asset.assetName}";
						content.tooltip = "";
						icon = AssetDatabase.GetCachedIcon(AssetDatabase.GUIDToAssetPath(asset.guid));
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

					if(gs.x > 0) rect = rect.Inset(4, 4, 4, 4);

					style.Draw(rect, "", false, false, false, false);
					rect.xMin += GetIndentationOffset() + 5;

					Rect rIcon, rLabel;

					if(gs.x > 0)
					{
						rect.SplitVertical(rect.height - listItemHeight, out rIcon, out rLabel);
					}
					else
					{
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
						obj = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(asset.guid), targetType);
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
					return AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(currentValue)) == a.guid;
				}
			}

			internal override void OnSearchChange()
			{
				projectAssetsFiltered.Clear();
				packageAssetsFiltered.Clear();
				if(!string.IsNullOrEmpty(searchString))
				{
					var projectGuids = AssetDatabase.FindAssets(searchString, new string[] { "Assets" });
					var packageGuids = AssetDatabase.FindAssets(searchString, new string[] { "Packages" });
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
