using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// Gerencia a vida do Player (Glóbulo Branco).
/// 5 corações, piscar vermelho ao levar dano, invulnerabilidade temporária.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("Vida")]
    public int maxHearts = 5;
    public int currentHearts;

    [Header("Invulnerabilidade")]
    public float invulnerabilityDuration = 1f;
    private bool isInvulnerable = false;

    [Header("Feedback Visual")]
    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    // Eventos
    public event Action<int> OnHealthChanged;
    public event Action OnPlayerDeath;

    void Awake()
    {
        currentHearts = maxHearts;
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
    }

    /// <summary>
    /// Aplicar dano ao player. Apenas projéteis Anti-Player (roxo) causam dano.
    /// </summary>
    public void TakeDamage(int damage = 1)
    {
        if (isInvulnerable) return;

        currentHearts -= damage;
        currentHearts = Mathf.Max(0, currentHearts);

        OnHealthChanged?.Invoke(currentHearts);

        if (currentHearts <= 0)
        {
            OnPlayerDeath?.Invoke();
            return;
        }

        // Feedback visual e invulnerabilidade
        StartCoroutine(InvulnerabilityCoroutine());
    }

    IEnumerator InvulnerabilityCoroutine()
    {
        isInvulnerable = true;
        
        // Piscar vermelho
        float elapsed = 0f;
        while (elapsed < invulnerabilityDuration)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = (Mathf.FloorToInt(elapsed * 10) % 2 == 0) 
                    ? Color.red 
                    : new Color(1f, 1f, 1f, 0.3f);
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;
        
        isInvulnerable = false;
    }

    public bool IsInvulnerable()
    {
        return isInvulnerable;
    }
}
