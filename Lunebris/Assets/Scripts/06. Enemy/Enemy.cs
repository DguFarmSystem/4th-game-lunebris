// Unity
using UnityEngine;

[DisallowMultipleComponent]
public class Enemy : MonoBehaviour
{
    [Range(0,1), Header("Moster's Attribute")]
    [SerializeField] private int attribute;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Attack")) return;

        other.gameObject.SetActive(false);

        Death();
    }

    public void Death()
    {
        gameObject.SetActive(false);
        Player.Player player = FindObjectOfType<Player.Player>();

        player.IncreaseXP(10);
    }
}
