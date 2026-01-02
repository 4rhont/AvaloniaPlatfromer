using System.IO;
using System.Text.Json;

namespace GameApp.Core.Levels
{
    public static class LevelLoader
    {
        public static LevelData Load(string levelId)
        {
            var path = Path.Combine(
                AppContext.BaseDirectory,
                "Levels",
                "LevelsData",
                $"{levelId}.json"
            );

            if (!File.Exists(path))
                throw new FileNotFoundException($"Level not found: {path}");

            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<LevelData>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                })!;
        }
    }
}
