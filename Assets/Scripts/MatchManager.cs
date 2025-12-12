using UnityEngine;
using TMPro;
using Photon.Pun;
using System.Collections.Generic;

public class MatchManager : MonoBehaviourPunCallbacks
{
    public static MatchManager Instance { get; private set; }

    [SerializeField] private TMP_Text scoresText;

    private Dictionary<int, IPlayerData> _players = new();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public void RegisterPlayer(int playerId, IPlayerData playerData)
    {
        if (_players.TryAdd(playerId, playerData))
        {
            Debug.Log($"Player {playerId} registered");
        }
    }
    
    public IPlayerData GetPlayerData(int playerId)
    {
        if (_players.TryGetValue(playerId, out IPlayerData playerData))
        {
            return playerData;
        }
        return null;
    }

    public void UpdatePlayerScore(int playerId, int delta)
    {
        if (_players.TryGetValue(playerId, out IPlayerData player))
        {
            player.UpdateScore(delta);
        }
    }

    public void UpdateScoresUI()
    {
        if (scoresText != null)
        {
            string scores = "Scores:\n";
            foreach (var kvp in _players)
            {
                scores += $"P{kvp.Key}: {kvp.Value.Score}\n";
            }
            scoresText.text = scores;
        }
    }

    public Color GetPlayerColor(int playerId)
    {
        if (_players.TryGetValue(playerId, out IPlayerData player))
        {
            return player.Color;
        }
        return Color.white;
    }

    public void ChangeNodeOwner(int oldOwner, int newOwner, int value)
    {
        if (oldOwner != -1)
        {
            UpdatePlayerScore(oldOwner, -value);
        }
        if (newOwner != -1)
        {
            UpdatePlayerScore(newOwner, value);
        }
    }
}
