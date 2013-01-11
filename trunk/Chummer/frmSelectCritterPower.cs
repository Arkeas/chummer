﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Xml;

namespace Chummer
{
	public partial class frmSelectCritterPower : Form
	{
		private string _strSelectedPower = "";
		private int _intSelectedRating = 0;
		private static string _strSelectCategory = "";
		private double _dblPowerPoints = 0.0;
		private bool _blnAddAgain = false;

		private XmlDocument _objXmlDocument = new XmlDocument();
		private XmlDocument _objXmlCritterDocument = new XmlDocument();
		private readonly Character _objCharacter;

		private List<ListItem> _lstCategory = new List<ListItem>();

		#region Control Events
		public frmSelectCritterPower(Character objCharacter)
		{
			InitializeComponent();
			LanguageManager.Instance.Load(GlobalOptions.Instance.Language, this);
			_objCharacter = objCharacter;
			MoveControls();
		}

		private void frmSelectCritterPower_Load(object sender, EventArgs e)
		{
			foreach (Label objLabel in this.Controls.OfType<Label>())
			{
				if (objLabel.Text.StartsWith("["))
					objLabel.Text = "";
			}

			_objXmlDocument = XmlManager.Instance.Load("critterpowers.xml");

			// Populate the Category list.
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

			if (_objCharacter.IsCritter)
				_objXmlCritterDocument = XmlManager.Instance.Load("critters.xml");
			else
				_objXmlCritterDocument = XmlManager.Instance.Load("metatypes.xml");
			XmlNode objXmlCritter = _objXmlCritterDocument.SelectSingleNode("/chummer/metatypes/metatype[name = \"" + _objCharacter.Metatype + "\"]");

			if (objXmlCritter == null)
			{
				_objXmlCritterDocument = XmlManager.Instance.Load("metatypes.xml");
				objXmlCritter = _objXmlCritterDocument.SelectSingleNode("/chummer/metatypes/metatype[name = \"" + _objCharacter.Metatype + "\"]");
			}

			// Remove Optional Powers if the Critter does not have access to them.
			if (objXmlCritter["optionalpowers"] == null)
			{
				foreach (ListItem objItem in _lstCategory)
				{
					if (objItem.Value == "Allowed Optional Powers")
					{
						_lstCategory.Remove(objItem);
						break;
					}
				}
			}

			// Remove Free Spirit Powers if the critter is not a Free Spirit.
			if (_objCharacter.Metatype != "Free Spirit")
			{
				foreach (ListItem objItem in _lstCategory)
				{
					if (objItem.Value == "Free Spirit")
					{
						_lstCategory.Remove(objItem);
						break;
					}
				}
			}

			// Remove Toxic Critter Powers if the critter is not a Toxic Critter.
			if (_objCharacter.MetatypeCategory != "Toxic Critters")
			{
				foreach (ListItem objItem in _lstCategory)
				{
					if (objItem.Value == "Toxic Critter Powers")
					{
						_lstCategory.Remove(objItem);
						break;
					}
				}
			}

			// Remove Emergent Powers if the critter is not a Sprite or A.I.
			if (!_objCharacter.MetatypeCategory.EndsWith("Sprites") && !_objCharacter.MetatypeCategory.EndsWith("Sprite") && !_objCharacter.MetatypeCategory.EndsWith("A.I.s") & _objCharacter.MetatypeCategory != "Technocritters" && _objCharacter.MetatypeCategory != "Protosapients")
			{
				foreach (ListItem objItem in _lstCategory)
				{
					if (objItem.Value == "Emergent")
					{
						_lstCategory.Remove(objItem);
						break;
					}
				}
			}

			// Remove Echoes Powers if the critter is not a Free Sprite.
			if (!_objCharacter.IsFreeSprite)
			{
				foreach (ListItem objItem in _lstCategory)
				{
					if (objItem.Value == "Echoes")
					{
						_lstCategory.Remove(objItem);
						break;
					}
				}
			}

			// Remove Shapeshifter Powers if the critter is not a Shapeshifter.
			if (_objCharacter.MetatypeCategory != "Shapeshifter")
			{
				foreach (ListItem objItem in _lstCategory)
				{
					if (objItem.Value == "Shapeshifter")
					{
						_lstCategory.Remove(objItem);
						break;
					}
				}
			}

			SortListItem objSort = new SortListItem();
			_lstCategory.Sort(objSort.Compare);
			cboCategory.DataSource = null;
			cboCategory.ValueMember = "Value";
			cboCategory.DisplayMember = "Name";
			cboCategory.DataSource = _lstCategory;

			// Select the first Category in the list.
			if (_strSelectCategory == "")
				cboCategory.SelectedIndex = 0;
			else
			{
				try
				{
					cboCategory.SelectedValue = _strSelectCategory;
				}
				catch
				{
				}
			}

			if (cboCategory.SelectedIndex == -1)
				cboCategory.SelectedIndex = 0;
		}

