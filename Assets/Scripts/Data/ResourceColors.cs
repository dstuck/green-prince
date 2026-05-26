using System.Collections.Generic;
using UnityEngine;

namespace GreenPrince
{
    public static class ResourceColors
    {
        static readonly Dictionary<ResourceType, Color> s_Colors = new()
        {
            { ResourceType.Food,  new Color(0.3f, 0.7f, 0.35f) },
            { ResourceType.Force, new Color(0.8f, 0.3f, 0.3f) },
            { ResourceType.Tools, new Color(0.3f, 0.45f, 0.8f) },
        };

        public static Color Get(ResourceType type) => s_Colors[type];
    }
}
