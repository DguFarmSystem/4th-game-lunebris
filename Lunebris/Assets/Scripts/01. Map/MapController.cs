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

    [SerializeField] private float convertDuration = 1f;

    [SerializeField] private float convertCoolTime = 3f;

    [SerializeField] private float eroisnTime = 30f;

    [SerializeField] private GameObject[] luxSkillInterfaces;

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
