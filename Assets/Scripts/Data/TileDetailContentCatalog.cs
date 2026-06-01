using System;
using System.Collections.Generic;
using UnityEngine;

namespace GreenPrince
{
    public readonly struct TileDetailContent
    {
        public TileDetailContent(string name, string description)
        {
            Name = name ?? string.Empty;
            Description = description ?? string.Empty;
        }

        public string Name { get; }
        public string Description { get; }
        public bool IsEmpty => string.IsNullOrEmpty(Name) && string.IsNullOrEmpty(Description);
    }

    public sealed class TileDetailContentCatalog
    {
        const string ResourcePath = "Content/tile-details";

        readonly Dictionary<string, TileDetailContent> m_Tiles = new();
        readonly Dictionary<string, TileDetailContent> m_Features = new();
        readonly Dictionary<string, TileDetailContent> m_Ui = new();

        public static TileDetailContentCatalog Load(TextAsset source = null)
        {
            var catalog = new TileDetailContentCatalog();
            source ??= Resources.Load<TextAsset>(ResourcePath);
            if (source == null)
            {
                Debug.LogWarning(
                    $"Tile detail content not found at Resources/{ResourcePath}. " +
                    "Shift-hover labels will use fallbacks.");
                return catalog;
            }

            catalog.Parse(source.text);
            return catalog;
        }

        public bool TryGetTile(string definitionId, out TileDetailContent content)
        {
            if (string.IsNullOrEmpty(definitionId))
            {
                content = default;
                return false;
            }

            return m_Tiles.TryGetValue(definitionId, out content);
        }

        public bool TryGetFeature(WorldFeatureKind kind, out TileDetailContent content)
        {
            return m_Features.TryGetValue(FeatureKey(kind), out content);
        }

        public bool TryGetUi(string key, out TileDetailContent content)
        {
            if (string.IsNullOrEmpty(key))
            {
                content = default;
                return false;
            }

            return m_Ui.TryGetValue(key, out content);
        }

        public static string FeatureKey(WorldFeatureKind kind)
        {
            return kind switch
            {
                WorldFeatureKind.FirstLandmark => "first_landmark",
                WorldFeatureKind.SecondLandmark => "second_landmark",
                WorldFeatureKind.GoblinCamp => "goblin_camp",
                _ => kind.ToString().ToLowerInvariant()
            };
        }

        void Parse(string yaml)
        {
            m_Tiles.Clear();
            m_Features.Clear();
            m_Ui.Clear();

            Dictionary<string, TileDetailContent> section = null;
            string currentKey = null;
            string pendingName = null;

            foreach (var rawLine in yaml.Split('\n'))
            {
                var line = rawLine.TrimEnd('\r');
                var trimmed = line.Trim();
                if (trimmed.Length == 0 || trimmed.StartsWith("#", StringComparison.Ordinal))
                    continue;

                if (!line.StartsWith(" ", StringComparison.Ordinal))
                {
                    FlushEntry(section, currentKey, pendingName, null);
                    currentKey = null;
                    pendingName = null;

                    section = trimmed switch
                    {
                        "tiles:" => m_Tiles,
                        "features:" => m_Features,
                        "ui:" => m_Ui,
                        _ => null
                    };

                    continue;
                }

                if (section == null)
                    continue;

                var indent = line.Length - line.TrimStart().Length;
                var content = line.TrimStart();

                if (indent <= 2 && content.EndsWith(":", StringComparison.Ordinal))
                {
                    FlushEntry(section, currentKey, pendingName, null);
                    currentKey = content[..^1].Trim();
                    pendingName = null;
                    continue;
                }

                if (currentKey == null)
                    continue;

                if (TryReadField(content, "name", out var nameValue))
                    pendingName = nameValue;
                else if (TryReadField(content, "description", out var descriptionValue))
                    FlushEntry(section, currentKey, pendingName, descriptionValue);
            }

            FlushEntry(section, currentKey, pendingName, null);
        }

        static bool TryReadField(string line, string field, out string value)
        {
            var prefix = field + ":";
            if (!line.StartsWith(prefix, StringComparison.Ordinal))
            {
                value = null;
                return false;
            }

            value = Unquote(line[prefix.Length..].Trim());
            return true;
        }

        static string Unquote(string value)
        {
            if (value.Length >= 2 && value[0] == '"' && value[^1] == '"')
                return value[1..^1].Replace("\\\"", "\"");
            return value;
        }

        static void FlushEntry(
            Dictionary<string, TileDetailContent> section,
            string key,
            string name,
            string description)
        {
            if (section == null || string.IsNullOrEmpty(key))
                return;

            if (description == null)
                return;

            section[key] = new TileDetailContent(name, description);
        }
    }
}
