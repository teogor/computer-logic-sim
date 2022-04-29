public class CustomChip : Chip
{
    public InputSignal[] inputSignals;
    public OutputSignal[] outputSignals;

    public override void ReceiveInputSignal(Pin pin)
    {
        base.ReceiveInputSignal(pin);
    }

    protected override void ProcessOutput()
    {
        // Send signals from input pins through the chip
        for (var i = 0; i < inputPins.Length; i++) inputSignals[i].SendSignal(inputPins[i].State);

        // Pass processed signals on to ouput pins
        for (var i = 0; i < outputPins.Length; i++)
        {
            var outputState = outputSignals[i].inputPins[0].State;
            outputPins[i].ReceiveSignal(outputState);
        }
    }
}