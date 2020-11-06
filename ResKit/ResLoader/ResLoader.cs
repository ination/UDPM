using System;
using System.Collections.Generic;
using UnityEngine;

using Project.Common.Core;
using Project.Common.Pool;

namespace Project.Module.ResKit
{
    using Object = UnityEngine.Object;

    public class ResLoader : DisposableObject,IResLoader
    {
        [Obsolete("请使用 ResLoader.Allocate() 获取 ResLoader 对象",true)]
        public ResLoader()
        {
             
        }
        /// <summary>
        /// ID:RKRL001 申请ResLoader对象 ResLoader.Allocate（IResLoaderStrategy strategy = null)
        /// </summary>
        /// <param name="strategy">加载策略</param>
        /// <returns></returns>
        public static ResLoader Allocate()
        {
            return SingletonSafeObjectPool<ResLoader>.Instance.AllocateObject();
        }

        /// <summary>
        /// ID:RKRL002 释放ResLoader对象 ResLoader.RecycleResObjectToCache
        /// </summary>
        public void RecycleResObjectToCache()
        {
            SingletonSafeObjectPool<ResLoader>.Instance.RecycleObject(this);
        }

        /// <summary>
        /// ID:RKRL003 同步加载AssetBundle里的资源 ResLoader.LoadSync<T>(string ownerBundle,string assetBundle)
        /// </summary>
        /// <param name="ownerBundle">assetBundle名字</param>
        /// <param name="assetName">资源名字</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T LoadSync<T>(string ownerBundle, string assetName) where T : Object
        {
            var resSearchKeys = ResSearchKeys.Allocate(assetName,ownerBundle,typeof(T));
            var retAsset = LoadResSync(resSearchKeys);
            resSearchKeys.RecycleResObjectToCache();
            return retAsset.Asset as T;
        }

        /// <summary>
        /// ID:RKRL003 只通过资源名字进行同步加载 ResLoader.LoadSync<T>(string assetName)
        /// </summary>
        /// <param name="assetName">资源名字</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T LoadSync<T>(string assetName) where T : Object
        {
            var resSearchKeys = ResSearchKeys.Allocate(assetName, null, typeof(T));
            var retAsset = LoadResSync(resSearchKeys);
            resSearchKeys.RecycleResObjectToCache();
            return retAsset.Asset as T;
        }
        

        /// <summary>
        /// ID:RKRL003 只通过资源名字进行同步加载,
        /// </summary>
        /// <param name="name">资源名字</param>
        /// <returns></returns>
        public Object LoadSync(string name)
        {
            var resSearchRule = ResSearchKeys.Allocate(name);
            var retAsset = LoadResSync(resSearchRule);
            resSearchRule.RecycleResObjectToCache();
            return retAsset.Asset;
        }

        public IRes LoadResSync(ResSearchKeys resSearchKeys)
        {
            AddToLoad(resSearchKeys);
            LoadSync();

            var res = ResMgr.Instance.GetRes(resSearchKeys, false);
            if (res == null)
            {                
                Debug.Log("Failed to Load Res:" + resSearchKeys);                
                return null;
            }
            return res;
        }

        private void LoadSync()
        {
            while (mWaitLoadList.Count > 0)
            {
                var first = mWaitLoadList.First.Value;
                --mLoadingCount;
                mWaitLoadList.RemoveFirst();

                if (first == null)
                {
                    return;
                }

                if (first.LoadSync())
                {
                }
            }
        }


        class CallBackWrap
        {
            private readonly Action<bool, IRes> mListener;
            private readonly IRes               mRes;

            public CallBackWrap(IRes r, Action<bool, IRes> l)
            {
                mRes = r;
                mListener = l;
            }

            public void Release()
            {
                mRes.UnRegisteOnResLoadDoneEvent(mListener);
            }

            public bool IsRes(IRes res)
            {
                return res.AssetName == mRes.AssetName;
            }
        }

