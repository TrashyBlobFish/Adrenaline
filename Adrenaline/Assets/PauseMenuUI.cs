using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using UnityEngine.Audio;
using System.Collections;

public class PauseMenuUI : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Button returnToLobbyButton;
    private NetworkChangeScenes sceneChanger;
    public bool isMenuOpen = false;

    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private string lowPassParameterName = "AmbienceLowPass";
    [SerializeField] private AudioMixerSnapshot pauseSnapshot;
    [SerializeField] private AudioMixerSnapshot gameplaySnapshot;
    [SerializeField] private float snapshotTransitionTime = 0.5f;

    private float pausedCutoff = 500f;
    private float activeCutoff = 22000f;

    void Start()
    {
        pausePanel.SetActive(false);
        sceneChanger = gameObject.GetComponent<NetworkChangeScenes>();
        
        // Ensure the return to lobby button is only visible to the host
        if (returnToLobbyButton != null)
        {
            returnToLobbyButton.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        //tempory need to change to detect if in main menu
        if (Input.GetKeyDown(KeyCode.Escape) && (SceneManager.GetActiveScene().name != "Menu"))
        {
            isMenuOpen = !isMenuOpen;
            pausePanel.SetActive(isMenuOpen);

            // Cursor management
            if (isMenuOpen)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                TransitionToPauseSnapshot();
                SetLowPassFilter(pausedCutoff);
                UpdateReturnToLobbyButtonVisibility();
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                TransitionToGameplaySnapshot();
                SetLowPassFilter(activeCutoff);
                UpdateReturnToLobbyButtonVisibility();
            }
        }
    }

    private void UpdateReturnToLobbyButtonVisibility()
    {
        if (returnToLobbyButton == null)
            return;

        bool isHost = NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;
        returnToLobbyButton.gameObject.SetActive(isHost && isMenuOpen);

        if (isHost && isMenuOpen)
        {
            Debug.Log("[PauseMenuUI] Host detected - showing return to lobby button.");
        }
        else if (!isHost)
        {
            Debug.Log("[PauseMenuUI] Non-host player - hiding return to lobby button.");
        }
    }

    private void TransitionToPauseSnapshot()
    {
        if (pauseSnapshot != null)
        {
            pauseSnapshot.TransitionTo(snapshotTransitionTime);
        }
    }

    private void TransitionToGameplaySnapshot()
    {
        if (gameplaySnapshot != null)
        {
            gameplaySnapshot.TransitionTo(snapshotTransitionTime);
        }
    }

    private void SetLowPassFilter(float frequency)
    {
        if (audioMixer != null)
        {
            audioMixer.SetFloat(lowPassParameterName, frequency);
            Debug.Log($"[PauseMenuUI] Lowpass filter set to {frequency}Hz");
        }
        else
        {
            Debug.LogWarning("[PauseMenuUI] AudioMixer not assigned!");
        }
    }

    public void OnReturnToLobbyClicked()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost && sceneChanger != null)
        {
            Debug.Log("[PauseMenuUI] Host returning to lobby...");
            sceneChanger.ChangeScene("Lobby");
        }
        else
        {
            Debug.LogWarning("[PauseMenuUI] Only the host can return to lobby!");
        }
    }

    public void QuitGame()
    {
        GameObject gameManagerGO = GameObject.Find("GameManager");
        if (gameManagerGO == null)
        {
            Debug.LogError("[PauseMenuUI] GameManager not found! Quitting directly.");
            Application.Quit();
            return;
        }

        SaveJSONData saveData = gameManagerGO.GetComponent<SaveJSONData>();
        if (saveData == null)
        {
            Debug.LogError("[PauseMenuUI] SaveJSONData component not found on GameManager! Quitting directly.");
            Application.Quit();
            return;
        }

        Debug.Log("[PauseMenuUI] Calling SaveAndExit...");
        saveData.SaveAndExit();
    }
}
