using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode;
using TMPro;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using UnityEngine.SceneManagement;

public class TestRelay : MonoBehaviour
{
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
    }

    private void OnSignedIn()
    {
        Debug.Log("Signed in " + AuthenticationService.Instance.PlayerId);
        GameObject.Find("GameManager").GetComponent<UserProfileData>().PlayerID = AuthenticationService.Instance.PlayerId;
    }

    private void OnDestroy()
    {
        AuthenticationService.Instance.SignedIn -= OnSignedIn;

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

            _ = DisconnectAndReturnToMenu();
        }
    }

    private async System.Threading.Tasks.Task DisconnectAndReturnToMenu()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
            await System.Threading.Tasks.Task.Delay(500);
        }

        if (AuthenticationService.Instance.IsSignedIn)
        {
            AuthenticationService.Instance.SignOut();
        }

        // Use regular SceneManager to go back to non-networked menu
        SceneManager.LoadScene("Menu");

        await System.Threading.Tasks.Task.Delay(500);
        ShowDisconnectedUI();
    }

    private void ShowDisconnectedUI()
    {
        GameObject menuUI = GameObject.Find("MenuUI");
        GameObject disconnectUI = GameObject.Find("DiscconectedUI");

        if (menuUI != null)
        {
            menuUI.SetActive(false);
        }

        if (disconnectUI != null)
        {
            disconnectUI.SetActive(true);
        }
    }

    public async void CreateRelay()
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

            // Start as host (server)
            NetworkManager.Singleton.StartHost();

            if (NetworkManagerUI != null)
            {
                NetworkManagerUI.SetActive(false);
            }

            // IMPORTANT: use NetworkSceneManager from the host
            if (NetworkManager.Singleton.IsServer)
            {
                NetworkManager.Singleton.SceneManager.LoadScene("Lobby", LoadSceneMode.Single);
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

            // Join as client; let host handle scene changes
            NetworkManager.Singleton.StartClient();

            if (NetworkManagerUI != null)
            {
                NetworkManagerUI.SetActive(false);
            }

            // DO NOT call SceneManager.LoadScene here for clients.
            // The server/host will move everyone with NetworkSceneManager.
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
}