using UnityEngine;
using Photon.Pun;

public class PlayerController : MonoBehaviourPunCallbacks
{
    [Header("Player Properties")]
    [SerializeField] private float clickPower = 1f;

    private Color _color;
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

        MatchManager.Instance.photonView.RPC("RPC_SetPlayerColor", RpcTarget.AllBuffered,
            PhotonNetwork.LocalPlayer.ActorNumber,
            _color.r, _color.g, _color.b, _color.a);
    }

    private int CalculateNodesSum()
    {
        //...
        return 0;
    }
}
