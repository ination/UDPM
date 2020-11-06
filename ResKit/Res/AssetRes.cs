using System;
using UnityEngine;
using System.Collections;

using Project.Common.Pool;

namespace Project.Module.ResKit
{
	public class AssetRes : Res
	{
		protected string[]           mAssetBundleArray;
		protected AssetBundleRequest mAssetBundleRequest;
		protected string                 mOwnerBundleName;

		public override string OwnerBundleName
		{
			get { return mOwnerBundleName; }
			set { mOwnerBundleName = value; }
		}
		
		public static AssetRes Allocate(string name, string onwerBundleName, Type assetTypde)
		{
			var res = SingletonSafeObjectPool<AssetRes>.Instance.AllocateObject();
			if (res != null)
			{
				res.AssetName = name;
				res.mOwnerBundleName = onwerBundleName;
				res.AssetType = assetTypde;
				res.InitAssetBundleName();
			}

			return res;
		}

		protected string AssetBundleName
		{
			get { return mAssetBundleArray == null ? null : mAssetBundleArray[0]; }
		}

		public AssetRes(string assetName) : base(assetName)
		{

		}

		public AssetRes()
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


			UnityEngine.Object obj = null;

#if UNITY_EDITOR
			if (AssetBundleSettings.SimulateAssetBundleInEditor && !string.Equals(mAssetName, "assetbundlemanifest"))
			{
				var resSearchKeys = ResSearchKeys.Allocate(AssetBundleName,null,typeof(AssetBundle));

				var abR = ResMgr.Instance.GetRes<AssetBundleRes>(resSearchKeys);
				resSearchKeys.RecycleResObjectToCache();

				var assetPaths = UnityEditor.AssetDatabase.GetAssetPathsFromAssetBundleAndAssetName(abR.AssetName, mAssetName);
				if (assetPaths.Length == 0)
				{
					Debug.LogError("Failed Load Asset:" + mAssetName);
					OnResLoadFaild();
					return false;
				}
				
				HoldDependRes();

				State = ResState.Loading;

				if (AssetType != null)
				{

					obj = UnityEditor.AssetDatabase.LoadAssetAtPath(assetPaths[0],AssetType);
				}
				else
				{
					obj = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPaths[0]);
				}
			}
			else
#endif
			{
				var resSearchKeys = ResSearchKeys.Allocate(AssetBundleName, null, typeof(AssetBundle));
				var abR = ResMgr.Instance.GetRes<AssetBundleRes>(resSearchKeys);
				resSearchKeys.RecycleResObjectToCache();

				
				if (abR == null || !abR.AssetBundle)
				{
					Debug.LogError("Failed to Load Asset, Not Find AssetBundleImage:" + AssetBundleName);
					return false;
				}
				
				HoldDependRes();

				State = ResState.Loading;

				if (AssetType != null)
				{
					obj = abR.AssetBundle.LoadAsset(mAssetName,AssetType);
				}
				else
				{
					obj = abR.AssetBundle.LoadAsset(mAssetName);
				}
			}

			UnHoldDependRes();

			if (obj == null)
			{
				Debug.LogError("Failed Load Asset:" + mAssetName + ":" + AssetType + ":" + AssetBundleName);
				OnResLoadFaild();
				return false;
			}

			mAsset = obj;

			State = ResState.Ready;
			return true;
		}

		public override void LoadAsync()
		{
			if (!CheckLoadAble())
			{
				return;
			}

			if (string.IsNullOrEmpty(AssetBundleName))
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

			
            //Object obj = null;
            var resSearchKeys = ResSearchKeys.Allocate(AssetBundleName,null,typeof(AssetBundle));
            var abR = ResMgr.Instance.GetRes<AssetBundleRes>(resSearchKeys);
			resSearchKeys.RecycleResObjectToCache();

#if UNITY_EDITOR
			if (AssetBundleSettings.SimulateAssetBundleInEditor && !string.Equals(mAssetName, "assetbundlemanifest"))
			{
				var assetPaths = UnityEditor.AssetDatabase.GetAssetPathsFromAssetBundleAndAssetName(abR.AssetName, mAssetName);
				if (assetPaths.Length == 0)
				{
					Debug.LogError("Failed Load Asset:" + mAssetName);
					OnResLoadFaild();
					finishCallback();
					yield break;
				}

				//确保加载过程中依赖资源不被释放:目前只有AssetRes需要处理该情况
				HoldDependRes();
				State = ResState.Loading;

				// 模拟等一帧
				yield return new WaitForEndOfFrame();
				
				UnHoldDependRes();

				if (AssetType != null)
				{

					mAsset = UnityEditor.AssetDatabase.LoadAssetAtPath(assetPaths[0],AssetType);
				}
				else
				{
					mAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPaths[0]);
				}

			}
			else
#endif
			{
				
				if (abR == null || abR.AssetBundle == null)
				{
					Debug.LogError("Failed to Load Asset, Not Find AssetBundleImage:" + AssetBundleName);
					OnResLoadFaild();
					finishCallback();
					yield break;
				}
				
				
				HoldDependRes();

				State = ResState.Loading;

				AssetBundleRequest abQ = null;
				
				if (AssetType != null)
				{
					abQ = abR.AssetBundle.LoadAssetAsync(mAssetName,AssetType);
					mAssetBundleRequest = abQ;

					yield return abQ;
				}
				else
				{
					abQ = abR.AssetBundle.LoadAssetAsync(mAssetName);
					mAssetBundleRequest = abQ;

					yield return abQ;
				}

				mAssetBundleRequest = null;

				UnHoldDependRes();

				if (!abQ.isDone)
				{
					Debug.LogError("Failed Load Asset:" + mAssetName);
					OnResLoadFaild();
					finishCallback();
					yield break;
				}

				mAsset = abQ.asset;
			}

			State = ResState.Ready;

			finishCallback();
		}

		public override string[] GetDependResList()
		{
			return mAssetBundleArray;
		}

		public override void OnRecycled()
		{
			mAssetBundleArray = null;
		}

		public override void RecycleResObjectToCache()
		{
			SingletonSafeObjectPool<AssetRes>.Instance.RecycleObject(this);
		}

		protected override float CalculateProgress()
		{
			if (mAssetBundleRequest == null)
			{
				return 0;
			}

			return mAssetBundleRequest.progress;
		}

		protected void InitAssetBundleName()
		{
			mAssetBundleArray = null;

			var resSearchKeys = ResSearchKeys.Allocate(mAssetName,mOwnerBundleName,AssetType);

			var config =  AssetBundleSettings.AssetBundleConfigFile.GetAssetData(resSearchKeys);

			resSearchKeys.RecycleResObjectToCache();

			if (config == null)
			{
				Debug.LogError("Not Find AssetData For Asset:" + mAssetName);
				return;
			}

			var assetBundleName = config.OwnerBundleName;

			if (string.IsNullOrEmpty(assetBundleName))
			{
				Debug.LogError("Not Find AssetBundle In Config:" + config.AssetBundleIndex + mOwnerBundleName);
				return;
			}

			mAssetBundleArray = new string[1];
			mAssetBundleArray[0] = assetBundleName;
		}

		public override string ToString()
		{
			return string.Format("Type:Asset\t {0}\t FromAssetBundle:{1}", base.ToString(), AssetBundleName);
		}
	}
}