		private void cmdOK_Click(object sender, EventArgs e)
		{
			AcceptForm();
		}

		private void cmdCancel_Click(object sender, EventArgs e)
		{
			this.DialogResult = DialogResult.Cancel;
		}

		private void trePowers_AfterSelect(object sender, TreeViewEventArgs e)
		{
			lblPowerPoints.Visible = false;
			lblPowerPointsLabel.Visible = false;
			try
			{
				if (trePowers.SelectedNode.Tag.ToString() != "")
				{
					XmlNode objXmlPower = _objXmlDocument.SelectSingleNode("/chummer/powers/power[name = \"" + trePowers.SelectedNode.Tag + "\"]");
					lblCritterPowerCategory.Text = objXmlPower["category"].InnerText;

					switch (objXmlPower["type"].InnerText)
					{
						case "M":
							lblCritterPowerType.Text = LanguageManager.Instance.GetString("String_SpellTypeMana");
							break;
						case "P":
							lblCritterPowerType.Text = LanguageManager.Instance.GetString("String_SpellTypePhysical");
							break;
						default:
							lblCritterPowerType.Text = "";
							break;
					}

					switch (objXmlPower["action"].InnerText)
					{
						case "Auto":
							lblCritterPowerAction.Text = LanguageManager.Instance.GetString("String_ActionAutomatic");
							break;
						case "Free":
							lblCritterPowerAction.Text = LanguageManager.Instance.GetString("String_ActionFree");
							break;
						case "Simple":
							lblCritterPowerAction.Text = LanguageManager.Instance.GetString("String_ActionSimple");
							break;
						case "Complex":
							lblCritterPowerAction.Text = LanguageManager.Instance.GetString("String_ActionComplex");
							break;
						case "Special":
							lblCritterPowerAction.Text = LanguageManager.Instance.GetString("String_SpellDurationSpecial");
							break;
					}

					string strRange = objXmlPower["range"].InnerText;
					strRange = strRange.Replace("Self", LanguageManager.Instance.GetString("String_SpellRangeSelf"));
					strRange = strRange.Replace("Special", LanguageManager.Instance.GetString("String_SpellDurationSpecial"));
					strRange = strRange.Replace("LOS", LanguageManager.Instance.GetString("String_SpellRangeLineOfSight"));
					strRange = strRange.Replace("LOI", LanguageManager.Instance.GetString("String_SpellRangeLineOfInfluence"));
					strRange = strRange.Replace("T", LanguageManager.Instance.GetString("String_SpellRangeTouch"));
					strRange = strRange.Replace("(A)", "(" + LanguageManager.Instance.GetString("String_SpellRangeArea") + ")");
					strRange = strRange.Replace("MAG", LanguageManager.Instance.GetString("String_AttributeMAGShort"));
					lblCritterPowerRange.Text = strRange;

					switch (objXmlPower["duration"].InnerText)
					{
						case "Instant":
							lblCritterPowerDuration.Text = LanguageManager.Instance.GetString("String_SpellDurationInstantLong");
							break;
						case "Sustained":
							lblCritterPowerDuration.Text = LanguageManager.Instance.GetString("String_SpellDurationSustained");
							break;
						case "Always":
							lblCritterPowerDuration.Text = LanguageManager.Instance.GetString("String_SpellDurationAlways");
							break;
						case "Special":
							lblCritterPowerDuration.Text = LanguageManager.Instance.GetString("String_SpellDurationSpecial");
							break;
						default:
							lblCritterPowerDuration.Text = objXmlPower["duration"].InnerText;
							break;
					}

					string strBook = _objCharacter.Options.LanguageBookShort(objXmlPower["source"].InnerText);
					string strPage = objXmlPower["page"].InnerText;
					if (objXmlPower["altpage"] != null)
						strPage = objXmlPower["altpage"].InnerText;
					lblCritterPowerSource.Text = strBook + " " + strPage;

					tipTooltip.SetToolTip(lblCritterPowerSource, _objCharacter.Options.LanguageBookLong(objXmlPower["source"].InnerText) + " " + LanguageManager.Instance.GetString("String_Page") + " " + strPage);

					if (objXmlPower["rating"] != null)
						nudCritterPowerRating.Enabled = true;
					else
						nudCritterPowerRating.Enabled = false;

					// If the character is a Free Spirit, populate the Power Points Cost as well.
					if (_objCharacter.Metatype == "Free Spirit")
					{
						XmlNode objXmlCritter = _objXmlCritterDocument.SelectSingleNode("/chummer/metatypes/metatype[name = \"" + _objCharacter.Metatype + "\"]");
						XmlNode objXmlCritterPower = objXmlCritter.SelectSingleNode("optionalpowers/power[. = \"" + trePowers.SelectedNode.Tag + "\"]");
						lblPowerPoints.Text = objXmlCritterPower.Attributes["cost"].InnerText;
						lblPowerPoints.Visible = true;
						lblPowerPointsLabel.Visible = true;
					}
				}
			}
			catch
			{
			}
		}

