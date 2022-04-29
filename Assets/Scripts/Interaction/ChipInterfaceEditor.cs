using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Allows player to add/remove/move/rename inputs or outputs of a chip.
public class ChipInterfaceEditor : InteractionHandler
{
    public enum EditorType
    {
        Input,
        Output
    }

    public enum HandleState
    {
        Default,
        Highlighted,
        Selected
    }

    private const int maxGroupSize = 8;
    private const float forwardDepth = -0.1f;

    public EditorType editorType;

    [Header("References")] public Transform chipContainer;

    public ChipSignal signalPrefab;
    public RectTransform propertiesUI;
    public TMP_InputField nameField;
    public Button deleteButton;
    public Toggle twosComplementToggle;
    public Transform signalHolder;

    [Header("Appearance")] public Vector2 handleSize;

    public Color handleCol;
    public Color highlightedHandleCol;
    public Color selectedHandleCol;
    public float propertiesUIX;
    public Vector2 propertiesHeightMinMax;
    public bool showPreviewSignal;
    public float groupSpacing = 1;
    private int currentGroupID;

    // Grouping
    private int currentGroupSize = 1;
    private float dragHandleStartY;
    private float dragMouseStartY;
    private Dictionary<int, ChipSignal[]> groupsByID;
    private Material handleMat;
    private Material highlightedHandleMat;

    private ChipSignal highlightedSignal;

    private BoxCollider2D inputBounds;

    // Dragging
    private bool isDragging;
    private bool mouseInInputBounds;
    private ChipSignal[] previewSignals;

    private Mesh quadMesh;
    private Material selectedHandleMat;

    public List<ChipSignal> signals { get; private set; }
    public List<ChipSignal> selectedSignals { get; private set; }

    private float BoundsTop => transform.position.y + transform.localScale.y / 2;

    private float BoundsBottom => transform.position.y - transform.localScale.y / 2f;

    private void Awake()
    {
        signals = new List<ChipSignal>();
        selectedSignals = new List<ChipSignal>();
        groupsByID = new Dictionary<int, ChipSignal[]>();

        inputBounds = GetComponent<BoxCollider2D>();
        MeshShapeCreator.CreateQuadMesh(ref quadMesh);
        handleMat = CreateUnlitMaterial(handleCol);
        highlightedHandleMat = CreateUnlitMaterial(highlightedHandleCol);
        selectedHandleMat = CreateUnlitMaterial(selectedHandleCol);

        previewSignals = new ChipSignal[maxGroupSize];
        for (var i = 0; i < maxGroupSize; i++)
        {
            var previewSignal = Instantiate(signalPrefab);
            previewSignal.SetInteractable(false);
            previewSignal.gameObject.SetActive(false);
            previewSignal.signalName = "Preview";
            previewSignal.transform.SetParent(transform, true);
            previewSignals[i] = previewSignal;
        }

        propertiesUI.gameObject.SetActive(false);
        deleteButton.onClick.AddListener(DeleteSelected);
    }

    public event Action<Chip> onDeleteChip;
    public event Action onChipsAddedOrDeleted;

    public override void OrderedUpdate()
    {
        if (!InputHelper.MouseOverUIObject())
        {
            UpdateColours();
            HandleInput();
        }

        DrawSignalHandles();
    }

    public void LoadSignal(ChipSignal signal)
    {
        signal.transform.parent = signalHolder;
        signals.Add(signal);
    }

    private void HandleInput()
    {
        var mousePos = InputHelper.MouseWorldPos;

        mouseInInputBounds = inputBounds.OverlapPoint(mousePos);
        if (mouseInInputBounds) RequestFocus();

        if (HasFocus)
        {
            highlightedSignal = GetSignalUnderMouse();

            // If a signal is highlighted (mouse is over its handle), then select it on mouse press
            if (highlightedSignal)
                if (Input.GetMouseButtonDown(0))
                    SelectSignal(highlightedSignal);

            // If a signal is selected, handle movement/renaming/deletion
            if (selectedSignals.Count > 0)
            {
                if (isDragging)
                {
                    var handleNewY = mousePos.y + (dragHandleStartY - dragMouseStartY);
                    var cancel = Input.GetKeyDown(KeyCode.Escape);
                    if (cancel) handleNewY = dragHandleStartY;

                    for (var i = 0; i < selectedSignals.Count; i++)
                    {
                        var y = CalcY(handleNewY, selectedSignals.Count, i);
                        SetYPos(selectedSignals[i].transform, y);
                    }

                    if (Input.GetMouseButtonUp(0)) isDragging = false;

                    // Cancel drag and deselect
                    if (cancel) FocusLost();
                }

                UpdateUIProperties();

                // Finished with selected signal, so deselect it
                if (Input.GetKeyDown(KeyCode.Return)) FocusLost();
            }

            HidePreviews();
            if (highlightedSignal == null && !isDragging)
                if (mouseInInputBounds)
                {
                    if (InputHelper.AnyOfTheseKeysDown(KeyCode.Plus, KeyCode.KeypadPlus, KeyCode.Equals))
                        currentGroupSize = Mathf.Clamp(currentGroupSize + 1, 1, maxGroupSize);
                    else if (InputHelper.AnyOfTheseKeysDown(KeyCode.Minus, KeyCode.KeypadMinus, KeyCode.Underscore))
                        currentGroupSize = Mathf.Clamp(currentGroupSize - 1, 1, maxGroupSize);

                    HandleSpawning();
                }
        }
    }

