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
        public Form1()
        {
            // Це стандартний метод, який малює кнопки.
            InitializeComponent();
            InitializeUI();
        }
        private void InitializeUI()
        {
            dgvMatrix.EditingControlShowing += dgvMatrix_EditingControlShowing;
            dgvMatrix.CellValidating += dgvMatrix_CellValidating;
            // Налаштування ComboBox.
            cmbMethod.Items.Clear();
            cmbMethod.Items.Add(new KruskalAlgorithm());
            cmbMethod.Items.Add(new PrimAlgorithm());
            cmbMethod.Items.Add(new BoruvkaAlgorithm());
            cmbMethod.Items.Add("Бенчмаркінг (Усі методи)");
            cmbMethod.DisplayMember = "Name";
            cmbMethod.SelectedIndex = 0;
            dgvMatrix.CellValidating += dgvMatrix_CellValidating;

            // --- НОВІ НАЛАШТУВАННЯ ДЛЯ DataGridView ---

            // 1. Увімкнення подвійної буферизації для плавного скролінгу (рефлексія)
            typeof(DataGridView).InvokeMember("DoubleBuffered",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.SetProperty,
                null, dgvMatrix, new object[] { true });

            // 2. Прибираємо зайве
            dgvMatrix.AllowUserToAddRows = false;       // Забирає порожній сірий рядок знизу
            dgvMatrix.AllowUserToDeleteRows = false;
            dgvMatrix.AllowUserToResizeRows = false;
            dgvMatrix.AllowUserToResizeColumns = false;
            dgvMatrix.RowHeadersVisible = true;         // Показувати бокові підписи
            dgvMatrix.ColumnHeadersVisible = true;      // Показувати верхні підписи

            // 3. Косметичні покращення
            dgvMatrix.BackgroundColor = Color.White;    // Білий фон замість сірого
            dgvMatrix.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            dgvMatrix.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter; // Текст по центру клітинок
            dgvMatrix.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            // Ініціалізуємо базову матрицю 5x5
            UpdateMatrixHeaders(5);
            nudGraphSize.Minimum = 2;
            nudGraphSize.Maximum = 20;
            nudGraphSize.Value = 5;
        }

        private void dgvMatrix_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            if (e.Control is TextBox tb)
            {
                tb.KeyPress -= TextBox_KeyPress; // Запобігаємо дублюванню
                tb.KeyPress += TextBox_KeyPress;
            }
        }

        private void TextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '.')
            {
                e.KeyChar = ','; // Миттєво замінюємо крапку на кому
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

            int cellSize = 30; // Ідеальний розмір для квадратної клітинки

            for (int i = 0; i < size; i++)
            {
                string headerName = ((char)('A' + i)).ToString();

                // Налаштовуємо стовпці (верхні підписи)
                dgvMatrix.Columns[i].HeaderText = headerName;
                dgvMatrix.Columns[i].Width = cellSize; // Фіксуємо ширину
                dgvMatrix.Columns[i].SortMode = DataGridViewColumnSortMode.NotSortable; // Забороняємо сортування по кліку

                // Налаштовуємо рядки (бокові підписи)
                dgvMatrix.Rows[i].HeaderCell.Value = headerName;
                dgvMatrix.Rows[i].Height = cellSize; // Фіксуємо висоту
            }

            // Автоматично підганяємо ширину бокового стовпця з літерами
            dgvMatrix.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.AutoSizeToAllHeaders;
        }

        // Натискання на кнопку "Розрахувати"
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
                    // Використання оператора 'as' для безпечного приведення типів
                    solver = selectedAlgorithm as MstAlgorithm;

                    // ОСЬ ТУТ ЗМІНЕНО: out double weight замість out int weight
                    mstEdges = solver.FindMST(currentGraph, out double weight, out int iters);

                    lblResult.Text = $"Метод: {solver.Name}\nВага: {weight}\nІтерацій: {iters}";
                    DrawGraph();
                    SaveResultsToFile(solver.Name, weight, iters);
                }
                else // Якщо вибрано рядок "Бенчмаркінг", а не об'єкт класу
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

            // --- НОВА ПЕРЕВІРКА НА КІЛЬКІСТЬ ЗНАКІВ ---
            // Використовуємо decimal для уникнення бінарних похибок (наприклад, коли 2.3 у пам'яті стає 2.29999999)
            decimal decValue = (decimal)value;

            // Якщо округлене до 2 знаків число не дорівнює введеному оригіналу, отже, знаків було більше!
            // (Якщо захочеш 3 знаки — просто зміни цифру 2 на 3 у дужках нижче)
            if (Math.Round(decValue, 2) != decValue)
            {
                e.Cancel = true;
                MessageBox.Show("Дозволяється вводити не більше 2 знаків після коми.",
                                "Помилка вводу", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            // ------------------------------------------

            // Змінюємо мінімальний ліміт на 0.01 (бо 0.001 тепер не пройде перевірку вище)
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
                        // ТРЮК: Нормалізуємо текст перед зчитуванням
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
            // Поліморфізм: масив базового типу зберігає об'єкти спадкоємців
            MstAlgorithm[] algorithms = { new PrimAlgorithm(), new KruskalAlgorithm(), new BoruvkaAlgorithm() };

            foreach (var alg in algorithms)
            {
                // ОСЬ ТУТ ЗМІНЕНО: out double w замість out int w
                var edges = alg.FindMST(currentGraph, out double w, out int iters);
                report += $"{alg.Name}: Вага = {w}, Ітерацій = {iters}\n";
                mstEdges = edges; // Залишаємо останнє дерево для відображення
            }

            lblResult.Text = report;
            DrawGraph();
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

                // Розставляємо вершини по колу
                for (int i = 0; i < n; i++)
                {
                    double angle = 2 * Math.PI * i / n;
                    positions[i] = new Point((int)(cx + radius * Math.Cos(angle)), (int)(cy + radius * Math.Sin(angle)));
                }

                // Малюємо всі ребра (сірі)
                var allEdges = currentGraph.GetAllEdges();
                Pen grayPen = new Pen(Color.LightGray, 2);
                foreach (var edge in allEdges)
                {
                    g.DrawLine(grayPen, positions[edge.U], positions[edge.V]);
                }

                // Малюємо ребра дерева (зелені)
                if (mstEdges != null)
                {
                    Pen greenPen = new Pen(Color.Green, 3);
                    foreach (var edge in mstEdges)
                    {
                        g.DrawLine(greenPen, positions[edge.U], positions[edge.V]);
                    }
                }

                // Малюємо вузли
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
            // Формуємо запис. У консолі чи файлі зразу видно, чия це робота.
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
                // Викликаємо наш новий метод замість прямого задавання RowCount/ColumnCount
                UpdateMatrixHeaders(newSize);
            }
        }

        private void nudGraphSize_Click(object sender, EventArgs e)
        {

        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            // Перевіряємо, чи є що зберігати
            if (string.IsNullOrWhiteSpace(lblResult.Text) || lblResult.Text.Contains("результат"))
            {
                MessageBox.Show("Спочатку розрахуйте остовне дерево!", "Увага", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                MessageBox.Show($"Дані успішно збережено у файл {Constants.DefaultExportFileName}", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка при збереженні файлу: " + ex.Message, "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
