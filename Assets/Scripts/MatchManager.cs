using UnityEngine;
using TMPro;
using Photon.Pun;
using System.Collections.Generic;
using System.Linq;

public class MatchManager : MonoBehaviourPunCallbacks
{
    public static MatchManager Instance { get; private set; }

    [SerializeField] private TMP_Text scoresText;
    [SerializeField] private TMP_Text goldText;

    private Dictionary<int, IPlayerData> _players = new();
    private Dictionary<int, Color> _playerColorsCache = new();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        Invoke(nameof(TryAssignMyStartingNode), 2f);
        //InvokeRepeating(nameof(UpdateGoldUI), 0f, 1f);
    }
    
    public void RegisterPlayer(int playerId, IPlayerData playerData)
    {
        if (_players.TryAdd(playerId, playerData))
        {
            _playerColorsCache[playerId] = playerData.Color;
            Debug.Log($"Player {playerId} registered");
            
            UpdateScoresUI();
            UpdateGoldUI();
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

    public void UpdatePlayerGold(int playerId, int delta)
    {
        if (_players.TryGetValue(playerId, out IPlayerData player))
        {
            player.UpdateGold(delta);
            UpdateGoldUI();
        }
    }

    public void UpdateScoresUI()
    {
        if (scoresText && _players.Count > 0)
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

    public void UpdateGoldUI()
    {
        if (goldText && PhotonNetwork.LocalPlayer != null)
        {
            int localPlayerId = PhotonNetwork.LocalPlayer.ActorNumber;
            if (_players.TryGetValue(localPlayerId, out IPlayerData playerData))
            {
                goldText.text = $": {playerData.GoldCount}";
            }
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

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();

        Debug.Log("=== ONJOINEDROOM СРАБОТАЛ! ===");
        Debug.Log($"Игрок {PhotonNetwork.LocalPlayer.ActorNumber} в комнате. Master: {PhotonNetwork.IsMasterClient}");

        UpdateScoresUI();
        UpdateGoldUI();

        // Даём стартовую ноду с задержкой 2 секунды (увеличил для надёжности)
        Invoke(nameof(TryAssignMyStartingNode), 2f);
    }

    private void TryAssignMyStartingNode()
    {
        Debug.Log("=== TRYASSIGNMYSTARTINGNODE ВЫЗВАН! ===");
        int myId = PhotonNetwork.LocalPlayer.ActorNumber;

        Debug.Log($"Попытка выдать стартовую ноду игроку {myId}. IsMaster: {PhotonNetwork.IsMasterClient}");

        if (PhotonNetwork.IsMasterClient)
        {
            AssignStartingNodeToPlayer(myId);
        }
        else
        {
            // Просим Master выдать нам ноду
            photonView.RPC("RPC_RequestStartingNode", RpcTarget.MasterClient, myId);
        }
    }

    [PunRPC]
    private void RPC_RequestStartingNode(int playerId)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log($"Master получил запрос на стартовую ноду от игрока {playerId}");
            AssignStartingNodeToPlayer(playerId);
        }
    }

    private void AssignStartingNodeToPlayer(int playerId)
    {
        if (GraphManager.Instance == null)
        {
            Debug.LogError("GraphManager не найден!");
            return;
        }

        List<GraphNode> freeNodes = new List<GraphNode>();

        foreach (GraphNode node in GraphManager.Instance.AllNodes)
        {
            if (node != null && node.CurrentNodeOwner == -1)
            {
                freeNodes.Add(node);
            }
        }

        Debug.Log($"Свободных нод для игрока {playerId}: {freeNodes.Count}");

        if (freeNodes.Count == 0)
        {
            Debug.LogWarning($"Нет свободных нод для игрока {playerId}!");
            return;
        }

        int randomIndex = UnityEngine.Random.Range(0, freeNodes.Count);
        GraphNode startingNode = freeNodes[randomIndex];

        PhotonView nodePv = startingNode.GetComponent<PhotonView>();
        if (nodePv != null)
        {
            nodePv.RPC("RPC_AssignAsStartingNode", RpcTarget.All, playerId);
            Debug.Log($"УСПЕШНО выдана стартовая нода игроку {playerId}");
        }
        else
        {
            Debug.LogError($"У ноды нет PhotonView!");
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
            UpdateGoldUI();
            Debug.Log($"Player {playerId} removed from MatchManager");
        }
        
        GraphNode[] allNodes = FindObjectsByType<GraphNode>(FindObjectsSortMode.None);
        foreach (GraphNode node in allNodes)
        {
            node.UpdateNodeColor();
        }
    }
}
