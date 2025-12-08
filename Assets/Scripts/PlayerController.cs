using UnityEngine;
using Photon.Pun;
using TMPro;

public class PlayerController : MonoBehaviourPunCallbacks
{
    [Header("Player Properties")]
    [SerializeField] private float clickPower = 1f;
    
    private float _goldCount = 0f;
    private int _ownedNodesCount = 0;
    
    private Color _color;
    
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
        
        _color = new Color(
            Random.Range(0.5f, 1f),
            Random.Range(0.5f, 1f),
            Random.Range(0.5f, 1f),
            1f
        );
    
        _photonView.RPC("RPC_SetPlayerColor", RpcTarget.AllBuffered,
            PhotonNetwork.LocalPlayer.ActorNumber,
            _color.r, _color.g, _color.b, _color.a);
    }
    
    private void Update()
    {
        if (!_photonView.IsMine)
            return;
        
        //...
    }
    
    [PunRPC]
    private void RPC_SetPlayerColor(int playerId, float r, float g, float b, float a)
    {
        Color color = new Color(r, g, b, a);
        
        GraphNode[] allNodes = FindObjectsOfType<GraphNode>();
        foreach (var node in allNodes)
        {
            node.RPC_UpdatePlayerColor(playerId, r, g, b, a);
        }
    }
}
