﻿namespace SatSolverDemo
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            var resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            rbtBooleanAlgebra = new RadioButton();
            rbtDimacs = new RadioButton();
            rtbInput = new RichTextBox();
            tbCnf = new TextBox();
            label1 = new Label();
            tbDimacs = new TextBox();
            label2 = new Label();
            label3 = new Label();
            tmSolve = new System.Windows.Forms.Timer(components);
            pbSolving = new ProgressBar();
            outerContainer = new SplitContainer();
            innerContainer = new SplitContainer();
            dgvSolutions = new DataGridView();
            ((System.ComponentModel.ISupportInitialize)outerContainer).BeginInit();
            outerContainer.Panel1.SuspendLayout();
            outerContainer.Panel2.SuspendLayout();
            outerContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)innerContainer).BeginInit();
            innerContainer.Panel1.SuspendLayout();
            innerContainer.Panel2.SuspendLayout();
            innerContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvSolutions).BeginInit();
            SuspendLayout();
            // 
            // rbtBooleanAlgebra
            // 
            rbtBooleanAlgebra.AutoSize = true;
            rbtBooleanAlgebra.Checked = true;
            rbtBooleanAlgebra.Location = new Point(11, 9);
            rbtBooleanAlgebra.Margin = new Padding(2);
            rbtBooleanAlgebra.Name = "rbtBooleanAlgebra";
            rbtBooleanAlgebra.Size = new Size(110, 19);
            rbtBooleanAlgebra.TabIndex = 1;
            rbtBooleanAlgebra.TabStop = true;
            rbtBooleanAlgebra.Text = "Boolean algebra";
            rbtBooleanAlgebra.UseVisualStyleBackColor = true;
            rbtBooleanAlgebra.CheckedChanged += OnInputModeChanged;
            // 
            // rbtDimacs
            // 
            rbtDimacs.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            rbtDimacs.AutoSize = true;
            rbtDimacs.CheckAlign = ContentAlignment.MiddleRight;
            rbtDimacs.Location = new Point(495, 9);
            rbtDimacs.Margin = new Padding(2);
            rbtDimacs.Name = "rbtDimacs";
            rbtDimacs.Size = new Size(69, 19);
            rbtDimacs.TabIndex = 2;
            rbtDimacs.Text = "DIMACS";
            rbtDimacs.UseVisualStyleBackColor = true;
            rbtDimacs.CheckedChanged += OnInputModeChanged;
            // 
            // rtbInput
            // 
            rtbInput.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            rtbInput.Location = new Point(11, 29);
            rtbInput.Margin = new Padding(2);
            rtbInput.Name = "rtbInput";
            rtbInput.ScrollBars = RichTextBoxScrollBars.Vertical;
            rtbInput.Size = new Size(553, 74);
            rtbInput.TabIndex = 3;
            rtbInput.Text = "";
            rtbInput.TextChanged += OnInputChanged;
            // 
            // tbCnf
            // 
            tbCnf.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            tbCnf.BackColor = SystemColors.Window;
            tbCnf.Location = new Point(11, 28);
            tbCnf.Margin = new Padding(2);
            tbCnf.Multiline = true;
            tbCnf.Name = "tbCnf";
            tbCnf.ReadOnly = true;
            tbCnf.ScrollBars = ScrollBars.Vertical;
            tbCnf.Size = new Size(553, 102);
            tbCnf.TabIndex = 4;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(11, 8);
            label1.Margin = new Padding(2, 0, 2, 0);
            label1.Name = "label1";
            label1.Size = new Size(144, 15);
            label1.TabIndex = 5;
            label1.Text = "Conjunctive normal form:";
            // 
            // tbDimacs
            // 
            tbDimacs.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            tbDimacs.BackColor = SystemColors.Window;
            tbDimacs.Location = new Point(11, 30);
            tbDimacs.Margin = new Padding(2);
            tbDimacs.Multiline = true;
            tbDimacs.Name = "tbDimacs";
            tbDimacs.ReadOnly = true;
            tbDimacs.ScrollBars = ScrollBars.Both;
            tbDimacs.Size = new Size(548, 127);
            tbDimacs.TabIndex = 6;
            tbDimacs.WordWrap = false;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(11, 11);
            label2.Margin = new Padding(2, 0, 2, 0);
            label2.Name = "label2";
            label2.Size = new Size(54, 15);
            label2.TabIndex = 7;
            label2.Text = "DIMACS:";
            // 
            // label3
            // 
            label3.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            label3.AutoSize = true;
            label3.Location = new Point(12, 168);
            label3.Margin = new Padding(2, 0, 2, 0);
            label3.Name = "label3";
            label3.Size = new Size(54, 15);
            label3.TabIndex = 8;
            label3.Text = "Solution:";
            // 
            // tmSolve
            // 
            tmSolve.Interval = 500;
            tmSolve.Tick += OnSolveTimer;
            // 
            // pbSolving
            // 
            pbSolving.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            pbSolving.Location = new Point(0, 242);
            pbSolving.MarqueeAnimationSpeed = 10;
            pbSolving.Name = "pbSolving";
            pbSolving.Size = new Size(575, 13);
            pbSolving.TabIndex = 10;
            // 
            // outerContainer
            // 
            outerContainer.Dock = DockStyle.Fill;
            outerContainer.Location = new Point(0, 0);
            outerContainer.Name = "outerContainer";
            outerContainer.Orientation = Orientation.Horizontal;
            // 
            // outerContainer.Panel1
            // 
            outerContainer.Panel1.Controls.Add(innerContainer);
            // 
            // outerContainer.Panel2
            // 
            outerContainer.Panel2.Controls.Add(dgvSolutions);
            outerContainer.Panel2.Controls.Add(label2);
            outerContainer.Panel2.Controls.Add(pbSolving);
            outerContainer.Panel2.Controls.Add(tbDimacs);
            outerContainer.Panel2.Controls.Add(label3);
            outerContainer.Size = new Size(575, 526);
            outerContainer.SplitterDistance = 262;
            outerContainer.TabIndex = 11;
            // 
            // innerContainer
            // 
            innerContainer.Dock = DockStyle.Fill;
            innerContainer.Location = new Point(0, 0);
            innerContainer.Name = "innerContainer";
            innerContainer.Orientation = Orientation.Horizontal;
            // 
            // innerContainer.Panel1
            // 
            innerContainer.Panel1.Controls.Add(rbtBooleanAlgebra);
            innerContainer.Panel1.Controls.Add(rbtDimacs);
            innerContainer.Panel1.Controls.Add(rtbInput);
            // 
            // innerContainer.Panel2
            // 
            innerContainer.Panel2.Controls.Add(label1);
            innerContainer.Panel2.Controls.Add(tbCnf);
            innerContainer.Size = new Size(575, 262);
            innerContainer.SplitterDistance = 116;
            innerContainer.TabIndex = 0;
            // 
            // dgvSolutions
            // 
            dgvSolutions.AllowUserToAddRows = false;
            dgvSolutions.AllowUserToDeleteRows = false;
            dgvSolutions.AllowUserToResizeColumns = false;
            dgvSolutions.AllowUserToResizeRows = false;
            dgvSolutions.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dgvSolutions.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.ColumnHeader;
            dgvSolutions.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvSolutions.Location = new Point(12, 186);
            dgvSolutions.Name = "dgvSolutions";
            dgvSolutions.ReadOnly = true;
            dgvSolutions.RowHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dgvSolutions.RowHeadersVisible = false;
            dgvSolutions.SelectionMode = DataGridViewSelectionMode.CellSelect;
            dgvSolutions.ShowCellErrors = false;
            dgvSolutions.ShowCellToolTips = false;
            dgvSolutions.ShowEditingIcon = false;
            dgvSolutions.ShowRowErrors = false;
            dgvSolutions.Size = new Size(552, 62);
            dgvSolutions.TabIndex = 11;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(575, 526);
            Controls.Add(outerContainer);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MinimizeBox = false;
            MinimumSize = new Size(498, 393);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "SatSolver Demo";
            outerContainer.Panel1.ResumeLayout(false);
            outerContainer.Panel2.ResumeLayout(false);
            outerContainer.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)outerContainer).EndInit();
            outerContainer.ResumeLayout(false);
            innerContainer.Panel1.ResumeLayout(false);
            innerContainer.Panel1.PerformLayout();
            innerContainer.Panel2.ResumeLayout(false);
            innerContainer.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)innerContainer).EndInit();
            innerContainer.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvSolutions).EndInit();
            ResumeLayout(false);
        }

        #endregion
        private RadioButton rbtBooleanAlgebra;
        private RadioButton rbtDimacs;
        private RichTextBox rtbInput;
        private TextBox tbCnf;
        private Label label1;
        private TextBox tbDimacs;
        private Label label2;
        private Label label3;
        private System.Windows.Forms.Timer tmSolve;
        private ProgressBar pbSolving;
        private SplitContainer outerContainer;
        private SplitContainer innerContainer;
        private DataGridView dgvSolutions;
    }
}
