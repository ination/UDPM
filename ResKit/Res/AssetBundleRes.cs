using UnityEngine;
using System.Collections;

using Project.Common.Pool;

namespace Project.Module.ResKit
{
    public class AssetBundleRes : Res
    {
        private bool                     mUnloadFlag = true;
        private string[]                 mDependResList;
        private AssetBundleCreateRequest mAssetBundleCreateRequest;

        public static AssetBundleRes Allocate(string name)
        {
            var res =  SingletonSafeObjectPool<AssetBundleRes>.Instance.AllocateObject();

            res.AssetName = name;
            res.AssetType = typeof(AssetBundle);
            res.InitAssetBundleName();
            
            return res;
        }

        private void InitAssetBundleName()
        {
            mDependResList =  AssetBundleSettings.AssetBundleConfigFile.GetAllDependenciesByUrl(AssetName);
        }

        public AssetBundle AssetBundle
        {
            get { return (AssetBundle) mAsset; }
            private set { mAsset = value; }
        }
        
        public override bool LoadSync()
        {
            if (!CheckLoadAble())
            {
                return false;
            }

            State = ResState.Loading;

#if UNITY_EDITOR
            if (AssetBundleSettings.SimulateAssetBundleInEditor)
            {
            }
            else
#endif
            {
                var url = AssetBundleSettings.AssetBundleName2Url(mAssetName);
                var bundle = AssetBundle.LoadFromFile(url);

                mUnloadFlag = true;

                if (bundle == null)
                {
                    Debug.LogError("Failed Load AssetBundle:" + mAssetName);
                    OnResLoadFaild();
                    return false;
                }

                AssetBundle = bundle;
            }

            State = ResState.Ready;

            return true;
        }

        public override void LoadAsync()
        {
            if (!CheckLoadAble())
            {
                return;
            }

            State = ResState.Loading;

            ResMgr.Instance.PushResEnumerator(this);
        }

        public override IEnumerator DoLoadAsync(System.Action finishCallback)
        {
            //开启的时候已经结束了
            if (RefCount <= 0)
            {
                OnResLoadFaild();
                finishCallback();
                yield break;
            }

#if UNITY_EDITOR
            if (AssetBundleSettings.SimulateAssetBundleInEditor)
            {
                yield return null;
            }
            else
#endif
            {
                var url = AssetBundleSettings.AssetBundleName2Url(mAssetName);
                var abcR = AssetBundle.LoadFromFileAsync(url);

                mAssetBundleCreateRequest = abcR;
                yield return abcR;
                mAssetBundleCreateRequest = null;

                if (!abcR.isDone)
                {
                    Debug.LogError("AssetBundleCreateRequest Not Done! Path:" + mAssetName);
                    OnResLoadFaild();
                    finishCallback();
                    yield break;
                }

                AssetBundle = abcR.assetBundle;
            }

            State = ResState.Ready;
            finishCallback();
        }

        public override string[] GetDependResList()
        {
            return mDependResList;
        }

        public override bool UnloadImage(bool flag)
        {
            if (AssetBundle != null)
            {
                mUnloadFlag = flag;
            }

            return true;
        }

        public override void RecycleResObjectToCache()
        {
            SingletonSafeObjectPool<AssetBundleRes>.Instance.RecycleObject(this);
        }

        public override void OnRecycled()
        {
            base.OnRecycled();
            mUnloadFlag = true;
            mDependResList = null;
        }

        protected override float CalculateProgress()
        {
            if (mAssetBundleCreateRequest == null)
            {
                return 0;
            }

            return mAssetBundleCreateRequest.progress;
        }

        protected override void OnReleaseRes()
        {
            if (AssetBundle != null)
            {
                AssetBundle.Unload(mUnloadFlag);
                AssetBundle = null;
            }
        }

        public override string ToString()
        {
            return string.Format("Type:AssetBundle\t {0}", base.ToString());
        }
    }
}