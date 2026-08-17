using UnityEngine;
using TMPro;

public class ScoreBarController : MonoBehaviour
{
    public static ScoreBarController Instance { get; private set; }

    [Header("UI Text References")]
    public TextMeshProUGUI itemsText;
    public TextMeshProUGUI deathsText;
    public TextMeshProUGUI timerText;

    private int itemsCollected = 0;
    private int deathCount = 0;
    private float elapsedTime = 0f;
    private bool isTimerRunning = true;

    void Awake()
    {
        Instance = this;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Set up the initial text values
        UpdateUI();
        
    }

    // Update is called once per frame
    void Update()
    {
        if (isTimerRunning)
        {
            // Add the time passed since the last frame
            elapsedTime += Time.deltaTime;
            UpdateTimerDisplay();
        }
        
    }
     // Call this function when the player grabs an item
    public void AddItem()
    {
        itemsCollected++;
        UpdateUI();
    }

    // Call this function when the player dies
    public void AddDeath()
    {
        deathCount++;
        UpdateUI();
    }

    // Call this function when the player hits the finish line
    public void StopTimer()
    {
        isTimerRunning = false;
    }

    void UpdateUI()
    {
        itemsText.text = "Items: " + itemsCollected;
        deathsText.text = "Deaths: " + deathCount;
    }

    void UpdateTimerDisplay()
    {
        // Turn the raw seconds into minutes and seconds format (00:00)
        int minutes = Mathf.FloorToInt(elapsedTime / 60);
        int seconds = Mathf.FloorToInt(elapsedTime % 60);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }
}
