using System;
using UnityEngine;

[Serializable]
public class SavedWire
{
    public int parentChipIndex;
    public int parentChipOutputIndex;
    public int childChipIndex;
    public int childChipInputIndex;
    public Vector2[] anchorPoints;

    public SavedWire(ChipSaveData chipSaveData, Wire wire)
    {
        var parentPin = wire.startPin;
        var childPin = wire.endPin;

        parentChipIndex = chipSaveData.ComponentChipIndex(parentPin.chip);
        parentChipOutputIndex = parentPin.index;

        childChipIndex = chipSaveData.ComponentChipIndex(childPin.chip);
        childChipInputIndex = childPin.index;

        anchorPoints = wire.anchorPoints.ToArray();
    }
}