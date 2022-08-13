using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;

namespace Sudoku
{
    public partial class Form1 : Form
    {
        const int CellSize = 75;

        static readonly Random Random = new();
        
        ArchipelagoSession session;
        DeathLinkService deathLinkService;

        public Form1()
        {
            InitializeComponent();

            createCells();

            startNewGame();
        }

        SudokuCell[,] cells = new SudokuCell[9, 9];

        private void createCells()
        {
            for (int i = 0; i < 9; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    var cell = new SudokuCell();

                    // Create 81 cells for with styles and locations based on the index
                    cell.Font = new Font(SystemFonts.DefaultFont.FontFamily, 20);
                    cell.Size = new Size(CellSize, CellSize);
                    cell.ForeColor = SystemColors.ControlDarkDark;
                    cell.Location = new Point(i * CellSize, j * CellSize);
                    cell.BackColor = ((i / 3) + (j / 3)) % 2 == 0 ? SystemColors.Control : Color.LightGray;
                    cell.FlatStyle = FlatStyle.Flat;
                    cell.FlatAppearance.BorderColor = Color.Black;
                    cell.X = i;
                    cell.Y = j;

                    // Assign key press event for each cells
                    cell.KeyPress += cell_keyPressed;

                    cells[i, j] = cell;
                    panel1.Controls.Add(cell);
                }
            }
        }

        private void cell_keyPressed(object sender, KeyPressEventArgs e)
        {
            var cell = sender as SudokuCell;

            // Do nothing if the cell is locked
            if (cell.IsLocked)
                return;

            int value;

            // Add the pressed key value in the cell only if it is a number
            if (int.TryParse(e.KeyChar.ToString(), out value))
            {
                // Clear the cell value if pressed key is zero
                if (value == 0)
                    cell.Clear();
                else
                    cell.Text = value.ToString();

                cell.ForeColor = SystemColors.ControlDarkDark;
            }
        }
        
        private void startNewGame()
        {
            loadValues();

            var hintsCount = 0;

            // Assign the hints count based on the 
            // level player chosen
            if (beginnerLevel.Checked)
                hintsCount = 45;
            else if (IntermediateLevel.Checked)
                hintsCount = 30;
            else if (AdvancedLevel.Checked)
                hintsCount = 15;

            showRandomValuesHints(hintsCount);
        }

        private void showRandomValuesHints(int hintsCount)
        {
            // Show value in radom cells
            // The hints count is based on the level player choose
            for (int i = 0; i < hintsCount; i++)
            {
                var rX = Random.Next(9);
                var rY = Random.Next(9);

                // Style the hint cells differently and
                // lock the cell so that player can't edit the value
                cells[rX, rY].Text = cells[rX, rY].Value.ToString();
                cells[rX, rY].ForeColor = Color.Black;
                cells[rX, rY].IsLocked = true;
            }
        }

        private void loadValues()
        {
            // Clear the values in each cells
            foreach (var cell in cells)
            {
                cell.Value = 0;
                cell.Clear();
            }

            // This method will be called recursively 
            // until it finds suitable values for each cells
            findValueForNextCell(0, -1);
        }

        private bool findValueForNextCell(int i, int j)
        {
            // Increment the i and j values to move to the next cell
            // and if the columsn ends move to the next row
            if (++j > 8)
            {
                j = 0;

                // Exit if the line ends
                if (++i > 8)
                    return true;
            }

            int value;
            var numsLeft = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9 };

            // Find a random and valid number for the cell and go to the next cell 
            // and check if it can be allocated with another random and valid number
            do
            {
                // If there is not numbers left in the list to try next, 
                // return to the previous cell and allocate it with a different number
                if (numsLeft.Count < 1)
                {
                    cells[i, j].Value = 0;
                    return false;
                }

                // Take a random number from the numbers left in the list
                value = numsLeft[Random.Next(0, numsLeft.Count)];
                cells[i, j].Value = value;

                // Remove the allocated value from the list
                numsLeft.Remove(value);
            }
            while (!isValidNumber(value, i, j) || !findValueForNextCell(i, j));
            
            return true;
        }

        private bool isValidNumber(int value, int x, int y)
        {
            for (int i = 0; i < 9; i++)
            {
                // Check all the cells in vertical direction
                if (i != y && cells[x, i].Value == value)
                    return false;

                // Check all the cells in horizontal direction
                if (i != x && cells[i, y].Value == value)
                    return false;
            }
            
            // Check all the cells in the specific block
            for (int i = x - (x % 3); i < x - (x % 3) + 3; i++)
            {
                for (int j = y - (y % 3); j < y - (y % 3) + 3; j++)
                {
                    if (i != x && j != y && cells[i, j].Value == value)
                        return false;
                }
            }

            return true;
        }
        
        private void checkButton_Click(object sender, EventArgs e)
        {
            bool hasError = false;

            foreach (var cell in cells)
                if (!string.Equals(cell.Value.ToString(), cell.Text))
                    hasError = true;

            if (hasError)
            {
                MessageBox.Show("Wrong inputs", "Result");
            }
            else
            {
                MessageBox.Show("Correct, unlocked 1 hint", "Result");

                if (session != null && session.Socket.Connected)
                {
                    var locationId = session.Locations.AllMissingLocations[Random.Next(0, session.Locations.AllMissingLocations.Count)];
                    session.Locations.ScoutLocationsAsync(true, locationId);
                }
            }
        }

        private void clearButton_Click(object sender, EventArgs e)
        {
            foreach (var cell in cells)
                if (cell.IsLocked == false)
                    cell.Clear();
        }

        private void newGameButton_Click(object sender, EventArgs e)
        {
            startNewGame();
        }

        private void ConnectButton_Click(object sender, EventArgs e)
        {
            if (session != null)
            {
                session = null;
                ConnectButton.Text = "Connect";
                UserText.Enabled = true;
                ServerText.Enabled = true;
                APLog.Clear();
            }

            var serverUri = ServerText.Text;

            if (!serverUri.StartsWith("ws://"))
                serverUri = "ws://" + serverUri;
            if (!serverUri.Contains(':'))
                serverUri += ":38281";
            else if (!serverUri.EndsWith(':'))
                serverUri += "38281";
            
            session = ArchipelagoSessionFactory.CreateSession(new Uri(serverUri));

            session.MessageLog.OnMessageReceived += MessageLog_OnMessageReceived;

            var result = session.TryConnectAndLogin("", UserText.Text, ItemsHandlingFlags.NoItems, tags: new[] { "BK_SUDOKU", "TEXT_ONLY" });

            if (!result.Successful)
            {
                MessageBox.Show(string.Join(',', ((LoginFailure)result).Errors), "Login Failed");
            }
            else
            {
                ConnectButton.Text = "Disconnect";
                UserText.Enabled = false;
                ServerText.Enabled = false;
            }
        }

        private void MessageLog_OnMessageReceived(Archipelago.MultiClient.Net.Helpers.LogMessage message)
        {
            if (message is not HintItemSendLogMessage hintMessage || hintMessage.SendingPlayerSlot != session.ConnectionInfo.Slot)
                return;

            foreach (var part in hintMessage.Parts)
            {
                APLog.ForeColor = part.Color;
                APLog.Text += part.Text;
            }

            APLog.Text += '\n';
        }
    }
}
