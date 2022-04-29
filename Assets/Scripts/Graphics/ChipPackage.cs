using TMPro;
using UnityEngine;

public class ChipPackage : MonoBehaviour
{
    private const string pinHolderName = "Pin Holder";

    public bool builtinChip;
    public TextMeshPro nameText;
    public Transform container;
    public Pin chipPinPrefab;

    private void Awake()
    {
        if (builtinChip)
        {
            var builtinChip = GetComponent<BuiltinChip>();
            SetSizeAndSpacing(GetComponent<Chip>());
            SetColour(builtinChip.packageColour);
        }
    }

    public void PackageCustomChip(ChipEditor chipEditor)
    {
        gameObject.name = chipEditor.chipName;
        nameText.text = chipEditor.chipName;
        nameText.color = chipEditor.chipNameColour;
        SetColour(chipEditor.chipColour);

        // Add and set up the custom chip component
        var chip = gameObject.AddComponent<CustomChip>();
        chip.chipName = chipEditor.chipName;

        // Set input signals
        chip.inputSignals = new InputSignal[chipEditor.inputsEditor.signals.Count];
        for (var i = 0; i < chip.inputSignals.Length; i++)
            chip.inputSignals[i] = (InputSignal) chipEditor.inputsEditor.signals[i];

        // Set output signals
        chip.outputSignals = new OutputSignal[chipEditor.outputsEditor.signals.Count];
        for (var i = 0; i < chip.outputSignals.Length; i++)
            chip.outputSignals[i] = (OutputSignal) chipEditor.outputsEditor.signals[i];

        // Create pins and set set package size
        SpawnPins(chip);
        SetSizeAndSpacing(chip);

        // Parent chip holder to the template, and hide
        var implementationHolder = chipEditor.chipImplementationHolder;
        implementationHolder.parent = transform;
        implementationHolder.localPosition = Vector3.zero;
        implementationHolder.gameObject.SetActive(false);
    }

    public void SpawnPins(CustomChip chip)
    {
        var pinHolder = new GameObject(pinHolderName).transform;
        pinHolder.parent = transform;
        pinHolder.localPosition = Vector3.zero;

        chip.inputPins = new Pin[chip.inputSignals.Length];
        chip.outputPins = new Pin[chip.outputSignals.Length];

        for (var i = 0; i < chip.inputPins.Length; i++)
        {
            var inputPin = Instantiate(chipPinPrefab, pinHolder.position, Quaternion.identity, pinHolder);
            inputPin.pinType = Pin.PinType.ChipInput;
            inputPin.chip = chip;
            inputPin.pinName = chip.inputSignals[i].outputPins[0].pinName;
            chip.inputPins[i] = inputPin;
            inputPin.SetScale();
        }

        for (var i = 0; i < chip.outputPins.Length; i++)
        {
            var outputPin = Instantiate(chipPinPrefab, pinHolder.position, Quaternion.identity, pinHolder);
            outputPin.pinType = Pin.PinType.ChipOutput;
            outputPin.chip = chip;
            outputPin.pinName = chip.outputSignals[i].inputPins[0].pinName;
            chip.outputPins[i] = outputPin;
            outputPin.SetScale();
        }
    }

    public void SetSizeAndSpacing(Chip chip)
    {
        nameText.fontSize = 2.5f;

        float containerHeightPadding = 0;
        var containerWidthPadding = 0.1f;
        var pinSpacePadding = Pin.radius * 0.2f;
        var containerWidth = nameText.preferredWidth + Pin.interactionRadius * 2f + containerWidthPadding;

        var numPins = Mathf.Max(chip.inputPins.Length, chip.outputPins.Length);
        var unpaddedContainerHeight = numPins * (Pin.radius * 2 + pinSpacePadding);
        var containerHeight = Mathf.Max(unpaddedContainerHeight, nameText.preferredHeight + 0.05f) +
                              containerHeightPadding;
        var topPinY = unpaddedContainerHeight / 2 - Pin.radius;
        var bottomPinY = -unpaddedContainerHeight / 2 + Pin.radius;
        const float z = -0.05f;

        // Input pins
        var numInputPinsToAutoPlace = chip.inputPins.Length;
        for (var i = 0; i < numInputPinsToAutoPlace; i++)
        {
            var percent = 0.5f;
            if (chip.inputPins.Length > 1) percent = i / (numInputPinsToAutoPlace - 1f);
            var posX = -containerWidth / 2f;

            var posY = Mathf.Lerp(topPinY, bottomPinY, percent);
            chip.inputPins[i].transform.localPosition = new Vector3(posX, posY, z);
        }

        // Output pins
        for (var i = 0; i < chip.outputPins.Length; i++)
        {
            var percent = 0.5f;
            if (chip.outputPins.Length > 1) percent = i / (chip.outputPins.Length - 1f);

            var posX = containerWidth / 2f;
            var posY = Mathf.Lerp(topPinY, bottomPinY, percent);
            chip.outputPins[i].transform.localPosition = new Vector3(posX, posY, z);
        }

        // Set container size
        container.transform.localScale = new Vector3(containerWidth, containerHeight, 1);

        GetComponent<BoxCollider2D>().size = new Vector2(containerWidth, containerHeight);
    }

    private void SetColour(Color colour)
    {
        container.GetComponent<MeshRenderer>().material.color = colour;
    }
}