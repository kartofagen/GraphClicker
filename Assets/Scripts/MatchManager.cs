using System;
using UnityEngine;
using TMPro;
using Photon.Pun;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

public class MatchManager : MonoBehaviourPunCallbacks
{
    public static MatchManager Instance { get; private set; }

    [SerializeField] private TMP_Text scoresText;
    [SerializeField] private TMP_Text goldText;

    [Header("Bonuses")]
    [SerializeField] private UnityEngine.UI.Button[] bonusButtons;
    [SerializeField] private int[] prices = {50, 100, 75};
    [SerializeField] private TMP_Text[] priceTexts;

[SerializeField] private Color avaliableColor;
    [SerializeField] private Color unavailableColor;
    
    private Dictionary<int, IPlayerData> _players = new();
    private Dictionary<int, Color> _playerColorsCache = new();

    private TMP_Text clickPowerText;

    [Header("End Game UI - Auto Find")]
    [SerializeField] private GameObject endGamePanel;
    [SerializeField] private TMP_Text endGameMessageText;

    private bool iWon;
    
    private const int MaxValue = Int32.MaxValue;

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
        SetupBonusButtons();
    }
    
    private void SetupBonusButtons()
    {
        bonusButtons[0].onClick.AddListener(() => BuyClickPowerBonus());
        bonusButtons[1].onClick.AddListener(() => BuyGoldMultiplierBonus());
        bonusButtons[2].onClick.AddListener(() => BuyRandomNodeBonus());
        
        clickPowerText = bonusButtons[0].gameObject.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>();
        
        UpdateBonusButtonsAvailability();
    }

    private void UpdateBonusButtonsAvailability()
    {
        if (PhotonNetwork.LocalPlayer == null) return;
        
        int localPlayerId = PhotonNetwork.LocalPlayer.ActorNumber;
        if (!_players.TryGetValue(localPlayerId, out IPlayerData playerData)) return;
        
        for (int i = 0; i < bonusButtons.Length; ++i)
        {
            int price = prices[i];
            bool isAvailable = playerData.GoldCount >= price;
            
            bonusButtons[i].interactable = isAvailable;
            
            TMP_Text buttonText = bonusButtons[i].GetComponentInChildren<TMP_Text>();
            if (buttonText != null)
            {
                buttonText.color = isAvailable ? avaliableColor : unavailableColor;
            }
        }
    }
    
    public void RegisterPlayer(int playerId, IPlayerData playerData)
    {
        if (_players.TryAdd(playerId, playerData))
        {
            _playerColorsCache[playerId] = playerData.Color;
            Debug.Log($"Player {playerId} registered");
            
            UpdateScoresUI();
            UpdateGoldUI();
            UpdateBonusButtonsAvailability();
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
        if (_players.TryGetValue(playerId, out IPlayerData player) && !iWon)
        {
            player.UpdateScore(delta);
            UpdateScoresUI();
        }
    }

    public void UpdatePlayerGold(int playerId, int delta)
    {
        if (_players.TryGetValue(playerId, out IPlayerData player) && !iWon)
        {
            player.UpdateGold(delta);
            UpdateGoldUI();
            UpdateBonusButtonsAvailability();
        }
    }

    public void UpdateScoresUI()
    {
        if (scoresText && _players.Count > 0)
        {
            int localPlayerId = PhotonNetwork.LocalPlayer.ActorNumber;
        
            IPlayerData localPlayerData = null;
            if (_players.TryGetValue(localPlayerId, out localPlayerData))
            {
                var otherPlayers = _players
                    .Where(kvp => kvp.Key != localPlayerId)
                    .OrderByDescending(kvp => kvp.Value.Score)
                    .ToList();
            
                string scores = "Заражение:\n";
            
                scores += $"Червь {localPlayerId} (Вы): {localPlayerData.Score}\n";
                foreach (var kvp in otherPlayers)
                {
                    scores += $"Червь {kvp.Key}: {kvp.Value.Score}\n";
                }
            
                scoresText.text = scores;
            }
            else
            {
                var sortedPlayers = _players.OrderBy(kvp => kvp.Key);
                string scores = "Заражение:\n";
                foreach (var kvp in sortedPlayers)
                {
                    scores += $"Червь {kvp.Key}: {kvp.Value.Score}\n";
                }
                scoresText.text = scores;
            }
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

    private void IncreasePrice(int index)
    {
        if (prices[index] <= MaxValue / 2)
        {
            prices[index] *= 2;
        }
        else
        {
            prices[index] = MaxValue;
        }
        priceTexts[index].text = prices[index].ToString();
    }
    
    public void BuyClickPowerBonus()
    {
        if (PhotonNetwork.LocalPlayer == null) return;
        
        int playerId = PhotonNetwork.LocalPlayer.ActorNumber;
        if (!_players.TryGetValue(playerId, out IPlayerData player)) return;
        
        if (player.GoldCount >= prices[0])
        {
            UpdatePlayerGold(playerId, -prices[0]);
            
            player.ApplyBonus("click_power", 2f);
            clickPowerText.text = $"УВЕЛИЧИТЬ СИЛУ КЛИКА: {player.ClickPower}";

            IncreasePrice(0);
            
            UpdateScoresUI();
            UpdateGoldUI();
        }
    }
    
    public void BuyGoldMultiplierBonus()
    {
        if (PhotonNetwork.LocalPlayer == null) return;
        
        int playerId = PhotonNetwork.LocalPlayer.ActorNumber;
        if (!_players.TryGetValue(playerId, out IPlayerData player)) return;
        
        if (player.GoldCount >= prices[1])
        {
            UpdatePlayerGold(playerId, -prices[1]);
            
            foreach (GraphNode node in GraphManager.Instance.AllNodes)
            {
                if (node.CurrentNodeOwner == playerId)
                {
                    node.ApplyGoldMultiplier(2f);
                }
            }

            IncreasePrice(1);
        }
    }
    
    public void BuyRandomNodeBonus()
    {
        if (PhotonNetwork.LocalPlayer == null) return;
        
        int playerId = PhotonNetwork.LocalPlayer.ActorNumber;
        if (!_players.TryGetValue(playerId, out IPlayerData player)) return;
        
        if (player.GoldCount >= prices[2])
        {
            UpdatePlayerGold(playerId, -prices[2]);
            
            AcquireRandomNodeForPlayer(playerId);
        }

        IncreasePrice(2);
    }
    
    private void AcquireRandomNodeForPlayer(int playerId)
    {
        if (GraphManager.Instance == null) return;
        
        List<GraphNode> availableNodes = new List<GraphNode>();
        
        foreach (GraphNode node in GraphManager.Instance.AllNodes)
        {
            if (node.CurrentNodeOwner != playerId)
            {
                availableNodes.Add(node);
            }
        }
        
        if (availableNodes.Count == 0)
        {
            Debug.Log("Нет доступных узлов для захвата");
            return;
        }
        
        int randomIndex = UnityEngine.Random.Range(0, availableNodes.Count);
        GraphNode targetNode = availableNodes[randomIndex];
        
        PhotonView nodePhotonView = targetNode.GetComponent<PhotonView>();
        if (nodePhotonView != null)
        {
            nodePhotonView.RPC("RPC_AcquireNode", RpcTarget.All, playerId);
        }
    }

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();

        Debug.Log("=== ONJOINEDROOM СРАБОТАЛ! ===");
        Debug.Log($"Игрок {PhotonNetwork.LocalPlayer.ActorNumber} в комнате. Master: {PhotonNetwork.IsMasterClient}");

        UpdateScoresUI();
        UpdateGoldUI();
        UpdateBonusButtonsAvailability();

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
            UpdateBonusButtonsAvailability();
            Debug.Log($"Player {playerId} removed from MatchManager");
        }
        
        GraphNode[] allNodes = FindObjectsByType<GraphNode>(FindObjectsSortMode.None);
        foreach (GraphNode node in allNodes)
        {
            node.UpdateNodeColor();
        }
    }

    private bool gameEnded = false;

    public void CheckForGameEnd()
    {
        if (gameEnded || GraphManager.Instance == null) return;

        GraphNode[] allNodes = GraphManager.Instance.AllNodes;
        if (allNodes == null || allNodes.Length == 0) return;

        int localPlayerId = PhotonNetwork.LocalPlayer.ActorNumber;

        int myNodesCount = 0;
        Dictionary<int, int> playerNodeCounts = new Dictionary<int, int>();

        // Считаем ноды у каждого игрока (игнорируем нейтральные с owner == -1)
        foreach (GraphNode node in allNodes)
        {
            int owner = node.CurrentNodeOwner;
            if (owner == -1) continue;

            if (owner == localPlayerId)
                myNodesCount++;

            if (playerNodeCounts.ContainsKey(owner))
                playerNodeCounts[owner]++;
            else
                playerNodeCounts[owner] = 1;
        }

        // Проверка на глобальную победу: кто-то захватил ВСЕ ноды
        foreach (var kvp in playerNodeCounts)
        {
            if (kvp.Value == allNodes.Length)
            {
                iWon = kvp.Key == localPlayerId;
                ShowEndGameScreen(iWon ? "ПОБЕДА!" : "ПОРАЖЕНИЕ!");
                return;
            }
        }

        // Проверка на личное поражение: у меня 0 нод
        // Но не показываем в первые 4 секунды после загрузки сцены (чтобы не было ложного поражения в начале)
        if (Time.timeSinceLevelLoad >= 6f && myNodesCount == 0)
        {
            ShowEndGameScreen("ПОРАЖЕНИЕ!");
        }
    }

    private void ShowEndGameScreen(string message)
    {
        if (gameEnded) return;
        gameEnded = true;

        endGamePanel.SetActive(true);
        endGameMessageText.text = message;

        // Отключаем все кнопки нод и бонусы, чтобы нельзя было дальше играть
        foreach (GraphNode node in GraphManager.Instance.AllNodes)
        {
            Button btn = node.GetComponent<Button>();
            if (btn != null) btn.interactable = false;
        }
        for (int i = 0; i < bonusButtons.Length; ++i)
        {
            bonusButtons[i].interactable = false;
        }
    }

    // Кнопка "В главное меню" на панели конца игры
    public void ReturnToMainMenu()
    {
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.LeaveRoom();
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(0); // индекс сцены меню
        }
    }

    public override void OnLeftRoom()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(0); // возвращаемся в главное меню
    }
}
