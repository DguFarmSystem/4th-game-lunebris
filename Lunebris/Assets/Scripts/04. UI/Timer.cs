// Unity
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
public class Timer : MonoBehaviour
{
    [Header("Time Limit(Minutes)")]
    [SerializeField] private float timeLimit;

    [Header("Timer TMP")]
    [SerializeField] private TextMeshProUGUI timerTMP;

    [Header("Phase Management")]
    [SerializeField] private float phaseConvertTime; // Minute
    [SerializeField] private EnemySpawner enemySpawner;

    // Timer Variables
    private float timer;
    public float ElapsedTime { get; private set; }
    private bool isStop;

    private void Start()
    {
        timeLimit *= 60;    // Convert Minutes to Second
        timer = timeLimit;  // Init Timer
    }

    private void Update()
    {
        UpdateTimer();  // Update Timer
    }

    private void UpdateTimer()
    {
        if (isStop) return;

        if (timer > 0f)
        {
            timer -= Time.deltaTime;
            ElapsedTime += Time.deltaTime;

            UpdateTimerTMP();
        }
        else if (timer <= 0f)
        {
            isStop = true;
            timer = 0f;
        }
    }

    /// <summary>
    /// Update TMP
    /// </summary>
    private void UpdateTimerTMP()
    {
        int minutes = Mathf.FloorToInt(timer / 60f);
        int seconds = Mathf.FloorToInt(timer % 60f);
        string formattedTime = string.Format("{0:00}:{1:00}", minutes, seconds);
        timerTMP.text = formattedTime;
    }
}
