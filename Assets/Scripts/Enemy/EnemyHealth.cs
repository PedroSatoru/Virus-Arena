using UnityEngine;
using System;

/// <summary>
/// Gerencia a vida dos inimigos.
/// Na Fase 1: todos os inimigos têm 1 HP.
/// </summary>
public class EnemyHealth : MonoBehaviour
{
    [Header("Vida")]
    public int maxHP = 1; // Fase 1: 1 hit kill
    private int currentHP;

    [Header("Feedback Visual")]
    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    public event Action OnEnemyDeath;

    void Awake()
    {
        currentHP = maxHP;
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
    }

    public void TakeDamage(int damage = 1)
    {
        currentHP -= damage;
        
        if (spriteRenderer != null)
        {
            // Flash branco ao levar dano
            StartCoroutine(DamageFlash());
        }

        if (currentHP <= 0)
        {
            Die();
        }
    }

    System.Collections.IEnumerator DamageFlash()
    {
        if (spriteRenderer != null)
            spriteRenderer.color = Color.white;
        yield return new WaitForSeconds(0.1f);
        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;
    }

    void Die()
    {
        OnEnemyDeath?.Invoke();
        
        // Efeito simples de morte: escalar para zero
        StartCoroutine(DeathEffect());
    }

    System.Collections.IEnumerator DeathEffect()
    {
        float duration = 0.3f;
        float elapsed = 0f;
        Vector3 originalScale = transform.localScale;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            transform.localScale = originalScale * (1f - t);
            if (spriteRenderer != null)
                spriteRenderer.color = new Color(1f, 1f, 0f, 1f - t); // Fade amarelo
            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }
}
