using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Xml;

// RatingChanged Event Handler.
public delegate void RatingChangedHandler(Object sender);
public delegate void BreakGroupHandler(Object sender);
// SpecializationChanged Event Handler.
public delegate void SpecializationChangedHandler(Object sender);
public delegate void SpecializationLeaveHandler(Object sender);
// DeleteSkill Event Handler
public delegate void DeleteSkillHandler(Object sender);
public delegate void SkillKaramClickHandler(Object sender);
public delegate void DiceRollerHandler(Object sender);

namespace Chummer
{
    public partial class SkillControl : UserControl
    {
		private Skill _objSkill;

        // RatingChanged Event.
        public event RatingChangedHandler RatingChanged;
        public event SpecializationChangedHandler SpecializationChanged;
		public event SpecializationLeaveHandler SpecializationLeave;
        public event DeleteSkillHandler DeleteSkill;
		public event SkillKaramClickHandler SkillKarmaClicked;
		public event DiceRollerHandler DiceRollerClicked;
		public event BreakGroupHandler BreakGroupClicked;

		private string _strOldSpec = "";
		private bool _blnSkipRefresh = false;

		#region Control Events
		public SkillControl()
        {
            InitializeComponent();
			LanguageManager.Instance.Load(GlobalOptions.Instance.Language, this);
        }

        private void SkillControl_Load(object sender, EventArgs e)
        {
			_blnSkipRefresh = true;
			if (_objSkill.KnowledgeSkill)
			{
				cboSkillName.Text = _objSkill.Name;
			}

			if (_objSkill.CharacterObject.Created)
			{
				string strTooltip = "";
				int intNewRating = _objSkill.Rating + 1;
				int intKarmaCost = 0;

				if (_objSkill.Rating < _objSkill.RatingMaximum)
				{
					if (KnowledgeSkill == false)
					{
						if (_objSkill.Rating == 0)
							intKarmaCost = _objSkill.CharacterObject.Options.KarmaNewActiveSkill;
						else
						{
							if (_objSkill.Rating == 6)
								// The cost for raising an Active Skill from 6 to 7 (thanks to Aptitude) is doubled.
								intKarmaCost = ((_objSkill.Rating + 1) * _objSkill.CharacterObject.Options.KarmaImproveActiveSkill) * 2;
							else
								intKarmaCost = (_objSkill.Rating + 1) * _objSkill.CharacterObject.Options.KarmaImproveActiveSkill;
						}
					}
					else
					{
						if (_objSkill.Rating == 0)
							intKarmaCost = _objSkill.CharacterObject.Options.KarmaNewKnowledgeSkill;
						else
							intKarmaCost = (_objSkill.Rating + 1) * _objSkill.CharacterObject.Options.KarmaImproveKnowledgeSkill;
					}

					// Double the Karma cost if the character is Uneducated and is a Technical Active, Academic, or Professional Skill.
					if (_objSkill.CharacterObject.Uneducated && (SkillCategory == "Technical Active" || SkillCategory == "Academic" || SkillCategory == "Professional"))
						intKarmaCost *= 2;
					strTooltip = LanguageManager.Instance.GetString("Tip_ImproveItem").Replace("{0}", intNewRating.ToString()).Replace("{1}", intKarmaCost.ToString());
					tipTooltip.SetToolTip(cmdImproveSkill, strTooltip);
				}

				ImprovementManager objImprovementManager = new ImprovementManager(_objSkill.CharacterObject);
				if (objImprovementManager.ValueOf(Improvement.ImprovementType.AdeptLinguistics) > 0 && SkillCategory == "Language" && SkillRating == 0)
					strTooltip = LanguageManager.Instance.GetString("Tip_ImproveItem").Replace("{0}", "1").Replace("{1}", "0");
				tipTooltip.SetToolTip(cmdImproveSkill, strTooltip);

				nudSkill.Visible = false;
				lblSkillRating.Visible = true;
				cmdImproveSkill.Visible = true;
				
				// Show the Dice Rolling button if the option is enabled.
				if (_objSkill.CharacterObject.Options.AllowSkillDiceRolling)
				{
					cmdRoll.Visible = true;
					this.Width += 30;
					cboSpec.Left += 30;
					cmdChangeSpec.Left += 30;
					cboKnowledgeSkillCategory.Left += 30;
					cmdDelete.Left += 30;
					tipTooltip.SetToolTip(cmdRoll, LanguageManager.Instance.GetString("Tip_DiceRoller"));
				}
				
				if (!_objSkill.ExoticSkill)
				{
					cmdChangeSpec.Visible = true;
					cboSpec.Enabled = false;
				}

				string strTip = LanguageManager.Instance.GetString("Tip_Skill_ChangeSpecialization").Replace("{0}", _objSkill.CharacterObject.Options.KarmaSpecialization.ToString());
				tipTooltip.SetToolTip(cmdChangeSpec, strTip);
			}
			if (KnowledgeSkill)
				this.Width = cmdDelete.Left + cmdDelete.Width;
			else
				this.Width = cmdChangeSpec.Left + cmdChangeSpec.Width;

			lblAttribute.Text = _objSkill.DisplayAttribute;

			RefreshControl();
			_blnSkipRefresh = false;
        }

