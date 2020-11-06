using UnityEngine;
using System.Collections;
using System.Linq;
using System.Collections.Generic;

using Project.Common.Singleton;
using Project.Common.Pool;
using Project.Common.Coroutine;

namespace Project.Module.ResKit
{
    public class ResMgr : Singleton<ResMgr>
    {

        private static bool mResMgrInited = false;

        /// <summary>
        /// 初始化bin文件
        /// </summary>
        public static void Init()
        {
            if (mResMgrInited) return;
            mResMgrInited = true;

            //SingletonSafeObjectPool<AssetBundleRes>.Instance.Init(40, 20);
            //SingletonSafeObjectPool<AssetRes>.Instance.Init(40, 20);
            //SingletonSafeObjectPool<ResourcesRes>.Instance.Init(40, 20);
            //SingletonSafeObjectPool<NetImageRes>.Instance.Init(40, 20);
            //SingletonSafeObjectPool<ResSearchKeys>.Instance.Init(40, 20);
            //SingletonSafeObjectPool<ResLoader>.Instance.Init(40, 20);


            Instance.InitResMgr();
        }


        public static IEnumerator InitAsync()
        {
            if (mResMgrInited) yield break;
            mResMgrInited = true;

            //SingletonSafeObjectPool<AssetBundleRes>.Instance.Init(40, 20);
            //SingletonSafeObjectPool<AssetRes>.Instance.Init(40, 20);
            //SingletonSafeObjectPool<ResourcesRes>.Instance.Init(40, 20);
            //SingletonSafeObjectPool<NetImageRes>.Instance.Init(40, 20);
            //SingletonSafeObjectPool<ResSearchKeys>.Instance.Init(40, 20);
            //SingletonSafeObjectPool<ResLoader>.Instance.Init(40, 20);

            yield return Instance.InitResMgrAsync();
        }


        public int Count
        {
            get { return mTable.Count(); }
        }

        #region 字段

        private ResTable mTable = new ResTable();

        [SerializeField] private int                         mCurrentCoroutineCount;
        private                  int                         mMaxCoroutineCount    = 8; //最快协成大概在6到8之间
        private                  LinkedList<IResEnumerator> mResEnumeratorStack = new LinkedList<IResEnumerator>();

        //Res 在ResMgr中 删除的问题，ResMgr定时收集列表中的Res然后删除
        private bool mIsResMapDirty;

        #endregion

        public IEnumerator InitResMgrAsync()
        {
#if UNITY_EDITOR
            if (AssetBundleSettings.SimulateAssetBundleInEditor)
            {
                AssetBundleSettings.AssetBundleConfigFile = EditorRuntimeAssetDataCollector.BuildDataTable();
                yield return null;
            }
            else
#endif
            {
                AssetBundleSettings.AssetBundleConfigFile.Reset();

                var outResult = new List<string>();
                string pathPrefix = "";
#if UNITY_EDITOR || UNITY_IOS
                pathPrefix = "file://";
#endif
                // 未进行过热更
                if (AssetBundleSettings.LoadAssetResFromStreammingAssetsPath)
                {
                    string streamingPath = Application.streamingAssetsPath + "/AssetBundles/" +
                                           AssetBundleSettings.GetPlatformName() + "/" +  AssetBundleSettings.AssetBundleConfigFile.FileName;
                    outResult.Add(pathPrefix + streamingPath);
                }
                // 进行过热更
                else
                {
                    string persistenPath = Application.persistentDataPath + "/AssetBundles/" +
                                           AssetBundleSettings.GetPlatformName() + "/" +  AssetBundleSettings.AssetBundleConfigFile.FileName;
                    outResult.Add(pathPrefix + persistenPath);
                }

                foreach (var outRes in outResult)
                {
                    Debug.Log(outRes);
                    yield return AssetBundleSettings.AssetBundleConfigFile.LoadFromFileAsync(outRes);
                }

                yield return null;
            }
        }

