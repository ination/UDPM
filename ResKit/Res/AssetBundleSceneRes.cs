using UnityEngine;
using System;
using Project.Common.Pool;

namespace Project.Module.ResKit
{
        
    public class AssetBundleSceneRes : AssetRes
    {
        public static AssetBundleSceneRes Allocate(string name)
        {
            AssetBundleSceneRes res = SingletonSafeObjectPool<AssetBundleSceneRes>.Instance.AllocateObject();
            if (res != null)
            {
                res.AssetName = name;
                res.InitAssetBundleName();
            }
            return res;
        }

        public AssetBundleSceneRes(string assetName) : base(assetName)
        {

        }

        public AssetBundleSceneRes()
        {

        }

        public override bool LoadSync()
        {
            if (!CheckLoadAble())
            {
                return false;
            }

            if (string.IsNullOrEmpty(AssetBundleName))
            {
                return false;
            }

            var resSearchKeys = ResSearchKeys.Allocate(AssetBundleName);
            
            var abR = ResMgr.Instance.GetRes<AssetBundleRes>(resSearchKeys);

            resSearchKeys.RecycleResObjectToCache();

            if (abR == null || abR.AssetBundle == null)
            {
                Debug.LogError("Failed to Load Asset, Not Find AssetBundleImage:" + abR);
                return false;
            }


            State = ResState.Ready;
            return true;
        }

        public override void LoadAsync()
        {
            LoadSync();
        }


        public override void RecycleResObjectToCache()
        {
            SingletonSafeObjectPool<AssetBundleSceneRes>.Instance.RecycleObject(this);
        }
    }
}