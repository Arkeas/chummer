﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Chummer
{
	public partial class frmReload : Form
	{
		private List<Gear> _lstAmmo = new List<Gear>();
		private List<string> _lstCount = new List<string>();

		#region Control Events
		public frmReload()
		{
			InitializeComponent();
			LanguageManager.Instance.Load(GlobalOptions.Instance.Language, this);
			MoveControls();
		}

		private void frmReload_Load(object sender, EventArgs e)
		{
			List<ListItem> lstAmmo = new List<ListItem>();

			// Add each of the items to a new List since we need to also grab their plugin information.
			foreach (Gear objGear in _lstAmmo)
			{
				ListItem objAmmo = new ListItem();
				objAmmo.Value = objGear.InternalId;
				objAmmo.Name = objGear.DisplayNameShort;
				objAmmo.Name += " x" + objGear.Quantity.ToString();
				if (objGear.Parent != null)
				{
					if (objGear.Parent.DisplayNameShort != string.Empty)
					{
						objAmmo.Name += " (" + objGear.Parent.DisplayNameShort;
						if (objGear.Parent.Location != string.Empty)
							objAmmo.Name += " @ " + objGear.Parent.Location;
						objAmmo.Name += ")";
					}
				}
				if (objGear.Location != string.Empty)
					objAmmo.Name += " (" + objGear.Location + ")";
				// Retrieve the plugin information if it has any.
				if (objGear.Children.Count > 0)
				{
					string strPlugins = "";
					foreach (Gear objChild in objGear.Children)
					{
						strPlugins += objChild.DisplayNameShort + ", ";
					}
					// Remove the trailing comma.
					strPlugins = strPlugins.Substring(0, strPlugins.Length - 2);
					// Append the plugin information to the name.
					objAmmo.Name += " [" + strPlugins + "]";
				}
				lstAmmo.Add(objAmmo);
			}

			// Populate the lists.
			cboAmmo.DataSource = lstAmmo;
			cboAmmo.ValueMember = "Value";
			cboAmmo.DisplayMember = "Name";

			cboType.DataSource = _lstCount;

			// If there's only 1 value in each list, the character doesn't have a choice, so just accept it.
			if (cboAmmo.Items.Count == 1 && cboType.Items.Count == 1)
				AcceptForm();
		}

		private void cmdCancel_Click(object sender, EventArgs e)
		{
			this.DialogResult = DialogResult.Cancel;
		}

		private void cmdOK_Click(object sender, EventArgs e)
		{
			AcceptForm();
		}

		private void cboAmmo_DropDown(object sender, EventArgs e)
		{
			// Resize the width of the DropDown so that the longest name fits.
			ComboBox objSender = (ComboBox)sender;
			int intWidth = objSender.DropDownWidth;
			Graphics objGraphics = objSender.CreateGraphics();
			Font objFont = objSender.Font;
			int intScrollWidth = (objSender.Items.Count > objSender.MaxDropDownItems) ? SystemInformation.VerticalScrollBarWidth : 0;
			int intNewWidth;
			foreach (ListItem objItem in ((ComboBox)sender).Items)
			{
				intNewWidth = (int)objGraphics.MeasureString(objItem.Name, objFont).Width + intScrollWidth;
				if (intWidth < intNewWidth)
				{
					intWidth = intNewWidth;
				}
			}
			objSender.DropDownWidth = intWidth;
		}
		#endregion

		#region Properties
		/// <summary>
		/// List of Ammo Gear that the user can selected.
		/// </summary>
		public List<Gear> Ammo
		{
			set
			{
				_lstAmmo = value;
			}
		}
		
		/// <summary>
		/// List of ammunition that the user can select.
		/// </summary>
		public List<string> Count
		{
			set
			{
				_lstCount = value;
			}
		}

		/// <summary>
		/// Name of the ammunition that was selected.
		/// </summary>
		public string SelectedAmmo
		{
			get
			{
				return cboAmmo.SelectedValue.ToString();
			}
		}

		/// <summary>
		/// Number of rounds that were selected to be loaded.
		/// </summary>
		public int SelectedCount
		{
			get
			{
				return Convert.ToInt32(cboType.Text);
			}
		}
		#endregion

		#region Methods
		/// <summary>
		/// Accept the selected item and close the form.
		/// </summary>
		private void AcceptForm()
		{
			this.DialogResult = DialogResult.OK;
		}

		private void MoveControls()
		{
			int intWidth = Math.Max(lblAmmoLabel.Width, lblTypeLabel.Width);
			cboAmmo.Left = lblAmmoLabel.Left + intWidth + 6;
			cboAmmo.Width = this.Width - cboAmmo.Left - 19;
			cboType.Left = lblTypeLabel.Left + intWidth + 6;
			cboType.Width = this.Width - cboType.Left - 19;
		}
		#endregion
	}
}