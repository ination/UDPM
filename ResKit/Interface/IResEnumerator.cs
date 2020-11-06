using System;
using System.Collections;

namespace Project.Module.ResKit
{
    public interface IResEnumerator
    {
        IEnumerator DoLoadAsync(System.Action finishCallback);
    }
}
