namespace Chummer
{
    partial class frmSelectProgram
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
			this.trePrograms = new System.Windows.Forms.TreeView();
			this.cmdOK = new System.Windows.Forms.Button();
			this.lblCommonSkillLabel = new System.Windows.Forms.Label();
			this.lblCommonSkill = new System.Windows.Forms.Label();
			this.cmdCancel = new System.Windows.Forms.Button();
			this.cmdOKAdd = new System.Windows.Forms.Button();
			this.lblSource = new System.Windows.Forms.Label();
			this.lblSourceLabel = new System.Windows.Forms.Label();
			this.txtSearch = new System.Windows.Forms.TextBox();
			this.lblSearchLabel = new System.Windows.Forms.Label();
			this.tipTooltip = new System.Windows.Forms.ToolTip(this.components);
			this.SuspendLayout();
			// 
			// trePrograms
			// 
			this.trePrograms.FullRowSelect = true;
			this.trePrograms.HideSelection = false;
			this.trePrograms.Location = new System.Drawing.Point(12, 12);
			this.trePrograms.Name = "trePrograms";
			this.trePrograms.Size = new System.Drawing.Size(241, 430);
			this.trePrograms.TabIndex = 6;
			this.trePrograms.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.trePrograms_AfterSelect);
			this.trePrograms.DoubleClick += new System.EventHandler(this.trePrograms_DoubleClick);
			// 
			// cmdOK
			// 
			this.cmdOK.Location = new System.Drawing.Point(429, 419);
			this.cmdOK.Name = "cmdOK";
			this.cmdOK.Size = new System.Drawing.Size(75, 23);
			this.cmdOK.TabIndex = 7;
			this.cmdOK.Tag = "String_OK";
			this.cmdOK.Text = "OK";
			this.cmdOK.UseVisualStyleBackColor = true;
			this.cmdOK.Click += new System.EventHandler(this.cmdOK_Click);
			// 
			// lblCommonSkillLabel
			// 
			this.lblCommonSkillLabel.AutoSize = true;
			this.lblCommonSkillLabel.Location = new System.Drawing.Point(259, 60);
			this.lblCommonSkillLabel.Name = "lblCommonSkillLabel";
			this.lblCommonSkillLabel.Size = new System.Drawing.Size(108, 13);
			this.lblCommonSkillLabel.TabIndex = 2;
			this.lblCommonSkillLabel.Tag = "Label_SelectProgram_CommonlyUsedSkill";
			this.lblCommonSkillLabel.Text = "Commonly Used Skill:";
			// 
			// lblCommonSkill
			// 
			this.lblCommonSkill.AutoSize = true;
			this.lblCommonSkill.Location = new System.Drawing.Point(373, 60);
			this.lblCommonSkill.Name = "lblCommonSkill";
			this.lblCommonSkill.Size = new System.Drawing.Size(39, 13);
			this.lblCommonSkill.TabIndex = 3;
			this.lblCommonSkill.Text = "[None]";
			// 
			// cmdCancel
			// 
			this.cmdCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.cmdCancel.Location = new System.Drawing.Point(348, 419);
			this.cmdCancel.Name = "cmdCancel";
			this.cmdCancel.Size = new System.Drawing.Size(75, 23);
			this.cmdCancel.TabIndex = 9;
			this.cmdCancel.Tag = "String_Cancel";
			this.cmdCancel.Text = "Cancel";
			this.cmdCancel.UseVisualStyleBackColor = true;
			this.cmdCancel.Click += new System.EventHandler(this.cmdCancel_Click);
			// 
			// cmdOKAdd
			// 
			this.cmdOKAdd.Location = new System.Drawing.Point(429, 390);
			this.cmdOKAdd.Name = "cmdOKAdd";
			this.cmdOKAdd.Size = new System.Drawing.Size(75, 23);
			this.cmdOKAdd.TabIndex = 8;
			this.cmdOKAdd.Tag = "String_AddMore";
			this.cmdOKAdd.Text = "&Add && More";
			this.cmdOKAdd.UseVisualStyleBackColor = true;
			this.cmdOKAdd.Click += new System.EventHandler(this.cmdOKAdd_Click);
			// 
			// lblSource
			// 
			this.lblSource.AutoSize = true;
			this.lblSource.Location = new System.Drawing.Point(310, 143);
			this.lblSource.Name = "lblSource";
			this.lblSource.Size = new System.Drawing.Size(47, 13);
			this.lblSource.TabIndex = 5;
			this.lblSource.Text = "[Source]";
			// 
			// lblSourceLabel
			// 
			this.lblSourceLabel.AutoSize = true;
			this.lblSourceLabel.Location = new System.Drawing.Point(259, 143);
			this.lblSourceLabel.Name = "lblSourceLabel";
			this.lblSourceLabel.Size = new System.Drawing.Size(44, 13);
			this.lblSourceLabel.TabIndex = 4;
			this.lblSourceLabel.Tag = "Label_Source";
			this.lblSourceLabel.Text = "Source:";
			// 
			// txtSearch
			// 
			this.txtSearch.Location = new System.Drawing.Point(330, 12);
			this.txtSearch.Name = "txtSearch";
			this.txtSearch.Size = new System.Drawing.Size(174, 20);
			this.txtSearch.TabIndex = 1;
			this.txtSearch.TextChanged += new System.EventHandler(this.txtSearch_TextChanged);
			this.txtSearch.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtSearch_KeyDown);
			this.txtSearch.KeyUp += new System.Windows.Forms.KeyEventHandler(this.txtSearch_KeyUp);
			// 
			// lblSearchLabel
			// 
			this.lblSearchLabel.AutoSize = true;
			this.lblSearchLabel.Location = new System.Drawing.Point(280, 15);
			this.lblSearchLabel.Name = "lblSearchLabel";
			this.lblSearchLabel.Size = new System.Drawing.Size(44, 13);
			this.lblSearchLabel.TabIndex = 0;
			this.lblSearchLabel.Tag = "Label_Search";
			this.lblSearchLabel.Text = "&Search:";
			// 
			// tipTooltip
			// 
			this.tipTooltip.AutoPopDelay = 10000;
			this.tipTooltip.InitialDelay = 250;
			this.tipTooltip.IsBalloon = true;
			this.tipTooltip.ReshowDelay = 100;
			this.tipTooltip.ToolTipIcon = System.Windows.Forms.ToolTipIcon.Info;
			this.tipTooltip.ToolTipTitle = "Chummer Help";
			// 
			// frmSelectProgram
			// 
			this.AcceptButton = this.cmdOK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.cmdCancel;
			this.ClientSize = new System.Drawing.Size(516, 454);
			this.Controls.Add(this.txtSearch);
			this.Controls.Add(this.lblSearchLabel);
			this.Controls.Add(this.lblSource);
			this.Controls.Add(this.lblSourceLabel);
			this.Controls.Add(this.cmdOKAdd);
			this.Controls.Add(this.cmdCancel);
			this.Controls.Add(this.lblCommonSkill);
			this.Controls.Add(this.lblCommonSkillLabel);
			this.Controls.Add(this.cmdOK);
			this.Controls.Add(this.trePrograms);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "frmSelectProgram";
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Tag = "Title_SelectProgram";
			this.Text = "Select a Program";
			this.Load += new System.EventHandler(this.frmSelectProgram_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TreeView trePrograms;
        private System.Windows.Forms.Button cmdOK;
        private System.Windows.Forms.Label lblCommonSkillLabel;
        private System.Windows.Forms.Label lblCommonSkill;
        private System.Windows.Forms.Button cmdCancel;
		private System.Windows.Forms.Button cmdOKAdd;
		private System.Windows.Forms.Label lblSource;
		private System.Windows.Forms.Label lblSourceLabel;
		private System.Windows.Forms.TextBox txtSearch;
		private System.Windows.Forms.Label lblSearchLabel;
		private System.Windows.Forms.ToolTip tipTooltip;
    }
}