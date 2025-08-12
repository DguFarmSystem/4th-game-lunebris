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

    private bool isWhite = false;
    private static readonly IReadOnlyList<string> attribute = new List<string> { "lux", "tenebris" };

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
        material.color = black;
        skin.material = materials[1];
        UpdateSkillInterface();
    }

    private void Update()
    {
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
    public void ConvertMap()
    {
        if (canConvert)
        {
            if (isWhite)
            {
                canConvert = false;
                StartCoroutine(ConvertMapCoroutine(white, black));  // W2B
                isWhite = false;
                UpdateSkillInterface();
                skin.material = materials[1]; // black skin
                Debug.Log(GetCurrentAttribute());
            }
            else
            {
                canConvert = false;
                StartCoroutine(ConvertMapCoroutine(black, white));  // B2W
                isWhite = true;
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
