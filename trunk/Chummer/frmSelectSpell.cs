﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Xml;

namespace Chummer
{
    public partial class frmSelectSpell : Form
    {
        private string _strSelectedSpell = "";

		private bool _blnAddAgain = false;
		private string _strLimitCategory = "";
		private string _strForceSpell = "";

		private XmlDocument _objXmlDocument = new XmlDocument();
		private readonly Character _objCharacter;

		#region Control Events
		public frmSelectSpell(Character objCharacter)
        {
            InitializeComponent();
			LanguageManager.Instance.Load(GlobalOptions.Instance.Language, this);
			_objCharacter = objCharacter;

			tipTooltip.SetToolTip(chkLimited, LanguageManager.Instance.GetString("Tip_SelectSpell_LimitedSpell"));

			MoveControls();
        }

        private void frmSelectSpell_Load(object sender, EventArgs e)
        {
			// If a value is forced, set the name of the spell and accept the form.
			if (_strForceSpell != "")
			{
				_strSelectedSpell = _strForceSpell;
				this.DialogResult = DialogResult.OK;
			}

			foreach (Label objLabel in this.Controls.OfType<Label>())
			{
				if (objLabel.Text.StartsWith("["))
					objLabel.Text = "";
			}

        	// Load the Spells information.
			_objXmlDocument = XmlManager.Instance.Load("spells.xml");

			// Populate the Category list.
			XmlNodeList objXmlNodeList = _objXmlDocument.SelectNodes("/chummer/categories/category");
			foreach (XmlNode objXmlCategory in objXmlNodeList)
			{
				if (_strLimitCategory == "" || _strLimitCategory == objXmlCategory.InnerText)
				{
					TreeNode nodCategory = new TreeNode();
					nodCategory.Tag = objXmlCategory.InnerText;
					if (objXmlCategory.Attributes["translate"] != null)
						nodCategory.Text = objXmlCategory.Attributes["translate"].InnerText;
					else
						nodCategory.Text = objXmlCategory.InnerText;

					treSpells.Nodes.Add(nodCategory);
				}
			}

            // Populate the Spell list.
			if (_strLimitCategory != "")
				objXmlNodeList = _objXmlDocument.SelectNodes("/chummer/spells/spell[category = \"" + _strLimitCategory + "\" and (" + _objCharacter.Options.BookXPath() + ")]");
			else
				objXmlNodeList = _objXmlDocument.SelectNodes("/chummer/spells/spell[" + _objCharacter.Options.BookXPath() + "]");

			treSpells.TreeViewNodeSorter = new SortByName();
            foreach (XmlNode objXmlSpell in objXmlNodeList)
            {
                TreeNode nodSpell = new TreeNode();
                TreeNode nodParent = new TreeNode();
				if (objXmlSpell["translate"] != null)
					nodSpell.Text = objXmlSpell["translate"].InnerText;
				else
					nodSpell.Text = objXmlSpell["name"].InnerText;
				nodSpell.Tag = objXmlSpell["name"].InnerText;
                // Check to see if there is already a Category node for the Spell's category.
                foreach (TreeNode nodCategory in treSpells.Nodes)
                {
                    if (nodCategory.Level == 0 && nodCategory.Tag.ToString() == objXmlSpell["category"].InnerText)
                    {
                        nodParent = nodCategory;
                    }
                }

                // Add the Spell to the Category node.
                nodParent.Nodes.Add(nodSpell);

				if (_strLimitCategory != "")
					nodParent.Expand();
            }

			if (_strLimitCategory != "")
				txtSearch.Enabled = false;
        }

