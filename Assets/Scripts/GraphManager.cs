using UnityEngine;
using System.Collections.Generic;

public class GraphManager : MonoBehaviour
{
    public static GraphManager Instance { get; private set; }

    [Header("Graph Config")]
    [SerializeField] private GraphNode[] allNodes;
    public GraphNode[] AllNodes => allNodes;

    [SerializeField] private LineRenderer linePrefab; // Prefab с LineRenderer (или UI Line Renderer)

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
        adjacencyList[12] = new List<int> {3, 12};
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

        for (int from = 0; from < allNodes.Length; from++)
        {
            foreach (int to in adjacencyList[from])
            {
                if (from < to) // избегаем дубликатов
                {
                    GameObject lineObj = new GameObject($"Edge_{from}_{to}");
                    // Можно сделать parent'ом пустой объект или граф, но не обязательно
                    lineObj.transform.parent = transform; // или null

                    LineRenderer lr = lineObj.AddComponent<LineRenderer>();

                    lr.useWorldSpace = true;

                    Vector3 startPos = allNodes[from].transform.position;
                    Vector3 endPos = allNodes[to].transform.position;

                    // УБИРАЕМ изменение Z! Оставляем как у нод (обычно Z=0)
                    // startPos.z = ... — УДАЛИТЬ!
                    // endPos.z = ... — УДАЛИТЬ!

                    lr.positionCount = 2;
                    lr.SetPosition(0, startPos);
                    lr.SetPosition(1, endPos);

                    // Толщина
                    lr.startWidth = 5f; // 10f — это ОЧЕНЬ толстая линия! В 2D это может быть 10 юнитов шириной!
                    lr.endWidth = 5f;

                    // Материал
                    lr.material = new Material(Shader.Find("Sprites/Default"));
                    lr.material.color = Color.black;

                    // Округлые концы
                    lr.numCapVertices = 8;

                    // ВАЖНО: порядок отрисовки
                    lr.sortingLayerName = "Default"; // или создай слой "Background" или "Edges"
                    lr.sortingOrder = 0; // линии под нодами

                    // Если ноды используют SpriteRenderer, поставь им sortingOrder = 10, например

                    edgeRenderers.Add(lr);
                }
            }
        }
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