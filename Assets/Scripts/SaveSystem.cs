using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;

namespace PhotalFrame.Save
{
    [Serializable]
    public class SaveData
    {
        // Player position/rotation
        public float posX, posY, posZ;
        public float rotX, rotY, rotZ, rotW;

        // Health
        public float currentHealth;

        // Upgrades
        public int spiritPoints;
        public int spiritOrbs;
        public int powerLevel;
        public int reloadLevel;
        public int rangeLevel;

        // Inventory
        public int filmType61Count;
        public int filmType90Count;
        public int herbalMedicineCount;
        public int virginTapeCount;

        // Game state
        public bool ghostTestDefeated;
        public List<string> collectedItemIds;
    }

    public static class SaveSystem
    {
        private static string SavePath => Application.persistentDataPath + "/savegame.json";
        private static bool shouldLoadOnStart = false;

        public static bool ShouldLoadOnStart
        {
            get => shouldLoadOnStart;
            set => shouldLoadOnStart = value;
        }

        public static bool SaveExists()
        {
            return File.Exists(SavePath);
        }

        public static void Save(SaveData data)
        {
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(SavePath, json);
            Debug.Log("Game saved to: " + SavePath);
        }

        public static SaveData Load()
        {
            if (!File.Exists(SavePath))
            {
                Debug.LogWarning("No save file found at: " + SavePath);
                return null;
            }

            string json = File.ReadAllText(SavePath);
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            Debug.Log("Game loaded from: " + SavePath);
            return data;
        }

        public static void DeleteSave()
        {
            if (File.Exists(SavePath))
            {
                File.Delete(SavePath);
                Debug.Log("Save file deleted.");
            }
        }
    }
}