        private void treSpells_AfterSelect(object sender, TreeViewEventArgs e)
        {
            // Only attempt to retrieve Spell information if a child node is selected.
            if (treSpells.SelectedNode.Level > 0)
            {
            	// Display the Spell information.
                XmlNode objXmlSpell = _objXmlDocument.SelectSingleNode("/chummer/spells/spell[name = \"" + treSpells.SelectedNode.Tag + "\"]");

				string[] strDescriptorsIn = objXmlSpell["descriptor"].InnerText.Split(',');
				string strDescriptors = "";
				foreach (string strDescriptor in strDescriptorsIn)
				{
					switch (strDescriptor.Trim())
					{
						case "Active":
							strDescriptors += LanguageManager.Instance.GetString("String_DescActive") + ", ";
							break;
						case "Area":
							strDescriptors += LanguageManager.Instance.GetString("String_DescArea") + ", ";
							break;
						case "Direct":
							strDescriptors += LanguageManager.Instance.GetString("String_DescDirect") + ", ";
							break;
						case "Directional":
							strDescriptors += LanguageManager.Instance.GetString("String_DescDirectional") + ", ";
							break;
						case "Elemental":
							strDescriptors += LanguageManager.Instance.GetString("String_DescElemental") + ", ";
							break;
						case "Environmental":
							strDescriptors += LanguageManager.Instance.GetString("String_DescEnvironmental") + ", ";
							break;
						case "Extended Area":
							strDescriptors += LanguageManager.Instance.GetString("String_DescExtendedArea") + ", ";
							break;
						case "Indirect":
							strDescriptors += LanguageManager.Instance.GetString("String_DescIndirect") + ", ";
							break;
						case "Mana":
							strDescriptors += LanguageManager.Instance.GetString("String_DescMana") + ", ";
							break;
						case "Mental":
							strDescriptors += LanguageManager.Instance.GetString("String_DescMental") + ", ";
							break;
						case "Multi-Sense":
							strDescriptors += LanguageManager.Instance.GetString("String_DescMultiSense") + ", ";
							break;
						case "Negative":
							strDescriptors += LanguageManager.Instance.GetString("String_DescNegative") + ", ";
							break;
						case "Obvious":
							strDescriptors += LanguageManager.Instance.GetString("String_DescObvious") + ", ";
							break;
						case "Passive":
							strDescriptors += LanguageManager.Instance.GetString("String_DescPassive") + ", ";
							break;
						case "Physical":
							strDescriptors += LanguageManager.Instance.GetString("String_DescPhysical") + ", ";
							break;
						case "Psychic":
							strDescriptors += LanguageManager.Instance.GetString("String_DescPsychic") + ", ";
							break;
						case "Realistic":
							strDescriptors += LanguageManager.Instance.GetString("String_DescRealistic") + ", ";
							break;
						case "Single-Sense":
							strDescriptors += LanguageManager.Instance.GetString("String_DescSingleSense") + ", ";
							break;
						case "Touch":
							strDescriptors += LanguageManager.Instance.GetString("String_DescTouch") + ", ";
							break;
					}
				}
				// Remove the trailing comma.
				if (strDescriptors != string.Empty)
					strDescriptors = strDescriptors.Substring(0, strDescriptors.Length - 2);
				lblDescriptors.Text = strDescriptors;

				switch (objXmlSpell["type"].InnerText)
				{
					case "M":
						lblType.Text = LanguageManager.Instance.GetString("String_SpellTypeMana");
						break;
					default:
						lblType.Text = LanguageManager.Instance.GetString("String_SpellTypePhysical");
						break;
				}

				switch (objXmlSpell["duration"].InnerText)
				{
					case "P":
						lblDuration.Text = LanguageManager.Instance.GetString("String_SpellDurationPermanent");
						break;
					case "S":
						lblDuration.Text = LanguageManager.Instance.GetString("String_SpellDurationSustained");
						break;
					default:
						lblDuration.Text = LanguageManager.Instance.GetString("String_SpellDurationInstant");
						break;
				}

				string strRange = objXmlSpell["range"].InnerText;
				strRange = strRange.Replace("Self", LanguageManager.Instance.GetString("String_SpellRangeSelf"));
				strRange = strRange.Replace("LOS", LanguageManager.Instance.GetString("String_SpellRangeLineOfSight"));
				strRange = strRange.Replace("LOI", LanguageManager.Instance.GetString("String_SpellRangeLineOfInfluence"));
				strRange = strRange.Replace("T", LanguageManager.Instance.GetString("String_SpellRangeTouch"));
				strRange = strRange.Replace("(A)", "(" + LanguageManager.Instance.GetString("String_SpellRangeArea") + ")");
				strRange = strRange.Replace("MAG", LanguageManager.Instance.GetString("String_AttributeMAGShort"));
				lblRange.Text = strRange;

				switch (objXmlSpell["damage"].InnerText)
				{
					case "P":
						lblDamage.Text = LanguageManager.Instance.GetString("String_DamagePhysical");
						break;
					case "S":
						lblDamage.Text = LanguageManager.Instance.GetString("String_DamageStun");
						break;
					default:
						lblDamage.Text = "";
						break;
				}

				lblDV.Text = objXmlSpell["dv"].InnerText.Replace("/", "÷").Replace("F", LanguageManager.Instance.GetString("String_SpellForce"));
				lblDV.Text = lblDV.Text.Replace("Overflow damage", LanguageManager.Instance.GetString("String_SpellOverflowDamage"));
				lblDV.Text = lblDV.Text.Replace("Damage Value", LanguageManager.Instance.GetString("String_SpellDamageValue"));
				lblDV.Text = lblDV.Text.Replace("Toxin DV", LanguageManager.Instance.GetString("String_SpellToxinDV"));
				lblDV.Text = lblDV.Text.Replace("Disease DV", LanguageManager.Instance.GetString("String_SpellDiseaseDV"));
				lblDV.Text = lblDV.Text.Replace("Radiation Power", LanguageManager.Instance.GetString("String_SpellRadiationPower"));

				string strBook = _objCharacter.Options.LanguageBookShort(objXmlSpell["source"].InnerText);
				string strPage = objXmlSpell["page"].InnerText;
				if (objXmlSpell["altpage"] != null)
					strPage = objXmlSpell["altpage"].InnerText;
				lblSource.Text = strBook + " " + strPage;

				tipTooltip.SetToolTip(lblSource, _objCharacter.Options.LanguageBookLong(objXmlSpell["source"].InnerText) + " " + LanguageManager.Instance.GetString("String_Page") + " " + strPage);
            }
        }

