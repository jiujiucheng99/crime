using UnityEngine;

namespace AmberCity.Prototype
{
    /// <summary>
    /// A modular Brick Row city block root. The blockout keeps each cell as its
    /// own GameObject so it can later become a prefab or additive scene.
    /// </summary>
    public sealed class BrickRowBlock : MonoBehaviour
    {
        public string BlockId { get; private set; }
        public int ColumnIndex { get; private set; }
        public int RowIndex { get; private set; }
        public Color BuildingColor { get; private set; }

        public void Configure(string blockId, int columnIndex, int rowIndex, Color buildingColor)
        {
            BlockId = blockId;
            ColumnIndex = columnIndex;
            RowIndex = rowIndex;
            BuildingColor = buildingColor;
        }
    }
}
