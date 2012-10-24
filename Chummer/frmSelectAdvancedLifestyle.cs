using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Xml;

namespace Chummer
{
	public partial class frmSelectAdvancedLifestyle : Form
	{
		private bool _blnAddAgain = false;
		private Lifestyle _objLifestyle;
		private Lifestyle _objSourceLifestyle;
		private readonly Character _objCharacter;
		private LifestyleType _objType = LifestyleType.Advanced;

		private XmlDocument _objXmlDocument = new XmlDocument();

		private bool _blnSkipRefresh = false;

		#region Control Events
		public frmSelectAdvancedLifestyle(Lifestyle objLifestyle, Character objCharacter)
		{
			InitializeComponent();
			LanguageManager.Instance.Load(GlobalOptions.Instance.Language, this);
			_objCharacter = objCharacter;
			_objLifestyle = objLifestyle;
			MoveControls();
		}

		private void frmSelectAdvancedLifestyle_Load(object sender, EventArgs e)
		{
			_blnSkipRefresh = true;

			foreach (Label objLabel in this.Controls.OfType<Label>())
			{
				if (objLabel.Text.StartsWith("["))
					objLabel.Text = "";
			}

			// Load the Lifestyles information.
			_objXmlDocument = XmlManager.Instance.Load("lifestyles.xml");

			// Populate the Advanced Lifestyle ComboBoxes.
			// Comforts.
			List<ListItem> lstComforts = new List<ListItem>();
			foreach (XmlNode objXmlComfort in _objXmlDocument.SelectNodes("/chummer/comforts/comfort"))
			{
				bool blnAdd = true;
				if (_objType != LifestyleType.Advanced && objXmlComfort["slp"] != null)
				{
					if (objXmlComfort["slp"].InnerText == "remove")
						blnAdd = false;
				}

				if (blnAdd)
				{
					ListItem objItem = new ListItem();
					objItem.Value = objXmlComfort["name"].InnerText;
					if (objXmlComfort["translate"] != null)
						objItem.Name = objXmlComfort["translate"].InnerText;
					else
						objItem.Name = objXmlComfort["name"].InnerText;
					lstComforts.Add(objItem);
				}
			}
			cboComforts.ValueMember = "Value";
			cboComforts.DisplayMember = "Name";
			cboComforts.DataSource = lstComforts;

			cboComforts.SelectedValue = _objLifestyle.Comforts;
			if (cboComforts.SelectedIndex == -1)
				cboComforts.SelectedIndex = 0;

			// Entertainment.
			List<ListItem> lstEntertainments = new List<ListItem>();
			foreach (XmlNode objXmlEntertainment in _objXmlDocument.SelectNodes("/chummer/entertainments/entertainment"))
			{
				bool blnAdd = true;
				if (_objType != LifestyleType.Advanced && objXmlEntertainment["slp"] != null)
				{
					if (objXmlEntertainment["slp"].InnerText == "remove")
						blnAdd = false;
				}

				if (blnAdd)
				{
					ListItem objItem = new ListItem();
					objItem.Value = objXmlEntertainment["name"].InnerText;
					if (objXmlEntertainment["translate"] != null)
						objItem.Name = objXmlEntertainment["translate"].InnerText;
					else
						objItem.Name = objXmlEntertainment["name"].InnerText;
					lstEntertainments.Add(objItem);
				}
			}
			cboEntertainment.ValueMember = "Value";
			cboEntertainment.DisplayMember = "Name";
			cboEntertainment.DataSource = lstEntertainments;
			
			cboEntertainment.SelectedValue = _objLifestyle.Entertainment;
			if (cboEntertainment.SelectedIndex == -1)
				cboEntertainment.SelectedIndex = 0;

			// Necessities.
			List<ListItem> lstNecessities = new List<ListItem>();
			foreach (XmlNode objXmlNecessity in _objXmlDocument.SelectNodes("/chummer/necessities/necessity"))
			{
				bool blnAdd = true;
				if (_objType != LifestyleType.Advanced && objXmlNecessity["slp"] != null)
				{
					if (objXmlNecessity["slp"].InnerText == "remove")
						blnAdd = false;
				}

				if (blnAdd)
				{
					ListItem objItem = new ListItem();
					objItem.Value = objXmlNecessity["name"].InnerText;
					if (objXmlNecessity["translate"] != null)
						objItem.Name = objXmlNecessity["translate"].InnerText;
					else
						objItem.Name = objXmlNecessity["name"].InnerText;
					lstNecessities.Add(objItem);
				}
			}
			cboNecessities.ValueMember = "Value";
			cboNecessities.DisplayMember = "Name";
			cboNecessities.DataSource = lstNecessities;

			cboNecessities.SelectedValue = _objLifestyle.Necessities;
			if (cboNecessities.SelectedIndex == -1)
				cboNecessities.SelectedIndex = 0;

			// Neighborhood.
			List<ListItem> lstNeighborhoods = new List<ListItem>();
			foreach (XmlNode objXmlNeighborhood in _objXmlDocument.SelectNodes("/chummer/neighborhoods/neighborhood"))
			{
				bool blnAdd = true;
				if (_objType != LifestyleType.Advanced && objXmlNeighborhood["slp"] != null)
				{
					if (objXmlNeighborhood["slp"].InnerText == "remove")
						blnAdd = false;
				}

				if (blnAdd)
				{
					ListItem objItem = new ListItem();
					objItem.Value = objXmlNeighborhood["name"].InnerText;
					if (objXmlNeighborhood["translate"] != null)
						objItem.Name = objXmlNeighborhood["translate"].InnerText;
					else
						objItem.Name = objXmlNeighborhood["name"].InnerText;
					lstNeighborhoods.Add(objItem);
				}
			}
			cboNeighborhood.ValueMember = "Value";
			cboNeighborhood.DisplayMember = "Name";
			cboNeighborhood.DataSource = lstNeighborhoods;

			cboNeighborhood.SelectedValue = _objLifestyle.Neighborhood;
			if (cboNeighborhood.SelectedIndex == -1)
				cboNeighborhood.SelectedIndex = 0;

			// Security.
			List<ListItem> lstSecurities = new List<ListItem>();
			foreach (XmlNode objXmlSecurity in _objXmlDocument.SelectNodes("/chummer/securities/security"))
			{
				bool blnAdd = true;
				if (_objType != LifestyleType.Advanced && objXmlSecurity["slp"] != null)
				{
					if (objXmlSecurity["slp"].InnerText == "remove")
						blnAdd = false;
				}

				if (blnAdd)
				{
					ListItem objItem = new ListItem();
					objItem.Value = objXmlSecurity["name"].InnerText;
					if (objXmlSecurity["translate"] != null)
						objItem.Name = objXmlSecurity["translate"].InnerText;
					else
						objItem.Name = objXmlSecurity["name"].InnerText;
					lstSecurities.Add(objItem);
				}
			}
			cboSecurity.ValueMember = "Value";
			cboSecurity.DisplayMember = "Name";
			cboSecurity.DataSource = lstSecurities;

			cboSecurity.SelectedValue = _objLifestyle.Security;
			if (cboSecurity.SelectedIndex == -1)
				cboSecurity.SelectedIndex = 0;

			// Fill the Positive Qualities list.
			foreach (XmlNode objXmlQuality in _objXmlDocument.SelectNodes("/chummer/qualities/quality[category = \"Positive\" and contains(allowed, \"" + _objType.ToString() + "\") and (" + _objCharacter.Options.BookXPath() + ")]"))
			{
				TreeNode nodQuality = new TreeNode();
				nodQuality.Tag = objXmlQuality["name"].InnerText + " [" + objXmlQuality["lp"].InnerText + "LP]";
				if (objXmlQuality["translate"] != null)
					nodQuality.Text = objXmlQuality["translate"].InnerText + " [" + objXmlQuality["lp"].InnerText + "LP]";
				else
					nodQuality.Text = objXmlQuality["name"].InnerText + " [" + objXmlQuality["lp"].InnerText + "LP]";
				trePositiveQualities.Nodes.Add(nodQuality);
			}

			// Fill the Negative Qualities list.
			foreach (XmlNode objXmlQuality in _objXmlDocument.SelectNodes("/chummer/qualities/quality[category = \"Negative\" and contains(allowed, \"" + _objType.ToString() + "\") and (" + _objCharacter.Options.BookXPath() + ")]"))
			{
				TreeNode nodQuality = new TreeNode();
				nodQuality.Tag = objXmlQuality["name"].InnerText + " [" + objXmlQuality["lp"].InnerText + "LP]";
				if (objXmlQuality["translate"] != null)
					nodQuality.Text = objXmlQuality["translate"].InnerText + " [" + objXmlQuality["lp"].InnerText + "LP]";
				else
					nodQuality.Text = objXmlQuality["name"].InnerText + " [" + objXmlQuality["lp"].InnerText + "LP]";
				treNegativeQualities.Nodes.Add(nodQuality);
			}

			SortTree(trePositiveQualities);
			SortTree(treNegativeQualities);

			if (_objSourceLifestyle != null)
			{
				txtLifestyleName.Text = _objSourceLifestyle.Name;
				cboComforts.SelectedValue = _objSourceLifestyle.Comforts;
				cboEntertainment.SelectedValue = _objSourceLifestyle.Entertainment;
				cboNecessities.SelectedValue = _objSourceLifestyle.Necessities;
				cboNeighborhood.SelectedValue = _objSourceLifestyle.Neighborhood;
				cboSecurity.SelectedValue = _objSourceLifestyle.Security;
				nudRoommates.Value = _objSourceLifestyle.Roommates;
				nudPercentage.Value = _objSourceLifestyle.Percentage;
				foreach (string strQuality in _objSourceLifestyle.Qualities)
				{
					foreach (TreeNode objNode in trePositiveQualities.Nodes)
					{
						if (objNode.Tag.ToString() == strQuality)
						{
							objNode.Checked = true;
							break;
						}
					}
					foreach (TreeNode objNode in treNegativeQualities.Nodes)
					{
						if (objNode.Tag.ToString() == strQuality)
						{
							objNode.Checked = true;
							break;
						}
					}
				}
			}

			// Safehouses have a cost per week instead of cost per month.
			if (_objType == LifestyleType.Safehouse)
				lblCostLabel.Text = LanguageManager.Instance.GetString("Label_SelectLifestyle_CostPerWeek");

			_blnSkipRefresh = false;
			CalculateValues();
		}

