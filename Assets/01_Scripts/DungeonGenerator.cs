using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DungeonGenerator : MonoBehaviour
{
    // ======== NIVELES ========
    [Header("Niveles")]
    [SerializeField] private int currentLevel = 1;
    [SerializeField] private int maxLevel = 5;
    [Tooltip("Nombre de la escena a cargar al terminar el nivel 5")]
    public string finalSceneName = "Scene_2";
    private RoomBehaviour firstRoomOfLevel; // ⬅️ PRIMER CUARTO DEL NIVEL

    // ======== PORTAL ========
    [Header("Portal Final de Cada Nivel")]
    public GameObject portalPrefab;     // tu portal prefab (con script Portal)
    public float portalHeight = 0.5f;

    // ======== PLAYER & CÁMARA ========
    [Header("Player")]
    public GameObject playerPrefab;
    public float playerEyeHeight = 1.6f;

    private Transform playerTransform;      // lo mantenemos entre niveles
    private bool firstRoomSaved = false;
    private Vector3 firstRoomWorldPos;

    // ======== GRID / REGLAS ========
    public class Cell
    {
        public bool visited = false;
        public bool[] status = new bool[4];
    }

    [System.Serializable]
    public class Rule
    {
        public GameObject room;
        public Vector2Int minPosition;
        public Vector2Int maxPosition;
        public bool obligatory;

        public int ProbabilityOfSpawning(int x, int y)
        {
            if (x >= minPosition.x && x <= maxPosition.x && y >= minPosition.y && y <= maxPosition.y)
                return obligatory ? 2 : 1;
            return 0;
        }
    }

    [Header("Grid")]
    public Vector2Int size;
    public int startPos = 0;
    public Rule[] rooms;
    public Vector2 offset;

    // runtime
    private List<Cell> board;
    private RoomBehaviour lastRoomCreated;
    private int roomCounter = 0;

    // ---------------------------------------------------------------------
    void Start()
    {
        BeginLevel();  // Inicia Nivel 1
    }

    // ==== Ciclo de nivel ==================================================
    private void BeginLevel()
    {
        // limpiar escena de rooms previos
        ClearDungeon();

        // reset estado de generación
        firstRoomSaved = false;
        firstRoomWorldPos = Vector3.zero;
        lastRoomCreated = null;
        roomCounter = 0;
        firstRoomOfLevel = null;

        // genera laberinto + rooms
        MazeGenerator();
    }

    // llamado por el portal cuando el player entra
    public void OnPortalEntered()
    {
        if (currentLevel < maxLevel)
        {
            currentLevel++;
            BeginLevel();                // avanzar de nivel (regenerar)
        }
        else
        {
            // nivel final: cargar la escena final
            if (!string.IsNullOrEmpty(finalSceneName))
            {
                SceneManager.LoadScene(finalSceneName);
            }
            else
            {
                Debug.LogWarning("[DG] finalSceneName vacío. No se cargó escena.");
            }
        }
    }

    // ==== Limpieza ========================================================
    private void ClearDungeon()
    {
        // Destruir cuartos previos (hijos de este transform)
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }

        // reset de roomVisited para la UI por cuarto
        RoomUIManager.PlayerRoomState.LastRoomId = -1;
    }

    // ==== Generación ======================================================
    void GenerateDungeon()
    {
        for (int i = 0; i < size.x; i++)
        {
            for (int j = 0; j < size.y; j++)
            {
                Cell currentCell = board[(i + j * size.x)];
                if (currentCell.visited)
                {
                    int randomRoom = -1;
                    List<int> availableRooms = new List<int>();

                    for (int k = 0; k < rooms.Length; k++)
                    {
                        int p = rooms[k].ProbabilityOfSpawning(i, j);
                        if (p == 2) { randomRoom = k; break; }
                        else if (p == 1) availableRooms.Add(k);
                    }

                    if (randomRoom == -1)
                    {
                        if (availableRooms.Count > 0) randomRoom = availableRooms[Random.Range(0, availableRooms.Count)];
                        else randomRoom = 0;
                    }

                    var newRoom = Instantiate(
                        rooms[randomRoom].room,
                        new Vector3(i * offset.x, 0, -j * offset.y),
                        Quaternion.identity,
                        transform
                    ).GetComponent<RoomBehaviour>();

                    newRoom.UpdateRoom(currentCell.status);
                    newRoom.name += " " + i + "-" + j;

                    // Asigna ID incremental, recuerda "último" (final del nivel)
                    roomCounter++;
                    newRoom.roomId = roomCounter;
                    lastRoomCreated = newRoom;

                    // Guardar primer cuarto para spawn (+ referenciar cuál es)
                    if (!firstRoomSaved && newRoom.spawnPoint != null)
                    {
                        firstRoomSaved = true;
                        firstRoomWorldPos = newRoom.spawnPoint.position;
                        firstRoomOfLevel = newRoom; // ⬅️ NUEVO
                    }

                    // crear el trigger RoomArea automáticamente
                    AddRoomArea(newRoom);
                }
            }
        }

        // ----- Spawn o recolocar player -----
        if (firstRoomSaved)
        {
            Vector3 pos = firstRoomWorldPos + Vector3.up * 0.5f;

            if (playerTransform == null && playerPrefab != null)
            {
                var playerGO = Instantiate(playerPrefab, pos, Quaternion.identity);
                playerTransform = playerGO.transform;
                AttachFirstPersonCamera(playerTransform);
            }
            else if (playerTransform != null)
            {
                var rb = playerTransform.GetComponent<Rigidbody>();
                if (rb) { rb.velocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
                playerTransform.SetPositionAndRotation(pos, Quaternion.identity);
            }

            // ✅ Marcar que ya estamos en el primer cuarto y suprimir el primer banner de "Cuarto"
            if (firstRoomOfLevel != null)
                RoomUIManager.PlayerRoomState.LastRoomId = firstRoomOfLevel.roomId;

            RoomUIManager.PlayerRoomState.suppressNextRoomBanner = true;

            // ✅ Mostrar "Nivel N" tras 1 frame (evita carreras de UI)
            StartCoroutine(ShowLevelBanner());
        }

        // ----- Portal en el cuarto final -----
        if (lastRoomCreated != null && portalPrefab != null)
        {
            Vector3 pos;

            if (lastRoomCreated.spawnPoint != null)
            {
                // Coloca el portal en el centro real del cuarto (spawnPoint)
                pos = lastRoomCreated.spawnPoint.position + Vector3.up * portalHeight;
            }
            else
            {
                // Fallbacks por si olvidaste el spawnPoint
                var area = lastRoomCreated.transform.Find("RoomArea");
                if (area != null && area.TryGetComponent<BoxCollider>(out var box))
                    pos = box.bounds.center + Vector3.up * portalHeight; // centro real del collider
                else
                {
                    // último recurso: calcula centro con bounds de colliders/renderers
                    pos = GetApproxRoomCenter(lastRoomCreated) + Vector3.up * portalHeight;
                }
            }

            var portal = Instantiate(portalPrefab, pos, Quaternion.identity, lastRoomCreated.transform);
            portal.name = $"PORTAL_Nivel_{currentLevel}";
            Debug.Log($"[DG] Portal colocado en {pos} para Room '{lastRoomCreated.name}' (Nivel {currentLevel})");
        }
    }

    private Vector3 GetApproxRoomCenter(RoomBehaviour room)
    {
        // 1) Intenta con colliders
        var cols = room.GetComponentsInChildren<Collider>(true);
        if (cols != null && cols.Length > 0)
        {
            Bounds b = cols[0].bounds;
            for (int i = 1; i < cols.Length; i++) b.Encapsulate(cols[i].bounds);
            return b.center;
        }

        // 2) Intenta con renderers (si no hay colliders en el piso)
        var rends = room.GetComponentsInChildren<Renderer>(true);
        if (rends != null && rends.Length > 0)
        {
            Bounds b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            return b.center;
        }

        // 3) Último recurso: asume centro de celda a partir del pivote (pivote en esquina)
        return room.transform.position + new Vector3(offset.x * 0.5f, 0f, -offset.y * 0.5f);
    }

    void MazeGenerator()
    {
        board = new List<Cell>();
        for (int i = 0; i < size.x; i++)
            for (int j = 0; j < size.y; j++)
                board.Add(new Cell());

        int currentCell = startPos;
        Stack<int> path = new Stack<int>();
        int k = 0;

        while (k < 1000)
        {
            k++;
            board[currentCell].visited = true;

            if (currentCell == board.Count - 1) break;

            List<int> neighbors = CheckNeighbors(currentCell);

            if (neighbors.Count == 0)
            {
                if (path.Count == 0) break;
                else currentCell = path.Pop();
            }
            else
            {
                path.Push(currentCell);
                int newCell = neighbors[Random.Range(0, neighbors.Count)];

                if (newCell > currentCell)
                {
                    // down / right
                    if (newCell - 1 == currentCell)
                    {
                        board[currentCell].status[2] = true;
                        currentCell = newCell;
                        board[currentCell].status[3] = true;
                    }
                    else
                    {
                        board[currentCell].status[1] = true;
                        currentCell = newCell;
                        board[currentCell].status[0] = true;
                    }
                }
                else
                {
                    // up / left
                    if (newCell + 1 == currentCell)
                    {
                        board[currentCell].status[3] = true;
                        currentCell = newCell;
                        board[currentCell].status[2] = true;
                    }
                    else
                    {
                        board[currentCell].status[0] = true;
                        currentCell = newCell;
                        board[currentCell].status[1] = true;
                    }
                }
            }
        }

        GenerateDungeon();
    }

    List<int> CheckNeighbors(int cell)
    {
        List<int> neighbors = new List<int>();

        // up
        if (cell - size.x >= 0 && !board[(cell - size.x)].visited)
            neighbors.Add((cell - size.x));
        // down
        if (cell + size.x < board.Count && !board[(cell + size.x)].visited)
            neighbors.Add((cell + size.x));
        // right
        if ((cell + 1) % size.x != 0 && !board[(cell + 1)].visited)
            neighbors.Add((cell + 1));
        // left
        if (cell % size.x != 0 && !board[(cell - 1)].visited)
            neighbors.Add((cell - 1));

        return neighbors;
    }

    // ==== utilidades ======================================================
    private void AttachFirstPersonCamera(Transform player)
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("No se encontró una cámara con tag MainCamera en la escena.");
            return;
        }

        cam.transform.SetParent(player);
        cam.transform.localPosition = new Vector3(0f, playerEyeHeight, 0f);
        cam.transform.localRotation = Quaternion.identity;

        var look = cam.GetComponent<FirstPersonLook>();
        if (look == null) look = cam.gameObject.AddComponent<FirstPersonLook>();
        look.playerBody = player;
    }

    private void AddRoomArea(RoomBehaviour room)
    {
        var areaGO = new GameObject("RoomArea");
        areaGO.transform.SetParent(room.transform, false);

        // ⬅️ CENTRADO EN EL SPAWNPOINT (si existe)
        Vector3 local = Vector3.zero;
        if (room.spawnPoint != null)
            local = room.transform.InverseTransformPoint(room.spawnPoint.position);
        areaGO.transform.localPosition = local;

        var box = areaGO.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(offset.x * 0.9f, 3f, offset.y * 0.9f);
        box.center = new Vector3(0f, 1.5f, 0f);

        var area = areaGO.AddComponent<RoomArea>();
        area.room = room;
    }

    private IEnumerator ShowLevelBanner()
    {
        yield return null; // esperar 1 frame por si la UI termina de inicializar
        RoomUIManager.Show($"Nivel {currentLevel}");
    }
}

