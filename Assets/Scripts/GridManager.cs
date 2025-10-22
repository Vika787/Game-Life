using UnityEngine;

public class GridManager : MonoBehaviour
{

    public int width = 20;
    public int height = 20;
    public float cellSize = 1f;
    public GameObject cellPrefab;

    [HideInInspector] public bool showFlashes = false;
    [HideInInspector] public bool imageModeActive = false;
    [HideInInspector] public bool battleModeActive = false;

    private Cell[,] grid;

    public int whiteScore = 0;
    public int blackScore = 0;

    // шаблоны фигур

    public static readonly int[,] Blinker = new int[,]
{
    { 1, 1, 1 }
};

    public static readonly int[,] Toad = new int[,]
    {
    { 1, 1, 1, 0 },
    { 0, 1, 1, 1 }
    };

    public static readonly int[,] Beacon = new int[,]
    {
    { 1, 1, 0, 0 },
    { 1, 0, 0, 0 },
    { 0, 0, 0, 1 },
    { 0, 0, 1, 1 }
    };

    public static readonly int[,] Glider = new int[,]
    {
    { 0, 1, 1 },
    { 1, 0, 1 },
    { 0, 0, 1 }
    };


    public static readonly int[,] Pulsar = new int[,]
    {

    {0,0,1,1,1,0,0,0,1,1,1,0,0},
    {0,0,0,0,0,0,0,0,0,0,0,0,0},
    {1,0,0,0,0,1,0,1,0,0,0,0,1},
    {1,0,0,0,0,1,0,1,0,0,0,0,1},
    {1,0,0,0,0,1,0,1,0,0,0,0,1},
    { 0,0,1,1,1,0,0,0,1,1,1,0,0},
    { 0,0,0,0,0,0,0,0,0,0,0,0,0},

    {0,0,1,1,1,0,0,0,1,1,1,0,0},
    {1,0,0,0,0,1,0,1,0,0,0,0,1},
    {1,0,0,0,0,1,0,1,0,0,0,0,1},
    {1,0,0,0,0,1,0,1,0,0,0,0,1},
    {0,0,0,0,0,0,0,0,0,0,0,0,0},
     {0,0,1,1,1,0,0,0,1,1,1,0,0},
    };


    void Start()
    {
        GenerateGrid();
    }


    public void GenerateGrid()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);

        transform.position = Vector3.zero;
        grid = new Cell[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2 pos = new Vector2(x * cellSize, y * cellSize);
                GameObject cellObj = Instantiate(cellPrefab, pos, Quaternion.identity, transform);
                cellObj.name = $"Cell_{x}_{y}";
                var cell = cellObj.GetComponent<Cell>();
                grid[x, y] = cell;
                cell.SetMode(imageModeActive, battleModeActive);
            }
        }


        transform.position = new Vector3(
            -width * cellSize / 2f + cellSize / 2f,
            -height * cellSize + 10,
            0
        );

    }



    public void PlacePattern(int[,] pattern, int startX, int startY)
    {
        for (int y = 0; y < pattern.GetLength(0); y++)
        {
            for (int x = 0; x < pattern.GetLength(1); x++)
            {
                int gx = startX + x;
                int gy = startY + y;

                if (gx >= 0 && gx < width && gy >= 0 && gy < height)
                {
                    bool alive = pattern[y, x] == 1;
                    grid[gx, gy].SetAlive(alive, showFlashes);
                }
            }
        }
    }



    public void NextGeneration()
    {

        if (battleModeActive)
        {
            NextGenerationBattle();
            return;
        }
        int[,] neighbors = new int[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                neighbors[x, y] = CountAliveNeighbors(x, y);
            }
        }


        bool[,] newState = new bool[width, height];


        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                bool isAlive = grid[x, y].isAlive;
                int n = neighbors[x, y];

                if (isAlive && (n == 2 || n == 3))
                {
                    newState[x, y] = true;
                }
                else if (!isAlive && n == 3)
                {
                    newState[x, y] = true;
                }
                else
                {
                    newState[x, y] = false;
                }
            }
        }


        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                grid[x, y].SetAlive(newState[x, y], showFlashes);
            }
        }

    }



    private void NextGenerationBattle()
    {

        Owner[,] nextOwners = new Owner[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int aliveNeighbors = 0;
                int whiteCount = 0;
                int blackCount = 0;

                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;

                        int nx = x + dx;
                        int ny = y + dy;

                        if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                        {
                            var n = grid[nx, ny];
                            if (n.isAlive)
                            {
                                aliveNeighbors++;
                                if (n.owner == Owner.White) whiteCount++;
                                if (n.owner == Owner.Black) blackCount++;
                            }
                        }
                    }
                }

                var c = grid[x, y];
                Owner newOwner = c.owner;
                bool alive = c.isAlive;

                if (alive && (aliveNeighbors == 2 || aliveNeighbors == 3))
                {
                    if (whiteCount > blackCount)
                    {
                        newOwner = Owner.White;
                        whiteScore++;
                    }
                    else if (blackCount > whiteCount)
                    {
                        newOwner = Owner.Black;
                        blackScore++;
                    }
                    else
                    {
                        newOwner = (Random.value > 0.5f) ? Owner.White : Owner.Black;
                        if (newOwner == Owner.White) whiteScore++; else blackScore++;
                    }
                }
                else if (!alive && aliveNeighbors == 3)
                {

                    if (whiteCount > blackCount)
                    {
                        newOwner = Owner.White;
                        whiteScore++;
                    }
                    else if (blackCount > whiteCount)
                    {
                        newOwner = Owner.Black;
                        blackScore++;
                    }
                    else
                    {
                        newOwner = (Random.value > 0.5f) ? Owner.White : Owner.Black;
                        if (newOwner == Owner.White) whiteScore++; else blackScore++;
                    }

                    alive = true;
                }
                else
                {
                    alive = false;
                    newOwner = Owner.None;
                }

                nextOwners[x, y] = newOwner;
            }
        }

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                grid[x, y].SetAlive(nextOwners[x, y] != Owner.None, showFlashes, nextOwners[x, y]);
    }


    public int CountAliveNeighbors(int x, int y)
    {
        int count = 0;

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0)
                    continue;

                int nx = x - dx;
                int ny = y - dy;

                if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                {
                    var neighbor = grid[nx, ny];
                    if (neighbor != null && neighbor.isAlive)
                        count++;
                }
            }
        }
        return count;

    }


    public void Randomize(float aliveChance = 0.25f)
    {

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                bool alive = Random.value < aliveChance;
                grid[x, y].SetAlive(alive, showFlashes);
            }
        }
    }
    
    public Cell GetCell(int x, int y) => grid[x, y];


}