using System.Collections;

namespace Project.Module.ResKit
{
    public interface IResDatas
    {
        string FileName { get; }

        /// <summary>
        /// 获取依赖
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        string[] GetAllDependenciesByUrl(string url);

        void LoadFromFile(string outRes);
        void Reset();
        IEnumerator LoadFromFileAsync(string outRes);
        // string GetAssetBundleName(string assetName, int assetBundleIndex, string ownerBundleName);
        AssetData GetAssetData(ResSearchKeys resSearchKeys);
        int AddAssetBundleName(string abName, string[] depends, out AssetDataGroup @group);
        // void AddAssetData(AssetData assetData);
        // void InitForEditor();
    }
}