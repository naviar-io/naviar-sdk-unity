using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace TensorFlowLite
{
    public static class FileUtil
    {
        public static byte[] LoadFile(string path)
        {
#if !UNITY_ANDROID || UNITY_EDITOR
            path = "file://" + path;
#endif
            var request = UnityWebRequest.Get(path);
            request.SendWebRequest();
            while (!request.isDone)
            {
            }
            return request.downloadHandler.data;
        }

        static bool IsPathRooted(string path)
        {
            if (path.StartsWith("jar:file:"))
            {
                return true;
            }
            return Path.IsPathRooted(path);
        }
    }
}
