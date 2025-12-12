using UnityEngine;
using Photon.Pun;

public interface IPlayerData
{
    Color Color { get; }
    int Score { get; }
    int ClickPower { get; }
    
    void UpdateScore(int delta);
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

        _color = Random.ColorHSV(
            0f, 1f,
            1f, 1f,
            1f, 1f,
            1f, 1f
            );

        int playerId = PhotonNetwork.LocalPlayer.ActorNumber;
        MatchManager.Instance.RegisterPlayer(playerId, this);
    }

    public void UpdateScore(int delta)
    {
        _score += delta;
    }

    private int CalculateNodesSum()
    {
        //...
        return 0;
    }
    
    
}
