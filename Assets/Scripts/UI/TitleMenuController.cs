using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// TitleScene의 버튼 흐름을 담당
/// </summary>
public class TitleMenuController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button exitButton;

    [Header("Scene")]
    [SerializeField] private string mainSceneName = "MainScene";

    [Header("Save")]
    [SerializeField] private string saveFileName = "save-slot.json";

    private void Awake()
    {
        if (!ValidateReferences())
        {
            enabled = false;
            return;
        }
    }

    private void OnEnable()
    {
        newGameButton.onClick.AddListener(HandleNewGameClicked);
        continueButton.onClick.AddListener(HandleContinueClicked);
        exitButton.onClick.AddListener(HandleExitClicked);

        RefreshContinueButtonState();
    }

    private void OnDisable()
    {
        if (newGameButton != null)
        {
            newGameButton.onClick.RemoveListener(HandleNewGameClicked);
        }

        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(HandleContinueClicked);
        }

        if (exitButton != null)
        {
            exitButton.onClick.RemoveListener(HandleExitClicked);
        }
    }

    private void HandleNewGameClicked()
    {
        GameStartContext.StartNewGame();
        SceneManager.LoadScene(mainSceneName);
    }

    private void HandleContinueClicked()
    {
        if (!HasSaveFile())
        {
            RefreshContinueButtonState();
            return;
        }

        GameStartContext.StartContinue();
        SceneManager.LoadScene(mainSceneName);
    }

    private void HandleExitClicked()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void RefreshContinueButtonState()
    {
        continueButton.interactable = HasSaveFile();
    }

    private bool HasSaveFile()
    {
        LocalJsonSaveService saveService = new LocalJsonSaveService(saveFileName);
        return File.Exists(saveService.SaveFilePath);
    }

    private bool ValidateReferences()
    {
        bool isValid = true;

        if (newGameButton == null)
        {
            Debug.LogWarning($"{nameof(TitleMenuController)}: newGameButton reference is missing.", this);
            isValid = false;
        }

        if (continueButton == null)
        {
            Debug.LogWarning($"{nameof(TitleMenuController)}: continueButton reference is missing.", this);
            isValid = false;
        }

        if (exitButton == null)
        {
            Debug.LogWarning($"{nameof(TitleMenuController)}: exitButton reference is missing.", this);
            isValid = false;
        }

        if (string.IsNullOrWhiteSpace(mainSceneName))
        {
            Debug.LogWarning($"{nameof(TitleMenuController)}: mainSceneName is empty.", this);
            isValid = false;
        }

        return isValid;
    }
}
