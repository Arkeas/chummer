﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using System.Xml.XPath;

namespace Chummer
{
	public partial class frmSelectQuality : Form
	{
		private string _strSelectedQuality = "";
		private bool _blnAddAgain = false;
		private readonly Character _objCharacter;
		private string _strIgnoreQuality = "";

		private XmlDocument _objXmlDocument = new XmlDocument();

		private List<ListItem> _lstCategory = new List<ListItem>();

		private static string _strSelectCategory = "";

		private readonly XmlDocument _objMetatypeDocument = new XmlDocument();
		private readonly XmlDocument _objCritterDocument = new XmlDocument();

		#region Control Events
		public frmSelectQuality(Character objCharacter)
		{
			InitializeComponent();
			LanguageManager.Instance.Load(GlobalOptions.Instance.Language, this);
			_objCharacter = objCharacter;

			_objMetatypeDocument = XmlManager.Instance.Load("metatypes.xml");
			_objCritterDocument = XmlManager.Instance.Load("critters.xml");

			MoveControls();
		}

		private void frmSelectQuality_Load(object sender, EventArgs e)
		{
			_objXmlDocument = XmlManager.Instance.Load("qualities.xml");

			foreach (Label objLabel in this.Controls.OfType<Label>())
			{
				if (objLabel.Text.StartsWith("["))
					objLabel.Text = "";
			}

			// Load the Quality information.
			_objXmlDocument = XmlManager.Instance.Load("qualities.xml");

			// Populate the Quality Category list.
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

			// Change the BP Label to Karma if the character is being built with Karma instead (or is in Career Mode).
			if (_objCharacter.BuildMethod == CharacterBuildMethod.Karma || _objCharacter.Created)
				lblBPLabel.Text = LanguageManager.Instance.GetString("Label_Karma");

			// Determine if the character should have access to the Metagenetic Qualities checkbox by checking if they meet the requirements for 360-degree Eyesight.
			// A dummy Quality is used since the character may take 360-degree Eyesight and we don't want it actually appearing in the list of Qualities.
			XmlNode objXmlNode = _objXmlDocument.SelectSingleNode("/chummer/qualities/quality[name = \"360-degree Eyesight\"]");
			XmlNode objXmlDummy = objXmlNode.Clone();
			objXmlDummy["name"].InnerText = "Dummy Quality";
			chkMetagenetic.Visible = RequirementMet(objXmlDummy, false);

			BuildQualityList();
		}

		private void cboCategory_SelectedIndexChanged(object sender, EventArgs e)
		{
			BuildQualityList();
		}

		private void lstQualities_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (lstQualities.Text == "")
				return;

			XmlNode objXmlQuality = _objXmlDocument.SelectSingleNode("/chummer/qualities/quality[name = \"" + lstQualities.SelectedValue + "\"]");
			if (_objCharacter.BuildMethod == CharacterBuildMethod.Karma || _objCharacter.Created)
			{
				int intBP = Convert.ToInt32(objXmlQuality["bp"].InnerText);
				lblBP.Text = (intBP * _objCharacter.Options.KarmaQuality).ToString();
			}
			else
				lblBP.Text = objXmlQuality["bp"].InnerText;
			if (chkFree.Checked)
				lblBP.Text = "0";

			string strBook = _objCharacter.Options.LanguageBookShort(objXmlQuality["source"].InnerText);
			string strPage = objXmlQuality["page"].InnerText;
			if (objXmlQuality["altpage"] != null)
				strPage = objXmlQuality["altpage"].InnerText;
			lblSource.Text = strBook + " " + strPage;

			tipTooltip.SetToolTip(lblSource, _objCharacter.Options.LanguageBookLong(objXmlQuality["source"].InnerText) + " " + LanguageManager.Instance.GetString("String_Page") + " " + strPage);
		}

		private void cmdOK_Click(object sender, EventArgs e)
		{
			_blnAddAgain = false;
			AcceptForm();
		}

		private void cmdOKAdd_Click(object sender, EventArgs e)
		{
			_blnAddAgain = true;
			AcceptForm();
		}

		private void cmdCancel_Click(object sender, EventArgs e)
		{
			this.DialogResult = DialogResult.Cancel;
		}

		private void lstQualities_DoubleClick(object sender, EventArgs e)
		{
			_blnAddAgain = false;
			AcceptForm();
		}

		private void chkLimitList_CheckedChanged(object sender, EventArgs e)
		{
			BuildQualityList();
		}

		private void chkFree_CheckedChanged(object sender, EventArgs e)
		{
			lstQualities_SelectedIndexChanged(sender, e);
		}

		private void chkMetagenetic_CheckedChanged(object sender, EventArgs e)
		{
			BuildQualityList();
		}

		private void txtSearch_TextChanged(object sender, EventArgs e)
		{
			BuildQualityList();
		}

