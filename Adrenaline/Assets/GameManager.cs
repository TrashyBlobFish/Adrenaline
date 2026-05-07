using System.Collections.Generic;
using System.Text;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Audio;

public class GameManager : NetworkBehaviour
{
    public float matchDuration = 180f; // 3 minutes
    private float matchTimer;
    private bool matchActive = false;
    public GameObject LobbyCode;

    private NetworkVariable<float> networkMatchTimer = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public GameObject GameUI; // Assign in Inspector
    public TMP_Text matchTimerText; // Assign in Inspector
    public TMP_Text winnerText; // Optional fallback
    public TMP_Text leaderboardText; // Assign in Inspector if you want a dedicated leaderboard text

    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private AudioMixerSnapshot menuSnapshot;
    [SerializeField] private AudioMixerSnapshot gameplaySnapshot;
    [SerializeField] private float snapshotTransitionTime = 1f;

    private void Start()
    {
        if (matchTimerText != null)
            matchTimerText.text = FormatTime(matchDuration);

        ClearResultText();
        TransitionToMenuSnapshot();
    }

    private void Update()
    {
        if (IsServer && matchActive)
        {
            matchTimer -= Time.deltaTime;
            if (matchTimer < 0f)
                matchTimer = 0f;

            networkMatchTimer.Value = matchTimer;

            if (matchTimer <= 0f)
            {
                matchActive = false;
                EndMatch();
            }
        }

        // Update timer UI for all
        if (matchTimerText != null)
            matchTimerText.text = FormatTime(networkMatchTimer.Value);
    }

    public void StartMatch()
    {
        if (!IsServer)
            return;

        matchTimer = matchDuration;
        matchActive = true;
        networkMatchTimer.Value = matchDuration;

        ClearResultText();
        TransitionToGameplaySnapshot();
        CloseLobbyServerRpc();
    }

    private void EndMatch()
    {
        Debug.Log("Match ended! Building leaderboard...");

        var players = Object.FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
        Debug.Log($"Total players to evaluate: {players.Length}");

        var leaderboardEntries = new List<LeaderboardEntry>(players.Length);

        foreach (var player in players)
        {
            string playerName = GetPlayerName(player);
            float batHoldTime = player.BatHoldTime;

            leaderboardEntries.Add(new LeaderboardEntry(playerName, batHoldTime));
            Debug.Log($"Player {player.NetworkObjectId} ({playerName}) bat hold time: {batHoldTime:F2}s");
        }

        leaderboardEntries.Sort((left, right) =>
        {
            int timeComparison = left.BatHoldTime.CompareTo(right.BatHoldTime);
            if (timeComparison != 0)
                return timeComparison;

            return string.Compare(left.PlayerName, right.PlayerName);
        });

        string leaderboardTextValue = BuildLeaderboardText(leaderboardEntries);

        if (leaderboardEntries.Count > 0)
        {
            Debug.Log($"Winner: {leaderboardEntries[0].PlayerName} with {leaderboardEntries[0].BatHoldTime:F2}s");
        }
        else
        {
            Debug.Log("No players could be determined.");
        }

        ShowLeaderboardClientRpc(leaderboardTextValue);
        TransitionToMenuSnapshot();
    }

    private string BuildLeaderboardText(List<LeaderboardEntry> entries)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Leaderboard");

        if (entries.Count == 0)
        {
            builder.Append("No players found.");
            return builder.ToString();
        }

        builder.AppendLine();

        for (int i = 0; i < entries.Count; i++)
        {
            LeaderboardEntry entry = entries[i];
            builder.AppendLine($"{i + 1}. {entry.PlayerName} - {FormatSeconds(entry.BatHoldTime)}");
        }

        return builder.ToString().TrimEnd();
    }

    private string GetPlayerName(PlayerMovement player)
    {
        if (player == null)
            return "Unknown";

        UserProfileData profile = player.GetComponent<UserProfileData>();

        if (profile == null)
            profile = player.GetComponentInChildren<UserProfileData>();

        if (profile == null)
            profile = player.GetComponentInParent<UserProfileData>();

        if (profile != null && !string.IsNullOrWhiteSpace(profile.PlayerID))
            return profile.PlayerID;

        return player.gameObject.name;
    }

    private string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);
        return $"{minutes:00}:{seconds:00}";
    }

    private string FormatSeconds(float time)
    {
        return $"{time:0.00}s";
    }

    private TMP_Text GetResultText()
    {
        return leaderboardText != null ? leaderboardText : winnerText;
    }

    private void ClearResultText()
    {
        TMP_Text resultText = GetResultText();
        if (resultText != null)
            resultText.text = "";
    }

    private void TransitionToMenuSnapshot()
    {
        if (menuSnapshot != null)
        {
            menuSnapshot.TransitionTo(snapshotTransitionTime);
        }
    }

    private void TransitionToGameplaySnapshot()
    {
        if (gameplaySnapshot != null)
        {
            gameplaySnapshot.TransitionTo(snapshotTransitionTime);
        }
    }

    [ServerRpc]
    private void CloseLobbyServerRpc()
    {
        Debug.Log("[GameManager] Closing relay lobby - no more clients can join.");

        // Call NetworkManager to disconnect the relay server from accepting new connections
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            // This effectively closes the lobby by preventing new joins
            NetworkManager.Singleton.OnServerStarted += OnServerStarted;
        }

        CloseLobbyClientRpc();
    }

    private void OnServerStarted()
    {
        // The lobby is now closed - no new players can join
        Debug.Log("[GameManager] Relay lobby has been closed successfully.");
        NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
    }

    [ClientRpc]
    private void CloseLobbyClientRpc()
    {
        // Update UI on all clients to reflect that lobby is closed
        Debug.Log("[GameManager] Lobby closed - match has started.");

        // Optional: Hide lobby UI elements
        if (GameUI != null)
        {
            // You can add specific UI handling here if needed
            LobbyCode.SetActive(false);
        }
    }

    [ClientRpc]
    private void ShowLeaderboardClientRpc(string leaderboard)
    {
        TMP_Text resultText = GetResultText();
        if (resultText == null)
            return;

        resultText.text = leaderboard;
    }

    private class LeaderboardEntry
    {
        public string PlayerName;
        public float BatHoldTime;

        public LeaderboardEntry(string playerName, float batHoldTime)
        {
            PlayerName = playerName;
            BatHoldTime = batHoldTime;
        }
    }
}