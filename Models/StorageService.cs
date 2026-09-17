using System;
using System.IO;
using System.Text.Json;

namespace OSTB.Models;

public static class StorageService
{
    private static readonly string AppDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "OSTB"
    );
    private static readonly string FilePath = Path.Combine(AppDir, "appsettings.json");

    public static void SaveData(AppData data)
    {
        try
        {
            Directory.CreateDirectory(AppDir);
            string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath, json);
        }
        catch
        {
            // Suppress persistence exceptions
        }
    }

    public static AppData LoadData()
    {
        try
        {
            if (!File.Exists(FilePath))
                return new AppData();

            string json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<AppData>(json) ?? new AppData();
        }
        catch
        {
            return new AppData();
        }
    }
}