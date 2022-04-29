using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadProjectMenu : MonoBehaviour
{
    public Button projectButtonPrefab;
    public Transform scrollHolder;

    [SerializeField] [HideInInspector] private List<Button> loadButtons;

    private void OnEnable()
    {
        var projectNames = SaveSystem.GetSaveNames();

        for (var i = 0; i < projectNames.Length; i++)
        {
            var projectName = projectNames[i];
            if (i >= loadButtons.Count) loadButtons.Add(Instantiate(projectButtonPrefab, scrollHolder));
            var loadButton = loadButtons[i];
            loadButton.GetComponentInChildren<TMP_Text>().text = projectName.Trim();
            loadButton.onClick.AddListener(() => LoadProject(projectName));
        }
    }

    public void LoadProject(string projectName)
    {
        SaveSystem.SetActiveProject(projectName);
        SceneManager.LoadScene(1);
    }
}