using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class GridManager : MonoBehaviour
{
    public Button startButton;
    public Button pauseButton;
    public Button randomButton;
    public Button endGameButton;
    public Slider speedSlider; 

    public GameObject cellPrefab;
    public int rows = 20;
    public int columns = 20;
    public float stepDelay = 0.5f; // delay between simulation steps in seconds

    private GameObject[,] grid;
    private bool isRunning = false;
    private bool gameOver = false;
    private float CellSpacing = 1.05f;

    public int maxChipsPerPlayer = 20;

    private bool placementPhase = true;
    private Owner activePlayer = Owner.Player1;
    private int p1ChipsPlaced = 0;
    private int p2ChipsPlaced = 0;

    struct NeighborInfo
    {
        public int aliveCount;
        public int player1Count;
        public int player2Count;
    }

    public TMP_Text p1ScoreText;
    public TMP_Text p2ScoreText;
    public TMP_Text statusText;

    private int p1Score = 0;
    private int p2Score = 0;


    NeighborInfo GetNeighborInfo(int x, int y)
    {
        NeighborInfo info = new NeighborInfo();
        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                if (i == 0 && j == 0) continue;
                int nx = x + i;
                int ny = y + j;
                if (nx >= 0 && nx < rows && ny >= 0 && ny < columns)
                {
                    CellClickHandler cell = grid[nx, ny].GetComponent<CellClickHandler>();
                    if (cell.IsAlive())
                    {
                        info.aliveCount++;
                        if (cell.GetOwner() == Owner.Player1) info.player1Count++;
                        else if (cell.GetOwner() == Owner.Player2) info.player2Count++;
                    }
                }
            }
        }
        return info;
    }

    void Start()
    {
        grid = new GameObject[rows, columns];

        for (int x = 0; x < rows; x++)
        {
            for (int y = 0; y < columns; y++)
            {
                Vector3 position = new Vector3(x * CellSpacing, y * CellSpacing, 0);
                GameObject cell = Instantiate(cellPrefab, position, Quaternion.identity, this.transform);
                cell.name = "Cell_" + x + "_" + y;

                cell.GetComponent<SpriteRenderer>().color = Color.white;
                cell.AddComponent<CellClickHandler>();

                grid[x, y] = cell;
            }
        }

        // UI buttons events
        startButton.onClick.AddListener(StartSimulation);
        pauseButton.onClick.AddListener(PauseSimulation);
        randomButton.onClick.AddListener(RandomFill);
        endGameButton.onClick.AddListener(EndGame);
        stepDelay = 0.1f / speedSlider.value;

        speedSlider.onValueChanged.AddListener(delegate { stepDelay = 0.1f / speedSlider.value; });
        UpdateUI();
    }

    public bool IsPlacementPhase() => placementPhase;

    void Update()
    {
        if (placementPhase && Input.GetMouseButtonDown(0))
        {
            Vector2 worldPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(worldPoint, Vector2.zero);

            if (hit.collider != null)
            {
                CellClickHandler cell = hit.collider.GetComponent<CellClickHandler>();
                if (cell != null)
                {
                    bool allowed = false;
                    if (activePlayer == Owner.Player1 && p1ChipsPlaced < maxChipsPerPlayer)
                    {
                        allowed = true;
                        p1ChipsPlaced++;
                    }
                    else if (activePlayer == Owner.Player2 && p2ChipsPlaced < maxChipsPerPlayer)
                    {
                        allowed = true;
                        p2ChipsPlaced++;
                    }

                    if (allowed)
                    {
                        cell.ToggleCell(activePlayer);
                        SwitchTurn();
                    }
                }
            }
            UpdateUI();
        }
    }

    private void SwitchTurn()
    {
        // Only allow switching if both haven't finished placement
        if (activePlayer == Owner.Player1)
            activePlayer = Owner.Player2;
        else
            activePlayer = Owner.Player1;

        if (p1ChipsPlaced >= maxChipsPerPlayer && p2ChipsPlaced >= maxChipsPerPlayer)
            placementPhase = false;
        UpdateUI();
    }

    void StartSimulation()
    {   
        gameOver = false;
        if (!isRunning)
        {
            isRunning = true;
            StartCoroutine(Simulate());
        }
        UpdateUI();
        
    }

    void PauseSimulation()
    {
        isRunning = false;
        StopAllCoroutines();
        UpdateUI();
    }

    void RandomFill()
    {
        PauseSimulation();

        for (int x = 0; x < rows; x++)
        {
            for (int y = 0; y < columns; y++)
            {
                bool alive = Random.value > 0.7f; // 30% chance alive

                if (alive)
                {
                    // Randomly assign owner
                    Owner randomOwner = (Random.value > 0.5f) ? Owner.Player1 : Owner.Player2;
                    grid[x, y].GetComponent<CellClickHandler>().SetAliveWithOwner(true, randomOwner);
                }
                else
                {
                    grid[x, y].GetComponent<CellClickHandler>().SetAliveWithOwner(false, Owner.None);
                }
            }
        }

        placementPhase = false; 
        p1ChipsPlaced = 0;
        p1Score = 0;
        p2ChipsPlaced = 0;
        p2Score = 0;
        gameOver = false;
        UpdateUI();
    }

    IEnumerator Simulate()
    {
        while (isRunning)
        {
            Step();
            yield return new WaitForSeconds(stepDelay);
        }
    }

    void Step()
    {
        if (!isRunning) {
            return;
        }
        bool[,] nextState = new bool[rows, columns];
        Owner[,] nextOwner = new Owner[rows, columns];

        for (int x = 0; x < rows; x++)
        {
            for (int y = 0; y < columns; y++)
            {
                var cell = grid[x, y].GetComponent<CellClickHandler>();
                NeighborInfo info = GetNeighborInfo(x, y);

                bool currentlyAlive = cell.IsAlive();
                Owner currentOwner = cell.GetOwner();

                if (currentlyAlive)
                {
                    nextState[x, y] = (info.aliveCount == 2 || info.aliveCount == 3);
                    nextOwner[x, y] = currentOwner;
                }
                else
                {
                    nextState[x, y] = (info.aliveCount == 3);
                    // New birth: determine majority
                    if (nextState[x, y])
                    {
                        if (info.player1Count > info.player2Count)
                            nextOwner[x, y] = Owner.Player1;
                        else if (info.player2Count > info.player1Count)
                            nextOwner[x, y] = Owner.Player2;
                        else
                            nextOwner[x, y] = Owner.None; // Tie or no alive neighbors
                    }
                    else
                    {
                        nextOwner[x, y] = Owner.None;
                    }
                }
            }
        }

        // Update grid and score
        for (int x = 0; x < rows; x++)
        {
            for (int y = 0; y < columns; y++)
            {
                CellClickHandler cell = grid[x, y].GetComponent<CellClickHandler>();
                Owner prevOwner = cell.GetOwner();
                bool prevAlive = cell.IsAlive();

                cell.SetAliveWithOwner(nextState[x, y], nextOwner[x, y]);
                
                // Score for birth
                if (!prevAlive && nextState[x, y] && nextOwner[x, y] == Owner.Player1)
                {
                    p1Score++;
                }
                else if (!prevAlive && nextState[x, y] && nextOwner[x, y] == Owner.Player2)
                {
                    p2Score++;
                }
            }
        }
        UpdateUI();

        bool anyAlive = false;
        for (int x = 0; x < rows; x++)
        {
            for (int y = 0; y < columns; y++)
            {
                if (grid[x, y].GetComponent<CellClickHandler>().IsAlive())
                {
                    anyAlive = true;
                    break;
                }
            }
            if (anyAlive) break;
        }

        if (!anyAlive)
        {
            EndGame();
        }
    }

    public bool IsRunning()
    {
        return isRunning;
    }

    public void EndGame()
    {
        PauseSimulation();
        placementPhase = false;
        isRunning = false;
        gameOver = true;
        UpdateUI();
    }

    void UpdateUI()
    {
        p1ScoreText.text = "Player 1 Score: " + p1Score;
        p2ScoreText.text = "Player 2 Score: " + p2Score;
        if (gameOver)
        {
            statusText.text = $"Game Over! Final Scores - Player 1: {p1Score}, Player 2: {p2Score}";
        }
        else if (placementPhase)
            statusText.text = "Placement Phase: " + (activePlayer == Owner.Player1 ? "Player 1's Turn" : "Player 2's Turn");
        else if (isRunning)
            statusText.text = "Simulation Running...";
        else
            statusText.text = "Paused";
    }
}
