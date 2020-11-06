using UnityEngine;
using UnityEditor;
using System.IO;

namespace Project.Module.ResKitEditor
{
    static internal class ResKitMenuItem
    {
        [MenuItem(ResKitAssetsMark.MarkMenuName)]
        public static void MarkAssetBundleDir()
        {
            var path = ResKitAssetsMark.GetSelectedPathOrFallback();
            ResKitAssetsMark.MarkAB(path);
        }

        //[MenuItem("Window/Res Kit %#r")]
        [MenuItem("Window/ResKit/ResKit AB Tool")]
        public static void ShowResKitWindow()
        {
            var window = (ResKitEditorWindow)EditorWindow.GetWindow(typeof(ResKitEditorWindow), true);
            window.position = new Rect(100, 100, 800, 600);
            window.Show();
        }
    }
}