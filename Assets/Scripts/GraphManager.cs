using UnityEngine;
using System.Collections.Generic;

public class GraphManager : MonoBehaviour
{
    public static GraphManager Instance { get; private set; }

    [Header("Graph Config")]
    [SerializeField] private GraphNode[] allNodes;
    public GraphNode[] AllNodes => allNodes;

    private Dictionary<int, List<int>> adjacencyList = new();
    private List<LineRenderer> edgeRenderers = new();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        SetupGraph();

        CreateEdgesVisual();
    }

    private void SetupGraph()
    {
        adjacencyList.Clear();
        adjacencyList[0] = new List<int> {20};
        adjacencyList[1] = new List<int> {2};
        adjacencyList[2] = new List<int> {1, 3};
        adjacencyList[3] = new List<int> {2, 4, 12, 14};
        adjacencyList[4] = new List<int> {3, 5, 7, 8, 19, 22};
        adjacencyList[5] = new List<int> {4, 6, 19, 20};
        adjacencyList[6] = new List<int> {5};
        adjacencyList[7] = new List<int> {4};
        adjacencyList[8] = new List<int> {4, 9, 14, 19};
        adjacencyList[9] = new List<int> {8, 10, 14, 15};
        adjacencyList[10] = new List<int> {9, 11};
        adjacencyList[11] = new List<int> {10};
        adjacencyList[12] = new List<int> {3, 13};
        adjacencyList[13] = new List<int> {12};
        adjacencyList[14] = new List<int> {3, 8, 9, 17};
        adjacencyList[15] = new List<int> {9, 16};
        adjacencyList[16] = new List<int> {15};
        adjacencyList[17] = new List<int> {14, 18};
        adjacencyList[18] = new List<int> {17};
        adjacencyList[19] = new List<int> {4, 5, 8, 23};
        adjacencyList[20] = new List<int> {0, 5};
        adjacencyList[21] = new List<int> {22};
        adjacencyList[22] = new List<int> {4, 21};
        adjacencyList[23] = new List<int> {19};
    }

    private void CreateEdgesVisual()
    {
        // Удаляем старые линии
        foreach (LineRenderer lr in edgeRenderers)
        {
            if (lr != null) Destroy(lr.gameObject);
        }

        edgeRenderers.Clear();

        // Получаем Canvas и его камеру
        Canvas canvas = allNodes[0].GetComponentInParent<Canvas>();
        Camera canvasCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        for (int from = 0; from < allNodes.Length; from++)
        {
            foreach (int to in adjacencyList[from])
            {
                if (from < to)
                {
                    GameObject lineObj = new GameObject($"Edge_{from}_{to}");
                    lineObj.transform.SetParent(transform, false);
                    
                    LineRenderer lr = lineObj.AddComponent<LineRenderer>();
                    lr.useWorldSpace = true;

                    lr.startWidth = 0.1f;
                    lr.endWidth = 0.1f;
                    lr.material = new Material(Shader.Find("Sprites/Default"));
                    lr.material.color = Color.white;
                    lr.numCapVertices = 8;
                    
                    RectTransform rectFrom = allNodes[from].GetComponent<RectTransform>();
                    RectTransform rectTo = allNodes[to].GetComponent<RectTransform>();

                    Vector3 startPos = GetWorldPositionFromRectTransform(rectFrom, canvas, canvasCamera);
                    Vector3 endPos = GetWorldPositionFromRectTransform(rectTo, canvas, canvasCamera);

                    startPos.z = transform.position.z + 1;
                    endPos.z = transform.position.z + 1;

                    lr.positionCount = 2;
                    lr.SetPosition(0, startPos);
                    lr.SetPosition(1, endPos);

                    edgeRenderers.Add(lr);
                }
            }
        }
    }

    private Vector3 GetWorldPositionFromRectTransform(RectTransform rectTransform, Canvas canvas, Camera canvasCamera)
    {
        // Для ScreenSpaceOverlay
        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            Vector3 screenPos = rectTransform.position;
            // Создаем точку в мировых координатах на определенной глубине
            return Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 10f));
        }
        // Для других режимов Canvas
        return rectTransform.position;
    }

    public List<int> GetNeighbors(int nodeIndex)
    {
        if (adjacencyList.TryGetValue(nodeIndex, out List<int> neighbors))
            return neighbors;
        return new List<int>();
    }

    public void OnNodeOwnerChanged(int nodeIndex)
    {
        
    }

    private GraphNode FindNodeAtPosition(Vector3 position)
    {
        foreach (GraphNode node in allNodes)
        {
            if (Vector3.Distance(node.transform.position, position) < 0.1f)
                return node;
        }
        return null;
    }
}