using UnityEngine;

/// <summary>
/// Plataforma que se move verticalmente (sobe e desce) de forma contínua.
/// Cada instância pode ter velocidade e amplitude diferentes para criar
/// movimentação dessincronizada entre plataformas.
/// </summary>
public class MovingPlatform : MonoBehaviour
{
    [Header("Movimento Vertical")]
    public float speed = 1f;           // Velocidade da oscilação
    public float amplitude = 1.5f;     // Distância máxima acima/abaixo do ponto inicial
    public float phaseOffset = 0f;     // Offset para dessincronizar plataformas

    private float startY;

    void Start()
    {
        startY = transform.position.y;
    }

    void Update()
    {
        float newY = startY + Mathf.Sin((Time.time * speed) + phaseOffset) * amplitude;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }
}
