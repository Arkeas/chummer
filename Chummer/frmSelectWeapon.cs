using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Xml;

namespace Chummer
{
    public partial class frmSelectWeapon : Form
    {
		private string _strSelectedWeapon = "";
		private int _intMarkup = 0;

		private bool _blnAddAgain = false;
		private static string _strSelectCategory = "";
		private readonly Character _objCharacter;

		private XmlDocument _objXmlDocument = new XmlDocument();

		private List<ListItem> _lstCategory = new List<ListItem>();

		#region Control Events
		public frmSelectWeapon(Character objCharacter, bool blnCareer = false)
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

        private void frmSelectWeapon_Load(object sender, EventArgs e)
        {
			foreach (Label objLabel in this.Controls.OfType<Label>())
			{
				if (objLabel.Text.StartsWith("["))
					objLabel.Text = "";
			}

        	// Load the Weapon information.
			_objXmlDocument = XmlManager.Instance.Load("weapons.xml");

            // Populate the Weapon Category list.
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

        private void cboCategory_SelectedIndexChanged(object sender, EventArgs e)
        {
        	List<ListItem> lstWeapons = new List<ListItem>();

			// Populate the Weapon list.
			XmlNodeList objXmlWeaponList = _objXmlDocument.SelectNodes("/chummer/weapons/weapon[category = \"" + cboCategory.SelectedValue + "\" and (" + _objCharacter.Options.BookXPath() + ")]");
			foreach (XmlNode objXmlWeapon in objXmlWeaponList)
			{
				ListItem objItem = new ListItem();
				objItem.Value = objXmlWeapon["name"].InnerText;
				if (objXmlWeapon["translate"] != null)
					objItem.Name = objXmlWeapon["translate"].InnerText;
				else
					objItem.Name = objXmlWeapon["name"].InnerText;
				lstWeapons.Add(objItem);
			}
			SortListItem objSort = new SortListItem();
			lstWeapons.Sort(objSort.Compare);
			lstWeapon.DataSource = null;
			lstWeapon.ValueMember = "Value";
			lstWeapon.DisplayMember = "Name";
			lstWeapon.DataSource = lstWeapons;
        }

        private void lstWeapon_SelectedIndexChanged(object sender, EventArgs e)
        {
			if (lstWeapon.Text == "")
				return;

            // Retireve the information for the selected Weapon.
        	XmlNode objXmlWeapon = _objXmlDocument.SelectSingleNode("/chummer/weapons/weapon[name = \"" + lstWeapon.SelectedValue + "\"]");

			Weapon objWeapon = new Weapon(_objCharacter);
			TreeNode objNode = new TreeNode();
			objWeapon.Create(objXmlWeapon, _objCharacter, objNode, null, null, null);

			lblWeaponReach.Text = objWeapon.TotalReach.ToString();
			lblWeaponDamage.Text = objWeapon.CalculatedDamage();
			lblWeaponAP.Text = objWeapon.TotalAP;
			lblWeaponMode.Text = objWeapon.CalculatedMode;
			lblWeaponRC.Text = objWeapon.TotalRC;
			lblWeaponAmmo.Text = objWeapon.CalculatedAmmo();
			lblWeaponAvail.Text = objWeapon.TotalAvail;

			int intItemCost = 0;
			double dblCost = Convert.ToDouble(objXmlWeapon["cost"].InnerText, GlobalOptions.Instance.CultureInfo);
			dblCost *= 1 + (Convert.ToDouble(nudMarkup.Value, GlobalOptions.Instance.CultureInfo) / 100.0);
			lblWeaponCost.Text = String.Format("{0:###,###,##0¥}", dblCost);
			try
			{
				intItemCost = Convert.ToInt32(dblCost);
			}
			catch
			{
			}

			if (chkFreeItem.Checked)
			{
				lblWeaponCost.Text = String.Format("{0:###,###,##0¥}", 0);
				intItemCost = 0;
			}

			lblTest.Text = _objCharacter.AvailTest(intItemCost, lblWeaponAvail.Text);

			string strBook = _objCharacter.Options.LanguageBookShort(objXmlWeapon["source"].InnerText);
			string strPage = objXmlWeapon["page"].InnerText;
			if (objXmlWeapon["altpage"] != null)
				strPage = objXmlWeapon["altpage"].InnerText;
			lblSource.Text = strBook + " " + strPage;

			// Build a list of included Accessories and Modifications that come with the weapon.
			string strAccessories = "";
			XmlNodeList objXmlNodeList = objXmlWeapon.SelectNodes("accessories/accessory");
			foreach (XmlNode objXmlAccessory in objXmlNodeList)
			{
				XmlNode objXmlItem = _objXmlDocument.SelectSingleNode("/chummer/accessories/accessory[name = \"" + objXmlAccessory.InnerText + "\"]");
				if (objXmlItem["translate"] != null)
					strAccessories += objXmlItem["translate"].InnerText + "\n";
				else
					strAccessories += objXmlItem["name"].InnerText + "\n";
			}
			objXmlNodeList = objXmlWeapon.SelectNodes("mods/mod");
			foreach (XmlNode objXmlMod in objXmlNodeList)
			{
				XmlNode objXmlItem = _objXmlDocument.SelectSingleNode("/chummer/mods/mod[name = \"" + objXmlMod.InnerText + "\"]");
				if (objXmlItem["translate"] != null)
					strAccessories += objXmlItem["translate"].InnerText + "\n";
				else
					strAccessories += objXmlItem["name"].InnerText + "\n";
			}
			if (strAccessories == "")
				lblIncludedAccessories.Text = LanguageManager.Instance.GetString("String_None");
			else
				lblIncludedAccessories.Text = strAccessories;

			tipTooltip.SetToolTip(lblSource, _objCharacter.Options.LanguageBookLong(objXmlWeapon["source"].InnerText) + " " + LanguageManager.Instance.GetString("String_Page") + " " + strPage);
        }

		private void cmdOK_Click(object sender, EventArgs e)
		{
			if (lstWeapon.Text != "")
				AcceptForm();
		}

		private void cmdCancel_Click(object sender, EventArgs e)
		{
			this.DialogResult = DialogResult.Cancel;
		}

		private void txtSearch_TextChanged(object sender, EventArgs e)
		{
			if (txtSearch.Text == "")
			{
				cboCategory_SelectedIndexChanged(sender, e);
				return;
			}

			// Treat everything as being uppercase so the search is case-insensitive.
			string strSearch = "/chummer/weapons/weapon[(" + _objCharacter.Options.BookXPath() + ") and category != \"Cyberware\" and category != \"Gear\" and ((contains(translate(name,'abcdefghijklmnopqrstuvwxyzàáâãäåçèéêëìíîïñòóôõöùúûüýß','ABCDEFGHIJKLMNOPQRSTUVWXYZÀÁÂÃÄÅÇÈÉÊËÌÍÎÏÑÒÓÔÕÖÙÚÛÜÝß'), \"" + txtSearch.Text.ToUpper() + "\") and not(translate)) or contains(translate(translate,'abcdefghijklmnopqrstuvwxyzàáâãäåçèéêëìíîïñòóôõöùúûüýß','ABCDEFGHIJKLMNOPQRSTUVWXYZÀÁÂÃÄÅÇÈÉÊËÌÍÎÏÑÒÓÔÕÖÙÚÛÜÝß'), \"" + txtSearch.Text.ToUpper() + "\"))]";

			XmlNodeList objXmlWeaponList = _objXmlDocument.SelectNodes(strSearch);
			List<ListItem> lstWeapons = new List<ListItem>();
			foreach (XmlNode objXmlWeapon in objXmlWeaponList)
			{
				ListItem objItem = new ListItem();
				objItem.Value = objXmlWeapon["name"].InnerText;
				if (objXmlWeapon["translate"] != null)
					objItem.Name = objXmlWeapon["translate"].InnerText;
				else
					objItem.Name = objXmlWeapon["name"].InnerText;

				try
				{
					objItem.Name += " [" + _lstCategory.Find(objFind => objFind.Value == objXmlWeapon["category"].InnerText).Name + "]";
					lstWeapons.Add(objItem);
				}
				catch
				{
				}
			}
			SortListItem objSort = new SortListItem();
			lstWeapons.Sort(objSort.Compare);
			lstWeapon.DataSource = null;
			lstWeapon.ValueMember = "Value";
			lstWeapon.DisplayMember = "Name";
			lstWeapon.DataSource = lstWeapons;
		}

		private void lstWeapon_DoubleClick(object sender, EventArgs e)
		{
			if (lstWeapon.Text != "")
				AcceptForm();
		}

		private void cmdOKAdd_Click(object sender, EventArgs e)
		{
			_blnAddAgain = true;
			cmdOK_Click(sender, e);
		}

		private void chkFreeItem_CheckedChanged(object sender, EventArgs e)
		{
			lstWeapon_SelectedIndexChanged(sender, e);
		}

		private void nudMarkup_ValueChanged(object sender, EventArgs e)
		{
			lstWeapon_SelectedIndexChanged(sender, e);
		}

		private void txtSearch_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Down)
			{
				try
				{
					lstWeapon.SelectedIndex++;
				}
				catch
				{
					try
					{
						lstWeapon.SelectedIndex = 0;
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
					lstWeapon.SelectedIndex--;
					if (lstWeapon.SelectedIndex == -1)
						lstWeapon.SelectedIndex = lstWeapon.Items.Count - 1;
				}
				catch
				{
					try
					{
						lstWeapon.SelectedIndex = lstWeapon.Items.Count - 1;
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
		/// Name of Weapon that was selected in the dialogue.
		/// </summary>
		public string SelectedWeapon
		{
			get
			{
				return _strSelectedWeapon;
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
			if (lstWeapon.Text != "")
			{
				XmlNode objNode = _objXmlDocument.SelectSingleNode("/chummer/weapons/weapon[name = \"" + lstWeapon.SelectedValue + "\"]");
				_strSelectCategory = objNode["category"].InnerText;
				_strSelectedWeapon = objNode["name"].InnerText;
				_intMarkup = Convert.ToInt32(nudMarkup.Value);

				this.DialogResult = DialogResult.OK;
			}
		}

		private void MoveControls()
		{
			int intWidth = Math.Max(lblWeaponDamageLabel.Width, lblWeaponAPLabel.Width);
			intWidth = Math.Max(intWidth, lblWeaponReachLabel.Width);
			intWidth = Math.Max(intWidth, lblWeaponAvailLabel.Width);
			intWidth = Math.Max(intWidth, lblWeaponCostLabel.Width);

			lblWeaponDamage.Left = lblWeaponDamageLabel.Left + intWidth + 6;
			lblWeaponAP.Left = lblWeaponAPLabel.Left + intWidth + 6;
			lblWeaponReach.Left = lblWeaponReachLabel.Left + intWidth + 6;
			lblWeaponAvail.Left = lblWeaponAvailLabel.Left + intWidth + 6;
			lblWeaponCost.Left = lblWeaponCostLabel.Left + intWidth + 6;

			lblWeaponRCLabel.Left = lblWeaponAP.Left + 74;
			lblWeaponRC.Left = lblWeaponRCLabel.Left + lblWeaponRCLabel.Width + 6;

			intWidth = Math.Max(lblWeaponAmmoLabel.Width, lblWeaponModeLabel.Width);
			intWidth = Math.Max(intWidth, lblTestLabel.Width);
			lblWeaponAmmoLabel.Left = lblWeaponAP.Left + 74;
			lblWeaponAmmo.Left = lblWeaponAmmoLabel.Left + intWidth + 6;
			lblWeaponModeLabel.Left = lblWeaponAP.Left + 74;
			lblWeaponMode.Left = lblWeaponModeLabel.Left + intWidth + 6;
			lblTestLabel.Left = lblWeaponAP.Left + 74;
			lblTest.Left = lblTestLabel.Left + intWidth + 6;

			nudMarkup.Left = lblMarkupLabel.Left + lblMarkupLabel.Width + 6;
			lblMarkupPercentLabel.Left = nudMarkup.Left + nudMarkup.Width;

			lblSource.Left = lblSourceLabel.Left + lblSourceLabel.Width + 6;

			lblSearchLabel.Left = txtSearch.Left - 6 - lblSearchLabel.Width;
		}
		#endregion
	}
}