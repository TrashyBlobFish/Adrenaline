using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class TestRelay : MonoBehaviour
{
    //Audio
    public AudioMixerSnapshot startScreenSnapshot;
    public AudioMixerSnapshot gameplaySnapshot;

    //UI
    public TextMeshProUGUI LobbyText;
    public TextMeshProUGUI JoinCodeUI;
    public GameObject NetworkManagerUI;
    public GameObject DiscconectedUI;
    public GameObject MenuUI;

    private async void Start()
    {
        await UnityServices.InitializeAsync();

        AuthenticationService.Instance.SignedIn += OnSignedIn;
        await AuthenticationService.Instance.SignInAnonymouslyAsync();

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnect;
        }

        SetStartSnapshot();
    }

    void SetStartSnapshot()
    {
        if (startScreenSnapshot != null)
        {
            startScreenSnapshot.TransitionTo(0f);
        }
    }

    private void OnSignedIn()
    {
        Debug.Log("Signed in " + AuthenticationService.Instance.PlayerId);
        GameObject gameManager = GameObject.Find("GameManager");
        if (gameManager != null)
        {
            UserProfileData profileData = gameManager.GetComponent<UserProfileData>();
            if (profileData != null)
            {
                profileData.PlayerID = AuthenticationService.Instance.PlayerId;
            }
        }
    }

    private void OnDestroy()
    {
        if (AuthenticationService.Instance != null)
        {
            AuthenticationService.Instance.SignedIn -= OnSignedIn;
        }

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnect;
        }
    }

    private void HandleClientDisconnect(ulong clientId)
    {
        if (NetworkManager.Singleton != null &&
            clientId == NetworkManager.Singleton.LocalClientId)
        {
            Debug.Log("Disconnected from server.");
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            SetGameUIActive(false);

            _ = DisconnectAndReturnToMenu();
        }
    }

    private async System.Threading.Tasks.Task DisconnectAndReturnToMenu()
    {
        SetGameUIActive(false);

        // Show disconnect screen
        if (DiscconectedUI != null)
        {
            DiscconectedUI.SetActive(true);
        }

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
            await System.Threading.Tasks.Task.Delay(500);
        }

        if (AuthenticationService.Instance != null && AuthenticationService.Instance.IsSignedIn)
        {
            AuthenticationService.Instance.SignOut();
        }

        SceneManager.LoadScene("Menu");
    }

    public async void CreateRelay(string startScene = "Lobby")
    {
        try
        {
            if (LobbyText != null)
            {
                LobbyText.text = "Creating Relay...";
            }

            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(5);
            string joincode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log("Join Code: " + joincode);

            if (JoinCodeUI != null)
            {
                JoinCodeUI.text = "Join Code: " + joincode;
            }

            RelayServerData relayServerData = AllocationUtils.ToRelayServerData(allocation, "dtls");

            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(relayServerData);

            NetworkManager.Singleton.StartHost();
            SetGameUIActive(true);

            if (NetworkManagerUI != null)
            {
                NetworkManagerUI.SetActive(false);
            }

            if (NetworkManager.Singleton.IsServer)
            {
                NetworkManager.Singleton.SceneManager.LoadScene(startScene, LoadSceneMode.Single);
                if (gameplaySnapshot != null)
                {
                    gameplaySnapshot.TransitionTo(0f);
                }
            }
        }
        catch (RelayServiceException e)
        {
            Debug.Log(e);
            if (LobbyText != null)
            {
                LobbyText.text = "Failed to create lobby.";
            }
        }
    }

    public async void JoinRelay(string joincode)
    {
        if (string.IsNullOrEmpty(joincode))
        {
            if (LobbyText != null)
            {
                LobbyText.text = "Enter a code first!";
            }
            return;
        }

        try
        {
            if (LobbyText != null)
            {
                LobbyText.text = "Joining...";
            }

            Debug.Log("Joining Relay with " + joincode);
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joincode);

            RelayServerData relayServerData = AllocationUtils.ToRelayServerData(joinAllocation, "dtls");

            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(relayServerData);

            NetworkManager.Singleton.StartClient();
            SetGameUIActive(true);

            if (NetworkManagerUI != null)
            {
                NetworkManagerUI.SetActive(false);
            }

            if (gameplaySnapshot != null)
            {
                gameplaySnapshot.TransitionTo(1f);
            }
        }
        catch (RelayServiceException e)
        {
            Debug.LogWarning(e.Message);
            if (LobbyText != null)
            {
                LobbyText.text = "Invalid code. Please try again.";
            }
        }
    }

    private void SetGameUIActive(bool isActive)
    {
        GameManager gameManager = Object.FindFirstObjectByType<GameManager>();
        if (gameManager != null && gameManager.GameUI != null)
        {
            gameManager.GameUI.SetActive(isActive);
        }
    }
}