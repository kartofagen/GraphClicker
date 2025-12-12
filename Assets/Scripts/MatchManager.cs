using UnityEngine;
using TMPro;
using Photon.Pun;
using System.Collections.Generic;
using System.Linq;

public class MatchManager : MonoBehaviourPunCallbacks
{
    public static MatchManager Instance { get; private set; }

    [SerializeField] private TMP_Text scoresText;

    private Dictionary<int, IPlayerData> _players = new();
    private Dictionary<int, Color> _playerColorsCache = new();

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
            _playerColorsCache[playerId] = playerData.Color;
            Debug.Log($"Player {playerId} registered");
            
            UpdateScoresUI();
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
            UpdateScoresUI();
        }
    }

    public void UpdateScoresUI()
    {
        if (scoresText != null && _players.Count > 0)
        {
            var sortedPlayers = _players.OrderBy(kvp => kvp.Key);
            
            string scores = "Scores:\n";
            foreach (var kvp in sortedPlayers)
            {
                scores += $"P{kvp.Key}: {kvp.Value.Score}\n";
            }
            scoresText.text = scores;
        }
    }

    public Color GetPlayerColor(int playerId)
    {
        if (_playerColorsCache.TryGetValue(playerId, out Color cachedColor))
        {
            return cachedColor;
        }
        
        if (_players.TryGetValue(playerId, out IPlayerData player))
        {
            _playerColorsCache[playerId] = player.Color;
            return player.Color;
        }
        
        Debug.LogWarning($"Color not found for player {playerId}, returning white");
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

    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        Debug.Log($"Player {newPlayer.ActorNumber} entered the room");
        
        if (PhotonNetwork.IsMasterClient)
        {
            foreach (var kvp in _players)
            {
                var playerController = FindObjectsOfType<PlayerController>()
                    .FirstOrDefault(pc => pc.photonView.OwnerActorNr == kvp.Key);
                
                if (playerController != null)
                {
                    playerController.RequestScoreSync();
                }
            }
        }
    }

    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        int playerId = otherPlayer.ActorNumber;
        if (_players.ContainsKey(playerId))
        {
            _players.Remove(playerId);
            _playerColorsCache.Remove(playerId);
            UpdateScoresUI();
            Debug.Log($"Player {playerId} removed from MatchManager");
        }
    }
}
