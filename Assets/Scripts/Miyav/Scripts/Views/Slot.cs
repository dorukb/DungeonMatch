using System;
using UnityEngine;

namespace DorkyProductions.Views
{
    public class Slot
    {
        public Guid containedObjectID { get; private set; }
        public int idx { get; private set; }
        private float spacing;
        private float itemWidth;
        public Slot(int idx, float spacing)
        {
            this.idx = idx;
            this.spacing = spacing;
            this.containedObjectID = Guid.Empty;
        }

        public void Fill(Guid id, float width)
        {
            this.containedObjectID = id;
            this.itemWidth = width;
        }

        public void MakeEmpty()
        {
            this.containedObjectID = Guid.Empty;
            this.itemWidth = 0;
        }
        public Vector2 GetPosition()
        {
            if (idx == 0) { return Vector2.zero; }
            
            float width = itemWidth / 2.0f;
            float xOffset = idx * (width + spacing);
            return new Vector2(xOffset, 0f);
        }
        public bool IsAvailable()
        {
            return containedObjectID == Guid.Empty;
        }
    }
}