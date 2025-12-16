using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using System;
using System.Collections.Generic;

public class GraphNode : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text ownerText;
    [SerializeField] private TMP_Text scoreText;

    [Header("Node Properties")]
    [Range(0, 100)]
    [SerializeField] private int initialValueMin = 10;
    [Range(0, 100)]
    [SerializeField] private int initialValueMax = 10;
    
    [Header("Gold Generation")]
    [SerializeField] private float goldGenerationInterval = 5f;
    [SerializeField] private float goldValuePerSecond = 0.1f;

    [Header("Color Settings")]
    [SerializeField] private Image buttonImage;

    private int _nodeValue;
    private int _currentNodeOwner = -1;
    public int CurrentNodeOwner => _currentNodeOwner;

    private PhotonView _photonView;
    private bool _isInitialized = false;
    private float _goldGenerationTimer;

    [Header("Graph")]
    [SerializeField] private int nodeIndex = -1;

    private GraphManager _graphManager;

    private GraphManager graphManager
    {
        get
        {
            if (_graphManager == null)
            {
                _graphManager = GraphManager.Instance;
                if (_graphManager == null)
                {
                    Debug.LogError($"[НОДА {nodeIndex}] GraphManager всё ещё null! Убедись, что объект GraphManager есть в сцене и активен.");
                }
                else
                {
                    Debug.Log($"[НОДА {nodeIndex}] GraphManager найден (lazy init)");
                }
            }
            return _graphManager;
        }
    }

    private void Awake()
    {
        _photonView = GetComponent<PhotonView>();

        if (button == null)
            button = GetComponent<Button>();

        if (buttonImage == null)
            buttonImage = GetComponent<Image>();

        button.onClick.AddListener(OnNodeClicked);

        _goldGenerationTimer = nodeIndex / 100f; // чтоб не все одновременно
    }

    private void Start()
    {
        if (PhotonNetwork.IsMasterClient && _photonView.IsMine)
        {
            InitializeNode();
        }
        else if (!_isInitialized)
        {
            _photonView.RPC("RPC_RequestSync", RpcTarget.MasterClient);
        }
    }
    
    private void Update()
    {
        if (PhotonNetwork.IsMasterClient && _currentNodeOwner != -1 && _nodeValue > 0)
        {
            _goldGenerationTimer += Time.deltaTime;
            
            if (_goldGenerationTimer >= goldGenerationInterval)
            {
                GenerateGold();
                _goldGenerationTimer = 0f;
            }
        }
    }

    private void GenerateGold()
    {
        int goldGenerated = Mathf.RoundToInt(_nodeValue * goldValuePerSecond * goldGenerationInterval);
        
        if (goldGenerated > 0)
        {
            MatchManager.Instance.UpdatePlayerGold(_currentNodeOwner, goldGenerated);
            Debug.Log($"Node [{nodeIndex}] generated {goldGenerated} gold for player {_currentNodeOwner}");
        }
    }

    private void InitializeNode()
    {
        _nodeValue = UnityEngine.Random.Range(initialValueMin, initialValueMax + 1);
        _currentNodeOwner = -1;
        _isInitialized = true;

        _photonView.RPC("RPC_SyncNodeState", RpcTarget.AllBuffered, _nodeValue, _currentNodeOwner);
    }

    [PunRPC]
    private void RPC_RequestSync()
    {
        if (PhotonNetwork.IsMasterClient && _photonView.IsMine)
        {
            _photonView.RPC("RPC_SyncNodeState", RpcTarget.Others, _nodeValue, _currentNodeOwner);
        }
    }

    [PunRPC]
    private void RPC_SyncNodeState(int value, int owner)
    {
        _nodeValue = value;
        _currentNodeOwner = owner;
        _isInitialized = true;

        UpdateNodeUI();
        UpdateNodeColor();

        graphManager?.OnNodeOwnerChanged(nodeIndex);
    }

    private void OnNodeClicked()
    {
        if (PhotonNetwork.LocalPlayer == null) return;

        int myId = PhotonNetwork.LocalPlayer.ActorNumber;
        int clickPower = MatchManager.Instance.GetPlayerData(myId)?.ClickPower ?? 1;

        bool canClick = false;

        // Если нода уже моя — можно кликать всегда
        if (_currentNodeOwner == myId)
        {
            canClick = true;
        }
        else
        {
            // Проверяем соседей
            List<int> neighbors = graphManager.GetNeighbors(nodeIndex);
            foreach (int neighIndex in neighbors)
            {
                GraphNode neighbor = graphManager.AllNodes[neighIndex];
                if (neighbor.CurrentNodeOwner == myId) // Используем публичное свойство!
                {
                    canClick = true;
                    break;
                }
            }

            // Дополнительное правило: первый захват нейтральной ноды разрешён всегда
            // (чтобы игрок мог начать игру)
            if (!canClick && _currentNodeOwner == -1)
            {
                bool hasAnyNode = false;
                foreach (GraphNode node in graphManager.AllNodes)
                {
                    if (node.CurrentNodeOwner == myId)
                    {
                        hasAnyNode = true;
                        break;
                    }
                }

                if (!hasAnyNode)
                {
                    canClick = true;
                }
            }
        }

        if (canClick)
        {
            _photonView.RPC("RPC_HandleClick", RpcTarget.All, myId, clickPower);
        }
        else
        {
            Debug.Log($"Player {myId} НЕ может захватить ноду [{nodeIndex}]: нет соседней своей вершины.");
        }
    }

    [PunRPC]
    private void RPC_HandleClick(int playerId, int clickPower)
    {
        Debug.Log($"Node clicked by player {playerId} with power {clickPower}");

        // Специальный случай: если значение 0 и никто не владеет — захватываем сразу
        if (_currentNodeOwner == -1 && _nodeValue == 0)
        {
            _currentNodeOwner = playerId;
        }

        if (_currentNodeOwner == playerId)
        {
            _nodeValue += clickPower;
            MatchManager.Instance.UpdatePlayerScore(playerId, clickPower);
        }
        else
        {
            int delta = -Math.Min(clickPower, _nodeValue);
            if (_currentNodeOwner != -1)
            {
                MatchManager.Instance.UpdatePlayerScore(_currentNodeOwner, delta);
            }
            _nodeValue += delta;
            if (_nodeValue <= 0)
            {
                _nodeValue = 0;
                _currentNodeOwner = -1;
            }
        }

        UpdateNodeUI();
        UpdateNodeColor();
        graphManager?.OnNodeOwnerChanged(nodeIndex);
        MatchManager.Instance.UpdateScoresUI();
    }

    private void UpdateNodeUI()
    {
        string ownerName = _currentNodeOwner == -1 ? "Nobody" : $"P{_currentNodeOwner}";
        ownerText.text = $"{ownerName}";
        
        scoreText.text = $"{_nodeValue}";

        /*// Правильные настройки для маленького текста
        text.fontSize = 24f;                    // Базовый размер — подбери (16–28 обычно хорошо)
        text.fontSizeMin = 10f;                 // Минимальный размер при авто-подгонке
        text.fontSizeMax = 28f;                 // Максимальный
        text.enableAutoSizing = true;           // Включаем авто-размер
        text.alignment = TextAlignmentOptions.Center; // По центру*/
    }

    public void UpdateNodeColor()
    {
        if (buttonImage == null) return;

        if (_currentNodeOwner == -1)
        {
            buttonImage.color = Color.gray;
        }
        else
        {
            Color playerColor = MatchManager.Instance.GetPlayerColor(_currentNodeOwner);
            buttonImage.color = playerColor;
        }
    }

    [PunRPC]
    public void RPC_SetOwner(int ownerActorNumber)
    {
        int oldOwner = _currentNodeOwner;
        _currentNodeOwner = ownerActorNumber;
        MatchManager.Instance.ChangeNodeOwner(oldOwner, _currentNodeOwner, _nodeValue);
        UpdateNodeUI();
        UpdateNodeColor();
        MatchManager.Instance.UpdateScoresUI();
    }

    [PunRPC]
    private void RPC_AssignAsStartingNode(int playerId)
    {
        if (_currentNodeOwner != -1) return; // Уже занята — не трогаем

        _currentNodeOwner = playerId;
        _nodeValue = Math.Max(_nodeValue, 10); // Минимум 10, или оставь как есть
        MatchManager.Instance.UpdatePlayerScore(playerId, _nodeValue);

        Debug.Log($"Нода [{nodeIndex}] назначена как стартовая игроку {playerId}");

        UpdateNodeUI();
        UpdateNodeColor();
        graphManager?.OnNodeOwnerChanged(nodeIndex);
        MatchManager.Instance.UpdateScoresUI();
    }
}