using System.Collections.Generic;
using Game.Inventory.UI.Presenters;
using UnityEngine;

namespace Game.Inventory.UI.Views
{
    public class PlayerStatsView : MonoBehaviour
    {
        [SerializeField] private Transform statRowParent;
        [SerializeField] private ItemDetailStatRowView statRowPrefab;

        private readonly List<ItemDetailStatRowView> _spawned = new List<ItemDetailStatRowView>();

        public void Render(IReadOnlyList<ItemDetailStat> stats)
        {
            foreach (var row in _spawned)
            {
                if (row != null) Destroy(row.gameObject);
            }
            _spawned.Clear();

            foreach (var stat in stats)
            {
                var row = Instantiate(statRowPrefab, statRowParent);
                row.Bind(stat);
                _spawned.Add(row);
            }
        }
    }
}