        private readonly List<IRes>         mResList      = new List<IRes>();
        private readonly LinkedList<IRes>   mWaitLoadList = new LinkedList<IRes>();
        private          Action             mListener;

        private int  mLoadingCount;

        private        LinkedList<CallBackWrap> mCallbackRecordList;
        

        public float Progress
        {
            get
            {
                if (mWaitLoadList.Count == 0)
                {
                    return 1;
                }

                var unit = 1.0f / mResList.Count;
                var currentValue = unit * (mResList.Count - mLoadingCount);

                var currentNode = mWaitLoadList.First;

                while (currentNode != null)
                {
                    currentValue += unit * currentNode.Value.Progress;
                    currentNode = currentNode.Next;
                }

                return currentValue;
            }
        }


        public void AddToLoad(List<string> list)
        {
            if (list == null)
            {
                return;
            }

            for (var i = list.Count - 1; i >= 0; --i)
            {
                var resSearchRule = ResSearchKeys.Allocate(list[i]);

                AddToLoad(resSearchRule);

                resSearchRule.RecycleResObjectToCache();
            }
        }

        public void AddToLoad(string assetName, Action<bool, IRes> listener = null,
            bool lastOrder = true)
        {
            var searchRule = ResSearchKeys.Allocate(assetName);
            AddToLoad(searchRule,listener,lastOrder);
            searchRule.RecycleResObjectToCache();
        }
        
        public void AddToLoad<T>(string assetName, Action<bool, IRes> listener = null,
            bool lastOrder = true)
        {
            var searchRule = ResSearchKeys.Allocate(assetName,null,typeof(T));
            AddToLoad(searchRule,listener,lastOrder);
            searchRule.RecycleResObjectToCache();
        }


        public void AddToLoad(string ownerBundle, string assetName, Action<bool, IRes> listener = null,
            bool lastOrder = true)
        {
            var searchRule = ResSearchKeys.Allocate(assetName,ownerBundle);

            AddToLoad(searchRule, listener, lastOrder);
            searchRule.RecycleResObjectToCache();
        }
        
        public void AddToLoad<T>(string ownerBundle, string assetName, Action<bool, IRes> listener = null,
            bool lastOrder = true)
        {
            var searchRule = ResSearchKeys.Allocate(assetName,ownerBundle,typeof(T));
            AddToLoad(searchRule, listener, lastOrder);
            searchRule.RecycleResObjectToCache();
        }
        
        private void AddToLoad(ResSearchKeys resSearchKeys, Action<bool, IRes> listener = null,
            bool lastOrder = true)
        {
            var res = FindResInArray(mResList, resSearchKeys);
            if (res != null)
            {
                if (listener != null)
                {
                    AddResListenerRecord(res, listener);
                    res.RegisteOnResLoadDoneEvent(listener);
                }

                return;
            }

            res = ResMgr.Instance.GetRes(resSearchKeys, true);

            if (res == null)
            {
                return;
            }

            if (listener != null)
            {
                AddResListenerRecord(res, listener);
                res.RegisteOnResLoadDoneEvent(listener);
            }

            //无论该资源是否加载完成，都需要添加对该资源依赖的引用
            var depends = res.GetDependResList();

            if (depends != null)
            {
                foreach (var depend in depends)
                {
                    var searchRule = ResSearchKeys.Allocate(depend,null,typeof(AssetBundle));

                    AddToLoad(searchRule);
                    
                    searchRule.RecycleResObjectToCache();
                }
            }

            AddResToArray(res, lastOrder);
        }


#if UNITY_EDITOR
        private readonly Dictionary<string, Sprite> mCachedSpriteDict = new Dictionary<string, Sprite>();
#endif

