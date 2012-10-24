﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using System.Xml.XPath;

namespace Chummer
{
	public partial class frmSelectArmorMod : Form
	{
		private string _strSelectedArmorMod = "";

		private string _strAllowedCategories = "";
		private bool _blnAddAgain = false;
		private int _intArmorCost = 0;
		private int _intMarkup = 0;
		private CapacityStyle _objCapacityStyle = CapacityStyle.Zero;

		private XmlDocument _objXmlDocument = new XmlDocument();
		private readonly Character _objCharacter;

		#region Control Events
		public frmSelectArmorMod(Character objCharacter, bool blnCareer = false)
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

		private void frmSelectArmorMod_Load(object sender, EventArgs e)
		{
			foreach (Label objLabel in this.Controls.OfType<Label>())
			{
				if (objLabel.Text.StartsWith("["))
					objLabel.Text = "";
			}

			List<ListItem> lstMods = new List<ListItem>();

			// Load the Armor information.
			_objXmlDocument = XmlManager.Instance.Load("armor.xml");

			// Populate the Mods list.
			string[] strAllowed = _strAllowedCategories.Split(',');
			string strMount = "";
			foreach (string strAllowedMount in strAllowed)
			{
				if (strAllowedMount != "")
					strMount += "category = \"" + strAllowedMount + "\" or ";
			}
			strMount += "category = \"General\"";
			XmlNodeList objXmlModList = _objXmlDocument.SelectNodes("/chummer/mods/mod[" + strMount + " and (" + _objCharacter.Options.BookXPath() + ")]");

			foreach (XmlNode objXmlMod in objXmlModList)
			{
				ListItem objItem = new ListItem();
				objItem.Value = objXmlMod["name"].InnerText;
				if (objXmlMod["translate"] != null)
					objItem.Name = objXmlMod["translate"].InnerText;
				else
					objItem.Name = objXmlMod["name"].InnerText;
				lstMods.Add(objItem);
			}
			SortListItem objSort = new SortListItem();
			lstMods.Sort(objSort.Compare);
			lstMod.ValueMember = "Value";
			lstMod.DisplayMember = "Name";
			lstMod.DataSource = lstMods;
		}

		private void lstMod_SelectedIndexChanged(object sender, EventArgs e)
		{
			UpdateSelectedArmor();
		}

		private void cmdOK_Click(object sender, EventArgs e)
		{
			if (lstMod.Text != "")
				AcceptForm();
		}

		private void cmdCancel_Click(object sender, EventArgs e)
		{
			this.DialogResult = DialogResult.Cancel;
		}

		private void nudRating_ValueChanged(object sender, EventArgs e)
		{
			UpdateSelectedArmor();
		}

		private void lstMod_DoubleClick(object sender, EventArgs e)
		{
			if (lstMod.Text != "")
				AcceptForm();
		}

		private void cmdOKAdd_Click(object sender, EventArgs e)
		{
			_blnAddAgain = true;
			cmdOK_Click(sender, e);
		}

		private void chkFreeItem_CheckedChanged(object sender, EventArgs e)
		{
			UpdateSelectedArmor();
		}