    private float CalcY(float mouseY, int groupSize, int index)
    {
        var centreY = mouseY;
        var halfExtent = groupSpacing * (groupSize - 1f);
        var maxY = centreY + halfExtent + handleSize.y / 2f;
        var minY = centreY - halfExtent - handleSize.y / 2f;

        if (maxY > BoundsTop)
            centreY -= maxY - BoundsTop;
        else if (minY < BoundsBottom) centreY += BoundsBottom - minY;

        var t = groupSize > 1 ? index / (groupSize - 1f) : 0.5f;
        t = t * 2 - 1;
        var posY = centreY - t * halfExtent;
        return posY;
    }

    public ChipSignal[][] GetGroups()
    {
        var keys = groupsByID.Keys;
        var groups = new ChipSignal[keys.Count][];
        var i = 0;
        foreach (var key in keys)
        {
            groups[i] = groupsByID[key];
            i++;
        }

        return groups;
    }

    // Handles spawning if user clicks, otherwise displays preview
    private void HandleSpawning()
    {
        var containerX = chipContainer.position.x +
                         chipContainer.localScale.x / 2 * (editorType == EditorType.Input ? -1 : 1);
        var centreY = ClampY(InputHelper.MouseWorldPos.y);

        // Spawn on mouse down
        if (Input.GetMouseButtonDown(0))
        {
            var isGroup = currentGroupSize > 1;
            var spawnedSignals = new ChipSignal[currentGroupSize];

            for (var i = 0; i < currentGroupSize; i++)
            {
                var posY = CalcY(InputHelper.MouseWorldPos.y, currentGroupSize, i);
                var spawnPos = new Vector3(containerX, posY, chipContainer.position.z + forwardDepth);

                var spawnedSignal = Instantiate(signalPrefab, spawnPos, Quaternion.identity, signalHolder);
                if (isGroup)
                {
                    spawnedSignal.GroupID = currentGroupID;
                    spawnedSignal.displayGroupDecimalValue = true;
                }

                signals.Add(spawnedSignal);
                spawnedSignals[i] = spawnedSignal;
            }

            if (isGroup)
            {
                groupsByID.Add(currentGroupID, spawnedSignals);
                // Reset group size after spawning
                currentGroupSize = 1;
                // Generate new ID for next group
                // This will be used to identify which signals were created together as a group
                currentGroupID++;
            }

            SelectSignal(signals[signals.Count - 1]);
            onChipsAddedOrDeleted?.Invoke();
        }
        // Draw handle and signal previews
        else
        {
            for (var i = 0; i < currentGroupSize; i++)
            {
                var posY = CalcY(InputHelper.MouseWorldPos.y, currentGroupSize, i);
                var spawnPos = new Vector3(containerX, posY, chipContainer.position.z + forwardDepth);
                DrawHandle(posY, HandleState.Highlighted);
                if (showPreviewSignal)
                {
                    previewSignals[i].gameObject.SetActive(true);
                    previewSignals[i].transform.position = spawnPos - Vector3.forward * forwardDepth;
                }
            }
        }
    }

    private void HidePreviews()
    {
        for (var i = 0; i < previewSignals.Length; i++) previewSignals[i].gameObject.SetActive(false);
    }

    private float ClampY(float y)
    {
        return Mathf.Clamp(y, BoundsBottom + handleSize.y / 2f, BoundsTop - handleSize.y / 2f);
    }

    protected override bool CanReleaseFocus()
    {
        if (isDragging) return false;
        if (mouseInInputBounds) return false;
        return true;
    }

    protected override void FocusLost()
    {
        highlightedSignal = null;
        selectedSignals.Clear();
        propertiesUI.gameObject.SetActive(false);

        HidePreviews();
        currentGroupSize = 1;
    }

    private void UpdateUIProperties()
    {
        if (selectedSignals.Count > 0)
        {
            var centre = (selectedSignals[0].transform.position +
                          selectedSignals[selectedSignals.Count - 1].transform.position) / 2;
            propertiesUI.transform.position =
                new Vector3(centre.x + propertiesUIX, centre.y, propertiesUI.transform.position.z);

            // Update signal properties
            for (var i = 0; i < selectedSignals.Count; i++)
            {
                selectedSignals[i].UpdateSignalName(nameField.text);
                selectedSignals[i].useTwosComplement = twosComplementToggle.isOn;
            }
        }
    }