        public void InitResMgr()
        {
#if UNITY_EDITOR
            if (AssetBundleSettings.SimulateAssetBundleInEditor)
            {
                AssetBundleSettings.AssetBundleConfigFile = EditorRuntimeAssetDataCollector.BuildDataTable();
            }
            else
#endif
            {
                AssetBundleSettings.AssetBundleConfigFile.Reset();

                var outResult = new List<string>();

                // 未进行过热更
                if (AssetBundleSettings.LoadAssetResFromStreammingAssetsPath)
                {
                    ResFileMgr.Instance.GetFileInInner(AssetBundleSettings.AssetBundleConfigFile.FileName, outResult);
                }
                // 进行过热更
                else
                {
                    ResFilePath.GetFileInFolder(ResFilePath.PersistentDataPath, AssetBundleSettings.AssetBundleConfigFile.FileName, outResult);
                }

                foreach (var outRes in outResult)
                {
                    Debug.Log(outRes);
                    AssetBundleSettings.AssetBundleConfigFile.LoadFromFile(outRes);
                }
            }
        }


        public void ClearOnUpdate()
        {
            mIsResMapDirty = true;
        }

        public IRes GetRes(ResSearchKeys resSearchKeys, bool createNew = false)
        {
            var res = mTable.GetResBySearchKeys(resSearchKeys);

            if (res != null)
            {
                return res;
            }

            if (!createNew)
            {
                Debug.Log(string.Format("createNew:{0}", createNew));
                return null;
            }

            res = ResFactory.Create(resSearchKeys);

            if (res != null)
            {
                mTable.Add(res);
            }

            return res;
        }

        public T GetRes<T>(ResSearchKeys resSearchKeys) where T : class, IRes
        {
            return GetRes(resSearchKeys) as T;
        }

        public void PushResEnumerator(IResEnumerator task)
        {
            if (task == null)
            {
                return;
            }

            mResEnumeratorStack.AddLast(task);
            TryStartNextResEnumerator();
        }

        private void Update()
        {
            if (mIsResMapDirty)
            {
                RemoveUnusedRes();
            }
        }

        private void RemoveUnusedRes()
        {
            if (!mIsResMapDirty)
            {
                return;
            }

            mIsResMapDirty = false;

            foreach (var res in mTable.ToArray())
            {
                if (res.RefCount <= 0 && res.State != ResState.Loading)
                {
                    if (res.ReleaseRes())
                    {
                        mTable.Remove(res);

                        res.RecycleResObjectToCache();
                    }
                }
            }
        }

#if UNITY_EDITOR
        private void OnGUI()
        {
            if (Input.GetKey(KeyCode.F1))
            {
                GUILayout.BeginVertical("box");

                GUILayout.Label("ResKit", new GUIStyle {fontSize = 30});
                GUILayout.Space(10);
                GUILayout.Label("ResInfo", new GUIStyle {fontSize = 20});
                mTable.ToList().ForEach(res => { GUILayout.Label((res as Res).ToString()); });
                GUILayout.Space(10);

                GUILayout.Label("Pools", new GUIStyle() {fontSize = 20});
                GUILayout.Label(string.Format("ResSearchRule:{0}",
                    SingletonSafeObjectPool<ResSearchKeys>.Instance.Count));
                GUILayout.Label(string.Format("ResLoader:{0}",
                    SingletonSafeObjectPool<ResLoader>.Instance.Count));
                GUILayout.EndVertical();
            }
        }
#endif

        private void OnResEnumeratorFinish()
        {
            --mCurrentCoroutineCount;
            TryStartNextResEnumerator();
        }

        private void TryStartNextResEnumerator()
        {
            if (mResEnumeratorStack.Count == 0)
            {
                return;
            }

            if (mCurrentCoroutineCount >= mMaxCoroutineCount)
            {
                return;
            }

            var task = mResEnumeratorStack.First.Value;
            mResEnumeratorStack.RemoveFirst();

            ++mCurrentCoroutineCount;
            CoroutineTaskTool.StartCoroutine(new CoroutineTask(task.DoLoadAsync(OnResEnumeratorFinish)));
        }

    }
}