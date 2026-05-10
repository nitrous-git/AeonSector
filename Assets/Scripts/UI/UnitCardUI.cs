using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UnitCardUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private TMP_Text enText;
    [SerializeField] private Image iconImage;
    [SerializeField] private Slider hpSlider;
    [SerializeField] private Slider energySlider;

    private CombatUnit currentUnit;

    private const float SliderMaxValue = 10f;

    private void Awake()
    {
        Hide();
    }

    public void Show(CombatUnit unit)
    {
        currentUnit = unit;

        if (unit == null || !unit.IsAlive || unit.Stats == null)
        {
            Hide();
            return;
        }

        gameObject.SetActive(true);

        UnitStats stats = unit.Stats;

        float hpPercent = stats.MaxHP > 0
            ? Mathf.Clamp01((float)unit.CurrentHP / stats.MaxHP)
            : 0f;

        float energyPercent = CalculateEnergy(unit);

        int displayCurrentHP = Mathf.RoundToInt(hpPercent * stats.DisplayMaxHP);
        int displayCurrentEN = Mathf.RoundToInt(energyPercent * stats.DisplayMaxEN);

        if (nameText != null)
        {
            nameText.text = !string.IsNullOrWhiteSpace(stats.UnitName)
                ? stats.UnitName
                : unit.name;
        }

        if (iconImage != null)
        {
            iconImage.sprite = stats.UnitIcon;
            iconImage.enabled = stats.UnitIcon != null;
        }

        if (hpText != null)
        {
            hpText.text = $"{displayCurrentHP}/{stats.DisplayMaxHP}";
        }

        if (enText != null)
        {
            enText.text = $"{displayCurrentEN}/{stats.DisplayMaxEN}";
        }

        if (hpSlider != null)
        {
            hpSlider.minValue = 0f;
            hpSlider.maxValue = SliderMaxValue;
            hpSlider.value = hpPercent * SliderMaxValue;
        }

        if (energySlider != null)
        {
            energySlider.minValue = 0f;
            energySlider.maxValue = SliderMaxValue;
            energySlider.value = energyPercent * SliderMaxValue;
        }
    }

    public void Refresh()
    {
        Show(currentUnit);
    }

    public void Hide()
    {
        currentUnit = null;
        gameObject.SetActive(false);
    }

    private float CalculateEnergy(CombatUnit unit)
    {
        UnitStats stats = unit.Stats;

        float hpPercent = stats.MaxHP > 0
            ? Mathf.Clamp01((float)unit.CurrentHP / stats.MaxHP)
            : 0f;

        float energy = 0f;

        // 70% comes from HP.
        energy += hpPercent * 0.70f;

        // 15% if the unit can still move.
        if (unit.CanMove)
            energy += 0.15f;

        // 15% if the unit can still attack.
        if (unit.CanAttack)
            energy += 0.15f;

        return Mathf.Clamp01(energy);
    }
}