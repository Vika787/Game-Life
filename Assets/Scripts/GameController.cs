using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class GameController : MonoBehaviour
{

    // кнопки
    
    public Button startPauseButton; // запуск/остановка процесса обновления поля
    public TextMeshProUGUI buttonLabel; // надпись на  кнопке startPauseButton
    public Slider speedSlider; // изменение скорости обновления поля
    public Button stepButton; // один шаг обновления поля



    public Button clearButton; // очищение поля


    public Button neighborsButton; // переключение на режим отображения соседей
    public Button flashButton; // переключение на режим отображения умираюших/оживающих клнток


    public Button styleButton; // переключение на режим с изображениями 
    public Button battleButton; // переключение на режим для двух игроков


    public Button generateButton; // генерация нового поля

    public Button randomButton; // генерация случайной расстановки на поле
    public Slider randomFillSlider; // изменение заполненности поля при генерации

    // поля для ввода

    public TMP_InputField widthInput; // ширина поля
    public TMP_InputField heightInput; // высота поля

    // меню фигур 

    public TMP_Dropdown patternDropdown;
    public int[,] currentPattern = null;
    public bool patternMode = false;

    // поля счета для режима на двух игроков

    public TextMeshProUGUI whiteScoreText;
    public TextMeshProUGUI blackScoreText;

    // процесс игры

    private bool isRunning = false;
    private Coroutine simulationCoroutine;

    // режимы

    private bool showFlashes = false;
    public bool showNeighbors = false;

    private bool imageMode = false;

    // для двух игроков
    private bool battleMode = false;
    private bool whiteTurn = true; // ход

    private bool battleSetup = false; // расстановка фишек
    private int whitePlaced = 0;
    private int blackPlaced = 0;
    private const int maxPlaced = 10;
    private bool battleRunning = false; // подсчет очков
    private Coroutine battleCoroutine;

    public bool BattleSetup => battleSetup;
    public bool WhiteTurn => whiteTurn;
    public int MaxPlaced => maxPlaced;

    public GridManager gridManager;




    void Start()
    {
        patternDropdown.captionText.text = "Figures"; 

        startPauseButton.onClick.AddListener(ToggleSimulation);
        clearButton.onClick.AddListener(ClearField);
        neighborsButton.onClick.AddListener(ToggleNeighbors);
        stepButton.onClick.AddListener(DoOneStep);
        randomButton.onClick.AddListener(GenerateRandom);
        flashButton.onClick.AddListener(ToggleFlashes);
        patternDropdown.onValueChanged.AddListener(OnPatternSelected);
        styleButton.onClick.AddListener(ToggleStyle);
        battleButton.onClick.AddListener(ToggleMode);
        generateButton.onClick.AddListener(OnGenerateClicked);

    }

    void UpdateGridManager()
    {
        gridManager.showFlashes = showFlashes;
        gridManager.battleModeActive = battleMode;
        gridManager.imageModeActive = imageMode;
    }


    public void RefreshNeighborView()
    {
        if (showNeighbors)
            UpdateNeighborDisplay();
    }



    // запуск процесса обновления поля
    IEnumerator RunSimulation()
    {
            while (isRunning)
            {
                UpdateGridManager();
                gridManager.NextGeneration();
                UpdateBattleScoreUI();
                RefreshNeighborView();
                yield return new WaitForSeconds(speedSlider.value);
            }
    }
    
    // остановка процесса обновления поля
    public void StopSimulation()
    {
        if (isRunning)
        {
            isRunning = false;
            if (simulationCoroutine != null)
            {
                StopCoroutine(simulationCoroutine);
                simulationCoroutine = null;
            }
            buttonLabel.text = "Start";
        }
    }
    
    // одна итерация обновления поля
    void DoOneStep()
    {
        if (!battleSetup && !battleRunning)
        {
            StopSimulation();
            UpdateGridManager();
            gridManager.NextGeneration();
            UpdateNeighborDisplay();
        }
    }
    // считывание размеров поля
    void OnGenerateClicked()
    {
        int w = int.Parse(widthInput.text);
        int h = int.Parse(heightInput.text);
        NewGrid(w, h);
    }

    // создание поля 
    public void NewGrid(int width, int height)
    {

        StopSimulation();
        gridManager.width = width;
        gridManager.height = height;
        UpdateGridManager();
        gridManager.GenerateGrid();
        RefreshNeighborView();
        RestartBattleMode();
    }
   
    // очищение поля

    void ClearField()
    {
        if (!battleRunning)
        {
            StopSimulation();
            UpdateGridManager();
            foreach (Transform child in gridManager.transform)
            {
                Cell cell = child.GetComponent<Cell>();
                if (cell != null)
                    cell.SetAlive(false, showFlashes);

            }
            RefreshNeighborView();
            RestartBattleMode();
        }
    }

    // создание рандомной расстановки на поле

    void GenerateRandom()
    {
        if (!battleMode && !battleRunning)
        {
            StopSimulation();
            UpdateGridManager();
            ClearField();
            float fillAmount = randomFillSlider != null ? randomFillSlider.value : 0.25f;
            gridManager.Randomize(fillAmount);
            RefreshNeighborView();
        }
    }

    // включение/выключение процесса обновления поля
    void ToggleSimulation()
    {
        if (!battleSetup)
        {
            if (isRunning)
            {
                StopSimulation();
            }
            else
            {
                isRunning = true;
                if (simulationCoroutine == null)
                    simulationCoroutine = StartCoroutine(RunSimulation());
                buttonLabel.text = "Pause";
            }
        }
    }
    
    // переключение режима flashes
    void ToggleFlashes()
    {
        if (!imageMode && !battleMode && !battleRunning)
        {
            showFlashes = !showFlashes;
            UpdateGridManager();
            flashButton.GetComponentInChildren<TextMeshProUGUI>().text =
            showFlashes ? "ON" : "OFF";
        }
    }

    // переключение режима neighbours
    void ToggleNeighbors()
    {
        if (!battleMode && !battleRunning)
        {
            StopSimulation();
            showNeighbors = !showNeighbors;
            neighborsButton.GetComponentInChildren<TextMeshProUGUI>().text =
                showNeighbors ? "Hide " : "Show";

            UpdateNeighborDisplay();
        }
    }
   
    // переключение режима двух игроков
    void ToggleMode()
    {
        if (!imageMode && !showFlashes && !showNeighbors && !battleRunning)
        {
            ClearField();
            battleMode = !battleMode;
            UpdateGridManager();
            RestartBattleMode();

            if (battleMode)
            {
                battleSetup = true;
                whiteTurn = true;
                whitePlaced = 0;
                blackPlaced = 0;
            }
            else
            {

                battleSetup = false;
            }

            battleButton.GetComponentInChildren<TextMeshProUGUI>().text =
                battleMode ? "Classic" : "Battle";

            foreach (Transform child in gridManager.transform)
            {
                Cell cell = child.GetComponent<Cell>();
                if (cell != null)
                    cell.SetMode(imageMode, battleMode);
            }
        }
    }
   
    // начало нового матча
    void RestartBattleMode()
    {
        if (battleMode)
        {
            gridManager.whiteScore = 0;
            gridManager.blackScore = 0;


            battleSetup = true;
            whiteTurn = true;
            whitePlaced = 0;
            blackPlaced = 0;
        }
        UpdateBattleScoreUI();
        
    }

    // расстановка фишек

    public void RegisterPlaced(Owner owner)
    {
        if (owner == Owner.White) whitePlaced++;
        else if (owner == Owner.Black) blackPlaced++;


        if (whitePlaced + blackPlaced < maxPlaced * 2)
        {
            whiteTurn = !whiteTurn;
        }
        else
        {
            EndBattleSetup();
        }
    }
    
    // этап набора очков в матче
    public void EndBattleSetup()
    {
        battleSetup = false;

        if (battleCoroutine != null)
            StopCoroutine(battleCoroutine);
        buttonLabel.text = "Pause";
        battleCoroutine = StartCoroutine(BattleMatchLoop());

    }

    private IEnumerator BattleMatchLoop()
    {
        battleRunning = true;

        isRunning = true;
        if (simulationCoroutine == null)
            simulationCoroutine = StartCoroutine(RunSimulation());

        yield return new WaitUntil(() => IsBattleOver());

        StopSimulation();

        battleRunning = false;
        buttonLabel.text = "Start";

        UpdateBattleScoreUI();

        yield return new WaitForSeconds(20f);

        RestartBattleMode();
    }


    private bool IsBattleOver()
    {
        if (!isRunning)
           return true;
        for (int x = 0; x < gridManager.width; x++)
            for (int y = 0; y < gridManager.height; y++)
                if (gridManager.GetCell(x, y).isAlive)
                    return false;
        return true;
    }


    // переключение режима с изображениями
    void ToggleStyle()
    {

        if (!battleMode && !showFlashes)
        {

            StopSimulation();
            imageMode = !imageMode;
            UpdateGridManager();
            UpdateNeighborDisplay();
            styleButton.GetComponentInChildren<TextMeshProUGUI>().text =
                imageMode ? "Classic" : "Froggie";


            foreach (Transform child in gridManager.transform)
            {
                Cell cell = child.GetComponent<Cell>();
                if (cell != null)
                    cell.SetMode(imageMode, battleMode);
            }
        }
       

    }

   // обновление счета в матче
   private void UpdateBattleScoreUI()
   {
            if (whiteScoreText == null || blackScoreText == null)
                return;

            int white = gridManager.whiteScore;
            int black = gridManager.blackScore;

            whiteScoreText.text = white.ToString();
            blackScoreText.text = black.ToString();

            if (white > black)
            {
                whiteScoreText.color = Color.red;
                blackScoreText.color = Color.white;
            }
            else if (black > white)
            {
                blackScoreText.color = Color.red;
                whiteScoreText.color = Color.white;
            }
            else
            {
                whiteScoreText.color = Color.white;
                blackScoreText.color = Color.white;
            }

    }


    public void UpdateNeighborDisplay()
    {
        if (!showNeighbors)
        {
            foreach (Transform child in gridManager.transform)
            {
                var cell = child.GetComponent<Cell>();
                if (cell != null)
                    cell.ShowNeighborCount(false, 0);
            }
            return;
        }

        for (int x = 0; x < gridManager.width; x++)
        {
            for (int y = 0; y < gridManager.height; y++)
            {
                var cell = gridManager.GetCell(x, y);
                int n = gridManager.CountAliveNeighbors(x, y);
                cell.ShowNeighborCount(true, n);
            }
        }
    }

    void OnPatternSelected(int index)
    {
        switch (index)
        {
            case 0: currentPattern = null; break;
            case 1: currentPattern = GridManager.Blinker; break;
            case 2: currentPattern = GridManager.Toad; break;
            case 3: currentPattern = GridManager.Beacon; break;
            case 4: currentPattern = GridManager.Glider; break;
            case 5: currentPattern = GridManager.Pulsar; break;
        }
        patternMode = currentPattern != null;
        patternDropdown.captionText.text = "Figures";

    }
}