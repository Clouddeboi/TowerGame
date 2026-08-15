using System;
using System.Collections.Generic;

namespace Game.Inventory.SaveSystem
{
    //plain serializable mirror of ItemInstance, referenced by stable string ids only,
    //never a direct ScriptableObject or ItemInstance reference
    [Serializable]
    public class ItemInstanceRecord
    {
        public string instanceId;
        public string definitionId;
        public int quantity;
        public float durability;
        public int currentCharges;
        public List<RolledStatRecord> rolledStats = new List<RolledStatRecord>();
        public List<string> enchantmentIds = new List<string>();
        public int upgradeLevel;
        public string customName;
        public bool isStolen;
        public string ownerId;
        public bool hasAppliedPoison;
        public string poisonEffectId;
        public float poisonRemainingDurationSeconds;
        public float poisonStrength;
        public List<TemporaryEffectRecord> temporaryEffects = new List<TemporaryEffectRecord>();
        public List<QuestStateEntryRecord> questState = new List<QuestStateEntryRecord>();
        public bool preventUnequip;
    }

    [Serializable]
    public class RolledStatRecord
    {
        public string statId;
        public float value;
    }

    [Serializable]
    public class TemporaryEffectRecord
    {
        public string effectId;
        public float remainingDurationSeconds;
        public float strength;
    }

    //a plain key/value pair record, used instead of a Dictionary field directly,
    //since most common Unity serializers (including JsonUtility) do not support
    //Dictionary serialization natively
    [Serializable]
    public class QuestStateEntryRecord
    {
        public string key;
        public string value;
    }
}