using UnityEngine;
using TMPro;
using Photon.Pun;
using System.Collections.Generic;

public class MatchManager : MonoBehaviourPunCallbacks
{
    public static MatchManager Instance { get; private set; }

    [SerializeField] private TMP_Text scoresText;

    private Dictionary<int, Color> playerColors = new Dictionary<int, Color>();
    private Dictionary<int, int> playerScores = new Dictionary<int, int>();

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

    public void UpdatePlayerScore(int playerId, int delta)
    {
        playerScores.TryAdd(playerId, 0);
        playerScores[playerId] += delta;
    }

    public void UpdateScoresUI()
    {
        if (scoresText != null)
        {
            string scores = "Scores:\n";
            foreach (var kvp in playerScores)
            {
                scores += $"P{kvp.Key}: {kvp.Value}\n";
            }
            scoresText.text = scores;
        }
    }

    public Color GetPlayerColor(int playerId)
    {
        if (playerColors.TryGetValue(playerId, out Color color))
        {
            return color;
        }
        return Color.white;
    }

    [PunRPC]
    public void RPC_SetPlayerColor(int playerId, float r, float g, float b, float a)
    {
        Color color = new Color(r, g, b, a);
        playerColors[playerId] = color;
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