        private void cmdOK_Click(object sender, EventArgs e)
        {
			try
			{
				if (treSpells.SelectedNode.Level > 0)
					AcceptForm();
			}
			catch
			{
			}
        }

        private void treSpells_DoubleClick(object sender, EventArgs e)
        {
			try
			{
				if (treSpells.SelectedNode.Level > 0)
					AcceptForm();
			}
			catch
			{
			}
        }

        private void cmdCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }

		private void txtSearch_TextChanged(object sender, EventArgs e)
		{
			// Treat everything as being uppercase so the search is case-insensitive.
			string strSearch = "/chummer/spells/spell[(" + _objCharacter.Options.BookXPath() + ") and ((contains(translate(name,'abcdefghijklmnopqrstuvwxyzàáâãäåçèéêëìíîïñòóôõöùúûüýß','ABCDEFGHIJKLMNOPQRSTUVWXYZÀÁÂÃÄÅÇÈÉÊËÌÍÎÏÑÒÓÔÕÖÙÚÛÜÝß'), \"" + txtSearch.Text.ToUpper() + "\") and not(translate)) or contains(translate(translate,'abcdefghijklmnopqrstuvwxyzàáâãäåçèéêëìíîïñòóôõöùúûüýß','ABCDEFGHIJKLMNOPQRSTUVWXYZÀÁÂÃÄÅÇÈÉÊËÌÍÎÏÑÒÓÔÕÖÙÚÛÜÝß'), \"" + txtSearch.Text.ToUpper() + "\"))]";

			treSpells.Nodes.Clear();

			// Populate the Category list.
			XmlNodeList objXmlNodeList = _objXmlDocument.SelectNodes("/chummer/categories/category");
			foreach (XmlNode objXmlCategory in objXmlNodeList)
			{
				if (_strLimitCategory == "" || _strLimitCategory == objXmlCategory.InnerText)
				{
					TreeNode nodCategory = new TreeNode();
					nodCategory.Tag = objXmlCategory.InnerText;
					if (objXmlCategory.Attributes["translate"] != null)
						nodCategory.Text = objXmlCategory.Attributes["translate"].InnerText;
					else
						nodCategory.Text = objXmlCategory.InnerText;

					treSpells.Nodes.Add(nodCategory);
				}
			}

			// Populate the Spell list.
			objXmlNodeList = _objXmlDocument.SelectNodes(strSearch);
			treSpells.TreeViewNodeSorter = new SortByName();
			foreach (XmlNode objXmlSpell in objXmlNodeList)
			{
				TreeNode nodSpell = new TreeNode();
				TreeNode nodParent = new TreeNode();
				if (objXmlSpell["translate"] != null)
					nodSpell.Text = objXmlSpell["translate"].InnerText;
				else
					nodSpell.Text = objXmlSpell["name"].InnerText;
				nodSpell.Tag = objXmlSpell["name"].InnerText;
				// Check to see if there is already a Category node for the Spell's category.
				foreach (TreeNode nodCategory in treSpells.Nodes)
				{
					if (nodCategory.Level == 0 && nodCategory.Tag.ToString() == objXmlSpell["category"].InnerText)
					{
						nodParent = nodCategory;
					}
				}

				// Add the Spell to the Category node.
				nodParent.Nodes.Add(nodSpell);
				nodParent.Expand();
			}

			List<TreeNode> lstRemove = new List<TreeNode>();
			foreach (TreeNode nodNode in treSpells.Nodes)
			{
				if (nodNode.Level == 0 && nodNode.Nodes.Count == 0)
					lstRemove.Add(nodNode);
			}

			foreach (TreeNode nodNode in lstRemove)
				treSpells.Nodes.Remove(nodNode);
		}

