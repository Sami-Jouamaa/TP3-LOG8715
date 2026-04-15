using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Player : NetworkBehaviour
{
    struct PlayerInput
    {
        public int tick;
        public Vector2 direction;
    }

    private Queue<PlayerInput> m_InputQueue = new Queue<PlayerInput>();
    private List<PlayerInput> m_InputBuffer = new List<PlayerInput>();
    private int m_CurrentTick = 0;

    [SerializeField]
    private float m_Velocity;

    [SerializeField]
    private float m_Size = 1;

    private GameState m_GameState;

    public NetworkVariable<bool> corrected = new NetworkVariable<bool>();
    // GameState peut etre nul si l'entite joueur est instanciee avant de charger MainScene
    public NetworkVariable<bool> inSync = new NetworkVariable<bool>(true);
    // GameState peut etre nul si l'entite joueur est instanciee avant de charger MainScene

    private GameState GameState
    {
        get
        {
            if (m_GameState == null)
            {
                m_GameState = FindFirstObjectByType<GameState>();
            }
            return m_GameState;
        }
    }

    private NetworkVariable<Vector2> m_Position = new NetworkVariable<Vector2>();

    public Vector2 Position => m_Position.Value;

    public Vector2 PredPosition = new();

    private void Awake()
    {
        m_GameState = FindFirstObjectByType<GameState>();
    }

    private void FixedUpdate()
    {
        // Si le stun est active, rien n'est mis a jour.
        if (GameState == null || GameState.IsStunned)
        {
            return;
        }

        // Seul le serveur met à jour la position de l'entite.
        if (IsServer)
        {
            UpdatePositionServer();
        }

        // Seul le client qui possede cette entite peut envoyer ses inputs. 
        if (IsClient && IsOwner)
        {
            UpdateInputClient();
            Reconcile();
        }
    }

    private NetworkVariable<int> LastProcessedTick = new NetworkVariable<int>();

    private void UpdatePositionServer()
    {
        if (!inSync.Value)
        {
            m_InputQueue.Clear();
            return;
        }
        // Mise a jour de la position selon dernier input reçu, puis consommation de l'input
        if (m_InputQueue.Count > 0)
        {
            var input = m_InputQueue.Dequeue();
            LastProcessedTick.Value = input.tick;
            m_Position.Value += input.direction * m_Velocity * Time.deltaTime;
            // Gestion des collisions avec l'exterieur de la zone de simulation
            var size = GameState.GameSize;
            corrected.Value = false;
            if (m_Position.Value.x - m_Size < -size.x)
            {
                m_Position.Value = new Vector2(-size.x + m_Size, m_Position.Value.y);
                corrected.Value = true;
            }
            else if (m_Position.Value.x + m_Size > size.x)
            {
                m_Position.Value = new Vector2(size.x - m_Size, m_Position.Value.y);
                corrected.Value = true;
            }
            if (m_Position.Value.y + m_Size > size.y)
            {
                m_Position.Value = new Vector2(m_Position.Value.x, size.y - m_Size);
                corrected.Value = true;
            }
            else if (m_Position.Value.y - m_Size < -size.y)
            {
                m_Position.Value = new Vector2(m_Position.Value.x, -size.y + m_Size);
                corrected.Value = true;
            }
        }
        if (corrected.Value)
        {
            inSync.Value = false;
        }

    }

    private void Reconcile()
    {
        if (!IsOwner) return;

        int serverTick = LastProcessedTick.Value;
        Vector2 serverPos = m_Position.Value;

        PredPosition = serverPos;

        m_InputBuffer.RemoveAll(i => i.tick <= serverTick);

        foreach (var input in m_InputBuffer)
        {
            PredPosition += input.direction * m_Velocity * Time.fixedDeltaTime;
        }
    }

    private void UpdateInputClient()
    {
        Vector2 inputDirection = new Vector2(0, 0);
        if (Input.GetKey(KeyCode.W))
        {
            inputDirection += Vector2.up;
        }
        if (Input.GetKey(KeyCode.A))
        {
            inputDirection += Vector2.left;
        }
        if (Input.GetKey(KeyCode.S))
        {
            inputDirection += Vector2.down;
        }
        if (Input.GetKey(KeyCode.D))
        {
            inputDirection += Vector2.right;
        }

        inputDirection = inputDirection.normalized;

        m_CurrentTick++;

        PlayerInput input = new PlayerInput
        {
            tick = m_CurrentTick,
            direction = inputDirection
        };

        PredPosition += input.direction * m_Velocity * Time.fixedDeltaTime;
        m_InputBuffer.Add(input);
        SendInputServerRpc(input.tick, input.direction);

        // PredPosition += inputDirection.normalized * m_Velocity * Time.deltaTime;

        // SendInputServerRpc(inputDirection.normalized);
    }


    [ServerRpc]
    private void SendInputServerRpc(int tick, Vector2 input)
    {
        // On utilise une file pour les inputs pour les cas ou on en recoit plusieurs en meme temps.
        m_InputQueue.Enqueue(new PlayerInput
        {
            tick = tick,
            direction = input
        });
    }




}
