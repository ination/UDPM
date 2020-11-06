using UnityEditor;
using UnityEngine;
using System.ComponentModel;
using System.Linq;

using Project.Common.Extension;
using Project.Module.ResKit;

namespace Project.Module.ResKitEditor
{
	public class ResKitEditorWindow : EditorWindow
	{
        private string rootStyle = "Box";
        private bool folderout = true;

        private Vector2 scrollPos;

        private string mResVersion = "100";
        private bool mEnableGenerateClass = false;

        private int mBuildTargetIndex = 0;

        private const string key_ab_resversion = "key_ab_resversion";
        public const string key_autogenerate_class = "key_res_autogenerate_class";

        public static bool EnableGenerateClass
        {
            get
            {
                return EditorPrefs.GetBool(key_autogenerate_class, false);
            }
        }

        public ResKitEditorWindow()
        {
            this.titleContent = new GUIContent("Res Kit");
        }

        private void OnEnable()
        {

        }

        public void OnDisable()
        {
        }

        private void OnGUI()
        {
            this.OnGUILayout();
        }

        public virtual void OnGUILayout()
        {
            EditorGUILayout.BeginVertical(rootStyle);
            {
                EditorGUILayout.BeginHorizontal();
                {
                    folderout = EditorGUILayout.Foldout(folderout, "Res Kit 设置");
                }
                EditorGUILayout.EndHorizontal();

                if (folderout)
                {
                    EditorGUILayout.TextField("PersistantPath:", Application.persistentDataPath);
                    EditorGUILayout.Space(2);
                    if (GUILayout.Button("打开 Persistent 目录"))
                    {
                        EditorUtility.RevealInFinder(Application.persistentDataPath);
                    }


                    {
                        EditorGUILayout.Space(5);

                        switch (EditorUserBuildSettings.activeBuildTarget)
                        {
                            case BuildTarget.WebGL:
                                mBuildTargetIndex = 3;
                                break;
                            case BuildTarget.Android:
                                mBuildTargetIndex = 2;
                                break;
                            case BuildTarget.iOS:
                                mBuildTargetIndex = 1;
                                break;
                            default:
                                mBuildTargetIndex = 0;
                                break;
                        }
                        string[] names = { "win/osx", "iOS", "Android", "WebGL" };
                        GUILayout.Toolbar(mBuildTargetIndex, names);
                    }

                    {
                        EditorGUILayout.Space(2);

                        mEnableGenerateClass = EditorPrefs.GetBool(key_autogenerate_class, true);
                        mEnableGenerateClass = GUILayout.Toggle(mEnableGenerateClass, "打 AB 包时，自动生成资源名常量代码");
                    }

                    {
                        EditorGUILayout.Space(2);

                        bool simulateMode = AssetBundleSettings.SimulateAssetBundleInEditor;
                        simulateMode = GUILayout.Toggle(simulateMode, "模拟模式（勾选后每当资源修改时无需再打 AB 包，开发阶段建议勾选，打真机包时取消勾选并打一次 AB 包）");
                        AssetBundleSettings.SimulateAssetBundleInEditor = simulateMode;
                    }

                    {
                        EditorGUILayout.Space(2);

                        mResVersion = EditorPrefs.GetString(key_ab_resversion, "100");
                        mResVersion = EditorGUILayout.TextField("ResVesion:", mResVersion);
                    }

                    EditorGUILayout.Space(2);
                    if (GUILayout.Button("生成代码（资源名常量) "))
                    {
                        BuildScript.WriteClass();
                        AssetDatabase.Refresh();
                    }

                    EditorGUILayout.Space(2);
                    if (GUILayout.Button("打 AB 包"))
                    {
                        //EditorLifecycle.PushCommand(() =>
                        //{
                        //    var window = container.Resolve<EditorWindow>();

                        //    if (window)
                        //    {
                        //        window.Close();
                        //    }

                            BuildWithTarget(EditorUserBuildSettings.activeBuildTarget);
                        //});
                    }

                    EditorGUILayout.Space(2);
                    if (GUILayout.Button("清空已生成的 AB 包"))
                    {
                        ForceClear();
                    }



                    //标记列表
                    var abNames = AssetDatabase.GetAllAssetBundleNames().SelectMany(n =>
                    {
                        var result = AssetDatabase.GetAssetPathsFromAssetBundle(n);
                        return result.Select(r =>
                        {
                            if (ResKitAssetsMark.Marked(r))
                            {
                                return r;
                            }

                            if (ResKitAssetsMark.Marked(r.GetPathParentFolder()))
                            {
                                return r.GetPathParentFolder();
                            }

                            return null;
                        }).Where(r => r != null).Distinct();
                    });
                    if (null != abNames)
                    {
                        EditorGUILayout.Space(2);
                        EditorGUILayout.LabelField("已标记 AB 列表:");

                        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
                        {
                            EditorGUILayout.BeginVertical();

                            foreach (var name in abNames)
                            {
                                EditorGUILayout.BeginHorizontal();

                                EditorGUILayout.LabelField(name);
                                if (GUILayout.Button("选择", GUILayout.Width(50)))
                                {
                                    Selection.objects = new[]
                                    {
                                        AssetDatabase.LoadAssetAtPath<Object>(name)
                                    };
                                }

                                if (GUILayout.Button("取消标记", GUILayout.Width(75)))
                                {
                                    ResKitAssetsMark.MarkAB(name);

                                    //EditorLifecycle.PushCommand(() =>
                                    //{
                                        
                                    //});
                                }

                                EditorGUILayout.EndHorizontal();
                            }

                            EditorGUILayout.EndVertical();
                        }
                        EditorGUILayout.EndScrollView();
                    }
                }

            }
            EditorGUILayout.EndVertical();

        }


        public static void ForceClear()
        {
            ResKitAssetsMark.ABOutputPath.DeleteDirIfExists();
            (Application.streamingAssetsPath + "/AssetBundles").DeleteDirIfExists();

            AssetDatabase.Refresh();
        }

        private static void BuildWithTarget(BuildTarget buildTarget)
        {
            AssetDatabase.RemoveUnusedAssetBundleNames();
            AssetDatabase.Refresh();
            BuildScript.BuildAssetBundles(buildTarget);
        }

    }
}