        public Sprite LoadSprite(string bundleName, string spriteName)
        {
#if UNITY_EDITOR
            if (AssetBundleSettings.SimulateAssetBundleInEditor)
            {
                if (mCachedSpriteDict.ContainsKey(spriteName))
                {
                    return mCachedSpriteDict[spriteName];
                }

                var texture = LoadSync<Texture2D>(bundleName, spriteName);
                var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                    Vector2.one * 0.5f);
                mCachedSpriteDict.Add(spriteName, sprite);
                return mCachedSpriteDict[spriteName];
            }
#endif

            return LoadSync<Sprite>(bundleName, spriteName);
        }


        public Sprite LoadSprite(string spriteName)
        {
#if UNITY_EDITOR
            if (AssetBundleSettings.SimulateAssetBundleInEditor)
            {
                if (mCachedSpriteDict.ContainsKey(spriteName))
                {
                    return mCachedSpriteDict[spriteName];
                }

                var texture = LoadSync(spriteName) as Texture2D;
                var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                    Vector2.one * 0.5f);
                mCachedSpriteDict.Add(spriteName, sprite);
                return mCachedSpriteDict[spriteName];
            }
#endif

            return LoadSync<Sprite>(spriteName);
        }


        public void LoadAsync(Action listener = null)
        {
            mListener = listener;
            DoLoadAsync();
        }

        public void ReleaseRes(string resName)
        {
            
            if (string.IsNullOrEmpty(resName))
            {
                return;
            }

#if UNITY_EDITOR
            if (AssetBundleSettings.SimulateAssetBundleInEditor)
            {
                if (mCachedSpriteDict.ContainsKey(resName))
                {
                    var sprite = mCachedSpriteDict[resName];
                    GameObject.Destroy(sprite);
                    mCachedSpriteDict.Remove(resName);
                }
            }
#endif
            var resSearchRule = ResSearchKeys.Allocate(resName);

            var res = ResMgr.Instance.GetRes(resSearchRule);
            resSearchRule.RecycleResObjectToCache();
            
            if (res == null)
            {
                return;
            }

            if (mWaitLoadList.Remove(res))
            {
                --mLoadingCount;
                if (mLoadingCount == 0)
                {
                    mListener = null;
                }
            }

            if (mResList.Remove(res))
            {
                res.UnRegisteOnResLoadDoneEvent(OnResLoadFinish);
                res.Release();
                ResMgr.Instance.ClearOnUpdate();
            }
        }

        public void ReleaseRes(string[] names)
        {
            if (names == null || names.Length == 0)
            {
                return;
            }

            for (var i = names.Length - 1; i >= 0; --i)
            {
                ReleaseRes(names[i]);
            }
        }

        public void ReleaseAllRes()
        {
#if UNITY_EDITOR
            if (AssetBundleSettings.SimulateAssetBundleInEditor)
            {
                foreach (var spritePair in mCachedSpriteDict)
                {
                    GameObject.Destroy(spritePair.Value);
                }

                mCachedSpriteDict.Clear();
            }
#endif

            mListener = null;
            mLoadingCount = 0;
            mWaitLoadList.Clear();

            if (mResList.Count > 0)
            {
                //确保首先删除的是AB，这样能对Asset的卸载做优化
                mResList.Reverse();

                for (var i = mResList.Count - 1; i >= 0; --i)
                {
                    mResList[i].UnRegisteOnResLoadDoneEvent(OnResLoadFinish);
                    mResList[i].Release();
                }

                mResList.Clear();

                //if (!ResMgr.IsApplicationQuit)
                if (Application.isPlaying)
                {
                    ResMgr.Instance.ClearOnUpdate();
                }
            }

            RemoveAllCallbacks(true);
        }

        public void UnloadAllInstantiateRes(bool flag)
        {
            if (mResList.Count > 0)
            {
                for (var i = mResList.Count - 1; i >= 0; --i)
                {
                    if (mResList[i].UnloadImage(flag))
                    {
                        if (mWaitLoadList.Remove(mResList[i]))
                        {
                            --mLoadingCount;
                        }

                        RemoveCallback(mResList[i], true);

                        mResList[i].UnRegisteOnResLoadDoneEvent(OnResLoadFinish);
                        mResList[i].Release();
                        mResList.RemoveAt(i);
                    }
                }

                ResMgr.Instance.ClearOnUpdate();
            }
        }

        public override void Dispose()
        {
            ReleaseAllRes();
            base.Dispose();
        }

        public void Dump()
        {
            foreach (var res in mResList)
            {
                Debug.Log(res.AssetName);
            }
        }
        

        private void DoLoadAsync()
        {
            if (mLoadingCount == 0)
            {
                if (mListener != null)
                {
                    var callback = mListener;
                    mListener = null;
                    callback();
                }

                return;
            }

            var nextNode = mWaitLoadList.First;
            LinkedListNode<IRes> currentNode = null;
            while (nextNode != null)
            {
                currentNode = nextNode;
                var res = currentNode.Value;
                nextNode = currentNode.Next;
                if (res.IsDependResLoadFinish())
                {
                    mWaitLoadList.Remove(currentNode);

                    if (res.State != ResState.Ready)
                    {
                        res.RegisteOnResLoadDoneEvent(OnResLoadFinish);
                        res.LoadAsync();
                    }
                    else
                    {
                        --mLoadingCount;
                    }
                }
            }
        }

        private void RemoveCallback(IRes res, bool release)
        {
            if (mCallbackRecordList != null)
            {
                var current = mCallbackRecordList.First;
                LinkedListNode<CallBackWrap> next = null;
                while (current != null)
                {
                    next = current.Next;
                    if (current.Value.IsRes(res))
                    {
                        if (release)
                        {
                            current.Value.Release();
                        }

                        mCallbackRecordList.Remove(current);
                    }

                    current = next;
                }
            }
        }

        private void RemoveAllCallbacks(bool release)
        {
            if (mCallbackRecordList != null)
            {
                var count = mCallbackRecordList.Count;
                while (count > 0)
                {
                    --count;
                    if (release)
                    {
                        mCallbackRecordList.Last.Value.Release();
                    }

                    mCallbackRecordList.RemoveLast();
                }
            }
        }

        private void OnResLoadFinish(bool result, IRes res)
        {
            --mLoadingCount;

            DoLoadAsync();
            if (mLoadingCount == 0)
            {
                RemoveAllCallbacks(false);

                if (mListener != null)
                {
                    mListener();
                }
            }
        }

        private void AddResToArray(IRes res, bool lastOrder)
        {
            var searchRule = ResSearchKeys.Allocate(res.AssetName,res.OwnerBundleName,res.AssetType);

            //再次确保队列中没有它
            var oldRes = FindResInArray(mResList, searchRule);
            
            searchRule.RecycleResObjectToCache();

            if (oldRes != null)
            {
                return;
            }

            res.Retain();
            mResList.Add(res);

            if (res.State != ResState.Ready)
            {
                ++mLoadingCount;
                if (lastOrder)
                {
                    mWaitLoadList.AddLast(res);
                }
                else
                {
                    mWaitLoadList.AddFirst(res);
                }
            }
        }

        private static IRes FindResInArray(List<IRes> list, ResSearchKeys resSearchKeys)
        {
            if (list == null)
            {
                return null;
            }

            for (var i = list.Count - 1; i >= 0; --i)
            {
                if (resSearchKeys.Match(list[i]))
                {
                    return list[i];
                }
            }

            return null;
        }

        private void AddResListenerRecord(IRes res, Action<bool, IRes> listener)
        {
            if (mCallbackRecordList == null)
            {
                mCallbackRecordList = new LinkedList<CallBackWrap>();
            }

            mCallbackRecordList.AddLast(new CallBackWrap(res, listener));
        }

        bool IPoolable.IsRecycled { get; set; }

        void IPoolable.OnRecycled()
        {
            ReleaseAllRes();
        }
    }
}