        private void nudSkill_ValueChanged(object sender, EventArgs e)
        {
            // Raise the RatingChanged Event when the NumericUpDown's Value changes.
            // The entire SkillControl is passed as an argument so the handling event can evaluate its contents.
			_objSkill.Rating = Convert.ToInt32(nudSkill.Value);
			RefreshControl();
			RatingChanged(this);
        }

        private void cboSpec_TextChanged(object sender, EventArgs e)
        {
            // Raise the SpecializationChanged Event when the DropDownList's Value changes.
            // The entire SkillControl is passed as an argument so the handling event can evaluate its contents.
			_objSkill.Specialization = cboSpec.Text;
			if (_objSkill.Specialization.Trim() == string.Empty)
			{
				cboSpec.Text = "";
				_objSkill.Specialization = "";
			}
			SpecializationChanged(this);
        }

        private void cmdDelete_Click(object sender, EventArgs e)
        {
            // Raise the DeleteSkill Event when the user has confirmed their desire to delete the Skill.
            // The entire SkillControl is passed as an argument so the handling event can evaluate its contents.
			DeleteSkill(this);
        }

		private void cboKnonwledgeSkillCategory_SelectedIndexChanged(object sender, EventArgs e)
		{
			// Raise the RatingChanged Event when the user has changed the Knowledge Skill's Category.
			// The entire SkillControl is passed as an argument so the handling event can evaluate its contents.
			SkillCategory = cboKnowledgeSkillCategory.SelectedValue.ToString();
			RatingChanged(this);
		}

		private void lblSkillName_Click(object sender, EventArgs e)
		{
			nudSkill.Focus();
		}

		private void cboSkillName_SelectedIndexChanged(object sender, EventArgs e)
		{
			_objSkill.Name = cboSkillName.Text;

			XmlDocument objXmlDocument = new XmlDocument();
			objXmlDocument = XmlManager.Instance.Load("skills.xml");

			// When the selected Knowledge Skill is changed, check the Skill file and build the pre-defined list of its Specializations (if any).
			XmlNode objXmlSkill = objXmlDocument.SelectSingleNode("/chummer/knowledgeskills/skill[name = \"" + cboSkillName.Text + "\"]");
			if (objXmlSkill == null)
				objXmlSkill = objXmlDocument.SelectSingleNode("/chummer/knowledgeskills/skill[translate = \"" + cboSkillName.Text + "\"]");
			if (objXmlSkill != null)
			{
				if (!_blnSkipRefresh)
					cboKnowledgeSkillCategory.SelectedValue = objXmlSkill["category"].InnerText;
				cboSpec.Items.Clear();
				foreach (XmlNode objXmlSpecialization in objXmlSkill.SelectNodes("specs/spec"))
				{
					if (objXmlSpecialization.Attributes["translate"] != null)
						cboSpec.Items.Add(objXmlSpecialization.Attributes["translate"].InnerText);
					else
						cboSpec.Items.Add(objXmlSpecialization.InnerText);
				}
			}
		}

