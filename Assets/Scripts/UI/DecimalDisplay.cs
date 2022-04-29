using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DecimalDisplay : MonoBehaviour
{
    public TMP_Text textPrefab;

    private List<SignalGroup> displayGroups;
    private ChipInterfaceEditor signalEditor;

    private void Start()
    {
        displayGroups = new List<SignalGroup>();

        signalEditor = GetComponent<ChipInterfaceEditor>();
        signalEditor.onChipsAddedOrDeleted += RebuildGroups;
    }

    private void Update()
    {
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        for (var i = 0; i < displayGroups.Count; i++) displayGroups[i].UpdateDisplay(signalEditor);
    }

    private void RebuildGroups()
    {
        for (var i = 0; i < displayGroups.Count; i++) Destroy(displayGroups[i].text.gameObject);
        displayGroups.Clear();

        var groups = signalEditor.GetGroups();

        foreach (var group in groups)
            if (group[0].displayGroupDecimalValue)
            {
                var text = Instantiate(textPrefab);
                text.transform.SetParent(transform, true);
                displayGroups.Add(new SignalGroup {signals = group, text = text});
            }

        UpdateDisplay();
    }

    public class SignalGroup
    {
        public ChipSignal[] signals;
        public TMP_Text text;

        public void UpdateDisplay(ChipInterfaceEditor editor)
        {
            if (editor.selectedSignals.Contains(signals[0]))
            {
                text.gameObject.SetActive(false);
            }
            else
            {
                text.gameObject.SetActive(true);
                var yPos = (signals[0].transform.position.y + signals[signals.Length - 1].transform.position.y) / 2f;
                text.transform.position = new Vector3(editor.transform.position.x, yPos, -0.5f);

                var useTwosComplement = signals[0].useTwosComplement;

                var decimalValue = 0;
                for (var i = 0; i < signals.Length; i++)
                {
                    var signalState = signals[signals.Length - 1 - i].currentState;
                    if (useTwosComplement && i == signals.Length - 1)
                        decimalValue |= -(signalState << i);
                    else
                        decimalValue |= signalState << i;
                }

                text.text = decimalValue + "";
            }
        }
    }
}