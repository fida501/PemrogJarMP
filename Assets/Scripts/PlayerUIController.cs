using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerUIController : MonoBehaviour
{
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI speedText;

    public void UpdateTimer()
    {
        timerText.text = GameManager.instance.gameTimer.ToString("F2");
    }

    public void UpdateSpeed(float speed)
    {
        if (speed < 0)
        {
            speed = speed * -1;
        }
        speedText.text = speed.ToString("F2");
    }
}