		private void cboSkillName_TextChanged(object sender, EventArgs e)
		{
			_objSkill.Name = cboSkillName.Text;
		}

		private void cmdImproveSkill_Click(object sender, EventArgs e)
		{
			// Raise the SkillKarmaClicked Even when the Improve button is clicked.
			// The entire SkillGroupControl is passed as an argument so the handling event can evaluate its contents.
			SkillKarmaClicked(this);
		}

		private void cmdChangeSpec_Click(object sender, EventArgs e)
		{
			_strOldSpec = cboSpec.Text;
			cboSpec.Enabled = true;
			cboSpec.Focus();
		}

		private void cboSpec_Leave(object sender, EventArgs e)
		{
			if (_objSkill.CharacterObject.Created && !_objSkill.ExoticSkill)
			{
				cboSpec.Enabled = false;
				SpecializationLeave(this);
			}
		}

		private void cmdRoll_Click(object sender, EventArgs e)
		{
			DiceRollerClicked(this);
		}

		private void cmdBreakGroup_Click(object sender, EventArgs e)
		{
			BreakGroupClicked(this);
		}
		#endregion

		#region Properties
		/// <summary>
		/// Skill object that this control is linked to.
		/// </summary>
		public Skill SkillObject
		{
			get
			{
				return _objSkill;
			}
			set
			{
				_objSkill = value;
			}
		}

        /// <summary>
        /// Skill name.
        /// </summary>
        public string SkillName
        {
            get
            {
				return _objSkill.Name;
            }
            set
            {
				_objSkill.Name = value;
				lblSkillName.Text = _objSkill.DisplayName;

				if (!KnowledgeSkill)
					tipTooltip.SetToolTip(lblSkillName, _objSkill.DisplayCategory + "\n" + _objSkill.CharacterObject.Options.LanguageBookLong(_objSkill.Source) + " " + LanguageManager.Instance.GetString("String_Page") + " " + _objSkill.Page);
            }
        }

        /// <summary>
        /// Skill Rating.
        /// </summary>
        public int SkillRating
        {
            get
            {
				return _objSkill.Rating;
            }
            set
            {
				if (value > _objSkill.RatingMaximum)
					value = _objSkill.RatingMaximum;
                nudSkill.Value = value;
				lblSkillRating.Text = value.ToString();
				_objSkill.Rating = value;

				if (value < _objSkill.RatingMaximum)
				{
					string strTooltip = "";
					int intNewRating = value + 1;
					int intKarmaCost = 0;

					if (KnowledgeSkill == false)
					{
						if (value == 0)
							intKarmaCost = _objSkill.CharacterObject.Options.KarmaNewActiveSkill;
						else
						{
							if (value == 6)
								// The cost for raising an Active Skill from 6 to 7 (thanks to Aptitude) is doubled.
								intKarmaCost = ((value + 1) * _objSkill.CharacterObject.Options.KarmaImproveActiveSkill) * 2;
							else
								intKarmaCost = (value + 1) * _objSkill.CharacterObject.Options.KarmaImproveActiveSkill;
						}
					}
					else
					{
						if (value == 0)
							intKarmaCost = _objSkill.CharacterObject.Options.KarmaNewKnowledgeSkill;
						else
							intKarmaCost = (value + 1) * _objSkill.CharacterObject.Options.KarmaImproveKnowledgeSkill;
					}
					
					// Double the Karma cost if the character is Uneducated and is a Technica Active, Academic, or Professional Skill.
					if (_objSkill.CharacterObject.Uneducated && (SkillCategory == "Technical Active" || SkillCategory == "Academic" || SkillCategory == "Professional"))
						intKarmaCost *= 2;
					strTooltip = LanguageManager.Instance.GetString("Tip_ImproveItem").Replace("{0}", intNewRating.ToString()).Replace("{1}", intKarmaCost.ToString());
					tipTooltip.SetToolTip(cmdImproveSkill, strTooltip);
					cmdImproveSkill.Enabled = true;
				}
				else
					cmdImproveSkill.Enabled = false;
            }
        }

