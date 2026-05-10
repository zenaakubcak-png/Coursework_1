using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
namespace Курсова_1_курс
{
    public partial class Form1 : Form
    {
        private Graph currentGraph;
        private List<Edge> mstEdges;
        private MstAlgorithm solver;
        private bool _hasResult = false;
        public Form1()
        {
            
            InitializeComponent();
            InitializeUI();
        }
        private void InitializeUI()
        {
            dgvMatrix.EditingControlShowing += dgvMatrix_EditingControlShowing;
            dgvMatrix.CellValidating += dgvMatrix_CellValidating;
            
            cmbMethod.Items.Clear();
            cmbMethod.Items.Add(new KruskalAlgorithm());
            cmbMethod.Items.Add(new PrimAlgorithm());
            cmbMethod.Items.Add(new BoruvkaAlgorithm());
            cmbMethod.Items.Add("Бенчмаркінг (Усі методи)");
            cmbMethod.DisplayMember = "Name";
            cmbMethod.SelectedIndex = 0;
            dgvMatrix.CellValidating += dgvMatrix_CellValidating;

            
            typeof(DataGridView).InvokeMember("DoubleBuffered",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.SetProperty,
                null, dgvMatrix, new object[] { true });

            dgvMatrix.AllowUserToAddRows = false;       
            dgvMatrix.AllowUserToDeleteRows = false;
            dgvMatrix.AllowUserToResizeRows = false;
            dgvMatrix.AllowUserToResizeColumns = false;
            dgvMatrix.RowHeadersVisible = true;         
            dgvMatrix.ColumnHeadersVisible = true;      

            dgvMatrix.BackgroundColor = Color.White;    
            dgvMatrix.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            dgvMatrix.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter; 
            dgvMatrix.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            
            UpdateMatrixHeaders(5);
            nudGraphSize.Minimum = 2;
            nudGraphSize.Maximum = 20;
            nudGraphSize.Value = 5;
        }

        private void dgvMatrix_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            if (e.Control is TextBox tb)
            {
                tb.KeyPress -= TextBox_KeyPress; 
                tb.KeyPress += TextBox_KeyPress;
            }
        }

