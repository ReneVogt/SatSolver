using Revo.BooleanAlgebra.Parsing;
using Revo.SatSolver;
using Revo.SatSolver.Parsing;
using System.Diagnostics;
using System.Text;

namespace SatSolverDemo
{
    public partial class MainForm : Form
    {
        readonly ToolTip _toolTip = new() { InitialDelay = 0 };

        bool _updating;
        CancellationTokenSource? _cancellationTokenSource;

        public MainForm()
        {
            InitializeComponent();
            rtbInput.Select();
        }

        private void OnInputModeChanged(object sender, EventArgs e)
        {
            tbCnf.Enabled = tbDimacs.Enabled = rbtBooleanAlgebra.Checked;
            tbCnf.BackColor = tbDimacs.BackColor = Color.FromKnownColor(rbtBooleanAlgebra.Checked ? KnownColor.Window : KnownColor.Control);
            _ = StartSolve();
        }
        private void OnInputChanged(object sender, EventArgs e)
        {
            if (_updating) return;
            tmSolve.Start();
        }

        private void OnSolveTimer(object sender, EventArgs e)
        {
            tmSolve.Stop();
            _ = StartSolve();
        }

        async Task StartSolve()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();
            try
            {
                pbSolving.Style = ProgressBarStyle.Marquee;
                var input = rtbInput.Text;
                var cancellationToken = _cancellationTokenSource.Token;
                var isAlgebra = rbtBooleanAlgebra.Checked;
                await Task.Run(() => ProcessInput(input, isAlgebra, cancellationToken), cancellationToken);
                pbSolving.Style = ProgressBarStyle.Blocks;
            }
            catch (OperationCanceledException)
            {
                // cancelled
            }
        }
        void ProcessInput(string input, bool isAlgebra, CancellationToken cancellationToken)
        {
            try
            {
                Problem problem;
                IReadOnlyDictionary<string, int>? mapping = null;

                BeginInvoke(ClearSyntaxErrors);
                BeginInvoke(() => UpdateCnf(string.Empty));
                BeginInvoke(() => UpdateDimacs(string.Empty));
                BeginInvoke(() => { dgvSolutions.Rows.Clear(); dgvSolutions.Columns.Clear(); });

                if (string.IsNullOrWhiteSpace(input))
                    return;

                if (isAlgebra)
                {
                    var expression = BooleanAlgebraParser.Parse(input);
                    if (cancellationToken.IsCancellationRequested) return;
                    problem = expression.ToProblem(out expression, out mapping);
                    if (cancellationToken.IsCancellationRequested) return;
                    BeginInvoke(() => UpdateCnf(expression.ToString()));
                    if (cancellationToken.IsCancellationRequested) return;
                    var dimacsBuilder = new StringBuilder(problem.ToString());
                    dimacsBuilder.AppendLine();
                    dimacsBuilder.AppendLine();
                    foreach (var kvp in mapping)
                        dimacsBuilder.AppendLine($"c {kvp.Value:D5}: {kvp.Key}");
                    BeginInvoke(() => UpdateDimacs(dimacsBuilder.ToString()));
                }
                else
                {
                    problem = DimacsParser.Parse(input).First();
                    BeginInvoke(ClearSyntaxErrors);
                }

                if (cancellationToken.IsCancellationRequested) return;

                BeginInvoke(() => InitializeSolutionList(problem, mapping));
                if (problem.NumberOfLiterals == 0) return;

                var watch = Stopwatch.StartNew();
                var solution = SatSolver.Solve(problem, cancellationToken: cancellationToken);
                var elapsed = watch.Elapsed;
                if (solution is not null)
                    BeginInvoke(() => AddSolution(solution, elapsed));
            }
            catch (Exception exception)
            {
                BeginInvoke(() => HandleProcessException(exception));
            }
        }

        sealed class LiteralNameComparer : IComparer<string?>
        {
            public int Compare(string? x, string? y)
            {
                if (x is null) return y is null ? 0 : -1;
                if (y is null) return 1;

                if (!(x.StartsWith('.') || y.StartsWith('.'))) return x.CompareTo(y);
                if (!x.StartsWith('.')) return -1;
                if (!y.StartsWith('.')) return 1;

                return int.Parse(x[2..]).CompareTo(int.Parse(y[2..]));
            }
            public static LiteralNameComparer Default { get; } = new();
        }

