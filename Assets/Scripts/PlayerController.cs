using UnityEngine;
using Photon.Pun;
using TMPro;

public class PlayerController : MonoBehaviourPunCallbacks
{
    [Header("Player Properties")]
    [SerializeField] private float clickPower = 1f;
    
    private float _goldCount = 0f;
    private int _ownedNodesCount = 0;
    
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
    }
    
    private void Update()
    {
        if (!_photonView.IsMine)
            return;
        
        //...
    }
}
