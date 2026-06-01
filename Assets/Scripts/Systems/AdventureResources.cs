using System;
using System.Collections.Generic;

namespace GreenPrince
{
    public class AdventureResources
    {
        readonly Dictionary<ResourceType, int> m_Values;
        int m_StepCount;

        public event Action Changed;

        /// <summary>True after the most recent RecordStep call consumed food.</summary>
        public bool StepTriggeredConsumption { get; private set; }

        public AdventureResources(int food = 10, int force = 5, int tools = 4)
        {
            m_Values = new Dictionary<ResourceType, int>
            {
                { ResourceType.Food,  food },
                { ResourceType.Force, force },
                { ResourceType.Tools, tools },
            };
        }

        public int Get(ResourceType type) => m_Values[type];

        public bool CanAfford(ResourceType type, int cost)
        {
            return cost <= 0 || m_Values[type] >= cost;
        }

        public void Spend(ResourceType type, int cost)
        {
            if (cost <= 0) return;
            m_Values[type] = Math.Max(0, m_Values[type] - cost);
            Changed?.Invoke();
        }

        public void Gain(ResourceType type, int amount)
        {
            if (amount <= 0) return;
            m_Values[type] += amount;
            Changed?.Invoke();
        }

        /// <summary>
        /// Records a step taken. Every <paramref name="foodInterval"/> steps,
        /// consumes 1 food. Returns false if food was 0 when consumption was due
        /// (game over).
        /// </summary>
        public bool RecordStep(int foodInterval)
        {
            StepTriggeredConsumption = false;
            m_StepCount++;
            if (m_StepCount < foodInterval) return true;

            m_StepCount = 0;

            if (m_Values[ResourceType.Food] <= 0)
                return false;

            m_Values[ResourceType.Food]--;
            StepTriggeredConsumption = true;
            Changed?.Invoke();
            return true;
        }
    }
}