		private void nudMarkup_ValueChanged(object sender, EventArgs e)
		{
			UpdateSelectedArmor();
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
		/// Armor's Cost.
		/// </summary>
		public int ArmorCost
		{
			set
			{
				_intArmorCost = value;
			}
		}

		/// <summary>
		/// Name of Accessory that was selected in the dialogue.
		/// </summary>
		public string SelectedArmorMod
		{
			get
			{
				return _strSelectedArmorMod;
			}
		}

		/// <summary>
		/// Rating that was selected in the dialogue.
		/// </summary>
		public int SelectedRating
		{
			get
			{
				return Convert.ToInt32(nudRating.Value);
			}
		}

		/// <summary>
		/// Categories that the Armor allows to be used.
		/// </summary>
		public string AllowedCategories
		{
			get
			{
				return _strAllowedCategories;
			}
			set
			{
				_strAllowedCategories = value;
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

		/// <summary>
		/// Capacity display style.
		/// </summary>
		public CapacityStyle CapacityDisplayStyle
		{
			set
			{
				_objCapacityStyle = value;
			}
		}
		#endregion

		#region Methods
		/// <summary>
		/// Update the information for the selected Armor Mod.
		/// </summary>
		private void UpdateSelectedArmor()
		{
			// Retireve the information for the selected Accessory.
			XmlNode objXmlMod = _objXmlDocument.SelectSingleNode("/chummer/mods/mod[name = \"" + lstMod.SelectedValue + "\"]");

			// Extract the Avil and Cost values from the Cyberware info since these may contain formulas and/or be based off of the Rating.
			// This is done using XPathExpression.
			XPathNavigator nav = _objXmlDocument.CreateNavigator();

			lblB.Text = objXmlMod["b"].InnerText;
			lblI.Text = objXmlMod["i"].InnerText;

			nudRating.Maximum = Convert.ToDecimal(objXmlMod["maxrating"].InnerText, GlobalOptions.Instance.CultureInfo);
			if (nudRating.Maximum == 1)
				nudRating.Enabled = false;
			else
				nudRating.Enabled = true;

			string strAvail = "";
			string strAvailExpr = "";
			strAvailExpr = objXmlMod["avail"].InnerText;
			
			XPathExpression xprAvail;
			if (strAvailExpr.Substring(strAvailExpr.Length - 1, 1) == "F" || strAvailExpr.Substring(strAvailExpr.Length - 1, 1) == "R")
			{
				strAvail = strAvailExpr.Substring(strAvailExpr.Length - 1, 1);
				// Remove the trailing character if it is "F" or "R".
				strAvailExpr = strAvailExpr.Substring(0, strAvailExpr.Length - 1);
			}
			try
			{
				xprAvail = nav.Compile(strAvailExpr.Replace("Rating", nudRating.Value.ToString()));
				lblAvail.Text = (Convert.ToInt32(nav.Evaluate(xprAvail))).ToString() + strAvail;
			}
			catch
			{
				lblAvail.Text = objXmlMod["avail"].InnerText;
			}
			lblAvail.Text = lblAvail.Text.Replace("R", LanguageManager.Instance.GetString("String_AvailRestricted")).Replace("F", LanguageManager.Instance.GetString("String_AvailForbidden"));

			// Cost.
			string strCost = objXmlMod["cost"].InnerText.Replace("Rating", nudRating.Value.ToString());
			strCost = strCost.Replace("Armor Cost", _intArmorCost.ToString());
			XPathExpression xprCost = nav.Compile(strCost);

			// Apply any markup.
			double dblCost = Convert.ToDouble(nav.Evaluate(xprCost), GlobalOptions.Instance.CultureInfo);
			dblCost *= 1 + (Convert.ToDouble(nudMarkup.Value, GlobalOptions.Instance.CultureInfo) / 100.0);

			lblCost.Text = String.Format("{0:###,###,##0¥}", dblCost);

			int intCost = Convert.ToInt32(dblCost);
			lblTest.Text = _objCharacter.AvailTest(intCost, lblAvail.Text);

			// Capacity.
			// XPathExpression cannot evaluate while there are square brackets, so remove them if necessary.
			string strCapacity = objXmlMod["armorcapacity"].InnerText;

			if (strCapacity.StartsWith("FixedValues"))
			{
				string[] strValues = strCapacity.Replace("FixedValues(", string.Empty).Replace(")", string.Empty).Split(',');
				strCapacity = strValues[Convert.ToInt32(nudRating.Value) - 1];
			}

			strCapacity = strCapacity.Substring(1, strCapacity.Length - 2);
			XPathExpression xprCapacity = nav.Compile(strCapacity.Replace("Rating", nudRating.Value.ToString()));

			if (_objCapacityStyle == CapacityStyle.Standard)
				lblCapacity.Text = "[" + nav.Evaluate(xprCapacity) + "]";
			else if (_objCapacityStyle == CapacityStyle.PerRating)
				lblCapacity.Text = "[" + nudRating.Value.ToString() + "]";
			else if (_objCapacityStyle == CapacityStyle.Zero)
				lblCapacity.Text = "[0]";

			if (chkFreeItem.Checked)
				lblCost.Text = String.Format("{0:###,###,##0¥}", 0);

			string strBook = _objCharacter.Options.LanguageBookShort(objXmlMod["source"].InnerText);
			string strPage = objXmlMod["page"].InnerText;
			if (objXmlMod["altpage"] != null)
				strPage = objXmlMod["altpage"].InnerText;
			lblSource.Text = strBook + " " + strPage;

			tipTooltip.SetToolTip(lblSource, _objCharacter.Options.LanguageBookLong(objXmlMod["source"].InnerText) + " " + LanguageManager.Instance.GetString("String_Page") + " " + strPage);
		}

		/// <summary>
		/// Accept the selected item and close the form.
		/// </summary>
		private void AcceptForm()
		{
			_strSelectedArmorMod = lstMod.SelectedValue.ToString();
			_intMarkup = Convert.ToInt32(nudMarkup.Value);
			this.DialogResult = DialogResult.OK;
		}

		private void MoveControls()
		{
			int intWidth = Math.Max(lblBLabel.Width, lblILabel.Width);
			intWidth = Math.Max(intWidth, lblRatingLabel.Width);
			intWidth = Math.Max(intWidth, lblCapacityLabel.Width);
			intWidth = Math.Max(intWidth, lblAvailLabel.Width);
			intWidth = Math.Max(intWidth, lblCostLabel.Width);

			lblB.Left = lblBLabel.Left + intWidth + 6;
			lblI.Left = lblILabel.Left + intWidth + 6;
			nudRating.Left = lblRatingLabel.Left + intWidth + 6;
			lblCapacity.Left = lblCapacityLabel.Left + intWidth + 6;
			lblAvail.Left = lblAvailLabel.Left + intWidth + 6;
			lblTestLabel.Left = lblAvail.Left + lblAvail.Width + 16;
			lblTest.Left = lblTestLabel.Left + lblTestLabel.Width + 6;
			lblCost.Left = lblCostLabel.Left + intWidth + 6;

			nudMarkup.Left = lblMarkupLabel.Left + lblMarkupLabel.Width + 6;
			lblMarkupPercentLabel.Left = nudMarkup.Left + nudMarkup.Width;

			lblSource.Left = lblSourceLabel.Left + lblSourceLabel.Width + 6;
		}
		#endregion
	}
}