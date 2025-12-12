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

    private void Awake()
    {
        _photonView = GetComponent<PhotonView>();

        if (button == null)
            button = GetComponent<Button>();

        if (text == null)
            text = GetComponentInChildren<TMP_Text>();

        if (buttonImage == null)
            buttonImage = GetComponent<Image>();

        if (_photonView.IsMine)
        {
            _nodeValue = UnityEngine.Random.Range(initialValueMin, initialValueMax);
            _currentNodeOwner = -1;
            _photonView.RPC("RPC_InitializeNode", RpcTarget.AllBuffered, _nodeValue, _currentNodeOwner);
        }

        button.onClick.AddListener(OnNodeClicked);
    }

    [PunRPC]
    private void RPC_InitializeNode(int value, int owner)
    {
        _nodeValue = value;
        _currentNodeOwner = owner;
        if (_currentNodeOwner != -1)
        {
            MatchManager.Instance.UpdatePlayerScore(_currentNodeOwner, _nodeValue);
            MatchManager.Instance.UpdateScoresUI();
        }
        
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

    private void UpdateNodeColor()
    {
        if (buttonImage == null) return;

        if (_currentNodeOwner == -1)
        {
            buttonImage.color = Color.gray;
        }
        else
        {
            buttonImage.color = MatchManager.Instance.GetPlayerColor(_currentNodeOwner);
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
