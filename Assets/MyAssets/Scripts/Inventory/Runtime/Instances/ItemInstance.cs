using System;
using System.Collections.Generic;
using Game.Inventory.Core;

namespace Game.Inventory.Instances
{
    //mutable, per-item runtime state
    //paired with an ItemDefinition through DefinitionId, resolved via ItemDatabase rather than a direct reference
    //never write this class into a ScriptableObject asset, it belongs on the heap only
    [Serializable]
    public class ItemInstance
    {
        private ItemInstanceId _instanceId;
        private ItemId _definitionId;
        private int _quantity;
        private float _durability;
        private int _currentCharges;
        private List<RolledStatModifier> _rolledStats;
        private List<ItemId> _enchantmentIds;
        private int _upgradeLevel;
        private string _customName;
        private bool _isStolen;
        private string _ownerId;
        private AppliedTemporaryEffect? _appliedPoison;
        private List<AppliedTemporaryEffect> _temporaryEffects;
        private Dictionary<string, string> _questState;

        private bool _preventUnequip;
        public bool PreventUnequip => _preventUnequip;

        public void SetPreventUnequip(bool prevent)
        {
            _preventUnequip = prevent;
}

        //internal constructor, creation goes through ItemInstanceFactory only
        //so instance id uniqueness stays guaranteed in one place
        internal ItemInstance(ItemInstanceId instanceId, ItemId definitionId, int quantity)
        {
            if (instanceId.IsEmpty)
            {
                throw new ArgumentException("ItemInstance requires a non-empty instance id.", nameof(instanceId));
            }

            if (definitionId.IsEmpty)
            {
                throw new ArgumentException("ItemInstance requires a non-empty definition id.", nameof(definitionId));
            }

            _instanceId = instanceId;
            _definitionId = definitionId;
            _quantity = Math.Max(0, quantity);
            _rolledStats = new List<RolledStatModifier>();
            _enchantmentIds = new List<ItemId>();
            _temporaryEffects = new List<AppliedTemporaryEffect>();
            _questState = new Dictionary<string, string>();
        }

        public ItemInstanceId InstanceId => _instanceId;
        public ItemId DefinitionId => _definitionId;
        public int Quantity => _quantity;
        public float Durability => _durability;
        public int CurrentCharges => _currentCharges;
        public IReadOnlyList<RolledStatModifier> RolledStats => _rolledStats;
        public IReadOnlyList<ItemId> EnchantmentIds => _enchantmentIds;
        public int UpgradeLevel => _upgradeLevel;
        public string CustomName => _customName;
        public bool IsStolen => _isStolen;
        public string OwnerId => _ownerId;
        public AppliedTemporaryEffect? AppliedPoison => _appliedPoison;
        public IReadOnlyList<AppliedTemporaryEffect> TemporaryEffects => _temporaryEffects;
        public IReadOnlyDictionary<string, string> QuestState => _questState;

        //quantity is mutated through InventoryService, not directly, this exists for that service to call into
        internal void SetQuantity(int newQuantity)
        {
            _quantity = Math.Max(0, newQuantity);
        }

        public void SetDurability(float durability)
        {
            _durability = durability;
        }

        public void SetCharges(int charges)
        {
            _currentCharges = charges;
        }

        public void AddRolledStat(RolledStatModifier stat)
        {
            _rolledStats.Add(stat);
        }

        public void AddEnchantment(ItemId enchantmentId)
        {
            if (!_enchantmentIds.Contains(enchantmentId))
            {
                _enchantmentIds.Add(enchantmentId);
            }
        }

        public void RemoveEnchantment(ItemId enchantmentId)
        {
            _enchantmentIds.Remove(enchantmentId);
        }

        public void SetUpgradeLevel(int level)
        {
            _upgradeLevel = level;
        }

        public void SetCustomName(string customName)
        {
            _customName = customName;
        }

        public void SetStolen(bool stolen)
        {
            _isStolen = stolen;
        }

        public void SetOwner(string ownerId)
        {
            _ownerId = ownerId;
        }

        public void ApplyPoison(AppliedTemporaryEffect poison)
        {
            _appliedPoison = poison;
        }

        public void ClearPoison()
        {
            _appliedPoison = null;
        }

        public void AddTemporaryEffect(AppliedTemporaryEffect effect)
        {
            _temporaryEffects.Add(effect);
        }

        public void RemoveExpiredTemporaryEffects()
        {
            _temporaryEffects.RemoveAll(e => e.remainingDurationSeconds <= 0f);
        }

        public void SetQuestState(string key, string value)
        {
            _questState[key] = value;
        }

        //the key used to decide whether two instances can stack together
        //instances that differ in durability, enchantments, ownership, rolled stats, or other unique data
        //produce different keys and therefore never silently merge
        public string GetStackKey()
        {
            if (_durability != 0f || _currentCharges != 0 || _rolledStats.Count > 0 ||
                _enchantmentIds.Count > 0 || _upgradeLevel != 0 || !string.IsNullOrEmpty(_customName) ||
                _isStolen || !string.IsNullOrEmpty(_ownerId) || _appliedPoison.HasValue ||
                _temporaryEffects.Count > 0 || _questState.Count > 0)
            {
                //any unique runtime state present means this instance forms its own stack
                return _definitionId + "|" + _instanceId;
            }

            //no unique state, safe to stack purely by definition
            return _definitionId.ToString();
        }
    }
}