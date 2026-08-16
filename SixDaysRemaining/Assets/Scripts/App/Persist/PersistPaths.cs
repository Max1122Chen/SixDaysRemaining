using System.IO;
using UnityEngine;

namespace SixDaysRemaining.App.Persist
{
    /// <summary>
    /// 双层存档路径：meta-profile / run-save（分文件）。
    /// </summary>
    public static class PersistPaths
    {
        public const string FolderName = "SixDaysRemaining";
        public const string MetaProfileFileName = "meta-profile.json";
        public const string RunSaveFileName = "run-save.json";

        private static string rootOverride;

        public static string RootDirectory
        {
            get
            {
                if (!string.IsNullOrEmpty(rootOverride))
                {
                    return rootOverride;
                }

                return Path.Combine(Application.persistentDataPath, FolderName);
            }
        }

        public static string MetaProfilePath
        {
            get { return Path.Combine(RootDirectory, MetaProfileFileName); }
        }

        public static string RunSavePath
        {
            get { return Path.Combine(RootDirectory, RunSaveFileName); }
        }

        /// <summary>EditMode 注入临时根目录；传 null 恢复默认。</summary>
        public static void SetRootOverrideForTests(string absoluteRootOrNull)
        {
            rootOverride = absoluteRootOrNull;
        }
    }
}
