using System;

using Project.Common.Pool;

namespace Project.Module.ResKit
{
    public class ResSearchKeys : IPoolable, IResPoolType
    {   
        public string AssetName { get; set; }

        public string OwnerBundle { get;  set; }

        public Type AssetType { get; set; }

        public static ResSearchKeys Allocate(string assetName, string ownerBundleName = null, Type assetType = null)
        {
            var resSearchRule = SingletonSafeObjectPool<ResSearchKeys>.Instance.AllocateObject();
            resSearchRule.AssetName = assetName.ToLower();
            resSearchRule.OwnerBundle = ownerBundleName == null ? null : ownerBundleName.ToLower();
            resSearchRule.AssetType = assetType;
            return resSearchRule;
        }

        // public static ResSearchKeys Allocate(IRes res)
        // {
        //     var resSearchRule = SingletonSafeObjectPool<ResSearchKeys>.Instance.Allocate();
        //     res.FillInfo2ResSearchKeys(resSearchRule);
        //     return resSearchRule;
        // }

        public void RecycleResObjectToCache()
        {
            SingletonSafeObjectPool<ResSearchKeys>.Instance.RecycleObject(this);
        }

        public bool Match(IRes res)
        {
            if (res.AssetName == AssetName)
            {
                var isMatch = true;

                if (AssetType != null)
                {
                    isMatch = res.AssetType == AssetType;
                }

                if (OwnerBundle != null)
                {
                    isMatch = isMatch && res.OwnerBundleName == OwnerBundle;
                }
                 
                return isMatch;
            }
            

            return false;
        }

        public override string ToString()
        {
            return string.Format("AssetName:{0} BundleName:{1} TypeName:{2}", AssetName, OwnerBundle,
                AssetType);
        }

        void IPoolable.OnRecycled()
        {
            AssetName = null;

            OwnerBundle = null;

            AssetType = null;
        }

        bool IPoolable.IsRecycled { get; set; }
    }
}