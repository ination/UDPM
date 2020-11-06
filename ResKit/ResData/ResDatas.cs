using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

using Project.Common.Utility;

namespace Project.Module.ResKit
{

    /// <summary>
    /// 默认的 ResData 支持
    /// </summary>
    public class ResDatas: IResDatas
    {
        [Serializable]
        public class SerializeData
        {
            private AssetDataGroup.SerializeData[] mAssetDataGroup;

            public AssetDataGroup.SerializeData[] AssetDataGroup
            {
                get { return mAssetDataGroup; }
                set { mAssetDataGroup = value; }
            }
        }
        
        public virtual string FileName
        {
            get { return "asset_bindle_config.bin"; }
        }
        
        public IList<AssetDataGroup> AllAssetDataGroups
        {
            get { return mAllAssetDataGroup; }
        }

        protected readonly List<AssetDataGroup> mAllAssetDataGroup = new List<AssetDataGroup>();

        private AssetDataTable mAssetDataTable = null;
        
        public ResDatas(){}

        public void Reset()
        {
            for (int i = mAllAssetDataGroup.Count - 1; i >= 0; --i)
            {
                mAllAssetDataGroup[i].Reset();
            }

            mAllAssetDataGroup.Clear();

            if (mAssetDataTable != null)
            {
                mAssetDataTable.Dispose();
            }

            mAssetDataTable = null;
        }

        public int AddAssetBundleName(string name, string[] depends, out AssetDataGroup group)
        {
            group = null;

            if (string.IsNullOrEmpty(name))
            {
                return -1;
            }

            var key = GetKeyFromABName(name);

            if (key == null)
            {
                return -1;
            }

            group = GetAssetDataGroup(key);

            if (group == null)
            {
                group = new AssetDataGroup(key);
                Debug.Log("#Create Config Group:" + key);
                mAllAssetDataGroup.Add(group);
            }

            return group.AddAssetBundleName(name, depends);
        }

        public string[] GetAllDependenciesByUrl(string url)
        {
			var abName = AssetBundleSettings.AssetBundleUrl2Name(url);
            
            for (var i = mAllAssetDataGroup.Count - 1; i >= 0; --i)
            {
                string[] depends;
                if (!mAllAssetDataGroup[i].GetAssetBundleDepends(abName, out depends))
                {
                    continue;
                }

                return depends;
            }

            return null;
        }
        

        public AssetData  GetAssetData(ResSearchKeys resSearchKeys)
        {
            if (mAssetDataTable == null)
            {
                mAssetDataTable = new AssetDataTable();
                
                for (var i = mAllAssetDataGroup.Count - 1; i >= 0; --i)
                {
                    foreach (var assetData in mAllAssetDataGroup[i].AssetDatas)
                    {
                        mAssetDataTable.Add(assetData);
                    }
                }
            }

            return mAssetDataTable.GetAssetDataByResSearchKeys(resSearchKeys);
        }

        public virtual void LoadFromFile(string path)
        {
			var data = SerializeUtility.DeserializeBinary(ResFileMgr.Instance.OpenReadStream(path));

            if (data == null)
            {
                Debug.LogError("Failed Deserialize AssetDataTable:" + path);
                return;
            }

            var sd = data as SerializeData;

            if (sd == null)
            {
                Debug.LogError("Failed Load AssetDataTable:" + path);
                return;
            }

            Debug.Log("Load AssetConfig From File:" + path);
            SetSerizlizeData(sd);
        }

        //IEnumerator LoadFile()
        //{
        //    UnityWebRequest request = UnityWebRequest.Get(@"E:\UnityProjects\TestFile\TestFile.txt");
        //    yield return request.SendWebRequest();
        //    if (request.isHttpError || request.isNetworkError)
        //    {
        //        Debug.Log(request.error);
        //    }
        //    else
        //    {
        //        ShowText.text = request.downloadHandler.text;
        //    }

        public virtual IEnumerator LoadFromFileAsync(string path)
        {
            using (var www = new WWW(path))
            {
                yield return www;

                if (www.error != null)
                {
                    Debug.LogError("Failed Deserialize AssetDataTable:" + path + " Error:" + www.error);
                    yield break;
                }

                var stream = new MemoryStream(www.bytes);

                var data = SerializeUtility.DeserializeBinary(stream);

                if (data == null)
                {
                    Debug.LogError("Failed Deserialize AssetDataTable:" + path);
                    yield break;
                }

                var sd = data as SerializeData;

                if (sd == null)
                {
                    Debug.LogError("Failed Load AssetDataTable:" + path);
                    yield break;
                }

                Debug.Log("Load AssetConfig From File:" + path);
                SetSerizlizeData(sd);
            }
        }

        public virtual void Save(string outPath)
        {
            SerializeData sd = new SerializeData
            {
                AssetDataGroup = new AssetDataGroup.SerializeData[mAllAssetDataGroup.Count]
            };

            for (var i = 0; i < mAllAssetDataGroup.Count; ++i)
            {
                sd.AssetDataGroup[i] = mAllAssetDataGroup[i].GetSerializeData();
            }

            if (SerializeUtility.SerializeBinary(outPath, sd))
            {
                Debug.Log("Success Save AssetDataTable:" + outPath);
            }
            else
            {
                Debug.LogError("Failed Save AssetDataTable:" + outPath);
            }
        }

        protected void SetSerizlizeData(SerializeData data)
        {
            if (data == null || data.AssetDataGroup == null)
            {
                return;
            }

            for (int i = data.AssetDataGroup.Length - 1; i >= 0; --i)
            {
                mAllAssetDataGroup.Add(BuildAssetDataGroup(data.AssetDataGroup[i]));
            }
            
            if (mAssetDataTable == null)
            {
                mAssetDataTable = new AssetDataTable();

                foreach (var serializeData in data.AssetDataGroup)
                {
                    foreach (var assetData in serializeData.assetDataArray)
                    {
                        mAssetDataTable.Add(assetData);
                    }
                }
            }
        }

        private AssetDataGroup BuildAssetDataGroup(AssetDataGroup.SerializeData data)
        {
            return new AssetDataGroup(data);
        }

        private AssetDataGroup GetAssetDataGroup(string key)
        {
            for (int i = mAllAssetDataGroup.Count - 1; i >= 0; --i)
            {
                if (mAllAssetDataGroup[i].key.Equals(key))
                {
                    return mAllAssetDataGroup[i];
                }
            }

            return null;
        }

        private static string GetKeyFromABName(string name)
        {
            int pIndex = name.IndexOf('/');

            if (pIndex < 0)
            {
                return name;
            }

            string key = name.Substring(0, pIndex);

            if (name.Contains("i18res"))
            {
                int i18Start = name.IndexOf("i18res") + 7;
                name = name.Substring(i18Start);
                pIndex = name.IndexOf('/');
                if (pIndex < 0)
                {
                    Debug.LogWarning("Not Valid AB Path:" + name);
                    return null;
                }

                string language = string.Format("[{0}]", name.Substring(0, pIndex));
                key = string.Format("{0}-i18res-{1}", key, language);
            }

            return key;
        }

    }
}
