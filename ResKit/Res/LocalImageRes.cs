using UnityEngine;
using System.Collections;

using Project.Common.Pool;

namespace Project.Module.ResKit
{
    public static class LocalImageResUtil
    {
        public static string ToLocalImageResName(this string selfFilePath)
        {
            return string.Format("LocalImage:{0}", selfFilePath);
        }
    }
    
    /// <summary>
    /// 本地图片加载器
    /// </summary>
    public class LocalImageRes : Res
    {
        private string mFullPath;
        private string mHashCode;
        private object mRawAsset;
        private WWW mWWW = null;

        public static LocalImageRes Allocate(string name)
        {
            LocalImageRes res = SingletonSafeObjectPool<LocalImageRes>.Instance.AllocateObject();
            if (res != null)
            {
                res.AssetName = name;
                res.SetUrl(name.Substring(11));
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
            get { return mFullPath; }
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

            mFullPath = url;
            mHashCode = string.Format("Photo_{0}", mFullPath.GetHashCode());
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
            SingletonSafeObjectPool<LocalImageRes>.Instance.RecycleObject(this);
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
        }

        //完全的WWW方式,Unity 帮助管理纹理缓存，并且效率貌似更高
        // TODO:persistantPath 用 read
        public override IEnumerator DoLoadAsync(System.Action finishCallback)
        {
//            var imageBytes = File.ReadAllBytes(mFullPath);

//            Texture2D loadTexture2D = new Texture2D(256, 256, TextureFormat.RGB24,false);
//            loadTexture2D.LoadImage(imageBytes);
            
//            if (RefCount <= 0)
//            {
//                OnResLoadFaild();
//                finishCallback();
//                yield break;
//            }

            WWW www = new WWW("file://" + mFullPath);

            mWWW = www;

            yield return www;

            mWWW = null;

            if (www.error != null)
            {
                Debug.LogError(string.Format("Res:{0}, WWW Errors:{1}", mFullPath, www.error));
                OnResLoadFaild();
                finishCallback();
                yield break;
            }

            if (!www.isDone)
            {
                Debug.LogError("LocalImageRes WWW Not Done! Url:" + mFullPath);
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

            mAsset = www.texture;

            www.Dispose();
            www = null;


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

        /*
        public IEnumerator StartResEnumerator2(Action finishCallback)
        {
            if (refCount <= 0)
            {
                OnResLoadFaild();
                finishCallback();
                yield break;
            }

            WWW www = new WWW("file://" + LocalResPath);
            yield return www;
            if (www.error != null)
            {
                Debug.LogError("WWW Error:" + www.error);
                OnResLoadFaild();
                finishCallback();
                yield break;
            }

            if (!www.isDone)
            {
                Debug.LogError("NetImageRes WWW Not Done! Url:" + m_Url);
                OnResLoadFaild();
                finishCallback();

                www.Dispose();
                www = null;

                yield break;
            }

            if (refCount <= 0)
            {
                OnResLoadFaild();
                finishCallback();

                www.Dispose();
                www = null;
                yield break;
            }

            TimeDebugger dt = new TimeDebugger("Tex");
            dt.Begin("LoadingTask");
            Texture2D tex = www.texture;
            tex.Compress(true);
            dt.End();
            dt.Dump(-1);

            m_Asset = tex;
            www.Dispose();
            www = null;

            resState = eResState.kReady;

            finishCallback();
        }
        */
    }
}
