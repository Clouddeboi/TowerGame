using Game.Inventory.UI.Presenters;
using TMPro;
using UnityEngine;

namespace Game.Inventory.UI.Views
{
    public class CompareRowView : MonoBehaviour
    {
        [SerializeField] private TMP_Text labelText;
        [SerializeField] private TMP_Text leftValueText;
        [SerializeField] private TMP_Text indicatorText;
        [SerializeField] private TMP_Text rightValueText;

        [SerializeField] private Color higherColor = new Color(0.3f, 0.8f, 0.3f);
        [SerializeField] private Color lowerColor = new Color(0.85f, 0.25f, 0.2f);
        [SerializeField] private Color equalColor = Color.gray;

        public void Bind(CompareStatRow row)
        {
            if (labelText != null) labelText.text = row.labelKey;
            if (leftValueText != null) leftValueText.text = row.leftValueText;
            if (rightValueText != null) rightValueText.text = row.rightValueText;

            if (indicatorText == null)
            {
                return;
            }

            switch (row.indicator)
            {
                case CompareIndicator.Higher:
                    indicatorText.text = "\u2191";
                    indicatorText.color = higherColor;
                    break;
                case CompareIndicator.Lower:
                    indicatorText.text = "\u2193";
                    indicatorText.color = lowerColor;
                    break;
                default:
                    indicatorText.text = "-";
                    indicatorText.color = equalColor;
                    break;
            }
        }
    }
}