    private void DrawSignalHandles()
    {
        for (var i = 0; i < signals.Count; i++)
        {
            var handleState = HandleState.Default;
            if (signals[i] == highlightedSignal) handleState = HandleState.Highlighted;
            if (selectedSignals.Contains(signals[i])) handleState = HandleState.Selected;

            DrawHandle(signals[i].transform.position.y, handleState);
        }
    }

    private ChipSignal GetSignalUnderMouse()
    {
        ChipSignal signalUnderMouse = null;
        var nearestDst = float.MaxValue;

        for (var i = 0; i < signals.Count; i++)
        {
            var currentSignal = signals[i];
            var handleY = currentSignal.transform.position.y;

            var handleCentre = new Vector2(transform.position.x, handleY);
            var mousePos = InputHelper.MouseWorldPos;

            const float selectionBufferY = 0.1f;

            var halfSizeX = transform.localScale.x / 2f;
            var halfSizeY = (handleSize.y + selectionBufferY) / 2f;
            var insideX = mousePos.x >= handleCentre.x - halfSizeX && mousePos.x <= handleCentre.x + halfSizeX;
            var insideY = mousePos.y >= handleCentre.y - halfSizeY && mousePos.y <= handleCentre.y + halfSizeY;

            if (insideX && insideY)
            {
                var dst = Mathf.Abs(mousePos.y - handleY);
                if (dst < nearestDst)
                {
                    nearestDst = dst;
                    signalUnderMouse = currentSignal;
                }
            }
        }

        return signalUnderMouse;
    }

    // Select signal (starts dragging, shows rename field)
    private void SelectSignal(ChipSignal signalToDrag)
    {
        // Dragging
        selectedSignals.Clear();
        for (var i = 0; i < signals.Count; i++)
            if (signals[i] == signalToDrag || ChipSignal.InSameGroup(signals[i], signalToDrag))
                selectedSignals.Add(signals[i]);
        var isGroup = selectedSignals.Count > 1;

        isDragging = true;

        dragMouseStartY = InputHelper.MouseWorldPos.y;
        if (selectedSignals.Count % 2 == 0)
        {
            var indexA = Mathf.Max(0, selectedSignals.Count / 2 - 1);
            var indexB = selectedSignals.Count / 2;
            dragHandleStartY =
                (selectedSignals[indexA].transform.position.y + selectedSignals[indexB].transform.position.y) / 2f;
        }
        else
        {
            dragHandleStartY = selectedSignals[selectedSignals.Count / 2].transform.position.y;
        }

        // Enable UI:
        propertiesUI.gameObject.SetActive(true);
        propertiesUI.sizeDelta = new Vector2(propertiesUI.sizeDelta.x,
            isGroup ? propertiesHeightMinMax.y : propertiesHeightMinMax.x);
        nameField.text = selectedSignals[0].signalName;
        nameField.Select();
        nameField.caretPosition = nameField.text.Length;
        twosComplementToggle.gameObject.SetActive(isGroup);
        twosComplementToggle.isOn = selectedSignals[0].useTwosComplement;
        UpdateUIProperties();
    }

    private void DeleteSelected()
    {
        for (var i = selectedSignals.Count - 1; i >= 0; i--)
        {
            var signalToDelete = selectedSignals[i];
            if (groupsByID.ContainsKey(signalToDelete.GroupID)) groupsByID.Remove(signalToDelete.GroupID);
            onDeleteChip?.Invoke(signalToDelete);
            signals.Remove(signalToDelete);
            Destroy(signalToDelete.gameObject);
        }

        onChipsAddedOrDeleted?.Invoke();
        selectedSignals.Clear();
        FocusLost();
    }

    private void DrawHandle(float y, HandleState handleState = HandleState.Default)
    {
        var renderZ = forwardDepth;
        Material currentHandleMat;
        switch (handleState)
        {
            case HandleState.Highlighted:
                currentHandleMat = highlightedHandleMat;
                break;
            case HandleState.Selected:
                currentHandleMat = selectedHandleMat;
                renderZ = forwardDepth * 2;
                break;
            default:
                currentHandleMat = handleMat;
                break;
        }

        var scale = new Vector3(handleSize.x, handleSize.y, 1);
        var pos3D = new Vector3(transform.position.x, y, transform.position.z + renderZ);
        var handleMatrix = Matrix4x4.TRS(pos3D, Quaternion.identity, scale);
        Graphics.DrawMesh(quadMesh, handleMatrix, currentHandleMat, 0);
    }

    private Material CreateUnlitMaterial(Color col)
    {
        var mat = new Material(Shader.Find("Unlit/Color"));
        mat.color = col;
        return mat;
    }

    private void SetYPos(Transform t, float y)
    {
        t.position = new Vector3(t.position.x, y, t.position.z);
    }

    private void UpdateColours()
    {
        handleMat.color = handleCol;
        highlightedHandleMat.color = highlightedHandleCol;
        selectedHandleMat.color = selectedHandleCol;
    }
}