		private void cboCategory_SelectedIndexChanged(object sender, EventArgs e)
		{
			XmlNode objXmlCritter = _objXmlCritterDocument.SelectSingleNode("/chummer/metatypes/metatype[name = \"" + _objCharacter.Metatype + "\"]");

			trePowers.Nodes.Clear();

			if (cboCategory.SelectedValue.ToString() == "Toxic Critter Powers")
			{
				// Display the special Toxic Critter Powers.
				foreach (XmlNode objXmlPower in _objXmlDocument.SelectNodes("/chummer/powers/power[toxic = \"yes\"]"))
				{
					TreeNode objNode = new TreeNode();
					objNode.Tag = objXmlPower["name"].InnerText;
					if (objXmlPower["translate"] != null)
						objNode.Text = objXmlPower["translate"].InnerText;
					else
						objNode.Text = objXmlPower["name"].InnerText;
					trePowers.Nodes.Add(objNode);
				}
			}
			else if (cboCategory.SelectedValue.ToString() == "Weakness")
			{
				// Display the special Toxic Critter Powers.
				foreach (XmlNode objXmlPower in _objXmlDocument.SelectNodes("/chummer/powers/power[category = \"Weakness\"]"))
				{
					TreeNode objNode = new TreeNode();
					objNode.Tag = objXmlPower["name"].InnerText;
					if (objXmlPower["translate"] != null)
						objNode.Text = objXmlPower["translate"].InnerText;
					else
						objNode.Text = objXmlPower["name"].InnerText;
					trePowers.Nodes.Add(objNode);
				}
			}
			else
			{
				// If the Critter is only allowed certain Powers, display only those.
				if (objXmlCritter["optionalpowers"] != null)
				{
					foreach (XmlNode objXmlCritterPower in objXmlCritter.SelectNodes("optionalpowers/power"))
					{
						XmlNode objXmlPower = _objXmlDocument.SelectSingleNode("/chummer/powers/power[name = \"" + objXmlCritterPower.InnerText + "\"]");
						TreeNode objNode = new TreeNode();
						objNode.Tag = objXmlPower["name"].InnerText;
						if (objXmlPower["translate"] != null)
							objNode.Text = objXmlPower["translate"].InnerText;
						else
							objNode.Text = objXmlPower["name"].InnerText;
						trePowers.Nodes.Add(objNode);
					}

					// Determine if the Critter has a physical presence Power (Materialization, Possession, or Inhabitation).
					bool blnPhysicalPresence = false;
					foreach (CritterPower objCheckPower in _objCharacter.CritterPowers)
					{
						if (objCheckPower.Name == "Materialization" || objCheckPower.Name == "Possession" || objCheckPower.Name == "Inhabitation")
						{
							blnPhysicalPresence = true;
							break;
						}
					}

					// Add any Critter Powers the Critter comes with that have been manually deleted so they can be re-added.
					foreach (XmlNode objXmlCritterPower in objXmlCritter.SelectNodes("powers/power"))
					{
						bool blnAddPower = true;
						// Make sure the Critter doesn't already have the Power.
						foreach (CritterPower objCheckPower in _objCharacter.CritterPowers)
						{
							if (objCheckPower.Name == objXmlCritterPower.InnerText)
							{
								blnAddPower = false;
								break;
							}
							if ((objCheckPower.Name == "Materialization" || objCheckPower.Name == "Possession" || objCheckPower.Name == "Inhabitation") && blnPhysicalPresence)
							{
								blnAddPower = false;
								break;
							}
						}

						if (blnAddPower)
						{
							XmlNode objXmlPower = _objXmlDocument.SelectSingleNode("/chummer/powers/power[name = \"" + objXmlCritterPower.InnerText + "\"]");
							TreeNode objNode = new TreeNode();
							objNode.Tag = objXmlPower["name"].InnerText;
							if (objXmlPower["translate"] != null)
								objNode.Text = objXmlPower["translate"].InnerText;
							else
								objNode.Text = objXmlPower["name"].InnerText;
							trePowers.Nodes.Add(objNode);

							// If Manifestation is one of the Powers, also include Inhabitation and Possess if they're not already in the list.
							if (!blnPhysicalPresence){
								if (objXmlPower["name"].InnerText == "Materialization")
								{
									bool blnFound = false;
									foreach (TreeNode objCheckNode in trePowers.Nodes)
									{
										if (objCheckNode.Tag.ToString() == "Possession")
										{
											blnFound = true;
											break;
										}
									}
									if (!blnFound)
									{
										XmlNode objXmlPossessionPower = _objXmlDocument.SelectSingleNode("/chummer/powers/power[name = \"Possession\"]");
										TreeNode objPossessionNode = new TreeNode();
										objPossessionNode.Tag = objXmlPossessionPower["name"].InnerText;
										if (objXmlPower["translate"] != null)
											objPossessionNode.Text = objXmlPossessionPower["translate"].InnerText;
										else
											objPossessionNode.Text = objXmlPossessionPower["name"].InnerText;
										trePowers.Nodes.Add(objPossessionNode);
									}

									blnFound = false;
									foreach (TreeNode objCheckNode in trePowers.Nodes)
									{
										if (objCheckNode.Tag.ToString() == "Inhabitation")
										{
											blnFound = true;
											break;
										}
									}
									if (!blnFound)
									{
										XmlNode objXmlPossessionPower = _objXmlDocument.SelectSingleNode("/chummer/powers/power[name = \"Inhabitation\"]");
										TreeNode objPossessionNode = new TreeNode();
										objPossessionNode.Tag = objXmlPossessionPower["name"].InnerText;
										if (objXmlPower["translate"] != null)
											objPossessionNode.Text = objXmlPossessionPower["translate"].InnerText;
										else
											objPossessionNode.Text = objXmlPossessionPower["name"].InnerText;
										trePowers.Nodes.Add(objPossessionNode);
									}
								}
							}
						}
					}
				}
				else
				{
					foreach (XmlNode objXmlPower in _objXmlDocument.SelectNodes("/chummer/powers/power[category = \"" + cboCategory.SelectedValue + "\"]"))
					{
						TreeNode objNode = new TreeNode();
						objNode.Tag = objXmlPower["name"].InnerText;
						if (objXmlPower["translate"] != null)
							objNode.Text = objXmlPower["translate"].InnerText;
						else
							objNode.Text = objXmlPower["name"].InnerText;
						trePowers.Nodes.Add(objNode);
					}
				}
			}
			trePowers.Sort();
		}

