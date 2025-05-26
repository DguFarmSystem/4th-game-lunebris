// Unity
using UnityEngine;

[DisallowMultipleComponent]
public class Enemy : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Attack")) return;

        other.gameObject.SetActive(false);

        Death();
    }

    private void Death()
    {
        gameObject.SetActive(false);
        Player.Player player = FindObjectOfType<Player.Player>();

        player.IncreaseXP(10);
    }
}
