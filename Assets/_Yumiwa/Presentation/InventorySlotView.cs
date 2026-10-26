using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using Yumiwa.Domain;

namespace Yumiwa.Presentation
{
    public sealed class InventorySlotView : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text qty;
        [SerializeField] private Image border;
        public int Index { get; set; }
        public string CurrentItemId { get; private set; }
        public bool IsHandHighlighted { get; private set; }
        public System.Action<int> OnClicked;
        public System.Action<int,int> OnDropped;

        public void Set(ItemDefinition item, bool isHand, bool isSelected)
        {
            CurrentItemId = item.ItemId;
            IsHandHighlighted = isHand;
            if (icon != null) icon.enabled = ItemDefinition.IsValid(item);
            if (qty != null) qty.text = ItemDefinition.IsValid(item) ? $"x{item.Quantity}" : "";
            if (border != null) border.color = isHand ? Color.yellow : (isSelected ? Color.white : Color.gray);
        }

        public void OnPointerClick(PointerEventData eventData) => OnClicked?.Invoke(Index);
        public void OnBeginDrag(PointerEventData eventData) { }
        public void OnDrag(PointerEventData eventData) { }
        public void OnEndDrag(PointerEventData eventData) { }
        public void OnDrop(PointerEventData eventData)
        {
            var from = eventData.pointerDrag?.GetComponent<InventorySlotView>()?.Index ?? -1;
            if (from >= 0) OnDropped?.Invoke(from, Index);
        }
    }
}
