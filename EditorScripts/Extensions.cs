using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace AdvancedObjectSelector
{
	internal static class Extensions
	{
		public static void SplitHorizontal(this Rect r, float leftRectWidth, out Rect left, out Rect right, float margin = 0)
		{
			left = new Rect(r)
			{
				width = leftRectWidth - margin * 0.5f
			};
			right = new Rect(r)
			{
				xMin = r.xMin + leftRectWidth + margin * 0.5f
			};
		}

		public static void SplitVertical(this Rect r, float topRectHeight, out Rect top, out Rect bottom, float margin = 0)
		{
			top = new Rect(r)
			{
				height = topRectHeight - margin * 0.5f
			};
			bottom = new Rect(r)
			{
				yMin = r.yMin + topRectHeight + margin * 0.5f
			};
		}

		public static Rect Inset(this Rect r, float left, float right, float top, float bottom)
		{
			return new Rect(r.x + left, r.y + top, r.width - left - right, r.height - top - bottom);
		}

		public static string GetHierarchyPathString(this Transform transform, Transform parent = null)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(transform.name);
			var t = transform;
			while(t.parent != null && t.parent != parent)
			{
				t = t.parent;
				sb.Insert(0, t.name + "/");
			}
			return sb.ToString();
		}
	} 
}
