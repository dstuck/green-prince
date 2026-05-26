using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GreenPrince
{
    public class PartyToken : MonoBehaviour
    {
        [SerializeField] InputActionReference m_MoveActionRef;
        [SerializeField] float m_MoveSpeed = 8f;
        [SerializeField] float m_StepCooldown = 0.2f;

        public event Action<Vector2Int> MoveRequested;

        Vector2Int m_GridPosition;
        Vector3 m_TargetWorldPos;
        float m_CooldownTimer;
        bool m_WasNeutral = true;

        public Vector2Int GridPosition => m_GridPosition;

        public void SetGridPosition(Vector2Int pos, Vector3 worldPos)
        {
            m_GridPosition = pos;
            m_TargetWorldPos = worldPos;
            transform.position = worldPos;
        }

        void OnEnable()
        {
            var action = m_MoveActionRef?.action;
            if (action != null) action.Enable();
            m_WasNeutral = true;
            m_CooldownTimer = 0f;
        }

        void OnDisable()
        {
            var action = m_MoveActionRef?.action;
            if (action != null) action.Disable();
        }

        void Update()
        {
            transform.position = Vector3.MoveTowards(
                transform.position, m_TargetWorldPos, m_MoveSpeed * Time.deltaTime);

            m_CooldownTimer -= Time.deltaTime;

            var action = m_MoveActionRef?.action;
            if (action == null) return;

            var raw = action.ReadValue<Vector2>();
            var dir = SnapToCardinal(raw);

            if (dir == Vector2Int.zero)
            {
                m_WasNeutral = true;
                return;
            }

            bool canStep = m_WasNeutral || m_CooldownTimer <= 0f;
            if (!canStep) return;

            m_WasNeutral = false;
            m_CooldownTimer = m_StepCooldown;
            MoveRequested?.Invoke(dir);
        }

        public void MoveTo(Vector2Int newGridPos, Vector3 worldPos)
        {
            m_GridPosition = newGridPos;
            m_TargetWorldPos = worldPos;
        }

        static Vector2Int SnapToCardinal(Vector2 input)
        {
            if (input.sqrMagnitude < 0.25f) return Vector2Int.zero;
            return Mathf.Abs(input.x) >= Mathf.Abs(input.y)
                ? new Vector2Int(input.x > 0 ? 1 : -1, 0)
                : new Vector2Int(0, input.y > 0 ? 1 : -1);
        }
    }
}
