using Unity.Netcode;
using UnityEngine;

public class CircleGhost : NetworkBehaviour
{
    [SerializeField]
    private MovingCircle m_MovingCircle;

    private Vector2 m_PredictedPosition;

    private float m_Delay;

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            m_PredictedPosition = m_MovingCircle.Position;
            var gameState = FindAnyObjectByType<GameState>();
            m_Delay = gameState.CurrentRTT * 0.5f;
        }
    }

    private void Update()
    {
        if (IsServer)
        {
            transform.position = m_MovingCircle.Position;
            return;
        }

        Vector2 serverPos = m_MovingCircle.Position;
        Vector2 velocity = m_MovingCircle.Velocity;

        Vector2 predicted = serverPos + velocity * m_Delay;

        float error = (predicted - m_PredictedPosition).sqrMagnitude;
        if (error > 0.25f)
        {
            m_PredictedPosition = predicted;
        }
        else
        {
            m_PredictedPosition = Vector2.Lerp(m_PredictedPosition, predicted, 0.2f);
        }

        transform.position = m_PredictedPosition;
    }
}