using UnityEngine;
using UnityEditor;

using Project.Common.Extension;
using Project.Module.ResKit;

namespace Project.Module.ResKitEditor
{
	public static class AssetBundleExporter
	{
		public static void BuildDataTable()
		{
			Debug.Log("Start BuildAssetDataTable!");
			ResDatas table = new ResDatas();
			EditorRuntimeAssetDataCollector.AddABInfo2ResDatas(table);

			var filePath =
				(ResFilePath.StreamingAssetsPath + AssetBundleSettings.RelativeABRootFolder).CreateDirIfNotExists() +
				table.FileName;
			
			table.Save(filePath);
			AssetDatabase.Refresh();
		}
	}
}
