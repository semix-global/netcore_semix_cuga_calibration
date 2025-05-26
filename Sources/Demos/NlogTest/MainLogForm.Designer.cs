namespace NlogTest
{
    partial class MainLogForm
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
            this.btnCleanLog = new FontAwesome.Sharp.IconButton();
            this.txtLog = new System.Windows.Forms.RichTextBox();
            this.buttonGenerateLog = new System.Windows.Forms.Button();
            this.panel1 = new System.Windows.Forms.Panel();
            this.splitter1 = new System.Windows.Forms.Splitter();
            this.buttonGenerateImageLog = new System.Windows.Forms.Button();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnCleanLog
            // 
            this.btnCleanLog.BackColor = System.Drawing.Color.Transparent;
            this.btnCleanLog.Dock = System.Windows.Forms.DockStyle.Right;
            this.btnCleanLog.FlatAppearance.BorderSize = 0;
            this.btnCleanLog.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCleanLog.ForeColor = System.Drawing.SystemColors.ControlText;
            this.btnCleanLog.IconChar = FontAwesome.Sharp.IconChar.Cut;
            this.btnCleanLog.IconColor = System.Drawing.SystemColors.ControlDarkDark;
            this.btnCleanLog.IconFont = FontAwesome.Sharp.IconFont.Auto;
            this.btnCleanLog.IconSize = 18;
            this.btnCleanLog.Location = new System.Drawing.Point(758, 0);
            this.btnCleanLog.Name = "btnCleanLog";
            this.btnCleanLog.Size = new System.Drawing.Size(32, 30);
            this.btnCleanLog.TabIndex = 31;
            this.btnCleanLog.UseVisualStyleBackColor = false;
            // 
            // txtLog
            // 
            this.txtLog.BackColor = System.Drawing.SystemColors.Control;
            this.txtLog.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtLog.Font = new System.Drawing.Font("Lucida Console", 10F);
            this.txtLog.ForeColor = System.Drawing.SystemColors.WindowText;
            this.txtLog.Location = new System.Drawing.Point(5, 45);
            this.txtLog.Margin = new System.Windows.Forms.Padding(0);
            this.txtLog.Name = "txtLog";
            this.txtLog.ReadOnly = true;
            this.txtLog.Size = new System.Drawing.Size(790, 400);
            this.txtLog.TabIndex = 30;
            this.txtLog.Text = "";
            // 
            // buttonGenerateLog
            // 
            this.buttonGenerateLog.Dock = System.Windows.Forms.DockStyle.Left;
            this.buttonGenerateLog.Location = new System.Drawing.Point(0, 0);
            this.buttonGenerateLog.Name = "buttonGenerateLog";
            this.buttonGenerateLog.Size = new System.Drawing.Size(143, 30);
            this.buttonGenerateLog.TabIndex = 32;
            this.buttonGenerateLog.Text = "Generate Log";
            this.buttonGenerateLog.UseVisualStyleBackColor = true;
            this.buttonGenerateLog.Click += new System.EventHandler(this.ButtonGenerateLogOnClick);
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.buttonGenerateImageLog);
            this.panel1.Controls.Add(this.buttonGenerateLog);
            this.panel1.Controls.Add(this.btnCleanLog);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(5, 5);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(790, 30);
            this.panel1.TabIndex = 33;
            // 
            // splitter1
            // 
            this.splitter1.Dock = System.Windows.Forms.DockStyle.Top;
            this.splitter1.Location = new System.Drawing.Point(5, 35);
            this.splitter1.Name = "splitter1";
            this.splitter1.Size = new System.Drawing.Size(790, 10);
            this.splitter1.TabIndex = 34;
            this.splitter1.TabStop = false;
            // 
            // buttonGenerateImageLog
            // 
            this.buttonGenerateImageLog.Dock = System.Windows.Forms.DockStyle.Left;
            this.buttonGenerateImageLog.Location = new System.Drawing.Point(143, 0);
            this.buttonGenerateImageLog.Name = "buttonGenerateImageLog";
            this.buttonGenerateImageLog.Size = new System.Drawing.Size(143, 30);
            this.buttonGenerateImageLog.TabIndex = 33;
            this.buttonGenerateImageLog.Text = "Generate Image Log";
            this.buttonGenerateImageLog.UseVisualStyleBackColor = true;
            this.buttonGenerateImageLog.Click += new System.EventHandler(this.ButtonGenerateImageLogOnClick);
            // 
            // MainLogForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.txtLog);
            this.Controls.Add(this.splitter1);
            this.Controls.Add(this.panel1);
            this.Name = "MainLogForm";
            this.Padding = new System.Windows.Forms.Padding(5);
            this.Text = "MainLogForm";
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private FontAwesome.Sharp.IconButton btnCleanLog;
        private RichTextBox txtLog;
        private Button buttonGenerateLog;
        private Panel panel1;
        private Splitter splitter1;
        private Button buttonGenerateImageLog;
    }
}
