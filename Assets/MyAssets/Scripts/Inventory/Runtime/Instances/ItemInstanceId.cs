using System;

namespace Game.Inventory.Instances
{
    //a unique identifier for one specific runtime item instance
    //distinct from ItemId, which identifies a shared definition
    //two swords of the same definition have the same ItemId but different ItemInstanceId
    [Serializable]
    public readonly struct ItemInstanceId : IEquatable<ItemInstanceId>
    {
        private readonly string _value;

        public ItemInstanceId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("ItemInstanceId cannot be null or whitespace.", nameof(value));
            }

            _value = value;
        }

        public bool IsEmpty => string.IsNullOrEmpty(_value);

        public static ItemInstanceId Empty => default;

        public bool Equals(ItemInstanceId other) => string.Equals(_value, other._value, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is ItemInstanceId other && Equals(other);

        public override int GetHashCode() => _value != null ? _value.GetHashCode() : 0;

        public override string ToString() => _value ?? string.Empty;

        public static bool operator ==(ItemInstanceId left, ItemInstanceId right) => left.Equals(right);

        public static bool operator !=(ItemInstanceId left, ItemInstanceId right) => !left.Equals(right);
    }
}