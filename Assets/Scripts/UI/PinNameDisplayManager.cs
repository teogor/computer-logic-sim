using System.Collections.Generic;
using UnityEngine;

public class PinNameDisplayManager : MonoBehaviour
{
    public PinNameDisplay pinNamePrefab;
    private ChipEditor chipEditor;
    private ChipEditorOptions editorDisplayOptions;
    private Pin highlightedPin;

    private List<PinNameDisplay> pinNameDisplays;
    private List<Pin> pinsToDisplay;

    private void Awake()
    {
        chipEditor = FindObjectOfType<ChipEditor>();
        editorDisplayOptions = FindObjectOfType<ChipEditorOptions>();
        chipEditor.pinAndWireInteraction.onMouseOverPin += OnMouseOverPin;
        chipEditor.pinAndWireInteraction.onMouseExitPin += OnMouseExitPin;

        pinNameDisplays = new List<PinNameDisplay>();
        pinsToDisplay = new List<Pin>();
    }

    private void LateUpdate()
    {
        var mode = editorDisplayOptions.activePinNameDisplayMode;
        pinsToDisplay.Clear();

        if (mode == ChipEditorOptions.PinNameDisplayMode.AlwaysMain ||
            mode == ChipEditorOptions.PinNameDisplayMode.AlwaysAll)
        {
            if (mode == ChipEditorOptions.PinNameDisplayMode.AlwaysAll)
                foreach (var chip in chipEditor.chipInteraction.allChips)
                {
                    pinsToDisplay.AddRange(chip.inputPins);
                    pinsToDisplay.AddRange(chip.outputPins);
                }

            foreach (var chip in chipEditor.inputsEditor.signals)
                if (!chipEditor.inputsEditor.selectedSignals.Contains(chip))
                    pinsToDisplay.AddRange(chip.outputPins);
            foreach (var chip in chipEditor.outputsEditor.signals)
                if (!chipEditor.outputsEditor.selectedSignals.Contains(chip))
                    pinsToDisplay.AddRange(chip.inputPins);
        }

        if (highlightedPin)
        {
            var nameDisplayKey = InputHelper.AnyOfTheseKeysHeld(KeyCode.LeftAlt, KeyCode.RightAlt);
            if (nameDisplayKey || mode == ChipEditorOptions.PinNameDisplayMode.Hover) pinsToDisplay.Add(highlightedPin);
        }

        DisplayPinName(pinsToDisplay);
    }

    public void DisplayPinName(List<Pin> pins)
    {
        if (pinNameDisplays.Count < pins.Count)
        {
            var numToAdd = pins.Count - pinNameDisplays.Count;
            for (var i = 0; i < numToAdd; i++) pinNameDisplays.Add(Instantiate(pinNamePrefab, transform));
        }
        else if (pinNameDisplays.Count > pins.Count)
        {
            for (var i = pins.Count; i < pinNameDisplays.Count; i++) pinNameDisplays[i].gameObject.SetActive(false);
        }

        for (var i = 0; i < pins.Count; i++)
        {
            pinNameDisplays[i].gameObject.SetActive(true);
            pinNameDisplays[i].Set(pins[i]);
        }
    }

    private void OnMouseOverPin(Pin pin)
    {
        highlightedPin = pin;
    }

    private void OnMouseExitPin(Pin pin)
    {
        if (highlightedPin == pin) highlightedPin = null;
    }
}