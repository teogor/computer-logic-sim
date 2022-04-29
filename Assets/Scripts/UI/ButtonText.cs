using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonText : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Button button;
    public TMP_Text buttonText;
    public Color normalCol = Color.white;
    public Color nonInteractableCol = Color.grey;
    public Color highlightedCol = Color.white;
    private bool highlighted;

    private void Update()
    {
        var col = highlighted ? highlightedCol : normalCol;
        buttonText.color = button.interactable ? col : nonInteractableCol;
        if (!Input.GetKeyDown(KeyCode.Delete) || !highlighted) return;
        var text = buttonText.text;
        if (text != "AND" && text != "NOT")
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        highlighted = false;
    }

    private void OnValidate()
    {
        if (button == null) button = GetComponent<Button>();
        if (buttonText == null) buttonText = transform.GetComponentInChildren<TMP_Text>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (button.interactable) highlighted = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        highlighted = false;
    }
}