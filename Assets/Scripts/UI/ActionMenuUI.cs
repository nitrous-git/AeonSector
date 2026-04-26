using UnityEngine;
using UnityEngine.UI;

public class ActionMenuUI : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button moveButton;
    [SerializeField] private Button rangedButton;
    [SerializeField] private Button meleeButton;
    [SerializeField] private Button endTurnButton;

    public void Refresh(CombatUnit selectedUnit)
    {
        bool hasUnit = selectedUnit != null;

        moveButton.interactable = hasUnit && selectedUnit.CanMove;
        rangedButton.interactable = hasUnit && selectedUnit.CanAttack;
        meleeButton.interactable = hasUnit && selectedUnit.CanAttack;
        endTurnButton.interactable = hasUnit;
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
