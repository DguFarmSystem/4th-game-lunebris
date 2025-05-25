// System
using System.Collections;
using System.Collections.Generic;

// Unity
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class MapController : MonoBehaviour
{
    [Header("변경할 메테리얼")]
    [SerializeField] private Material material;

    [Header("속성 밸런스 슬라이더")]
    [SerializeField] private Slider balanceSlider;

    // Define Color
    private Color black = new Color(0.179f, 0.179f, 0.179f);
    private Color white = new Color(0.9f, 0.9f, 0.9f);

    private bool isWhite = false;
    private static readonly IReadOnlyList<string> attribute = new List<string> { "lux", "tenebris" };

    [Header("전환 기간")]
    [SerializeField] private float convertDuration = 1f;

    [Header("전환 쿨타임")]
    [SerializeField] private float convertCoolTime = 3f;

    [Header("잠식에 걸리는 총 시간")]
    [SerializeField] private float eroisnTime = 30f;

    [Header("Lux 스킬 인터페이스 배열")]
    [SerializeField] private GameObject[] luxSkillInterfaces;

    [Header("Tenebris 스킬 인터페이스 배열")]
    [SerializeField] private GameObject[] tenebrisSkillInterfaces;

    private bool canConvert = true;

    private void Awake()
    {
        material.color = black;
        UpdateSkillInterface();
    }

    private void Update()
    {
        ConvertMap();
        UpdateBalanceSilder();
    }

    private void UpdateBalanceSilder()
    {
        if(GetCurrentAttribute() == "lux")
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
    private void ConvertMap()
    {
        if (Input.GetKeyDown(KeyCode.LeftControl) && canConvert)
        {
            if (isWhite)
            {
                canConvert = false;
                StartCoroutine(ConvertMapCoroutine(white, black));  // W2B
                isWhite = false;
                UpdateSkillInterface();
                Debug.Log(GetCurrentAttribute());
            }
            else
            {
                canConvert = false;
                StartCoroutine(ConvertMapCoroutine(black, white));  // B2W
                isWhite = true;
                UpdateSkillInterface();
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
    
    public string GetCurrentAttribute()
    {
        if (isWhite) return attribute[0];
        else return attribute[1];
    }

    private void UpdateSkillInterface()
    {
        int index = 0;

        if (isWhite)
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
