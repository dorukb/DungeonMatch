using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace DorkyProductions.Views
{
    [RequireComponent(typeof(RectTransform))]
    public class RowLayout  : MonoBehaviour
    {
        [SerializeField] private float spacing = 25f;
        private readonly List<RectTransform> _children = new List<RectTransform>();

        private RectTransform _myTransform;
        private readonly List<Slot> _slots = new List<Slot>();
        private void Awake()
        {
            _myTransform = GetComponent<RectTransform>();
        }

        public void SetupSlots(int slotCount)
        {
            for (int i = 0; i < slotCount; i++)
            {
                var slot = new Slot(i, spacing);
                _slots.Add(slot);
            }
        }
        public RectTransform GetAnchor()
        {
            return _myTransform;
        }
        public void AddChild(RectTransform newChild, Guid childID, Slot slot)
        {
            _children.Add(newChild);
            slot.Fill(childID, newChild.rect.width);
        }

        public void RemoveChild(RectTransform child, Guid childID)
        {
            _children.Remove(child);
            int idx = _slots.FindIndex(t => t.containedObjectID == childID);
            if (idx > -1)
            {
                _slots[idx].MakeEmpty();
            }
            else
            {
                Debug.LogWarning("Card to remove is NOT contained in a slot!!. Guid: " + childID);
            }
        }
        public Slot GetFirstEmptySlot()
        {
            return _slots.FirstOrDefault(t => t.IsAvailable());
        }

        public bool HasEmptySlot()
        {
            return _slots.Any(t => t.IsAvailable());
        }
    }
}