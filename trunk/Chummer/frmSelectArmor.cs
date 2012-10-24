﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Xml;

namespace Chummer
{
	public partial class frmSelectArmor : Form
	{
		private string _strSelectedArmor = "";

		private bool _blnAddAgain = false;
		private static string _strSelectCategory = "";
		private int _intMarkup = 0;

		private XmlDocument _objXmlDocument = new XmlDocument();
		private readonly Character _objCharacter;

		private List<ListItem> _lstCategory = new List<ListItem>();

		#region Control Events
		public frmSelectArmor(Character objCharacter, bool blnCareer = false)
		{
			InitializeComponent();
			LanguageManager.Instance.Load(GlobalOptions.Instance.Language, this);
			chkFreeItem.Visible = blnCareer;
			lblMarkupLabel.Visible = blnCareer;
			nudMarkup.Visible = blnCareer;
			lblMarkupPercentLabel.Visible = blnCareer;
			_objCharacter = objCharacter;
			MoveControls();
		}

		private void frmSelectArmor_Load(object sender, EventArgs e)
		{
			foreach (Label objLabel in this.Controls.OfType<Label>())
			{
				if (objLabel.Text.StartsWith("["))
					objLabel.Text = "";
			}

			// Load the Armor information.
			_objXmlDocument = XmlManager.Instance.Load("armor.xml");

			// Populate the Armor Category list.
			XmlNodeList objXmlCategoryList = _objXmlDocument.SelectNodes("/chummer/categories/category");
			foreach (XmlNode objXmlCategory in objXmlCategoryList)
			{
				ListItem objItem = new ListItem();
				objItem.Value = objXmlCategory.InnerText;
				if (objXmlCategory.Attributes != null)
				{
					if (objXmlCategory.Attributes["translate"] != null)
						objItem.Name = objXmlCategory.Attributes["translate"].InnerText;
					else
						objItem.Name = objXmlCategory.InnerText;
				}
				else
					objItem.Name = objXmlCategory.InnerXml;
				_lstCategory.Add(objItem);
			}
			cboCategory.ValueMember = "Value";
			cboCategory.DisplayMember = "Name";
			cboCategory.DataSource = _lstCategory;

			// Select the first Category in the list.
			if (_strSelectCategory == "")
				cboCategory.SelectedIndex = 0;
			else
				cboCategory.SelectedValue = _strSelectCategory;

			if (cboCategory.SelectedIndex == -1)
				cboCategory.SelectedIndex = 0;
		}

		private void cmdOK_Click(object sender, EventArgs e)
		{
			AcceptForm();
		}

		private void lstArmor_DoubleClick(object sender, EventArgs e)
		{
			AcceptForm();
		}

		private void cmdCancel_Click(object sender, EventArgs e)
		{
			this.DialogResult = DialogResult.Cancel;
		}

		private void lstArmor_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (lstArmor.Text == "")
				return;

			// Get the information for the selected piece of Armor.
			XmlNode objXmlArmor = _objXmlDocument.SelectSingleNode("/chummer/armors/armor[name = \"" + lstArmor.SelectedValue + "\"]");
			// Create the Armor so we can show its Total Avail (some Armor includes Chemical Seal which adds +6 which wouldn't be factored in properly otherwise).
			Armor objArmor = new Armor(_objCharacter);
			TreeNode objNode = new TreeNode();
			objArmor.Create(objXmlArmor, objNode, null, true);

			lblBallistic.Text = objXmlArmor["b"].InnerText;
			lblImpact.Text = objXmlArmor["i"].InnerText;
			lblAvail.Text = objArmor.TotalAvail;

			// Check for a Variable Cost.
			int intItemCost = 0;
			if (objXmlArmor["cost"].InnerText.StartsWith("Variable"))
			{
				int intMin = 0;
				int intMax = 0;
				string strCost = objXmlArmor["cost"].InnerText.Replace("Variable(", string.Empty).Replace(")", string.Empty);
				if (strCost.Contains("-"))
				{
					string[] strValues = strCost.Split('-');
					intMin = Convert.ToInt32(strValues[0]);
					intMax = Convert.ToInt32(strValues[1]);
				}
				else
					intMin = Convert.ToInt32(strCost.Replace("+", string.Empty));

				if (intMax == 0)
				{
					intMax = 1000000;
					lblCost.Text = String.Format("{0:###,###,##0¥+}", intMin);
				}
				else
					lblCost.Text = String.Format("{0:###,###,##0}", intMin) + "-" + String.Format("{0:###,###,##0¥}", intMax);

				intItemCost = intMin;
			}
			else
			{
				double dblCost = Convert.ToDouble(objXmlArmor["cost"].InnerText, GlobalOptions.Instance.CultureInfo);
				dblCost *= 1 + (Convert.ToDouble(nudMarkup.Value, GlobalOptions.Instance.CultureInfo) / 100.0);
				lblCost.Text = String.Format("{0:###,###,##0¥}", dblCost);
				intItemCost = Convert.ToInt32(dblCost);
			}
			
