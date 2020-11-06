using System;
using UnityEngine;
using System.Collections;

using Project.Common.Pool;

namespace Project.Module.ResKit
{
   
    public static class NetImageResUtil
    {
        public static string ToNetImageResName(this string selfHttpUrl)
        {
            return string.Format("NetImage:{0}", selfHttpUrl);
        }
    }
    
    public class NetImageRes : Res
    {
        private string mUrl;
        private string mHashCode;
        private object mRawAsset;
        private WWW mWWW;

        public static NetImageRes Allocate(string name)
        {
            NetImageRes res = SingletonSafeObjectPool<NetImageRes>.Instance.AllocateObject();
            if (res != null)
            {
                res.AssetName = name;
                res.SetUrl(name.Substring(9));
            }
            return res;
        }

        public void SetDownloadProgress(int totalSize, int download)
        {

        }

        public string LocalResPath
        {
            get { return string.Format("{0}{1}", ResFilePath.PersistentDataPath4Photo, mHashCode); }
        }

        public virtual object RawAsset
        {
            get { return mRawAsset; }
        }

        public bool NeedDownload
        {
            get { return RefCount > 0; }
        }

        public string Url
        {
            get { return mUrl; }
        }

        public int FileSize
        {
            get { return 1; }
        }

        public void SetUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return;
            }

            mUrl = url;
            mHashCode = string.Format("Photo_{0}", mUrl.GetHashCode());
        }

        public override bool UnloadImage(bool flag)
        {
            return false;
        }

        public override bool LoadSync()
        {
            return false;
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

            DoLoadWork();
        }

        private void DoLoadWork()
        {
            State = ResState.Loading;

            OnDownLoadResult(true);

            //检测本地文件是否存在
            /*
            if (!File.Exists(LocalResPath))
            {
                ResDownloader.S.AddDownloadTask(this);
            }
            else
            {
                OnDownLoadResult(true);
            }
            */
        }

        protected override void OnReleaseRes()
        {
            if (mAsset != null)
            {
                GameObject.Destroy(mAsset);
                mAsset = null;
            }

            mRawAsset = null;
        }

        public override void RecycleResObjectToCache()
        {
            SingletonSafeObjectPool<NetImageRes>.Instance.RecycleObject(this);
        }

        public override void OnRecycled()
        {

        }

        public void DeleteOldResFile()
        {
            //throw new NotImplementedException();
        }

        public void OnDownLoadResult(bool result)
        {
            if (!result)
            {
                OnResLoadFaild();
                return;
            }

            if (RefCount <= 0)
            {
                State = ResState.Waiting;
                return;
            }

            ResMgr.Instance.PushResEnumerator(this);
            //ResMgr.S.PostLoadTask(LoadImage());
        }

        //完全的WWW方式,Unity 帮助管理纹理缓存，并且效率貌似更高
        public override IEnumerator DoLoadAsync(System.Action finishCallback)
        {
            if (RefCount <= 0)
            {
                OnResLoadFaild();
                finishCallback();
                yield break;
            }

            WWW www = new WWW(mUrl);

            mWWW = www;

            yield return www;

            mWWW = null;

            if (www.error != null)
            {
                Debug.LogError(string.Format("Res:{0}, WWW Errors:{1}", mUrl, www.error));
                OnResLoadFaild();
                finishCallback();
                yield break;
            }

            if (!www.isDone)
            {
                Debug.LogError("NetImageRes WWW Not Done! Url:" + mUrl);
                OnResLoadFaild();
                finishCallback();

                www.Dispose();
                www = null;

                yield break;
            }

            if (RefCount <= 0)
            {
                OnResLoadFaild();
                finishCallback();

                www.Dispose();
                www = null;
                yield break;
            }

            //TimeDebugger dt = new TimeDebugger("Tex");
            //dt.Begin("LoadingTask");
            //这里是同步的操作
            mAsset = www.texture;
            //dt.End();

            www.Dispose();
            www = null;

            //dt.Dump(-1);

            State = ResState.Ready;

            finishCallback();
        }

        protected override float CalculateProgress()
        {
            if (mWWW == null)
            {
                return 0;
            }

            return mWWW.progress;
        }
    }
}
