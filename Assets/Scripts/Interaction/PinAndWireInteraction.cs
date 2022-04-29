using System;
using System.Collections.Generic;
using UnityEngine;

public class PinAndWireInteraction : InteractionHandler
{
    public LayerMask pinMask;
    public LayerMask wireMask;
    public Transform wireHolder;
    public Wire wirePrefab;

    private State currentState;
    private Wire highlightedWire;
    private Pin pinUnderMouse;
    private Dictionary<Pin, Wire> wiresByChipInputPin;
    private Pin wireStartPin;
    private Wire wireToPlace;
    public List<Wire> allWires { get; private set; }

    private void Awake()
    {
        allWires = new List<Wire>();
        wiresByChipInputPin = new Dictionary<Pin, Wire>();
    }

    public event Action onConnectionChanged;
    public event Action<Pin> onMouseOverPin;
    public event Action<Pin> onMouseExitPin;

    public void Init(ChipInteraction chipInteraction, ChipInterfaceEditor inputEditor, ChipInterfaceEditor outputEditor)
    {
        chipInteraction.onDeleteChip += DeleteChipWires;
        inputEditor.onDeleteChip += DeleteChipWires;
        outputEditor.onDeleteChip += DeleteChipWires;
    }

    public override void OrderedUpdate()
    {
        var mouseOverUI = InputHelper.MouseOverUIObject();

        if (!mouseOverUI)
        {
            HandlePinHighlighting();

            switch (currentState)
            {
                case State.None:
                    HandleWireHighlighting();
                    HandleWireDeletion();
                    HandleWireCreation();
                    break;
                case State.PlacingWire:
                    HandleWirePlacement();
                    break;
            }
        }
    }

    public void LoadWire(Wire wire)
    {
        wire.transform.parent = wireHolder;
        allWires.Add(wire);
        wiresByChipInputPin.Add(wire.ChipInputPin, wire);
    }

    private void HandleWireHighlighting()
    {
        var wireUnderMouse = InputHelper.GetObjectUnderMouse2D(wireMask);
        if (wireUnderMouse && pinUnderMouse == null)
        {
            if (highlightedWire) highlightedWire.SetSelectionState(false);
            highlightedWire = wireUnderMouse.GetComponent<Wire>();
            highlightedWire.SetSelectionState(true);
        }
        else if (highlightedWire)
        {
            highlightedWire.SetSelectionState(false);
            highlightedWire = null;
        }
    }

    private void HandleWireDeletion()
    {
        if (highlightedWire)
            if (InputHelper.AnyOfTheseKeysDown(KeyCode.Backspace, KeyCode.Delete))
            {
                RequestFocus();
                if (HasFocus)
                {
                    DestroyWire(highlightedWire);
                    onConnectionChanged?.Invoke();
                }
            }
    }

    private void HandleWirePlacement()
    {
        // Cancel placing wire
        if (InputHelper.AnyOfTheseKeysDown(KeyCode.Escape, KeyCode.Backspace, KeyCode.Delete) ||
            Input.GetMouseButtonDown(1))
        {
            StopPlacingWire();
        }
        // Update wire position and check if user wants to try connect the wire
        else
        {
            var mousePos = InputHelper.MouseWorldPos;

            wireToPlace.UpdateWireEndPoint(mousePos);

            // Left mouse press
            if (Input.GetMouseButtonDown(0))
            {
                // If mouse pressed over pin, try connecting the wire to that pin
                if (pinUnderMouse)
                    TryPlaceWire(wireStartPin, pinUnderMouse);
                // If mouse pressed over empty space, add anchor point to wire
                else
                    wireToPlace.AddAnchorPoint(mousePos);
            }
            // Left mouse release
            else if (Input.GetMouseButtonUp(0))
            {
                if (pinUnderMouse && pinUnderMouse != wireStartPin) TryPlaceWire(wireStartPin, pinUnderMouse);
            }
        }
    }

    public Wire GetWire(Pin childPin)
    {
        if (wiresByChipInputPin.ContainsKey(childPin)) return wiresByChipInputPin[childPin];
        return null;
    }

