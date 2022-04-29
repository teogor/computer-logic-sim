using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    public TMP_InputField projectNameField;
    public Button confirmProjectButton;
    public Toggle fullscreenToggle;

    private void Awake()
    {
        fullscreenToggle.onValueChanged.AddListener(SetFullScreen);
    }

    private void LateUpdate()
    {
        confirmProjectButton.interactable = projectNameField.text.Trim().Length > 0;
        if (fullscreenToggle.isOn != Screen.fullScreen) fullscreenToggle.SetIsOnWithoutNotify(Screen.fullScreen);
    }

    public void StartNewProject()
    {
        var projectName = projectNameField.text;
        SaveSystem.SetActiveProject(projectName);
        SceneManager.LoadScene(1);
    }

    public void SetResolution16x9(int width)
    {
        Screen.SetResolution(width, Mathf.RoundToInt(width * (9 / 16f)), Screen.fullScreenMode);
    }

    public void SetFullScreen(bool fullscreenOn)
    {
        //Screen.fullScreen = fullscreenOn;
        var nativeRes = Screen.resolutions[Screen.resolutions.Length - 1];
        if (fullscreenOn)
        {
            Screen.SetResolution(nativeRes.width, nativeRes.height, FullScreenMode.FullScreenWindow);
        }
        else
        {
            var windowedScale = 0.75f;
            var x = nativeRes.width / 16;
            var y = nativeRes.height / 9;
            var m = (int) (Mathf.Min(x, y) * windowedScale);
            Screen.SetResolution(16 * m, 9 * m, FullScreenMode.Windowed);
        }
    }

    public void Quit()
    {
        Application.Quit();
    }
}