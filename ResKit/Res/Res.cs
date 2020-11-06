using UnityEngine;
using System;
using System.Collections;

using Project.Common.Pool;
using Project.Common.RefCounter;

namespace Project.Module.ResKit
{
    public class Res : SimpleRefObject, IRes, IPoolable
    {
        
        protected string                 mAssetName;
        private   ResState               mResState = ResState.Waiting;
        protected UnityEngine.Object     mAsset;
        private event Action<bool, IRes> mOnResLoadDoneEvent;

        public string AssetName
        {
            get { return mAssetName; }
            protected set { mAssetName = value; }
        }


        public ResState State
        {
            get { return mResState; }
            set
            {
                mResState = value;
                if (mResState == ResState.Ready)
                {
                    NotifyResLoadDoneEvent(true);
                }
            }
        }

        public virtual string OwnerBundleName { get; set; }

        public Type AssetType { get; set; }

        /// <summary>
        /// 弃用
        /// </summary>
        public float Progress
        {
            get
            {
                switch (mResState)
                {
                    case ResState.Loading:
                        return CalculateProgress();
                    case ResState.Ready:
                        return 1;
                }

                return 0;
            }
        }

        protected virtual float CalculateProgress()
        {
            return 0;
        }

        public UnityEngine.Object Asset
        {
            get { return mAsset; }
        }

        public bool IsRecycled { get; set; }



        public void RegisteOnResLoadDoneEvent(Action<bool, IRes> listener)
        {
            if (listener == null)
            {
                return;
            }

            if (mResState == ResState.Ready)
            {
                listener(true, this);
                return;
            }

            mOnResLoadDoneEvent += listener;
        }

        public void UnRegisteOnResLoadDoneEvent(Action<bool, IRes> listener)
        {
            if (listener == null)
            {
                return;
            }

            if (mOnResLoadDoneEvent == null)
            {
                return;
            }

            mOnResLoadDoneEvent -= listener;
        }

        protected void OnResLoadFaild()
        {
            mResState = ResState.Waiting;
            NotifyResLoadDoneEvent(false);
        }

        private void NotifyResLoadDoneEvent(bool result)
        {
            if (mOnResLoadDoneEvent != null)
            {
                mOnResLoadDoneEvent(result, this);
                mOnResLoadDoneEvent = null;
            }
            
        }

        protected Res(string assetName)
        {
            IsRecycled = false;
            mAssetName = assetName;
        }

        public Res()
        {
            IsRecycled = false;
        }

        protected bool CheckLoadAble()
        {
            return mResState == ResState.Waiting;
        }

        protected void HoldDependRes()
        {
            var depends = GetDependResList();
            if (depends == null || depends.Length == 0)
            {
                return;
            }

            for (var i = depends.Length - 1; i >= 0; --i)
            {
                var resSearchRule = ResSearchKeys.Allocate(depends[i],null,typeof(AssetBundle));
                var res = ResMgr.Instance.GetRes(resSearchRule, false);
                resSearchRule.RecycleResObjectToCache();
                
                if (res != null)
                {
                    res.Retain();
                }
            }
        }

        protected void UnHoldDependRes()
        {
            var depends = GetDependResList();
            if (depends == null || depends.Length == 0)
            {
                return;
            }

            for (var i = depends.Length - 1; i >= 0; --i)
            {
                var resSearchRule = ResSearchKeys.Allocate(depends[i]);
                var res = ResMgr.Instance.GetRes(resSearchRule, false);
                resSearchRule.RecycleResObjectToCache();
                
                if (res != null)
                {
                    res.Release();
                }
            }
        }

        #region 子类实现

        public virtual bool LoadSync()
        {
            return false;
        }

        public virtual void LoadAsync()
        {
        }

        public virtual string[] GetDependResList()
        {
            return null;
        }

        public bool IsDependResLoadFinish()
        {
            var depends = GetDependResList();
            if (depends == null || depends.Length == 0)
            {
                return true;
            }

            for (var i = depends.Length - 1; i >= 0; --i)
            {
                var resSearchRule = ResSearchKeys.Allocate(depends[i]);
                var res = ResMgr.Instance.GetRes(resSearchRule, false);
                resSearchRule.RecycleResObjectToCache();
                
                if (res == null || res.State != ResState.Ready)
                {
                    return false;
                }
            }

            return true;
        }

        public virtual bool UnloadImage(bool flag)
        {
            return false;
        }

        public bool ReleaseRes()
        {
            if (mResState == ResState.Loading)
            {
                return false;
            }

            if (mResState != ResState.Ready)
            {
                return true;
            }

            //Log.I("Release Res:" + mName);

            OnReleaseRes();

            mResState = ResState.Waiting;
            mOnResLoadDoneEvent = null;
            return true;
        }

        protected virtual void OnReleaseRes()
        {
            //如果Image 直接释放了，这里会直接变成NULL
            if (mAsset != null)
            {
                if (mAsset is GameObject)
                {

                }
                else
                {
                    Resources.UnloadAsset(mAsset);
                }

                mAsset = null;
            }
        }

        protected override void OnZeroRef()
        {
            if (mResState == ResState.Loading)
            {
                return;
            }

            ReleaseRes();
        }

        public virtual void RecycleResObjectToCache()
        {

        }

        public virtual void OnRecycled()
        {
            mAssetName = null;
            mOnResLoadDoneEvent = null;
        }

        public virtual IEnumerator DoLoadAsync(Action finishCallback)
        {
            finishCallback();
            yield break;
        }

        public override string ToString()
        {
            return string.Format("Name:{0}\t State:{1}\t RefCount:{2}", AssetName, State, RefCount);
        }

        #endregion
    }
}