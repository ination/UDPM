using System;
using System.Collections.Generic;
using UnityEngine;

using Project.Common.RefCounter;

namespace Project.Module.ResKit
{
    
    public enum ResState
    {
        Waiting = 0,
        Loading = 1,
        Ready   = 2,
    }

    public static class ResLoadType
    {
        public const short AssetBundle   = 0;
        public const short ABAsset       = 1;
        public const short ABScene       = 2;
        public const short Internal      = 3;
        public const short NetImageRes   = 4;
        public const short LocalImageRes = 5;
    }



    public interface IRes : IRefCounter, IResPoolType, IResEnumerator
    {
        string AssetName { get; }

        string OwnerBundleName { get; }

        ResState State { get; }

        UnityEngine.Object Asset { get; }

        float Progress { get; }
        Type AssetType { get; set; }

        void RegisteOnResLoadDoneEvent(Action<bool, IRes> listener);
        void UnRegisteOnResLoadDoneEvent(Action<bool, IRes> listener);

        bool UnloadImage(bool flag);

        bool LoadSync();

        void LoadAsync();

        string[] GetDependResList();

        bool IsDependResLoadFinish();

        bool ReleaseRes();
        
    }
}