        /// <summary>
        /// Maximum Skill Rating.
        /// </summary>
        public int SkillRatingMaximum
        {
            get
            {
				return _objSkill.RatingMaximum;
            }
			set
			{
				nudSkill.Maximum = value;
			}
        }

        /// <summary>
        /// Is the Skill a Knowledge Skill?
        /// </summary>
        public bool KnowledgeSkill
        {
            get
            {
				return _objSkill.KnowledgeSkill;
            }
            set
            {
                lblSkillName.Visible = !value;
				lblAttribute.Visible = !value;
				_objSkill.KnowledgeSkill = value;

                if (value)
                {
					_blnSkipRefresh = true;
					// Read the list of Categories from the XML file.
					List<ListItem> lstCategories = new List<ListItem>();
					XmlDocument objXmlDocument = new XmlDocument();
					objXmlDocument = XmlManager.Instance.Load("skills.xml");
					
					XmlNodeList objXmlSkillList = objXmlDocument.SelectNodes("/chummer/categories/category[@type = \"knowledge\"]");
					foreach (XmlNode objXmlCategory in objXmlSkillList)
					{
						ListItem objItem = new ListItem();
						objItem.Value = objXmlCategory.InnerText;
						if (objXmlCategory.Attributes["translate"] != null)
							objItem.Name = objXmlCategory.Attributes["translate"].InnerText;
						else
							objItem.Name = objXmlCategory.InnerText;
						lstCategories.Add(objItem);
					}
					cboKnowledgeSkillCategory.ValueMember = "Value";
					cboKnowledgeSkillCategory.DisplayMember = "Name";
					cboKnowledgeSkillCategory.DataSource = lstCategories;

					cboSkillName.Visible = true;
					cboKnowledgeSkillCategory.Visible = true;
					if (cboKnowledgeSkillCategory.Text == "")
						cboKnowledgeSkillCategory.SelectedIndex = 0;

					// Populate the list of Knowledge Skills.
					objXmlSkillList = objXmlDocument.SelectNodes("/chummer/knowledgeskills/skill");

					cboSkillName.Items.Clear();
					foreach (XmlNode objXmlSkill in objXmlSkillList)
					{
						if (objXmlSkill["translate"] != null)
							cboSkillName.Items.Add(objXmlSkill["translate"].InnerText);
						else
							cboSkillName.Items.Add(objXmlSkill["name"].InnerText);
					}
					_blnSkipRefresh = false;

					if (_objSkill.SkillCategory != "")
						cboKnowledgeSkillCategory.SelectedValue = _objSkill.SkillCategory;
					else
						_objSkill.Attribute = "LOG";
                }
                else
                {
					cboKnowledgeSkillCategory.Visible = false;
                }
            }
        }

        /// <summary>
        /// Specialization.
        /// </summary>
        public string SkillSpec
        {
            get
            {
				return _objSkill.Specialization;
            }
			set
			{
				cboSpec.Text = value;
				_objSkill.Specialization = value;
			}
        }

		/// <summary>
        /// Name of the Skill Group the Skill is currently a part of (blank for no group).
        /// </summary>
        public string SkillGroup
        {
            get
            {
				return _objSkill.SkillGroup;
            }
            set
            {
				_objSkill.SkillGroup = value;
            }
        }

