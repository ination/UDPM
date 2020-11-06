using System;

using Project.Common.Pool;

namespace Project.Module.ResKit
{   
    public interface IResLoader : IPoolable, IResPoolType
    {
        void AddToLoad(string assetName, Action<bool, IRes> listener, bool lastOrder = true);
        void AddToLoad(string ownerBundleName, string assetName, Action<bool, IRes> listener, bool lastOrder = true);
        
        void ReleaseAllRes();
        void UnloadAllInstantiateRes(bool flag);
    }
}
