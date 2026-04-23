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

    // Vida passada de uma cena para outra
    public static float savedBodyHP = -1f;
    public static int savedPlayerHearts = -1;

    public static void ResetState()
    {
        hasTripleShot = false;
        hasSpeedBoost = false;
        hasHeartAndBodyHP = false;
        currentPhase = 1;
        savedBodyHP = -1f;
        savedPlayerHearts = -1;
    }
}
