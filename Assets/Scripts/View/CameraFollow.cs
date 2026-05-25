using UnityEngine;

namespace GreenPrince
{
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] Transform m_Target;
        [SerializeField] float m_SmoothSpeed = 5f;
        [SerializeField] Vector3 m_Offset = new Vector3(0f, 0f, -10f);

        [Header("Grid Bounds")]
        [SerializeField] float m_MinX;
        [SerializeField] float m_MaxX = 19f;
        [SerializeField] float m_MinY;
        [SerializeField] float m_MaxY = 6f;

        void LateUpdate()
        {
            if (m_Target == null) return;

            var desired = m_Target.position + m_Offset;
            desired.x = Mathf.Clamp(desired.x, m_MinX, m_MaxX);
            desired.y = Mathf.Clamp(desired.y, m_MinY, m_MaxY);

            transform.position = Vector3.Lerp(transform.position, desired, m_SmoothSpeed * Time.deltaTime);
        }
    }
}
