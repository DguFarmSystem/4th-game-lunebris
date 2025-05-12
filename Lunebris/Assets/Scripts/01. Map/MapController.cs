// System
using System.Collections;
using System.Collections.Generic;

// Unity
using UnityEngine;

[DisallowMultipleComponent]
public class MapController : MonoBehaviour
{
    [Header("변경할 메테리얼")]
    [SerializeField] private Material material;

    // Define Color
    private Color black = new Color(0.179f, 0.179f, 0.179f);
    private Color white = new Color(0.9f, 0.9f, 0.9f);

    private bool isWhite = false;
    private static readonly IReadOnlyList<string> state = new List<string> { "lux", "tenebris" };

    [SerializeField] private float convertDuration = 1f;
    private bool canConvert = true;

    private void Awake()
    {
        material.color = black;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftControl) && canConvert)
        {
            ConvertMap();
        }
    }

    private void ConvertMap()
    {
        if (isWhite)
        {
            canConvert = false;
            StartCoroutine(ConvertMapCoroutine(white, black));  // W2B
            isWhite = false;
            Debug.Log(GetCurrentState());
        }
        else
        {
            canConvert = false;
            StartCoroutine(ConvertMapCoroutine(black, white));  // B2W
            isWhite = true;
            Debug.Log(GetCurrentState());
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

        canConvert = true;
    }
    
    public string GetCurrentState()
    {
        if (isWhite) return state[0];
        else return state[1];
    }
}
