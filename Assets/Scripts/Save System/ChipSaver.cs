using System.IO;
using UnityEngine;

public static class ChipSaver
{
    private const bool usePrettyPrint = true;

    public static void Save(ChipEditor chipEditor)
    {
        var chipSaveData = new ChipSaveData(chipEditor);

        // Generate new chip save string
        var compositeChip = new SavedChip(chipSaveData);
        var saveString = JsonUtility.ToJson(compositeChip, usePrettyPrint);

        // Generate save string for wire layout
        var wiringSystem = new SavedWireLayout(chipSaveData);
        var wiringSaveString = JsonUtility.ToJson(wiringSystem, usePrettyPrint);

        // Write to file
        var savePath = SaveSystem.GetPathToSaveFile(chipEditor.chipName);
        using (var writer = new StreamWriter(savePath))
        {
            writer.Write(saveString);
        }

        var wireLayoutSavePath = SaveSystem.GetPathToWireSaveFile(chipEditor.chipName);
        using (var writer = new StreamWriter(wireLayoutSavePath))
        {
            writer.Write(wiringSaveString);
        }
    }
}