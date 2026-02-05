using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class PauseMenuUI : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Button returnToLobbyButton;
    private NetworkChangeScenes sceneChanger;
    private bool isMenuOpen = false;

    void Start()
    {
        pausePanel.SetActive(false);
        sceneChanger = gameObject.GetComponent<NetworkChangeScenes>();
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
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
            {
                returnToLobbyButton.gameObject.SetActive(true);
            }
        }
    }
    public void OnReturnToLobbyClicked()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost && sceneChanger != null)
        {
            sceneChanger.ChangeScene("Lobby");
        }
    }


    public void QuitGame()
    {
        GameObject.Find("GameManager").GetComponent<SaveJSONData>().SaveAndExit();
    }
}
