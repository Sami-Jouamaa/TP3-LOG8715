using Unity.Netcode;
using UnityEngine;

public class CircleGhost : NetworkBehaviour
{
    [SerializeField]
    private MovingCircle m_MovingCircle;

    private Vector2 m_PredictedPosition;

    public override void OnNetworkSpawn()
    {
        if (!IsServer && m_MovingCircle != null)
        {
            m_PredictedPosition = m_MovingCircle.Position;
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

        m_PredictedPosition += velocity * Time.fixedDeltaTime;

        float error = (serverPos - m_PredictedPosition).sqrMagnitude;

        const float threshold = 0.5f;

        if (error > threshold * threshold)
        {
            m_PredictedPosition = serverPos;
        }

        transform.position = m_PredictedPosition;
    }
}