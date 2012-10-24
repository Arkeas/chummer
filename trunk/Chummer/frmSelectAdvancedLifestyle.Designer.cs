namespace Chummer
{
	partial class frmSelectAdvancedLifestyle
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
			this.cboSecurity = new System.Windows.Forms.ComboBox();
			this.lblSecurity = new System.Windows.Forms.Label();
			this.cboNeighborhood = new System.Windows.Forms.ComboBox();
			this.lblNeighborhood = new System.Windows.Forms.Label();
			this.cboNecessities = new System.Windows.Forms.ComboBox();
			this.lblNecessities = new System.Windows.Forms.Label();
			this.cboEntertainment = new System.Windows.Forms.ComboBox();
			this.lblEntertainment = new System.Windows.Forms.Label();
			this.cboComforts = new System.Windows.Forms.ComboBox();
			this.lblComforts = new System.Windows.Forms.Label();
			this.cmdOKAdd = new System.Windows.Forms.Button();
			this.cmdCancel = new System.Windows.Forms.Button();
			this.cmdOK = new System.Windows.Forms.Button();
			this.trePositiveQualities = new System.Windows.Forms.TreeView();
			this.treNegativeQualities = new System.Windows.Forms.TreeView();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.lblTotalLPLabel = new System.Windows.Forms.Label();
			this.lblTotalLP = new System.Windows.Forms.Label();
			this.lblCostLabel = new System.Windows.Forms.Label();
			this.lblCost = new System.Windows.Forms.Label();
			this.lblLifestyleNameLabel = new System.Windows.Forms.Label();
			this.txtLifestyleName = new System.Windows.Forms.TextBox();
			this.nudPercentage = new System.Windows.Forms.NumericUpDown();
			this.lblPercentage = new System.Windows.Forms.Label();
			this.lblSource = new System.Windows.Forms.Label();
			this.lblSourceLabel = new System.Windows.Forms.Label();
			this.nudRoommates = new System.Windows.Forms.NumericUpDown();
			this.lblRoommates = new System.Windows.Forms.Label();
			this.tipTooltip = new System.Windows.Forms.ToolTip(this.components);
			((System.ComponentModel.ISupportInitialize)(this.nudPercentage)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.nudRoommates)).BeginInit();
			this.SuspendLayout();
			// 
			// cboSecurity
			// 
			this.cboSecurity.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cboSecurity.FormattingEnabled = true;
			this.cboSecurity.Location = new System.Drawing.Point(337, 156);
			this.cboSecurity.Name = "cboSecurity";
			this.cboSecurity.Size = new System.Drawing.Size(164, 21);
			this.cboSecurity.TabIndex = 11;
			this.cboSecurity.SelectedIndexChanged += new System.EventHandler(this.cboSecurity_SelectedIndexChanged);
			// 
			// lblSecurity
			// 
			this.lblSecurity.AutoSize = true;
			this.lblSecurity.Location = new System.Drawing.Point(256, 159);
			this.lblSecurity.Name = "lblSecurity";
			this.lblSecurity.Size = new System.Drawing.Size(48, 13);
			this.lblSecurity.TabIndex = 10;
			this.lblSecurity.Tag = "Label_SelectAdvancedLifestyle_Security";
			this.lblSecurity.Text = "Security:";
			// 
			// cboNeighborhood
			// 
			this.cboNeighborhood.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cboNeighborhood.FormattingEnabled = true;
			this.cboNeighborhood.Location = new System.Drawing.Point(337, 129);
			this.cboNeighborhood.Name = "cboNeighborhood";
			this.cboNeighborhood.Size = new System.Drawing.Size(164, 21);
			this.cboNeighborhood.TabIndex = 9;
			this.cboNeighborhood.SelectedIndexChanged += new System.EventHandler(this.cboNeighborhood_SelectedIndexChanged);
			// 
			// lblNeighborhood
			// 
			this.lblNeighborhood.AutoSize = true;
			this.lblNeighborhood.Location = new System.Drawing.Point(256, 132);
			this.lblNeighborhood.Name = "lblNeighborhood";
			this.lblNeighborhood.Size = new System.Drawing.Size(77, 13);
			this.lblNeighborhood.TabIndex = 8;
			this.lblNeighborhood.Tag = "Label_SelectAdvancedLifestyle_Neighborhood";
			this.lblNeighborhood.Text = "Neighborhood:";
			// 
			// cboNecessities
			// 
			this.cboNecessities.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cboNecessities.FormattingEnabled = true;
			this.cboNecessities.Location = new System.Drawing.Point(337, 102);
			this.cboNecessities.Name = "cboNecessities";
			this.cboNecessities.Size = new System.Drawing.Size(164, 21);
			this.cboNecessities.TabIndex = 7;
			this.cboNecessities.SelectedIndexChanged += new System.EventHandler(this.cboNecessities_SelectedIndexChanged);
			// 
			// lblNecessities
			// 
			this.lblNecessities.AutoSize = true;
			this.lblNecessities.Location = new System.Drawing.Point(256, 105);
			this.lblNecessities.Name = "lblNecessities";
			this.lblNecessities.Size = new System.Drawing.Size(64, 13);
			this.lblNecessities.TabIndex = 6;
			this.lblNecessities.Tag = "Label_SelectAdvancedLifestyle_Necessities";
			this.lblNecessities.Text = "Necessities:";
			// 
			// cboEntertainment
			// 
			this.cboEntertainment.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cboEntertainment.FormattingEnabled = true;
			this.cboEntertainment.Location = new System.Drawing.Point(337, 75);
			this.cboEntertainment.Name = "cboEntertainment";
			this.cboEntertainment.Size = new System.Drawing.Size(164, 21);
			this.cboEntertainment.TabIndex = 5;
			this.cboEntertainment.SelectedIndexChanged += new System.EventHandler(this.cboEntertainment_SelectedIndexChanged);
			// 
			// lblEntertainment
			// 
			this.lblEntertainment.AutoSize = true;
			this.lblEntertainment.Location = new System.Drawing.Point(256, 78);
			this.lblEntertainment.Name = "lblEntertainment";
			this.lblEntertainment.Size = new System.Drawing.Size(75, 13);
			this.lblEntertainment.TabIndex = 4;
			this.lblEntertainment.Tag = "Label_SelectAdvancedLifestyle_Entertainment";
			this.lblEntertainment.Text = "Entertainment:";
			// 
			// cboComforts
			// 
			this.cboComforts.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cboComforts.FormattingEnabled = true;
			this.cboComforts.Location = new System.Drawing.Point(337, 48);
			this.cboComforts.Name = "cboComforts";
			this.cboComforts.Size = new System.Drawing.Size(164, 21);
			this.cboComforts.TabIndex = 3;
			this.cboComforts.SelectedIndexChanged += new System.EventHandler(this.cboComforts_SelectedIndexChanged);
			// 
			// lblComforts
			// 
			this.lblComforts.AutoSize = true;
			this.lblComforts.Location = new System.Drawing.Point(256, 51);
			this.lblComforts.Name = "lblComforts";
			this.lblComforts.Size = new System.Drawing.Size(51, 13);
			this.lblComforts.TabIndex = 2;
			this.lblComforts.Tag = "Label_SelectAdvancedLifestyle_Comforts";
			this.lblComforts.Text = "Comforts:";
			// 
			// cmdOKAdd
			// 
			this.cmdOKAdd.Location = new System.Drawing.Point(483, 308);
			this.cmdOKAdd.Name = "cmdOKAdd";
			this.cmdOKAdd.Size = new System.Drawing.Size(75, 23);
			this.cmdOKAdd.TabIndex = 27;
			this.cmdOKAdd.Tag = "String_AddMore";
			this.cmdOKAdd.Text = "&Add && More";
			this.cmdOKAdd.UseVisualStyleBackColor = true;
			this.cmdOKAdd.Click += new System.EventHandler(this.cmdOKAdd_Click);
			// 
			// cmdCancel
			// 
			this.cmdCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.cmdCancel.Location = new System.Drawing.Point(402, 337);
			this.cmdCancel.Name = "cmdCancel";
			this.cmdCancel.Size = new System.Drawing.Size(75, 23);
			this.cmdCancel.TabIndex = 28;
			this.cmdCancel.Tag = "String_Cancel";
			this.cmdCancel.Text = "Cancel";
			this.cmdCancel.UseVisualStyleBackColor = true;
			this.cmdCancel.Click += new System.EventHandler(this.cmdCancel_Click);
			// 
			// cmdOK
			// 
			this.cmdOK.Location = new System.Drawing.Point(483, 337);
			this.cmdOK.Name = "cmdOK";
			this.cmdOK.Size = new System.Drawing.Size(75, 23);
			this.cmdOK.TabIndex = 26;
			this.cmdOK.Tag = "String_OK";
			this.cmdOK.Text = "OK";
			this.cmdOK.UseVisualStyleBackColor = true;
			this.cmdOK.Click += new System.EventHandler(this.cmdOK_Click);
			// 
			// trePositiveQualities
			// 
			this.trePositiveQualities.CheckBoxes = true;
			this.trePositiveQualities.HideSelection = false;
			this.trePositiveQualities.Location = new System.Drawing.Point(12, 25);
			this.trePositiveQualities.Name = "trePositiveQualities";
			this.trePositiveQualities.ShowLines = false;
			this.trePositiveQualities.ShowPlusMinus = false;
			this.trePositiveQualities.ShowRootLines = false;
			this.trePositiveQualities.Size = new System.Drawing.Size(238, 152);
			this.trePositiveQualities.TabIndex = 23;
			this.trePositiveQualities.AfterCheck += new System.Windows.Forms.TreeViewEventHandler(this.trePositiveQualities_AfterCheck);
			this.trePositiveQualities.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.trePositiveQualities_AfterSelect);
			// 
			// treNegativeQualities
			// 
			this.treNegativeQualities.CheckBoxes = true;
			this.treNegativeQualities.HideSelection = false;
			this.treNegativeQualities.Location = new System.Drawing.Point(15, 201);
			this.treNegativeQualities.Name = "treNegativeQualities";
			this.treNegativeQualities.ShowLines = false;
			this.treNegativeQualities.ShowPlusMinus = false;
			this.treNegativeQualities.ShowRootLines = false;
			this.treNegativeQualities.Size = new System.Drawing.Size(235, 152);
			this.treNegativeQualities.TabIndex = 25;
			this.treNegativeQualities.AfterCheck += new System.Windows.Forms.TreeViewEventHandler(this.treNegativeQualities_AfterCheck);
			this.treNegativeQualities.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treNegativeQualities_AfterSelect);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(12, 9);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(87, 13);
			this.label1.TabIndex = 22;
			this.label1.Tag = "Label_SelectAdvancedLifestyle_PositiveQualities";
			this.label1.Text = "Positive Qualities";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(12, 185);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(93, 13);
			this.label2.TabIndex = 24;
			this.label2.Tag = "Label_SelectAdvancedLifestyle_NegativeQualities";
			this.label2.Text = "Negative Qualities";
			// 
			// lblTotalLPLabel
			// 
			this.lblTotalLPLabel.AutoSize = true;
			this.lblTotalLPLabel.Location = new System.Drawing.Point(256, 199);
			this.lblTotalLPLabel.Name = "lblTotalLPLabel";
			this.lblTotalLPLabel.Size = new System.Drawing.Size(50, 13);
			this.lblTotalLPLabel.TabIndex = 12;
			this.lblTotalLPLabel.Tag = "Label_SelectAdvancedLifestyle_TotalLP";
			this.lblTotalLPLabel.Text = "Total LP:";
			// 
			// lblTotalLP
			// 
			this.lblTotalLP.AutoSize = true;
			this.lblTotalLP.Location = new System.Drawing.Point(334, 199);
			this.lblTotalLP.Name = "lblTotalLP";
			this.lblTotalLP.Size = new System.Drawing.Size(26, 13);
			this.lblTotalLP.TabIndex = 13;
			this.lblTotalLP.Text = "[LP]";
			// 
			// lblCostLabel
			// 
			this.lblCostLabel.AutoSize = true;
			this.lblCostLabel.Location = new System.Drawing.Point(256, 268);
			this.lblCostLabel.Name = "lblCostLabel";
			this.lblCostLabel.Size = new System.Drawing.Size(66, 13);
			this.lblCostLabel.TabIndex = 18;
			this.lblCostLabel.Tag = "Label_SelectLifestyle_CostPerMonth";
			this.lblCostLabel.Text = "Cost/Month:";
			// 
			// lblCost
			// 
			this.lblCost.AutoSize = true;
			this.lblCost.Location = new System.Drawing.Point(334, 268);
			this.lblCost.Name = "lblCost";
			this.lblCost.Size = new System.Drawing.Size(34, 13);
			this.lblCost.TabIndex = 19;
			this.lblCost.Text = "[Cost]";
			// 
			// lblLifestyleNameLabel
			// 
			this.lblLifestyleNameLabel.AutoSize = true;
			this.lblLifestyleNameLabel.Location = new System.Drawing.Point(256, 25);
			this.lblLifestyleNameLabel.Name = "lblLifestyleNameLabel";
			this.lblLifestyleNameLabel.Size = new System.Drawing.Size(38, 13);
			this.lblLifestyleNameLabel.TabIndex = 0;
			this.lblLifestyleNameLabel.Tag = "Label_Name";
			this.lblLifestyleNameLabel.Text = "Name:";
			// 
			// txtLifestyleName
			// 
			this.txtLifestyleName.Location = new System.Drawing.Point(337, 22);
			this.txtLifestyleName.Name = "txtLifestyleName";
			this.txtLifestyleName.Size = new System.Drawing.Size(164, 20);
			this.txtLifestyleName.TabIndex = 1;
			// 
			// nudPercentage
			// 
			this.nudPercentage.Location = new System.Drawing.Point(337, 243);
			this.nudPercentage.Maximum = new decimal(new int[] {
            900,
            0,
            0,
            0});
			this.nudPercentage.Name = "nudPercentage";
			this.nudPercentage.Size = new System.Drawing.Size(45, 20);
			this.nudPercentage.TabIndex = 17;
			this.nudPercentage.Value = new decimal(new int[] {
            100,
            0,
            0,
            0});
			this.nudPercentage.ValueChanged += new System.EventHandler(this.nudPercentage_ValueChanged);
			// 
			// lblPercentage
			// 
			this.lblPercentage.AutoSize = true;
			this.lblPercentage.Location = new System.Drawing.Point(256, 245);
			this.lblPercentage.Name = "lblPercentage";
			this.lblPercentage.Size = new System.Drawing.Size(51, 13);
			this.lblPercentage.TabIndex = 16;
			this.lblPercentage.Tag = "Label_SelectLifestyle_PercentToPay";
			this.lblPercentage.Text = "% to Pay:";
			// 
			// lblSource
			// 
			this.lblSource.AutoSize = true;
			this.lblSource.Location = new System.Drawing.Point(334, 291);
			this.lblSource.Name = "lblSource";
			this.lblSource.Size = new System.Drawing.Size(47, 13);
			this.lblSource.TabIndex = 21;
			this.lblSource.Text = "[Source]";
			// 
			// lblSourceLabel
			// 
			this.lblSourceLabel.AutoSize = true;
			this.lblSourceLabel.Location = new System.Drawing.Point(256, 291);
			this.lblSourceLabel.Name = "lblSourceLabel";
			this.lblSourceLabel.Size = new System.Drawing.Size(44, 13);
			this.lblSourceLabel.TabIndex = 20;
			this.lblSourceLabel.Tag = "Label_Source";
			this.lblSourceLabel.Text = "Source:";
			// 
			// nudRoommates
			// 
			this.nudRoommates.Location = new System.Drawing.Point(337, 220);
			this.nudRoommates.Maximum = new decimal(new int[] {
            10,
            0,
            0,
            0});
			this.nudRoommates.Name = "nudRoommates";
			this.nudRoommates.Size = new System.Drawing.Size(45, 20);
			this.nudRoommates.TabIndex = 15;
			this.nudRoommates.ValueChanged += new System.EventHandler(this.nudRoommates_ValueChanged);
			// 
			// lblRoommates
			// 
			this.lblRoommates.AutoSize = true;
			this.lblRoommates.Location = new System.Drawing.Point(256, 222);
			this.lblRoommates.Name = "lblRoommates";
			this.lblRoommates.Size = new System.Drawing.Size(66, 13);
			this.lblRoommates.TabIndex = 14;
			this.lblRoommates.Tag = "Label_SelectLifestyle_Roommates";
			this.lblRoommates.Text = "Roommates:";
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
			// frmSelectAdvancedLifestyle
			// 
			this.AcceptButton = this.cmdOK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.cmdCancel;
			this.ClientSize = new System.Drawing.Size(570, 365);
			this.Controls.Add(this.nudRoommates);
			this.Controls.Add(this.lblRoommates);
			this.Controls.Add(this.lblSource);
			this.Controls.Add(this.lblSourceLabel);
			this.Controls.Add(this.nudPercentage);
			this.Controls.Add(this.lblPercentage);
			this.Controls.Add(this.txtLifestyleName);
			this.Controls.Add(this.lblLifestyleNameLabel);
			this.Controls.Add(this.lblCost);
			this.Controls.Add(this.lblCostLabel);
			this.Controls.Add(this.lblTotalLP);
			this.Controls.Add(this.lblTotalLPLabel);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.treNegativeQualities);
			this.Controls.Add(this.trePositiveQualities);
			this.Controls.Add(this.cmdOKAdd);
			this.Controls.Add(this.cmdCancel);
			this.Controls.Add(this.cmdOK);
			this.Controls.Add(this.cboSecurity);
			this.Controls.Add(this.lblSecurity);
			this.Controls.Add(this.cboNeighborhood);
			this.Controls.Add(this.lblNeighborhood);
			this.Controls.Add(this.cboNecessities);
			this.Controls.Add(this.lblNecessities);
			this.Controls.Add(this.cboEntertainment);
			this.Controls.Add(this.lblEntertainment);
			this.Controls.Add(this.cboComforts);
			this.Controls.Add(this.lblComforts);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "frmSelectAdvancedLifestyle";
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Tag = "Title_SelectAdvancedLifestyle";
			this.Text = "Build Advanced Lifestyle";
			this.Load += new System.EventHandler(this.frmSelectAdvancedLifestyle_Load);
			((System.ComponentModel.ISupportInitialize)(this.nudPercentage)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.nudRoommates)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.ComboBox cboSecurity;
		private System.Windows.Forms.Label lblSecurity;
		private System.Windows.Forms.ComboBox cboNeighborhood;
		private System.Windows.Forms.Label lblNeighborhood;
		private System.Windows.Forms.ComboBox cboNecessities;
		private System.Windows.Forms.Label lblNecessities;
		private System.Windows.Forms.ComboBox cboEntertainment;
		private System.Windows.Forms.Label lblEntertainment;
		private System.Windows.Forms.ComboBox cboComforts;
		private System.Windows.Forms.Label lblComforts;
		private System.Windows.Forms.Button cmdOKAdd;
		private System.Windows.Forms.Button cmdCancel;
		private System.Windows.Forms.Button cmdOK;
		private System.Windows.Forms.TreeView trePositiveQualities;
		private System.Windows.Forms.TreeView treNegativeQualities;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label lblTotalLPLabel;
		private System.Windows.Forms.Label lblTotalLP;
		private System.Windows.Forms.Label lblCostLabel;
		private System.Windows.Forms.Label lblCost;
		private System.Windows.Forms.Label lblLifestyleNameLabel;
		private System.Windows.Forms.TextBox txtLifestyleName;
		private System.Windows.Forms.NumericUpDown nudPercentage;
		private System.Windows.Forms.Label lblPercentage;
		private System.Windows.Forms.Label lblSource;
		private System.Windows.Forms.Label lblSourceLabel;
		private System.Windows.Forms.NumericUpDown nudRoommates;
		private System.Windows.Forms.Label lblRoommates;
		private System.Windows.Forms.ToolTip tipTooltip;
	}
}