		private void cmdOKAdd_Click(object sender, EventArgs e)
		{
			_blnAddAgain = true;
			cmdOK_Click(sender, e);
		}

		private void txtSearch_KeyDown(object sender, KeyEventArgs e)
		{
			if (treSpells.SelectedNode == null)
			{
				if (treSpells.Nodes.Count > 0)
					treSpells.SelectedNode = treSpells.Nodes[0];
			}
			if (e.KeyCode == Keys.Down)
			{
				try
				{
					treSpells.SelectedNode = treSpells.SelectedNode.NextVisibleNode;
					if (treSpells.SelectedNode == null)
						treSpells.SelectedNode = treSpells.Nodes[0];
				}
				catch
				{
				}
			}
			if (e.KeyCode == Keys.Up)
			{
				try
				{
					treSpells.SelectedNode = treSpells.SelectedNode.PrevVisibleNode;
					if (treSpells.SelectedNode == null)
						treSpells.SelectedNode = treSpells.Nodes[treSpells.Nodes.Count - 1].LastNode;
				}
				catch
				{
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
		/// Whether or not a Limited version of the Spell was selected.
		/// </summary>
		public bool Limited
		{
			get
			{
				return chkLimited.Checked;
			}
		}

		/// <summary>
		/// Limit the Spell list to a particular Category.
		/// </summary>
		public string LimitCategory
		{
			set
			{
				_strLimitCategory = value;
			}
		}

		/// <summary>
		/// Force a particular Spell to be selected.
		/// </summary>
		public string ForceSpellName
		{
			set
			{
				_strForceSpell = value;
			}
		}

		/// <summary>
        /// Spell that was selected in the dialogue.
        /// </summary>
        public string SelectedSpell
        {
            get
            {
                return _strSelectedSpell;
            }
        }
		#endregion

		#region Methods
		/// <summary>
		/// Accept the selected item and close the form.
		/// </summary>
        private void AcceptForm()
        {
			_strSelectedSpell = treSpells.SelectedNode.Tag.ToString();
            this.DialogResult = DialogResult.OK;
		}

		private void MoveControls()
		{
			int intWidth = Math.Max(lblDescriptorsLabel.Width, lblTypeLabel.Width);
			intWidth = Math.Max(intWidth, lblTypeLabel.Width);
			intWidth = Math.Max(intWidth, lblRangeLabel.Width);
			intWidth = Math.Max(intWidth, lblDamageLabel.Width);
			intWidth = Math.Max(intWidth, lblDurationLabel.Width);
			intWidth = Math.Max(intWidth, lblDVLabel.Width);

			lblDescriptors.Left = lblDescriptorsLabel.Left + intWidth + 6;
			lblType.Left = lblTypeLabel.Left + intWidth + 6;
			lblRange.Left = lblRangeLabel.Left + intWidth + 6;
			lblDamage.Left = lblDamageLabel.Left + intWidth + 6;
			lblDuration.Left = lblDurationLabel.Left + intWidth + 6;
			lblDV.Left = lblDVLabel.Left + intWidth + 6;

			lblSource.Left = lblSourceLabel.Left + lblSourceLabel.Width + 6;

			lblSearchLabel.Left = txtSearch.Left - 6 - lblSearchLabel.Width;
		}
		#endregion
	}
}