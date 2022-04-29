using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Manager : MonoBehaviour
{
    private static Manager instance;

    public ChipEditor chipEditorPrefab;
    public ChipPackage chipPackagePrefab;
    public Wire wirePrefab;
    public Chip[] builtinChips;

    private ChipEditor activeChipEditor;
    private int currentChipCreationIndex;

    public static ChipEditor ActiveChipEditor => instance.activeChipEditor;

    private void Awake()
    {
        instance = this;
        activeChipEditor = FindObjectOfType<ChipEditor>();
        FindObjectOfType<CreateMenu>().onChipCreatePressed += SaveAndPackageChip;
    }

    private void Start()
    {
        SaveSystem.Init();
        SaveSystem.LoadAll(this);
    }

    public event Action<Chip> customChipCreated;

    public Chip LoadChip(ChipSaveData loadedChipData)
    {
        activeChipEditor.LoadFromSaveData(loadedChipData);
        currentChipCreationIndex = activeChipEditor.creationIndex;

        var loadedChip = PackageChip();
        LoadNewEditor();
        return loadedChip;
    }

    private void SaveAndPackageChip()
    {
        ChipSaver.Save(activeChipEditor);
        PackageChip();
        LoadNewEditor();
    }

    private Chip PackageChip()
    {
        var package = Instantiate(chipPackagePrefab, transform);
        package.PackageCustomChip(activeChipEditor);
        package.gameObject.SetActive(false);

        var customChip = package.GetComponent<Chip>();
        customChipCreated?.Invoke(customChip);
        currentChipCreationIndex++;
        return customChip;
    }

    private void LoadNewEditor()
    {
        if (activeChipEditor) Destroy(activeChipEditor.gameObject);
        activeChipEditor = Instantiate(chipEditorPrefab, Vector3.zero, Quaternion.identity);
        activeChipEditor.creationIndex = currentChipCreationIndex;
    }

    public void SpawnChip(Chip chip)
    {
        activeChipEditor.chipInteraction.SpawnChip(chip);
    }

    public void LoadMainMenu()
    {
        SceneManager.LoadScene(0);
    }
}