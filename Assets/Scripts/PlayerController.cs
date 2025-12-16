using UnityEngine;
using Photon.Pun;

public interface IPlayerData
{
    Color Color { get; }
    int Score { get; }
    int GoldCount { get; }
    int ClickPower { get; }
    
    void UpdateScore(int delta);
    void UpdateGold(int delta);
    void ApplyBonus(string bonusType, float value);
}

public class PlayerController : MonoBehaviourPunCallbacks, IPlayerData
{
    public Color Color => _color;
    public int Score => _score;
    public int GoldCount => _goldCount;
    public int ClickPower => clickPower;
    
    [Header("Properties")]
    [SerializeField] private int clickPower = 1;

    private Color _color;
    private int _score;
    private int _goldCount = 0;
    
    private GraphNode[] _ownedNodes;

    private PhotonView _photonView;
    private bool _isInitialized = false;

    private void Awake()
    {
        _photonView = GetComponent<PhotonView>();
        
        if (_photonView.IsMine)
        {
            InitializeLocalPlayer();
        }
    }

    private void InitializeLocalPlayer()
    {
        Debug.Log($"Local player initialized with number: {PhotonNetwork.LocalPlayer.ActorNumber}");

        _color = Random.ColorHSV(0f, 1f, 1f, 1f, 1f, 1f, 1f, 1f);
        int playerId = PhotonNetwork.LocalPlayer.ActorNumber;
        
        MatchManager.Instance.RegisterPlayer(playerId, this);
        
        _photonView.RPC("RPC_SyncPlayerColor", RpcTarget.AllBuffered, playerId, _color.r, _color.g, _color.b, _color.a);
    }

    [PunRPC]
    private void RPC_SyncPlayerColor(int playerId, float r, float g, float b, float a)
    {
        _color = new Color(r, g, b, a);
        
        if (!_photonView.IsMine && !_isInitialized)
        {
            MatchManager.Instance.RegisterPlayer(playerId, this);
            _isInitialized = true;
            Debug.Log($"Remote player {playerId} registered with color: {_color}");
        }
    }

    public void UpdateScore(int delta)
    {
        if (_photonView.IsMine)
        {
            _photonView.RPC("RPC_UpdateScore", RpcTarget.All, delta);
        }
    }

    public void UpdateGold(int delta)
    {
        if (_photonView.IsMine)
        {
            _photonView.RPC("RPC_UpdateGold", RpcTarget.All, delta);
        }
    }
    
    public void ApplyBonus(string bonusType, float value)
    {
        if (_photonView.IsMine)
        {
            _photonView.RPC("RPC_ApplyBonus", RpcTarget.All, bonusType, value);
        }
    }

    [PunRPC]
    private void RPC_UpdateScore(int delta)
    {
        _score += delta;
    }
    
    [PunRPC]
    private void RPC_UpdateGold(int delta)
    {
        _goldCount += delta;
    }
    
    [PunRPC]
    private void RPC_ApplyBonus(string bonusType, float value)
    {
        if (bonusType == "click_power")
        {
            clickPower = Mathf.RoundToInt(clickPower * value);
            Debug.Log($"Player click power increased to: {clickPower}");
        }
    }
    
    public void RequestScoreSync()
    {
        if (_photonView.IsMine)
        {
            _photonView.RPC("RPC_UpdateScore", RpcTarget.All, _score);
        }
    }
}
