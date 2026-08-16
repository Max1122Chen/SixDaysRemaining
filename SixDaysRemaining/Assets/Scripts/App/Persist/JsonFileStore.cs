using System;
using System.IO;
using UnityEngine;

namespace SixDaysRemaining.App.Persist
{
    /// <summary>
    /// 薄 JSON 文件读写：原子写、坏档不抛未处理异常。
    /// </summary>
    public static class JsonFileStore
    {
        public static bool Exists(string path)
        {
            return !string.IsNullOrEmpty(path) && File.Exists(path);
        }

        public static bool TryLoad<T>(string path, out T data, out string error)
        {
            data = default(T);
            error = null;
            if (string.IsNullOrEmpty(path))
            {
                error = "path empty";
                return false;
            }

            if (!File.Exists(path))
            {
                error = "file missing";
                return false;
            }

            try
            {
                string json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json))
                {
                    error = "file empty";
                    return false;
                }

                data = JsonUtility.FromJson<T>(json);
                if (data == null)
                {
                    error = "json parse null";
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                data = default(T);
                return false;
            }
        }

        public static bool Save<T>(string path, T data, out string error)
        {
            error = null;
            if (string.IsNullOrEmpty(path))
            {
                error = "path empty";
                return false;
            }

            if (data == null)
            {
                error = "data null";
                return false;
            }

            string tmp = path + ".tmp";
            try
            {
                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(tmp, json);
                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                File.Move(tmp, path);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                try
                {
                    if (File.Exists(tmp))
                    {
                        File.Delete(tmp);
                    }
                }
                catch
                {
                    // ignore cleanup failure
                }

                return false;
            }
        }

        public static bool TryDelete(string path, out string error)
        {
            error = null;
            if (string.IsNullOrEmpty(path))
            {
                error = "path empty";
                return false;
            }

            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                string bak = path + ".bak";
                if (File.Exists(bak))
                {
                    File.Delete(bak);
                }

                string tmp = path + ".tmp";
                if (File.Exists(tmp))
                {
                    File.Delete(tmp);
                }

                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public static bool TryBackup(string path, out string error)
        {
            error = null;
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return true;
            }

            try
            {
                string bak = path + ".bak";
                File.Copy(path, bak, true);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }
    }
}
