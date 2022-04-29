using System;

[Serializable]
public class SavedWireLayout
{
    public SavedWire[] serializableWires;

    public SavedWireLayout(ChipSaveData chipSaveData)
    {
        var allWires = chipSaveData.wires;
        serializableWires = new SavedWire[allWires.Length];

        for (var i = 0; i < allWires.Length; i++) serializableWires[i] = new SavedWire(chipSaveData, allWires[i]);
    }
}