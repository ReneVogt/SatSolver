namespace SatSolverDemo
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void OnInputModeChanged(object sender, EventArgs e)
        {
            tbCnf.Enabled = tbDimacs.Enabled = rbtBooleanAlgebra.Checked;
            tbCnf.BackColor = tbDimacs.BackColor = Color.FromKnownColor(rbtBooleanAlgebra.Checked ? KnownColor.Window : KnownColor.Control);
        }
        private void OnInputChanged(object sender, EventArgs e)
        {
            tmSolve.Start();
        }

        private void OnSolveTimer(object sender, EventArgs e)
        {
            tmSolve.Stop();
        }
    }
}
