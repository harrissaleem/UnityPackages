using System.IO;
using UnityEngine;

namespace Phezu.Util
{
    public static class UserData
    {
        /// <summary>
        /// Saves value at path.
        /// </summary>
        /// <param name="path">Exclude the extension of the file at the end of the path.</param>
        public static void Save<T>(string path, T value)
        {
            string jsonValue = JsonUtility.ToJson(value);
            string filePath = Application.persistentDataPath + "/" + path + ".json";

            FileInfo file = new(filePath);
            file.Directory.Create();

            File.WriteAllText(filePath, jsonValue);
        }
        /// <summary>
        /// Loads a MonoBehavior or ScriptableObject to an already instantiated instance.
        /// </summary>
        /// <param name="path">Exclude the extension of the file at the end of the path.</param>
        /// <param name="mono">Reference to the object where the data will be written.</param>
        public static bool LoadMonoOrScriptable(string path, object mono)
        {
            string filePath = Application.persistentDataPath + "/" + path + ".json";

            if (!File.Exists(filePath))
                return false;

            string jsonValue = File.ReadAllText(filePath);

            JsonUtility.FromJsonOverwrite(jsonValue, mono);

            return true;
        }

        /// <summary>
        /// Loads anything but a MonoBehavior or ScriptableObject and returns it.
        /// </summary>
        /// <param name="path">Exclude the extension of the file at the end of the path.</param>
        /// <returns>Default value on failure.</returns>
        public static T LoadObject<T>(string path)
        {
            string filePath = Application.persistentDataPath + "/" + path + ".json";

            if (!File.Exists(filePath))
                return default;

            string jsonValue = File.ReadAllText(filePath);

            return JsonUtility.FromJson<T>(jsonValue);
        }
    }
}