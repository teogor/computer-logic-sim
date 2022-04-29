using System.Diagnostics;
using System.IO;
using UnityEngine;
using Debug = UnityEngine.Debug;

public static class SaveSystem
{
    private const string fileExtension = ".txt";

    private static string activeProjectName = "Untitled";

    private static string CurrentSaveProfileDirectoryPath => Path.Combine(SaveDataDirectoryPath, activeProjectName);

    private static string CurrentSaveProfileWireLayoutDirectoryPath =>
        Path.Combine(CurrentSaveProfileDirectoryPath, "WireLayout");

    public static string SaveDataDirectoryPath
    {
        get
        {
            const string saveFolderName = "SaveData";
            return Path.Combine(Application.persistentDataPath, saveFolderName);
        }
    }

    public static void SetActiveProject(string projectName)
    {
        activeProjectName = projectName;
    }

    public static void Init()
    {
        // Create save directory (if doesn't exist already)
        Directory.CreateDirectory(CurrentSaveProfileDirectoryPath);
        Directory.CreateDirectory(CurrentSaveProfileWireLayoutDirectoryPath);
    }

    public static void LoadAll(Manager manager)
    {
        // Load any saved chips
        var sw = Stopwatch.StartNew();
        var chipSavePaths = Directory.GetFiles(CurrentSaveProfileDirectoryPath, "*" + fileExtension);
        ChipLoader.LoadAllChips(chipSavePaths, manager);
        Debug.Log("Load time: " + sw.ElapsedMilliseconds);
    }

    public static string GetPathToSaveFile(string saveFileName)
    {
        return Path.Combine(CurrentSaveProfileDirectoryPath, saveFileName + fileExtension);
    }

    public static string GetPathToWireSaveFile(string saveFileName)
    {
        return Path.Combine(CurrentSaveProfileWireLayoutDirectoryPath, saveFileName + fileExtension);
    }

    public static string[] GetSaveNames()
    {
        var savedProjectPaths = new string[0];
        if (Directory.Exists(SaveDataDirectoryPath))
            savedProjectPaths = Directory.GetDirectories(SaveDataDirectoryPath);
        for (var i = 0; i < savedProjectPaths.Length; i++)
        {
            var pathSections = savedProjectPaths[i].Split(Path.DirectorySeparatorChar);
            savedProjectPaths[i] = pathSections[pathSections.Length - 1];
        }

        return savedProjectPaths;
    }
}