		private void trePowers_DoubleClick(object sender, EventArgs e)
		{
			AcceptForm();
		}

		private void cmdOKAdd_Click(object sender, EventArgs e)
		{
			_blnAddAgain = true;
			cmdOK_Click(sender, e);
		}
		#endregion

		#region Methods
		/// <summary>
		/// Accept the selected item and close the form.
		/// </summary>
		private void AcceptForm()
		{
			try
			{
				if (trePowers.SelectedNode == null)
					return;
			}
			catch
			{
				return;
			}

			if (nudCritterPowerRating.Enabled)
				_intSelectedRating = Convert.ToInt32(nudCritterPowerRating.Value);
			_strSelectCategory = cboCategory.SelectedValue.ToString();
			_strSelectedPower = trePowers.SelectedNode.Tag.ToString();

			// If the character is a Free Spirit (PC, not the Critter version), populate the Power Points Cost as well.
			if (_objCharacter.Metatype == "Free Spirit" && !_objCharacter.IsCritter)
			{
				XmlNode objXmlCritter = _objXmlCritterDocument.SelectSingleNode("/chummer/metatypes/metatype[name = \"" + _objCharacter.Metatype + "\"]");
				XmlNode objXmlPower = objXmlCritter.SelectSingleNode("optionalpowers/power[. = \"" + trePowers.SelectedNode.Tag + "\"]");
				_dblPowerPoints = Convert.ToDouble(objXmlPower.Attributes["cost"].InnerText, GlobalOptions.Instance.CultureInfo);
			}

			this.DialogResult = DialogResult.OK;
		}

