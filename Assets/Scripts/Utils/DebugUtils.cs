using System;
using System.IO;
using System.Text;
using naviar.VPSService.JSONs;
using Unity.Collections;
using UnityEngine;


namespace naviar.VPSService
{
    public static class DebugUtils
    {
        /// <summary>
        /// Save images in PersistentDataPath before sending them to the VPS server.
        /// Use it to debug camera frames.
        /// </summary>
        public static bool SaveImagesLocaly = false;

        /// <summary>
        /// Name of the folder in PersistentDataPath, where all debug files are saved.
        /// </summary>
        private static string DumpsFolderName = "Dumps";

        /// <summary>
        /// Name of the folder in PersistentDataPath, where all debug files are saved.
        /// </summary>
        private static string DumpsFolderPath => Path.Combine(Application.persistentDataPath, DumpsFolderName);

        /// <summary>
        /// Compression level for debug images.
        /// </summary>
        private static int JpgCompress = 100;

        /// <summary>
        /// Save bytes buffer to PersistentDataPath dumps folder as .jpg image.
        /// If not null, suffix will be added to file name.
        /// </summary>
        public static void SaveDebugImage(NativeArray<byte> image, VPSTextureRequirement reqs, string suffix = null)
        {
            var texture = new Texture2D(reqs.Width, reqs.Height, reqs.Format, false);
            texture.LoadRawTextureData(image);
            SaveDebugImage(texture, suffix);
            GameObject.Destroy(texture);
        }

        /// <summary>
        /// Save Texture2D to PersistentDataPath dumps folder as .jpg image.
        /// If not null, suffix will be added to file name.
        /// </summary>
        public static void SaveDebugImage(Texture2D image, string suffix = null)
        {
            var img = image.EncodeToJPG(JpgCompress);
            var imgPath = SaveDebugBytes(img, DateTime.Now.ToString("dd.MM.yyyy_hh.mm.ss.fff"), "jpg", suffix);
            VPSLogger.Log(LogLevel.DEBUG, $"Saved camera image: {imgPath}");
        }

        /// <summary>
        /// Save request message to PersistentDataPath dumps folder as .json text file.
        /// If not null, suffix will be added to file name.
        /// </summary>
        public static void SaveJson(RequestStruct metaMsg, string suffix = null)
        {
            var json = DataCollector.Serialize(metaMsg);
            var encodedJson = Encoding.Unicode.GetBytes(json);

            var jsonPath = SaveDebugBytes(encodedJson, DateTime.Now.ToString("dd.MM.yyyy_hh.mm.ss.fff"), "json", suffix);
            VPSLogger.Log(LogLevel.DEBUG, $"Saved json request: {jsonPath}");
        }

        /// <summary>
        /// Save .embd file to PersistentDataPath dumps folder.
        /// If not null, suffix will be added to file name.
        /// </summary>
        public static void SaveDebugEmbd(byte[] embd, string suffix = null)
        {
            var embdPath = SaveDebugBytes(embd, DateTime.Now.ToString("dd.MM.yyyy_hh.mm.ss.fff"), "embd", suffix);
            VPSLogger.Log(LogLevel.DEBUG, $"Saved .embd file: {embdPath}");
        }


        private static string SaveDebugBytes(byte[] data, string name, string ext, string suffix = null)
        {
            // make sure that directory exist
            Directory.CreateDirectory(DumpsFolderPath);

            // save file
            var fileName = suffix == null ? $"{name}.{ext}" : $"{name}_{suffix}.{ext}";
            var filePath = Path.Combine(DumpsFolderPath, fileName);
            File.WriteAllBytes(filePath, data);
            return filePath;
        }
    }
}