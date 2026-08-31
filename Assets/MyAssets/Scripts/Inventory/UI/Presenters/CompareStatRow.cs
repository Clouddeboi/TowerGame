namespace Game.Inventory.UI.Presenters
{
    public enum CompareIndicator
    {
        Higher,
        Lower,
        Equal
    }

    //one stat row across both compared items, leftValueText/rightValueText are
    //pre-formatted display strings, indicator is computed from the left item's
    //perspective (Higher means left beats right)
    public readonly struct CompareStatRow
    {
        public readonly string labelKey;
        public readonly string leftValueText;
        public readonly string rightValueText;
        public readonly CompareIndicator indicator;

        public CompareStatRow(string labelKey, string leftValueText, string rightValueText, CompareIndicator indicator)
        {
            this.labelKey = labelKey;
            this.leftValueText = leftValueText;
            this.rightValueText = rightValueText;
            this.indicator = indicator;
        }
    }
}