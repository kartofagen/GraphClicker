using UnityEngine;

public class DontDestroyEndGame : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        gameObject.SetActive(false);
    }
}