        private void TextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '.')
            {
                e.KeyChar = ','; 
                e.Handled = true;
                TextBox tb = sender as TextBox;
                int pos = tb.SelectionStart;
                tb.Text = tb.Text.Insert(pos, ",");
                tb.SelectionStart = pos + 1;
            }
        }
        private void UpdateMatrixHeaders(int size)
        {
            dgvMatrix.RowCount = size;
            dgvMatrix.ColumnCount = size;

            int cellSize = 30; 

            for (int i = 0; i < size; i++)
            {
                string headerName = ((char)('A' + i)).ToString();

                dgvMatrix.Columns[i].HeaderText = headerName;
                dgvMatrix.Columns[i].Width = cellSize; 
                dgvMatrix.Columns[i].SortMode = DataGridViewColumnSortMode.NotSortable; 

                dgvMatrix.Rows[i].HeaderCell.Value = headerName;
                dgvMatrix.Rows[i].Height = cellSize; 
            }

            dgvMatrix.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.AutoSizeToAllHeaders;
        }

        private void btnCalculate_Click(object sender, EventArgs e)
        {
            try
            {
                ReadGraphFromGrid();
                if (currentGraph.GetAllEdges().Count == 0)
                {
                    MessageBox.Show(
                        "Граф не містить жодного ребра!\nВведіть хоча б одне ненульове значення у матрицю.",
                        "Порожній граф",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }
                if (cmbMethod.SelectedItem is MstAlgorithm selectedAlgorithm)
                {
                    
                    solver = selectedAlgorithm as MstAlgorithm;

                    
                    mstEdges = solver.FindMST(currentGraph, out double weight, out int iters);

                    lblResult.Text = $"Метод: {solver.Name}\nВага: {weight}\nІтерацій: {iters}";
                    _hasResult = true;
                    DrawGraph();
                    SaveResultsToFile(solver.Name, weight, iters);
                }
                else 
                {
                    RunBenchmark();
                }
            }
            catch (GraphException ex)
            {
                MessageBox.Show(ex.Message, "Помилка графа", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception)
            {
                MessageBox.Show("Перевірте правильність введених даних у матрицю. Мають бути лише числа.", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void dgvMatrix_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            string input = e.FormattedValue?.ToString();
            if (input != null)
                input = input.Replace('.', ',');

            if (string.IsNullOrWhiteSpace(input))
                return;

            string normalizedInput = input.Replace(',', '.');

            bool isNumber = double.TryParse(normalizedInput,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out double value);

            if (!isNumber)
            {
                e.Cancel = true;
                MessageBox.Show("Введіть коректне число (наприклад: 5, -3, 2.3 або 2,3).",
                                "Помилка вводу", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            decimal decValue = (decimal)value;

            if (Math.Round(decValue, 2) != decValue)
            {
                e.Cancel = true;
                MessageBox.Show("Дозволяється вводити не більше 2 знаків після коми.",
                                "Помилка вводу", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            if (value != 0 && Math.Abs(value) < 0.01)
            {
                e.Cancel = true;
                MessageBox.Show("Число занадто мале. Мінімальне значення дробу: 0.01 (або -0.01).",
                                "Помилка вводу", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            if (value < -1000 || value > 1000)
            {
                e.Cancel = true;
                MessageBox.Show("Вага ребра має бути в межах від -1000 до 1000.",
                                "Помилка вводу", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ReadGraphFromGrid()
        {
            int size = dgvMatrix.RowCount;
            currentGraph = new Graph(size);

            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    var cellValue = dgvMatrix.Rows[i].Cells[j].Value;
                    if (cellValue != null && !string.IsNullOrWhiteSpace(cellValue.ToString()))
                    {
                        
                        string normalizedString = cellValue.ToString().Replace(',', '.');

                        if (double.TryParse(normalizedString,
                            System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture,
                            out double parsedValue))
                        {
                            currentGraph[i, j] = parsedValue;
                            currentGraph[j, i] = parsedValue;
                        }
                    }
                }
            }
        }

        private void RunBenchmark()
        {
            string report = "=== Бенчмаркінг ===\n";
            MstAlgorithm[] algorithms = { new PrimAlgorithm(), new KruskalAlgorithm(), new BoruvkaAlgorithm() };

            foreach (var alg in algorithms)
            {
                var edges = alg.FindMST(currentGraph, out double w, out int iters);
                report += $"{alg.Name}: Вага = {w}, Ітерацій = {iters}\n";
                mstEdges = edges; // Залишаємо останнє дерево для відображення
            }

            lblResult.Text = report;
            DrawGraph();
            _hasResult = true;
        }

        private void DrawGraph()
        {
            if (pictureBox.Width == 0 || pictureBox.Height == 0) return;

            Bitmap bmp = new Bitmap(pictureBox.Width, pictureBox.Height);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                int n = currentGraph.VerticesCount;
                Point[] positions = new Point[n];
                int cx = pictureBox.Width / 2;
                int cy = pictureBox.Height / 2;
                int radius = Math.Min(cx, cy) - 30;

                for (int i = 0; i < n; i++)
                {
                    double angle = 2 * Math.PI * i / n;
                    positions[i] = new Point((int)(cx + radius * Math.Cos(angle)), (int)(cy + radius * Math.Sin(angle)));
                }

                var allEdges = currentGraph.GetAllEdges();
                Pen grayPen = new Pen(Color.LightGray, 2);
                foreach (var edge in allEdges)
                {
                    g.DrawLine(grayPen, positions[edge.U], positions[edge.V]);
                }

                if (mstEdges != null)
                {
                    Pen greenPen = new Pen(Color.Green, 3);
                    foreach (var edge in mstEdges)
                    {
                        g.DrawLine(greenPen, positions[edge.U], positions[edge.V]);
                    }
                }

                Font font = new Font("Arial", 12, FontStyle.Bold);
                for (int i = 0; i < n; i++)
                {
                    Rectangle rect = new Rectangle(positions[i].X - Constants.NodeRadius, positions[i].Y - Constants.NodeRadius, Constants.NodeRadius * 2, Constants.NodeRadius * 2);
                    g.FillEllipse(Brushes.White, rect);
                    g.DrawEllipse(Pens.Black, rect);
                    g.DrawString(((char)('A' + i)).ToString(), font, Brushes.Black, positions[i].X - 8, positions[i].Y - 8);
                }
            }
            pictureBox.Image = bmp;
        }

        private void SaveResultsToFile(string method, double weight, int iterations)
        {
            using (StreamWriter sw = new StreamWriter(Constants.DefaultExportFileName, true))
            {
                sw.WriteLine($"[{DateTime.Now}] Розробив: Євген Якубчак, група ІП-55.");
                sw.WriteLine($"Метод: {method} | Вага: {weight} | Ітерацій: {iterations}\n");
            }
        }

        private void picCanvas_Paint(object sender, PaintEventArgs e)
        {

        }

        private void nudGraphSize_ValueChanged(object sender, EventArgs e)
        {
            int newSize = (int)nudGraphSize.Value;

            // Обмежуємо розмір графа, щоб уникнути помилок
            if (newSize > 0 && newSize <= Constants.MaxVertices)
            {
                UpdateMatrixHeaders(newSize);
            }
        }

        private void nudGraphSize_Click(object sender, EventArgs e)
        {

        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (!_hasResult)
            {
                MessageBox.Show(
                    "Спочатку розрахуйте остовне дерево!",
                    "Увага",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (StreamWriter sw = new StreamWriter(Constants.DefaultExportFileName, true))
                {
                    sw.WriteLine($"[{DateTime.Now}] Розробив: Євген Якубчак, група ІП-55.");
                    sw.WriteLine(lblResult.Text);
                    sw.WriteLine(new string('-', 40));
                }
                MessageBox.Show(
                    $"Дані успішно збережено у файл {Constants.DefaultExportFileName}",
                    "Готово",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Помилка при збереженні файлу: " + ex.Message,
                    "Помилка",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}
