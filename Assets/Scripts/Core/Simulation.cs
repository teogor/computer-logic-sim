using UnityEngine;

public class Simulation : MonoBehaviour
{
    private static Simulation instance;

    public float minStepTime = 0.075f;
    private ChipEditor chipEditor;
    private InputSignal[] inputSignals;
    private float lastStepTime;

    public static int simulationFrame { get; private set; }

    private static Simulation Instance
    {
        get
        {
            if (!instance) instance = FindObjectOfType<Simulation>();
            return instance;
        }
    }

    private void Awake()
    {
        simulationFrame = 0;
    }

    private void Update()
    {
        if (Time.time - lastStepTime > minStepTime)
        {
            lastStepTime = Time.time;
            StepSimulation();
        }
    }

    private void StepSimulation()
    {
        simulationFrame++;
        RefreshChipEditorReference();

        // Clear output signals
        var outputSignals = chipEditor.outputsEditor.signals;
        for (var i = 0; i < outputSignals.Count; i++) outputSignals[i].SetDisplayState(0);

        // Init chips
        var allChips = chipEditor.chipInteraction.allChips;
        for (var i = 0; i < allChips.Count; i++) allChips[i].InitSimulationFrame();

        // Process inputs
        var inputSignals = chipEditor.inputsEditor.signals;
        // Tell all signal generators to send their signal out
        for (var i = 0; i < inputSignals.Count; i++) ((InputSignal) inputSignals[i]).SendSignal();
    }

    private void RefreshChipEditorReference()
    {
        if (chipEditor == null) chipEditor = FindObjectOfType<ChipEditor>();
    }
}