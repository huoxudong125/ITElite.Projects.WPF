namespace ITElite.Projects.WPF.Controls.DeepZoom.Core
{
    /// <summary>
    ///     Represents a Tile in a Deep Zoom image.
    /// </summary>
    internal struct Tile
    {
        public Tile(int level, int column, int row)
            : this()
        {
            Level = level;
            Row = row;
            Column = column;
        }

        public int Level { get; set; }
        public int Row { get; set; }
        public int Column { get; set; }

        public override string ToString()
        {
            return Level + "_" + Row + "_" + Column;
        }
    }
}