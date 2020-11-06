using UnityEditor;
using System.IO;

namespace Project.Module.ResKitEditor
{
    [InitializeOnLoad]
    public class ResKitAssetsMark
    {
		public const string ABOutputPath = "AssetBundles";
		public const string MarkMenuName = "Assets/ResKit - AssetBundle Mark";

		static ResKitAssetsMark()
		{
			Selection.selectionChanged = OnSelectionChanged;
		}

		public static void OnSelectionChanged()
		{
			var path = GetSelectedPathOrFallback();
			if (!string.IsNullOrEmpty(path))
			{
				Menu.SetChecked(MarkMenuName, Marked(path));
			}
		}

		public static string GetSelectedPathOrFallback()
		{
			var path = string.Empty;

			foreach (var obj in Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.Assets))
			{
				path = AssetDatabase.GetAssetPath(obj);

				if (!string.IsNullOrEmpty(path) && File.Exists(path))
				{
					return path;
				}
			}

			return path;
		}

		public static bool Marked(string path)
		{
			var ai = AssetImporter.GetAtPath(path);
			var dir = new DirectoryInfo(path);
			return string.Equals(ai.assetBundleName, dir.Name.Replace(".", "_").ToLower());
		}

		public static void MarkAB(string path)
		{
			if (!string.IsNullOrEmpty(path))
			{
				var ai = AssetImporter.GetAtPath(path);
				var dir = new DirectoryInfo(path);

				if (Marked(path))
				{
					Menu.SetChecked(MarkMenuName, false);
					ai.assetBundleName = null;
				}
				else
				{
					Menu.SetChecked(MarkMenuName, true);
					ai.assetBundleName = dir.Name.Replace(".", "_");
				}

				AssetDatabase.RemoveUnusedAssetBundleNames();
			}
		}
	}
}
