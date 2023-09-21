using UnityEngine;
using UnityEditor;
using System.Reflection;
using System;

namespace AdvancedObjectSelector
{
	public static class AssetPreviewLoader
	{
		private static MethodInfo loadPreviewMethod;

		private static readonly object[] loadPreviewMethodParams = new object[1];

		public static Texture2D LoadPreviewFromGUID(string guid)
		{
			if(loadPreviewMethod == null)
			{
				loadPreviewMethod = typeof(AssetPreview).GetMethod("GetAssetPreviewFromGUID", BindingFlags.Static | BindingFlags.NonPublic, null, new Type[] { typeof(string) }, null);
			}
			loadPreviewMethodParams[0] = guid;
			return loadPreviewMethod.Invoke(null, loadPreviewMethodParams) as Texture2D;
		}

	}
}
