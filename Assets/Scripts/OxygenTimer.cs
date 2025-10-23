using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class OxygenTimer : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI timerText;
    [SerializeField] PlayerHealth playerHealth; 
    private float timeLeft;
    private int suffocationDamage = 1;
    private float timeBetweenDamage = 0.05f;
    private float lastTimeDamaged = -Mathf.Infinity;

    void Start()
    {
        SetTimeLeft(90);
    }

    private void SetTimeLeft(float timeInSeconds)
    {
        timeLeft = timeInSeconds;

        if (timeLeft <= 0)
        {
            timeLeft = 0;

            TimerRanOut();
        }
    }

    private void RemoveTime(float timeInSeconds)
    {
        SetTimeLeft(timeLeft -= timeInSeconds);
    }

    private void AddTime(float timeInSeconds)
    {
        SetTimeLeft(timeLeft += timeInSeconds);
        
        if (timeLeft > 0)
        {
            timerText.color = Color.black;
        }
    }

    private void TimerRanOut()
    {
        timerText.color = new Color32(140, 0, 0, 255); // Dark Red

        if (Time.time - lastTimeDamaged >= timeBetweenDamage)
        {
            playerHealth.TakeDamage(suffocationDamage);
            lastTimeDamaged = Time.time;
        }
    }

    private void UpdateCounter()
    {
        int minutes = Mathf.FloorToInt(timeLeft / 60);
        int seconds = Mathf.FloorToInt(timeLeft % 60);
        float milliseconds = (timeLeft % 1) * 1000;

        if (timeLeft < 60)
        {
            timerText.text = string.Format("{0:00}.{1:00}", seconds, Mathf.FloorToInt(milliseconds / 10));
        }
        else
        {
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }

    void Update()
    {
        RemoveTime(Time.deltaTime);

        UpdateCounter();
    }
}
