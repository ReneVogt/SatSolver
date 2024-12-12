namespace SatSolverDemo
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
            lvSolutions = new ListView();
            tmSolve = new System.Windows.Forms.Timer(components);
            SuspendLayout();
            // 
            // rbtBooleanAlgebra
            // 
            rbtBooleanAlgebra.AutoSize = true;
            rbtBooleanAlgebra.Checked = true;
            rbtBooleanAlgebra.Location = new Point(12, 12);
            rbtBooleanAlgebra.Name = "rbtBooleanAlgebra";
            rbtBooleanAlgebra.Size = new Size(165, 29);
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
            rbtDimacs.Location = new Point(924, 12);
            rbtDimacs.Name = "rbtDimacs";
            rbtDimacs.Size = new Size(104, 29);
            rbtDimacs.TabIndex = 2;
            rbtDimacs.Text = "DIMACS";
            rbtDimacs.UseVisualStyleBackColor = true;
            rbtDimacs.CheckedChanged += OnInputModeChanged;
            // 
            // rtbInput
            // 
            rtbInput.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            rtbInput.Location = new Point(12, 47);
            rtbInput.Name = "rtbInput";
            rtbInput.Size = new Size(1016, 118);
            rtbInput.TabIndex = 3;
            rtbInput.Text = "";
            rtbInput.TextChanged += OnInputChanged;
            // 
            // tbCnf
            // 
            tbCnf.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            tbCnf.BackColor = SystemColors.Window;
            tbCnf.Location = new Point(12, 206);
            tbCnf.Multiline = true;
            tbCnf.Name = "tbCnf";
            tbCnf.ReadOnly = true;
            tbCnf.ScrollBars = ScrollBars.Vertical;
            tbCnf.Size = new Size(1016, 93);
            tbCnf.TabIndex = 4;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(12, 178);
            label1.Name = "label1";
            label1.Size = new Size(213, 25);
            label1.TabIndex = 5;
            label1.Text = "Conjunctive normal form:";
            // 
            // tbDimacs
            // 
            tbDimacs.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            tbDimacs.BackColor = SystemColors.Window;
            tbDimacs.Location = new Point(12, 347);
            tbDimacs.Multiline = true;
            tbDimacs.Name = "tbDimacs";
            tbDimacs.ReadOnly = true;
            tbDimacs.ScrollBars = ScrollBars.Both;
            tbDimacs.Size = new Size(397, 279);
            tbDimacs.TabIndex = 6;
            tbDimacs.WordWrap = false;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(12, 319);
            label2.Name = "label2";
            label2.Size = new Size(83, 25);
            label2.TabIndex = 7;
            label2.Text = "DIMACS:";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(425, 319);
            label3.Name = "label3";
            label3.Size = new Size(90, 25);
            label3.TabIndex = 8;
            label3.Text = "Solutions:";
            // 
            // lvSolutions
            // 
            lvSolutions.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            lvSolutions.Location = new Point(425, 347);
            lvSolutions.Name = "lvSolutions";
            lvSolutions.Size = new Size(603, 279);
            lvSolutions.TabIndex = 9;
            lvSolutions.UseCompatibleStateImageBehavior = false;
            lvSolutions.View = View.Details;
            // 
            // tmSolve
            // 
            tmSolve.Interval = 500;
            tmSolve.Tick += OnSolveTimer;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1040, 660);
            Controls.Add(lvSolutions);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(tbDimacs);
            Controls.Add(label1);
            Controls.Add(tbCnf);
            Controls.Add(rtbInput);
            Controls.Add(rbtDimacs);
            Controls.Add(rbtBooleanAlgebra);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(4, 5, 4, 5);
            MaximizeBox = false;
            MinimizeBox = false;
            MinimumSize = new Size(700, 600);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "SatSolver Demo";
            ResumeLayout(false);
            PerformLayout();
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
        private ListView lvSolutions;
        private System.Windows.Forms.Timer tmSolve;
    }
}
