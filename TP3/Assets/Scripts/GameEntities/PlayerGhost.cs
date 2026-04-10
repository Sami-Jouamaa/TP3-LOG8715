using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerGhost : NetworkBehaviour
{
    [SerializeField] 
    private Player m_Player;
    [SerializeField] 
    private SpriteRenderer m_SpriteRenderer;

    public override void OnNetworkSpawn()
    {
        // L'entite qui appartient au client est recoloriee en rouge
        if (IsOwner)
        {
            m_SpriteRenderer.color = Color.red;
        }
    }

    private void Update()
    {
        if (IsOwner)
        {
            if (m_Player.corrected.Value && !m_Player.inSync.Value)
            {
                Debug.Log("corrected");
                m_Player.PredPosition = m_Player.Position;
                SendinSyncServerRpc();
            }
            transform.position = m_Player.PredPosition;
        }
        else {
            Debug.Log("not owner");
            transform.position = m_Player.Position;
        }
    }
        [ServerRpc]
    private void SendinSyncServerRpc()
    {
        // On utilise une file pour les inputs pour les cas ou on en recoit plusieurs en meme temps.
        m_Player.inSync.Value = true;
    }
}
