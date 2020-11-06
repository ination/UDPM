using UnityEngine;
using System.Collections;

using Project.Common.Pool;

namespace Project.Module.ResKit
{
    public enum InternalResNamePrefixType
    {
        Url, // resources://
        Folder, // Resources/
    }

    public class ResourcesRes : Res
    {
        private ResourceRequest mResourceRequest;

        private string mPath;

        public static ResourcesRes Allocate(string name, InternalResNamePrefixType prefixType)
        {
            var res = SingletonSafeObjectPool<ResourcesRes>.Instance.AllocateObject();
            if (res != null)
            {
                res.AssetName = name;
            }

            if (prefixType == InternalResNamePrefixType.Url)
            {
                res.mPath = name.Substring("resources://".Length);
            }
            else
            {
                res.mPath = name.Substring("Resources/".Length);
            }

            return res;
        }

        public override bool LoadSync()
        {
            if (!CheckLoadAble())
            {
                return false;
            }

            if (string.IsNullOrEmpty(mAssetName))
            {
                return false;
            }

            State = ResState.Loading;

            
            if (AssetType != null)
            {
                mAsset = Resources.Load(mPath,AssetType);
            }
            else
            {
                mAsset = Resources.Load(mPath);
            }
            
            
            if (mAsset == null)
            {
                Debug.LogError("Failed to Load Asset From Resources:" + mPath);
                OnResLoadFaild();
                return false;
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

            if (string.IsNullOrEmpty(mAssetName))
            {
                return;
            }

            State = ResState.Loading;

            ResMgr.Instance.PushResEnumerator(this);
        }

        public override IEnumerator DoLoadAsync(System.Action finishCallback)
        {
            if (RefCount <= 0)
            {
                OnResLoadFaild();
                finishCallback();
                yield break;
            }

            ResourceRequest resourceRequest = null;

            if (AssetType != null)
            {
                resourceRequest = Resources.LoadAsync(mPath, AssetType);
            }
            else
            {
                resourceRequest = Resources.LoadAsync(mPath);
            }

            mResourceRequest = resourceRequest;
            yield return resourceRequest;
            mResourceRequest = null;

            if (!resourceRequest.isDone)
            {
                Debug.LogError("Failed to Load Resources:" + mAssetName);
                OnResLoadFaild();
                finishCallback();
                yield break;
            }

            mAsset = resourceRequest.asset;

            State = ResState.Ready;

            finishCallback();
        }

        public override void RecycleResObjectToCache()
        {
            SingletonSafeObjectPool<ResourcesRes>.Instance.RecycleObject(this);
        }

        protected override float CalculateProgress()
        {
            if (mResourceRequest == null)
            {
                return 0;
            }

            return mResourceRequest.progress;
        }

        public override string ToString()
        {
            return string.Format("Type:Resources {1}", AssetName, base.ToString());
        }
    }
}