using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

using UnityEngine.UI;

public enum Owner
{
    None,
    White,
    Black
}

public class Cell : MonoBehaviour, IPointerClickHandler
{
    public bool isAlive = false; 
    private SpriteRenderer sr;

    // для режима двух игроков
    public Owner owner = Owner.None; 
    private bool battleMode = false;
 
    // для режима flashes
    private float highlightTime = 0f;
    private Color highlightColor;
    private static readonly float flashDuration = 0.1f;

    private TextMeshPro text; // для режима neighbours
 
    // для режима с изображениями
    private bool useImages = false;
    public Sprite[] aliveSprites;
    public Sprite deadSprite;
    public Sprite baseSprite;
    private SpriteRenderer overlayRenderer;
    private Sprite currentAliveSprite;

    private GameController gameController;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        overlayRenderer = transform.Find("Overlay")?.GetComponent<SpriteRenderer>();
        text = GetComponentInChildren<TextMeshPro>();
        gameController = FindFirstObjectByType<GameController>();
        UpdateVisual();
        ShowNeighborCount(false, 0);
    }

    private void Update()
    {
        if (highlightTime > 0f)
        {
            highlightTime -= Time.deltaTime;
            sr.color = Color.Lerp(highlightColor, isAlive ? new Color(0, 11f, 0.81f, 0.19f) : Color.gray, 1f - highlightTime / flashDuration);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (gameController == null)
            return;

        gameController.StopSimulation();


        if (gameController.gridManager.battleModeActive && gameController.BattleSetup)
        {
            if (!isAlive)
            {
                var owner = gameController.WhiteTurn ? Owner.White : Owner.Black;
                SetAlive(true, false, owner);

                gameController.RegisterPlaced(owner);
            }
            return;
        }

        if (gameController.patternMode)
        {

            var pattern = gameController.currentPattern;
            if (pattern != null)
            {
                Vector3 cellPos = transform.localPosition;
                int gx = Mathf.RoundToInt(cellPos.x / gameController.gridManager.cellSize);
                int gy = Mathf.RoundToInt(cellPos.y / gameController.gridManager.cellSize);

                gameController.gridManager.PlacePattern(pattern, gx, gy);
                gameController.patternDropdown.value = 0;
                gameController.currentPattern = null;
                gameController.patternMode = false;

            }
        }
        else
        {

            isAlive = !isAlive;
            UpdateVisual();
        }
        if (gameController.showNeighbors)
            gameController.UpdateNeighborDisplay();
    }

    public void SetAlive(bool alive, bool allowFlash = false, Owner newOwner = Owner.None)
    {
        if (isAlive != alive && allowFlash)
        {
            highlightTime = flashDuration;
            highlightColor = alive ? Color.yellow : new Color(0.5f, 0f, 0f);
        }

        if (!isAlive && alive && aliveSprites != null && aliveSprites.Length > 0)
        {
            currentAliveSprite = aliveSprites[Random.Range(0, aliveSprites.Length)];
        }

        if (battleMode && !isAlive && alive && newOwner != Owner.None)
            owner = newOwner;

        isAlive = alive;
        UpdateVisual();
    }

    private void UpdateVisual()
    {

        if (battleMode)
        {
            if (!isAlive)
            {
                sr.sprite = baseSprite;
                sr.color = Color.grey;
            }
            else
            {
                sr.sprite = baseSprite;
                sr.color = owner == Owner.White ? Color.white : Color.black;
            }
            return;
        }


        if (useImages)
        {
            sr.color = Color.white;
            sr.sprite = deadSprite;
            if (isAlive)
            {

                if (currentAliveSprite == null && aliveSprites != null && aliveSprites.Length > 0)
                    currentAliveSprite = aliveSprites[Random.Range(0, aliveSprites.Length)];

                overlayRenderer.sprite = currentAliveSprite;
            }
            else
            {
                currentAliveSprite = null;
                overlayRenderer.sprite = null;

            }

        }
        else
        {
            overlayRenderer.sprite = null;
            sr.sprite = baseSprite;
            sr.color = isAlive ? new Color(0, 11f, 0.81f, 0.19f) : Color.gray;
        }
    }


    public void ShowNeighborCount(bool visible, int n)
    {
        if (text == null) return;
        text.text = visible ? n.ToString() : "";
        if (visible)
        {
            if (n <= 1) text.color = new Color(0.1f, 0.25f, 0.81f);
            else if (n == 2) text.color = new Color(0.81f, 0.77f, 0.11f);
            else if (n == 3) text.color = new Color(0.03f, 0.32f, 0.01f);
            else text.color = Color.red;
        }
    }

    public void SetMode(bool enabled, bool battle = false)
    {
        useImages = enabled;
        battleMode = battle;
        UpdateVisual();
    }
}