		private void cmdOK_Click(object sender, EventArgs e)
		{
			if (txtLifestyleName.Text == "")
			{
				MessageBox.Show(LanguageManager.Instance.GetString("Message_SelectAdvancedLifestyle_LifestyleName"), LanguageManager.Instance.GetString("MessageTitle_SelectAdvancedLifestyle_LifestyleName"), MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}
			AcceptForm();
		}

		private void cmdCancel_Click(object sender, EventArgs e)
		{
			this.DialogResult = DialogResult.Cancel;
		}

		private void cmdOKAdd_Click(object sender, EventArgs e)
		{
			_blnAddAgain = true;
			cmdOK_Click(sender, e);
		}

		private void trePositiveQualities_AfterCheck(object sender, TreeViewEventArgs e)
		{
			CalculateValues();
		}

		private void treNegativeQualities_AfterCheck(object sender, TreeViewEventArgs e)
		{
			CalculateValues();
		}

		private void cboComforts_SelectedIndexChanged(object sender, EventArgs e)
		{
			CalculateValues();
		}

		private void cboEntertainment_SelectedIndexChanged(object sender, EventArgs e)
		{
			CalculateValues();
		}

		private void cboNecessities_SelectedIndexChanged(object sender, EventArgs e)
		{
			CalculateValues();
		}

		private void cboNeighborhood_SelectedIndexChanged(object sender, EventArgs e)
		{
			CalculateValues();
		}

		private void cboSecurity_SelectedIndexChanged(object sender, EventArgs e)
		{
			CalculateValues();
		}

		private void nudPercentage_ValueChanged(object sender, EventArgs e)
		{
			CalculateValues();
		}

		private void nudRoommates_ValueChanged(object sender, EventArgs e)
		{
			CalculateValues();
		}

		private void trePositiveQualities_AfterSelect(object sender, TreeViewEventArgs e)
		{
			string strQualityName = trePositiveQualities.SelectedNode.Tag.ToString().Substring(0, trePositiveQualities.SelectedNode.Tag.ToString().IndexOf('[') - 1);
			XmlNode objXmlQuality = _objXmlDocument.SelectSingleNode("/chummer/qualities/quality[name = \"" + strQualityName + "\"]");
			string strBook = _objCharacter.Options.LanguageBookShort(objXmlQuality["source"].InnerText);
			string strPage = objXmlQuality["page"].InnerText;
			if (objXmlQuality["altpage"] != null)
				strPage = objXmlQuality["altpage"].InnerText;
			lblSource.Text = strBook + " " + strPage;

			tipTooltip.SetToolTip(lblSource, _objCharacter.Options.LanguageBookLong(objXmlQuality["source"].InnerText) + " " + LanguageManager.Instance.GetString("String_Page") + " " + strPage);
		}

		private void treNegativeQualities_AfterSelect(object sender, TreeViewEventArgs e)
		{
			string strQualityName = treNegativeQualities.SelectedNode.Tag.ToString().Substring(0, treNegativeQualities.SelectedNode.Tag.ToString().IndexOf('[') - 1);
			XmlNode objXmlQuality = _objXmlDocument.SelectSingleNode("/chummer/qualities/quality[name = \"" + strQualityName + "\"]");
			string strBook = _objCharacter.Options.LanguageBookShort(objXmlQuality["source"].InnerText);
			string strPage = objXmlQuality["page"].InnerText;
			if (objXmlQuality["altpage"] != null)
				strPage = objXmlQuality["altpage"].InnerText;
			lblSource.Text = strBook + " " + strPage;

			tipTooltip.SetToolTip(lblSource, _objCharacter.Options.LanguageBookLong(objXmlQuality["source"].InnerText) + " " + LanguageManager.Instance.GetString("String_Page") + " " + strPage);
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
		/// Lifestyle that was created in the dialogue.
		/// </summary>
		public Lifestyle SelectedLifestyle
		{
			get
			{
				return _objLifestyle;
			}
		}

		/// <summary>
		/// Type of Lifestyle to create.
		/// </summary>
		public LifestyleType StyleType
		{
			get
			{
				return _objType;
			}
			set
			{
				_objType = value;
			}
		}
		#endregion

		#region Methods
		/// <summary>
		/// Accept the selected item and close the form.
		/// </summary>
		private void AcceptForm()
		{
			_objLifestyle.Source = "RC";
			_objLifestyle.Page = "154";
			_objLifestyle.Name = txtLifestyleName.Text;
			_objLifestyle.Comforts = cboComforts.SelectedValue.ToString();
			_objLifestyle.Entertainment = cboEntertainment.SelectedValue.ToString();
			_objLifestyle.Necessities = cboNecessities.SelectedValue.ToString();
			_objLifestyle.Neighborhood = cboNeighborhood.SelectedValue.ToString();
			_objLifestyle.Security = cboSecurity.SelectedValue.ToString();
			_objLifestyle.Cost = CalculateValues(false);
			_objLifestyle.Roommates = Convert.ToInt32(nudRoommates.Value);
			_objLifestyle.Percentage = Convert.ToInt32(nudPercentage.Value);
			_objLifestyle.Qualities.Clear();

			// Calculate the LP used by the Lifestyle (not including Qualities) and determine the appropriate Lifestyle to pull Starting Nuyen amounts from.
			int intEffectiveLP = 0;
			int intTotalLP = 0;

			// Calculate the cost of the 5 aspects. This determines the effective LP. Effective LP determines the effective equivalent Lifestyle (such as Medium) for determining the Starting Nuyen multiplier.
			XmlNode objXmlAspect = _objXmlDocument.SelectSingleNode("/chummer/comforts/comfort[name = \"" + cboComforts.SelectedValue + "\"]");
			intEffectiveLP += Convert.ToInt32(objXmlAspect["lp"].InnerText);
			objXmlAspect = _objXmlDocument.SelectSingleNode("/chummer/entertainments/entertainment[name = \"" + cboEntertainment.SelectedValue + "\"]");
			intEffectiveLP += Convert.ToInt32(objXmlAspect["lp"].InnerText);
			objXmlAspect = _objXmlDocument.SelectSingleNode("/chummer/necessities/necessity[name = \"" + cboNecessities.SelectedValue + "\"]");
			intEffectiveLP += Convert.ToInt32(objXmlAspect["lp"].InnerText);
			objXmlAspect = _objXmlDocument.SelectSingleNode("/chummer/neighborhoods/neighborhood[name = \"" + cboNeighborhood.SelectedValue + "\"]");
			intEffectiveLP += Convert.ToInt32(objXmlAspect["lp"].InnerText);
			objXmlAspect = _objXmlDocument.SelectSingleNode("/chummer/securities/security[name = \"" + cboSecurity.SelectedValue + "\"]");
			intEffectiveLP += Convert.ToInt32(objXmlAspect["lp"].InnerText);

			// Find the appropriate Lifestyle.
			XmlNode objXmlEffectiveLifestyle;
			if (intEffectiveLP >= 21)
				objXmlEffectiveLifestyle = _objXmlDocument.SelectSingleNode("/chummer/lifestyles/lifestyle[name = \"Luxury\"]");
			else if (intEffectiveLP >= 16 && intEffectiveLP <= 20)
				objXmlEffectiveLifestyle = _objXmlDocument.SelectSingleNode("/chummer/lifestyles/lifestyle[name = \"High\"]");
			else if (intEffectiveLP >= 11 && intEffectiveLP <= 15)
				objXmlEffectiveLifestyle = _objXmlDocument.SelectSingleNode("/chummer/lifestyles/lifestyle[name = \"Middle\"]");
			else if (intEffectiveLP >= 6 && intEffectiveLP <= 10)
				objXmlEffectiveLifestyle = _objXmlDocument.SelectSingleNode("/chummer/lifestyles/lifestyle[name = \"Low\"]");
			else if (intEffectiveLP >= 1 && intEffectiveLP <= 5)
				objXmlEffectiveLifestyle = _objXmlDocument.SelectSingleNode("/chummer/lifestyles/lifestyle[name = \"Squatter\"]");
			else
				objXmlEffectiveLifestyle = _objXmlDocument.SelectSingleNode("/chummer/lifestyles/lifestyle[name = \"Street\"]");

			// Add the Qualities LP to determine the total Lifestyle LP.
			// Calculate the cost of Positive Qualities.
			foreach (TreeNode objNode in trePositiveQualities.Nodes)
			{
				if (objNode.Checked)
				{
					objXmlAspect = _objXmlDocument.SelectSingleNode("/chummer/qualities/quality[name = \"" + GetQualityName(objNode.Tag.ToString()) + "\" and category = \"Positive\"]");
					intTotalLP += Convert.ToInt32(objXmlAspect["lp"].InnerText);
				}
			}

			// Calculate the cost of Negative Qualities.
			foreach (TreeNode objNode in treNegativeQualities.Nodes)
			{
				if (objNode.Checked)
				{
					objXmlAspect = _objXmlDocument.SelectSingleNode("/chummer/qualities/quality[name = \"" + GetQualityName(objNode.Tag.ToString()) + "\" and category = \"Negative\"]");
					intTotalLP += Convert.ToInt32(objXmlAspect["lp"].InnerText);
				}
			}

			// Find the appropriate Lifestyle.
			XmlNode objXmlActualLifestyle;
			if (intTotalLP >= 21)
				objXmlActualLifestyle = _objXmlDocument.SelectSingleNode("/chummer/lifestyles/lifestyle[name = \"Luxury\"]");
			else if (intTotalLP >= 16 && intTotalLP <= 20)
				objXmlActualLifestyle = _objXmlDocument.SelectSingleNode("/chummer/lifestyles/lifestyle[name = \"High\"]");
			else if (intTotalLP >= 11 && intTotalLP <= 15)
				objXmlActualLifestyle = _objXmlDocument.SelectSingleNode("/chummer/lifestyles/lifestyle[name = \"Middle\"]");
			else if (intTotalLP >= 6 && intTotalLP <= 10)
				objXmlActualLifestyle = _objXmlDocument.SelectSingleNode("/chummer/lifestyles/lifestyle[name = \"Low\"]");
			else if (intTotalLP >= 1 && intTotalLP <= 5)
				objXmlActualLifestyle = _objXmlDocument.SelectSingleNode("/chummer/lifestyles/lifestyle[name = \"Squatter\"]");
			else
				objXmlActualLifestyle = _objXmlDocument.SelectSingleNode("/chummer/lifestyles/lifestyle[name = \"Street\"]");

			// Get the starting Nuyen information.
			_objLifestyle.Dice = Convert.ToInt32(objXmlEffectiveLifestyle["dice"].InnerText);
			_objLifestyle.Multiplier = Convert.ToInt32(objXmlEffectiveLifestyle["multiplier"].InnerText);
			_objLifestyle.StyleType = _objType;

			foreach (TreeNode objNode in trePositiveQualities.Nodes)
			{
				if (objNode.Checked)
					_objLifestyle.Qualities.Add(objNode.Tag.ToString());
			}
			foreach (TreeNode objNode in treNegativeQualities.Nodes)
			{
				if (objNode.Checked)
					_objLifestyle.Qualities.Add(objNode.Tag.ToString());
			}

			this.DialogResult = DialogResult.OK;
		}

		/// <summary>
		/// Get the name of a Quality by parsing out its LP cost.
		/// </summary>
		/// <param name="strQuality">String to parse.</param>
		private string GetQualityName(string strQuality)
		{
			string strTemp = strQuality;
			int intPos = strTemp.IndexOf('[');

			strTemp = strTemp.Substring(0, intPos - 1);

			return strTemp;
		}

		/// <summary>
		/// Calculate the LP value for the selected items.
		/// </summary>
		private int CalculateValues(bool blnIncludePercentage = true)
		{
			if (_blnSkipRefresh)
				return 0;

			int intLP = 0;
			int intNuyen = 0;

			// Calculate the cost of the 5 aspects.
			// Comforts LP.
			XmlNode objXmlAspect = _objXmlDocument.SelectSingleNode("/chummer/comforts/comfort[name = \"" + cboComforts.SelectedValue + "\"]");
			if (_objType != LifestyleType.Advanced)
				if (objXmlAspect["slp"] != null)
					intLP += Convert.ToInt32(objXmlAspect["slp"].InnerText);
				else
					intLP += Convert.ToInt32(objXmlAspect["lp"].InnerText);
			else
				intLP += Convert.ToInt32(objXmlAspect["lp"].InnerText);

			// Entertainment LP.
			objXmlAspect = _objXmlDocument.SelectSingleNode("/chummer/entertainments/entertainment[name = \"" + cboEntertainment.SelectedValue + "\"]");
			if (_objType != LifestyleType.Advanced)
				if (objXmlAspect["slp"] != null)
					intLP += Convert.ToInt32(objXmlAspect["slp"].InnerText);
				else
					intLP += Convert.ToInt32(objXmlAspect["lp"].InnerText);
			else
				intLP += Convert.ToInt32(objXmlAspect["lp"].InnerText);

			// Necessities LP.
			objXmlAspect = _objXmlDocument.SelectSingleNode("/chummer/necessities/necessity[name = \"" + cboNecessities.SelectedValue + "\"]");
			if (_objType != LifestyleType.Advanced)
				if (objXmlAspect["slp"] != null)
					intLP += Convert.ToInt32(objXmlAspect["slp"].InnerText);
				else
					intLP += Convert.ToInt32(objXmlAspect["lp"].InnerText);
			else
				intLP += Convert.ToInt32(objXmlAspect["lp"].InnerText);

			// Neighborhood LP.
			objXmlAspect = _objXmlDocument.SelectSingleNode("/chummer/neighborhoods/neighborhood[name = \"" + cboNeighborhood.SelectedValue + "\"]");
			if (_objType != LifestyleType.Advanced)
				if (objXmlAspect["slp"] != null)
					intLP += Convert.ToInt32(objXmlAspect["slp"].InnerText);
				else
					intLP += Convert.ToInt32(objXmlAspect["lp"].InnerText);
			else
				intLP += Convert.ToInt32(objXmlAspect["lp"].InnerText);

			// Security LP.
			objXmlAspect = _objXmlDocument.SelectSingleNode("/chummer/securities/security[name = \"" + cboSecurity.SelectedValue + "\"]");
			if (_objType != LifestyleType.Advanced)
				if (objXmlAspect["slp"] != null)
					intLP += Convert.ToInt32(objXmlAspect["slp"].InnerText);
				else
					intLP += Convert.ToInt32(objXmlAspect["lp"].InnerText);
			else
				intLP += Convert.ToInt32(objXmlAspect["lp"].InnerText);

			// Calculate the cost of Positive Qualities.
			foreach (TreeNode objNode in trePositiveQualities.Nodes)
			{
				if (objNode.Checked)
				{
					objXmlAspect = _objXmlDocument.SelectSingleNode("/chummer/qualities/quality[name = \"" + GetQualityName(objNode.Tag.ToString()) + "\" and category = \"Positive\"]");
					intLP += Convert.ToInt32(objXmlAspect["lp"].InnerText);
				}
			}

			// Calculate the cost of Negative Qualities.
			foreach (TreeNode objNode in treNegativeQualities.Nodes)
			{
				if (objNode.Checked)
				{
					objXmlAspect = _objXmlDocument.SelectSingleNode("/chummer/qualities/quality[name = \"" + GetQualityName(objNode.Tag.ToString()) + "\" and category = \"Negative\"]");
					intLP += Convert.ToInt32(objXmlAspect["lp"].InnerText);
				}
			}

			if (intLP < 0)
				intLP = 0;

			// Determine the Nuyen cost.
			// Safehouses use the safehousecosts section instead as their pricing is slightly different.
			string strCostsName = "costs";
			if (_objType == LifestyleType.Safehouse)
				strCostsName = "safehousecosts";

			if (intLP < 29)
			{
				objXmlAspect = _objXmlDocument.SelectSingleNode("/chummer/" + strCostsName + "/cost[lp = \"" + intLP.ToString() + "\"]");
				intNuyen = Convert.ToInt32(objXmlAspect["cost"].InnerText);
			}
			else
			{
				objXmlAspect = _objXmlDocument.SelectSingleNode("/chummer/" + strCostsName + "/cost[lp = \"30\"]");
				intNuyen = Convert.ToInt32(objXmlAspect["cost"].InnerText);
				objXmlAspect = _objXmlDocument.SelectSingleNode("/chummer/" + strCostsName + "/cost[lp = \"31+\"]");
				intNuyen += ((intLP - 30) * Convert.ToInt32(objXmlAspect["cost"].InnerText));
			}

			if (blnIncludePercentage)
			{
				intNuyen = Convert.ToInt32(Convert.ToDouble(intNuyen, GlobalOptions.Instance.CultureInfo) * (1.0 + Convert.ToDouble(nudRoommates.Value / 10, GlobalOptions.Instance.CultureInfo)));
				intNuyen = Convert.ToInt32(Convert.ToDouble(intNuyen, GlobalOptions.Instance.CultureInfo) * Convert.ToDouble(nudPercentage.Value / 100, GlobalOptions.Instance.CultureInfo));
			}

			lblTotalLP.Text = intLP.ToString();
			lblCost.Text = String.Format("{0:###,###,##0¥}", intNuyen);

			return intNuyen;
		}

		/// <summary>
		/// Lifestyle to update when editing.
		/// </summary>
		/// <param name="objLifestyle">Lifestyle to edit.</param>
		public void SetLifestyle(Lifestyle objLifestyle)
		{
			_objSourceLifestyle = objLifestyle;
			_objType = objLifestyle.StyleType;
		}

		/// <summary>
		/// Sort the contents of a TreeView alphabetically.
		/// </summary>
		/// <param name="treTree">TreeView to sort.</param>
		private void SortTree(TreeView treTree)
		{
			List<TreeNode> lstNodes = new List<TreeNode>();
			foreach (TreeNode objNode in treTree.Nodes)
				lstNodes.Add(objNode);
			treTree.Nodes.Clear();
			try
			{
				SortByName objSort = new SortByName();
				lstNodes.Sort(objSort.Compare);
			}
			catch
			{
			}
			foreach (TreeNode objNode in lstNodes)
			treTree.Nodes.Add(objNode);
		}

		private void MoveControls()
		{
			int intLeft = 0;
			intLeft = Math.Max(lblLifestyleNameLabel.Left + lblLifestyleNameLabel.Width, lblComforts.Left + lblComforts.Width);
			intLeft = Math.Max(intLeft, lblEntertainment.Left + lblEntertainment.Width);
			intLeft = Math.Max(intLeft, lblNecessities.Left + lblNecessities.Width);
			intLeft = Math.Max(intLeft, lblNeighborhood.Left + lblNeighborhood.Width);
			intLeft = Math.Max(intLeft, lblSecurity.Left + lblSecurity.Width);

			txtLifestyleName.Left = intLeft + 6;
			cboComforts.Left = intLeft + 6;
			cboEntertainment.Left = intLeft + 6;
			cboNecessities.Left = intLeft + 6;
			cboNeighborhood.Left = intLeft + 6;
			cboSecurity.Left = intLeft + 6;
		}
		#endregion
	}
}