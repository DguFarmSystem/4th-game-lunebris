// System
using System.Collections;
using System.Collections.Generic;

// Unity
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class EnemyKillDetector : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Slider luxPowerSlider;
    [SerializeField] private Slider tenePowerSlider;

    [Header("Power 관련")]
    [SerializeField] private int currentLuxPower;
    [SerializeField] private int currentTenePower;
    [SerializeField] private int maxLuxPower = 100;
    [SerializeField] private int maxTenePower = 100;

    [Header("테스트용")]
    [SerializeField] private int increment;

    [Header("상호작용 관련")]
    [SerializeField] private AugmentationUI augmentation;

    public void UpdateKillPower(ElementType _type)
    {
        if (_type == ElementType.Lux) IncreaseLuxPower();
        else if (_type == ElementType.Tenebris) IncreaseTenePower();
    }

    public void IncreaseLuxPower()
    {
        currentLuxPower += increment;

        if (currentLuxPower >= maxLuxPower)
        {
            augmentation.OpenAugmentationUI();
            currentLuxPower = currentLuxPower - maxLuxPower;
        }

        luxPowerSlider.value = (float)currentLuxPower / (float)maxLuxPower;

        Debug.Log("빛 에너지 증가");
    }

    public void IncreaseTenePower()
    {
        currentTenePower += increment;

        if (currentTenePower >= maxTenePower)
        {
            augmentation.OpenAugmentationUI();
            currentTenePower = currentTenePower - maxTenePower;
        }

        tenePowerSlider.value = (float)currentTenePower / (float)maxTenePower;

        Debug.Log("어둠 에너지 증가");
    }
}
