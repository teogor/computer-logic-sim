using UnityEngine;

public class Chip : MonoBehaviour
{
    public string chipName = "Untitled";
    public Pin[] inputPins;
    public Pin[] outputPins;

    // Cached components
    [HideInInspector] public BoxCollider2D bounds;

    private int lastSimulatedFrame;
    private int lastSimulationInitFrame;

    // Number of input signals received (on current simulation step)
    private int numInputSignalsReceived;

    public Vector2 BoundsSize => bounds.size;

    protected virtual void Awake()
    {
        bounds = GetComponent<BoxCollider2D>();
    }

    protected virtual void Start()
    {
        SetPinIndices();
    }

    public void InitSimulationFrame()
    {
        if (lastSimulationInitFrame != Simulation.simulationFrame)
        {
            lastSimulationInitFrame = Simulation.simulationFrame;
            ProcessCycleAndUnconnectedInputs();
        }
    }

    // Receive input signal from pin: either pin has power, or pin does not have power.
    // Once signals from all input pins have been received, calls the ProcessOutput() function.
    public virtual void ReceiveInputSignal(Pin pin)
    {
        // Reset if on new step of simulation
        if (lastSimulatedFrame != Simulation.simulationFrame)
        {
            lastSimulatedFrame = Simulation.simulationFrame;
            numInputSignalsReceived = 0;
            InitSimulationFrame();
        }

        numInputSignalsReceived++;

        if (numInputSignalsReceived == inputPins.Length) ProcessOutput();
    }

    private void ProcessCycleAndUnconnectedInputs()
    {
        for (var i = 0; i < inputPins.Length; i++)
            if (inputPins[i].cyclic)
                ReceiveInputSignal(inputPins[i]);
            else if (!inputPins[i].parentPin) inputPins[i].ReceiveSignal(0);
            //ReceiveInputSignal (inputPins[i]);
    }

    // Called once all inputs to the component are known.
    // Sends appropriate output signals to output pins
    protected virtual void ProcessOutput()
    {
    }

    private void SetPinIndices()
    {
        for (var i = 0; i < inputPins.Length; i++) inputPins[i].index = i;
        for (var i = 0; i < outputPins.Length; i++) outputPins[i].index = i;
    }
}