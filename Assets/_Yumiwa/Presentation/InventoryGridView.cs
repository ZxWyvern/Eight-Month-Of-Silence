using UnityEngine;
using UnityEngine.UI;
using Yumiwa.Application;

namespace Yumiwa.Presentation
{
    public sealed class InventoryGridView : MonoBehaviour
    {
        [SerializeField] private GridLayoutGroup grid;
        [SerializeField] private InventorySlotView slotPrefab;
        private InventorySlotView[] slots = new InventorySlotView[6];
        public System.Action<int,int> OnMoveRequested;

        void Awake()
        {
            if (grid == null) grid = GetComponent<GridLayoutGroup>();
            if (grid != null)
            {
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = 3;
                grid.cellSize = new Vector2(80, 80);
                grid.spacing = new Vector2(8, 8);
            }
            // Auto-bind 6 slot views if not already set (for scene-created grid)
            if (slots == null || slots.Length != 6 || slots[0] == null)
            {
                var found = GetComponentsInChildren<InventorySlotView>(true);
                if (found.Length == 6) BindSlots(found);
            }
        }

        public void Render(InventorySlot[] inv)
        {
            for (int i = 0; i < 6; i++)
            {
                if (i < slots.Length && slots[i] != null)
                    slots[i].Set(inv[i].IsOccupied ? inv[i].Item : default, i < 2, false);
            }
        }

        public void BindSlots(InventorySlotView[] views)
        {
            slots = views;
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null)
                {
                    slots[i].Index = i;
                    slots[i].OnClicked = (idx) => OnMoveRequested?.Invoke(idx, idx);
                    slots[i].OnDropped = (from, to) => OnMoveRequested?.Invoke(from, to);
                }
            }
        }
    }
}
