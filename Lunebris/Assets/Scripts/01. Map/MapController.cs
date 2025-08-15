// System
using System.Collections;
using System.Collections.Generic;

// Unity
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class MapController : MonoBehaviour
{
    [SerializeField] private Material material;

    [SerializeField] private Slider balanceSlider;

    // Define Color
    private Color black = new Color(0.179f, 0.179f, 0.179f);
    private Color white = new Color(0.9f, 0.9f, 0.9f);

    [SerializeField] private ElementType currentType;

    [Header("변환 시간")]
    [SerializeField] private float convertDuration = 1f;

    [Header("변환 쿨타임")]
    [SerializeField] private float convertCoolTime = 3f;

    [Header("속성 최대 유지 가능 시간")]
    [SerializeField] private float eroisnTime = 30f;

    [SerializeField] private GameObject[] luxSkillInterfaces;

    [SerializeField] private GameObject[] tenebrisSkillInterfaces;

    [SerializeField] private Player.SkillCaster passiveCaster;

    [Header("플레이어 스킨 변경 변수")]
    [SerializeField] private Material[] materials;
    [SerializeField] private SkinnedMeshRenderer skin;

    private bool canConvert = true;

    private void Awake()
    {
        // 어둠으로 초기화
        material.color = black;
        skin.material = materials[1];
        UpdateSkillInterface();
        currentType = ElementType.Tenebris;
    }

    private void Update()
    {
        UpdateBalanceSilder();
    }

    private void UpdateBalanceSilder()
    {
        if(GetCurrentAttribute() == ElementType.Lux)
        {
            balanceSlider.value -= 1 / eroisnTime * Time.deltaTime;
        }
        else
        {
            balanceSlider.value += 1 / eroisnTime * Time.deltaTime;
        }
    }

    /// <summary>
    /// Convert Map Method
    /// </summary>
    public void ConvertMap()
    {
        if (canConvert)
        {
            if (currentType == ElementType.Lux)
            {
                canConvert = false;
                StartCoroutine(ConvertMapCoroutine(white, black));  // W2B
                currentType = ElementType.Lux;
                UpdateSkillInterface();
                skin.material = materials[1]; // black skin
                Debug.Log(GetCurrentAttribute());
            }
            else
            {
                canConvert = false;
                StartCoroutine(ConvertMapCoroutine(black, white));  // B2W
                currentType = ElementType.Tenebris;
                UpdateSkillInterface();
                skin.material = materials[0]; // white skin
                Debug.Log(GetCurrentAttribute());
            }
        }
    }

    private IEnumerator ConvertMapCoroutine(Color _startColor, Color _endColor)
    {
        float elapsedTime = 0f;

        while (elapsedTime < convertDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / convertDuration;
            material.color = Color.Lerp(_startColor, _endColor, t);
            yield return null;      // Skip 1 Frame
        }

        yield return new WaitForSeconds(convertCoolTime);

        canConvert = true;
    }
    
    public ElementType GetCurrentAttribute()
    {
        if (currentType == ElementType.Lux) return ElementType.Lux;
        else return ElementType.Tenebris;
    }

    private void UpdateSkillInterface()
    {
        int index = 0;

        if (currentType == ElementType.Lux)
        {
            foreach(GameObject tenebris in tenebrisSkillInterfaces)
            {
                tenebris.transform.SetSiblingIndex(index);
                index++;
            }
            foreach (GameObject lux in luxSkillInterfaces)
            {
                lux.transform.SetSiblingIndex(index);
                index++;
            }
        }
        else
        {
            foreach (GameObject lux in luxSkillInterfaces)
            {
                lux.transform.SetSiblingIndex(index);
                index++;
            }
            foreach (GameObject tenebris in tenebrisSkillInterfaces)
            {
                tenebris.transform.SetSiblingIndex(index);
                index++;
            }
        }
    }
}