		private void MoveControls()
		{
			int intWidth = Math.Max(lblCritterPowerCategoryLabel.Width, lblCritterPowerTypeLabel.Width);
			intWidth = Math.Max(intWidth, lblCritterPowerActionLabel.Width);
			intWidth = Math.Max(intWidth, lblCritterPowerRangeLabel.Width);
			intWidth = Math.Max(intWidth, lblCritterPowerDurationLabel.Width);
			intWidth = Math.Max(intWidth, lblCritterPowerRatingLabel.Width);
			intWidth = Math.Max(intWidth, lblCritterPowerSourceLabel.Width);
			intWidth = Math.Max(intWidth, lblPowerPointsLabel.Width);

			lblCritterPowerCategory.Left = lblCritterPowerCategoryLabel.Left + intWidth + 6;
			lblCritterPowerType.Left = lblCritterPowerTypeLabel.Left + intWidth + 6;
			lblCritterPowerAction.Left = lblCritterPowerActionLabel.Left + intWidth + 6;
			lblCritterPowerRange.Left = lblCritterPowerRangeLabel.Left + intWidth + 6;
			lblCritterPowerDuration.Left = lblCritterPowerDurationLabel.Left + intWidth + 6;
			nudCritterPowerRating.Left = lblCritterPowerRatingLabel.Left + intWidth + 6;
			lblCritterPowerSource.Left = lblCritterPowerSourceLabel.Left + intWidth + 6;
			lblPowerPoints.Left = lblPowerPointsLabel.Left + intWidth + 6;
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
		/// Criter Power that was selected in the dialogue.
		/// </summary>
		public string SelectedPower
		{
			get
			{
				return _strSelectedPower;
			}
		}

		/// <summary>
		/// Rating for the Critter Power that was selected in the dialogue.
		/// </summary>
		public int SelectedRating
		{
			get
			{
				return _intSelectedRating;
			}
		}

		/// <summary>
		/// Power Point cost for the Critter Power (only applies to Free Spirits).
		/// </summary>
		public double PowerPoints
		{
			get
			{
				return _dblPowerPoints;
			}
		}
		#endregion
	}
}