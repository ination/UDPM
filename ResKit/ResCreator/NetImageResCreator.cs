using UnityEngine;
using System.Collections;

namespace Project.Module.ResKit
{
    public class NetImageResCreator : IResCreator
    {
        public bool Match(ResSearchKeys resSearchKeys)
        {
            return resSearchKeys.AssetName.StartsWith("netimage:");
        }

        public IRes Create(ResSearchKeys resSearchKeys)
        {
            return NetImageRes.Allocate(resSearchKeys.AssetName);
        }
    }
}

