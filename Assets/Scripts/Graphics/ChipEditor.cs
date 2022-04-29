using UnityEngine;

public class ChipEditor : MonoBehaviour
{
    public Transform chipImplementationHolder;
    public Transform wireHolder;
    public ChipInterfaceEditor inputsEditor;
    public ChipInterfaceEditor outputsEditor;
    public ChipInteraction chipInteraction;
    public PinAndWireInteraction pinAndWireInteraction;

    [HideInInspector] public string chipName;

    [HideInInspector] public Color chipColour;

    [HideInInspector] public Color chipNameColour;

    [HideInInspector] public int creationIndex;

    private void Awake()
    {
        InteractionHandler[] allHandlers = {inputsEditor, outputsEditor, chipInteraction, pinAndWireInteraction};
        foreach (var handler in allHandlers) handler.InitAllHandlers(allHandlers);

        pinAndWireInteraction.Init(chipInteraction, inputsEditor, outputsEditor);
        pinAndWireInteraction.onConnectionChanged += OnChipNetworkModified;
        GetComponentInChildren<Canvas>().worldCamera = Camera.main;
    }

    private void LateUpdate()
    {
        inputsEditor.OrderedUpdate();
        outputsEditor.OrderedUpdate();
        pinAndWireInteraction.OrderedUpdate();
        chipInteraction.OrderedUpdate();
    }

    private void OnChipNetworkModified()
    {
        CycleDetector.MarkAllCycles(this);
    }

    public void LoadFromSaveData(ChipSaveData saveData)
    {
        chipName = saveData.chipName;
        chipColour = saveData.chipColour;
        chipNameColour = saveData.chipNameColour;
        creationIndex = saveData.creationIndex;

        // Load component chips
        for (var i = 0; i < saveData.componentChips.Length; i++)
        {
            var componentChip = saveData.componentChips[i];
            if (componentChip as InputSignal)
                inputsEditor.LoadSignal(componentChip as InputSignal);
            else if (componentChip as OutputSignal)
                outputsEditor.LoadSignal(componentChip as OutputSignal);
            else
                chipInteraction.LoadChip(componentChip);
        }

        // Load wires
        if (saveData.wires != null)
            for (var i = 0; i < saveData.wires.Length; i++)
                pinAndWireInteraction.LoadWire(saveData.wires[i]);
    }
}