using System.Collections.Generic;
using UnityEngine;

namespace GreenPrince
{
    public static class CampResourceColors
    {
        static readonly Dictionary<CampResourceType, Color> s_Colors = new()
        {
            { CampResourceType.Technology, new Color(0.7f, 0.4f, 0.8f) },
            { CampResourceType.Experience, new Color(0.8f, 0.6f, 0.3f) },
            { CampResourceType.Lore,       new Color(0.4f, 0.7f, 0.6f) },
        };

        public static Color Get(CampResourceType type) => s_Colors[type];
    }
}

