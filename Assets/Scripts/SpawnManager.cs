using UnityEngine;
using Photon.Pun;

public class SpawnManager : MonoBehaviour
{
    [SerializeField] private GameObject spawnPrefab;
    [SerializeField] private Transform spawnPoint;

    private void Start()
    {
        PhotonNetwork.Instantiate(spawnPrefab.name, spawnPoint.position, Quaternion.identity);
    }
}
