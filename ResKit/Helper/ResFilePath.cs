using UnityEngine;
using System.Linq;
using System.IO;
using System.Collections.Generic;

namespace Project.Module.ResKit
{
	public class ResFilePath
	{
		private static string           mPersistentDataPath;
		private static string           mStreamingAssetsPath;
		private static string           mPersistentDataPath4Res;
		private static string           mPersistentDataPath4Photo;

		// 外部目录  
		public static string PersistentDataPath
		{
			get
			{
				if (null == mPersistentDataPath)
				{
					mPersistentDataPath = Application.persistentDataPath + "/";
				}

				return mPersistentDataPath;
			}
		}

		// 内部目录
		public static string StreamingAssetsPath
		{
			get
			{
				if (null == mStreamingAssetsPath)
				{
					#if UNITY_IPHONE && !UNITY_EDITOR
					mStreamingAssetsPath = Application.streamingAssetsPath + "/";
					#elif UNITY_ANDROID && !UNITY_EDITOR
					mStreamingAssetsPath = Application.streamingAssetsPath + "/";
					#elif (UNITY_STANDALONE_WIN) && !UNITY_EDITOR
					mStreamingAssetsPath = Application.streamingAssetsPath + "/";//GetParentDir(Application.dataPath, 2) + "/BuildRes/";
					#elif UNITY_STANDALONE_OSX && !UNITY_EDITOR
					mStreamingAssetsPath = Application.streamingAssetsPath + "/";
					#else
					mStreamingAssetsPath = Application.streamingAssetsPath + "/";
					#endif
				}

				return mStreamingAssetsPath;
			}
		}

		// 外部资源目录
		public static string PersistentDataPath4Res
		{
			get
			{
				if (null == mPersistentDataPath4Res)
				{
					mPersistentDataPath4Res = PersistentDataPath + "Res/";

					if (!Directory.Exists(mPersistentDataPath4Res))
					{
						Directory.CreateDirectory(mPersistentDataPath4Res);
						#if UNITY_IPHONE && !UNITY_EDITOR
						UnityEngine.iOS.Device.SetNoBackupFlag(mPersistentDataPath4Res);
						#endif
					}
				}

				return mPersistentDataPath4Res;
			}
		}

		// 外部头像缓存目录
		public static string PersistentDataPath4Photo
		{
			get
			{
				if (null == mPersistentDataPath4Photo)
				{
					mPersistentDataPath4Photo = PersistentDataPath + "Photos\\";

					if (!Directory.Exists(mPersistentDataPath4Photo))
					{
						Directory.CreateDirectory(mPersistentDataPath4Photo);
					}
					
				}

				return mPersistentDataPath4Photo;
			}
		}

		// 资源路径，优先返回外存资源路径
		public static string GetResPathInPersistentOrStream(string relativePath)
		{
			string resPersistentPath = string.Format("{0}{1}", ResFilePath.PersistentDataPath4Res, relativePath);

			if (File.Exists(resPersistentPath))
			{
				return resPersistentPath;
			}
			else
			{
				return ResFilePath.StreamingAssetsPath + relativePath;
			}
		}

		// 上一级目录
		public static string GetParentDir(string dir, int floor = 1)
		{
			string subDir = dir;

			for (int i = 0; i < floor; ++i)
			{
				int last = subDir.LastIndexOf('/');
				subDir = subDir.Substring(0, last);
			}

			return subDir;
		}

		public static void GetFileInFolder(string dirName, string fileName, List<string> outResult)
		{
			if (outResult == null)
			{
				return;
			}

			var dir = new DirectoryInfo(dirName);

			if (null != dir.Parent && dir.Attributes.ToString().IndexOf("System") > -1)
			{
				return;
			}

			var fileInfos = dir.GetFiles(fileName);
			outResult.AddRange(fileInfos.Select(fileInfo => fileInfo.FullName));

			var dirInfos = dir.GetDirectories();
			foreach (var dinfo in dirInfos)
			{
				GetFileInFolder(dinfo.FullName, fileName, outResult);
			}
		}
	}
}
