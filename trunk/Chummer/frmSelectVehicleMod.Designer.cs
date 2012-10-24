﻿namespace Chummer
{
	partial class frmSelectVehicleMod
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
			this.lblMaximumCapacity = new System.Windows.Forms.Label();
			this.lblCost = new System.Windows.Forms.Label();
			this.lblCostLabel = new System.Windows.Forms.Label();
			this.lblAvail = new System.Windows.Forms.Label();
			this.lblAvailLabel = new System.Windows.Forms.Label();
			this.nudRating = new System.Windows.Forms.NumericUpDown();
			this.lblRatingLabel = new System.Windows.Forms.Label();
			this.cmdCancel = new System.Windows.Forms.Button();
			this.cmdOK = new System.Windows.Forms.Button();
			this.lstMod = new System.Windows.Forms.ListBox();
			this.lblSlots = new System.Windows.Forms.Label();
			this.lblSlotsLabel = new System.Windows.Forms.Label();
			this.lblCategory = new System.Windows.Forms.Label();
			this.lblCategoryLabel = new System.Windows.Forms.Label();
			this.lblLimit = new System.Windows.Forms.Label();
			this.cmdOKAdd = new System.Windows.Forms.Button();
			this.lblSource = new System.Windows.Forms.Label();
			this.lblSourceLabel = new System.Windows.Forms.Label();
			this.chkFreeItem = new System.Windows.Forms.CheckBox();
			this.txtSearch = new System.Windows.Forms.TextBox();
			this.lblSearchLabel = new System.Windows.Forms.Label();
			this.nudMarkup = new System.Windows.Forms.NumericUpDown();
			this.lblMarkupLabel = new System.Windows.Forms.Label();
			this.lblMarkupPercentLabel = new System.Windows.Forms.Label();
			this.lblTest = new System.Windows.Forms.Label();
			this.lblTestLabel = new System.Windows.Forms.Label();
			this.tipTooltip = new System.Windows.Forms.ToolTip(this.components);
			((System.ComponentModel.ISupportInitialize)(this.nudRating)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.nudMarkup)).BeginInit();
			this.SuspendLayout();
			// 
			// lblMaximumCapacity
			// 
			this.lblMaximumCapacity.AutoSize = true;
			this.lblMaximumCapacity.Location = new System.Drawing.Point(382, 248);
			this.lblMaximumCapacity.Name = "lblMaximumCapacity";
			this.lblMaximumCapacity.Size = new System.Drawing.Size(101, 13);
			this.lblMaximumCapacity.TabIndex = 19;
			this.lblMaximumCapacity.Text = "[Maximum Capacity]";
			// 
			// lblCost
			// 
			this.lblCost.AutoSize = true;
			this.lblCost.Location = new System.Drawing.Point(439, 120);
			this.lblCost.Name = "lblCost";
			this.lblCost.Size = new System.Drawing.Size(19, 13);
			this.lblCost.TabIndex = 10;
			this.lblCost.Text = "[0]";
			// 
			// lblCostLabel
			// 
			this.lblCostLabel.AutoSize = true;
			this.lblCostLabel.Location = new System.Drawing.Point(382, 120);
			this.lblCostLabel.Name = "lblCostLabel";
			this.lblCostLabel.Size = new System.Drawing.Size(31, 13);
			this.lblCostLabel.TabIndex = 9;
			this.lblCostLabel.Tag = "Label_Cost";
			this.lblCostLabel.Text = "Cost:";
			// 
			// lblAvail
			// 
			this.lblAvail.AutoSize = true;
			this.lblAvail.Location = new System.Drawing.Point(439, 98);
			this.lblAvail.Name = "lblAvail";
			this.lblAvail.Size = new System.Drawing.Size(19, 13);
			this.lblAvail.TabIndex = 6;
			this.lblAvail.Text = "[0]";
			// 
			// lblAvailLabel
			// 
			this.lblAvailLabel.AutoSize = true;
			this.lblAvailLabel.Location = new System.Drawing.Point(382, 98);
			this.lblAvailLabel.Name = "lblAvailLabel";
			this.lblAvailLabel.Size = new System.Drawing.Size(33, 13);
			this.lblAvailLabel.TabIndex = 5;
			this.lblAvailLabel.Tag = "Label_Avail";
			this.lblAvailLabel.Text = "Avail:";
			// 
			// nudRating
			// 
			this.nudRating.Enabled = false;
			this.nudRating.Location = new System.Drawing.Point(442, 164);
			this.nudRating.Name = "nudRating";
			this.nudRating.Size = new System.Drawing.Size(37, 20);
			this.nudRating.TabIndex = 14;
			this.nudRating.ValueChanged += new System.EventHandler(this.nudRating_ValueChanged);
			// 
			// lblRatingLabel
			// 
			this.lblRatingLabel.AutoSize = true;
			this.lblRatingLabel.Location = new System.Drawing.Point(382, 166);
			this.lblRatingLabel.Name = "lblRatingLabel";
			this.lblRatingLabel.Size = new System.Drawing.Size(41, 13);
			this.lblRatingLabel.TabIndex = 13;
			this.lblRatingLabel.Tag = "Label_Rating";
			this.lblRatingLabel.Text = "Rating:";
			// 
			// cmdCancel
			// 
			this.cmdCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.cmdCancel.Location = new System.Drawing.Point(471, 382);
			this.cmdCancel.Name = "cmdCancel";
			this.cmdCancel.Size = new System.Drawing.Size(75, 23);
			this.cmdCancel.TabIndex = 25;
			this.cmdCancel.Tag = "String_Cancel";
			this.cmdCancel.Text = "Cancel";
			this.cmdCancel.UseVisualStyleBackColor = true;
			this.cmdCancel.Click += new System.EventHandler(this.cmdCancel_Click);
			// 
			// cmdOK
			// 
			this.cmdOK.Location = new System.Drawing.Point(552, 382);
			this.cmdOK.Name = "cmdOK";
			this.cmdOK.Size = new System.Drawing.Size(75, 23);
			this.cmdOK.TabIndex = 23;
			this.cmdOK.Tag = "String_OK";
			this.cmdOK.Text = "OK";
			this.cmdOK.UseVisualStyleBackColor = true;
			this.cmdOK.Click += new System.EventHandler(this.cmdOK_Click);
			// 
			// lstMod
			// 
			this.lstMod.FormattingEnabled = true;
			this.lstMod.Location = new System.Drawing.Point(12, 13);
			this.lstMod.Name = "lstMod";
			this.lstMod.Size = new System.Drawing.Size(364, 394);
			this.lstMod.TabIndex = 22;
			this.lstMod.SelectedIndexChanged += new System.EventHandler(this.lstMod_SelectedIndexChanged);
			this.lstMod.DoubleClick += new System.EventHandler(this.lstMod_DoubleClick);
			// 
			// lblSlots
			// 
			this.lblSlots.AutoSize = true;
			this.lblSlots.Location = new System.Drawing.Point(439, 143);
			this.lblSlots.Name = "lblSlots";
			this.lblSlots.Size = new System.Drawing.Size(19, 13);
			this.lblSlots.TabIndex = 12;
			this.lblSlots.Text = "[0]";
			// 
			// lblSlotsLabel
			// 
			this.lblSlotsLabel.AutoSize = true;
			this.lblSlotsLabel.Location = new System.Drawing.Point(382, 143);
			this.lblSlotsLabel.Name = "lblSlotsLabel";
			this.lblSlotsLabel.Size = new System.Drawing.Size(33, 13);
			this.lblSlotsLabel.TabIndex = 11;
			this.lblSlotsLabel.Tag = "Label_Slots";
			this.lblSlotsLabel.Text = "Slots:";
			// 
			// lblCategory
			// 
			this.lblCategory.AutoSize = true;
			this.lblCategory.Location = new System.Drawing.Point(439, 52);
			this.lblCategory.Name = "lblCategory";
			this.lblCategory.Size = new System.Drawing.Size(55, 13);
			this.lblCategory.TabIndex = 3;
			this.lblCategory.Text = "[Category]";
			// 
			// lblCategoryLabel
			// 
			this.lblCategoryLabel.AutoSize = true;
			this.lblCategoryLabel.Location = new System.Drawing.Point(382, 52);
			this.lblCategoryLabel.Name = "lblCategoryLabel";
			this.lblCategoryLabel.Size = new System.Drawing.Size(52, 13);
			this.lblCategoryLabel.TabIndex = 2;
			this.lblCategoryLabel.Tag = "Label_Category";
			this.lblCategoryLabel.Text = "Category:";
			// 
			// lblLimit
			// 
			this.lblLimit.AutoSize = true;
			this.lblLimit.Location = new System.Drawing.Point(439, 75);
			this.lblLimit.Name = "lblLimit";
			this.lblLimit.Size = new System.Drawing.Size(34, 13);
			this.lblLimit.TabIndex = 4;
			this.lblLimit.Text = "[Limit]";
			// 
			// cmdOKAdd
			// 
			this.cmdOKAdd.Location = new System.Drawing.Point(552, 353);
			this.cmdOKAdd.Name = "cmdOKAdd";
			this.cmdOKAdd.Size = new System.Drawing.Size(75, 23);
			this.cmdOKAdd.TabIndex = 24;
			this.cmdOKAdd.Tag = "String_AddMore";
			this.cmdOKAdd.Text = "&Add && More";
			this.cmdOKAdd.UseVisualStyleBackColor = true;
			this.cmdOKAdd.Click += new System.EventHandler(this.cmdOKAdd_Click);
			// 
			// lblSource
			// 
			this.lblSource.AutoSize = true;
			this.lblSource.Location = new System.Drawing.Point(433, 304);
			this.lblSource.Name = "lblSource";
			this.lblSource.Size = new System.Drawing.Size(47, 13);
			this.lblSource.TabIndex = 21;
			this.lblSource.Text = "[Source]";
			// 
			// lblSourceLabel
			// 
			this.lblSourceLabel.AutoSize = true;
			this.lblSourceLabel.Location = new System.Drawing.Point(382, 304);
			this.lblSourceLabel.Name = "lblSourceLabel";
			this.lblSourceLabel.Size = new System.Drawing.Size(44, 13);
			this.lblSourceLabel.TabIndex = 20;
			this.lblSourceLabel.Tag = "Label_Source";
			this.lblSourceLabel.Text = "Source:";
			// 
			// chkFreeItem
			// 
			this.chkFreeItem.AutoSize = true;
			this.chkFreeItem.Location = new System.Drawing.Point(385, 192);
			this.chkFreeItem.Name = "chkFreeItem";
			this.chkFreeItem.Size = new System.Drawing.Size(50, 17);
			this.chkFreeItem.TabIndex = 15;
			this.chkFreeItem.Tag = "Checkbox_Free";
			this.chkFreeItem.Text = "Free!";
			this.chkFreeItem.UseVisualStyleBackColor = true;
			this.chkFreeItem.Visible = false;
			this.chkFreeItem.CheckedChanged += new System.EventHandler(this.chkFreeItem_CheckedChanged);
			// 
			// txtSearch
			// 
			this.txtSearch.Location = new System.Drawing.Point(453, 12);
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
			this.lblSearchLabel.Location = new System.Drawing.Point(403, 15);
			this.lblSearchLabel.Name = "lblSearchLabel";
			this.lblSearchLabel.Size = new System.Drawing.Size(44, 13);
			this.lblSearchLabel.TabIndex = 0;
			this.lblSearchLabel.Tag = "Label_Search";
			this.lblSearchLabel.Text = "&Search:";
			// 
			// nudMarkup
			// 
			this.nudMarkup.Location = new System.Drawing.Point(442, 215);
			this.nudMarkup.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
			this.nudMarkup.Minimum = new decimal(new int[] {
            99,
            0,
            0,
            -2147483648});
			this.nudMarkup.Name = "nudMarkup";
			this.nudMarkup.Size = new System.Drawing.Size(56, 20);
			this.nudMarkup.TabIndex = 17;
			this.nudMarkup.ValueChanged += new System.EventHandler(this.nudMarkup_ValueChanged);
			// 
			// lblMarkupLabel
			// 
			this.lblMarkupLabel.AutoSize = true;
			this.lblMarkupLabel.Location = new System.Drawing.Point(382, 217);
			this.lblMarkupLabel.Name = "lblMarkupLabel";
			this.lblMarkupLabel.Size = new System.Drawing.Size(46, 13);
			this.lblMarkupLabel.TabIndex = 16;
			this.lblMarkupLabel.Tag = "Label_SelectGear_Markup";
			this.lblMarkupLabel.Text = "Markup:";
			// 
			// lblMarkupPercentLabel
			// 
			this.lblMarkupPercentLabel.AutoSize = true;
			this.lblMarkupPercentLabel.Location = new System.Drawing.Point(497, 217);
			this.lblMarkupPercentLabel.Name = "lblMarkupPercentLabel";
			this.lblMarkupPercentLabel.Size = new System.Drawing.Size(15, 13);
			this.lblMarkupPercentLabel.TabIndex = 18;
			this.lblMarkupPercentLabel.Text = "%";
			// 
			// lblTest
			// 
			this.lblTest.AutoSize = true;
			this.lblTest.Location = new System.Drawing.Point(548, 98);
			this.lblTest.Name = "lblTest";
			this.lblTest.Size = new System.Drawing.Size(19, 13);
			this.lblTest.TabIndex = 8;
			this.lblTest.Text = "[0]";
			// 
			// lblTestLabel
			// 
			this.lblTestLabel.AutoSize = true;
			this.lblTestLabel.Location = new System.Drawing.Point(497, 98);
			this.lblTestLabel.Name = "lblTestLabel";
			this.lblTestLabel.Size = new System.Drawing.Size(31, 13);
			this.lblTestLabel.TabIndex = 7;
			this.lblTestLabel.Tag = "Label_Test";
			this.lblTestLabel.Text = "Test:";
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
			// frmSelectVehicleMod
			// 
			this.AcceptButton = this.cmdOK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.cmdCancel;
			this.ClientSize = new System.Drawing.Size(639, 417);
			this.Controls.Add(this.lblTest);
			this.Controls.Add(this.lblTestLabel);
			this.Controls.Add(this.nudMarkup);
			this.Controls.Add(this.lblMarkupLabel);
			this.Controls.Add(this.lblMarkupPercentLabel);
			this.Controls.Add(this.txtSearch);
			this.Controls.Add(this.lblSearchLabel);
			this.Controls.Add(this.chkFreeItem);
			this.Controls.Add(this.lblSource);
			this.Controls.Add(this.lblSourceLabel);
			this.Controls.Add(this.cmdOKAdd);
			this.Controls.Add(this.lblLimit);
			this.Controls.Add(this.lblCategory);
			this.Controls.Add(this.lblCategoryLabel);
			this.Controls.Add(this.lblSlots);
			this.Controls.Add(this.lblSlotsLabel);
			this.Controls.Add(this.lblMaximumCapacity);
			this.Controls.Add(this.lblCost);
			this.Controls.Add(this.lblCostLabel);
			this.Controls.Add(this.lblAvail);
			this.Controls.Add(this.lblAvailLabel);
			this.Controls.Add(this.nudRating);
			this.Controls.Add(this.lblRatingLabel);
			this.Controls.Add(this.cmdCancel);
			this.Controls.Add(this.cmdOK);
			this.Controls.Add(this.lstMod);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "frmSelectVehicleMod";
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Tag = "Title_SelectVehicleMod";
			this.Text = "Select a Vehicle Modification";
			this.Load += new System.EventHandler(this.frmSelectVehicleMod_Load);
			((System.ComponentModel.ISupportInitialize)(this.nudRating)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.nudMarkup)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label lblMaximumCapacity;
		private System.Windows.Forms.Label lblCost;
		private System.Windows.Forms.Label lblCostLabel;
		private System.Windows.Forms.Label lblAvail;
		private System.Windows.Forms.Label lblAvailLabel;
		private System.Windows.Forms.NumericUpDown nudRating;
		private System.Windows.Forms.Label lblRatingLabel;
		private System.Windows.Forms.Button cmdCancel;
		private System.Windows.Forms.Button cmdOK;
		private System.Windows.Forms.ListBox lstMod;
		private System.Windows.Forms.Label lblSlots;
		private System.Windows.Forms.Label lblSlotsLabel;
		private System.Windows.Forms.Label lblCategory;
		private System.Windows.Forms.Label lblCategoryLabel;
		private System.Windows.Forms.Label lblLimit;
		private System.Windows.Forms.Button cmdOKAdd;
		private System.Windows.Forms.Label lblSource;
		private System.Windows.Forms.Label lblSourceLabel;
		private System.Windows.Forms.CheckBox chkFreeItem;
		private System.Windows.Forms.TextBox txtSearch;
		private System.Windows.Forms.Label lblSearchLabel;
		private System.Windows.Forms.NumericUpDown nudMarkup;
		private System.Windows.Forms.Label lblMarkupLabel;
		private System.Windows.Forms.Label lblMarkupPercentLabel;
		private System.Windows.Forms.Label lblTest;
		private System.Windows.Forms.Label lblTestLabel;
		private System.Windows.Forms.ToolTip tipTooltip;
	}
}