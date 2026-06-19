using UnityEngine;

namespace GreenPrince
{
    /// <summary>
    /// Keeps a consistent world view on short WebGL embeds (itch.io) by easing orthographic size.
    /// </summary>
    public class ViewportCameraFit : MonoBehaviour
    {
        [SerializeField] Camera m_Camera;
        [SerializeField] float m_ReferenceOrthoSize = 5f;
        [SerializeField] float m_ReferenceHeight = 1080f;
        [SerializeField] float m_MinOrthoSize = 5f;
        [SerializeField] float m_MaxOrthoSize = 7.5f;

        void Awake()
        {
            if (m_Camera == null)
                m_Camera = GetComponent<Camera>();
            Apply();
        }

        void Apply()
        {
            if (m_Camera == null || !m_Camera.orthographic)
                return;

            float height = Mathf.Max(Screen.height, 480f);
            float factor = m_ReferenceHeight / height;
            m_Camera.orthographicSize = Mathf.Clamp(m_ReferenceOrthoSize * factor, m_MinOrthoSize, m_MaxOrthoSize);
        }
    }
}
