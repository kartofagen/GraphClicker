using UnityEngine;
using Photon.Pun;

public interface IPlayerData
{
    Color Color { get; }
    int Score { get; }
    int ClickPower { get; }
    
    void UpdateScore(int delta, bool fromRPC = false);
}

public class PlayerController : MonoBehaviourPunCallbacks, IPlayerData
{
    public Color Color => _color;
    public int Score => _score;
    public int ClickPower => clickPower;
    
    [Header("Properties")]
    [SerializeField] private int clickPower = 1;

    private Color _color;
    private int _score;
    
    private GraphNode[] _ownedNodes;
    private int _ownedNodesSum = 0;

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

    public void UpdateScore(int delta, bool fromRPC = false)
    {
        _score += delta;
        
        if (_photonView.IsMine && !fromRPC)
        {
            _photonView.RPC("RPC_UpdateScore", RpcTarget.All, _score);
        }
    }

    [PunRPC]
    private void RPC_UpdateScore(int newScore)
    {
        _score = newScore;
        MatchManager.Instance.UpdateScoresUI();
    }
    
    public void RequestScoreSync()
    {
        if (_photonView.IsMine)
        {
            _photonView.RPC("RPC_UpdateScore", RpcTarget.All, _score);
        }
    }

    private int CalculateNodesSum()
    {
        //...
        return 0;
    }
}
