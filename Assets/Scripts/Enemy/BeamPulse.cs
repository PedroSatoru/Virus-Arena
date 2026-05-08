using UnityEngine;

/// <summary>
/// Efeito visual de pulsação nos feixes indicadores do Boss.
/// Faz o LineRenderer pulsar a opacidade para alertar o jogador.
/// </summary>
public class BeamPulse : MonoBehaviour
{
    private LineRenderer lr;
    private Color baseStart;
    private Color baseEnd;

    void Start()
    {
        lr = GetComponent<LineRenderer>();
        if (lr != null)
        {
            baseStart = lr.startColor;
            baseEnd = lr.endColor;
        }
    }

    void Update()
    {
        if (lr == null) return;
        float pulse = 0.3f + Mathf.PingPong(Time.time * 3f, 0.5f);
        Color c1 = baseStart; c1.a = pulse;
        Color c2 = baseEnd; c2.a = pulse * 0.5f;
        lr.startColor = c1;
        lr.endColor = c2;
    }
}