			if (objXmlArmor["armorcapacity"].InnerText == "0" || objXmlArmor["armorcapacity"].InnerText == "")
			{
				double dblB = Math.Ceiling(Convert.ToDouble(objXmlArmor["b"].InnerText, GlobalOptions.Instance.CultureInfo) * 1.5);
				double dblI = Math.Ceiling(Convert.ToDouble(objXmlArmor["i"].InnerText, GlobalOptions.Instance.CultureInfo) * 1.5);
				double dblHighest = Math.Max(dblB, dblI);
				double dblReturn = Math.Max(dblHighest, 6.0);
				lblCapacity.Text = dblReturn.ToString();
			}
			else
				lblCapacity.Text = objXmlArmor["armorcapacity"].InnerText;

			if (chkFreeItem.Checked)
			{
				lblCost.Text = String.Format("{0:###,###,##0¥}", 0);
				intItemCost = 0;
			}

			lblTest.Text = _objCharacter.AvailTest(intItemCost, lblAvail.Text);

			string strBook = _objCharacter.Options.LanguageBookShort(objXmlArmor["source"].InnerText);
			string strPage = objXmlArmor["page"].InnerText;
			if (objXmlArmor["altpage"] != null)
				strPage = objXmlArmor["altpage"].InnerText;
			lblSource.Text = strBook + " " + strPage;

			tipTooltip.SetToolTip(lblSource, _objCharacter.Options.LanguageBookLong(objXmlArmor["source"].InnerText) + " " + LanguageManager.Instance.GetString("String_Page") + " " + strPage);
		}

		private void cboCategory_SelectedIndexChanged(object sender, EventArgs e)
		{
			List<ListItem> lstArmors = new List<ListItem>();

			// Populate the Armor list.
			XmlNodeList objXmlArmorList = _objXmlDocument.SelectNodes("/chummer/armors/armor[category = \"" + cboCategory.SelectedValue + "\" and (" + _objCharacter.Options.BookXPath() + ")]");
			foreach (XmlNode objXmlArmor in objXmlArmorList)
			{
				ListItem objItem = new ListItem();
				objItem.Value = objXmlArmor["name"].InnerText;
				if (objXmlArmor["translate"] != null)
					objItem.Name = objXmlArmor["translate"].InnerText;
				else
					objItem.Name = objXmlArmor["name"].InnerText;
				lstArmors.Add(objItem);
			}
			SortListItem objSort = new SortListItem();
			lstArmors.Sort(objSort.Compare);
			lstArmor.DataSource = null;
			lstArmor.ValueMember = "Value";
			lstArmor.DisplayMember = "Name";
			lstArmor.DataSource = lstArmors;
		}

		private void cmdOKAdd_Click(object sender, EventArgs e)
		{
			_blnAddAgain = true;
			cmdOK_Click(sender, e);
		}

		private void txtSearch_TextChanged(object sender, EventArgs e)
		{
			if (txtSearch.Text == "")
			{
				cboCategory_SelectedIndexChanged(sender, e);
				return;
			}

			string strCategoryFilter = "";

			foreach (object objListItem in cboCategory.Items)
			{
				ListItem objItem = (ListItem)objListItem;
				if (objItem.Value != "")
					strCategoryFilter += "category = \"" + objItem.Value + "\" or ";
			}

			// Treat everything as being uppercase so the search is case-insensitive.
			string strSearch = "/chummer/armors/armor[(" + _objCharacter.Options.BookXPath() + ") and ((contains(translate(name,'abcdefghijklmnopqrstuvwxyzàáâãäåçèéêëìíîïñòóôõöùúûüýß','ABCDEFGHIJKLMNOPQRSTUVWXYZÀÁÂÃÄÅÇÈÉÊËÌÍÎÏÑÒÓÔÕÖÙÚÛÜÝß'), \"" + txtSearch.Text.ToUpper() + "\") and not(translate)) or contains(translate(translate,'abcdefghijklmnopqrstuvwxyzàáâãäåçèéêëìíîïñòóôõöùúûüýß','ABCDEFGHIJKLMNOPQRSTUVWXYZÀÁÂÃÄÅÇÈÉÊËÌÍÎÏÑÒÓÔÕÖÙÚÛÜÝß'), \"" + txtSearch.Text.ToUpper() + "\"))";
			if (strCategoryFilter != "")
				strSearch += " and (" + strCategoryFilter + ")";
			// Remove the trailing " or )";
			if (strSearch.EndsWith(" or )"))
			{
				strSearch = strSearch.Substring(0, strSearch.Length - 4) + ")";
			}
			strSearch += "]";

			XmlNodeList objXmlArmorList = _objXmlDocument.SelectNodes(strSearch);
			List<ListItem> lstArmors = new List<ListItem>();
			foreach (XmlNode objXmlArmor in objXmlArmorList)
			{
				ListItem objItem = new ListItem();
				objItem.Value = objXmlArmor["name"].InnerText;
				if (objXmlArmor["translate"] != null)
					objItem.Name = objXmlArmor["translate"].InnerText;
				else
					objItem.Name = objXmlArmor["name"].InnerText;

				try
				{
					objItem.Name += " [" + _lstCategory.Find(objFind => objFind.Value == objXmlArmor["category"].InnerText).Name + "]";
					lstArmors.Add(objItem);
				}
				catch
				{
				}
			}
			SortListItem objSort = new SortListItem();
			lstArmors.Sort(objSort.Compare);
			lstArmor.DataSource = null;
			lstArmor.ValueMember = "Value";
			lstArmor.DisplayMember = "Name";
			lstArmor.DataSource = lstArmors;
		}

