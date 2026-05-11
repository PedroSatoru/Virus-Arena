using UnityEngine;

/// <summary>
/// Mantém as decisões do jogador (como Power UPs) persistentes
/// entre carregamentos de cena.
/// </summary>
public static class GlobalState
{
    public static bool hasTripleShot = false;
    public static bool hasSpeedBoost = false;
    public static bool hasHeartAndBodyHP = false;

    public static int currentPhase = 1;

    /// <summary>
    /// Fase cujo órgão foi perdido (0 = nenhum, 1 = pulmão, 2 = coração, 3 = cérebro).
    /// </summary>
    public static int organLostPhase = 0;

    // Vida passada de uma cena para outra
    public static float savedBodyHP = -1f;
    public static int savedPlayerHearts = -1;

    // ============================================================
    // MODO INFINITO
    // ============================================================
    /// <summary>
    /// Flag que indica que a cena atual é uma run de Modo Infinito.
    /// </summary>
    public static bool isInfiniteMode = false;

    /// <summary>
    /// Fase selecionada para o modo infinito (1, 2 ou 3).
    /// </summary>
    public static int infinitePhase = 1;

    /// <summary>
    /// Controles de uso único no modo infinito.
    /// Triple Shot e Speed Boost só podem ser escolhidos 1x por run.
    /// Vida Extra não tem limite.
    /// </summary>
    public static bool infiniteTripleShotUsed = false;
    public static bool infiniteSpeedBoostUsed = false;

    public static void ResetState()
    {
        hasTripleShot = false;
        hasSpeedBoost = false;
        hasHeartAndBodyHP = false;
        currentPhase = 1;
        organLostPhase = 0;
        savedBodyHP = -1f;
        savedPlayerHearts = -1;
    }

    /// <summary>
    /// Reinicia apenas os dados de uma run de modo infinito,
    /// preservando os records salvos em PlayerPrefs.
    /// </summary>
    public static void ResetInfiniteRun()
    {
        hasTripleShot = false;
        hasSpeedBoost = false;
        hasHeartAndBodyHP = false;
        organLostPhase = 0;
        savedBodyHP = -1f;
        savedPlayerHearts = -1;
        infiniteTripleShotUsed = false;
        infiniteSpeedBoostUsed = false;
    }
}
