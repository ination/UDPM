using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Project.Module.ResKit
{
    public static class ResFactory
    {
        public static IRes Create(ResSearchKeys resSearchKeys)
        {
            var retRes = mResCreators
                .Where(creator => creator.Match(resSearchKeys))
                .Select(creator => creator.Create(resSearchKeys))
                .FirstOrDefault();

            if (retRes == null)
            {
                Debug.LogError("Failed to Create Res. Not Find By ResSearchKeys:" + resSearchKeys);
                return null;
            }

            return retRes;
        }

        public static void AddResCreator<T>() where T : IResCreator, new()
        {
            mResCreators.Add(new T());
        }

        public static void RemoveResCreator<T>() where T : IResCreator, new()
        {
            mResCreators.RemoveAll(r => r.GetType() == typeof(T));
        }

        public static void AddResCreator(IResCreator resCreator)
        {
            mResCreators.Add(resCreator);
        }

        static List<IResCreator> mResCreators = new List<IResCreator>()
        {
            new ResourcesResCreator(),
            new AssetBundleResCreator(),
            new AssetResCreator(),
            new AssetBundleSceneResCreator(),
            new NetImageResCreator(),
            new LocalImageResCreator()
        };
    }
    
}