		private void txtSearch_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Down)
			{
				try
				{
					lstQualities.SelectedIndex++;
				}
				catch
				{
					try
					{
						lstQualities.SelectedIndex = 0;
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
					lstQualities.SelectedIndex--;
					if (lstQualities.SelectedIndex == -1)
						lstQualities.SelectedIndex = lstQualities.Items.Count - 1;
				}
				catch
				{
					try
					{
						lstQualities.SelectedIndex = lstQualities.Items.Count - 1;
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
		/// Quality that was selected in the dialogue.
		/// </summary>
		public string SelectedQuality
		{
			get
			{
				return _strSelectedQuality;
			}
		}

		/// <summary>
		/// Forcefully add a Category to the list.
		/// </summary>
		public string ForceCategory
		{
			set
			{
				cboCategory.DataSource = null;
				cboCategory.Items.Add(value);
			}
		}

		/// <summary>
		/// A Quality the character has that should be ignored for checking Fobidden requirements (which would prevent upgrading/downgrading a Quality).
		/// </summary>
		public string IgnoreQuality
		{
			set
			{
				_strIgnoreQuality = value;
			}
		}

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
		/// Whether or not the item has no cost.
		/// </summary>
		public bool FreeCost
		{
			get
			{
				return chkFree.Checked;
			}
		}
		#endregion

		#region Methods
		/// <summary>
		/// Build the list of Qualities.
		/// </summary>
		private void BuildQualityList()
		{
			List<ListItem> lstQuality = new List<ListItem>();
			if (txtSearch.Text.Trim() != "")
			{
				// Treat everything as being uppercase so the search is case-insensitive.
				string strSearch = "/chummer/qualities/quality[(" + _objCharacter.Options.BookXPath() + ") and ((contains(translate(name,'abcdefghijklmnopqrstuvwxyzàáâãäåçèéêëìíîïñòóôõöùúûüýß','ABCDEFGHIJKLMNOPQRSTUVWXYZÀÁÂÃÄÅÇÈÉÊËÌÍÎÏÑÒÓÔÕÖÙÚÛÜÝß'), \"" + txtSearch.Text.ToUpper() + "\") and not(translate)) or contains(translate(translate,'abcdefghijklmnopqrstuvwxyzàáâãäåçèéêëìíîïñòóôõöùúûüýß','ABCDEFGHIJKLMNOPQRSTUVWXYZÀÁÂÃÄÅÇÈÉÊËÌÍÎÏÑÒÓÔÕÖÙÚÛÜÝß'), \"" + txtSearch.Text.ToUpper() + "\"))";
				if (chkMetagenetic.Checked)
				{
					strSearch += " and (required/oneof[contains(., 'Changeling (Class I SURGE)')] or metagenetic = 'yes')";
				}
				strSearch += "]";

				XmlNodeList objXmlQualityList = _objXmlDocument.SelectNodes(strSearch);
				foreach (XmlNode objXmlQuality in objXmlQualityList)
				{
					if (!chkLimitList.Checked || (chkLimitList.Checked && RequirementMet(objXmlQuality, false)))
					{
						ListItem objItem = new ListItem();
						objItem.Value = objXmlQuality["name"].InnerText;
						if (objXmlQuality["translate"] != null)
							objItem.Name = objXmlQuality["translate"].InnerText;
						else
							objItem.Name = objXmlQuality["name"].InnerText;

						try
						{
							objItem.Name += " [" + _lstCategory.Find(objFind => objFind.Value == objXmlQuality["category"].InnerText).Name + "]";
							lstQuality.Add(objItem);
						}
						catch
						{
						}
					}
				}
			}
			else
			{
				XmlDocument objXmlMetatypeDocument = new XmlDocument();
				if (_objCharacter.Metatype == "A.I." || _objCharacter.MetatypeCategory == "Technocritters" || _objCharacter.MetatypeCategory == "Protosapients")
					objXmlMetatypeDocument = XmlManager.Instance.Load("metatypes.xml");

				string strXPath = "category = \"" + cboCategory.SelectedValue + "\" and (" + _objCharacter.Options.BookXPath() + ")";
				if (chkMetagenetic.Checked)
				{
					strXPath += " and (required/oneof[contains(., 'Changeling (Class I SURGE)')] or metagenetic = 'yes')";
				}

				foreach (XmlNode objXmlQuality in _objXmlDocument.SelectNodes("/chummer/qualities/quality[" + strXPath + "]"))
				{
					if (objXmlQuality["name"].InnerText.StartsWith("Infected"))
					{
					}
					if ((_objCharacter.Metatype == "A.I." || _objCharacter.MetatypeCategory == "Technocritters" || _objCharacter.MetatypeCategory == "Protosapients") && chkLimitList.Checked)
					{
						XmlNode objXmlMetatype = objXmlMetatypeDocument.SelectSingleNode("/chummer/metatypes/metatype[name = \"" + _objCharacter.Metatype + "\"]");
						if (objXmlMetatype.SelectSingleNode("qualityrestriction/" + cboCategory.SelectedValue.ToString().ToLower() + "/quality[. = \"" + objXmlQuality["name"].InnerText + "\"]") != null)
						{
							if (!chkLimitList.Checked || (chkLimitList.Checked && RequirementMet(objXmlQuality, false)))
							{
								ListItem objItem = new ListItem();
								objItem.Value = objXmlQuality["name"].InnerText;
								if (objXmlQuality["translate"] != null)
									objItem.Name = objXmlQuality["translate"].InnerText;
								else
									objItem.Name = objXmlQuality["name"].InnerText;

								lstQuality.Add(objItem);
							}
						}
					}
					else
					{
						if (!chkLimitList.Checked || (chkLimitList.Checked && RequirementMet(objXmlQuality, false)))
						{
							ListItem objItem = new ListItem();
							objItem.Value = objXmlQuality["name"].InnerText;
							if (objXmlQuality["translate"] != null)
								objItem.Name = objXmlQuality["translate"].InnerText;
							else
								objItem.Name = objXmlQuality["name"].InnerText;

							lstQuality.Add(objItem);
						}
					}
				}
			}
			SortListItem objSort = new SortListItem();
			lstQuality.Sort(objSort.Compare);
			lstQualities.DataSource = null;
			lstQualities.ValueMember = "Value";
			lstQualities.DisplayMember = "Name";
			lstQualities.DataSource = lstQuality;
		}

		/// <summary>
		/// Accept the selected item and close the form.
		/// </summary>
		private void AcceptForm()
		{
			if (lstQualities.Text == "")
				return;

			XmlNode objNode = _objXmlDocument.SelectSingleNode("/chummer/qualities/quality[name = \"" + lstQualities.SelectedValue + "\"]");
			_strSelectedQuality = objNode["name"].InnerText;
			_strSelectCategory = objNode["category"].InnerText;

			if (!RequirementMet(objNode, true))
				return;

			this.DialogResult = DialogResult.OK;
		}

		/// <summary>
		/// Check if the Quality's requirements/restrictions are being met.
		/// </summary>
		/// <param name="objXmlQuality">XmlNode of the Quality.</param>
		/// <param name="blnShowMessage">Whether or not a message should be shown if the requirements are not met.</param>
		private bool RequirementMet(XmlNode objXmlQuality, bool blnShowMessage)
		{
			// Ignore the rules.
			if (_objCharacter.IgnoreRules)
				return true;

			// See if the character already has this Quality and whether or not multiple copies are allowed.
			bool blnAllowMultiple = false;
			if (objXmlQuality["limit"] != null)
			{
				if (objXmlQuality["limit"].InnerText == "no")
					blnAllowMultiple = true;
			}
			if (!blnAllowMultiple)
			{
				// Multiples aren't allowed, so make sure the character does not already have it.
				foreach (Quality objQuality in _objCharacter.Qualities)
				{
					if (objQuality.Name == objXmlQuality["name"].InnerText)
					{
						if (blnShowMessage)
							MessageBox.Show(LanguageManager.Instance.GetString("Message_SelectQuality_QualityLimit"), LanguageManager.Instance.GetString("MessageTitle_SelectQuality_QualityLimit"), MessageBoxButtons.OK, MessageBoxIcon.Information);
						return false;
					}
				}
			}

			if (objXmlQuality.InnerXml.Contains("forbidden"))
			{
				bool blnRequirementForbidden = false;
				string strForbidden = "";

				// Loop through the oneof requirements.
				XmlNodeList objXmlForbiddenList = objXmlQuality.SelectNodes("forbidden/oneof");
				foreach (XmlNode objXmlOneOf in objXmlForbiddenList)
				{
					XmlNodeList objXmlOneOfList = objXmlOneOf.ChildNodes;

					foreach (XmlNode objXmlForbidden in objXmlOneOfList)
					{
						if (objXmlForbidden.Name == "quality")
						{
							// Run through all of the Qualities the character has and see if the current forbidden item exists.
							// If so, turn on the RequirementForbidden flag so it cannot be selected.
							foreach (Quality objCharacterQuality in _objCharacter.Qualities)
							{
								if (objCharacterQuality.Name == objXmlForbidden.InnerText && objCharacterQuality.Name != _strIgnoreQuality)
								{
									blnRequirementForbidden = true;
									strForbidden += "\n\t" + objCharacterQuality.DisplayNameShort;
								}
							}
						}
						else if (objXmlForbidden.Name == "metatype")
						{
							// Check the Metatype restriction.
							if (objXmlForbidden.InnerText == _objCharacter.Metatype)
							{
								blnRequirementForbidden = true;
								XmlNode objNode = _objMetatypeDocument.SelectSingleNode("/chummer/metatypes/metatype[name = \"" + objXmlForbidden.InnerText + "\"]");
								if (objNode == null)
									objNode = _objCritterDocument.SelectSingleNode("/chummer/metatypes/metatype[name = \"" + objXmlForbidden.InnerText + "\"]");
								if (objNode["translate"] != null)
									strForbidden += "\n\t" + objNode["translate"].InnerText;
								else
									strForbidden += "\n\t" + objXmlForbidden.InnerText;
							}
						}
						else if (objXmlForbidden.Name == "metatypecategory")
						{
							// Check the Metatype Category restriction.
							if (objXmlForbidden.InnerText == _objCharacter.MetatypeCategory)
							{
								blnRequirementForbidden = true;
								XmlNode objNode = _objMetatypeDocument.SelectSingleNode("/chummer/categories/category[. = \"" + objXmlForbidden.InnerText + "\"]");
								if (objNode == null)
									objNode = _objCritterDocument.SelectSingleNode("/chummer/categories/category[. = \"" + objXmlForbidden.InnerText + "\"]");
								if (objNode.Attributes["translate"] != null)
									strForbidden += "\n\t" + objNode.Attributes["translate"].InnerText;
								else
									strForbidden += "\n\t" + objXmlForbidden.InnerText;
							}
						}
						else if (objXmlForbidden.Name == "metavariant")
						{
							// Check the Metavariant restriction.
							if (objXmlForbidden.InnerText == _objCharacter.Metavariant)
							{
								blnRequirementForbidden = true;
								XmlNode objNode = _objMetatypeDocument.SelectSingleNode("/chummer/metatypes/metatype/metavariants/metavariant[name = \"" + objXmlForbidden.InnerText + "\"]");
								if (objNode == null)
									objNode = _objCritterDocument.SelectSingleNode("/chummer/metatypes/metatype/metavariants/metavariant[name = \"" + objXmlForbidden.InnerText + "\"]");
								if (objNode["translate"] != null)
									strForbidden += "\n\t" + objNode["translate"].InnerText;
								else
									strForbidden += "\n\t" + objXmlForbidden.InnerText;
							}
						}
						else if (objXmlForbidden.Name == "metagenetic")
						{
							// Check to see if the character has a Metagenetic Quality.
							foreach (Quality objQuality in _objCharacter.Qualities)
							{
								XmlNode objXmlCheck = _objXmlDocument.SelectSingleNode("/chummer/qualities/quality[name = \"" + objQuality.Name + "\"]");
								if (objXmlCheck["metagenetic"] != null)
								{
									if (objXmlCheck["metagenetic"].InnerText.ToLower() == "yes")
									{
										blnRequirementForbidden = true;
										strForbidden += "\n\t" + objQuality.DisplayName;
										break;
									}
								}
							}
						}
					}
				}

				// The character is not allowed to take the Quality, so display a message and uncheck the item.
				if (blnRequirementForbidden)
				{
					if (blnShowMessage)
						MessageBox.Show(LanguageManager.Instance.GetString("Message_SelectQuality_QualityRestriction") + strForbidden, LanguageManager.Instance.GetString("MessageTitle_SelectQuality_QualityRestriction"), MessageBoxButtons.OK, MessageBoxIcon.Information);
					return false;
				}
			}

			if (objXmlQuality.InnerXml.Contains("required"))
			{
				string strRequirement = "";
				bool blnRequirementMet = true;

				// Loop through the oneof requirements.
				XmlNodeList objXmlRequiredList = objXmlQuality.SelectNodes("required/oneof");
				foreach (XmlNode objXmlOneOf in objXmlRequiredList)
				{
					bool blnOneOfMet = false;
					string strThisRequirement = "\n" + LanguageManager.Instance.GetString("Message_SelectQuality_OneOf");
					XmlNodeList objXmlOneOfList = objXmlOneOf.ChildNodes;
					foreach (XmlNode objXmlRequired in objXmlOneOfList)
					{
						if (objXmlRequired.Name == "quality")
						{
							// Run through all of the Qualities the character has and see if the current required item exists.
							// If so, turn on the RequirementMet flag so it can be selected.
							foreach (Quality objCharacterQuality in _objCharacter.Qualities)
							{
								if (objCharacterQuality.Name == objXmlRequired.InnerText)
									blnOneOfMet = true;
							}

							if (!blnOneOfMet)
							{
								XmlNode objNode = _objXmlDocument.SelectSingleNode("/chummer/qualities/quality[name = \"" + objXmlRequired.InnerText + "\"]");
								if (objNode["translate"] != null)
									strThisRequirement += "\n\t" + objNode["translate"].InnerText;
								else
									strThisRequirement += "\n\t" + objXmlRequired.InnerText;
							}
						}
						else if (objXmlRequired.Name == "metatype")
						{
							// Check the Metatype requirement.
							if (objXmlRequired.InnerText == _objCharacter.Metatype)
								blnOneOfMet = true;
							else
							{
								XmlNode objNode = _objMetatypeDocument.SelectSingleNode("/chummer/metatypes/metatype[name = \"" + objXmlRequired.InnerText + "\"]");
								if (objNode == null)
									objNode = _objCritterDocument.SelectSingleNode("/chummer/metatypes/metatype[name = \"" + objXmlRequired.InnerText + "\"]");
								if (objNode["translate"] != null)
									strThisRequirement += "\n\t" + objNode["translate"].InnerText;
								else
									strThisRequirement += "\n\t" + objXmlRequired.InnerText;
							}
						}
						else if (objXmlRequired.Name == "metatypecategory")
						{
							// Check the Metatype Category requirement.
							if (objXmlRequired.InnerText == _objCharacter.MetatypeCategory)
								blnOneOfMet = true;
							else
							{
								XmlNode objNode = _objMetatypeDocument.SelectSingleNode("/chummer/categories/category[. = \"" + objXmlRequired.InnerText + "\"]");
								if (objNode == null)
									objNode = _objCritterDocument.SelectSingleNode("/chummer/categories/category[. = \"" + objXmlRequired.InnerText + "\"]");
								if (objNode.Attributes["translate"] != null)
									strThisRequirement += "\n\t" + objNode.Attributes["translate"].InnerText;
								else
									strThisRequirement = "\n\t" + objXmlRequired.InnerText;
							}
						}
						else if (objXmlRequired.Name == "metavariant")
						{
							// Check the Metavariant requirement.
							if (objXmlRequired.InnerText == _objCharacter.Metavariant)
								blnOneOfMet = true;
							else
							{
								XmlNode objNode = _objMetatypeDocument.SelectSingleNode("/chummer/metatypes/metatype/metavariants/metavariant[name = \"" + objXmlRequired.InnerText + "\"]");
								if (objNode == null)
									objNode = _objCritterDocument.SelectSingleNode("/chummer/metatypes/metatype/metavariants/metavariant[name = \"" + objXmlRequired.InnerText + "\"]");
								if (objNode["translate"] != null)
									strThisRequirement += "\n\t" + objNode["translate"].InnerText;
								else
									strThisRequirement += "\n\t" + objXmlRequired.InnerText;
							}
						}
						else if (objXmlRequired.Name == "inherited")
						{
							strThisRequirement += "\n\t" + LanguageManager.Instance.GetString("Message_SelectQuality_Inherit");
						}
						else if (objXmlRequired.Name == "careerkarma")
						{
							// Check Career Karma requirement.
							if (_objCharacter.CareerKarma >= Convert.ToInt32(objXmlRequired.InnerText))
								blnOneOfMet = true;
							else
								strThisRequirement = "\n\t" + LanguageManager.Instance.GetString("Message_SelectQuality_RequireKarma").Replace("{0}", objXmlRequired.InnerText);
						}
						else if (objXmlRequired.Name == "ess")
						{
							// Check Essence requirement.
							if (objXmlRequired.InnerText.StartsWith("-"))
							{
								// Essence must be less than the value.
								if (_objCharacter.Essence < Convert.ToDecimal(objXmlRequired.InnerText.Replace("-", string.Empty), GlobalOptions.Instance.CultureInfo))
									blnOneOfMet = true;
							}
							else
							{
								// Essence must be equal to or greater than the value.
								if (_objCharacter.Essence >= Convert.ToDecimal(objXmlRequired.InnerText, GlobalOptions.Instance.CultureInfo))
									blnOneOfMet = true;
							}

						}
						else if (objXmlRequired.Name == "skill")
						{
							// Check if the character has the required Skill.
							foreach (Skill objSkill in _objCharacter.Skills)
							{
								if (objSkill.Name == objXmlRequired["name"].InnerText)
								{
									if (objSkill.Rating >= Convert.ToInt32(objXmlRequired["val"].InnerText))
									{
										blnOneOfMet = true;
										break;
									}
								}
							}
						}
						else if (objXmlRequired.Name == "attribute")
						{
							// Check to see if an Attribute meets a requirement.
							Attribute objAttribute = _objCharacter.GetAttribute(objXmlRequired["name"].InnerText);

							if (objXmlRequired["total"] != null)
							{
								// Make sure the Attribute's total value meets the requirement.
								if (objAttribute.TotalValue >= Convert.ToInt32(objXmlRequired["total"].InnerText))
									blnOneOfMet = true;
							}
						}
						else if (objXmlRequired.Name == "attributetotal")
						{
							// Check if the character's Attributes add up to a particular total.
							string strAttributes = objXmlRequired["attributes"].InnerText;
							strAttributes = strAttributes.Replace("BOD", _objCharacter.GetAttribute("BOD").Value.ToString());
							strAttributes = strAttributes.Replace("AGI", _objCharacter.GetAttribute("AGI").Value.ToString());
							strAttributes = strAttributes.Replace("REA", _objCharacter.GetAttribute("REA").Value.ToString());
							strAttributes = strAttributes.Replace("STR", _objCharacter.GetAttribute("STR").Value.ToString());
							strAttributes = strAttributes.Replace("CHA", _objCharacter.GetAttribute("CHA").Value.ToString());
							strAttributes = strAttributes.Replace("INT", _objCharacter.GetAttribute("INT").Value.ToString());
							strAttributes = strAttributes.Replace("INT", _objCharacter.GetAttribute("LOG").Value.ToString());
							strAttributes = strAttributes.Replace("WIL", _objCharacter.GetAttribute("WIL").Value.ToString());
							strAttributes = strAttributes.Replace("MAG", _objCharacter.GetAttribute("MAG").Value.ToString());
							strAttributes = strAttributes.Replace("RES", _objCharacter.GetAttribute("RES").Value.ToString());
							strAttributes = strAttributes.Replace("EDG", _objCharacter.GetAttribute("EDG").Value.ToString());

							XmlDocument objXmlDocument = new XmlDocument();
							XPathNavigator nav = objXmlDocument.CreateNavigator();
							XPathExpression xprAttributes = nav.Compile(strAttributes);
							if (Convert.ToInt32(nav.Evaluate(xprAttributes)) >= Convert.ToInt32(objXmlRequired["val"].InnerText))
								blnOneOfMet = true;
						}
						else if (objXmlRequired.Name == "skillgrouptotal")
						{
							// Check if the total combined Ratings of Skill Groups adds up to a particular total.
							int intTotal = 0;
							string[] strGroups = objXmlRequired["skillgroups"].InnerText.Split('+');
							for (int i = 0; i <= strGroups.Length - 1; i++)
							{
								foreach (SkillGroup objGroup in _objCharacter.SkillGroups)
								{
									if (objGroup.Name == strGroups[i])
									{
										intTotal += objGroup.Rating;
										break;
									}
								}
							}

							if (intTotal >= Convert.ToInt32(objXmlRequired["val"].InnerText))
								blnOneOfMet = true;
						}
						else if (objXmlRequired.Name == "cyberwares")
						{
							// Check to see if the character has a number of the required Cyberware/Bioware items.
							int intTotal = 0;

							// Check Cyberware.
							foreach (XmlNode objXmlCyberware in objXmlRequired.SelectNodes("cyberware"))
							{
								foreach (Cyberware objCyberware in _objCharacter.Cyberware)
								{
									if (objCyberware.Name == objXmlCyberware.InnerText)
									{
										if (objXmlCyberware.Attributes["select"] == null)
										{
											intTotal++;
											break;
										}
										else if (objXmlCyberware.Attributes["select"].InnerText == objCyberware.Location)
										{
											intTotal++;
											break;
										}
									}
								}
							}

							// Check Bioware.
							foreach (XmlNode objXmlBioware in objXmlRequired.SelectNodes("bioware"))
							{
								foreach (Cyberware objCyberware in _objCharacter.Cyberware)
								{
									if (objCyberware.Name == objXmlBioware.InnerText)
									{
										intTotal++;
										break;
									}
								}
							}

							// Check Cyberware name that contain a straing.
							foreach (XmlNode objXmlCyberware in objXmlRequired.SelectNodes("cyberwarecontains"))
							{
								foreach (Cyberware objCyberware in _objCharacter.Cyberware)
								{
									if (objCyberware.Name.Contains(objXmlCyberware.InnerText))
									{
										if (objXmlCyberware.Attributes["select"] == null)
										{
											intTotal++;
											break;
										}
										else if (objXmlCyberware.Attributes["select"].InnerText == objCyberware.Location)
										{
											intTotal++;
											break;
										}
									}
								}
							}

							// Check Bioware name that contain a straing.
							foreach (XmlNode objXmlCyberware in objXmlRequired.SelectNodes("biowarecontains"))
							{
								foreach (Cyberware objCyberware in _objCharacter.Cyberware)
								{
									if (objCyberware.Name.Contains(objXmlCyberware.InnerText))
									{
										if (objXmlCyberware.Attributes["select"] == null)
										{
											intTotal++;
											break;
										}
										else if (objXmlCyberware.Attributes["select"].InnerText == objCyberware.Location)
										{
											intTotal++;
											break;
										}
									}
								}
							}

							// Check for Cyberware Plugins.
							foreach (XmlNode objXmlCyberware in objXmlRequired.SelectNodes("cyberwareplugin"))
							{
								foreach (Cyberware objCyberware in _objCharacter.Cyberware)
								{
									foreach (Cyberware objPlugin in objCyberware.Children)
									{
										if (objPlugin.Name == objXmlCyberware.InnerText)
										{
											intTotal++;
											break;
										}
									}
								}
							}

							// Check for Cyberware Categories.
							foreach (XmlNode objXmlCyberware in objXmlRequired.SelectNodes("cyberwarecategory"))
							{
								foreach (Cyberware objCyberware in _objCharacter.Cyberware)
								{
									if (objCyberware.Category == objXmlCyberware.InnerText)
										intTotal++;
								}
							}

							if (intTotal >= Convert.ToInt32(objXmlRequired["count"].InnerText))
								blnOneOfMet = true;
						}
						else if (objXmlRequired.Name == "streetcredvsnotoriety")
						{
							// Street Cred must be higher than Notoriety.
							if (_objCharacter.StreetCred >= _objCharacter.Notoriety)
								blnOneOfMet = true;
						}
					}

					// Update the flag for requirements met.
					blnRequirementMet = blnRequirementMet && blnOneOfMet;
					strRequirement += strThisRequirement;
				}

				// Loop through the allof requirements.
				objXmlRequiredList = objXmlQuality.SelectNodes("required/allof");
				foreach (XmlNode objXmlAllOf in objXmlRequiredList)
				{
					bool blnAllOfMet = true;
					string strThisRequirement = "\n" + LanguageManager.Instance.GetString("Message_SelectQuality_AllOf");
					XmlNodeList objXmlAllOfList = objXmlAllOf.ChildNodes;
					foreach (XmlNode objXmlRequired in objXmlAllOfList)
					{
						bool blnFound = false;
						if (objXmlRequired.Name == "quality")
						{
							// Run through all of the Qualities the character has and see if the current required item exists.
							// If so, turn on the RequirementMet flag so it can be selected.
							foreach (Quality objCharacterQuality in _objCharacter.Qualities)
							{
								if (objCharacterQuality.Name == objXmlRequired.InnerText)
									blnFound = true;
							}

							if (!blnFound)
							{
								XmlNode objNode = _objXmlDocument.SelectSingleNode("/chummer/qualities/quality[name = \"" + objXmlRequired.InnerText + "\"]");
								if (objNode["translate"] != null)
									strThisRequirement += "\n\t" + objNode["translate"].InnerText;
								else
									strThisRequirement += "\n\t" + objXmlRequired.InnerText;
							}
						}
						else if (objXmlRequired.Name == "metatype")
						{
							// Check the Metatype requirement.
							if (objXmlRequired.InnerText == _objCharacter.Metatype)
								blnFound = true;
							else
							{
								XmlNode objNode = _objMetatypeDocument.SelectSingleNode("/chummer/metatypes/metatype[name = \"" + objXmlRequired.InnerText + "\"]");
								if (objNode == null)
									objNode = _objCritterDocument.SelectSingleNode("/chummer/metatypes/metatype[name = \"" + objXmlRequired.InnerText + "\"]");
								if (objNode["translate"] != null)
									strThisRequirement += "\n\t" + objNode["translate"].InnerText;
								else
									strThisRequirement += "\n\t" + objXmlRequired.InnerText;
							}
						}
						else if (objXmlRequired.Name == "metatypecategory")
						{
							// Check the Metatype Category requirement.
							if (objXmlRequired.InnerText == _objCharacter.MetatypeCategory)
								blnFound = true;
							else
							{
								XmlNode objNode = _objMetatypeDocument.SelectSingleNode("/chummer/categories/category[. = \"" + objXmlRequired.InnerText + "\"]");
								if (objNode == null)
									objNode = _objCritterDocument.SelectSingleNode("/chummer/categories/category[. = \"" + objXmlRequired.InnerText + "\"]");
								if (objNode.Attributes["translate"] != null)
									strThisRequirement += "\n\t" + objNode.Attributes["translate"].InnerText;
								else
									strThisRequirement = "\n\t" + objXmlRequired.InnerText;
							}
						}
						else if (objXmlRequired.Name == "metavariant")
						{
							// Check the Metavariant requirement.
							if (objXmlRequired.InnerText == _objCharacter.Metavariant)
								blnFound = true;
							else
							{
								XmlNode objNode = _objMetatypeDocument.SelectSingleNode("/chummer/metatypes/metatype/metavariants/metavariant[name = \"" + objXmlRequired.InnerText + "\"]");
								if (objNode == null)
									objNode = _objCritterDocument.SelectSingleNode("/chummer/metatypes/metatype/metavariants/metavariant[name = \"" + objXmlRequired.InnerText + "\"]");
								if (objNode["translate"] != null)
									strThisRequirement += "\n\t" + objNode["translate"].InnerText;
								else
									strThisRequirement += "\n\t" + objXmlRequired.InnerText;
							}
						}
						else if (objXmlRequired.Name == "inherited")
						{
							strThisRequirement += "\n\t" + LanguageManager.Instance.GetString("Message_SelectQuality_Inherit");
						}
						else if (objXmlRequired.Name == "careerkarma")
						{
							// Check Career Karma requirement.
							if (_objCharacter.CareerKarma >= Convert.ToInt32(objXmlRequired.InnerText))
								blnFound = true;
							else
								strThisRequirement = "\n\t" + LanguageManager.Instance.GetString("Message_SelectQuality_RequireKarma").Replace("{0}", objXmlRequired.InnerText);
						}
						else if (objXmlRequired.Name == "ess")
						{
							// Check Essence requirement.
							if (objXmlRequired.InnerText.StartsWith("-"))
							{
								// Essence must be less than the value.
								if (_objCharacter.Essence < Convert.ToDecimal(objXmlRequired.InnerText.Replace("-", string.Empty), GlobalOptions.Instance.CultureInfo))
									blnFound = true;
							}
							else
							{
								// Essence must be equal to or greater than the value.
								if (_objCharacter.Essence >= Convert.ToDecimal(objXmlRequired.InnerText, GlobalOptions.Instance.CultureInfo))
									blnFound = true;
							}

						}
						else if (objXmlRequired.Name == "skill")
						{
							// Check if the character has the required Skill.
							foreach (Skill objSkill in _objCharacter.Skills)
							{
								if (objSkill.Name == objXmlRequired["name"].InnerText)
								{
									if (objSkill.Rating >= Convert.ToInt32(objXmlRequired["val"].InnerText))
									{
										blnFound = true;
										break;
									}
								}
							}
						}
						else if (objXmlRequired.Name == "attribute")
						{
							// Check to see if an Attribute meets a requirement.
							Attribute objAttribute = _objCharacter.GetAttribute(objXmlRequired["name"].InnerText);

							if (objXmlRequired["total"] != null)
							{
								// Make sure the Attribute's total value meets the requirement.
								if (objAttribute.TotalValue >= Convert.ToInt32(objXmlRequired["total"].InnerText))
									blnFound = true;
							}
						}
						else if (objXmlRequired.Name == "attributetotal")
						{
							// Check if the character's Attributes add up to a particular total.
							string strAttributes = objXmlRequired["attributes"].InnerText;
							strAttributes = strAttributes.Replace("BOD", _objCharacter.GetAttribute("BOD").Value.ToString());
							strAttributes = strAttributes.Replace("AGI", _objCharacter.GetAttribute("AGI").Value.ToString());
							strAttributes = strAttributes.Replace("REA", _objCharacter.GetAttribute("REA").Value.ToString());
							strAttributes = strAttributes.Replace("STR", _objCharacter.GetAttribute("STR").Value.ToString());
							strAttributes = strAttributes.Replace("CHA", _objCharacter.GetAttribute("CHA").Value.ToString());
							strAttributes = strAttributes.Replace("INT", _objCharacter.GetAttribute("INT").Value.ToString());
							strAttributes = strAttributes.Replace("INT", _objCharacter.GetAttribute("LOG").Value.ToString());
							strAttributes = strAttributes.Replace("WIL", _objCharacter.GetAttribute("WIL").Value.ToString());
							strAttributes = strAttributes.Replace("MAG", _objCharacter.GetAttribute("MAG").Value.ToString());
							strAttributes = strAttributes.Replace("RES", _objCharacter.GetAttribute("RES").Value.ToString());
							strAttributes = strAttributes.Replace("EDG", _objCharacter.GetAttribute("EDG").Value.ToString());

							XmlDocument objXmlDocument = new XmlDocument();
							XPathNavigator nav = objXmlDocument.CreateNavigator();
							XPathExpression xprAttributes = nav.Compile(strAttributes);
							if (Convert.ToInt32(nav.Evaluate(xprAttributes)) >= Convert.ToInt32(objXmlRequired["val"].InnerText))
								blnFound = true;
						}
						else if (objXmlRequired.Name == "skillgrouptotal")
						{
							// Check if the total combined Ratings of Skill Groups adds up to a particular total.
							int intTotal = 0;
							string[] strGroups = objXmlRequired["skillgroups"].InnerText.Split('+');
							for (int i = 0; i <= strGroups.Length - 1; i++)
							{
								foreach (SkillGroup objGroup in _objCharacter.SkillGroups)
								{
									if (objGroup.Name == strGroups[i])
									{
										intTotal += objGroup.Rating;
										break;
									}
								}
							}

							if (intTotal >= Convert.ToInt32(objXmlRequired["val"].InnerText))
								blnFound = true;
						}
						else if (objXmlRequired.Name == "cyberwares")
						{
							// Check to see if the character has a number of the required Cyberware/Bioware items.
							int intTotal = 0;

							// Check Cyberware.
							foreach (XmlNode objXmlCyberware in objXmlRequired.SelectNodes("cyberware"))
							{
								foreach (Cyberware objCyberware in _objCharacter.Cyberware)
								{
									if (objCyberware.Name == objXmlCyberware.InnerText)
									{
										if (objXmlCyberware.Attributes["select"] == null)
										{
											intTotal++;
											break;
										}
										else if (objXmlCyberware.Attributes["select"].InnerText == objCyberware.Location)
										{
											intTotal++;
											break;
										}
									}
								}
							}

							// Check Bioware.
							foreach (XmlNode objXmlBioware in objXmlRequired.SelectNodes("bioware"))
							{
								foreach (Cyberware objCyberware in _objCharacter.Cyberware)
								{
									if (objCyberware.Name == objXmlBioware.InnerText)
									{
										intTotal++;
										break;
									}
								}
							}

							// Check Cyberware name that contain a straing.
							foreach (XmlNode objXmlCyberware in objXmlRequired.SelectNodes("cyberwarecontains"))
							{
								foreach (Cyberware objCyberware in _objCharacter.Cyberware)
								{
									if (objCyberware.Name.Contains(objXmlCyberware.InnerText))
									{
										if (objXmlCyberware.Attributes["select"] == null)
										{
											intTotal++;
											break;
										}
										else if (objXmlCyberware.Attributes["select"].InnerText == objCyberware.Location)
										{
											intTotal++;
											break;
										}
									}
								}
							}

							// Check Bioware name that contain a straing.
							foreach (XmlNode objXmlCyberware in objXmlRequired.SelectNodes("biowarecontains"))
							{
								foreach (Cyberware objCyberware in _objCharacter.Cyberware)
								{
									if (objCyberware.Name.Contains(objXmlCyberware.InnerText))
									{
										if (objXmlCyberware.Attributes["select"] == null)
										{
											intTotal++;
											break;
										}
										else if (objXmlCyberware.Attributes["select"].InnerText == objCyberware.Location)
										{
											intTotal++;
											break;
										}
									}
								}
							}

							// Check for Cyberware Plugins.
							foreach (XmlNode objXmlCyberware in objXmlRequired.SelectNodes("cyberwareplugin"))
							{
								foreach (Cyberware objCyberware in _objCharacter.Cyberware)
								{
									foreach (Cyberware objPlugin in objCyberware.Children)
									{
										if (objPlugin.Name == objXmlCyberware.InnerText)
										{
											intTotal++;
											break;
										}
									}
								}
							}

							// Check for Cyberware Categories.
							foreach (XmlNode objXmlCyberware in objXmlRequired.SelectNodes("cyberwarecategory"))
							{
								foreach (Cyberware objCyberware in _objCharacter.Cyberware)
								{
									if (objCyberware.Category == objXmlCyberware.InnerText)
										intTotal++;
								}
							}

							if (intTotal >= Convert.ToInt32(objXmlRequired["count"].InnerText))
								blnFound = true;
						}
						else if (objXmlRequired.Name == "streetcredvsnotoriety")
						{
							// Street Cred must be higher than Notoriety.
							if (_objCharacter.StreetCred >= _objCharacter.Notoriety)
								blnFound = true;
						}

						// If this item was not found, fail the AllOfMet condition.
						if (!blnFound)
							blnAllOfMet = false;
					}

					// Update the flag for requirements met.
					blnRequirementMet = blnRequirementMet && blnAllOfMet;
					strRequirement += strThisRequirement;
				}

				// The character has not met the requirements, so display a message and uncheck the item.
				if (!blnRequirementMet)
				{
					string strMessage = LanguageManager.Instance.GetString("Message_SelectQuality_QualityRequirement");
					strMessage += strRequirement;

					if (blnShowMessage)
						MessageBox.Show(strMessage, LanguageManager.Instance.GetString("MessageTitle_SelectQuality_QualityRequirement"), MessageBoxButtons.OK, MessageBoxIcon.Information);
					return false;
				}
			}

			return true;
		}

		private void MoveControls()
		{
			int intWidth = Math.Max(lblBPLabel.Width, lblSourceLabel.Width);
			lblBP.Left = lblBPLabel.Left + intWidth + 6;
			lblSource.Left = lblSourceLabel.Left + intWidth + 6;

			lblSearchLabel.Left = txtSearch.Left - 6 - lblSearchLabel.Width;
		}
		#endregion
	}
}