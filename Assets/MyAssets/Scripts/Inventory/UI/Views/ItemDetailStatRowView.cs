using Game.Inventory.UI.Presenters;
using TMPro;
using UnityEngine;

namespace Game.Inventory.UI.Views
{
    //one labeled stat row with an optional colored comparison delta
    public class ItemDetailStatRowView : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text labelText;

        [SerializeField]
        private TMP_Text valueText;

        [SerializeField]
        private TMP_Text deltaText;

        [SerializeField]
        private Color positiveDeltaColor = Color.green;

        [SerializeField]
        private Color negativeDeltaColor = Color.red;

        public void Bind(ItemDetailStat stat)
        {
            if (labelText != null)
            {
                labelText.text = stat.labelKey;
            }

            if (valueText != null)
            {
                valueText.text = stat.valueText;
                valueText.color = stat.isUnmetRequirement ? negativeDeltaColor : Color.white;
            }

            if (deltaText == null)
            {
                return;
            }

            if (!stat.comparisonDelta.HasValue || Mathf.Approximately(stat.comparisonDelta.Value, 0f))
            {
                deltaText.text = string.Empty;
                return;
            }

            float delta = stat.comparisonDelta.Value;
            string sign = delta > 0f ? "+" : string.Empty;

            deltaText.text = $"({sign}{delta:0.#})";
            deltaText.color = delta > 0f ? positiveDeltaColor : negativeDeltaColor;
        }
    }
}