		/// <summary>
		/// Whether or not the Skill is currently rolled into its Skill Group.
		/// </summary>
		public bool IsGrouped
		{
			get
			{
				return _objSkill.IsGrouped;
			}
			set
			{
				_objSkill.IsGrouped = value;

				// When Grouped in Career Mode, everything but the Improve button is disabled.
				if (value)
					lblSkillName.ForeColor = SystemColors.GrayText;
				else
					lblSkillName.ForeColor = SystemColors.ControlText;
				lblSkillRating.Enabled = !value;
				nudSkill.Enabled = !value;
				if (_objSkill.CharacterObject.Created && !_objSkill.ExoticSkill)
					cboSpec.Enabled = false;
				else if (!_objSkill.CharacterObject.Created && !_objSkill.ExoticSkill)
					cboSpec.Enabled = !value;
				else
					cboSpec.Enabled = true;

				// If we're in Create Mode, show the Break Group button if the Skill is Grouped.
				if (!_objSkill.CharacterObject.Created && _objSkill.IsGrouped)
					cmdBreakGroup.Visible = _objSkill.CharacterObject.Options.BreakSkillGroupsInCreateMode;
				else
					cmdBreakGroup.Visible = false;
			}
		}

		/// <summary>
		/// Name of the Skill Category the Skill is currently a part of.
		/// </summary>
		public string SkillCategory
		{
			get
			{
				return _objSkill.SkillCategory;
			}
			set
			{
				if (_blnSkipRefresh)
					return;

				_objSkill.SkillCategory = value;

				if (_objSkill.KnowledgeSkill)
				{
					if (value == "")
					{
						cboKnowledgeSkillCategory.SelectedIndex = 0;
						_objSkill.Attribute = "LOG";
						_objSkill.SkillCategory = "Academic";
					}
					else
						cboKnowledgeSkillCategory.SelectedValue = value;
					RefreshControl();
				}
			}
		}

		/// <summary>
		/// Attribute the Skill is linked to.
		/// </summary>
		public string Attribute
		{
			get
			{
				return _objSkill.Attribute;
			}
		}

		/// <summary>
		/// Whether or not the Delete button should be displayed.
		/// </summary>
        public bool AllowDelete
        {
            get
            {
				return _objSkill.AllowDelete;
            }
            set
            {
				_objSkill.AllowDelete = value;
                if (value)
                {
                    cmdDelete.Visible = true;
                }
                else
                {
                    cmdDelete.Visible = false;
                }
            }
        }

		/// <summary>
		/// Specialization that was selected before editing it.
		/// </summary>
		public string OldSpecialization
		{
			get
			{
				return _strOldSpec;
			}
		}
		#endregion

		#region Methods
		/// <summary>
		/// Add a string to the Specialization list.
		/// </summary>
		/// <param name="strSpec">String to add.</param>
        public void AddSpec(string strSpec)
        {
            cboSpec.Items.Add(strSpec);
        }

