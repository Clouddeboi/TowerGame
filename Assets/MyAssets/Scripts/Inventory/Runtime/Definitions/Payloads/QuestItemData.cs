using UnityEngine;

namespace Game.Inventory.Definitions.Payloads
{
    //optional data attached to an ItemDefinition that represents a quest item
    [System.Serializable]
    public class QuestItemData
    {
        [SerializeField]
        private string questId;

        [SerializeField]
        private int minimumQuestStage;

        [SerializeField]
        private bool canBeRemoved = true;

        [SerializeField]
        private bool hiddenBeforeDiscovery;

        [SerializeField]
        private bool allowDuplicates;

        public string QuestId => questId;
        public int MinimumQuestStage => minimumQuestStage;
        public bool CanBeRemoved => canBeRemoved;
        public bool HiddenBeforeDiscovery => hiddenBeforeDiscovery;
        public bool AllowDuplicates => allowDuplicates;
    }
}