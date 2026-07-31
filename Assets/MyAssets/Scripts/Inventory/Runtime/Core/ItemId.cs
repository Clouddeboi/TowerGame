using System;

namespace Game.Inventory.Core
{
    //A stable, immutable identifier for an item definition. This is the only
    //value that should ever be used to reference an item across save data,
    //code, and designer facing tools. Display names must never be used as
    //identifiers because they can change during localization or content
    //revision without breaking references.
    [Serializable]
    public readonly struct ItemId : IEquatable<ItemId>
    {
        private readonly string _value;

        public ItemId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("ItemId cannot be null or whitespace.", nameof(value));
            }

            _value = value;
        }

        //True if this id was default constructed and holds no value
        public bool IsEmpty => string.IsNullOrEmpty(_value);

        public static ItemId Empty => default;

        public bool Equals(ItemId other) => string.Equals(_value, other._value, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is ItemId other && Equals(other);

        public override int GetHashCode() => _value != null ? _value.GetHashCode() : 0;

        public override string ToString() => _value ?? string.Empty;

        public static bool operator ==(ItemId left, ItemId right) => left.Equals(right);

        public static bool operator !=(ItemId left, ItemId right) => !left.Equals(right);

        //Explicit, intentional conversion only, there is no implicit string
        //cast so that raw strings cannot silently be used as item identifiers. 
        //This is to ensure that all item identifiers are used consistently
        //throughout the codebase.
        public static ItemId FromRaw(string value) => new ItemId(value);
    }
}