    private void TryPlaceWire(Pin startPin, Pin endPin)
    {
        if (Pin.IsValidConnection(startPin, endPin))
        {
            var chipInputPin = startPin.pinType == Pin.PinType.ChipInput ? startPin : endPin;
            RemoveConflictingWire(chipInputPin);

            wireToPlace.Place(endPin);
            Pin.MakeConnection(startPin, endPin);
            allWires.Add(wireToPlace);
            wiresByChipInputPin.Add(chipInputPin, wireToPlace);
            wireToPlace = null;
            currentState = State.None;

            onConnectionChanged?.Invoke();
        }
        else
        {
            StopPlacingWire();
        }
    }

    // Pin cannot have multiple inputs, so when placing a new wire, first remove the wire that already goes to that pin (if there is one)
    private void RemoveConflictingWire(Pin chipInputPin)
    {
        if (wiresByChipInputPin.ContainsKey(chipInputPin)) DestroyWire(wiresByChipInputPin[chipInputPin]);
    }

    private void DestroyWire(Wire wire)
    {
        wiresByChipInputPin.Remove(wire.ChipInputPin);
        allWires.Remove(wire);
        Pin.RemoveConnection(wire.startPin, wire.endPin);
        Destroy(wire.gameObject);
    }

    private void HandleWireCreation()
    {
        if (Input.GetMouseButtonDown(
                0)) // Wire can be created from a pin, or from another wire (in which case it uses that wire's start pin)
            if (pinUnderMouse || highlightedWire)
            {
                RequestFocus();
                if (HasFocus)
                {
                    currentState = State.PlacingWire;
                    wireToPlace = Instantiate(wirePrefab, wireHolder);

                    // Creating new wire starting from pin
                    if (pinUnderMouse)
                    {
                        wireStartPin = pinUnderMouse;
                        wireToPlace.ConnectToFirstPin(wireStartPin);
                    }
                    // Creating new wire starting from existing wire
                    else if (highlightedWire)
                    {
                        wireStartPin = highlightedWire.ChipOutputPin;
                        wireToPlace.ConnectToFirstPinViaWire(wireStartPin, highlightedWire, InputHelper.MouseWorldPos);
                    }
                }
            }
    }

    private void HandlePinHighlighting()
    {
        var mousePos = InputHelper.MouseWorldPos;
        var pinCollider = Physics2D.OverlapCircle(mousePos, Pin.interactionRadius - Pin.radius, pinMask);
        if (pinCollider)
        {
            var newPinUnderMouse = pinCollider.GetComponent<Pin>();
            if (pinUnderMouse != newPinUnderMouse)
            {
                if (pinUnderMouse != null)
                {
                    pinUnderMouse.MouseExit();
                    onMouseExitPin?.Invoke(pinUnderMouse);
                }

                newPinUnderMouse.MouseEnter();
                pinUnderMouse = newPinUnderMouse;
                onMouseOverPin?.Invoke(pinUnderMouse);
            }
        }
        else
        {
            if (pinUnderMouse)
            {
                pinUnderMouse.MouseExit();
                onMouseExitPin?.Invoke(pinUnderMouse);
                pinUnderMouse = null;
            }
        }
    }

    // Delete all wires connected to given chip
    private void DeleteChipWires(Chip chip)
    {
        var wiresToDestroy = new List<Wire>();

        foreach (var outputPin in chip.outputPins)
        foreach (var childPin in outputPin.childPins)
            wiresToDestroy.Add(wiresByChipInputPin[childPin]);

        foreach (var inputPin in chip.inputPins)
            if (inputPin.parentPin)
                wiresToDestroy.Add(wiresByChipInputPin[inputPin]);

        for (var i = 0; i < wiresToDestroy.Count; i++) DestroyWire(wiresToDestroy[i]);
        onConnectionChanged?.Invoke();
    }

    private void StopPlacingWire()
    {
        if (wireToPlace)
        {
            Destroy(wireToPlace.gameObject);
            wireToPlace = null;
            wireStartPin = null;
        }

        currentState = State.None;
    }

    protected override void FocusLost()
    {
        if (pinUnderMouse)
        {
            pinUnderMouse.MouseExit();
            pinUnderMouse = null;
        }

        if (highlightedWire)
        {
            highlightedWire.SetSelectionState(false);
            highlightedWire = null;
        }

        currentState = State.None;
    }

    protected override bool CanReleaseFocus()
    {
        if (currentState == State.PlacingWire || pinUnderMouse) return false;

        return true;
    }

    private enum State
    {
        None,
        PlacingWire
    }
}