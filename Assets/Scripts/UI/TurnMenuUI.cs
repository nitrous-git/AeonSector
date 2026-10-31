using System;
using TMPro;
using UnityEngine;

public class TurnMenuUI : MonoBehaviour
{
    [SerializeField] private TMP_Text turnText;

    public void UpdateTurnText(int currentTurnText, int maxTurnText)
    {
        if (turnText != null)
            turnText.text = $"{currentTurnText:00}";
    }
}
