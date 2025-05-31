// Unity
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
public class Timer : MonoBehaviour
{
    [Header("ÃÑ ½Ã°£(ºÐ)")]
    [SerializeField] private float time;

    [Header("Timer TMP")]
    [SerializeField] private TextMeshProUGUI timerTMP;

    private float timer;
    private bool isStop;

    private void Start()
    {
        time *= 60;
        timer = time;
    }

    private void Update()
    {
        UpdateTimer();
    }

    private void UpdateTimer()
    {
        if (isStop) return;

        if (timer > 0f)
        {
            timer -= Time.deltaTime;
            UpdateTimerTMP();
        }
        else if (timer <= 0f)
        {
            isStop = true;
            timer = 0f;
        }
    }

    private void UpdateTimerTMP()
    {
        int minutes = Mathf.FloorToInt(timer / 60f);
        int seconds = Mathf.FloorToInt(timer % 60f);
        string formattedTime = string.Format("{0:00}:{1:00}", minutes, seconds);
        timerTMP.text = formattedTime;
    }
}
