using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI; 
using TMPro; 

public class GameManager : NetworkBehaviour
{
    public float matchDuration = 180f; // 3 minutes
    private float matchTimer;
    private bool matchActive = false;
    private NetworkVariable<float> networkMatchTimer = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public TMP_Text matchTimerText; // Assign in Inspector
    public TMP_Text winnerText;     // Assign in Inspector

    void Start()
    {
        if (matchTimerText != null)
            matchTimerText.text = FormatTime(matchDuration);

        if (winnerText != null)
            winnerText.text = "";

    }

    void Update()
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
        if (winnerText != null)
            winnerText.text = "";
    }

    private void EndMatch()
    {
        Debug.Log("Match ended! Calculating results...");

        var players = Object.FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
        Debug.Log($"Total players to evaluate: {players.Length}");

        // Turn off all baseball bats for all players
        foreach (var player in players)
        {
            player.HasBaseballBat = false;
        }

        PlayerMovement winner = null;
        float minBatTime = float.MaxValue;

        foreach (var player in players)
        {
            float playerBatTime = player.BatHoldTime;
            Debug.Log($"Player {player.NetworkObjectId} bat hold time: {playerBatTime}");

            if (playerBatTime < minBatTime)
            {
                minBatTime = playerBatTime;
                winner = player;
            }
        }

        if (winner != null)
        {
            Debug.Log($"Winner: Player {winner.OwnerClientId} bat time: {minBatTime:F2}s");
            ShowMatchResultClientRpc(winner.OwnerClientId);
        }
        else
        {
            Debug.Log("No winner could be determined.");
            ShowMatchResultClientRpc(ulong.MaxValue); // No winner
        }

    }

    private string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);
        return $"{minutes:00}:{seconds:00}";
    }

    [ClientRpc]
    private void ShowMatchResultClientRpc(ulong winnerClientId)
    {
        if (winnerText == null)
            return;

        if (NetworkManager.Singleton.LocalClientId == winnerClientId)
            winnerText.text = "You Win!";
        else if (winnerClientId == ulong.MaxValue)
            winnerText.text = "No winner!";
        else
            winnerText.text = "You Lose!";
    }
}