using System;
using TMPro;
using UnityEngine;

public class TurnMenuUI : MonoBehaviour
{
    [SerializeField] private TMP_Text turnText;

    public void UpdateTurnText(int currentTurnText, int maxTurnText)
    {
        currentTurnText = Math.Min(currentTurnText, maxTurnText);
        turnText.text = $"{currentTurnText:00}/{maxTurnText:00}";
    }
}
