using UnityEngine;
using System.Collections;

namespace Project.Module.ResKit
{
    public class LocalImageResCreator : IResCreator
    {
        public bool Match(ResSearchKeys resSearchKeys)
        {
            return resSearchKeys.AssetName.StartsWith("localimage:");
        }

        public IRes Create(ResSearchKeys resSearchKeys)
        {
            return NetImageRes.Allocate(resSearchKeys.AssetName);
        }
    }
}

