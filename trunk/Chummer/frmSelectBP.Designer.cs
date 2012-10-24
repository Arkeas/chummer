﻿namespace Chummer
{
    partial class frmSelectBP
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
			this.components = new System.ComponentModel.Container();
			this.nudBP = new System.Windows.Forms.NumericUpDown();
			this.cmdOK = new System.Windows.Forms.Button();
			this.lblDescription = new System.Windows.Forms.Label();
			this.chkIgnoreRules = new System.Windows.Forms.CheckBox();
			this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
			this.cmdCancel = new System.Windows.Forms.Button();
			this.cboBuildMethod = new System.Windows.Forms.ComboBox();
			this.lblMaxAvail = new System.Windows.Forms.Label();
			this.nudMaxAvail = new System.Windows.Forms.NumericUpDown();
			((System.ComponentModel.ISupportInitialize)(this.nudBP)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.nudMaxAvail)).BeginInit();
			this.SuspendLayout();
			// 
			// nudBP
			// 
			this.nudBP.Location = new System.Drawing.Point(82, 58);
			this.nudBP.Maximum = new decimal(new int[] {
            5000,
            0,
            0,
            0});
			this.nudBP.Name = "nudBP";
			this.nudBP.Size = new System.Drawing.Size(55, 20);
			this.nudBP.TabIndex = 2;
			this.nudBP.Value = new decimal(new int[] {
            400,
            0,
            0,
            0});
			// 
			// cmdOK
			// 
			this.cmdOK.Location = new System.Drawing.Point(191, 107);
			this.cmdOK.Name = "cmdOK";
			this.cmdOK.Size = new System.Drawing.Size(75, 23);
			this.cmdOK.TabIndex = 6;
			this.cmdOK.Tag = "String_OK";
			this.cmdOK.Text = "OK";
			this.cmdOK.UseVisualStyleBackColor = true;
			this.cmdOK.Click += new System.EventHandler(this.cmdOK_Click);
			// 
			// lblDescription
			// 
			this.lblDescription.Location = new System.Drawing.Point(12, 9);
			this.lblDescription.Name = "lblDescription";
			this.lblDescription.Size = new System.Drawing.Size(248, 44);
			this.lblDescription.TabIndex = 0;
			this.lblDescription.Tag = "String_SelectBP_BPSummary";
			this.lblDescription.Text = "Enter the amount of Build Points you are allowed to create your character with (D" +
    "efault 400).";
			// 
			// chkIgnoreRules
			// 
			this.chkIgnoreRules.AutoSize = true;
			this.chkIgnoreRules.Location = new System.Drawing.Point(47, 84);
			this.chkIgnoreRules.Name = "chkIgnoreRules";
			this.chkIgnoreRules.Size = new System.Drawing.Size(177, 17);
			this.chkIgnoreRules.TabIndex = 5;
			this.chkIgnoreRules.Tag = "Checkbox_SelectBP_IgnoreRules";
			this.chkIgnoreRules.Text = "Ignore Character Creation Rules";
			this.toolTip1.SetToolTip(this.chkIgnoreRules, "When checked, all BP and Attribute/Skill maximum rules will be ignored. Intended " +
        "for Game Master/NPC use only.");
			this.chkIgnoreRules.UseVisualStyleBackColor = true;
			// 
			// toolTip1
			// 
			this.toolTip1.AutoPopDelay = 10000;
			this.toolTip1.InitialDelay = 250;
			this.toolTip1.IsBalloon = true;
			this.toolTip1.ReshowDelay = 100;
			this.toolTip1.ToolTipIcon = System.Windows.Forms.ToolTipIcon.Info;
			this.toolTip1.ToolTipTitle = "Chummer Help";
			// 
			// cmdCancel
			// 
			this.cmdCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.cmdCancel.Location = new System.Drawing.Point(110, 107);
			this.cmdCancel.Name = "cmdCancel";
			this.cmdCancel.Size = new System.Drawing.Size(75, 23);
			this.cmdCancel.TabIndex = 7;
			this.cmdCancel.Tag = "String_Cancel";
			this.cmdCancel.Text = "Cancel";
			this.cmdCancel.UseVisualStyleBackColor = true;
			this.cmdCancel.Click += new System.EventHandler(this.cmdCancel_Click);
			// 
			// cboBuildMethod
			// 
			this.cboBuildMethod.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cboBuildMethod.FormattingEnabled = true;
			this.cboBuildMethod.Location = new System.Drawing.Point(15, 58);
			this.cboBuildMethod.Name = "cboBuildMethod";
			this.cboBuildMethod.Size = new System.Drawing.Size(61, 21);
			this.cboBuildMethod.TabIndex = 1;
			this.cboBuildMethod.SelectedIndexChanged += new System.EventHandler(this.cboBuildMethod_SelectedIndexChanged);
			// 
			// lblMaxAvail
			// 
			this.lblMaxAvail.AutoSize = true;
			this.lblMaxAvail.Location = new System.Drawing.Point(153, 62);
			this.lblMaxAvail.Name = "lblMaxAvail";
			this.lblMaxAvail.Size = new System.Drawing.Size(59, 13);
			this.lblMaxAvail.TabIndex = 3;
			this.lblMaxAvail.Tag = "Label_SelectBP_MaxAvail";
			this.lblMaxAvail.Text = "Max Avail.:";
			// 
			// nudMaxAvail
			// 
			this.nudMaxAvail.Location = new System.Drawing.Point(218, 59);
			this.nudMaxAvail.Name = "nudMaxAvail";
			this.nudMaxAvail.Size = new System.Drawing.Size(42, 20);
			this.nudMaxAvail.TabIndex = 4;
			this.nudMaxAvail.Value = new decimal(new int[] {
            12,
            0,
            0,
            0});
			// 
			// frmSelectBP
			// 
			this.AcceptButton = this.cmdOK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.cmdCancel;
			this.ClientSize = new System.Drawing.Size(278, 136);
			this.ControlBox = false;
			this.Controls.Add(this.nudMaxAvail);
			this.Controls.Add(this.lblMaxAvail);
			this.Controls.Add(this.cboBuildMethod);
			this.Controls.Add(this.cmdCancel);
			this.Controls.Add(this.chkIgnoreRules);
			this.Controls.Add(this.lblDescription);
			this.Controls.Add(this.cmdOK);
			this.Controls.Add(this.nudBP);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "frmSelectBP";
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Tag = "Title_SelectBP";
			this.Text = "Choose BP Amount";
			this.Load += new System.EventHandler(this.frmSelectBP_Load);
			((System.ComponentModel.ISupportInitialize)(this.nudBP)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.nudMaxAvail)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

		private System.Windows.Forms.NumericUpDown nudBP;
        private System.Windows.Forms.Button cmdOK;
        private System.Windows.Forms.Label lblDescription;
		private System.Windows.Forms.CheckBox chkIgnoreRules;
		private System.Windows.Forms.ToolTip toolTip1;
		private System.Windows.Forms.Button cmdCancel;
		private System.Windows.Forms.ComboBox cboBuildMethod;
		private System.Windows.Forms.Label lblMaxAvail;
		private System.Windows.Forms.NumericUpDown nudMaxAvail;
    }
}