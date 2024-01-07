using System.Windows.Forms;

namespace Sudoku
{
  class SudokuCell : Button
  {
    public int Value { get; set; }
    public bool IsLocked { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int Grid { get; set; }

    public void Clear()
    {
      this.Text = string.Empty;
      this.IsLocked = false;
    }
  }
}
