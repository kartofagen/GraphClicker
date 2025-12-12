using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using System;

public class GraphNode : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text text;

    [Header("Node Properties")]
    [Range(0, 100)]
    [SerializeField] private int initialValueMin = 10;
    [Range(0, 100)]
    [SerializeField] private int initialValueMax = 10;

    [Header("Color Settings")]
    [SerializeField] private Image buttonImage;

    private int _nodeValue;
    private int _currentNodeOwner = -1;

    private PhotonView _photonView;
    private bool _isInitialized = false;

    private void Awake()
    {
        _photonView = GetComponent<PhotonView>();

        if (button == null)
            button = GetComponent<Button>();

        if (text == null)
            text = GetComponentInChildren<TMP_Text>();

        if (buttonImage == null)
            buttonImage = GetComponent<Image>();

        button.onClick.AddListener(OnNodeClicked);
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
    }

    private void OnNodeClicked()
    {
        if (PhotonNetwork.LocalPlayer != null)
        {
            int playerId = PhotonNetwork.LocalPlayer.ActorNumber;
            int clickPower = MatchManager.Instance.GetPlayerData(playerId).ClickPower;

            _photonView.RPC("RPC_HandleClick", RpcTarget.All, playerId, clickPower);
        }
    }

    [PunRPC]
    private void RPC_HandleClick(int playerId, int clickPower)
    {
        Debug.Log($"Node clicked by player {playerId} with power {clickPower}");
        
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
        MatchManager.Instance.UpdateScoresUI();
    }

    private void UpdateNodeUI()
    {
        string ownerText = _currentNodeOwner == -1 ? "Nobody" : $"P{_currentNodeOwner}";
        text.text = $"Value: {_nodeValue}\nOwner: {ownerText}";
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
}
