using Revo.BooleanAlgebra.Parsing;
using Revo.BooleanAlgebra.Transformers;
using Revo.SatSolver;
using Revo.SatSolver.Parsing;
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
                BeginInvoke(UpdateCnf, string.Empty);
                BeginInvoke(UpdateDimacs, string.Empty);
                BeginInvoke(() => { lvSolutions.Items.Clear(); lvSolutions.Columns.Clear(); });

                if (string.IsNullOrWhiteSpace(input))
                    return;

                if (isAlgebra)
                {
                    var expression = BooleanAlgebraParser.Parse(input);
                    if (cancellationToken.IsCancellationRequested) return;
                    expression = TseitinTransformer.Transform(expression);
                    if (cancellationToken.IsCancellationRequested) return;
                    expression = RedundancyReducer.Reduce(expression);
                    if (cancellationToken.IsCancellationRequested) return;

                    problem = expression.ToProblem(out expression, out mapping);
                    BeginInvoke(UpdateCnf, expression.ToString());
                    if (cancellationToken.IsCancellationRequested) return;
                    var dimacsBuilder = new StringBuilder(problem.ToString());
                    dimacsBuilder.AppendLine();
                    dimacsBuilder.AppendLine();
                    foreach (var kvp in mapping)
                        dimacsBuilder.AppendLine($"c {kvp.Value:D5}: {kvp.Key}");
                    BeginInvoke(UpdateDimacs, dimacsBuilder.ToString());
                }
                else
                {
                    problem = DimacsParser.Parse(input).First();
                    BeginInvoke(ClearSyntaxErrors);
                }

                if (cancellationToken.IsCancellationRequested) return;

                BeginInvoke(InitializeSolutionList, problem, mapping);
                foreach (var solution in SatSolver.Solve(problem, cancellationToken))
                    BeginInvoke(AddSolution, solution, mapping);
            }
            catch (Exception exception)
            {
                BeginInvoke(HandleProcessException, exception);
            }
        }

        void InitializeSolutionList(Problem problem, IReadOnlyDictionary<string, int>? mapping)
        {
            lvSolutions.SuspendLayout();
            lvSolutions.Columns.Clear();
            lvSolutions.Items.Clear();
            var columnNames = mapping?.Keys.OrderBy(k => k.StartsWith('.')).ThenBy(k => k).ToArray() ?? Enumerable.Range(1, problem.NumberOfLiterals).Select(i => i.ToString()).ToArray();
            foreach (var name in columnNames) lvSolutions.Columns.Add(name);
            lvSolutions.Columns.Add(string.Empty);
            lvSolutions.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
            ShowOrHideTseitinColumns();
            lvSolutions.ResumeLayout();
        }
        void AddSolution(Literal[] solution, IReadOnlyDictionary<string, int>? mapping)
        {
            tbDimacs.AppendText($"{Environment.NewLine}s {string.Join(" ", solution.Select(literal => literal.Sense ? literal.Id : -literal.Id))} 0");
            var literals = solution.ToDictionary(l => l.Id, l => l.Sense);
            lvSolutions.Items.Add(new ListViewItem(Enumerable.Range(1, lvSolutions.Columns.Count).Select(i => new ListViewItem.ListViewSubItem { BackColor = literals.TryGetValue(i, out var sense) ? sense ? Color.Green : Color.Red : DefaultBackColor }).ToArray(), -1));
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

        private void OnDrawSolutionSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            if (e is not { SubItem.BackColor: var color }) return;
            using var brush = new SolidBrush(color);
            e.Graphics.FillRectangle(brush, e.Bounds);
        }
        private void OnDrawSolutionColumn(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            e.DrawDefault = true;
        }

        private void OnShowTseitinChanged(object sender, EventArgs e)
        {
            ShowOrHideTseitinColumns();
        }
        void ShowOrHideTseitinColumns()
        {
            if (cbTseitin.Checked)
            {
                lvSolutions.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
                return;
            }

            lvSolutions.SuspendLayout();
            foreach (var column in lvSolutions.Columns.Cast<ColumnHeader>().Where(column => column.Text.StartsWith('.')))
                column.Width = 0;
            lvSolutions.ResumeLayout();
        }
    }
}
