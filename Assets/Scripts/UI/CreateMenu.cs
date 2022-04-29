using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class CreateMenu : MonoBehaviour
{
    public Button menuOpenButton;
    public GameObject menuHolder;
    public TMP_InputField chipNameField;
    public Button doneButton;
    public Button cancelButton;
    public Slider hueSlider;
    public Slider saturationSlider;
    public Slider valueSlider;

    [Range(0, 1)] public float textColThreshold = 0.5f;

    public Color[] suggestedColours;
    private int suggestedColourIndex;

    private void Start()
    {
        doneButton.onClick.AddListener(FinishCreation);
        menuOpenButton.onClick.AddListener(OpenMenu);
        cancelButton.onClick.AddListener(CloseMenu);

        chipNameField.onValueChanged.AddListener(ChipNameFieldChanged);
        suggestedColourIndex = Random.Range(0, suggestedColours.Length);

        hueSlider.onValueChanged.AddListener(ColourSliderChanged);
        saturationSlider.onValueChanged.AddListener(ColourSliderChanged);
        valueSlider.onValueChanged.AddListener(ColourSliderChanged);
    }

    private void Update()
    {
        if (menuHolder.activeSelf) // Force name input field to remain focused
            if (!chipNameField.isFocused)
            {
                chipNameField.Select();
                // Put caret at end of text (instead of selecting the text, which is annoying in this case)
                chipNameField.caretPosition = chipNameField.text.Length;
            }
    }

    public event Action onChipCreatePressed;

    private void ColourSliderChanged(float sliderValue)
    {
        var chipCol = Color.HSVToRGB(hueSlider.value, saturationSlider.value, valueSlider.value);
        UpdateColour(chipCol);
    }

    private void ChipNameFieldChanged(string value)
    {
        var formattedName = value.ToUpper();
        doneButton.interactable = IsValidChipName(formattedName.Trim());
        chipNameField.text = formattedName;
        Manager.ActiveChipEditor.chipName = formattedName.Trim();
    }

    private bool IsValidChipName(string chipName)
    {
        if (chipName == "AND" || chipName == "NOT" || chipName.Length == 0) return false;
        return true;
    }

    private void OpenMenu()
    {
        menuHolder.SetActive(true);
        chipNameField.text = "";
        ChipNameFieldChanged("");
        chipNameField.Select();
        SetSuggestedColour();
    }

    private void CloseMenu()
    {
        menuHolder.SetActive(false);
    }

    private void FinishCreation()
    {
        if (onChipCreatePressed != null) onChipCreatePressed.Invoke();
        CloseMenu();
    }

    private void SetSuggestedColour()
    {
        var suggestedChipColour = suggestedColours[suggestedColourIndex];
        suggestedChipColour.a = 1;
        suggestedColourIndex = (suggestedColourIndex + 1) % suggestedColours.Length;

        float hue;
        float sat;
        float val;
        Color.RGBToHSV(suggestedChipColour, out hue, out sat, out val);
        hueSlider.SetValueWithoutNotify(hue);
        saturationSlider.SetValueWithoutNotify(sat);
        valueSlider.SetValueWithoutNotify(val);
        UpdateColour(suggestedChipColour);
    }

    private void UpdateColour(Color chipCol)
    {
        var cols = chipNameField.colors;
        cols.normalColor = chipCol;
        cols.highlightedColor = chipCol;
        cols.selectedColor = chipCol;
        cols.pressedColor = chipCol;
        chipNameField.colors = cols;

        var luma = chipCol.r * 0.213f + chipCol.g * 0.715f + chipCol.b * 0.072f;
        var chipNameCol = luma > textColThreshold ? Color.black : Color.white;
        chipNameField.textComponent.color = chipNameCol;

        Manager.ActiveChipEditor.chipColour = chipCol;
        Manager.ActiveChipEditor.chipNameColour = chipNameField.textComponent.color;
    }
}