		/// <summary>
		/// Update the Modified Rating shown.
		/// </summary>
		public void RefreshControl()
		{
			bool blnSkillsoft = false;
			ImprovementManager objImprovementManager = new ImprovementManager(_objSkill.CharacterObject);

			int intRating = _objSkill.TotalRating;
			lblModifiedRating.Text = intRating.ToString();

			int intSkillRating = _objSkill.Rating;
			foreach (Gear objGear in _objSkill.CharacterObject.Gear)
			{
				// Look for any Skillsoft that would conflict with the Skill's Rating.
				if (objGear.Equipped && objGear.Category == "Skillsofts" && (objGear.Extra == _objSkill.Name || objGear.Extra == _objSkill.Name + ", " + LanguageManager.Instance.GetString("Label_SelectGear_Hacked")))
				{
					if (objGear.Rating > _objSkill.Rating)
					{
						// Use the Skillsoft's Rating or Skillwire Rating, whichever is lower.
						// If this is a Knowsoft or Linguasoft, it is not limited to the Skillwire Rating.
						if (objGear.Name == "Activesoft")
							intSkillRating = Math.Min(objGear.Rating, objImprovementManager.ValueOf(Improvement.ImprovementType.Skillwire));
						else
							intSkillRating = objGear.Rating;
						blnSkillsoft = true;
						break;
					}
				}

				foreach (Gear objChild in objGear.Children)
				{
					if (objChild.Equipped && objChild.Category == "Skillsofts" && (objChild.Extra == _objSkill.Name || objChild.Extra == _objSkill.Name + ", " + LanguageManager.Instance.GetString("Label_SelectGear_Hacked")))
					{
						if (objChild.Rating > _objSkill.Rating)
						{
							// Use the Skillsoft's Rating or Skillwire Rating, whichever is lower.
							// If this is a Knowsoft or Linguasoft, it is not limited to the Skillwire Rating.
							if (objChild.Name == "Activesoft")
								intSkillRating = Math.Min(objChild.Rating, objImprovementManager.ValueOf(Improvement.ImprovementType.Skillwire));
							else
								intSkillRating = objChild.Rating;
							blnSkillsoft = true;
							break;
						}
					}
				}
			}

			foreach (TechProgram objProgram in _objSkill.CharacterObject.TechPrograms)
			{
				// Look for any Skillsoft Complex Forms that would conflict with the Skill's Rating.
				if (objProgram.Category == "Skillsofts" && (objProgram.Extra == _objSkill.Name || objProgram.Extra == _objSkill.Name + ", " + LanguageManager.Instance.GetString("Label_SelectGear_Hacked")))
				{
					if (objProgram.Rating > _objSkill.Rating)
					{
						// Use the Complex Form's Rating or Skillwire Rating, whichever is lower.
						// If this is a Knowsoft or Linguasoft, it is not limited to the Skillwire Rating.
						if (objProgram.Name == "Activesoft")
							intSkillRating = Math.Min(objProgram.Rating, objImprovementManager.ValueOf(Improvement.ImprovementType.Skillwire));
						else
							intSkillRating = objProgram.Rating;
						blnSkillsoft = true;
						break;
					}
				}
			}

			if (cboSpec.Text != "" && !_objSkill.ExoticSkill)
				lblModifiedRating.Text += " (" + (intRating + 2).ToString() + ")";

			lblAttribute.Text = _objSkill.DisplayAttribute;

			// Build the Tooltip.
			string strTooltip = "";
			if (blnSkillsoft)
				strTooltip += LanguageManager.Instance.GetString("Tip_Skill_SkillsoftRating") + " (" + intSkillRating.ToString() + ")";
			else
				strTooltip += LanguageManager.Instance.GetString("Tip_Skill_SkillRating") + " (" + _objSkill.Rating.ToString() + ")";

			if (_objSkill.Default && intSkillRating == 0)
				strTooltip += " - " + LanguageManager.Instance.GetString("Tip_Skill_Defaulting") + " (1)";
			if ((!_objSkill.Default && intSkillRating > 0) || _objSkill.Default)
			{
				if (_objSkill.Attribute == "MAG" && _objSkill.CharacterObject.AdeptEnabled && _objSkill.CharacterObject.MagicianEnabled)
				{
					// The character is a Mystic Adept, so only the MAG invested in Magician counts towards the Skill.
					strTooltip += " + " + LanguageManager.Instance.GetString("String_Attribute" + _objSkill.Attribute + "Short") + " (" + _objSkill.CharacterObject.MAGMagician.ToString() + ")";
				}
				else
				{
					strTooltip += " + " + LanguageManager.Instance.GetString("String_Attribute" + _objSkill.Attribute + "Short") + " (" + _objSkill.AttributeModifiers.ToString() + ")";
				}
			}

			// Modifiers only apply when not Defaulting.
			if (intSkillRating > 0 || _objSkill.CharacterObject.Options.SkillDefaultingIncludesModifiers)
			{
				if (_objSkill.RatingModifiers != 0)
				{
					if (_objSkill.CharacterObject.Options.EnforceMaximumSkillRatingModifier)
					{
						int intModifier = _objSkill.RatingModifiers;
						if (intModifier > Convert.ToInt32(Math.Floor(Convert.ToDouble(intSkillRating, GlobalOptions.Instance.CultureInfo) * 0.5)))
						{
							int intMax = intModifier;
							intModifier = Convert.ToInt32(Math.Floor(Convert.ToDouble(intSkillRating, GlobalOptions.Instance.CultureInfo) * 0.5));
							if (intModifier != 0)
								strTooltip += " + " + LanguageManager.Instance.GetString("Tip_Skill_RatingModifiers") + " (" + intModifier.ToString() + " " + LanguageManager.Instance.GetString("String_Of") + " " + intMax.ToString() + ")";
							else
								strTooltip += " + " + LanguageManager.Instance.GetString("Tip_Skill_RatingModifiers") + " (0 " + LanguageManager.Instance.GetString("String_Of") + " " + intMax.ToString() + ")";
						}
						else
							strTooltip += " + " + LanguageManager.Instance.GetString("Tip_Skill_RatingModifiers") + " (" + _objSkill.RatingModifiers.ToString() + ")";
					}
					else
						strTooltip += " + " + LanguageManager.Instance.GetString("Tip_Skill_RatingModifiers") + " (" + _objSkill.RatingModifiers.ToString() + ")";
				}
				// Dice Pool Modifiers.
				//if (_objSkill.DicePoolModifiers != 0)
				//	strTooltip += " + " + LanguageManager.Instance.GetString("Tip_Skill_DicePoolModifiers") + " (" + _objSkill.DicePoolModifiers.ToString() + ")";
				strTooltip += _objSkill.DicePoolModifiersTooltip;
			}

			if (_objSkill.SkillCategory == "Language" && _objSkill.KnowledgeSkill && intSkillRating == 0)
			{
				lblModifiedRating.Text = "N";
				strTooltip = LanguageManager.Instance.GetString("Tip_Skill_NativeLanguage");
			}
			
			// Condition Monitor Modifiers.
			if (objImprovementManager.ValueOf(Improvement.ImprovementType.ConditionMonitor) < 0)
				strTooltip += " + " + LanguageManager.Instance.GetString("Tip_Skill_Wounds") + " (" + objImprovementManager.ValueOf(Improvement.ImprovementType.ConditionMonitor).ToString() + ")";

			tipTooltip.SetToolTip(lblModifiedRating, strTooltip);

			if (_objSkill.Rating > 0 && !_objSkill.KnowledgeSkill)
			{
				this.BackColor = SystemColors.ButtonHighlight;
				//lblSkillName.Font = new Font(lblSkillName.Font, FontStyle.Bold);
				lblModifiedRating.Font = new Font(lblModifiedRating.Font, FontStyle.Bold);
			}
			else
			{
				this.BackColor = SystemColors.Control;
				//lblSkillName.Font = new Font(lblSkillName.Font, FontStyle.Regular);
				lblModifiedRating.Font = new Font(lblModifiedRating.Font, FontStyle.Regular);
			}

			// Specializations should not be enabled for Active Skills in Create Mode if their Rating is 0.
			if (!_objSkill.KnowledgeSkill && !_objSkill.ExoticSkill && !_objSkill.CharacterObject.Created)
			{
				if (_objSkill.Rating > 0 && !_objSkill.IsGrouped)
					cboSpec.Enabled = true;
				else
				{
					cboSpec.Enabled = false;
					cboSpec.Text = "";
				}
			}
			if (!_objSkill.KnowledgeSkill && !_objSkill.ExoticSkill && _objSkill.CharacterObject.Created)
			{
				if (_objSkill.Rating > 0)
					cmdChangeSpec.Enabled = true;
				else
					cmdChangeSpec.Enabled = false;
			}

			if (_objSkill.CharacterObject.Created)
			{
				lblSkillRating.Text = _objSkill.Rating.ToString();
				// Enable or disable the Improve Skill button as necessary.
				if (_objSkill.Rating < _objSkill.RatingMaximum)
					cmdImproveSkill.Enabled = true;
				else
					cmdImproveSkill.Enabled = false;
			}
		}
		#endregion
    }
}