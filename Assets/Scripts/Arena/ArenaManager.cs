using UnityEngine;
using System.Collections;

/// <summary>
/// Gerencia a arena (cenário) e seus hitboxes.
/// Recebe dano de projéteis Anti-Corpo e notifica o GameManager.
/// Fase 1: apenas chão recebe dano.
/// </summary>
public class ArenaManager : MonoBehaviour
{
    [Header("Referências")]
    public SpriteRenderer floorRenderer;
    public SpriteRenderer leftWallRenderer;
    public SpriteRenderer rightWallRenderer;
    public SpriteRenderer ceilingRenderer;

    [Header("Feedback")]
    public float flashDuration = 0.2f;

    private Color floorOriginalColor;
    private Color wallOriginalColor;
    private Color ceilingOriginalColor;

    void Start()
    {
        if (floorRenderer != null) floorOriginalColor = floorRenderer.color;
        if (leftWallRenderer != null) wallOriginalColor = leftWallRenderer.color;
        if (ceilingRenderer != null) ceilingOriginalColor = ceilingRenderer.color;
    }

    /// <summary>
    /// Chamado quando um projétil Anti-Corpo atinge a arena.
    /// </summary>
    public void TakeArenaDamage(int damage)
    {
        GameManager gm = GameManager.Instance;
        if (gm != null)
        {
            gm.ApplyOrganDamage(damage * 10f); // Cada hit = 10 de dano base
        }

        // Flash vermelho no chão
        if (floorRenderer != null)
        {
            StartCoroutine(FlashColor(floorRenderer, floorOriginalColor));
        }
    }

    /// <summary>
    /// Feedback visual: piscar vermelho na superfície atingida.
    /// </summary>
    IEnumerator FlashColor(SpriteRenderer sr, Color originalColor)
    {
        sr.color = Color.red;
        yield return new WaitForSeconds(flashDuration);
        sr.color = originalColor;
    }
}