        void InitializeSolutionList(Problem problem, IReadOnlyDictionary<string, int>? mapping)
        {
            dgvSolutions.SuspendLayout();
            dgvSolutions.Rows.Clear();
            dgvSolutions.Columns.Clear();

            var columnNames = mapping?.Keys.OrderBy(name => mapping[name]).ToArray() ?? [.. Enumerable.Range(1, problem.NumberOfLiterals).Select(i => i.ToString())];
            foreach (var name in columnNames)
                dgvSolutions.Columns.Add(new DataGridViewColumn { HeaderText = name });
            foreach (var (column, displayIndex) in dgvSolutions.Columns.Cast<DataGridViewColumn>().OrderBy(column => column.HeaderText, LiteralNameComparer.Default).Select((column, index) => (column, index))) column.DisplayIndex = displayIndex;
            dgvSolutions.ResumeLayout(true);
        }
        void AddSolution(Literal[] solution, TimeSpan elapsed)
        {
            tbDimacs.AppendText($"{Environment.NewLine}c Solved after {elapsed}:");
            tbDimacs.AppendText($"{Environment.NewLine}s {string.Join(" ", solution.Select(literal => literal.Sense ? literal.Id : -literal.Id))} 0");
            var literals = solution.ToDictionary(l => l.Id, l => l.Sense);
            var row = new DataGridViewRow();
            foreach (var color in Enumerable.Range(1, dgvSolutions.Columns.Count)
                .Select(i => literals.TryGetValue(i, out var sense) ? sense ? Color.Green : Color.Red : Color.FromKnownColor(KnownColor.Window)))
                row.Cells.Add(new DataGridViewTextBoxCell
                {
                    Style = new DataGridViewCellStyle
                    {
                        BackColor = color,
                        SelectionBackColor = color,
                        SelectionForeColor = color
                    }
                });
            dgvSolutions.Rows.Add(row);
        }

        void UpdateCnf(string cnf)
        {
            ClearSyntaxErrors();
            tbCnf.Text = cnf;
        }
        void UpdateDimacs(string dimacs)
        {
            ClearSyntaxErrors();
            tbDimacs.Text = dimacs;
        }
        void ClearSyntaxErrors()
        {
            _toolTip.RemoveAll();
            _updating = true;
            rtbInput.SuspendLayout();
            var originalStart = rtbInput.SelectionStart;
            var originalLength = rtbInput.SelectionLength;
            rtbInput.SelectionStart = 0;
            rtbInput.SelectionLength = rtbInput.Text.Length;
            rtbInput.SelectionBackColor = Color.FromKnownColor(KnownColor.Window);
            rtbInput.SelectionStart = originalStart;
            rtbInput.SelectionLength = originalLength;
            rtbInput.ResumeLayout();
            _updating = false;
        }
        void SetCnfSyntaxError(int line, int position, string message) => SetSyntaxError(rtbInput.GetFirstCharIndexFromLine(line) + position, message);
        void SetSyntaxError(int position, string message)
        {
            _toolTip.SetToolTip(rtbInput, message);
            _toolTip.Show(message, rtbInput);
            _updating = true;
            rtbInput.SuspendLayout();
            var originalStart = rtbInput.SelectionStart;
            var originalLength = rtbInput.SelectionLength;
            try
            {
                rtbInput.SelectionStart = 0;
                rtbInput.SelectionLength = rtbInput.Text.Length;
                rtbInput.SelectionBackColor = Color.FromKnownColor(KnownColor.Window);
                rtbInput.SelectionStart = Math.Max(0, Math.Min(position, rtbInput.Text.Length - 1));
                rtbInput.SelectionLength = 1;
                rtbInput.SelectionBackColor = Color.LightPink;
            }
            finally
            {
                rtbInput.SelectionStart = originalStart;
                rtbInput.SelectionLength = originalLength;
                rtbInput.ResumeLayout();
                _updating = false;
            }

        }
        void HandleProcessException(Exception exception)
        {
            switch (exception)
            {
                case OperationCanceledException: return;
                case DimacsException { Line: var line, Position: var position }: SetCnfSyntaxError(line, position, exception.Message); return;
                case InvalidBooleanAlgebraException { Position: var position }: SetSyntaxError(position, exception.Message); return;
                default: MessageBox.Show($"Unexpected error: {exception}", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error); return;
            }
        }
    }
}
