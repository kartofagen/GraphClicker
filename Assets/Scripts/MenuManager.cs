using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI; // добавь это

public class MenuManager : MonoBehaviourPunCallbacks
{
    [SerializeField] private int maxPlayers = 5;
    [SerializeField] private Button playButton; // кнопка Play
    [SerializeField] private Button exitButton; // кнопка Exit
    [SerializeField] private bool OfflineMode = false;

    private void Awake()
    {
        // Находим кнопки автоматически, если не назначены
        if (playButton == null)
        {
            playButton = GameObject.Find("PlayButton")?.GetComponent<Button>(); // замени на точное имя твоей кнопки Play
        }

        if (exitButton == null)
        {
            exitButton = GameObject.Find("ExitButton")?.GetComponent<Button>(); // замени на точное имя твоей кнопки Exit
        }

        // Подписываемся на клики
        if (playButton != null)
        {
            playButton.onClick.RemoveAllListeners(); // очищаем старые
            playButton.onClick.AddListener(JoinRandomRoom);
        }

        if (exitButton != null)
        {
            exitButton.onClick.RemoveAllListeners();
            exitButton.onClick.AddListener(QuitGame);
        }
    }

    private void Start()
    {
        if (!PhotonNetwork.IsConnected)
        {
            if (!OfflineMode)
                PhotonNetwork.ConnectUsingSettings();
            else
                PhotonNetwork.OfflineMode = true;
        }
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Successfully connected to server!");
        if (playButton != null) playButton.interactable = true;
    }

    private void CreateRoom()
    {
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = maxPlayers;
        PhotonNetwork.CreateRoom($"Room{Random.Range(0, 1000)}", roomOptions);
    }
    
    public void JoinRandomRoom()
    {
        PhotonNetwork.JoinRandomRoom();
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("Can't find available room: " + message + ". Creating new one...");
        CreateRoom();
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Room joined or created!");
        PhotonNetwork.LoadLevel(1); // GameNetworking
    }

    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}