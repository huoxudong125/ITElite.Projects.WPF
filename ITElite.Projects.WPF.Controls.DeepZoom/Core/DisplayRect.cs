using System.Windows;

namespace ITElite.Projects.WPF.Controls.DeepZoom.Core
{
    /// <summary>
    ///     Contains Rect elements that define the rectangle to be displayed.
    /// </summary>
    public struct DisplayRect
    {
        public DisplayRect(double x, double y, double width, double height, int minLevel, int maxLevel)
            : this()
        {
            Rect = new Rect(x, y, width, height);
            MinLevel = minLevel;
            MaxLevel = maxLevel;
        }

        public Rect Rect { get; set; }
        public int MinLevel { get; set; }
        public int MaxLevel { get; set; }
    }
}