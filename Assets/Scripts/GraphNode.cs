using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using System.Collections.Generic;

public class GraphNode : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text text;
    [SerializeField] private TMP_Text scoresText;
    
    [Header("Node Properties")]
    [SerializeField] private int initialValue = 10;
    
    [Header("Color Settings")]
    [SerializeField] private Image buttonImage;
    private static Dictionary<int, Color> _playerColors = new Dictionary<int, Color>();
    
    private int _nodeValue;
    private int _currentNodeOwner = -1;
    private Dictionary<int, int> _playerScores = new Dictionary<int, int>();
    
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
            _nodeValue = initialValue;
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
        UpdateNodeUI();
        UpdateScoresText();
        UpdateNodeColor();
    }
    
    private void OnNodeClicked()
    {
        if (PhotonNetwork.LocalPlayer != null)
        {
            int localPlayerActorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
            int clickPower = 1;
            
            _photonView.RPC("RPC_HandleClick", RpcTarget.All, localPlayerActorNumber, clickPower);
        }
    }
    
    [PunRPC]
    private void RPC_HandleClick(int clickerActorNumber, int clickPower)
    {
        if (_currentNodeOwner == clickerActorNumber)
        {
            _nodeValue += clickPower;
            
            _playerScores.TryAdd(clickerActorNumber, 0);
            _playerScores[clickerActorNumber] = _nodeValue;
        }
        else
        {
            _nodeValue -= clickPower;
        
            if (_nodeValue <= 0)
            {
                _nodeValue = 0;
                _currentNodeOwner = clickerActorNumber;
            }
        }
    
        UpdateNodeUI();
        UpdateScoresText();
        UpdateNodeColor();
    }
    
    private void UpdateNodeUI()
    {
        string ownerText = _currentNodeOwner == -1 ? "Nobody" : $"P{_currentNodeOwner}";
        
        text.text = $"Value: {_nodeValue}\nOwner: {ownerText}";
    }
    
    [PunRPC]
    public void RPC_SetOwner(int ownerActorNumber)
    {
        _currentNodeOwner = ownerActorNumber;
        UpdateNodeUI();
    }
    
    [PunRPC]
    public void RPC_SetValue(int value)
    {
        _nodeValue = value;
        UpdateNodeUI();
    }
    
    private void UpdateScoresText()
    {
        if (scoresText != null)
        {
            string scores = "Scores:\n";
            foreach (var kvp in _playerScores)
            {
                scores += $"P{kvp.Key}: {kvp.Value}\n";
            }
            scoresText.text = scores;
        }
    }

    private void UpdateNodeColor()
    {
        if (buttonImage == null) return;
    
        if (_currentNodeOwner == -1)
        {
            buttonImage.color = Color.gray;
        }
        else if (_playerColors.ContainsKey(_currentNodeOwner))
        {
            buttonImage.color = _playerColors[_currentNodeOwner];
        }
        else
        {
            buttonImage.color = Color.white;
        }
    }
    
    [PunRPC]
    public void RPC_UpdatePlayerColor(int playerId, float r, float g, float b, float a)
    {
        Color color = new Color(r, g, b, a);
        _playerColors[playerId] = color;
    
        if (_currentNodeOwner == playerId)
        {
            UpdateNodeColor();
        }
    }
}
