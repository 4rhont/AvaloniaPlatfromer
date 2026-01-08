using System;
using System.IO;
using System.Text.Json;

namespace GameApp.Core.Services
{
    public class SaveGameData
    {
        public string CurrentLevelId { get; set; } = "level1";
        public int PlayerHealth { get; set; } = 5;
        public DateTime SaveTime { get; set; } = DateTime.Now;
        public int Version { get; set; } = 1;
    }

    public static class SaveSystemService
    {
        private static readonly string SaveDirectory =
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "GameApp",
                "Saves"
            );

        private static readonly string SaveFilePath =
            Path.Combine(SaveDirectory, "save.json");

        public static void SaveGame(SaveGameData data)
        {
            try
            {
                if (!Directory.Exists(SaveDirectory))
                    Directory.CreateDirectory(SaveDirectory);

                var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                File.WriteAllText(SaveFilePath, json);

                System.Diagnostics.Debug.WriteLine(
                    $"[SaveSystem] Game saved: level={data.CurrentLevelId}, hp={data.PlayerHealth}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[SaveSystem] Error saving game: {ex.Message}");
                throw;
            }
        }

        public static SaveGameData? LoadGame()
        {
            try
            {
                if (!File.Exists(SaveFilePath))
                    return null;

                var json = File.ReadAllText(SaveFilePath);
                var data = JsonSerializer.Deserialize<SaveGameData>(json);

                System.Diagnostics.Debug.WriteLine(
                    $"[SaveSystem] Game loaded: level={data?.CurrentLevelId}, hp={data?.PlayerHealth}");

                return data;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[SaveSystem] Error loading game: {ex.Message}");
                return null;
            }
        }

        public static bool HasSave()
        {
            return File.Exists(SaveFilePath);
        }

        public static void DeleteSave()
        {
            try
            {
                if (File.Exists(SaveFilePath))
                    File.Delete(SaveFilePath);

                System.Diagnostics.Debug.WriteLine("[SaveSystem] Save deleted");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[SaveSystem] Error deleting save: {ex.Message}");
            }
        }

        public static DateTime? GetSaveTime()
        {
            var data = LoadGame();
            return data?.SaveTime;
        }
    }
}