		private void chkFreeItem_CheckedChanged(object sender, EventArgs e)
		{
			lstArmor_SelectedIndexChanged(sender, e);
		}

		private void nudMarkup_ValueChanged(object sender, EventArgs e)
		{
			lstArmor_SelectedIndexChanged(sender, e);
		}

		private void txtSearch_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Down)
			{
				try
				{
					lstArmor.SelectedIndex++;
				}
				catch
				{
					try
					{
						lstArmor.SelectedIndex = 0;
					}
					catch
					{
					}
				}
			}
			if (e.KeyCode == Keys.Up)
			{
				try
				{
					lstArmor.SelectedIndex--;
					if (lstArmor.SelectedIndex == -1)
						lstArmor.SelectedIndex = lstArmor.Items.Count - 1;
				}
				catch
				{
					try
					{
						lstArmor.SelectedIndex = lstArmor.Items.Count - 1;
					}
					catch
					{
					}
				}
			}
		}

		private void txtSearch_KeyUp(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Up)
				txtSearch.Select(txtSearch.Text.Length, 0);
		}
		#endregion

		#region Properties
		/// <summary>
		/// Whether or not the user wants to add another item after this one.
		/// </summary>
		public bool AddAgain
		{
			get
			{
				return _blnAddAgain;
			}
		}

		/// <summary>
		/// Armor that was selected in the dialogue.
		/// </summary>
		public string SelectedArmor
		{
			get
			{
				return _strSelectedArmor;
			}
		}

		/// <summary>
		/// Whether or not the item should be added for free.
		/// </summary>
		public bool FreeCost
		{
			get
			{
				return chkFreeItem.Checked;
			}
		}

		/// <summary>
		/// Markup percentage.
		/// </summary>
		public int Markup
		{
			get
			{
				return _intMarkup;
			}
		}
		#endregion

		#region Methods
		/// <summary>
		/// Accept the selected item and close the form.
		/// </summary>
		private void AcceptForm()
		{
			if (lstArmor.Text != "")
			{
				XmlNode objNode = _objXmlDocument.SelectSingleNode("/chummer/armors/armor[name = \"" + lstArmor.SelectedValue + "\"]");
				_strSelectCategory = objNode["category"].InnerText;
				_strSelectedArmor = objNode["name"].InnerText;
				_intMarkup = Convert.ToInt32(nudMarkup.Value);

				this.DialogResult = DialogResult.OK;
			}
		}

		private void MoveControls()
		{
			int intWidth = Math.Max(lblBallisticLabel.Width, lblImpactLabel.Width);
			intWidth = Math.Max(intWidth, lblCapacityLabel.Width);
			intWidth = Math.Max(intWidth, lblAvailLabel.Width);
			intWidth = Math.Max(intWidth, lblCostLabel.Width);

			lblBallistic.Left = lblBallisticLabel.Left + intWidth + 6;
			lblImpact.Left = lblImpactLabel.Left + intWidth + 6;
			lblCapacity.Left = lblCapacityLabel.Left + intWidth + 6;
			lblAvail.Left = lblAvailLabel.Left + intWidth + 6;
			lblTestLabel.Left = lblAvail.Left + lblAvail.Width + 16;
			lblCost.Left = lblCostLabel.Left + intWidth + 6;

			nudMarkup.Left = lblMarkupLabel.Left + lblMarkupLabel.Width + 6;
			lblMarkupPercentLabel.Left = nudMarkup.Left + nudMarkup.Width;

			lblSource.Left = lblSourceLabel.Left + lblSourceLabel.Width + 6;

			lblSearchLabel.Left = txtSearch.Left - 6 - lblSearchLabel.Width;
		}
		#endregion
	}
}