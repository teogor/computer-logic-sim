

// Provides input signal (0 or 1) to a chip.
// When designing a chip, this input signal can be manually set to 0 or 1 by the player.
public class InputSignal : ChipSignal
{
    protected override void Start()
    {
        base.Start();
        SetCol();
    }

    private void OnMouseDown()
    {
        ToggleActive();
    }

    public void ToggleActive()
    {
        currentState = 1 - currentState;
        SetCol();
    }

    public void SendSignal(int signal)
    {
        currentState = signal;
        outputPins[0].ReceiveSignal(signal);
        SetCol();
    }

    public void SendSignal()
    {
        outputPins[0].ReceiveSignal(currentState);
    }

    private void SetCol()
    {
        SetDisplayState(currentState);
    }

    public override void UpdateSignalName(string newName)
    {
        base.UpdateSignalName(newName);
        outputPins[0].pinName = newName;
    }
}