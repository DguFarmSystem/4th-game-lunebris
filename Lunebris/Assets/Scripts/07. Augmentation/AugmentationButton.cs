// Unity
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
public class AugmentationButton : MonoBehaviour
{
    private Button button;
    private TextMeshProUGUI tmp;
    private string grade;

    private void Start()
    {
        button = GetComponent<Button>();
        tmp = GetComponentInChildren<TextMeshProUGUI>();
    }
}
