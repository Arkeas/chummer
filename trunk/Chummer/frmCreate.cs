﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using System.Xml.XPath;

namespace Chummer
{
	public partial class frmCreate : Form
	{
		// Set the default culture to en-US so we work with decimals correctly.
		private Character _objCharacter;
		private MainController _objController;

		private CharacterOptions _objOptions;
		private CommonFunctions _objFunctions;
		private bool _blnSkipRefresh = false;
		private bool _blnSkipUpdate = false;
		private bool _blnLoading = false;
		private bool _blnIsDirty = false;
		private bool _blnSkipToolStripRevert = false;
		private bool _blnReapplyImprovements = false;
		private bool _blnFreestyle = false;
		private int _intDragLevel = 0;
		private MouseButtons _objDragButton = new MouseButtons();
		private bool _blnDraggingGear = false;
		
		// Create the XmlManager that will handle finding all of the XML files.
		private ImprovementManager _objImprovementManager;

		#region Form Events
		public frmCreate(Character objCharacter)
		{
			_objCharacter = objCharacter;
			_objOptions = _objCharacter.Options;
			_objFunctions = new CommonFunctions(_objCharacter);
			_objImprovementManager = new ImprovementManager(_objCharacter);
			_objController = new MainController(_objCharacter);
			InitializeComponent();

			// Add EventHandlers for the MAG and RES enabled events and tab enabled events.
			_objCharacter.MAGEnabledChanged += objCharacter_MAGEnabledChanged;
			_objCharacter.RESEnabledChanged += objCharacter_RESEnabledChanged;
			_objCharacter.AdeptTabEnabledChanged += objCharacter_AdeptTabEnabledChanged;
			_objCharacter.MagicianTabEnabledChanged += objCharacter_MagicianTabEnabledChanged;
			_objCharacter.TechnomancerTabEnabledChanged += objCharacter_TechnomancerTabEnabledChanged;
			_objCharacter.InitiationTabEnabledChanged += objCharacter_InitiationTabEnabledChanged;
			_objCharacter.CritterTabEnabledChanged += objCharacter_CritterTabEnabledChanged;
			_objCharacter.BlackMarketEnabledChanged += objCharacter_BlackMarketChanged;
			_objCharacter.UneducatedChanged += objCharacter_UneducatedChanged;
			_objCharacter.UncouthChanged += objCharacter_UncouthChanged;
			_objCharacter.InfirmChanged += objCharacter_InfirmChanged;
			GlobalOptions.Instance.MRUChanged += PopulateMRU;

			LanguageManager.Instance.Load(GlobalOptions.Instance.Language, this);

			// Update the text in the Menus so they can be merged with frmMain properly.
			foreach (ToolStripMenuItem objItem in mnuCreateMenu.Items.OfType<ToolStripMenuItem>())
			{
				if (objItem.Tag != null)
				{
					objItem.Text = LanguageManager.Instance.GetString(objItem.Tag.ToString());
				}
			}

			SetTooltips();
			MoveControls();
		}

		/// <summary>
		/// Set the form to Loading mode so that certain events do not fire while data is being populated.
		/// </summary>
		public bool Loading
		{
			set
			{
				_blnLoading = value;
			}
		}

		private void TreeView_MouseDown(object sender, MouseEventArgs e)
		{
			// Generic event for all TreeViews to allow right-clicking to select a TreeNode so the proper ContextMenu is shown.
			//if (e.Button == System.Windows.Forms.MouseButtons.Right)
			//{
				TreeView objTree = (TreeView)sender;
				objTree.SelectedNode = objTree.HitTest(e.X, e.Y).Node;
			//}
		}

		private void frmCreate_Load(object sender, EventArgs e)
		{
			_blnLoading = true;
			if (!_objCharacter.IsCritter && (_objCharacter.BuildMethod == CharacterBuildMethod.BP && _objCharacter.BuildPoints == 0) || (_objCharacter.BuildMethod == CharacterBuildMethod.Karma && _objCharacter.BuildKarma == 0))
			{
				_blnFreestyle = true;
				tssBPRemain.Visible = false;
				tssBPRemainLabel.Visible = false;
			}

			// Set the Statusbar Labels if we're using Karma to build.
			if (_objCharacter.BuildMethod == CharacterBuildMethod.Karma)
			{
				tssBPLabel.Text = LanguageManager.Instance.GetString("Label_Karma");
				tssBPRemainLabel.Text = LanguageManager.Instance.GetString("Label_KarmaRemaining");
				tabBPSummary.Text = LanguageManager.Instance.GetString("Tab_BPSummary_Karma");
				lblQualityBPLabel.Text = LanguageManager.Instance.GetString("Label_Karma");
			}

			// Remove the Magician, Adept, and Technomancer tabs since they are not in use until the appropriate Quality is selected.
			if (!_objCharacter.MagicianEnabled)
				tabCharacterTabs.TabPages.Remove(tabMagician);
			if (!_objCharacter.AdeptEnabled)
				tabCharacterTabs.TabPages.Remove(tabAdept);
			if (!_objCharacter.TechnomancerEnabled)
				tabCharacterTabs.TabPages.Remove(tabTechnomancer);
			if (!_objCharacter.CritterEnabled)
				tabCharacterTabs.TabPages.Remove(tabCritter);

			// Set the visibility of the Bioware Suites menu options.
			mnuSpecialAddBiowareSuite.Visible = _objCharacter.Options.AllowBiowareSuites;
			mnuSpecialCreateBiowareSuite.Visible = _objCharacter.Options.AllowBiowareSuites;

			if (_objCharacter.BlackMarket)
			{
				chkCyberwareBlackMarketDiscount.Visible = true;
				chkArmorBlackMarketDiscount.Visible = true;
				chkWeaponBlackMarketDiscount.Visible = true;
				chkGearBlackMarketDiscount.Visible = true;
				chkVehicleBlackMarketDiscount.Visible = true;
			}

			// Remove the Improvements Tab.
			tabCharacterTabs.TabPages.Remove(tabImprovements);

			if (_objCharacter.BuildMethod == CharacterBuildMethod.Karma)
			{
				if (!_objCharacter.MAGEnabled && !_objCharacter.RESEnabled)
					tabCharacterTabs.TabPages.Remove(tabInitiation);
				else
				{
					if (_objCharacter.MAGEnabled)
					{
						tabInitiation.Text = LanguageManager.Instance.GetString("Tab_Initiation");
						lblInitiateGradeLabel.Text = LanguageManager.Instance.GetString("Label_InitiationGrade");
						cmdAddMetamagic.Text = LanguageManager.Instance.GetString("Button_AddMetamagic");
						chkInitiationGroup.Text = LanguageManager.Instance.GetString("Checkbox_GroupInitiation");
						chkInitiationOrdeal.Text = LanguageManager.Instance.GetString("Checkbox_InitiationOrdeal");
						chkJoinGroup.Text = LanguageManager.Instance.GetString("Checkbox_JoinedGroup");
						chkJoinGroup.Checked = _objCharacter.GroupMember;
						txtGroupName.Text = _objCharacter.GroupName;
						txtGroupNotes.Text = _objCharacter.GroupNotes;
					}
					else
					{
						tabInitiation.Text = LanguageManager.Instance.GetString("Tab_Submersion");
						lblInitiateGradeLabel.Text = LanguageManager.Instance.GetString("Label_SubmersionGrade");
						cmdAddMetamagic.Text = LanguageManager.Instance.GetString("Button_AddEcho");
						chkInitiationGroup.Text = LanguageManager.Instance.GetString("Checkbox_NetworkSubmersion");
						chkInitiationOrdeal.Text = LanguageManager.Instance.GetString("Checkbox_SubmersionTask");
						chkJoinGroup.Text = LanguageManager.Instance.GetString("Checkbox_JoinedNetwork");
						chkJoinGroup.Checked = _objCharacter.GroupMember;
						txtGroupName.Text = _objCharacter.GroupName;
						txtGroupNotes.Text = _objCharacter.GroupNotes;
					}
				}
			}
			else
			{
				if (!_objCharacter.InitiationEnabled)
					tabCharacterTabs.TabPages.Remove(tabInitiation);
			}

			// If the character has a mugshot, decode it and put it in the PictureBox.
			if (_objCharacter.Mugshot != "")
			{
				byte[] bytImage = Convert.FromBase64String(_objCharacter.Mugshot);
				MemoryStream objStream = new MemoryStream(bytImage, 0, bytImage.Length);
				objStream.Write(bytImage, 0, bytImage.Length);
				Image imgMugshot = Image.FromStream(objStream, true);
				picMugshot.Image = imgMugshot;
			}

			// Populate character information fields.
			XmlDocument objMetatypeDoc = new XmlDocument();
			XmlNode objMetatypeNode;
			string strMetatype = "";
			string strBook = "";
			string strPage = "";

			objMetatypeDoc = XmlManager.Instance.Load("metatypes.xml");
			{
				objMetatypeNode = objMetatypeDoc.SelectSingleNode("/chummer/metatypes/metatype[name = \"" + _objCharacter.Metatype + "\"]");
				if (objMetatypeNode == null)
					objMetatypeDoc = XmlManager.Instance.Load("critters.xml");
				objMetatypeNode = objMetatypeDoc.SelectSingleNode("/chummer/metatypes/metatype[name = \"" + _objCharacter.Metatype + "\"]");

				if (objMetatypeNode["translate"] != null)
					strMetatype = objMetatypeNode["translate"].InnerText;
				else
					strMetatype = _objCharacter.Metatype;

				strBook = _objOptions.LanguageBookShort(objMetatypeNode["source"].InnerText);
				if (objMetatypeNode["altpage"] != null)
					strPage = objMetatypeNode["altpage"].InnerText;
				else
					strPage = objMetatypeNode["page"].InnerText;

				if (_objCharacter.Metavariant != "")
				{
					objMetatypeNode = objMetatypeNode.SelectSingleNode("metavariants/metavariant[name = \"" + _objCharacter.Metavariant + "\"]");

					if (objMetatypeNode["translate"] != null)
						strMetatype += " (" + objMetatypeNode["translate"].InnerText + ")";
					else
						strMetatype += " (" + _objCharacter.Metavariant + ")";

					strBook = _objOptions.LanguageBookShort(objMetatypeNode["source"].InnerText);
					if (objMetatypeNode["altpage"] != null)
						strPage = objMetatypeNode["altpage"].InnerText;
					else
						strPage = objMetatypeNode["page"].InnerText;
				}
			}
			lblMetatype.Text = strMetatype;
			lblMetatypeSource.Text = strBook + " " + strPage;
			txtCharacterName.Text = _objCharacter.Name;
			txtSex.Text = _objCharacter.Sex;
			txtAge.Text = _objCharacter.Age;
			txtEyes.Text = _objCharacter.Eyes;
			txtHeight.Text = _objCharacter.Height;
			txtWeight.Text = _objCharacter.Weight;
			txtSkin.Text = _objCharacter.Skin;
			txtHair.Text = _objCharacter.Hair;
			txtDescription.Text = _objCharacter.Description;
			txtBackground.Text = _objCharacter.Background;
			txtConcept.Text = _objCharacter.Concept;
			txtNotes.Text = _objCharacter.Notes;
			txtAlias.Text = _objCharacter.Alias;
			txtPlayerName.Text = _objCharacter.PlayerName;

			// Check for Special Attributes.
			lblMAGLabel.Enabled = _objCharacter.MAGEnabled;
			lblMAGAug.Enabled = _objCharacter.MAGEnabled;
			nudMAG.Enabled = _objCharacter.MAGEnabled;
			lblMAGMetatype.Enabled = _objCharacter.MAGEnabled;
			lblFoci.Visible = _objCharacter.MAGEnabled;
			treFoci.Visible = _objCharacter.MAGEnabled;
			cmdCreateStackedFocus.Visible = _objCharacter.MAGEnabled;

			lblRESLabel.Enabled = _objCharacter.RESEnabled;
			lblRESAug.Enabled = _objCharacter.RESEnabled;
			nudRES.Enabled = _objCharacter.RESEnabled;
			lblRESMetatype.Enabled = _objCharacter.RESEnabled;

			// Define the XML objects that will be used.
			XmlDocument objXmlDocument = new XmlDocument();

			// Populate the Qualities list.
			foreach (Quality objQuality in _objCharacter.Qualities)
			{
				TreeNode objNode = new TreeNode();
				objNode.Text = objQuality.DisplayName;
				objNode.Tag = objQuality.InternalId;
				objNode.ContextMenuStrip = cmsQuality;

				if (objQuality.Notes != string.Empty)
					objNode.ForeColor = Color.SaddleBrown;
				else
				{
					if (objQuality.OriginSource == QualitySource.Metatype || objQuality.OriginSource == QualitySource.MetatypeRemovable)
						objNode.ForeColor = SystemColors.GrayText;
				}
				objNode.ToolTipText = objQuality.Notes;

				if (objQuality.Type == QualityType.Positive)
				{
					treQualities.Nodes[0].Nodes.Add(objNode);
					treQualities.Nodes[0].Expand();
				}
				else
				{
					treQualities.Nodes[1].Nodes.Add(objNode);
					treQualities.Nodes[1].Expand();
				}
			}

			// Populate the Magician Traditions list.
			objXmlDocument = XmlManager.Instance.Load("traditions.xml");
			List<ListItem> lstTraditions = new List<ListItem>();
			ListItem objBlank = new ListItem();
			objBlank.Value = "";
			objBlank.Name = "";
			lstTraditions.Add(objBlank);
			foreach (XmlNode objXmlTradition in objXmlDocument.SelectNodes("/chummer/traditions/tradition[" + _objOptions.BookXPath() + "]"))
			{
				ListItem objItem = new ListItem();
				objItem.Value = objXmlTradition["name"].InnerText;
				if (objXmlTradition["translate"] != null)
					objItem.Name = objXmlTradition["translate"].InnerText;
				else
					objItem.Name = objXmlTradition["name"].InnerText;
				lstTraditions.Add(objItem);
			}
			SortListItem objSort = new SortListItem();
			lstTraditions.Sort(objSort.Compare);
			cboTradition.ValueMember = "Value";
			cboTradition.DisplayMember = "Name";
			cboTradition.DataSource = lstTraditions;

			// Populate the Technomancer Streams list.
			objXmlDocument = XmlManager.Instance.Load("streams.xml");
			List<ListItem> lstStreams = new List<ListItem>();
			lstStreams.Add(objBlank);
			foreach (XmlNode objXmlTradition in objXmlDocument.SelectNodes("/chummer/traditions/tradition[" + _objOptions.BookXPath() + "]"))
			{
				ListItem objItem = new ListItem();
				objItem.Value = objXmlTradition["name"].InnerText;
				if (objXmlTradition["translate"] != null)
					objItem.Name = objXmlTradition["translate"].InnerText;
				else
					objItem.Name = objXmlTradition["name"].InnerText;
				lstStreams.Add(objItem);
			}
			lstStreams.Sort(objSort.Compare);
			cboStream.ValueMember = "Value";
			cboStream.DisplayMember = "Name";
			cboStream.DataSource = lstStreams;

			// Load the Metatype information before going anywhere else. Doing this later causes the Attributes to get messed up because of calls
			// to UpdateCharacterInformation();
			MetatypeSelected();

			// If the character is a Mystic Adept, set the values for the Mystic Adept NUD.
			if (_objCharacter.AdeptEnabled && _objCharacter.MagicianEnabled)
			{
				nudMysticAdeptMAGMagician.Maximum = _objCharacter.MAG.TotalValue;
				nudMysticAdeptMAGMagician.Value = _objCharacter.MAGMagician;
				lblMysticAdeptMAGAdept.Text = _objCharacter.MAGAdept.ToString();

				lblMysticAdeptAssignment.Visible = true;
				lblMysticAdeptAssignmentAdept.Visible = true;
				lblMysticAdeptAssignmentMagician.Visible = true;
				lblMysticAdeptMAGAdept.Visible = true;
				nudMysticAdeptMAGMagician.Visible = true;
			}

			// Nuyen can be affected by Qualities, so adjust the total amount available to the character.
			if (!_objCharacter.IgnoreRules)
				nudNuyen.Maximum = _objCharacter.NuyenMaximumBP;
			else
				nudNuyen.Maximum = 100000;

			// Nuyen.
			nudNuyen.Value = _objCharacter.NuyenBP;

			// Load the Skills information.
			objXmlDocument = XmlManager.Instance.Load("skills.xml");

			// Populate the Skills Controls.
			XmlNodeList objXmlNodeList = objXmlDocument.SelectNodes("/chummer/skills/skill");
			// Counter to keep track of the number of Controls that have been added to the Panel so we can determine their vertical positioning.
			int i = -1;
			foreach (Skill objSkill in _objCharacter.Skills)
			{
				if (!objSkill.KnowledgeSkill && !objSkill.ExoticSkill)
				{
					i++;
					SkillControl objSkillControl = new SkillControl();
					objSkillControl.SkillObject = objSkill;

					// Attach an EventHandler for the RatingChanged and SpecializationChanged Events.
					objSkillControl.RatingChanged += objActiveSkill_RatingChanged;
					objSkillControl.SpecializationChanged += objSkill_SpecializationChanged;
					objSkillControl.BreakGroupClicked += objSkill_BreakGroupClicked;

					objSkillControl.SkillName = objSkill.Name;
					objSkillControl.SkillCategory = objSkill.SkillCategory;
					objSkillControl.SkillGroup = objSkill.SkillGroup;
					objSkillControl.SkillRatingMaximum = objSkill.RatingMaximum;
					objSkillControl.SkillRating = objSkill.Rating;
					objSkillControl.SkillSpec = objSkill.Specialization;

					XmlNode objXmlSkill = objXmlDocument.SelectSingleNode("/chummer/skills/skill[name = \"" + objSkill.Name + "\"]");
					// Populate the Skill's Specializations (if any).
					foreach (XmlNode objXmlSpecialization in objXmlSkill.SelectNodes("specs/spec"))
					{
						if (objXmlSpecialization.Attributes["translate"] != null)
							objSkillControl.AddSpec(objXmlSpecialization.Attributes["translate"].InnerText);
						else
							objSkillControl.AddSpec(objXmlSpecialization.InnerText);
					}

					// Set the control's vertical position and add it to the Skills Panel.
					objSkillControl.Top = i * objSkillControl.Height;
					objSkillControl.Width = 510;
					objSkillControl.AutoScroll = false;
					panActiveSkills.Controls.Add(objSkillControl);
				}
			}

			// Exotic Skills.
			foreach (Skill objSkill in _objCharacter.Skills)
			{
				if (objSkill.ExoticSkill)
				{
					i++;
					SkillControl objSkillControl = new SkillControl();
					objSkillControl.SkillObject = objSkill;

					// Attach an EventHandler for the RatingChanged and SpecializationChanged Events.
					objSkillControl.RatingChanged += objActiveSkill_RatingChanged;
					objSkillControl.SpecializationChanged += objSkill_SpecializationChanged;
					objSkillControl.BreakGroupClicked += objSkill_BreakGroupClicked;

					objSkillControl.SkillName = objSkill.Name;
					objSkillControl.SkillCategory = objSkill.SkillCategory;
					objSkillControl.SkillGroup = objSkill.SkillGroup;
					objSkillControl.SkillRatingMaximum = objSkill.RatingMaximum;
					objSkillControl.SkillRating = objSkill.Rating;
					objSkillControl.SkillSpec = objSkill.Specialization;

					XmlNode objXmlSkill = objXmlDocument.SelectSingleNode("/chummer/skills/skill[name = \"" + objSkill.Name + "\"]");
					// Populate the Skill's Specializations (if any).
					foreach (XmlNode objXmlSpecialization in objXmlSkill.SelectNodes("specs/spec"))
					{
						if (objXmlSpecialization.Attributes["translate"] != null)
							objSkillControl.AddSpec(objXmlSpecialization.Attributes["translate"].InnerText);
						else
							objSkillControl.AddSpec(objXmlSpecialization.InnerText);
					}

					// Look through the Weapons file and grab the names of items that are part of the appropriate Exotic Category or use the matching Exoctic Skill.
					XmlDocument objXmlWeaponDocument = XmlManager.Instance.Load("weapons.xml");
					XmlNodeList objXmlWeaponList = objXmlWeaponDocument.SelectNodes("/chummer/weapons/weapon[category = \"" + objSkill.Name + "s\" or useskill = \"" + objSkill.Name + "s\"]");
					foreach (XmlNode objXmlWeapon in objXmlWeaponList)
					{
						if (objXmlWeapon["translate"] != null)
							objSkillControl.AddSpec(objXmlWeapon["translate"].InnerText);
						else
							objSkillControl.AddSpec(objXmlWeapon["name"].InnerText);
					}

					// Set the control's vertical position and add it to the Skills Panel.
					objSkillControl.Top = i * objSkillControl.Height;
					objSkillControl.Width = 510;
					objSkillControl.AutoScroll = false;
					panActiveSkills.Controls.Add(objSkillControl);
				}
			}

			// Populate the Skill Groups list.
			i = -1;
			foreach (SkillGroup objGroup in _objCharacter.SkillGroups)
			{
				i++;
				SkillGroupControl objGroupControl = new SkillGroupControl(_objCharacter.Options);
				objGroupControl.SkillGroupObject = objGroup;

				// Attach an EventHandler for the GetRatingChanged Event.
				objGroupControl.GroupRatingChanged += objGroup_RatingChanged;

				// Populate the control, set its vertical position and add it to the Skill Groups Panel. A Skill Group cannot start with a Rating higher than 4.
				objGroupControl.GroupName = objGroup.Name;
				if (objGroup.Rating > objGroup.RatingMaximum)
					objGroup.RatingMaximum = objGroup.Rating;
				objGroupControl.GroupRatingMaximum = objGroup.RatingMaximum;
				objGroupControl.GroupRating = objGroup.Rating;
				objGroupControl.Top = i * objGroupControl.Height;
				objGroupControl.Width = 250;

				// Loop through all of the Active Skills the character has and set their maximums if needed.
				if (objGroup.RatingMaximum > 6)
				{
					foreach (SkillControl objSkill in panActiveSkills.Controls)
					{
						if (objSkill.IsGrouped && objSkill.SkillGroup == objGroup.Name)
						{
							objSkill.SkillRatingMaximum = objGroup.RatingMaximum;
							objSkill.SkillObject.RatingMaximum = objGroup.RatingMaximum;
							objSkill.SkillRating = objGroup.Rating;
						}
					}
				}

				if (_objCharacter.Uneducated)
				{
					objGroupControl.IsEnabled = !objGroup.HasTechnicalSkills;
				}

				if (_objCharacter.Uncouth)
				{
					objGroupControl.IsEnabled = !objGroup.HasSocialSkills;
				}

				if (_objCharacter.Infirm)
				{
					objGroupControl.IsEnabled = !objGroup.HasPhysicalSkills;
				}

				panSkillGroups.Controls.Add(objGroupControl);
			}

			// Populate Knowledge Skills.
			i = -1;
			foreach (Skill objSkill in _objCharacter.Skills)
			{
				if (objSkill.KnowledgeSkill)
				{
					i++;
					SkillControl objSkillControl = new SkillControl();
					objSkillControl.SkillObject = objSkill;

					// Attach an EventHandler for the RatingChanged and SpecializationChanged Events.
					objSkillControl.RatingChanged += objKnowledgeSkill_RatingChanged;
					objSkillControl.SpecializationChanged += objSkill_SpecializationChanged;
					objSkillControl.DeleteSkill += objKnowledgeSkill_DeleteSkill;
					objSkillControl.BreakGroupClicked += objSkill_BreakGroupClicked;

					objSkillControl.KnowledgeSkill = true;
					objSkillControl.SkillCategory = objSkill.SkillCategory;
					objSkillControl.AllowDelete = true;
					objSkillControl.SkillRatingMaximum = objSkill.RatingMaximum;
					objSkillControl.SkillRating = objSkill.Rating;
					objSkillControl.SkillName = objSkill.Name;
					objSkillControl.SkillSpec = objSkill.Specialization;
					objSkillControl.Top = i * objSkillControl.Height;
					objSkillControl.AutoScroll = false;
					panKnowledgeSkills.Controls.Add(objSkillControl);
				}
			}

			// Populate Contacts and Enemies.
			int intContact = -1;
			int intEnemy = -1;
			foreach (Contact objContact in _objCharacter.Contacts)
			{
				if (objContact.EntityType == ContactType.Contact)
				{
					intContact++;
					ContactControl objContactControl = new ContactControl();
					// Attach an EventHandler for the ConnectionRatingChanged, LoyaltyRatingChanged, DeleteContact, and FileNameChanged Events.
					objContactControl.ConnectionRatingChanged += objContact_ConnectionRatingChanged;
					objContactControl.ConnectionGroupRatingChanged += objContact_ConnectionGroupRatingChanged;
					objContactControl.LoyaltyRatingChanged += objContact_LoyaltyRatingChanged;
					objContactControl.DeleteContact += objContact_DeleteContact;
					objContactControl.FileNameChanged += objContact_FileNameChanged;
					
					objContactControl.ContactObject = objContact;
					objContactControl.ContactName = objContact.Name;
					objContactControl.ConnectionRating = objContact.Connection;
					objContactControl.LoyaltyRating = objContact.Loyalty;
					objContactControl.EntityType = objContact.EntityType;
					objContactControl.BackColor = objContact.Colour;

					objContactControl.Top = intContact * objContactControl.Height;
					panContacts.Controls.Add(objContactControl);
				}
				if (objContact.EntityType == ContactType.Enemy)
				{
					intEnemy++;
					ContactControl objContactControl = new ContactControl();
					// Attach an EventHandler for the ConnectioNRatingChanged, LoyaltyRatingChanged, DeleteContact, and FileNameChanged Events.
					objContactControl.ConnectionRatingChanged += objEnemy_ConnectionRatingChanged;
					objContactControl.ConnectionGroupRatingChanged += objEnemy_ConnectionGroupRatingChanged;
					objContactControl.LoyaltyRatingChanged += objEnemy_LoyaltyRatingChanged;
					objContactControl.DeleteContact += objEnemy_DeleteContact;
					objContactControl.FileNameChanged += objEnemy_FileNameChanged;

					objContactControl.ContactObject = objContact;
					objContactControl.ContactName = objContact.Name;
					objContactControl.ConnectionRating = objContact.Connection;
					objContactControl.LoyaltyRating = objContact.Loyalty;
					objContactControl.EntityType = objContact.EntityType;
					objContactControl.BackColor = objContact.Colour;

					objContactControl.Top = intEnemy * objContactControl.Height;
					panEnemies.Controls.Add(objContactControl);
				}
				if (objContact.EntityType == ContactType.Pet)
				{
					PetControl objContactControl = new PetControl();
					// Attach an EventHandler for the DeleteContact and FileNameChanged Events.
					objContactControl.DeleteContact += objPet_DeleteContact;
					objContactControl.FileNameChanged += objPet_FileNameChanged;

					objContactControl.ContactObject = objContact;
					objContactControl.ContactName = objContact.Name;
					objContactControl.BackColor = objContact.Colour;

					panPets.Controls.Add(objContactControl);
				}
			}

			// Populate Armor.
			// Start by populating Locations.
			foreach (string strLocation in _objCharacter.ArmorBundles)
			{
				TreeNode objLocation = new TreeNode();
				objLocation.Tag = strLocation;
				objLocation.Text = strLocation;
				objLocation.ContextMenuStrip = cmsArmorLocation;
				treArmor.Nodes.Add(objLocation);
			}
			foreach (Armor objArmor in _objCharacter.Armor)
			{
				_objFunctions.CreateArmorTreeNode(objArmor, treArmor, cmsArmor, cmsArmorMod, cmsArmorGear);
			}

			// Populate Weapons.
			// Start by populating Locations.
			foreach (string strLocation in _objCharacter.WeaponLocations)
			{
				TreeNode objLocation = new TreeNode();
				objLocation.Tag = strLocation;
				objLocation.Text = strLocation;
				objLocation.ContextMenuStrip = cmsWeaponLocation;
				treWeapons.Nodes.Add(objLocation);
			}
			foreach (Weapon objWeapon in _objCharacter.Weapons)
			{
				_objFunctions.CreateWeaponTreeNode(objWeapon, treWeapons.Nodes[0], cmsWeapon, cmsWeaponMod, cmsWeaponAccessory, cmsWeaponAccessoryGear);
			}

			PopulateCyberwareList();

			// Populate Spell list.
			foreach (Spell objSpell in _objCharacter.Spells)
			{
				TreeNode objNode = new TreeNode();
				objNode.Text = objSpell.DisplayName;
				objNode.Tag = objSpell.InternalId;
				objNode.ContextMenuStrip = cmsSpell;
				if (objSpell.Notes != string.Empty)
					objNode.ForeColor = Color.SaddleBrown;
				objNode.ToolTipText = objSpell.Notes;

				switch (objSpell.Category)
				{
					case "Combat":
						treSpells.Nodes[0].Nodes.Add(objNode);
						treSpells.Nodes[0].Expand();
						break;
					case "Detection":
						treSpells.Nodes[1].Nodes.Add(objNode);
						treSpells.Nodes[1].Expand();
						break;
					case "Health":
						treSpells.Nodes[2].Nodes.Add(objNode);
						treSpells.Nodes[2].Expand();
						break;
					case "Illusion":
						treSpells.Nodes[3].Nodes.Add(objNode);
						treSpells.Nodes[3].Expand();
						break;
					case "Manipulation":
						treSpells.Nodes[4].Nodes.Add(objNode);
						treSpells.Nodes[4].Expand();
						break;
					case "Geomancy Ritual":
						treSpells.Nodes[5].Nodes.Add(objNode);
						treSpells.Nodes[5].Expand();
						break;
				}
			}

			// Populate Adept Powers.
			i = -1;
			foreach (Power objPower in _objCharacter.Powers)
			{
				i++;
				PowerControl objPowerControl = new PowerControl();
				objPowerControl.PowerObject = objPower;

				// Attach an EventHandler for the PowerRatingChanged Event.
				objPowerControl.PowerRatingChanged += objPower_PowerRatingChanged;
				objPowerControl.DeletePower += objPower_DeletePower;

				objPowerControl.PowerName = objPower.Name;
				objPowerControl.Extra = objPower.Extra;
				objPowerControl.PointsPerLevel = objPower.PointsPerLevel;
				objPowerControl.LevelEnabled = objPower.LevelsEnabled;
				if (objPower.MaxLevels > 0)
					objPowerControl.MaxLevels = objPower.MaxLevels;
				objPowerControl.RefreshMaximum(_objCharacter.MAG.TotalValue);
				if (objPower.Rating < 1)
					objPower.Rating = 1;
				objPowerControl.PowerLevel = Convert.ToInt32(objPower.Rating);
				if (objPower.DiscountedAdeptWay)
					objPowerControl.DiscountedByAdeptWay = true;
				if (objPower.DiscountedGeas)
					objPowerControl.DiscountedByGeas = true;

				objPowerControl.Top = i * objPowerControl.Height;
				panPowers.Controls.Add(objPowerControl);
			}

			// Populate Magician Spirits.
			i = -1;
			foreach (Spirit objSpirit in _objCharacter.Spirits)
			{
				if (objSpirit.EntityType == SpiritType.Spirit)
				{
					i++;
					SpiritControl objSpiritControl = new SpiritControl();
					objSpiritControl.SpiritObject = objSpirit;

					// Attach an EventHandler for the ServicesOwedChanged Event.
					objSpiritControl.ServicesOwedChanged += objSpirit_ServicesOwedChanged;
					objSpiritControl.ForceChanged += objSpirit_ForceChanged;
					objSpiritControl.BoundChanged += objSpirit_BoundChanged;
					objSpiritControl.DeleteSpirit += objSpirit_DeleteSpirit;
					objSpiritControl.FileNameChanged += objSpirit_FileNameChanged;

					objSpiritControl.SpiritName = objSpirit.Name;
					objSpiritControl.ServicesOwed = objSpirit.ServicesOwed;
					if (_objCharacter.AdeptEnabled && _objCharacter.MagicianEnabled)
					{
						if (_objOptions.SpiritForceBasedOnTotalMAG)
							objSpiritControl.ForceMaximum = _objCharacter.MAG.TotalValue;
						else
							objSpiritControl.ForceMaximum = _objCharacter.MAGMagician;
					}
					else
						objSpiritControl.ForceMaximum = _objCharacter.MAG.TotalValue;
					objSpiritControl.CritterName = objSpirit.CritterName;
					objSpiritControl.Force = objSpirit.Force;
					objSpiritControl.Bound = objSpirit.Bound;
					objSpiritControl.RebuildSpiritList(_objCharacter.MagicTradition);

					objSpiritControl.Top = i * objSpiritControl.Height;
					panSpirits.Controls.Add(objSpiritControl);
				}
			}

			// Populate Technomancer Sprites.
			i = -1;
			foreach (Spirit objSpirit in _objCharacter.Spirits)
			{
				if (objSpirit.EntityType == SpiritType.Sprite)
				{
					i++;
					SpiritControl objSpiritControl = new SpiritControl();
					objSpiritControl.SpiritObject = objSpirit;
					objSpiritControl.EntityType = SpiritType.Sprite;

					// Attach an EventHandler for the ServicesOwedChanged Event.
					objSpiritControl.ServicesOwedChanged += objSprite_ServicesOwedChanged;
					objSpiritControl.ForceChanged += objSprite_ForceChanged;
					objSpiritControl.BoundChanged += objSprite_BoundChanged;
					objSpiritControl.DeleteSpirit += objSprite_DeleteSpirit;
					objSpiritControl.FileNameChanged += objSprite_FileNameChanged;

					objSpiritControl.SpiritName = objSpirit.Name;
					objSpiritControl.ServicesOwed = objSpirit.ServicesOwed;
					objSpiritControl.ForceMaximum = _objCharacter.RES.TotalValue;
					objSpiritControl.CritterName = objSpiritControl.CritterName;
					objSpiritControl.Force = objSpirit.Force;
					objSpiritControl.Bound = objSpirit.Bound;
					objSpiritControl.RebuildSpiritList(_objCharacter.TechnomancerStream);

					objSpiritControl.Top = i * objSpiritControl.Height;
					panSprites.Controls.Add(objSpiritControl);
				}
			}

			// Populate Technomancer Complex Forms/Programs.
			foreach (TechProgram objProgram in _objCharacter.TechPrograms)
			{
				TreeNode objNode = new TreeNode();
				objNode.Text = objProgram.DisplayName;
				if (objProgram.Extra != "")
					objNode.Text += " (" + objProgram.Extra + ")";
				objNode.Tag = objProgram.InternalId;
				if (Convert.ToInt32(objProgram.CalculatedCapacity) > 0)
					objNode.ContextMenuStrip = cmsComplexForm;
				if (objProgram.Notes != string.Empty)
					objNode.ForeColor = Color.SaddleBrown;
				objNode.ToolTipText = objProgram.Notes;

				foreach (TechProgramOption objOption in objProgram.Options)
				{
					TreeNode objChild = new TreeNode();
					objChild.Text = objOption.DisplayName;
					objChild.ContextMenuStrip = cmsComplexFormPlugin;
					if (objOption.Extra != "")
						objChild.Text += " (" + objProgram.Extra + ")";
					objChild.Tag = objOption.InternalId;
					if (objOption.Notes != string.Empty)
						objChild.ForeColor = Color.SaddleBrown;
					objChild.ToolTipText = objOption.Notes;
					objNode.Nodes.Add(objChild);
					objNode.Expand();
				}

				switch (objProgram.Category)
				{
					case "Advanced":
						treComplexForms.Nodes[0].Nodes.Add(objNode);
						treComplexForms.Nodes[0].Expand();
						break;
					case "ARE Programs":
						treComplexForms.Nodes[1].Nodes.Add(objNode);
						treComplexForms.Nodes[1].Expand();
						break;
					case "Autosoft":
						treComplexForms.Nodes[2].Nodes.Add(objNode);
						treComplexForms.Nodes[2].Expand();
						break;
					case "Common Use":
						treComplexForms.Nodes[3].Nodes.Add(objNode);
						treComplexForms.Nodes[3].Expand();
						break;
					case "Hacking":
						treComplexForms.Nodes[4].Nodes.Add(objNode);
						treComplexForms.Nodes[4].Expand();
						break;
					case "Malware":
						treComplexForms.Nodes[5].Nodes.Add(objNode);
						treComplexForms.Nodes[5].Expand();
						break;
					case "Sensor Software":
						treComplexForms.Nodes[6].Nodes.Add(objNode);
						treComplexForms.Nodes[6].Expand();
						break;
					case "Skillsofts":
						treComplexForms.Nodes[7].Nodes.Add(objNode);
						treComplexForms.Nodes[7].Expand();
						break;
					case "Tactical AR Software":
						treComplexForms.Nodes[8].Nodes.Add(objNode);
						treComplexForms.Nodes[8].Expand();
						break;
				}
			}

			// Populate Martial Arts.
			foreach (MartialArt objMartialArt in _objCharacter.MartialArts)
			{
				TreeNode objMartialArtNode = new TreeNode();
				objMartialArtNode.Text = objMartialArt.DisplayName;
				objMartialArtNode.Tag = objMartialArt.Name;
				objMartialArtNode.ContextMenuStrip = cmsMartialArts;
				if (objMartialArt.Notes != string.Empty)
					objMartialArtNode.ForeColor = Color.SaddleBrown;
				objMartialArtNode.ToolTipText = objMartialArt.Notes;

				foreach (MartialArtAdvantage objAdvantage in objMartialArt.Advantages)
				{
					TreeNode objAdvantageNode = new TreeNode();
					objAdvantageNode.Text = objAdvantage.DisplayName;
					objAdvantageNode.Tag = objAdvantage.InternalId;
					objMartialArtNode.Nodes.Add(objAdvantageNode);
					objMartialArtNode.Expand();
				}

				treMartialArts.Nodes[0].Nodes.Add(objMartialArtNode);
				treMartialArts.Nodes[0].Expand();
			}

			// Populate Martial Art Maneuvers.
			foreach (MartialArtManeuver objManeuver in _objCharacter.MartialArtManeuvers)
			{
				TreeNode objManeuverNode = new TreeNode();
				objManeuverNode.Text = objManeuver.DisplayName;
				objManeuverNode.Tag = objManeuver.InternalId;
				objManeuverNode.ContextMenuStrip = cmsMartialArtManeuver;
				if (objManeuver.Notes != string.Empty)
					objManeuverNode.ForeColor = Color.SaddleBrown;
				objManeuverNode.ToolTipText = objManeuver.Notes;

				treMartialArts.Nodes[1].Nodes.Add(objManeuverNode);
				treMartialArts.Nodes[1].Expand();
			}

			// Populate Lifestyles.
			foreach (Lifestyle objLifestyle in _objCharacter.Lifestyles)
			{
				TreeNode objLifestyleNode = new TreeNode();
				objLifestyleNode.Text = objLifestyle.DisplayName;
				objLifestyleNode.Tag = objLifestyle.InternalId;
				if (objLifestyle.Comforts != "")
					objLifestyleNode.ContextMenuStrip = cmsAdvancedLifestyle;
				else
					objLifestyleNode.ContextMenuStrip = cmsLifestyleNotes;
				if (objLifestyle.Notes != string.Empty)
					objLifestyleNode.ForeColor = Color.SaddleBrown;
				objLifestyleNode.ToolTipText = objLifestyle.Notes;
				treLifestyles.Nodes[0].Nodes.Add(objLifestyleNode);
			}
			treLifestyles.Nodes[0].Expand();

			PopulateGearList();

			// Populate Foci.
			_objController.PopulateFocusList(treFoci);

			// Populate Vehicles.
			foreach (Vehicle objVehicle in _objCharacter.Vehicles)
			{
				_objFunctions.CreateVehicleTreeNode(objVehicle, treVehicles, cmsVehicle, cmsVehicleLocation, cmsVehicleWeapon, cmsVehicleWeaponMod, cmsVehicleWeaponAccessory, cmsVehicleWeaponAccessoryGear, cmsVehicleGear);
			}

			// Populate Initiation/Submersion information.
			if (_objCharacter.InitiateGrade > 0 || _objCharacter.SubmersionGrade > 0)
			{
				foreach (Metamagic objMetamagic in _objCharacter.Metamagics)
				{
					TreeNode objNode = new TreeNode();
					objNode.Text = objMetamagic.DisplayName;
					objNode.Tag = objMetamagic.InternalId;
					objNode.ContextMenuStrip = cmsMetamagic;
					if (objMetamagic.Notes != string.Empty)
						objNode.ForeColor = Color.SaddleBrown;
					objNode.ToolTipText = objMetamagic.Notes;
					treMetamagic.Nodes.Add(objNode);
				}

				if (_objCharacter.InitiateGrade > 0)
					lblInitiateGrade.Text = _objCharacter.InitiateGrade.ToString();
				else
					lblInitiateGrade.Text = _objCharacter.SubmersionGrade.ToString();
			}

			// Populate Critter Powers.
			foreach (CritterPower objPower in _objCharacter.CritterPowers)
			{
				TreeNode objNode = new TreeNode();
				objNode.Text = objPower.DisplayName;
				objNode.Tag = objPower.InternalId;
				objNode.ContextMenuStrip = cmsCritterPowers;
				if (objPower.Notes != string.Empty)
					objNode.ForeColor = Color.SaddleBrown;
				objNode.ToolTipText = objPower.Notes;

				if (objPower.Category != "Weakness")
				{
					treCritterPowers.Nodes[0].Nodes.Add(objNode);
					treCritterPowers.Nodes[0].Expand();
				}
				else
				{
					treCritterPowers.Nodes[1].Nodes.Add(objNode);
					treCritterPowers.Nodes[1].Expand();
				}
			}

			// Load the Cyberware information.
			objXmlDocument = XmlManager.Instance.Load("cyberware.xml");

			// Populate the Grade list.
			List<ListItem> lstCyberwareGrades = new List<ListItem>();
			foreach (Grade objGrade in GlobalOptions.CyberwareGrades)
			{
				ListItem objItem = new ListItem();
				objItem.Value = objGrade.Name;
				objItem.Name = objGrade.DisplayName;
				lstCyberwareGrades.Add(objItem);
			}
			cboCyberwareGrade.ValueMember = "Value";
			cboCyberwareGrade.DisplayMember = "Name";
			cboCyberwareGrade.DataSource = lstCyberwareGrades;

			_blnLoading = false;

			// Select the Magician's Tradition.
			if (_objCharacter.MagicTradition != "")
				cboTradition.SelectedValue = _objCharacter.MagicTradition;

			// Select the Technomancer's Stream.
			if (_objCharacter.TechnomancerStream != "")
				cboStream.SelectedValue = _objCharacter.TechnomancerStream;

			// Clear the Dirty flag which gets set when creating a new Character.
			CalculateBP();
			_blnIsDirty = false;
			UpdateWindowTitle();
			if (_objCharacter.AdeptEnabled)
				CalculatePowerPoints();

			treGear.ItemDrag += treGear_ItemDrag;
			treGear.DragEnter += treGear_DragEnter;
			treGear.DragDrop += treGear_DragDrop;

			treLifestyles.ItemDrag += treLifestyles_ItemDrag;
			treLifestyles.DragEnter += treLifestyles_DragEnter;
			treLifestyles.DragDrop += treLifestyles_DragDrop;

			treArmor.ItemDrag += treArmor_ItemDrag;
			treArmor.DragEnter += treArmor_DragEnter;
			treArmor.DragDrop += treArmor_DragDrop;

			treWeapons.ItemDrag += treWeapons_ItemDrag;
			treWeapons.DragEnter += treWeapons_DragEnter;
			treWeapons.DragDrop += treWeapons_DragDrop;

			treVehicles.ItemDrag += treVehicles_ItemDrag;
			treVehicles.DragEnter += treVehicles_DragEnter;
			treVehicles.DragDrop += treVehicles_DragDrop;

			// Merge the ToolStrips.
			ToolStripManager.RevertMerge("toolStrip");
			ToolStripManager.Merge(toolStrip, "toolStrip");

			// If this is a Sprite, re-label the Mental Attribute Labels.
			if (_objCharacter.Metatype.EndsWith("Sprite"))
			{
				lblBODLabel.Enabled = false;
				nudBOD.Enabled = false;
				lblAGILabel.Enabled = false;
				nudAGI.Enabled = false;
				lblREALabel.Enabled = false;
				nudREA.Enabled = false;
				lblSTRLabel.Enabled = false;
				nudSTR.Enabled = false;
				lblCHALabel.Text = LanguageManager.Instance.GetString("String_AttributePilot");
				lblINTLabel.Text = LanguageManager.Instance.GetString("String_AttributeResponse");
				lblLOGLabel.Text = LanguageManager.Instance.GetString("String_AttributeFirewall");
				lblWILLabel.Enabled = false;
				nudWIL.Enabled = false;
			}
			else if (_objCharacter.Metatype.EndsWith("A.I.") || _objCharacter.MetatypeCategory == "Technocritters" || _objCharacter.MetatypeCategory == "Protosapients")
			{
				lblRatingLabel.Visible = true;
				lblRating.Visible = true;
				lblSystemLabel.Visible = true;
				lblSystem.Visible = true;
				lblFirewallLabel.Visible = true;
				lblFirewall.Visible = true;
				lblResponseLabel.Visible = true;
				nudResponse.Visible = true;
				nudResponse.Enabled = true;
				nudResponse.Value = _objCharacter.Response;
				lblSignalLabel.Visible = true;
				nudSignal.Visible = true;
				nudSignal.Enabled = true;
				nudSignal.Value = _objCharacter.Signal;

				// Disable the Physical Attribute controls.
				lblBODLabel.Enabled = false;
				lblAGILabel.Enabled = false;
				lblREALabel.Enabled = false;
				lblSTRLabel.Enabled = false;
				nudBOD.Enabled = false;
				nudAGI.Enabled = false;
				nudREA.Enabled = false;
				nudSTR.Enabled = false;
			}

			mnuSpecialConvertToFreeSprite.Visible = _objCharacter.IsSprite;

			// Run through all of the Skills and Enable/Disable them as needed.
			foreach (SkillControl objSkillControl in panActiveSkills.Controls)
			{
				if (objSkillControl.Attribute == "MAG")
					objSkillControl.Enabled = _objCharacter.MAGEnabled;
				if (objSkillControl.Attribute == "RES")
					objSkillControl.Enabled = _objCharacter.RESEnabled;
			}
			// Run through all of the Skill Groups and Disable them if all of their Skills are currently inaccessible.
			foreach (SkillGroupControl objSkillGroupControl in panSkillGroups.Controls)
			{
				bool blnEnabled = false;
				foreach (Skill objSkill in _objCharacter.Skills)
				{
					if (objSkill.SkillGroup == objSkillGroupControl.GroupName)
					{
						if (objSkill.Attribute == "MAG" || objSkill.Attribute == "RES")
						{
							if (objSkill.Attribute == "MAG" && _objCharacter.MAGEnabled)
								blnEnabled = true;
							if (objSkill.Attribute == "RES" && _objCharacter.RESEnabled)
								blnEnabled = true;
						}
						else
							blnEnabled = true;
					}
				}
				objSkillGroupControl.IsEnabled = blnEnabled;
				if (!blnEnabled)
					objSkillGroupControl.GroupRating = 0;
			}

			// Populate the Skill Filter DropDown.
			List<ListItem> lstFilter = new List<ListItem>();
			ListItem itmAll = new ListItem();
			itmAll.Value = "0";
			itmAll.Name = LanguageManager.Instance.GetString("String_SkillFilterAll");
			ListItem itmRatingAboveZero = new ListItem();
			itmRatingAboveZero.Value = "1";
			itmRatingAboveZero.Name = LanguageManager.Instance.GetString("String_SkillFilterRatingAboveZero");
			ListItem itmTotalRatingAboveZero = new ListItem();
			itmTotalRatingAboveZero.Value = "2";
			itmTotalRatingAboveZero.Name = LanguageManager.Instance.GetString("String_SkillFilterTotalRatingAboveZero");
			ListItem itmRatingEqualZero = new ListItem();
			itmRatingEqualZero.Value = "3";
			itmRatingEqualZero.Name = LanguageManager.Instance.GetString("String_SkillFilterRatingZero");
			lstFilter.Add(itmAll);
			lstFilter.Add(itmRatingAboveZero);
			lstFilter.Add(itmTotalRatingAboveZero);
			lstFilter.Add(itmRatingEqualZero);

			objXmlDocument = XmlManager.Instance.Load("skills.xml");
			objXmlNodeList = objXmlDocument.SelectNodes("/chummer/categories/category[@type = \"active\"]");
			foreach (XmlNode objNode in objXmlNodeList)
			{
				ListItem objItem = new ListItem();
				objItem.Value = objNode.InnerText;
				if (objNode.Attributes["translate"] != null)
					objItem.Name = LanguageManager.Instance.GetString("Label_Category") + " " + objNode.Attributes["translate"].InnerText;
				else
					objItem.Name = LanguageManager.Instance.GetString("Label_Category") + " " + objNode.InnerText;
				lstFilter.Add(objItem);
			}

			// Add items for Attributes.
			ListItem itmBOD = new ListItem();
			itmBOD.Value = "BOD";
			itmBOD.Name = LanguageManager.Instance.GetString("String_ExpenseAttribute") + ": " + LanguageManager.Instance.GetString("String_AttributeBODShort");
			ListItem itmAGI = new ListItem();
			itmAGI.Value = "AGI";
			itmAGI.Name = LanguageManager.Instance.GetString("String_ExpenseAttribute") + ": " + LanguageManager.Instance.GetString("String_AttributeAGIShort");
			ListItem itmREA = new ListItem();
			itmREA.Value = "REA";
			itmREA.Name = LanguageManager.Instance.GetString("String_ExpenseAttribute") + ": " + LanguageManager.Instance.GetString("String_AttributeREAShort");
			ListItem itmSTR = new ListItem();
			itmSTR.Value = "STR";
			itmSTR.Name = LanguageManager.Instance.GetString("String_ExpenseAttribute") + ": " + LanguageManager.Instance.GetString("String_AttributeSTRShort");
			ListItem itmCHA = new ListItem();
			itmCHA.Value = "CHA";
			itmCHA.Name = LanguageManager.Instance.GetString("String_ExpenseAttribute") + ": " + LanguageManager.Instance.GetString("String_AttributeCHAShort");
			ListItem itmINT = new ListItem();
			itmINT.Value = "INT";
			itmINT.Name = LanguageManager.Instance.GetString("String_ExpenseAttribute") + ": " + LanguageManager.Instance.GetString("String_AttributeINTShort");
			ListItem itmLOG = new ListItem();
			itmLOG.Value = "LOG";
			itmLOG.Name = LanguageManager.Instance.GetString("String_ExpenseAttribute") + ": " + LanguageManager.Instance.GetString("String_AttributeLOGShort");
			ListItem itmWIL = new ListItem();
			itmWIL.Value = "WIL";
			itmWIL.Name = LanguageManager.Instance.GetString("String_ExpenseAttribute") + ": " + LanguageManager.Instance.GetString("String_AttributeWILShort");
			ListItem itmMAG = new ListItem();
			itmMAG.Value = "MAG";
			itmMAG.Name = LanguageManager.Instance.GetString("String_ExpenseAttribute") + ": " + LanguageManager.Instance.GetString("String_AttributeMAGShort");
			ListItem itmRES = new ListItem();
			itmRES.Value = "RES";
			itmRES.Name = LanguageManager.Instance.GetString("String_ExpenseAttribute") + ": " + LanguageManager.Instance.GetString("String_AttributeRESShort");
			lstFilter.Add(itmBOD);
			lstFilter.Add(itmAGI);
			lstFilter.Add(itmREA);
			lstFilter.Add(itmSTR);
			lstFilter.Add(itmCHA);
			lstFilter.Add(itmINT);
			lstFilter.Add(itmLOG);
			lstFilter.Add(itmWIL);
			lstFilter.Add(itmMAG);
			lstFilter.Add(itmRES);

			// Add Skill Groups to the filter.
			objXmlNodeList = objXmlDocument.SelectNodes("/chummer/categories/category[@type = \"active\"]");
			foreach (SkillGroup objGroup in _objCharacter.SkillGroups)
			{
				ListItem itmGroup = new ListItem();
				itmGroup.Value = "GROUP:" + objGroup.Name;
				itmGroup.Name = LanguageManager.Instance.GetString("String_ExpenseSkillGroup") + ": " + objGroup.DisplayName;
				lstFilter.Add(itmGroup);
			}

			cboSkillFilter.DataSource = lstFilter;
			cboSkillFilter.ValueMember = "Value";
			cboSkillFilter.DisplayMember = "Name";
			cboSkillFilter.SelectedIndex = 0;
			cboSkillFilter_SelectedIndexChanged(null, null);

			if (_objCharacter.MetatypeCategory == "Mundane Critters")
				mnuSpecialMutantCritter.Visible = true;
			if (_objCharacter.MetatypeCategory == "Mutant Critters")
				mnuSpecialToxicCritter.Visible = true;
			if (_objCharacter.MetatypeCategory == "Cyberzombie")
				mnuSpecialCyberzombie.Visible = false;

			_objFunctions.SortTree(treCyberware);
			_objFunctions.SortTree(treSpells);
			_objFunctions.SortTree(treComplexForms);
			_objFunctions.SortTree(treQualities);
			_objFunctions.SortTree(treCritterPowers);
			_objFunctions.SortTree(treMartialArts);
			UpdateMentorSpirits();
			UpdateInitiationGradeList();

			UpdateCharacterInfo();

			_blnIsDirty = false;
			UpdateWindowTitle(false);
			RefreshPasteStatus();

			// Stupid hack to get the MDI icon to show up properly.
			this.Icon = this.Icon.Clone() as System.Drawing.Icon;
		}

		private void frmCreate_FormClosing(object sender, FormClosingEventArgs e)
		{
			// If there are unsaved changes to the character, as the user if they would like to save their changes.
			if (_blnIsDirty)
			{
				string strCharacterName = _objCharacter.Alias;
				if (_objCharacter.Alias.Trim() == string.Empty)
					strCharacterName = LanguageManager.Instance.GetString("String_UnnamedCharacter");
				DialogResult objResult = MessageBox.Show(LanguageManager.Instance.GetString("Message_UnsavedChanges").Replace("{0}", strCharacterName), LanguageManager.Instance.GetString("MessageTitle_UnsavedChanges"), MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
				if (objResult == DialogResult.Yes)
				{
					// Attempt to save the Character. If the user cancels the Save As dialogue that may open, cancel the closing event so that changes are not lost.
					bool blnResult = SaveCharacter();
					if (!blnResult)
						e.Cancel = true;
				}
				else if (objResult == DialogResult.Cancel)
				{
					e.Cancel = true;
				}
			}
			// Reset the ToolStrip so the Save button is removed for the currently closing window.
			if (!e.Cancel)
			{
				if (!_blnSkipToolStripRevert)
					ToolStripManager.RevertMerge("toolStrip");

				// Unsubscribe from events.
				_objCharacter.MAGEnabledChanged -= objCharacter_MAGEnabledChanged;
				_objCharacter.RESEnabledChanged -= objCharacter_RESEnabledChanged;
				_objCharacter.AdeptTabEnabledChanged -= objCharacter_AdeptTabEnabledChanged;
				_objCharacter.MagicianTabEnabledChanged -= objCharacter_MagicianTabEnabledChanged;
				_objCharacter.TechnomancerTabEnabledChanged -= objCharacter_TechnomancerTabEnabledChanged;
				_objCharacter.InitiationTabEnabledChanged -= objCharacter_InitiationTabEnabledChanged;
				_objCharacter.CritterTabEnabledChanged -= objCharacter_CritterTabEnabledChanged;
				_objCharacter.BlackMarketEnabledChanged -= objCharacter_BlackMarketChanged;
				_objCharacter.UneducatedChanged -= objCharacter_UneducatedChanged;
				_objCharacter.UncouthChanged -= objCharacter_UncouthChanged;
				_objCharacter.InfirmChanged -= objCharacter_InfirmChanged;
				GlobalOptions.Instance.MRUChanged -= PopulateMRU;

				treGear.ItemDrag -= treGear_ItemDrag;
				treGear.DragEnter -= treGear_DragEnter;
				treGear.DragDrop -= treGear_DragDrop;

				treLifestyles.ItemDrag -= treLifestyles_ItemDrag;
				treLifestyles.DragEnter -= treLifestyles_DragEnter;
				treLifestyles.DragDrop -= treLifestyles_DragDrop;

				treArmor.ItemDrag -= treArmor_ItemDrag;
				treArmor.DragEnter -= treArmor_DragEnter;
				treArmor.DragDrop -= treArmor_DragDrop;

				treWeapons.ItemDrag -= treWeapons_ItemDrag;
				treWeapons.DragEnter -= treWeapons_DragEnter;
				treWeapons.DragDrop -= treWeapons_DragDrop;

				treVehicles.ItemDrag -= treVehicles_ItemDrag;
				treVehicles.DragEnter -= treVehicles_DragEnter;
				treVehicles.DragDrop -= treVehicles_DragDrop;

				// Remove events from all UserControls.
				foreach (SkillControl objSkillControl in panSkillGroups.Controls.OfType<SkillControl>())
				{
					objSkillControl.RatingChanged -= objActiveSkill_RatingChanged;
					objSkillControl.SpecializationChanged -= objSkill_SpecializationChanged;
					objSkillControl.BreakGroupClicked -= objSkill_BreakGroupClicked;
				}

				foreach (SkillGroupControl objGroupControl in panSkillGroups.Controls.OfType<SkillGroupControl>())
				{
					objGroupControl.GroupRatingChanged -= objGroup_RatingChanged;
				}

				foreach (SkillControl objSkillControl in panKnowledgeSkills.Controls.OfType<SkillControl>())
				{
					objSkillControl.RatingChanged -= objKnowledgeSkill_RatingChanged;
					objSkillControl.SpecializationChanged -= objSkill_SpecializationChanged;
					objSkillControl.DeleteSkill -= objKnowledgeSkill_DeleteSkill;
					objSkillControl.BreakGroupClicked -= objSkill_BreakGroupClicked;
				}

				foreach (ContactControl objContactControl in panContacts.Controls.OfType<ContactControl>())
				{
					objContactControl.ConnectionRatingChanged -= objContact_ConnectionRatingChanged;
					objContactControl.ConnectionGroupRatingChanged -= objContact_ConnectionGroupRatingChanged;
					objContactControl.LoyaltyRatingChanged -= objContact_LoyaltyRatingChanged;
					objContactControl.DeleteContact -= objContact_DeleteContact;
					objContactControl.FileNameChanged -= objContact_FileNameChanged;
				}

				foreach (ContactControl objContactControl in panEnemies.Controls.OfType<ContactControl>())
				{
					objContactControl.ConnectionRatingChanged -= objEnemy_ConnectionRatingChanged;
					objContactControl.ConnectionGroupRatingChanged -= objEnemy_ConnectionGroupRatingChanged;
					objContactControl.LoyaltyRatingChanged -= objEnemy_LoyaltyRatingChanged;
					objContactControl.DeleteContact -= objEnemy_DeleteContact;
					objContactControl.FileNameChanged -= objEnemy_FileNameChanged;
				}

				foreach (PetControl objContactControl in panPets.Controls.OfType<PetControl>())
				{
					objContactControl.DeleteContact -= objPet_DeleteContact;
					objContactControl.FileNameChanged -= objPet_FileNameChanged;
				}

				foreach (PowerControl objPowerControl in panPowers.Controls.OfType<PowerControl>())
				{
					objPowerControl.PowerRatingChanged -= objPower_PowerRatingChanged;
					objPowerControl.DeletePower -= objPower_DeletePower;
				}

				foreach (SpiritControl objSpiritControl in panSpirits.Controls.OfType<SpiritControl>())
				{
					objSpiritControl.ServicesOwedChanged -= objSpirit_ServicesOwedChanged;
					objSpiritControl.ForceChanged -= objSpirit_ForceChanged;
					objSpiritControl.BoundChanged -= objSpirit_BoundChanged;
					objSpiritControl.DeleteSpirit -= objSpirit_DeleteSpirit;
					objSpiritControl.FileNameChanged -= objSpirit_FileNameChanged;
				}

				foreach (SpiritControl objSpiritControl in panSprites.Controls.OfType<SpiritControl>())
				{
					objSpiritControl.ServicesOwedChanged -= objSprite_ServicesOwedChanged;
					objSpiritControl.ForceChanged -= objSprite_ForceChanged;
					objSpiritControl.BoundChanged -= objSprite_BoundChanged;
					objSpiritControl.DeleteSpirit -= objSprite_DeleteSpirit;
					objSpiritControl.FileNameChanged -= objSprite_FileNameChanged;
				}

				// Trash the global variables and dispose of the Form.
				_objOptions = null;
				_objCharacter = null;
				_objImprovementManager = null;
				this.Dispose(true);
			}
		}

		private void frmCreate_Activated(object sender, EventArgs e)
		{
			// Merge the ToolStrips.
			ToolStripManager.RevertMerge("toolStrip");
			ToolStripManager.Merge(toolStrip, "toolStrip");
		}

		private void frmCreate_Shown(object sender, EventArgs e)
		{
			// Clear all of the placeholder Labels.
			foreach (Label objLabel in tabCommon.Controls.OfType<Label>())
			{
				if (objLabel.Text.StartsWith("["))
					objLabel.Text = "";
			}

			foreach (Label objLabel in tabMartialArts.Controls.OfType<Label>())
			{
				if (objLabel.Text.StartsWith("["))
					objLabel.Text = "";
			}

			foreach (Label objLabel in tabMagician.Controls.OfType<Label>())
			{
				if (objLabel.Text.StartsWith("["))
					objLabel.Text = "";
			}

			foreach (Label objLabel in tabTechnomancer.Controls.OfType<Label>())
			{
				if (objLabel.Text.StartsWith("["))
					objLabel.Text = "";
			}

			foreach (Label objLabel in tabCyberware.Controls.OfType<Label>())
			{
				if (objLabel.Text.StartsWith("["))
					objLabel.Text = "";
			}

			foreach (Label objLabel in tabLifestyle.Controls.OfType<Label>())
			{
				if (objLabel.Text.StartsWith("["))
					objLabel.Text = "";
			}

			foreach (Label objLabel in tabArmor.Controls.OfType<Label>())
			{
				if (objLabel.Text.StartsWith("["))
					objLabel.Text = "";
			}

			foreach (Label objLabel in tabWeapons.Controls.OfType<Label>())
			{
				if (objLabel.Text.StartsWith("["))
					objLabel.Text = "";
			}

			foreach (Label objLabel in tabGear.Controls.OfType<Label>())
			{
				if (objLabel.Text.StartsWith("["))
					objLabel.Text = "";
			}

			foreach (Label objLabel in tabVehicles.Controls.OfType<Label>())
			{
				if (objLabel.Text.StartsWith("["))
					objLabel.Text = "";
			}

			foreach (Label objLabel in tabInitiation.Controls.OfType<Label>())
			{
				if (objLabel.Text.StartsWith("["))
					objLabel.Text = "";
			}

			foreach (Label objLabel in tabCritter.Controls.OfType<Label>())
			{
				if (objLabel.Text.StartsWith("["))
					objLabel.Text = "";
			}

			frmCreate_Resize(sender, e);
		}

		private void frmCreate_Resize(object sender, EventArgs e)
		{
			TabPage objPage = tabCharacterTabs.SelectedTab;
			// Reseize the form elements with the form.

			// Character Info Tab.
			int intHeight = ((objPage.Height - lblDescription.Top) / 4 - 20);
			txtDescription.Height = intHeight;
			lblBackground.Top = txtDescription.Top + txtDescription.Height + 3;
			txtBackground.Top = lblBackground.Top + lblBackground.Height + 3;
			txtBackground.Height = intHeight;
			lblConcept.Top = txtBackground.Top + txtBackground.Height + 3;
			txtConcept.Top = lblConcept.Top + lblConcept.Height + 3;
			txtConcept.Height = intHeight;
			lblNotes.Top = txtConcept.Top + txtConcept.Height + 3;
			txtNotes.Top = lblNotes.Top + lblNotes.Height + 3;
			txtNotes.Height = intHeight;
		}
		#endregion

		#region Character Events
		private void objCharacter_MAGEnabledChanged(object sender)
		{
			if (_blnReapplyImprovements)
				return;

			// Change to the status of MAG being enabled.
			lblMAGLabel.Enabled = _objCharacter.MAGEnabled;
			lblMAGAug.Enabled = _objCharacter.MAGEnabled;
			nudMAG.Enabled = _objCharacter.MAGEnabled;
			lblMAGMetatype.Enabled = _objCharacter.MAGEnabled;

			lblFoci.Visible = _objCharacter.MAGEnabled;
			treFoci.Visible = _objCharacter.MAGEnabled;
			cmdCreateStackedFocus.Visible = _objCharacter.MAGEnabled;

			if (_objCharacter.MAGEnabled)
			{
				int intEssenceLoss = 0;
				if (!_objOptions.ESSLossReducesMaximumOnly)
					intEssenceLoss = _objCharacter.EssencePenalty;
				nudMAG.Minimum = _objCharacter.MAG.MetatypeMinimum;
				nudMAG.Maximum = _objCharacter.MAG.MetatypeMaximum + intEssenceLoss;

				// If the character is being build with Karma, show the Initiation Tab.
				if (_objCharacter.BuildMethod == CharacterBuildMethod.Karma)
				{
					if (!tabCharacterTabs.TabPages.Contains(tabInitiation))
					{
						tabCharacterTabs.TabPages.Insert(3, tabInitiation);
						tabInitiation.Text = LanguageManager.Instance.GetString("Tab_Initiation");
						lblInitiateGradeLabel.Text = LanguageManager.Instance.GetString("Label_InitiationGrade");
						cmdAddMetamagic.Text = LanguageManager.Instance.GetString("Button_AddMetamagic");
						chkInitiationGroup.Text = LanguageManager.Instance.GetString("Checkbox_GroupInitiation");
						chkInitiationOrdeal.Text = LanguageManager.Instance.GetString("Checkbox_InitiationOrdeal");
						chkJoinGroup.Text = LanguageManager.Instance.GetString("Checkbox_JoinedGroup");
					}
				}
			}
			else
			{
				ClearInitiationTab();
				tabCharacterTabs.TabPages.Remove(tabInitiation);
				// Put MAG back to the Metatype minimum.
				nudMAG.Value = nudMAG.Minimum;
			}

			// Run through all of the Skills and Enable/Disable them as needed.
			foreach (SkillControl objSkillControl in panActiveSkills.Controls)
			{
				if (objSkillControl.Attribute == "MAG")
				{
					objSkillControl.Enabled = _objCharacter.MAGEnabled;
					if (!objSkillControl.Enabled)
						objSkillControl.SkillRating = 0;
				}
			}
			// Run through all of the Skill Groups and Disable them if all of their Skills are currently inaccessible.
			foreach (SkillGroupControl objSkillGroupControl in panSkillGroups.Controls)
			{
				bool blnEnabled = false;
				foreach (Skill objSkill in _objCharacter.Skills)
				{
					if (objSkill.SkillGroup == objSkillGroupControl.GroupName)
					{
						if (objSkill.Attribute == "MAG" || objSkill.Attribute == "RES")
						{
							if (objSkill.Attribute == "MAG" && _objCharacter.MAGEnabled)
								blnEnabled = true;
							if (objSkill.Attribute == "RES" && _objCharacter.RESEnabled)
								blnEnabled = true;
						}
						else
							blnEnabled = true;
					}
				}
				objSkillGroupControl.IsEnabled = blnEnabled;
				if (!blnEnabled)
					objSkillGroupControl.GroupRating = 0;
			}
		}

		private void objCharacter_RESEnabledChanged(object sender)
		{
			if (_blnReapplyImprovements)
				return;

			// Change to the status of RES being enabled.
			lblRESLabel.Enabled = _objCharacter.RESEnabled;
			lblRESAug.Enabled = _objCharacter.RESEnabled;
			nudRES.Enabled = _objCharacter.RESEnabled;
			lblRESMetatype.Enabled = _objCharacter.RESEnabled;

			if (_objCharacter.RESEnabled)
			{
				int intEssenceLoss = 0;
				if (!_objOptions.ESSLossReducesMaximumOnly)
					intEssenceLoss = _objCharacter.EssencePenalty;
				nudRES.Minimum = _objCharacter.RES.MetatypeMinimum;
				nudRES.Maximum = _objCharacter.RES.MetatypeMaximum + intEssenceLoss;

				// If the character is being build with Karma, show the Initiation Tab.
				if (_objCharacter.BuildMethod == CharacterBuildMethod.Karma){
					if (!tabCharacterTabs.TabPages.Contains(tabInitiation))
					{
						tabCharacterTabs.TabPages.Insert(3, tabInitiation);
						tabInitiation.Text = LanguageManager.Instance.GetString("Tab_Submersion");
						lblInitiateGradeLabel.Text = LanguageManager.Instance.GetString("Label_SubmersionGrade");
						cmdAddMetamagic.Text = LanguageManager.Instance.GetString("Button_AddEcho");
						chkInitiationGroup.Text = LanguageManager.Instance.GetString("Checkbox_NetworkSubmersion");
						chkInitiationOrdeal.Text = LanguageManager.Instance.GetString("Checkbox_SubmersionTask");
						chkJoinGroup.Text = LanguageManager.Instance.GetString("Checkbox_JoinedNetwork");
					}
				}
			}
			else
			{
				ClearInitiationTab();
				tabCharacterTabs.TabPages.Remove(tabInitiation);
				// Put RES back to the Metatype minimum.
				nudRES.Value = nudRES.Minimum;
			}

			// Run through all of the Skills and Enable/Disable them as needed.
			foreach (SkillControl objSkillControl in panActiveSkills.Controls)
			{
				if (objSkillControl.Attribute == "RES")
				{
					objSkillControl.Enabled = _objCharacter.RESEnabled;
					if (!objSkillControl.Enabled)
						objSkillControl.SkillRating = 0;
				}
			}
			// Run through all of the Skill Groups and Disable them if all of their Skills are currently inaccessible.
			foreach (SkillGroupControl objSkillGroupControl in panSkillGroups.Controls)
			{
				bool blnEnabled = false;
				foreach (Skill objSkill in _objCharacter.Skills)
				{
					if (objSkill.SkillGroup == objSkillGroupControl.GroupName)
					{
						if (objSkill.Attribute == "MAG" || objSkill.Attribute == "RES")
						{
							if (objSkill.Attribute == "MAG" && _objCharacter.MAGEnabled)
								blnEnabled = true;
							if (objSkill.Attribute == "RES" && _objCharacter.RESEnabled)
								blnEnabled = true;
						}
						else
							blnEnabled = true;
					}
				}
				objSkillGroupControl.IsEnabled = blnEnabled;
				if (!blnEnabled)
					objSkillGroupControl.GroupRating = 0;
			}
		}

		private void objCharacter_AdeptTabEnabledChanged(object sender)
		{
			if (_blnReapplyImprovements)
				return;

			// Change to the status of Adept being enabled.
			if (_objCharacter.AdeptEnabled)
			{
				if (!tabCharacterTabs.TabPages.Contains(tabAdept))
					tabCharacterTabs.TabPages.Insert(3, tabAdept);

				CalculatePowerPoints();
			}
			else
			{
				ClearAdeptTab();
				tabCharacterTabs.TabPages.Remove(tabAdept);
			}

			// Show the Mystic Adept control if the character is a Mystic Adept, otherwise hide them.
			if (_objCharacter.AdeptEnabled && _objCharacter.MagicianEnabled)
			{
				lblMysticAdeptAssignment.Visible = true;
				lblMysticAdeptAssignmentAdept.Visible = true;
				lblMysticAdeptAssignmentMagician.Visible = true;
				lblMysticAdeptMAGAdept.Visible = true;
				nudMysticAdeptMAGMagician.Visible = true;
				nudMysticAdeptMAGMagician.Maximum = _objCharacter.MAG.TotalValue;
			}
			else
			{
				lblMysticAdeptAssignment.Visible = false;
				lblMysticAdeptAssignmentAdept.Visible = false;
				lblMysticAdeptAssignmentMagician.Visible = false;
				lblMysticAdeptMAGAdept.Visible = false;
				nudMysticAdeptMAGMagician.Visible = false;
			}
		}

		private void objCharacter_MagicianTabEnabledChanged(object sender)
		{
			if (_blnReapplyImprovements)
				return;

			// Change to the status of Magician being enabled.
			if (_objCharacter.MagicianEnabled)
			{
				if (!tabCharacterTabs.TabPages.Contains(tabMagician))
					tabCharacterTabs.TabPages.Insert(3, tabMagician);
			}
			else
			{
				ClearSpellTab();
				tabCharacterTabs.TabPages.Remove(tabMagician);
			}

			// Show the Mystic Adept control if the character is a Mystic Adept, otherwise hide them.
			if (_objCharacter.AdeptEnabled && _objCharacter.MagicianEnabled)
			{
				lblMysticAdeptAssignment.Visible = true;
				lblMysticAdeptAssignmentAdept.Visible = true;
				lblMysticAdeptAssignmentMagician.Visible = true;
				lblMysticAdeptMAGAdept.Visible = true;
				nudMysticAdeptMAGMagician.Visible = true;
				nudMysticAdeptMAGMagician.Maximum = _objCharacter.MAG.TotalValue;
			}
			else
			{
				lblMysticAdeptAssignment.Visible = false;
				lblMysticAdeptAssignmentAdept.Visible = false;
				lblMysticAdeptAssignmentMagician.Visible = false;
				lblMysticAdeptMAGAdept.Visible = false;
				nudMysticAdeptMAGMagician.Visible = false;
			}
		}

		private void objCharacter_TechnomancerTabEnabledChanged(object sender)
		{
			if (_blnReapplyImprovements)
				return;

			// Change to the status of Technomancer being enabled.
			if (_objCharacter.TechnomancerEnabled)
			{
				if (!tabCharacterTabs.TabPages.Contains(tabTechnomancer))
					tabCharacterTabs.TabPages.Insert(3, tabTechnomancer);
			}
			else
			{
				ClearTechnomancerTab();
				tabCharacterTabs.TabPages.Remove(tabTechnomancer);
			}
		}

		private void objCharacter_InitiationTabEnabledChanged(object sender)
		{
			if (_blnReapplyImprovements)
				return;

			// If we're building with Karma, do nothing since this only applies to BP.
			if (_objCharacter.BuildMethod == CharacterBuildMethod.Karma)
				return;

			// Change the status of the Initiation tab being show.
			if (_objCharacter.InitiationEnabled)
			{
				if (!tabCharacterTabs.TabPages.Contains(tabInitiation))
					tabCharacterTabs.TabPages.Insert(4, tabInitiation);
			}
			else
			{
				ClearInitiationTab();
				tabCharacterTabs.TabPages.Remove(tabInitiation);
			}
		}

		private void objCharacter_CritterTabEnabledChanged(object sender)
		{
			if (_blnReapplyImprovements)
				return;

			// Change the status of Critter being enabled.
			if (_objCharacter.CritterEnabled)
			{
				if (!tabCharacterTabs.TabPages.Contains(tabCritter))
					tabCharacterTabs.TabPages.Insert(3, tabCritter);
			}
			else
			{
				// Remove all Critter Powers.
				ClearCritterTab();
				tabCharacterTabs.TabPages.Remove(tabCritter);
			}
		}

		private void objCharacter_BlackMarketChanged(object sender)
		{
			if (_blnReapplyImprovements)
				return;

			// Change the status of Black Market being enabled.
			if (_objCharacter.BlackMarket)
			{
				chkCyberwareBlackMarketDiscount.Visible = true;
				chkArmorBlackMarketDiscount.Visible = true;
				chkWeaponBlackMarketDiscount.Visible = true;
				chkGearBlackMarketDiscount.Visible = true;
				chkVehicleBlackMarketDiscount.Visible = true;
			}
			else
			{
				chkCyberwareBlackMarketDiscount.Visible = false;
				chkArmorBlackMarketDiscount.Visible = false;
				chkWeaponBlackMarketDiscount.Visible = false;
				chkGearBlackMarketDiscount.Visible = false;
				chkVehicleBlackMarketDiscount.Visible = false;
			}
		}

		private void objCharacter_UneducatedChanged(object sender)
		{
			if (_blnReapplyImprovements)
				return;

			// Change to the status of Uneducated being enabled.
			if (_objCharacter.Uneducated)
			{
				// If Uneducated is being added, run through all of the Technical Active Skills and disable them.
				// Do not break SkillGroups as these will be used if this is ever removed.
				foreach (SkillGroupControl objSkillGroupControl in panSkillGroups.Controls)
				{
					if (objSkillGroupControl.HasTechnicalSkills)
					{
						objSkillGroupControl.GroupRating = 0;
						objSkillGroupControl.IsEnabled = false;
					}
				}
			}
			else
			{
				// If Uneducated is being removed, run through all of the Technical Active Skills and re-enable them.
				// If they were a part of a SkillGroup, set their Rating back.
				foreach (SkillGroupControl objSkillGroupControl in panSkillGroups.Controls)
				{
					if (objSkillGroupControl.HasTechnicalSkills)
					{
						objSkillGroupControl.IsEnabled = true;
					}
				}
			}
		}

		private void objCharacter_UncouthChanged(object sender)
		{
			if (_blnReapplyImprovements)
				return;

			// Change to the status of Uncouth being enabled.
			if (_objCharacter.Uncouth)
			{
				// If Uncouth is being added, run through all of the Social Active Skills and disable them.
				// Do not break SkillGroups as these will be used if this is ever removed.
				foreach (SkillGroupControl objSkillGroupControl in panSkillGroups.Controls)
				{
					if (objSkillGroupControl.HasSocialSkills)
					{
						objSkillGroupControl.GroupRating = 0;
						objSkillGroupControl.IsEnabled = false;
					}
				}
			}
			else
			{
				// If Uncouth is being removed, run through all of the Social Active Skills and re-enable them.
				// If they were a part of a SkillGroup, set their Rating back.
				foreach (SkillGroupControl objSkillGroupControl in panSkillGroups.Controls)
				{
					if (objSkillGroupControl.HasSocialSkills)
					{
						objSkillGroupControl.IsEnabled = true;
					}
				}
			}
		}

		private void objCharacter_InfirmChanged(object sender)
		{
			if (_blnReapplyImprovements)
				return;

			// Change to the status of Infirm being enabled.
			if (_objCharacter.Infirm)
			{
				// If Infirm is being added, run through all of the Physical Active Skills and disable them.
				// Do not break SkillGroups as these will be used if this is ever removed.
				foreach (SkillGroupControl objSkillGroupControl in panSkillGroups.Controls)
				{
					if (objSkillGroupControl.HasPhysicalSkills)
					{
						objSkillGroupControl.GroupRating = 0;
						objSkillGroupControl.IsEnabled = false;
					}
				}
			}
			else
			{
				// If Infirm is being removed, run through all of the Physical Active Skills and re-enable them.
				// If they were a part of a SkillGroup, set their Rating back.
				foreach (SkillGroupControl objSkillGroupControl in panSkillGroups.Controls)
				{
					if (objSkillGroupControl.HasPhysicalSkills)
					{
						objSkillGroupControl.IsEnabled = true;
					}
				}
			}
		}
		#endregion

		#region Menu Events
		private void mnuFileSave_Click(object sender, EventArgs e)
		{
			SaveCharacter();
		}

		private void mnuFileSaveAs_Click(object sender, EventArgs e)
		{
			SaveCharacterAs();
		}

		private void tsbSave_Click(object sender, EventArgs e)
		{
			mnuFileSave_Click(sender, e);
		}

		private void tsbPrint_Click(object sender, EventArgs e)
		{
			_objCharacter.Print(false);
		}

		private void mnuFilePrint_Click(object sender, EventArgs e)
		{
			_objCharacter.Print(false);
		}

		private void mnuFileClose_Click(object sender, EventArgs e)
		{
			this.Close();
		}

		private void mnuSpecialAddPACKSKit_Click(object sender, EventArgs e)
		{
			AddPACKSKit();
		}

		private void mnuSpecialCreatePACKSKit_Click(object sender, EventArgs e)
		{
			CreatePACKSKit();
		}

		private void mnuSpecialChangeMetatype_Click(object sender, EventArgs e)
		{
			ChangeMetatype();
		}

		private void mnuSpecialMutantCritter_Click(object sender, EventArgs e)
		{
			_objCharacter.MetatypeCategory = "Mutant Critters";
			mnuSpecialMutantCritter.Visible = false;
			mnuSpecialToxicCritter.Visible = true;

			// Update the Critter's Attribute maximums to 1.5X their current value (or 1, whichever is higher).
			_objCharacter.BOD.MetatypeMaximum = Math.Max(1, Convert.ToInt32(Math.Ceiling(Convert.ToDouble(_objCharacter.BOD.Value, GlobalOptions.Instance.CultureInfo) * 1.5)));
			_objCharacter.AGI.MetatypeMaximum = Math.Max(1, Convert.ToInt32(Math.Ceiling(Convert.ToDouble(_objCharacter.AGI.Value, GlobalOptions.Instance.CultureInfo) * 1.5)));
			_objCharacter.REA.MetatypeMaximum = Math.Max(1, Convert.ToInt32(Math.Ceiling(Convert.ToDouble(_objCharacter.REA.Value, GlobalOptions.Instance.CultureInfo) * 1.5)));
			_objCharacter.STR.MetatypeMaximum = Math.Max(1, Convert.ToInt32(Math.Ceiling(Convert.ToDouble(_objCharacter.STR.Value, GlobalOptions.Instance.CultureInfo) * 1.5)));
			_objCharacter.CHA.MetatypeMaximum = Math.Max(1, Convert.ToInt32(Math.Ceiling(Convert.ToDouble(_objCharacter.CHA.Value, GlobalOptions.Instance.CultureInfo) * 1.5)));
			_objCharacter.INT.MetatypeMaximum = Math.Max(1, Convert.ToInt32(Math.Ceiling(Convert.ToDouble(_objCharacter.INT.Value, GlobalOptions.Instance.CultureInfo) * 1.5)));
			_objCharacter.LOG.MetatypeMaximum = Math.Max(1, Convert.ToInt32(Math.Ceiling(Convert.ToDouble(_objCharacter.LOG.Value, GlobalOptions.Instance.CultureInfo) * 1.5)));
			_objCharacter.WIL.MetatypeMaximum = Math.Max(1, Convert.ToInt32(Math.Ceiling(Convert.ToDouble(_objCharacter.WIL.Value, GlobalOptions.Instance.CultureInfo) * 1.5)));
			_objCharacter.EDG.MetatypeMaximum = Math.Max(1, Convert.ToInt32(Math.Ceiling(Convert.ToDouble(_objCharacter.EDG.Value, GlobalOptions.Instance.CultureInfo) * 1.5)));
			_objCharacter.MAG.MetatypeMaximum = Convert.ToInt32(Math.Ceiling(Convert.ToDouble(_objCharacter.CHA.Value, GlobalOptions.Instance.CultureInfo) * 1.5));
			_objCharacter.RES.MetatypeMaximum = Convert.ToInt32(Math.Ceiling(Convert.ToDouble(_objCharacter.REA.Value, GlobalOptions.Instance.CultureInfo) * 1.5));

			_objCharacter.BOD.MetatypeMinimum = _objCharacter.BOD.Value;
			_objCharacter.AGI.MetatypeMinimum = _objCharacter.AGI.Value;
			_objCharacter.REA.MetatypeMinimum = _objCharacter.REA.Value;
			_objCharacter.STR.MetatypeMinimum = _objCharacter.STR.Value;
			_objCharacter.CHA.MetatypeMinimum = _objCharacter.CHA.Value;
			_objCharacter.INT.MetatypeMinimum = _objCharacter.INT.Value;
			_objCharacter.LOG.MetatypeMinimum = _objCharacter.LOG.Value;
			_objCharacter.WIL.MetatypeMinimum = _objCharacter.WIL.Value;
			_objCharacter.EDG.MetatypeMinimum = _objCharacter.EDG.Value;
			_objCharacter.MAG.MetatypeMinimum = _objCharacter.MAG.Value;
			_objCharacter.RES.MetatypeMinimum = _objCharacter.RES.Value;

			// Count the number of Skill points the Critter currently has.
			foreach (Skill objSkill in _objCharacter.Skills)
				_objCharacter.MutantCritterBaseSkills += objSkill.Rating;

			UpdateCharacterInfo();
		}

		private void mnuSpecialToxicCritter_Click(object sender, EventArgs e)
		{
			_objCharacter.MetatypeCategory = "Toxic Critters";
			mnuSpecialToxicCritter.Visible = false;
		}

		private void mnuSpecialCyberzombie_Click(object sender, EventArgs e)
		{
			bool blnEssence = true;
			bool blnCyberware = false;
			string strMessage = LanguageManager.Instance.GetString("Message_CyberzombieRequirements");

			// Make sure the character has an Essence lower than 0.
			if (_objCharacter.Essence >= 0)
			{
				strMessage += "\n\t" + LanguageManager.Instance.GetString("Message_CyberzombieRequirementsEssence");
				blnEssence = false;
			}

			// Make sure the character has an Invoked Memory Stimulator.
			foreach (Cyberware objCyberware in _objCharacter.Cyberware)
			{
				if (objCyberware.Name == "Invoked Memory Stimulator")
					blnCyberware = true;
			}

			if (!blnCyberware)
				strMessage += "\n\t" + LanguageManager.Instance.GetString("Message_CyberzombieRequirementsStimulator");

			if (!blnEssence || !blnCyberware)
			{
				MessageBox.Show(strMessage, LanguageManager.Instance.GetString("MessageTitle_CyberzombieRequirements"), MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			if (MessageBox.Show(LanguageManager.Instance.GetString("Message_CyberzombieConfirm"), LanguageManager.Instance.GetString("MessageTitle_CyberzombieConfirm"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
				return;

			// Get the player to roll Dice to make a WIL Test and record the result.
			frmDiceHits frmWILHits = new frmDiceHits();
			frmWILHits.Text = LanguageManager.Instance.GetString("String_CyberzombieWILText");
			frmWILHits.Description = LanguageManager.Instance.GetString("String_CyberzombieWILDescription");
			int intDice = _objCharacter.WIL.TotalValue;
			int intThreshold = 3 + (Convert.ToInt32(_objCharacter.EssencePenalty - Convert.ToInt32(_objCharacter.EssenceMaximum)));
			frmWILHits.Dice = intDice;
			frmWILHits.ShowDialog(this);

			if (frmWILHits.DialogResult != DialogResult.OK)
				return;

			int intWILResult = frmWILHits.Result;

			// The character gains 10 + ((Threshold - Hits) * 10)BP worth of Negative Qualities.
			int intResult = 10;
			if (intWILResult < intThreshold)
			{
				intResult = (intThreshold - intWILResult) * 10;
			}
			_objImprovementManager.CreateImprovement("", Improvement.ImprovementSource.Cyberzombie, "Cyberzombie Qualities", Improvement.ImprovementType.FreeNegativeQualities, "", intResult * -1);

			// Convert the character.
			// Characters lose access to Resonance.
			_objCharacter.RESEnabled = false;
			
			// Gain MAG that is permanently set to 1.
			_objCharacter.MAGEnabled = true;
			_objCharacter.MAG.MetatypeMinimum = 1;
			_objCharacter.MAG.MetatypeMaximum = 1;
			_objCharacter.MAG.Value = 1;

			// Add the Cyberzombie Lifestyle if it is not already taken.
			bool blnHasLifestyle = false;
			foreach (Lifestyle objLifestyle in _objCharacter.Lifestyles)
			{
				if (objLifestyle.Name == "Cyberzombie Lifestyle Addition")
					blnHasLifestyle = true;
			}
			if (!blnHasLifestyle)
			{
				XmlDocument objXmlLifestyleDocument = XmlManager.Instance.Load("lifestyles.xml");
				XmlNode objXmlLifestyle = objXmlLifestyleDocument.SelectSingleNode("/chummer/lifestyles/lifestyle[name = \"Cyberzombie Lifestyle Addition\"]");

				TreeNode objLifestyleNode = new TreeNode();
				Lifestyle objLifestyle = new Lifestyle(_objCharacter);
				objLifestyle.Create(objXmlLifestyle, objLifestyleNode);
				_objCharacter.Lifestyles.Add(objLifestyle);

				treLifestyles.Nodes[0].Nodes.Add(objLifestyleNode);
				treLifestyles.Nodes[0].Expand();
			}

			// Change the MetatypeCategory to Cyberzombie.
			_objCharacter.MetatypeCategory = "Cyberzombie";

			// Gain access to Critter Powers.
			_objCharacter.CritterEnabled = true;

			// Gain the Dual Natured Critter Power if it does not yet exist.
			bool blnHasPower = false;
			foreach (CritterPower objPower in _objCharacter.CritterPowers)
			{
				if (objPower.Name == "Dual Natured")
					blnHasPower = true;
			}
			if (!blnHasPower)
			{
				XmlDocument objXmlPowerDocument = XmlManager.Instance.Load("critterpowers.xml");
				XmlNode objXmlPowerNode = objXmlPowerDocument.SelectSingleNode("/chummer/powers/power[name = \"Dual Natured\"]");

				TreeNode objNode = new TreeNode();
				CritterPower objCritterPower = new CritterPower(_objCharacter);
				objCritterPower.Create(objXmlPowerNode, _objCharacter, objNode);
				_objCharacter.CritterPowers.Add(objCritterPower);

				treCritterPowers.Nodes[0].Nodes.Add(objNode);
				treCritterPowers.Nodes[0].Expand();
			}

			// Gain the Immunity (Normal Weapons) Critter Power if it does not yet exist.
			blnHasPower = false;
			foreach (CritterPower objPower in _objCharacter.CritterPowers)
			{
				if (objPower.Name == "Immunity" && objPower.Extra == "Normal Weapons")
					blnHasPower = true;
			}
			if (!blnHasPower)
			{
				XmlDocument objXmlPowerDocument = XmlManager.Instance.Load("critterpowers.xml");
				XmlNode objXmlPowerNode = objXmlPowerDocument.SelectSingleNode("/chummer/powers/power[name = \"Immunity\"]");

				TreeNode objNode = new TreeNode();
				CritterPower objCritterPower = new CritterPower(_objCharacter);
				objCritterPower.Create(objXmlPowerNode, _objCharacter, objNode, 0, "Normal Weapons");
				_objCharacter.CritterPowers.Add(objCritterPower);

				treCritterPowers.Nodes[0].Nodes.Add(objNode);
				treCritterPowers.Nodes[0].Expand();
			}

			mnuSpecialCyberzombie.Visible = false;

			_blnIsDirty = true;
			UpdateWindowTitle();

			UpdateCharacterInfo();
		}

		private void mnuSpecialAddCyberwareSuite_Click(object sender, EventArgs e)
		{
			AddCyberwareSuite(Improvement.ImprovementSource.Cyberware);
		}

		private void mnuSpecialAddBiowareSuite_Click(object sender, EventArgs e)
		{
			AddCyberwareSuite(Improvement.ImprovementSource.Bioware);
		}

		private void mnuSpecialCreateCyberwareSuite_Click(object sender, EventArgs e)
		{
			CreateCyberwareSuite(Improvement.ImprovementSource.Cyberware);
		}

		private void mnuSpecialCreateBiowareSuite_Click(object sender, EventArgs e)
		{
			CreateCyberwareSuite(Improvement.ImprovementSource.Bioware);
		}

		private void Menu_DropDownOpening(object sender, EventArgs e)
		{
			foreach (ToolStripMenuItem objItem in ((ToolStripMenuItem)sender).DropDownItems.OfType<ToolStripMenuItem>())
			{
				if (objItem.Tag != null)
				{
					objItem.Text = LanguageManager.Instance.GetString(objItem.Tag.ToString());
				}
			}
		}

		private void mnuSpecialReapplyImprovements_Click(object sender, EventArgs e)
		{
			// This only re-applies the Improvements for everything the character has. If a match is not found in the data files, the current Improvement information is left as-is.
			// Verify that the user wants to go through with it.
			if (MessageBox.Show(LanguageManager.Instance.GetString("Message_ConfirmReapplyImprovements"), LanguageManager.Instance.GetString("MessageTitle_ConfirmReapplyImprovements"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
				return;
			
			// Record the status of any flags that normally trigger character events.
			bool blnMAGEnabled = _objCharacter.MAGEnabled;
			bool blnRESEnabled = _objCharacter.RESEnabled;
			bool blnUneducated = _objCharacter.Uneducated;
			bool blnUncouth = _objCharacter.Uncouth;
			bool blnInfirm = _objCharacter.Infirm;

			_blnReapplyImprovements = true;

			// Refresh Qualities.
			XmlDocument objXmlDocument = XmlManager.Instance.Load("qualities.xml");
			foreach (Quality objQuality in _objCharacter.Qualities)
			{
				string strSelected = objQuality.Extra;

				XmlNode objNode = objXmlDocument.SelectSingleNode("/chummer/qualities/quality[name = \"" + objQuality.Name + "\"]");
				if (objNode != null)
				{
					_objImprovementManager.RemoveImprovements(Improvement.ImprovementSource.Quality, objQuality.InternalId);
					if (objNode["bonus"] != null)
					{
						_objImprovementManager.ForcedValue = strSelected;
						_objImprovementManager.CreateImprovements(Improvement.ImprovementSource.Quality, objQuality.InternalId, objNode["bonus"], false, 1, objQuality.DisplayNameShort);
						if (_objImprovementManager.SelectedValue != "")
							objQuality.Extra = _objImprovementManager.SelectedValue;

						for (int i = 0; i <= 1; i++)
						{
							foreach (TreeNode objTreeNode in treQualities.Nodes[i].Nodes)
							{
								if (objTreeNode.Tag.ToString() == objQuality.InternalId)
								{
									objTreeNode.Text = objQuality.DisplayName;
									break;
								}
							}
						}
					}
				}
			}

			// Refresh Martial Art Advantages.
			objXmlDocument = XmlManager.Instance.Load("martialarts.xml");
			foreach (MartialArt objMartialArt in _objCharacter.MartialArts)
			{
				foreach (MartialArtAdvantage objAdvantage in objMartialArt.Advantages)
				{
					XmlNode objNode = objXmlDocument.SelectSingleNode("/chummer/martialarts/martialart[name = \"" + objMartialArt.Name + "\"]/advantages/advantage[name = \"" + objAdvantage.Name + "\"]");
					if (objNode != null)
					{
						_objImprovementManager.RemoveImprovements(Improvement.ImprovementSource.MartialArtAdvantage, objAdvantage.InternalId);
						if (objNode["bonus"] != null)
						{
							_objImprovementManager.CreateImprovements(Improvement.ImprovementSource.MartialArtAdvantage, objAdvantage.InternalId, objNode["bonus"], false, 1, objAdvantage.DisplayNameShort);
						}
					}
				}
			}

			// Refresh Spells.
			objXmlDocument = XmlManager.Instance.Load("spells.xml");
			foreach (Spell objSpell in _objCharacter.Spells)
			{
				string strSelected = objSpell.Extra;

				XmlNode objNode = objXmlDocument.SelectSingleNode("/chummer/spells/spell[name = \"" + objSpell.Name + "\"]");
				if (objNode != null)
				{
					_objImprovementManager.RemoveImprovements(Improvement.ImprovementSource.Spell, objSpell.InternalId);
					if (objNode["bonus"] != null)
					{
						_objImprovementManager.ForcedValue = strSelected;
						_objImprovementManager.CreateImprovements(Improvement.ImprovementSource.Spell, objSpell.InternalId, objNode["bonus"], false, 1, objSpell.DisplayNameShort);
						if (_objImprovementManager.SelectedValue != "")
							objSpell.Extra = _objImprovementManager.SelectedValue;

						foreach (TreeNode objParentNode in treSpells.Nodes)
						{
							foreach (TreeNode objChildNode in objParentNode.Nodes)
							{
								if (objChildNode.Tag.ToString() == objSpell.InternalId)
								{
									objChildNode.Text = objSpell.DisplayName;
									break;
								}
							}
						}
					}
				}
			}

			// Refresh Adept Powers.
			objXmlDocument = XmlManager.Instance.Load("powers.xml");
			foreach (Power objPower in _objCharacter.Powers)
			{
				string strSelected = objPower.Extra;

				XmlNode objNode = objXmlDocument.SelectSingleNode("/chummer/powers/power[name = \"" + objPower.Name + "\"]");
				if (objNode != null)
				{
					_objImprovementManager.RemoveImprovements(Improvement.ImprovementSource.Power, objPower.InternalId);
					if (objNode["bonus"] != null)
					{
						_objImprovementManager.ForcedValue = strSelected;
						_objImprovementManager.CreateImprovements(Improvement.ImprovementSource.Power, objPower.InternalId, objNode["bonus"], false, Convert.ToInt32(objPower.Rating), objPower.DisplayNameShort);
					}
				}
			}

			// Refresh Complex Forms.
			objXmlDocument = XmlManager.Instance.Load("programs.xml");
			foreach (TechProgram objProgram in _objCharacter.TechPrograms)
			{
				string strSelected = objProgram.Extra;

				XmlNode objNode = objXmlDocument.SelectSingleNode("/chummer/programs/program[name = \"" + objProgram.Name + "\"]");
				if (objNode != null)
				{
					_objImprovementManager.RemoveImprovements(Improvement.ImprovementSource.ComplexForm, objProgram.InternalId);
					if (objNode["bonus"] != null)
					{
						_objImprovementManager.ForcedValue = strSelected;
						_objImprovementManager.CreateImprovements(Improvement.ImprovementSource.ComplexForm, objProgram.InternalId, objNode["bonus"], false, objProgram.Rating, objProgram.DisplayNameShort);
						if (_objImprovementManager.SelectedValue != "")
							objProgram.Extra = _objImprovementManager.SelectedValue;

						foreach (TreeNode objParentNode in treComplexForms.Nodes)
						{
							foreach (TreeNode objChildNode in objParentNode.Nodes)
							{
								if (objChildNode.Tag.ToString() == objProgram.InternalId)
								{
									objChildNode.Text = objProgram.DisplayName;
									break;
								}
							}
						}
					}
				}

				foreach (TechProgramOption objOption in objProgram.Options)
				{
					string strOptionSelected = objOption.Extra;

					XmlNode objChild = objXmlDocument.SelectSingleNode("/chummer/options/option[name = \"" + objOption.Name + "\"]");

					if (objChild != null)
					{
						_objImprovementManager.RemoveImprovements(Improvement.ImprovementSource.ComplexForm, objOption.InternalId);
						if (objChild["bonus"] != null)
						{
							_objImprovementManager.ForcedValue = strOptionSelected;
							_objImprovementManager.CreateImprovements(Improvement.ImprovementSource.ComplexForm, objOption.InternalId, objChild["bonus"], false, objOption.Rating, objOption.DisplayNameShort);
							if (_objImprovementManager.SelectedValue != "")
								objOption.Extra = _objImprovementManager.SelectedValue;

							foreach (TreeNode objParentNode in treComplexForms.Nodes)
							{
								foreach (TreeNode objChildNode in objParentNode.Nodes)
								{
									foreach (TreeNode objPluginNode in objChildNode.Nodes)
									{
										if (objPluginNode.Tag.ToString() == objProgram.InternalId)
										{
											objPluginNode.Text = objProgram.DisplayName;
											break;
										}
									}
								}
							}
						}
					}
				}
			}

			// Refresh Critter Powers.
			objXmlDocument = XmlManager.Instance.Load("critterpowers.xml");
			foreach (CritterPower objPower in _objCharacter.CritterPowers)
			{
				string strSelected = objPower.Extra;

				XmlNode objNode = objXmlDocument.SelectSingleNode("/chummer/powers/power[name = \"" + objPower.Name + "\"]");

				if (objNode != null)
				{
					_objImprovementManager.RemoveImprovements(Improvement.ImprovementSource.CritterPower, objPower.InternalId);
					if (objNode["bonus"] != null)
					{
						int intRating = 0;
						try
						{
							intRating = Convert.ToInt32(strSelected);
						}
						catch
						{
							_objImprovementManager.ForcedValue = strSelected;
						}
						_objImprovementManager.CreateImprovements(Improvement.ImprovementSource.CritterPower, objPower.InternalId, objNode["bonus"], false, intRating, objPower.DisplayNameShort);
						if (_objImprovementManager.SelectedValue != "")
							objPower.Extra = _objImprovementManager.SelectedValue;

						foreach (TreeNode objParentNode in treComplexForms.Nodes)
						{
							foreach (TreeNode objChildNode in objParentNode.Nodes)
							{
								if (objChildNode.Tag.ToString() == objPower.InternalId)
								{
									objChildNode.Text = objPower.DisplayName;
									break;
								}
							}
						}
					}
				}
			}

			// Refresh Metamagics and Echoes.
			foreach (Metamagic objMetamagic in _objCharacter.Metamagics)
			{
				if (objMetamagic.SourceType == Improvement.ImprovementSource.Metamagic)
				{
					objXmlDocument = XmlManager.Instance.Load("metamagic.xml");
					XmlNode objNode = objXmlDocument.SelectSingleNode("/chummer/metamagics/metamagic[name = \"" + objMetamagic.Name + "\"]");

					if (objNode != null)
					{
						_objImprovementManager.RemoveImprovements(Improvement.ImprovementSource.Metamagic, objMetamagic.InternalId);
						if (objNode["bonus"] != null)
							_objImprovementManager.CreateImprovements(Improvement.ImprovementSource.Metamagic, objMetamagic.InternalId, objNode["bonus"], false, 1, objMetamagic.DisplayNameShort);
					}
				}
				else
				{
					objXmlDocument = XmlManager.Instance.Load("echoes.xml");
					XmlNode objNode = objXmlDocument.SelectSingleNode("/chummer/echoes/echo[name = \"" + objMetamagic.Name + "\"]");

					if (objNode != null)
					{
						_objImprovementManager.RemoveImprovements(Improvement.ImprovementSource.Echo, objMetamagic.InternalId);
						if (objNode["bonus"] != null)
							_objImprovementManager.CreateImprovements(Improvement.ImprovementSource.Echo, objMetamagic.InternalId, objNode["bonus"], false, 1, objMetamagic.DisplayNameShort);
					}
				}
			}

			// Refresh Cyberware and Bioware.
			foreach (Cyberware objCyberware in _objCharacter.Cyberware)
			{
				if (objCyberware.SourceType == Improvement.ImprovementSource.Cyberware)
				{
					objXmlDocument = XmlManager.Instance.Load("cyberware.xml");
					XmlNode objNode = objXmlDocument.SelectSingleNode("/chummer/cyberwares/cyberware[name = \"" + objCyberware.Name + "\"]");

					if (objNode != null)
					{
						_objImprovementManager.RemoveImprovements(Improvement.ImprovementSource.Cyberware, objCyberware.InternalId);
						if (objNode["bonus"] != null)
							_objImprovementManager.CreateImprovements(Improvement.ImprovementSource.Cyberware, objCyberware.InternalId, objNode["bonus"], false, objCyberware.Rating, objCyberware.DisplayNameShort);
						if (_objImprovementManager.SelectedValue != "")
							objCyberware.Location = _objImprovementManager.SelectedValue;

						foreach (TreeNode objParentNode in treCyberware.Nodes)
						{
							foreach (TreeNode objChildNode in objParentNode.Nodes)
							{
								if (objChildNode.Tag.ToString() == objCyberware.InternalId)
								{
									objChildNode.Text = objCyberware.DisplayName;
									break;
								}
							}
						}
					}

					foreach (Cyberware objPlugin in objCyberware.Children)
					{
						XmlNode objChild = objXmlDocument.SelectSingleNode("/chummer/cyberwares/cyberware[name = \"" + objPlugin.Name + "\"]");

						if (objChild != null)
						{
							_objImprovementManager.RemoveImprovements(Improvement.ImprovementSource.Cyberware, objPlugin.InternalId);
							if (objChild["bonus"] != null)
								_objImprovementManager.CreateImprovements(Improvement.ImprovementSource.Cyberware, objPlugin.InternalId, objChild["bonus"], false, objPlugin.Rating, objPlugin.DisplayNameShort);
						}
					}
				}
				else
				{
					objXmlDocument = XmlManager.Instance.Load("bioware.xml");
					XmlNode objNode = objXmlDocument.SelectSingleNode("/chummer/biowares/bioware[name = \"" + objCyberware.Name + "\"]");

					if (objNode != null)
					{
						_objImprovementManager.RemoveImprovements(Improvement.ImprovementSource.Bioware, objCyberware.InternalId);
						if (objNode["bonus"] != null)
							_objImprovementManager.CreateImprovements(Improvement.ImprovementSource.Bioware, objCyberware.InternalId, objNode["bonus"], false, objCyberware.Rating, objCyberware.DisplayNameShort);
						if (_objImprovementManager.SelectedValue != "")
							objCyberware.Location = _objImprovementManager.SelectedValue;

						foreach (TreeNode objParentNode in treCyberware.Nodes)
						{
							foreach (TreeNode objChildNode in objParentNode.Nodes)
							{
								if (objChildNode.Tag.ToString() == objCyberware.InternalId)
								{
									objChildNode.Text = objCyberware.DisplayName;
									break;
								}
							}
						}
					}

					foreach (Cyberware objPlugin in objCyberware.Children)
					{
						XmlNode objChild = objXmlDocument.SelectSingleNode("/chummer/biowares/bioware[name = \"" + objPlugin.Name + "\"]");

						if (objChild != null)
						{
							_objImprovementManager.RemoveImprovements(Improvement.ImprovementSource.Bioware, objPlugin.InternalId);
							if (objChild["bonus"] != null)
								_objImprovementManager.CreateImprovements(Improvement.ImprovementSource.Bioware, objPlugin.InternalId, objChild["bonus"], false, objPlugin.Rating, objPlugin.DisplayNameShort);
						}
					}
				}
			}

			// Refresh Armors.
			foreach (Armor objArmor in _objCharacter.Armor)
			{
				objXmlDocument = XmlManager.Instance.Load("armor.xml");
				XmlNode objNode = objXmlDocument.SelectSingleNode("/chummer/armors/armor[name = \"" + objArmor.Name + "\"]");

				if (objNode != null)
				{
					_objImprovementManager.RemoveImprovements(Improvement.ImprovementSource.Armor, objArmor.InternalId);
					if (objNode["bonus"] != null)
						_objImprovementManager.CreateImprovements(Improvement.ImprovementSource.Armor, objArmor.InternalId, objNode["bonus"], false, 1, objArmor.DisplayNameShort);
					if (_objImprovementManager.SelectedValue != "")
						objArmor.Extra = _objImprovementManager.SelectedValue;

					foreach (TreeNode objParentNode in treArmor.Nodes)
					{
						foreach (TreeNode objChildNode in objParentNode.Nodes)
						{
							if (objChildNode.Tag.ToString() == objArmor.InternalId)
							{
								objChildNode.Text = objArmor.DisplayName;
								break;
							}
						}
					}
				}

				foreach (ArmorMod objMod in objArmor.ArmorMods)
				{
					XmlNode objChild = objXmlDocument.SelectSingleNode("/chummer/mods/mod[name = \"" + objMod.Name + "\"]");

					if (objChild != null)
					{
						_objImprovementManager.RemoveImprovements(Improvement.ImprovementSource.ArmorMod, objMod.InternalId);
						if (objChild["bonus"] != null)
							_objImprovementManager.CreateImprovements(Improvement.ImprovementSource.ArmorMod, objMod.InternalId, objChild["bonus"], false, 1, objMod.DisplayNameShort);
						if (_objImprovementManager.SelectedValue != "")
							objMod.Extra = _objImprovementManager.SelectedValue;

						foreach (TreeNode objParentNode in treArmor.Nodes)
						{
							foreach (TreeNode objChildNode in objParentNode.Nodes)
							{
								foreach (TreeNode objPluginNode in objChildNode.Nodes)
								{
									if (objPluginNode.Tag.ToString() == objMod.InternalId)
									{
										objPluginNode.Text = objMod.DisplayName;
										break;
									}
								}
							}
						}
					}
				}

				foreach (Gear objGear in objArmor.Gear)
				{
					objXmlDocument = XmlManager.Instance.Load("gear.xml");
					XmlNode objChild = objXmlDocument.SelectSingleNode("/chummer/gears/gear[name = \"" + objGear.Name + "\" and category = \"" + objGear.Category + "\"]");

					if (objChild != null)
					{
						_objImprovementManager.RemoveImprovements(Improvement.ImprovementSource.Gear, objGear.InternalId);
						if (objChild["bonus"] != null)
							_objImprovementManager.CreateImprovements(Improvement.ImprovementSource.Gear, objGear.InternalId, objChild["bonus"], false, objGear.Rating, objGear.DisplayNameShort);
						if (_objImprovementManager.SelectedValue != "")
							objGear.Extra = _objImprovementManager.SelectedValue;

						foreach (TreeNode objParentNode in treArmor.Nodes)
						{
							foreach (TreeNode objChildNode in objParentNode.Nodes)
							{
								foreach (TreeNode objPluginNode in objChildNode.Nodes)
								{
									if (objPluginNode.Tag.ToString() == objGear.InternalId)
									{
										objPluginNode.Text = objGear.DisplayName;
										break;
									}
								}
							}
						}
					}
				}
			}

			// Refresh Gear.
			objXmlDocument = XmlManager.Instance.Load("gear.xml");
			foreach (Gear objGear in _objCharacter.Gear)
			{
				string strSelected = objGear.Extra;
				XmlNode objNode = objXmlDocument.SelectSingleNode("/chummer/gears/gear[name = \"" + objGear.Name + "\" and category = \"" + objGear.Category + "\"]");

				if (objNode != null)
				{
					_objImprovementManager.RemoveImprovements(Improvement.ImprovementSource.Gear, objGear.InternalId);
					if (objNode["bonus"] != null)
					{
						_objImprovementManager.ForcedValue = strSelected;
						_objImprovementManager.CreateImprovements(Improvement.ImprovementSource.Gear, objGear.InternalId, objNode["bonus"], false, objGear.Rating, objGear.DisplayNameShort);
						if (_objImprovementManager.SelectedValue != "")
							objGear.Extra = _objImprovementManager.SelectedValue;

						foreach (TreeNode objParentNode in treGear.Nodes)
						{
							foreach (TreeNode objChildNode in objParentNode.Nodes)
							{
								if (objChildNode.Tag.ToString() == objGear.InternalId)
								{
									objChildNode.Text = objGear.DisplayName;
									break;
								}
							}
						}
					}
				}

				foreach (Gear objPlugin in objGear.Children)
				{
					string strPluginSelected = objPlugin.Extra;
					XmlNode objChild = objXmlDocument.SelectSingleNode("/chummer/gears/gear[name = \"" + objPlugin.Name + "\" and category = \"" + objPlugin.Category + "\"]");

					if (objChild != null)
					{
						_objImprovementManager.RemoveImprovements(Improvement.ImprovementSource.Gear, objPlugin.InternalId);
						if (objChild["bonus"] != null)
						{
							_objImprovementManager.ForcedValue = strPluginSelected;
							_objImprovementManager.CreateImprovements(Improvement.ImprovementSource.Gear, objPlugin.InternalId, objChild["bonus"], false, objPlugin.Rating, objPlugin.DisplayNameShort);
							if (_objImprovementManager.SelectedValue != "")
								objPlugin.Extra = _objImprovementManager.SelectedValue;

							foreach (TreeNode objParentNode in treGear.Nodes)
							{
								foreach (TreeNode objChildNode in objParentNode.Nodes)
								{
									foreach (TreeNode objPluginNode in objChildNode.Nodes)
									{
										if (objPluginNode.Tag.ToString() == objPlugin.InternalId)
										{
											objPluginNode.Text = objPlugin.DisplayName;
											break;
										}
									}
								}
							}
						}
					}

					foreach (Gear objSubPlugin in objPlugin.Children)
					{
						string strSubPluginSelected = objSubPlugin.Extra;
						XmlNode objSubChild = objXmlDocument.SelectSingleNode("/chummer/gears/gear[name = \"" + objSubPlugin.Name + "\" and category = \"" + objSubPlugin.Category + "\"]");

						if (objSubChild != null)
						{
							_objImprovementManager.RemoveImprovements(Improvement.ImprovementSource.Gear, objSubPlugin.InternalId);
							if (objSubChild["bonus"] != null)
							{
								_objImprovementManager.ForcedValue = strSubPluginSelected;
								_objImprovementManager.CreateImprovements(Improvement.ImprovementSource.Gear, objSubPlugin.InternalId, objSubChild["bonus"], false, objSubPlugin.Rating, objSubPlugin.DisplayNameShort);
								if (_objImprovementManager.SelectedValue != "")
									objSubPlugin.Extra = _objImprovementManager.SelectedValue;

								foreach (TreeNode objParentNode in treGear.Nodes)
								{
									foreach (TreeNode objChildNode in objParentNode.Nodes)
									{
										foreach (TreeNode objPluginNode in objChildNode.Nodes)
										{
											foreach (TreeNode objSubPluginNode in objPluginNode.Nodes)
											{
												if (objSubPluginNode.Tag.ToString() == objSubPlugin.InternalId)
												{
													objSubPluginNode.Text = objSubPlugin.DisplayName;
													break;
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}

			_blnReapplyImprovements = false;

			// If the status of any Character Event flags has changed, manually trigger those events.
			if (blnMAGEnabled != _objCharacter.MAGEnabled)
				objCharacter_MAGEnabledChanged(this);
			if (blnRESEnabled != _objCharacter.RESEnabled)
				objCharacter_RESEnabledChanged(this);
			if (blnUneducated != _objCharacter.Uneducated)
				objCharacter_UneducatedChanged(this);
			if (blnUncouth != _objCharacter.Uncouth)
				objCharacter_UncouthChanged(this);
			if (blnInfirm != _objCharacter.Infirm)
				objCharacter_InfirmChanged(this);

			_blnIsDirty = true;
			UpdateWindowTitle();
			UpdateCharacterInfo();
		}

		private void mnuEditCopy_Click(object sender, EventArgs e)
		{
			if (tabCharacterTabs.SelectedTab == tabStreetGear)
			{
				// Lifestyle Tab.
				if (tabStreetGearTabs.SelectedTab == tabLifestyle)
				{
					try
					{
						// Copy the selected Lifestyle.
						Lifestyle objCopyLifestyle = _objFunctions.FindLifestyle(treLifestyles.SelectedNode.Tag.ToString(), _objCharacter.Lifestyles);

						if (objCopyLifestyle == null)
							return;

						MemoryStream objStream = new MemoryStream();
						XmlTextWriter objWriter = new XmlTextWriter(objStream, System.Text.Encoding.Unicode);
						objWriter.Formatting = Formatting.Indented;
						objWriter.Indentation = 1;
						objWriter.IndentChar = '\t';

						objWriter.WriteStartDocument();

						// </characters>
						objWriter.WriteStartElement("character");

						objCopyLifestyle.Save(objWriter);

						// </characters>
						objWriter.WriteEndElement();

						// Finish the document and flush the Writer and Stream.
						objWriter.WriteEndDocument();
						objWriter.Flush();
						objStream.Flush();

						// Read the stream.
						StreamReader objReader = new StreamReader(objStream);
						objStream.Position = 0;
						XmlDocument objCharacterXML = new XmlDocument();

						// Put the stream into an XmlDocument.
						string strXML = objReader.ReadToEnd();
						objCharacterXML.LoadXml(strXML);

						objWriter.Close();
						objStream.Close();

						GlobalOptions.Instance.Clipboard = objCharacterXML;
						GlobalOptions.Instance.ClipboardContentType = ClipboardContentType.Lifestyle;
						//Clipboard.SetText(objCharacterXML.OuterXml);
					}
					catch
					{
					}
				}

				// Armor Tab.
				if (tabStreetGearTabs.SelectedTab == tabArmor)
				{
					try
					{
						// Copy the selected Armor.
						Armor objCopyArmor = _objFunctions.FindArmor(treArmor.SelectedNode.Tag.ToString(), _objCharacter.Armor);

						if (objCopyArmor != null)
						{
							MemoryStream objStream = new MemoryStream();
							XmlTextWriter objWriter = new XmlTextWriter(objStream, System.Text.Encoding.Unicode);
							objWriter.Formatting = Formatting.Indented;
							objWriter.Indentation = 1;
							objWriter.IndentChar = '\t';

							objWriter.WriteStartDocument();

							// </characters>
							objWriter.WriteStartElement("character");

							objCopyArmor.Save(objWriter);

							// </characters>
							objWriter.WriteEndElement();

							// Finish the document and flush the Writer and Stream.
							objWriter.WriteEndDocument();
							objWriter.Flush();
							objStream.Flush();

							// Read the stream.
							StreamReader objReader = new StreamReader(objStream);
							objStream.Position = 0;
							XmlDocument objCharacterXML = new XmlDocument();

							// Put the stream into an XmlDocument.
							string strXML = objReader.ReadToEnd();
							objCharacterXML.LoadXml(strXML);

							objWriter.Close();
							objStream.Close();

							GlobalOptions.Instance.Clipboard = objCharacterXML;
							GlobalOptions.Instance.ClipboardContentType = ClipboardContentType.Armor;

							RefreshPasteStatus();
							return;
						}

						// Attempt to copy Gear.
						Gear objCopyGear = _objFunctions.FindArmorGear(treArmor.SelectedNode.Tag.ToString(), _objCharacter.Armor, out objCopyArmor);

						if (objCopyGear != null)
						{
							MemoryStream objStream = new MemoryStream();
							XmlTextWriter objWriter = new XmlTextWriter(objStream, System.Text.Encoding.Unicode);
							objWriter.Formatting = Formatting.Indented;
							objWriter.Indentation = 1;
							objWriter.IndentChar = '\t';

							objWriter.WriteStartDocument();

							// </characters>
							objWriter.WriteStartElement("character");

							if (objCopyGear.GetType() == typeof(Commlink))
							{
								Commlink objCommlink = (Commlink)objCopyGear;
								objCommlink.Save(objWriter);
								GlobalOptions.Instance.ClipboardContentType = ClipboardContentType.Commlink;
							}
							else if (objCopyGear.GetType() == typeof(OperatingSystem))
							{
								OperatingSystem objOS = (OperatingSystem)objCopyGear;
								objOS.Save(objWriter);
								GlobalOptions.Instance.ClipboardContentType = ClipboardContentType.OperatingSystem;
							}
							else
							{
								objCopyGear.Save(objWriter);
								GlobalOptions.Instance.ClipboardContentType = ClipboardContentType.Gear;
							}

							if (objCopyGear.WeaponID != Guid.Empty.ToString())
							{
								// Copy any Weapon that comes with the Gear.
								Weapon objCopyWeapon = _objFunctions.FindWeapon(objCopyGear.WeaponID, _objCharacter.Weapons);
								objCopyWeapon.Save(objWriter);
							}

							// </characters>
							objWriter.WriteEndElement();

							// Finish the document and flush the Writer and Stream.
							objWriter.WriteEndDocument();
							objWriter.Flush();
							objStream.Flush();

							// Read the stream.
							StreamReader objReader = new StreamReader(objStream);
							objStream.Position = 0;
							XmlDocument objCharacterXML = new XmlDocument();

							// Put the stream into an XmlDocument.
							string strXML = objReader.ReadToEnd();
							objCharacterXML.LoadXml(strXML);

							objWriter.Close();
							objStream.Close();

							GlobalOptions.Instance.Clipboard = objCharacterXML;

							RefreshPasteStatus();
							return;
						}
					}
					catch
					{
					}
				}

				// Weapons Tab.
				if (tabStreetGearTabs.SelectedTab == tabWeapons)
				{
					try
					{
						// Copy the selected Weapon.
						Weapon objCopyWeapon = _objFunctions.FindWeapon(treWeapons.SelectedNode.Tag.ToString(), _objCharacter.Weapons);

						if (objCopyWeapon != null)
						{
							// Do not let the user copy Gear or Cyberware Weapons.
							if (objCopyWeapon.Category == "Gear" || objCopyWeapon.Category.StartsWith("Cyberware"))
								return;

							MemoryStream objStream = new MemoryStream();
							XmlTextWriter objWriter = new XmlTextWriter(objStream, System.Text.Encoding.Unicode);
							objWriter.Formatting = Formatting.Indented;
							objWriter.Indentation = 1;
							objWriter.IndentChar = '\t';

							objWriter.WriteStartDocument();

							// </characters>
							objWriter.WriteStartElement("character");

							objCopyWeapon.Save(objWriter);

							// </characters>
							objWriter.WriteEndElement();

							// Finish the document and flush the Writer and Stream.
							objWriter.WriteEndDocument();
							objWriter.Flush();
							objStream.Flush();

							// Read the stream.
							StreamReader objReader = new StreamReader(objStream);
							objStream.Position = 0;
							XmlDocument objCharacterXML = new XmlDocument();

							// Put the stream into an XmlDocument.
							string strXML = objReader.ReadToEnd();
							objCharacterXML.LoadXml(strXML);

							objWriter.Close();
							objStream.Close();

							GlobalOptions.Instance.Clipboard = objCharacterXML;
							GlobalOptions.Instance.ClipboardContentType = ClipboardContentType.Weapon;

							RefreshPasteStatus();
							return;
						}

						WeaponAccessory objAccessory = new WeaponAccessory(_objCharacter);
						Gear objCopyGear = _objFunctions.FindWeaponGear(treWeapons.SelectedNode.Tag.ToString(), _objCharacter.Weapons, out objAccessory);

						if (objCopyGear != null)
						{
							MemoryStream objStream = new MemoryStream();
							XmlTextWriter objWriter = new XmlTextWriter(objStream, System.Text.Encoding.Unicode);
							objWriter.Formatting = Formatting.Indented;
							objWriter.Indentation = 1;
							objWriter.IndentChar = '\t';

							objWriter.WriteStartDocument();

							// </characters>
							objWriter.WriteStartElement("character");

							if (objCopyGear.GetType() == typeof(Commlink))
							{
								Commlink objCommlink = (Commlink)objCopyGear;
								objCommlink.Save(objWriter);
								GlobalOptions.Instance.ClipboardContentType = ClipboardContentType.Commlink;
							}
							else if (objCopyGear.GetType() == typeof(OperatingSystem))
							{
								OperatingSystem objOS = (OperatingSystem)objCopyGear;
								objOS.Save(objWriter);
								GlobalOptions.Instance.ClipboardContentType = ClipboardContentType.OperatingSystem;
							}
							else
							{
								objCopyGear.Save(objWriter);
								GlobalOptions.Instance.ClipboardContentType = ClipboardContentType.Gear;
							}

							if (objCopyGear.WeaponID != Guid.Empty.ToString())
							{
								// Copy any Weapon that comes with the Gear.
								Weapon objCopyGearWeapon = _objFunctions.FindWeapon(objCopyGear.WeaponID, _objCharacter.Weapons);
								objCopyGearWeapon.Save(objWriter);
							}

							// </characters>
							objWriter.WriteEndElement();

							// Finish the document and flush the Writer and Stream.
							objWriter.WriteEndDocument();
							objWriter.Flush();
							objStream.Flush();

							// Read the stream.
							StreamReader objReader = new StreamReader(objStream);
							objStream.Position = 0;
							XmlDocument objCharacterXML = new XmlDocument();

							// Put the stream into an XmlDocument.
							string strXML = objReader.ReadToEnd();
							objCharacterXML.LoadXml(strXML);

							objWriter.Close();
							objStream.Close();

							GlobalOptions.Instance.Clipboard = objCharacterXML;

							RefreshPasteStatus();
							return;
						}
					}
					catch
					{
					}
				}

				// Gear Tab.
				if (tabStreetGearTabs.SelectedTab == tabGear)
				{
					try
					{
						// Copy the selected Gear.
						Gear objCopyGear = _objFunctions.FindGear(treGear.SelectedNode.Tag.ToString(), _objCharacter.Gear);

						if (objCopyGear == null)
							return;

						MemoryStream objStream = new MemoryStream();
						XmlTextWriter objWriter = new XmlTextWriter(objStream, System.Text.Encoding.Unicode);
						objWriter.Formatting = Formatting.Indented;
						objWriter.Indentation = 1;
						objWriter.IndentChar = '\t';

						objWriter.WriteStartDocument();

						// </characters>
						objWriter.WriteStartElement("character");

						if (objCopyGear.GetType() == typeof(Commlink))
						{
							Commlink objCommlink = (Commlink)objCopyGear;
							objCommlink.Save(objWriter);
							GlobalOptions.Instance.ClipboardContentType = ClipboardContentType.Commlink;
						}
						else if (objCopyGear.GetType() == typeof(OperatingSystem))
						{
							OperatingSystem objOS = (OperatingSystem)objCopyGear;
							objOS.Save(objWriter);
							GlobalOptions.Instance.ClipboardContentType = ClipboardContentType.OperatingSystem;
						}
						else
						{
							objCopyGear.Save(objWriter);
							GlobalOptions.Instance.ClipboardContentType = ClipboardContentType.Gear;
						}

						if (objCopyGear.WeaponID != Guid.Empty.ToString())
						{
							// Copy any Weapon that comes with the Gear.
							Weapon objCopyWeapon = _objFunctions.FindWeapon(objCopyGear.WeaponID, _objCharacter.Weapons);
							objCopyWeapon.Save(objWriter);
						}

						// </characters>
						objWriter.WriteEndElement();

						// Finish the document and flush the Writer and Stream.
						objWriter.WriteEndDocument();
						objWriter.Flush();
						objStream.Flush();

						// Read the stream.
						StreamReader objReader = new StreamReader(objStream);
						objStream.Position = 0;
						XmlDocument objCharacterXML = new XmlDocument();

						// Put the stream into an XmlDocument.
						string strXML = objReader.ReadToEnd();
						objCharacterXML.LoadXml(strXML);

						objWriter.Close();
						objStream.Close();

						GlobalOptions.Instance.Clipboard = objCharacterXML;
						//Clipboard.SetText(objCharacterXML.OuterXml);
					}
					catch
					{
					}
				}
			}

			// Vehicles Tab.
			if (tabCharacterTabs.SelectedTab == tabVehicles)
			{
				try
				{
					if (treVehicles.SelectedNode.Level == 1)
					{
						// Copy the selected Vehicle.
						Vehicle objCopyVehicle = _objFunctions.FindVehicle(treVehicles.SelectedNode.Tag.ToString(), _objCharacter.Vehicles);

						MemoryStream objStream = new MemoryStream();
						XmlTextWriter objWriter = new XmlTextWriter(objStream, System.Text.Encoding.Unicode);
						objWriter.Formatting = Formatting.Indented;
						objWriter.Indentation = 1;
						objWriter.IndentChar = '\t';

						objWriter.WriteStartDocument();

						// </characters>
						objWriter.WriteStartElement("character");

						objCopyVehicle.Save(objWriter);

						// </characters>
						objWriter.WriteEndElement();

						// Finish the document and flush the Writer and Stream.
						objWriter.WriteEndDocument();
						objWriter.Flush();
						objStream.Flush();

						// Read the stream.
						StreamReader objReader = new StreamReader(objStream);
						objStream.Position = 0;
						XmlDocument objCharacterXML = new XmlDocument();

						// Put the stream into an XmlDocument.
						string strXML = objReader.ReadToEnd();
						objCharacterXML.LoadXml(strXML);

						objWriter.Close();
						objStream.Close();

						GlobalOptions.Instance.Clipboard = objCharacterXML;
						GlobalOptions.Instance.ClipboardContentType = ClipboardContentType.Vehicle;
						//Clipboard.SetText(objCharacterXML.OuterXml);
					}
					else
					{
						Vehicle objVehicle = new Vehicle(_objCharacter);
						Gear objCopyGear = _objFunctions.FindVehicleGear(treVehicles.SelectedNode.Tag.ToString(), _objCharacter.Vehicles, out objVehicle);

						if (objCopyGear != null)
						{
							MemoryStream objStream = new MemoryStream();
							XmlTextWriter objWriter = new XmlTextWriter(objStream, System.Text.Encoding.Unicode);
							objWriter.Formatting = Formatting.Indented;
							objWriter.Indentation = 1;
							objWriter.IndentChar = '\t';

							objWriter.WriteStartDocument();

							// </characters>
							objWriter.WriteStartElement("character");

							if (objCopyGear.GetType() == typeof(Commlink))
							{
								Commlink objCommlink = (Commlink)objCopyGear;
								objCommlink.Save(objWriter);
								GlobalOptions.Instance.ClipboardContentType = ClipboardContentType.Commlink;
							}
							else if (objCopyGear.GetType() == typeof(OperatingSystem))
							{
								OperatingSystem objOS = (OperatingSystem)objCopyGear;
								objOS.Save(objWriter);
								GlobalOptions.Instance.ClipboardContentType = ClipboardContentType.OperatingSystem;
							}
							else
							{
								objCopyGear.Save(objWriter);
								GlobalOptions.Instance.ClipboardContentType = ClipboardContentType.Gear;
							}

							if (objCopyGear.WeaponID != Guid.Empty.ToString())
							{
								// Copy any Weapon that comes with the Gear.
								Weapon objCopyWeapon = _objFunctions.FindWeapon(objCopyGear.WeaponID, _objCharacter.Weapons);
								objCopyWeapon.Save(objWriter);
							}

							// </characters>
							objWriter.WriteEndElement();

							// Finish the document and flush the Writer and Stream.
							objWriter.WriteEndDocument();
							objWriter.Flush();
							objStream.Flush();

							// Read the stream.
							StreamReader objReader = new StreamReader(objStream);
							objStream.Position = 0;
							XmlDocument objCharacterXML = new XmlDocument();

							// Put the stream into an XmlDocument.
							string strXML = objReader.ReadToEnd();
							objCharacterXML.LoadXml(strXML);

							objWriter.Close();
							objStream.Close();

							GlobalOptions.Instance.Clipboard = objCharacterXML;

							RefreshPasteStatus();
							return;
						}

						foreach (Vehicle objCharacterVehicle in _objCharacter.Vehicles)
						{
							foreach (VehicleMod objMod in objCharacterVehicle.Mods)
							{
								Weapon objCopyWeapon = _objFunctions.FindWeapon(treVehicles.SelectedNode.Tag.ToString(), objMod.Weapons);
								if (objCopyWeapon != null)
								{
									// Do not let the user copy Gear or Cyberware Weapons.
									if (objCopyWeapon.Category == "Gear" || objCopyWeapon.Category.StartsWith("Cyberware"))
										return;

									MemoryStream objStream = new MemoryStream();
									XmlTextWriter objWriter = new XmlTextWriter(objStream, System.Text.Encoding.Unicode);
									objWriter.Formatting = Formatting.Indented;
									objWriter.Indentation = 1;
									objWriter.IndentChar = '\t';

									objWriter.WriteStartDocument();

									// </characters>
									objWriter.WriteStartElement("character");

									objCopyWeapon.Save(objWriter);

									// </characters>
									objWriter.WriteEndElement();

									// Finish the document and flush the Writer and Stream.
									objWriter.WriteEndDocument();
									objWriter.Flush();
									objStream.Flush();

									// Read the stream.
									StreamReader objReader = new StreamReader(objStream);
									objStream.Position = 0;
									XmlDocument objCharacterXML = new XmlDocument();

									// Put the stream into an XmlDocument.
									string strXML = objReader.ReadToEnd();
									objCharacterXML.LoadXml(strXML);

									objWriter.Close();
									objStream.Close();

									GlobalOptions.Instance.Clipboard = objCharacterXML;
									GlobalOptions.Instance.ClipboardContentType = ClipboardContentType.Weapon;

									RefreshPasteStatus();
									return;
								}
							}
						}
					}
				}
				catch
				{
				}
			}
			RefreshPasteStatus();
		}

		private void mnuEditPaste_Click(object sender, EventArgs e)
		{
			if (tabCharacterTabs.SelectedTab == tabStreetGear)
			{
				// Lifestyle Tab.
				if (tabStreetGearTabs.SelectedTab == tabLifestyle)
				{
					// Paste Lifestyle.
					Lifestyle objLifestyle = new Lifestyle(_objCharacter);
					XmlNode objXmlNode = GlobalOptions.Instance.Clipboard.SelectSingleNode("/character/lifestyle");
					if (objXmlNode != null)
					{
						objLifestyle.Load(objXmlNode, true);
						// Reset the number of months back to 1 since 0 isn't valid in Create Mode.
						objLifestyle.Months = 1;

						_objCharacter.Lifestyles.Add(objLifestyle);

						TreeNode objLifestyleNode = new TreeNode();
						objLifestyleNode.Text = objLifestyle.DisplayName;
						objLifestyleNode.Tag = objLifestyle.InternalId;
						if (objLifestyle.Comforts != "")
							objLifestyleNode.ContextMenuStrip = cmsAdvancedLifestyle;
						else
							objLifestyleNode.ContextMenuStrip = cmsLifestyleNotes;
						if (objLifestyle.Notes != string.Empty)
							objLifestyleNode.ForeColor = Color.SaddleBrown;
						objLifestyleNode.ToolTipText = objLifestyle.Notes;
						treLifestyles.Nodes[0].Nodes.Add(objLifestyleNode);

						UpdateCharacterInfo();
						_blnIsDirty = true;
						UpdateWindowTitle();
						return;
					}
				}

				// Armor Tab.
				if (tabStreetGearTabs.SelectedTab == tabArmor)
				{
					// Paste Armor.
					Armor objArmor = new Armor(_objCharacter);
					XmlNode objXmlNode = GlobalOptions.Instance.Clipboard.SelectSingleNode("/character/armor");
					if (objXmlNode != null)
					{
						objArmor.Load(objXmlNode, true);

						_objCharacter.Armor.Add(objArmor);

						_objFunctions.CreateArmorTreeNode(objArmor, treArmor, cmsArmor, cmsArmorMod, cmsArmorGear);

						UpdateCharacterInfo();
						_blnIsDirty = true;
						UpdateWindowTitle();
						return;
					}

					// Paste Gear.
					Gear objGear = new Gear(_objCharacter);
					objXmlNode = GlobalOptions.Instance.Clipboard.SelectSingleNode("/character/gear");

					if (objXmlNode != null)
					{
						switch (objXmlNode["category"].InnerText)
						{
							case "Commlink":
							case "Commlink Upgrade":
								Commlink objCommlink = new Commlink(_objCharacter);
								objCommlink.Load(objXmlNode, true);
								objGear = objCommlink;
								break;
							case "Commlink Operating System":
							case "Commlink Operating System Upgrade":
								OperatingSystem objOperatingSystem = new OperatingSystem(_objCharacter);
								objOperatingSystem.Load(objXmlNode, true);
								objGear = objOperatingSystem;
								break;
							default:
								Gear objNewGear = new Gear(_objCharacter);
								objNewGear.Load(objXmlNode, true);
								objGear = objNewGear;
								break;
						}

						foreach (Armor objCharacterArmor in _objCharacter.Armor)
						{
							if (objCharacterArmor.InternalId == treArmor.SelectedNode.Tag.ToString())
							{
								objCharacterArmor.Gear.Add(objGear);
								TreeNode objNode = new TreeNode();
								objNode.Text = objGear.DisplayName;
								objNode.Tag = objGear.InternalId;
								objNode.ContextMenuStrip = cmsArmorGear;

								_objFunctions.BuildGearTree(objGear, objNode, cmsArmorGear);

								treArmor.SelectedNode.Nodes.Add(objNode);
								treArmor.SelectedNode.Expand();
							}
						}

						// Add any Weapons that come with the Gear.
						objXmlNode = GlobalOptions.Instance.Clipboard.SelectSingleNode("/character/weapon");
						if (objXmlNode != null)
						{
							Weapon objWeapon = new Weapon(_objCharacter);
							objWeapon.Load(objXmlNode, true);
							_objCharacter.Weapons.Add(objWeapon);
							objGear.WeaponID = objWeapon.InternalId;
							_objFunctions.CreateWeaponTreeNode(objWeapon, treWeapons.Nodes[0], cmsWeapon, cmsWeaponMod, cmsWeaponAccessory, cmsWeaponAccessoryGear);
						}

						UpdateCharacterInfo();
						_blnIsDirty = true;
						UpdateWindowTitle();
						return;
					}
				}

				// Weapons Tab.
				if (tabStreetGearTabs.SelectedTab == tabWeapons)
				{
					// Paste Gear into a Weapon Accessory.
					Gear objGear = new Gear(_objCharacter);
					XmlNode objXmlNode = GlobalOptions.Instance.Clipboard.SelectSingleNode("/character/gear");
					if (objXmlNode != null)
					{
						switch (objXmlNode["category"].InnerText)
						{
							case "Commlink":
							case "Commlink Upgrade":
								Commlink objCommlink = new Commlink(_objCharacter);
								objCommlink.Load(objXmlNode, true);
								objGear = objCommlink;
								break;
							case "Commlink Operating System":
							case "Commlink Operating System Upgrade":
								OperatingSystem objOperatingSystem = new OperatingSystem(_objCharacter);
								objOperatingSystem.Load(objXmlNode, true);
								objGear = objOperatingSystem;
								break;
							default:
								Gear objNewGear = new Gear(_objCharacter);
								objNewGear.Load(objXmlNode, true);
								objGear = objNewGear;
								break;
						}

						objGear.Parent = null;

						// Make sure that a Weapon Accessory is selected and that it allows Gear of the item's Category.
						WeaponAccessory objAccessory = _objFunctions.FindWeaponAccessory(treWeapons.SelectedNode.Tag.ToString(), _objCharacter.Weapons);
						bool blnAllowPaste = false;
						if (objAccessory.AllowGear != null)
						{
							foreach (XmlNode objAllowed in objAccessory.AllowGear.SelectNodes("gearcategory"))
							{
								if (objAllowed.InnerText == objGear.Category)
								{
									blnAllowPaste = true;
									break;
								}
							}
						}
						if (blnAllowPaste)
						{
							objAccessory.Gear.Add(objGear);
							TreeNode objNode = new TreeNode();
							objNode.Text = objGear.DisplayName;
							objNode.Tag = objGear.InternalId;
							objNode.ContextMenuStrip = cmsWeaponAccessoryGear;

							_objFunctions.BuildGearTree(objGear, objNode, cmsWeaponAccessoryGear);

							treWeapons.SelectedNode.Nodes.Add(objNode);
							treWeapons.SelectedNode.Expand();

							// Add any Weapons that come with the Gear.
							objXmlNode = GlobalOptions.Instance.Clipboard.SelectSingleNode("/character/weapon");
							if (objXmlNode != null)
							{
								Weapon objGearWeapon = new Weapon(_objCharacter);
								objGearWeapon.Load(objXmlNode, true);
								_objCharacter.Weapons.Add(objGearWeapon);
								objGear.WeaponID = objGearWeapon.InternalId;
								_objFunctions.CreateWeaponTreeNode(objGearWeapon, treWeapons.Nodes[0], cmsWeapon, cmsWeaponMod, cmsWeaponAccessory, cmsWeaponAccessoryGear);
							}

							UpdateCharacterInfo();
							_blnIsDirty = true;
							UpdateWindowTitle();
							return;
						}
					}

					// Paste Weapon.
					Weapon objWeapon = new Weapon(_objCharacter);
					objXmlNode = GlobalOptions.Instance.Clipboard.SelectSingleNode("/character/weapon");
					if (objXmlNode != null)
					{
						objWeapon.Load(objXmlNode, true);
						objWeapon.VehicleMounted = false;

						_objCharacter.Weapons.Add(objWeapon);

						_objFunctions.CreateWeaponTreeNode(objWeapon, treWeapons.Nodes[0], cmsWeapon, cmsWeaponMod, cmsWeaponAccessory, cmsWeaponAccessoryGear);

						UpdateCharacterInfo();
						_blnIsDirty = true;
						UpdateWindowTitle();
						return;
					}
				}

				// Gear Tab.
				if (tabStreetGearTabs.SelectedTab == tabGear)
				{
					// Paste Gear.
					Gear objGear = new Gear(_objCharacter);
					XmlNode objXmlNode = GlobalOptions.Instance.Clipboard.SelectSingleNode("/character/gear");
					if (objXmlNode != null)
					{
						switch (objXmlNode["category"].InnerText)
						{
							case "Commlink":
							case "Commlink Upgrade":
								Commlink objCommlink = new Commlink(_objCharacter);
								objCommlink.Load(objXmlNode, true);
								_objCharacter.Gear.Add(objCommlink);
								objGear = objCommlink;
								break;
							case "Commlink Operating System":
							case "Commlink Operating System Upgrade":
								OperatingSystem objOperatingSystem = new OperatingSystem(_objCharacter);
								objOperatingSystem.Load(objXmlNode, true);
								_objCharacter.Gear.Add(objOperatingSystem);
								objGear = objOperatingSystem;
								break;
							default:
								Gear objNewGear = new Gear(_objCharacter);
								objNewGear.Load(objXmlNode, true);
								_objCharacter.Gear.Add(objNewGear);
								objGear = objNewGear;
								break;
						}

						objGear.Parent = null;

						TreeNode objNode = new TreeNode();
						objNode.Text = objGear.DisplayName;
						objNode.Tag = objGear.InternalId;
						if (objGear.Notes != string.Empty)
							objNode.ForeColor = Color.SaddleBrown;
						objNode.ToolTipText = objGear.Notes;

						_objFunctions.BuildGearTree(objGear, objNode, cmsGear);

						objNode.ContextMenuStrip = cmsGear;

						TreeNode objParent = new TreeNode();
						if (objGear.Location == "")
							objParent = treGear.Nodes[0];
						else
						{
							foreach (TreeNode objFind in treGear.Nodes)
							{
								if (objFind.Text == objGear.Location)
								{
									objParent = objFind;
									break;
								}
							}
						}
						objParent.Nodes.Add(objNode);
						objParent.Expand();

						// Add any Weapons that come with the Gear.
						objXmlNode = GlobalOptions.Instance.Clipboard.SelectSingleNode("/character/weapon");
						if (objXmlNode != null)
						{
							Weapon objWeapon = new Weapon(_objCharacter);
							objWeapon.Load(objXmlNode, true);
							_objCharacter.Weapons.Add(objWeapon);
							objGear.WeaponID = objWeapon.InternalId;
							_objFunctions.CreateWeaponTreeNode(objWeapon, treWeapons.Nodes[0], cmsWeapon, cmsWeaponMod, cmsWeaponAccessory, cmsWeaponAccessoryGear);
						}

						UpdateCharacterInfo();
						_blnIsDirty = true;
						UpdateWindowTitle();
						return;
					}
				}
			}

			// Vehicles Tab.
			if (tabCharacterTabs.SelectedTab == tabVehicles)
			{
				// Paste Vehicle.
				Vehicle objVehicle = new Vehicle(_objCharacter);
				XmlNode objXmlNode = GlobalOptions.Instance.Clipboard.SelectSingleNode("/character/vehicle");
				if (objXmlNode != null)
				{
					objVehicle.Load(objXmlNode, true);

					_objCharacter.Vehicles.Add(objVehicle);

					_objFunctions.CreateVehicleTreeNode(objVehicle, treVehicles, cmsVehicle, cmsVehicleLocation, cmsVehicleWeapon, cmsVehicleWeaponMod, cmsVehicleWeaponAccessory, cmsVehicleWeaponAccessoryGear, cmsVehicleGear);

					UpdateCharacterInfo();
					_blnIsDirty = true;
					UpdateWindowTitle();
					return;
				}

				// Paste Gear.
				Gear objGear = new Gear(_objCharacter);
				objXmlNode = GlobalOptions.Instance.Clipboard.SelectSingleNode("/character/gear");

				if (objXmlNode != null)
				{
					switch (objXmlNode["category"].InnerText)
					{
						case "Commlink":
						case "Commlink Upgrade":
							Commlink objCommlink = new Commlink(_objCharacter);
							objCommlink.Load(objXmlNode, true);
							objGear = objCommlink;
							break;
						case "Commlink Operating System":
						case "Commlink Operating System Upgrade":
							OperatingSystem objOperatingSystem = new OperatingSystem(_objCharacter);
							objOperatingSystem.Load(objXmlNode, true);
							objGear = objOperatingSystem;
							break;
						default:
							Gear objNewGear = new Gear(_objCharacter);
							objNewGear.Load(objXmlNode, true);
							objGear = objNewGear;
							break;
					}

					// Paste the Gear into a Vehicle.
					foreach (Vehicle objCharacterVehicle in _objCharacter.Vehicles)
					{
						if (objCharacterVehicle.InternalId == treVehicles.SelectedNode.Tag.ToString())
						{
							objCharacterVehicle.Gear.Add(objGear);
							TreeNode objNode = new TreeNode();
							objNode.Text = objGear.DisplayName;
							objNode.Tag = objGear.InternalId;
							objNode.ContextMenuStrip = cmsVehicleGear;
							objVehicle = objCharacterVehicle;

							_objFunctions.BuildGearTree(objGear, objNode, cmsVehicleGear);

							treVehicles.SelectedNode.Nodes.Add(objNode);
							treVehicles.SelectedNode.Expand();
						}
					}

					// Paste the Gear into a Vehicle's Gear.
					Vehicle objTempVehicle = objVehicle;
					Gear objVehicleGear = _objFunctions.FindVehicleGear(treVehicles.SelectedNode.Tag.ToString(), _objCharacter.Vehicles, out objVehicle);
					if (objVehicle == null)
						objVehicle = objTempVehicle;
					if (objVehicleGear != null)
					{
						objVehicleGear.Children.Add(objGear);
						objGear.Parent = objVehicleGear;
						TreeNode objNode = new TreeNode();
						objNode.Text = objGear.DisplayName;
						objNode.Tag = objGear.InternalId;
						objNode.ContextMenuStrip = cmsVehicleGear;

						_objFunctions.BuildGearTree(objGear, objNode, cmsVehicleGear);

						treVehicles.SelectedNode.Nodes.Add(objNode);
						treVehicles.SelectedNode.Expand();
					}

					UpdateCharacterInfo();
					_blnIsDirty = true;
					UpdateWindowTitle();
					return;
				}

				// Paste Weapon.
				Weapon objWeapon = new Weapon(_objCharacter);
				objXmlNode = GlobalOptions.Instance.Clipboard.SelectSingleNode("/character/weapon");
				if (objXmlNode != null)
				{
					objWeapon.Load(objXmlNode, true);
					objWeapon.VehicleMounted = true;

					try
					{
						// Weapons can only be added to Vehicle Mods that support them (Weapon Mounts and Mechanical Arms).
						VehicleMod objMod = new VehicleMod(_objCharacter);
						foreach (Vehicle objCharacterVehicle in _objCharacter.Vehicles)
						{
							foreach (VehicleMod objVehicleMod in objCharacterVehicle.Mods)
							{
								if (objVehicleMod.InternalId == treVehicles.SelectedNode.Tag.ToString())
								{
									if (objVehicleMod.Name.StartsWith("Weapon Mount") || objVehicleMod.Name.StartsWith("Mechanical Arm"))
									{
										objVehicleMod.Weapons.Add(objWeapon);

										_objFunctions.CreateWeaponTreeNode(objWeapon, treVehicles.SelectedNode, cmsVehicleWeapon, cmsVehicleWeaponMod, cmsVehicleWeaponAccessory, null);

										UpdateCharacterInfo();
										_blnIsDirty = true;
										UpdateWindowTitle();
										return;
									}
								}
							}
						}
					}
					catch
					{
					}
				}
			}
		}

		private void tsbCopy_Click(object sender, EventArgs e)
		{
			mnuEditCopy_Click(sender, e);
		}

		private void tsbPaste_Click(object sender, EventArgs e)
		{
			mnuEditPaste_Click(sender, e);
		}

		private void mnuSpecialBPAvailLimit_Click(object sender, EventArgs e)
		{
			frmSelectBP frmPickBP = new frmSelectBP(_objCharacter, true);
			frmPickBP.ShowDialog(this);

			if (frmPickBP.DialogResult == DialogResult.Cancel)
				UpdateCharacterInfo();
		}

		private void mnuSpecialConvertToFreeSprite_Click(object sender, EventArgs e)
		{
			XmlDocument objXmlDocument = XmlManager.Instance.Load("critterpowers.xml");
			XmlNode objXmlPower = objXmlDocument.SelectSingleNode("/chummer/powers/power[name = \"Denial\"]");
			TreeNode objNode = new TreeNode();
			CritterPower objPower = new CritterPower(_objCharacter);
			objPower.Create(objXmlPower, _objCharacter, objNode);
			objPower.CountTowardsLimit = false;
			objNode.ContextMenuStrip = cmsCritterPowers;
			if (objPower.InternalId == Guid.Empty.ToString())
				return;

			_objCharacter.CritterPowers.Add(objPower);

			treCritterPowers.Nodes[0].Nodes.Add(objNode);
			treCritterPowers.Nodes[0].Expand();

			_objCharacter.MetatypeCategory = "Free Sprite";
			mnuSpecialConvertToFreeSprite.Visible = false;

			_objFunctions.SortTree(treCritterPowers);
			UpdateCharacterInfo();

			_blnIsDirty = true;
			UpdateWindowTitle();
		}
		#endregion

		#region Attribute Events
		private void nudBOD_ValueChanged(object sender, EventArgs e)
		{
			// Don't attempt to do anything while the data is still being populated.
			if (_blnLoading)
				return;

			// Verify that the Attribute can be improved within the rules.
			if (!CanImproveAttribute("nudBOD") && nudBOD.Value >= nudBOD.Maximum && !_objCharacter.IgnoreRules)
			{
				try
				{
					nudBOD.Value = nudBOD.Maximum - 1;
					ShowAttributeRule();
				}
				catch
				{
					nudBOD.Value = nudBOD.Minimum;
				}
			}

			// Make sure the number of BP spent on attributes does not exceed half of the BP allowed for character creation.
			if (!_objCharacter.IgnoreRules && !_objOptions.AllowExceedAttributeBP && !_blnFreestyle)
			{
				if (_objCharacter.BuildMethod == CharacterBuildMethod.BP)
				{
					if (CalculatePrimaryAttributeBP() > _objCharacter.BuildPoints / 2)
					{
						try
						{
							nudBOD.Value -= 1;
							ShowAttributeBPRule();
						}
						catch
						{
							nudBOD.Value = nudBOD.Minimum;
						}
					}
				}
				else
				{
					if (!_objOptions.SpecialAttributeKarmaLimit)
					{
						// Include Special Attributes in the max 50% of Karma spent on Attributes limit.
						if (CalculatePrimaryAttributeBP() + CalculateSpecialAttributeBP() > ((_objCharacter.BuildKarma / 2) + (_objCharacter.MetatypeBP * 2)))
						{
							try
							{
								nudBOD.Value -= 1;
								ShowAttributeBPRule();
							}
							catch
							{
								nudBOD.Value = nudBOD.Minimum;
							}
						}
					}
					else
					{
						// Don't include Special Attributes in the max 50% of Karma spent on Attributes limit.
						if (CalculatePrimaryAttributeBP() > ((_objCharacter.BuildKarma / 2) + (_objCharacter.MetatypeBP * 2)))
						{
							try
							{
								nudBOD.Value -= 1;
								ShowAttributeBPRule();
							}
							catch
							{
								nudBOD.Value = nudBOD.Minimum;
							}
						}
					}
				}
			}
			_objCharacter.BOD.Value = Convert.ToInt32(nudBOD.Value);
			UpdateCharacterInfo();

			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void nudAGI_ValueChanged(object sender, EventArgs e)
		{
			// Don't attempt to do anything while the data is still being populated.
			if (_blnLoading)
				return;

			// Verify that the Attribute can be improved within the rules.
			if (!CanImproveAttribute("nudAGI") && nudAGI.Value >= nudAGI.Maximum && !_objCharacter.IgnoreRules)
			{
				try
				{
					nudAGI.Value = nudAGI.Maximum - 1;
					ShowAttributeRule();
				}
				catch
				{
					nudAGI.Value = nudAGI.Minimum;
				}
			}

			// Make sure the number of BP spent on attributes does not exceed half of the BP allowed for character creation.
			if (!_objCharacter.IgnoreRules && !_objOptions.AllowExceedAttributeBP && !_blnFreestyle)
			{
				if (_objCharacter.BuildMethod == CharacterBuildMethod.BP)
				{
					if (CalculatePrimaryAttributeBP() > _objCharacter.BuildPoints / 2)
					{
						try
						{
							nudAGI.Value -= 1;
							ShowAttributeBPRule();
						}
						catch
						{
							nudAGI.Value = nudAGI.Minimum;
						}
					}
				}
				else
				{
					if (!_objOptions.SpecialAttributeKarmaLimit)
					{
						// Include Special Attributes in the max 50% of Karma spent on Attributes limit.
						if (CalculatePrimaryAttributeBP() + CalculateSpecialAttributeBP() > ((_objCharacter.BuildKarma / 2) + (_objCharacter.MetatypeBP * 2)))
						{
							try
							{
								nudAGI.Value -= 1;
								ShowAttributeBPRule();
							}
							catch
							{
								nudAGI.Value = nudAGI.Minimum;
							}
						}
					}
					else
					{
						// Don't include Special Attributes in the max 50% of Karma spent on Attributes limit.
						if (CalculatePrimaryAttributeBP() > ((_objCharacter.BuildKarma / 2) + (_objCharacter.MetatypeBP * 2)))
						{
							try
							{
								nudAGI.Value -= 1;
								ShowAttributeBPRule();
							}
							catch
							{
								nudAGI.Value = nudAGI.Minimum;
							}
						}
					}
				}
			}
			_objCharacter.AGI.Value = Convert.ToInt32(nudAGI.Value);
			UpdateCharacterInfo();

			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void nudREA_ValueChanged(object sender, EventArgs e)
		{
			// Don't attempt to do anything while the data is still being populated.
			if (_blnLoading)
				return;

			// Verify that the Attribute can be improved within the rules.
			if (!CanImproveAttribute("nudREA") && nudREA.Value >= nudREA.Maximum && !_objCharacter.IgnoreRules)
			{
				try
				{
					nudREA.Value = nudREA.Maximum - 1;
					ShowAttributeRule();
				}
				catch
				{
					nudREA.Value = nudREA.Minimum;
				}
			}

			// Make sure the number of BP spent on attributes does not exceed half of the BP allowed for character creation.
			if (!_objCharacter.IgnoreRules && !_objOptions.AllowExceedAttributeBP && !_blnFreestyle)
			{
				if (_objCharacter.BuildMethod == CharacterBuildMethod.BP)
				{
					if (CalculatePrimaryAttributeBP() > _objCharacter.BuildPoints / 2)
					{
						try
						{
							nudREA.Value -= 1;
							ShowAttributeBPRule();
						}
						catch
						{
							nudREA.Value = nudREA.Minimum;
						}
					}
				}
				else
				{
					if (!_objOptions.SpecialAttributeKarmaLimit)
					{
						// Include Special Attributes in the max 50% of Karma spent on Attributes limit.
						if (CalculatePrimaryAttributeBP() + CalculateSpecialAttributeBP() > ((_objCharacter.BuildKarma / 2) + (_objCharacter.MetatypeBP * 2)))
						{
							try
							{
								nudREA.Value -= 1;
								ShowAttributeBPRule();
							}
							catch
							{
								nudREA.Value = nudREA.Minimum;
							}
						}
					}
					else
					{
						// Don't include Special Attributes in the max 50% of Karma spent on Attributes limit.
						if (CalculatePrimaryAttributeBP() > ((_objCharacter.BuildKarma / 2) + (_objCharacter.MetatypeBP * 2)))
						{
							try
							{
								nudREA.Value -= 1;
								ShowAttributeBPRule();
							}
							catch
							{
								nudREA.Value = nudREA.Minimum;
							}
						}
					}
				}
			}
			_objCharacter.REA.Value = Convert.ToInt32(nudREA.Value);
			UpdateCharacterInfo();

			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void nudSTR_ValueChanged(object sender, EventArgs e)
		{
			// Don't attempt to do anything while the data is still being populated.
			if (_blnLoading)
				return;

			// Verify that the Attribute can be improved within the rules.
			if (!CanImproveAttribute("nudSTR") && nudSTR.Value >= nudSTR.Maximum && !_objCharacter.IgnoreRules)
			{
				try
				{
					nudSTR.Value = nudSTR.Maximum - 1;
					ShowAttributeRule();
				}
				catch
				{
					nudSTR.Value = nudSTR.Minimum;
				}
			}

			// Make sure the number of BP spent on attributes does not exceed half of the BP allowed for character creation.
			if (!_objCharacter.IgnoreRules && !_objOptions.AllowExceedAttributeBP && !_blnFreestyle)
			{
				if (_objCharacter.BuildMethod == CharacterBuildMethod.BP)
				{
					if (CalculatePrimaryAttributeBP() > _objCharacter.BuildPoints / 2)
					{
						try
						{
							nudSTR.Value -= 1;
							ShowAttributeBPRule();
						}
						catch
						{
							nudSTR.Value = nudSTR.Minimum;
						}
					}
				}
				else
				{
					if (!_objOptions.SpecialAttributeKarmaLimit)
					{
						// Include Special Attributes in the max 50% of Karma spent on Attributes limit.
						if (CalculatePrimaryAttributeBP() + CalculateSpecialAttributeBP() > ((_objCharacter.BuildKarma / 2) + (_objCharacter.MetatypeBP * 2)))
						{
							try
							{
								nudSTR.Value -= 1;
								ShowAttributeBPRule();
							}
							catch
							{
								nudSTR.Value = nudSTR.Minimum;
							}
						}
					}
					else
					{
						// Don't include Special Attributes in the max 50% of Karma spent on Attributes limit.
						if (CalculatePrimaryAttributeBP() > ((_objCharacter.BuildKarma / 2) + (_objCharacter.MetatypeBP * 2)))
						{
							try
							{
								nudSTR.Value -= 1;
								ShowAttributeBPRule();
							}
							catch
							{
								nudSTR.Value = nudSTR.Minimum;
							}
						}
					}
				}
			}
			_objCharacter.STR.Value = Convert.ToInt32(nudSTR.Value);
			UpdateCharacterInfo();

			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void nudCHA_ValueChanged(object sender, EventArgs e)
		{
			// Don't attempt to do anything while the data is still being populated.
			if (_blnLoading)
				return;

			// Verify that the Attribute can be improved within the rules.
			if (!CanImproveAttribute("nudCHA") && nudCHA.Value >= nudCHA.Maximum && !_objCharacter.IgnoreRules)
			{
				try
				{
					nudCHA.Value = nudCHA.Maximum - 1;
					ShowAttributeRule();
				}
				catch
				{
					nudCHA.Value = nudCHA.Minimum;
				}
			}

			// Make sure the number of BP spent on attributes does not exceed half of the BP allowed for character creation.
			if (!_objCharacter.IgnoreRules && !_objOptions.AllowExceedAttributeBP && !_blnFreestyle)
			{
				if (_objCharacter.BuildMethod == CharacterBuildMethod.BP)
				{
					if (CalculatePrimaryAttributeBP() > _objCharacter.BuildPoints / 2)
					{
						try
						{
							nudCHA.Value -= 1;
							ShowAttributeBPRule();
						}
						catch
						{
							nudCHA.Value = nudCHA.Minimum;
						}
					}
				}
				else
				{
					if (!_objOptions.SpecialAttributeKarmaLimit)
					{
						// Include Special Attributes in the max 50% of Karma spent on Attributes limit.
						if (CalculatePrimaryAttributeBP() + CalculateSpecialAttributeBP() > ((_objCharacter.BuildKarma / 2) + (_objCharacter.MetatypeBP * 2)))
						{
							try
							{
								nudCHA.Value -= 1;
								ShowAttributeBPRule();
							}
							catch
							{
								nudCHA.Value = nudCHA.Minimum;
							}
						}
					}
					else
					{
						// Don't include Special Attributes in the max 50% of Karma spent on Attributes limit.
						if (CalculatePrimaryAttributeBP() > ((_objCharacter.BuildKarma / 2) + (_objCharacter.MetatypeBP * 2)))
						{
							try
							{
								nudCHA.Value -= 1;
								ShowAttributeBPRule();
							}
							catch
							{
								nudCHA.Value = nudCHA.Minimum;
							}
						}
					}
				}
			}
			_objCharacter.CHA.Value = Convert.ToInt32(nudCHA.Value);
			UpdateCharacterInfo();

			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void nudINT_ValueChanged(object sender, EventArgs e)
		{
			// Don't attempt to do anything while the data is still being populated.
			if (_blnLoading)
				return;

			// Verify that the Attribute can be improved within the rules.
			if (!CanImproveAttribute("nudINT") && nudINT.Value >= nudINT.Maximum && !_objCharacter.IgnoreRules)
			{
				try
				{
					nudINT.Value = nudINT.Maximum - 1;
					ShowAttributeRule();
				}
				catch
				{
					nudINT.Value = nudINT.Minimum;
				}
			}

			// Make sure the number of BP spent on attributes does not exceed half of the BP allowed for character creation.
			if (!_objCharacter.IgnoreRules && !_objOptions.AllowExceedAttributeBP && !_blnFreestyle)
			{
				if (_objCharacter.BuildMethod == CharacterBuildMethod.BP)
				{
					if (CalculatePrimaryAttributeBP() > _objCharacter.BuildPoints / 2)
					{
						try
						{
							nudINT.Value -= 1;
							ShowAttributeBPRule();
						}
						catch
						{
							nudINT.Value = nudINT.Minimum;
						}
					}
				}
				else
				{
					if (!_objOptions.SpecialAttributeKarmaLimit)
					{
						// Include Special Attributes in the max 50% of Karma spent on Attributes limit.
						if (CalculatePrimaryAttributeBP() + CalculateSpecialAttributeBP() > ((_objCharacter.BuildKarma / 2) + (_objCharacter.MetatypeBP * 2)))
						{
							try
							{
								nudINT.Value -= 1;
								ShowAttributeBPRule();
							}
							catch
							{
								nudINT.Value = nudINT.Minimum;
							}
						}
					}
					else
					{
						// Don't include Special Attributes in the max 50% of Karma spent on Attributes limit.
						if (CalculatePrimaryAttributeBP() > ((_objCharacter.BuildKarma / 2) + (_objCharacter.MetatypeBP * 2)))
						{
							try
							{
								nudINT.Value -= 1;
								ShowAttributeBPRule();
							}
							catch
							{
								nudINT.Value = nudINT.Minimum;
							}
						}
					}
				}
			}
			_objCharacter.INT.Value = Convert.ToInt32(nudINT.Value);
			UpdateCharacterInfo();

			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void nudLOG_ValueChanged(object sender, EventArgs e)
		{
			// Don't attempt to do anything while the data is still being populated.
			if (_blnLoading)
				return;

			// Verify that the Attribute can be improved within the rules.
			if (!CanImproveAttribute("nudLOG") && nudLOG.Value >= nudLOG.Maximum && !_objCharacter.IgnoreRules)
			{
				try
				{
					nudLOG.Value = nudLOG.Maximum - 1;
					ShowAttributeRule();
				}
				catch
				{
					nudLOG.Value = nudLOG.Minimum;
				}
			}

			// Make sure the number of BP spent on attributes does not exceed half of the BP allowed for character creation.
			if (!_objCharacter.IgnoreRules && !_objOptions.AllowExceedAttributeBP && !_blnFreestyle)
			{
				if (_objCharacter.BuildMethod == CharacterBuildMethod.BP)
				{
					if (CalculatePrimaryAttributeBP() > _objCharacter.BuildPoints / 2)
					{
						try
						{
							nudLOG.Value -= 1;
							ShowAttributeBPRule();
						}
						catch
						{
							nudLOG.Value = nudLOG.Minimum;
						}
					}
				}
				else
				{
					if (!_objOptions.SpecialAttributeKarmaLimit)
					{
						// Include Special Attributes in the max 50% of Karma spent on Attributes limit.
						if (CalculatePrimaryAttributeBP() + CalculateSpecialAttributeBP() > ((_objCharacter.BuildKarma / 2) + (_objCharacter.MetatypeBP * 2)))
						{
							try
							{
								nudLOG.Value -= 1;
								ShowAttributeBPRule();
							}
							catch
							{
								nudLOG.Value = nudLOG.Minimum;
							}
						}
					}
					else
					{
						// Don't include Special Attributes in the max 50% of Karma spent on Attributes limit.
						if (CalculatePrimaryAttributeBP() > ((_objCharacter.BuildKarma / 2) + (_objCharacter.MetatypeBP * 2)))
						{
							try
							{
								nudLOG.Value -= 1;
								ShowAttributeBPRule();
							}
							catch
							{
								nudLOG.Value = nudLOG.Minimum;
							}
						}
					}
				}
			}
			_objCharacter.LOG.Value = Convert.ToInt32(nudLOG.Value);
			UpdateCharacterInfo();

			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void nudWIL_ValueChanged(object sender, EventArgs e)
		{
			// Don't attempt to do anything while the data is still being populated.
			if (_blnLoading)
				return;

			// Verify that the Attribute can be improved within the rules.
			if (!CanImproveAttribute("nudWIL") && nudWIL.Value >= nudWIL.Maximum && !_objCharacter.IgnoreRules)
			{
				try
				{
					nudWIL.Value = nudWIL.Maximum - 1;
					ShowAttributeRule();
				}
				catch
				{
					nudWIL.Value = nudLOG.Minimum;
				}
			}

			// Make sure the number of BP spent on attributes does not exceed half of the BP allowed for character creation.
			if (!_objCharacter.IgnoreRules && !_objOptions.AllowExceedAttributeBP && !_blnFreestyle)
			{
				if (_objCharacter.BuildMethod == CharacterBuildMethod.BP)
				{
					if (CalculatePrimaryAttributeBP() > _objCharacter.BuildPoints / 2)
					{
						try
						{
							nudWIL.Value -= 1;
							ShowAttributeBPRule();
						}
						catch
						{
							nudWIL.Value = nudWIL.Minimum;
						}
					}
				}
				else
				{
					if (!_objOptions.SpecialAttributeKarmaLimit)
					{
						// Include Special Attributes in the max 50% of Karma spent on Attributes limit.
						if (CalculatePrimaryAttributeBP() + CalculateSpecialAttributeBP() > ((_objCharacter.BuildKarma / 2) + (_objCharacter.MetatypeBP * 2)))
						{
							try
							{
								nudWIL.Value -= 1;
								ShowAttributeBPRule();
							}
							catch
							{
								nudWIL.Value = nudWIL.Minimum;
							}
						}
					}
					else
					{
						// Don't include Special Attributes in the max 50% of Karma spent on Attributes limit.
						if (CalculatePrimaryAttributeBP() > ((_objCharacter.BuildKarma / 2) + (_objCharacter.MetatypeBP * 2)))
						{
							try
							{
								nudWIL.Value -= 1;
								ShowAttributeBPRule();
							}
							catch
							{
								nudWIL.Value = nudWIL.Minimum;
							}
						}
					}
				}
			}
			_objCharacter.WIL.Value = Convert.ToInt32(nudWIL.Value);
			UpdateCharacterInfo();

			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void nudEDG_ValueChanged(object sender, EventArgs e)
		{
			// Don't attempt to do anything while the data is still being populated.
			if (_blnLoading)
				return;

			// Make sure the number of BP spent on attributes does not exceed half of the BP allowed for character creation.
			if (!_objCharacter.IgnoreRules && !_objOptions.AllowExceedAttributeBP && !_blnFreestyle)
			{
				if (_objCharacter.BuildMethod == CharacterBuildMethod.Karma && !_objOptions.SpecialAttributeKarmaLimit)
				{
					if (CalculatePrimaryAttributeBP() + CalculateSpecialAttributeBP() > ((_objCharacter.BuildKarma / 2) + (_objCharacter.MetatypeBP * 2)))
					{
						try
						{
							nudEDG.Value -= 1;
							ShowAttributeBPRule();
						}
						catch
						{
							nudEDG.Value = nudEDG.Minimum;
						}
					}
				}
			}
			_objCharacter.EDG.Value = Convert.ToInt32(nudEDG.Value);
			UpdateCharacterInfo();

			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void nudMAG_ValueChanged(object sender, EventArgs e)
		{
			// Don't attempt to do anything while the data is still being populated.
			if (_blnLoading)
				return;

			// Make sure the number of BP spent on attributes does not exceed half of the BP allowed for character creation.
			if (!_objCharacter.IgnoreRules && !_objOptions.AllowExceedAttributeBP && !_blnFreestyle)
			{
				if (_objCharacter.BuildMethod == CharacterBuildMethod.Karma && !_objOptions.SpecialAttributeKarmaLimit)
				{
					if (CalculatePrimaryAttributeBP() + CalculateSpecialAttributeBP() > ((_objCharacter.BuildKarma / 2) + (_objCharacter.MetatypeBP * 2)))
					{
						try
						{
							nudMAG.Value -= 1;
							ShowAttributeBPRule();
						}
						catch
						{
							nudMAG.Value = nudMAG.Minimum;
						}
					}
				}
			}
			_objCharacter.MAG.Value = Convert.ToInt32(nudMAG.Value);

			if (_objCharacter.Metatype == "Free Spirit")
			{
				// MAG determines the Metatype Maximum for Free Spirit, so change the Metatype Maximum for all other Attributes.
				_objCharacter.BOD.MetatypeMaximum = _objCharacter.MAG.Value;
				_objCharacter.AGI.MetatypeMaximum = _objCharacter.MAG.Value;
				_objCharacter.REA.MetatypeMaximum = _objCharacter.MAG.Value;
				_objCharacter.STR.MetatypeMaximum = _objCharacter.MAG.Value;
				_objCharacter.CHA.MetatypeMaximum = _objCharacter.MAG.Value;
				_objCharacter.INT.MetatypeMaximum = _objCharacter.MAG.Value;
				_objCharacter.LOG.MetatypeMaximum = _objCharacter.MAG.Value;
				_objCharacter.WIL.MetatypeMaximum = _objCharacter.MAG.Value;
				_objCharacter.EDG.MetatypeMaximum = _objCharacter.MAG.Value;
				_objCharacter.ESS.MetatypeMaximum = _objCharacter.MAG.Value;
			}

			// Update the maximum value for the Mystic Adept MAG field.
			nudMysticAdeptMAGMagician.Maximum = _objCharacter.MAG.TotalValue;

			UpdateCharacterInfo();

			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void nudRES_ValueChanged(object sender, EventArgs e)
		{
			// Don't attempt to do anything while the data is still being populated.
			if (_blnLoading)
				return;

			// Make sure the number of BP spent on attributes does not exceed half of the BP allowed for character creation.
			if (!_objCharacter.IgnoreRules && !_objOptions.AllowExceedAttributeBP && !_blnFreestyle)
			{
				if (_objCharacter.BuildMethod == CharacterBuildMethod.Karma && !_objOptions.SpecialAttributeKarmaLimit)
				{
					if (CalculatePrimaryAttributeBP() + CalculateSpecialAttributeBP() > ((_objCharacter.BuildKarma / 2) + (_objCharacter.MetatypeBP * 2)))
					{
						try
						{
							nudRES.Value -= 1;
							ShowAttributeBPRule();
						}
						catch
						{
							nudRES.Value = nudRES.Minimum;
						}
					}
				}
			}
			_objCharacter.RES.Value = Convert.ToInt32(nudRES.Value);

			try
			{
				if (treComplexForms.SelectedNode.Level == 1)
				{
					// Locate the Program that is selected in the tree.
					TechProgram objProgram = _objFunctions.FindTechProgram(treComplexForms.SelectedNode.Tag.ToString(), _objCharacter.TechPrograms);

					_blnSkipRefresh = true;
					if (objProgram.MaxRating == 0)
						nudComplexFormRating.Maximum = _objCharacter.RES.TotalValue;
					else
						nudComplexFormRating.Maximum = Math.Min(objProgram.MaxRating, _objCharacter.RES.TotalValue);
					_blnSkipRefresh = true;
				}
			}
			catch
			{
			}

			UpdateCharacterInfo();

			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void nudMysticAdeptMAGMagician_ValueChanged(object sender, EventArgs e)
		{
			_objCharacter.MAGMagician = Convert.ToInt32(nudMysticAdeptMAGMagician.Value);
			_objCharacter.MAGAdept = Convert.ToInt32(_objCharacter.MAG.TotalValue - nudMysticAdeptMAGMagician.Value);
			UpdateCharacterInfo();

			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void nudResponse_ValueChanged(object sender, EventArgs e)
		{
			_objCharacter.Response = Convert.ToInt32(nudResponse.Value);
			UpdateCharacterInfo();

			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void nudSignal_ValueChanged(object sender, EventArgs e)
		{
			_objCharacter.Signal = Convert.ToInt32(nudSignal.Value);
			UpdateCharacterInfo();

			_blnIsDirty = true;
			UpdateWindowTitle();
		}
		#endregion

		#region SkillControl Events
		private void objActiveSkill_RatingChanged(Object sender)
		{
			// Handle the RatingChanged event for the Active SkillControl object.
			SkillControl objSkillControl = (SkillControl)sender;
			if (!_objCharacter.IgnoreRules)
			{
				if (!CanImproveSkill(objSkillControl.SkillName, panActiveSkills) && objSkillControl.SkillRating > 4)
				{
					objSkillControl.SkillRating = 4;
				}
			}

			// If Summoning's Rating has changed, make sure the number of Services Owed by Spirits is brought in line.
			if (objSkillControl.SkillName == "Summoning")
			{
				foreach (SpiritControl objSpiritControl in panSpirits.Controls)
				{
					if (objSpiritControl.ServicesOwed > objSkillControl.SkillRating)
						objSpiritControl.ServicesOwed = objSkillControl.SkillRating;
				}
			}

			// If Comiling's Rating has changed, make sure the number of Services Owed by Sprites is brought in line.
			if (objSkillControl.SkillName == "Compiling")
			{
				foreach (SpiritControl objSpriteControl in panSprites.Controls)
				{
					if (objSpriteControl.ServicesOwed > objSkillControl.SkillRating)
						objSpriteControl.ServicesOwed = objSkillControl.SkillRating;
				}
			}

			UpdateCharacterInfo();

			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void objKnowledgeSkill_RatingChanged(Object sender)
		{
			// Handle the RatingChanged even for the Knowledge SkillControl object.
			if (!_objCharacter.IgnoreRules)
			{
				SkillControl objSkillControl = (SkillControl)sender;
				if (!CanImproveSkill(objSkillControl.SkillName, panKnowledgeSkills) && objSkillControl.SkillRating > 4)
				{
					objSkillControl.SkillRating = 4;
				}
			}
			UpdateCharacterInfo();

			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void objSkill_SpecializationChanged(Object sender)
		{
			// Handle the SpecializationChanged event for the SkillControl object.
			UpdateCharacterInfo();

			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void objKnowledgeSkill_DeleteSkill(Object sender)
		{
			if (!_objFunctions.ConfirmDelete(LanguageManager.Instance.GetString("Message_DeleteKnowledgeSkill")))
				return;

			// Handle the DeleteSkill event for the SkillControl object.
			SkillControl objSender = (SkillControl)sender;
			bool blnFound = false;
			foreach (SkillControl objSkillControl in panKnowledgeSkills.Controls)
			{
				// Set the flag to show that we have found the Skill.
				if (objSkillControl == objSender)
				{
					blnFound = true;
					_objCharacter.Skills.Remove(objSkillControl.SkillObject);
				}

				// Once the Skill has been found, all of the other SkillControls on the Panel should move up 25 pixels to fill in the gap that deleting this one will cause.
				if (blnFound)
					objSkillControl.Top -= 23;
			}
			// Remove the SkillControl that raised the Event.
			panKnowledgeSkills.Controls.Remove(objSender);
			UpdateCharacterInfo();

			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void objSkill_BreakGroupClicked(Object sender)
		{
			SkillControl objSkillControl = (SkillControl)sender;

			SkillGroup objSkillGroup = new SkillGroup();
			foreach (SkillGroupControl objSkillGroupControl in panSkillGroups.Controls)
			{
				if (objSkillGroupControl.GroupName == objSkillControl.SkillGroup)
				{
					objSkillGroup = objSkillGroupControl.SkillGroupObject;
					break;
				}
			}

			// Verify that the user wants to break the Skill Group.
			if (MessageBox.Show(LanguageManager.Instance.GetString("Message_BreakSkillGroup").Replace("{0}", objSkillGroup.DisplayName), LanguageManager.Instance.GetString("MessageTitle_BreakSkillGroup"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
				return;
			else
			{
				string strSkillGroup = objSkillControl.SkillGroup;
				int intRating = 0;

				// Break the Skill Group itself.
				foreach (SkillGroupControl objSkillGroupControl in panSkillGroups.Controls)
				{
					if (objSkillGroupControl.GroupName == strSkillGroup)
					{
						intRating = objSkillGroupControl.GroupRating;
						objSkillGroupControl.Broken = true;
						break;
					}
				}

				// Remove all of the Active Skills from the Skill Group being broken.
				string strGroup = objSkillControl.SkillGroup;
				foreach (SkillControl objActiveSkilll in panActiveSkills.Controls)
				{
					if (objActiveSkilll.SkillGroup == strGroup)
					{
						objActiveSkilll.IsGrouped = false;
						objActiveSkilll.SkillRating = intRating;
					}
				}
			}
		}

		private void objGroup_RatingChanged(Object sender)
		{
			// Handle the GroupRatingChanged event for the SkillGroupControl object.
			SkillGroupControl objGroupControl = (SkillGroupControl)sender;
			XmlDocument objXmlDocument = XmlManager.Instance.Load("skills.xml");

			// Retrieve the list of Skills that are associated with the Skill Group.
			XmlNodeList objXmlSkillList = objXmlDocument.SelectNodes("/chummer/skills/skill[skillgroup = \"" + objGroupControl.GroupName + "\"]");

			foreach (XmlNode objXmlSkill in objXmlSkillList)
			{
				// Run through all of the Skills in the Active Skill Panel and update the ones that match the Skills in the Skill Group.
				foreach (SkillControl objSkillControl in panActiveSkills.Controls)
				{
					if (objSkillControl.SkillName == objXmlSkill["name"].InnerText)
					{
						if (objGroupControl.GroupRating > 0)
						{
							// Setting a Group's Rating above 0 should place the Skill in the Group and disable the SkillControl.
							if (objGroupControl.GroupRating > objSkillControl.SkillRatingMaximum)
								objSkillControl.SkillRatingMaximum = objGroupControl.GroupRating;
							objSkillControl.SkillRating = objGroupControl.GroupRating;
							objSkillControl.IsGrouped = true;
						}
						else
						{
							// Returning a Group's Rating back to 0 should release the Skill from the Group and re-enable the SkillControl.
							objSkillControl.SkillRating = 0;
							objSkillControl.IsGrouped = false;
						}
					}
				}
			}

			UpdateCharacterInfo();

			_blnIsDirty = true;
			UpdateWindowTitle();
		}
		#endregion

		#region ContactControl Events
		private void objContact_ConnectionRatingChanged(Object sender)
		{
			// Handle the ConnectionRatingChanged Event for the ContactControl object.
			UpdateCharacterInfo();

			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void objContact_ConnectionGroupRatingChanged(Object sender)
		{
			// Handle the ConnectionGroupRatingChanged Event for the ContactControl object.
			UpdateCharacterInfo();

			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void objContact_LoyaltyRatingChanged(Object sender)
		{
			// Handle the LoyaltyRatingChanged Event for the ContactControl object.
			UpdateCharacterInfo();

			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void objContact_DeleteContact(Object sender)
		{
			if (!_objFunctions.ConfirmDelete(LanguageManager.Instance.GetString("Message_DeleteContact")))
				return;

			// Handle the DeleteContact Event for the ContactControl object.
			ContactControl objSender = (ContactControl)sender;
			bool blnFound = false;
			foreach (ContactControl objContactControl in panContacts.Controls)
			{
				// Set the flag to show that we have found the Contact.
				if (objContactControl == objSender)
					blnFound = true;

				// Once the Contact has been found, all of the other ContactControls on the Panel should move up 25 pixels to fill in the gap that deleting this one will cause.
				if (blnFound)
				{
					_objCharacter.Contacts.Remove(objContactControl.ContactObject);
					objContactControl.Top -= 25;
				}
			}
			// Remove the ContactControl that raised the Event.
			panContacts.Controls.Remove(objSender);
			UpdateCharacterInfo();

			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void objContact_FileNameChanged(Object sender)
		{
			// Handle the FileNameChanged Event for the ContactControl object.
			UpdateCharacterInfo();

			_blnIsDirty = true;
			UpdateWindowTitle();
		}
		#endregion

		#region EnemyControl Events
		private void objEnemy_ConnectionRatingChanged(Object sender)
		{
			// Handle the ConnectionRatingChanged Event for the ContactControl object.
			int intNegativeQualityBP = 0;
			// Calculate the BP used for Negative Qualities.
			foreach (Quality objQuality in _objCharacter.Qualities)
			{
				if (objQuality.Type == QualityType.Negative && objQuality.ContributeToLimit)
					intNegativeQualityBP += objQuality.BP;
			}
			// Include the amount of free Negative Qualities from Improvements.
			intNegativeQualityBP -= _objImprovementManager.ValueOf(Improvement.ImprovementType.FreeNegativeQualities);

			// Adjust for Karma build method.
			if (_objCharacter.BuildMethod == CharacterBuildMethod.Karma)
				intNegativeQualityBP *= _objOptions.KarmaQuality;

			int intBPUsed = 0;
			foreach (ContactControl objContactControl in panEnemies.Controls)
			{
				if (_objCharacter.BuildMethod == CharacterBuildMethod.BP)
					intBPUsed -= (objContactControl.ConnectionRating + objContactControl.LoyaltyRating + objContactControl.GroupRating);
				else
					intBPUsed -= (objContactControl.ConnectionRating + objContactControl.LoyaltyRating + objContactControl.GroupRating) * _objOptions.KarmaQuality;
			}

			int intEnemyMax = 0;
			int intQualityMax = 0;
			string strQualityPoints = "";
			string strEnemyPoints = "";
			if (_objCharacter.BuildMethod == CharacterBuildMethod.BP)
			{
				intEnemyMax = 25;
				intQualityMax = 35;
				strEnemyPoints = "25 " + LanguageManager.Instance.GetString("String_BP");
				strQualityPoints = "35 " + LanguageManager.Instance.GetString("String_BP");
			}
			else
			{
				intEnemyMax = 50;
				intQualityMax = 70;
				strEnemyPoints = "50 " + LanguageManager.Instance.GetString("String_Karma");
				strQualityPoints = "70 " + LanguageManager.Instance.GetString("String_Karma");
			}

			if (intBPUsed < (intEnemyMax * -1) && !_objCharacter.IgnoreRules)
			{
				MessageBox.Show(LanguageManager.Instance.GetString("Message_EnemyLimit").Replace("{0}", strEnemyPoints), LanguageManager.Instance.GetString("MessageTitle_EnemyLimit"), MessageBoxButtons.OK, MessageBoxIcon.Information);
				ContactControl objSender = (ContactControl)sender;
				objSender.ConnectionRating -= (intEnemyMax * -1) - intBPUsed;
				return;
			}

			if (!_objOptions.ExceedNegativeQualities)
			{
				if (intBPUsed + intNegativeQualityBP < (intQualityMax * -1) && !_objCharacter.IgnoreRules)
				{
					MessageBox.Show(LanguageManager.Instance.GetString("Message_NegativeQualityLimit").Replace("{0}", strQualityPoints), LanguageManager.Instance.GetString("MessageTitle_NegativeQualityLimit"), MessageBoxButtons.OK, MessageBoxIcon.Information);
					ContactControl objSender = (ContactControl)sender;
					if (_objCharacter.BuildMethod == CharacterBuildMethod.BP)
						objSender.ConnectionRating -= ((intQualityMax * -1) - (intBPUsed + intNegativeQualityBP));
					else
						objSender.ConnectionRating -= (((intQualityMax * -1) - (intBPUsed + intNegativeQualityBP)) / _objOptions.KarmaQuality);
				}
			}

			UpdateCharacterInfo();

			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void objEnemy_ConnectionGroupRatingChanged(Object sender)
		{
			// Handle the ConnectionRatingChanged Event for the ContactControl object.
			int intNegativeQualityBP = 0;
			// Calculate the BP used for Negative Qualities.
			foreach (Quality objQuality in _objCharacter.Qualities)
			{
				if (objQuality.Type == QualityType.Negative && objQuality.ContributeToLimit)
					intNegativeQualityBP += objQuality.BP;
			}
			// Include the amount of free Negative Qualities from Improvements.
			intNegativeQualityBP -= _objImprovementManager.ValueOf(Improvement.ImprovementType.FreeNegativeQualities);

			// Adjust for Karma build method.
			if (_objCharacter.BuildMethod == CharacterBuildMethod.Karma)
				intNegativeQualityBP *= _objOptions.KarmaQuality;

			int intBPUsed = 0;
			foreach (ContactControl objContactControl in panEnemies.Controls)
			{
				if (_objCharacter.BuildMethod == CharacterBuildMethod.BP)
					intBPUsed -= (objContactControl.ConnectionRating + objContactControl.LoyaltyRating + objContactControl.GroupRating);
				else
					intBPUsed -= (objContactControl.ConnectionRating + objContactControl.LoyaltyRating + objContactControl.GroupRating) * _objOptions.KarmaQuality;
			}

			int intEnemyMax = 0;
			int intQualityMax = 0;
			string strEnemyPoints = "";
			string strQualityPoints = "";
			if (_objCharacter.BuildMethod == CharacterBuildMethod.BP)
			{
				intEnemyMax = 25;
				intQualityMax = 35;
				strEnemyPoints = "25 " + LanguageManager.Instance.GetString("String_BP");
				strQualityPoints = "35 " + LanguageManager.Instance.GetString("String_BP");
			}
			else
			{
				intEnemyMax = 50;
				intQualityMax = 70;
				strEnemyPoints = "50 " + LanguageManager.Instance.GetString("String_Karma");
				strQualityPoints = "70 " + LanguageManager.Instance.GetString("String_Karma");
			}

			if (intBPUsed < (intEnemyMax * -1) && !_objCharacter.IgnoreRules)
			{
				MessageBox.Show(LanguageManager.Instance.GetString("Message_EnemyLimit").Replace("{0}", strEnemyPoints), LanguageManager.Instance.GetString("MessageTitle_EnemyLimit"), MessageBoxButtons.OK, MessageBoxIcon.Information);
				ContactControl objSender = (ContactControl)sender;
				objSender.ContactObject.AreaOfInfluence = 0;
				objSender.ContactObject.MagicalResources = 0;
				objSender.ContactObject.MatrixResources = 0;
				objSender.ContactObject.Membership = 0;
				objSender.GroupRating = 0;
				return;
			}

			if (!_objOptions.ExceedNegativeQualities)
			{
				if (intBPUsed + intNegativeQualityBP < (intQualityMax * -1) && !_objCharacter.IgnoreRules)
				{
					MessageBox.Show(LanguageManager.Instance.GetString("Message_NegativeQualityLimit").Replace("{0}", strQualityPoints), LanguageManager.Instance.GetString("MessageTitle_NegativeQualityLimit"), MessageBoxButtons.OK, MessageBoxIcon.Information);
					ContactControl objSender = (ContactControl)sender;
					objSender.ContactObject.AreaOfInfluence = 0;
					objSender.ContactObject.MagicalResources = 0;
					objSender.ContactObject.MatrixResources = 0;
					objSender.ContactObject.Membership = 0;
					objSender.GroupRating = 0;
					return;
				}
			}

			UpdateCharacterInfo();

			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void objEnemy_LoyaltyRatingChanged(Object sender)
		{
			// Handle the LoyaltyRatingChanged Event for the ContactControl object.
			int intNegativeQualityBP = 0;
			// Calculate the BP used for Negative Qualities.
			foreach (Quality objQuality in _objCharacter.Qualities)
			{
				if (objQuality.Type == QualityType.Negative && objQuality.ContributeToLimit)
					intNegativeQualityBP += objQuality.BP;
			}
			// Include the amount of free Negative Qualities from Improvements.
			intNegativeQualityBP -= _objImprovementManager.ValueOf(Improvement.ImprovementType.FreeNegativeQualities);

			// Adjust for Karma build method.
			if (_objCharacter.BuildMethod == CharacterBuildMethod.Karma)
				intNegativeQualityBP *= _objOptions.KarmaQuality;

			int intBPUsed = 0;
			foreach (ContactControl objContactControl in panEnemies.Controls)
			{
				if (_objCharacter.BuildMethod == CharacterBuildMethod.BP)
					intBPUsed -= (objContactControl.ConnectionRating + objContactControl.LoyaltyRating + objContactControl.GroupRating);
				else
					intBPUsed -= (objContactControl.ConnectionRating + objContactControl.LoyaltyRating + objContactControl.GroupRating) * _objOptions.KarmaQuality;
			}

			int intEnemyMax = 0;
			int intQualityMax = 0;
			string strEnemyPoints = "";
			string strQualityPoints = "";
			if (_objCharacter.BuildMethod == CharacterBuildMethod.BP)
			{
				intEnemyMax = 25;
				intQualityMax = 35;
				strEnemyPoints = "25 " + LanguageManager.Instance.GetString("String_BP");
				strQualityPoints = "35 " + LanguageManager.Instance.GetString("String_BP");
			}
			else
			{
				intEnemyMax = 50;
				intQualityMax = 70;
				strEnemyPoints = "50 " + LanguageManager.Instance.GetString("String_Karma");
				strQualityPoints = "70 " +LanguageManager.Instance.GetString("String_Karma");
			}

			if (intBPUsed < (intEnemyMax * -1) && !_objCharacter.IgnoreRules)
			{
				MessageBox.Show(LanguageManager.Instance.GetString("Message_EnemyLimit").Replace("{0}", strEnemyPoints), LanguageManager.Instance.GetString("MessageTitle_EnemyLimit"), MessageBoxButtons.OK, MessageBoxIcon.Information);
				ContactControl objSender = (ContactControl)sender;
				objSender.LoyaltyRating -= (intEnemyMax * -1) - intBPUsed;
				return;
			}

			if (!_objOptions.ExceedNegativeQualities)
			{
				if (intBPUsed + intNegativeQualityBP < (intQualityMax * -1) && !_objCharacter.IgnoreRules)
				{
					MessageBox.Show(LanguageManager.Instance.GetString("Message_NegativeQualityLimit").Replace("{0}", strQualityPoints), LanguageManager.Instance.GetString("MessageTitle_NegativeQualityLimit"), MessageBoxButtons.OK, MessageBoxIcon.Information);
					ContactControl objSender = (ContactControl)sender;
					if (_objCharacter.BuildMethod == CharacterBuildMethod.BP)
						objSender.LoyaltyRating -= ((intQualityMax * -1) - (intBPUsed + intNegativeQualityBP));
					else
						objSender.LoyaltyRating -= (((intQualityMax * -1) - (intBPUsed + intNegativeQualityBP)) / _objOptions.KarmaQuality);
				}
			}

			UpdateCharacterInfo();

			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void objEnemy_DeleteContact(Object sender)
		{
			if (!_objFunctions.ConfirmDelete(LanguageManager.Instance.GetString("Message_DeleteEnemy")))
				return;

			// Handle the DeleteCOntact Event for the ContactControl object.
			ContactControl objSender = (ContactControl)sender;
			bool blnFound = false;
			foreach (ContactControl objContactControl in panEnemies.Controls)
			{
				// Set the flag to show that we have found the contact.
				if (objContactControl == objSender)
					blnFound = true;

				// Once the Enemy has been found, all of the other ContactControls on the Panel should move up 25 pixels to fill in the gap that deleting this one will cause.
				if (blnFound)
				{
					_objCharacter.Contacts.Remove(objContactControl.ContactObject);
					objContactControl.Top -= 25;
				}
			}
			// Remove the ContactControl that raised the Event.
			panEnemies.Controls.Remove(objSender);
			UpdateCharacterInfo();

			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void objEnemy_FileNameChanged(Object sender)
		{
			// Handle the FileNameChanged Event for the ContactControl object.
			UpdateCharacterInfo();

			_blnIsDirty = true;
			UpdateWindowTitle();
		}
		#endregion

		#region PetControl Events
		private void objPet_DeleteContact(Object sender)
		{
			if (!_objFunctions.ConfirmDelete(LanguageManager.Instance.GetString("Message_DeleteContact")))
				return;

			// Handle the DeleteContact Event for the ContactControl object.
			PetControl objSender = (PetControl)sender;
			bool blnFound = false;
			foreach (PetControl objContactControl in panPets.Controls)
			{
				// Set the flag to show that we have found the Contact.
				if (objContactControl == objSender)
					blnFound = true;

				// Once the Contact has been found, all of the other ContactControls on the Panel should move up 25 pixels to fill in the gap that deleting this one will cause.
				if (blnFound)
					_objCharacter.Contacts.Remove(objContactControl.ContactObject);
			}
			// Remove the ContactControl that raised the Event.
			panPets.Controls.Remove(objSender);
			UpdateCharacterInfo();

			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void objPet_FileNameChanged(Object sender)
		{
			// Handle the FileNameChanged Event for the ContactControl object.
			UpdateCharacterInfo();

			_blnIsDirty = true;
			UpdateWindowTitle();
		}
		#endregion

		#region SpiritControl Events
		private void objSpirit_ForceChanged(Object sender)
		{
			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void objSpirit_BoundChanged(Object sender)
		{
			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void objSpirit_ServicesOwedChanged(Object sender)
		{
			// Handle the ServicesOwedChanged Event for the SpiritControl object.
			// A Spirit cannot owe more services than the character's Summoning Skill Rating.
			SpiritControl objSpiritControl = (SpiritControl)sender;
			int intSkillValue = 0;

			// Retrieve the character's Summoning Skill Rating.
			foreach (SkillControl objSkillControl in panActiveSkills.Controls)
			{
				if (objSkillControl.SkillName == "Summoning")
					intSkillValue = objSkillControl.SkillRating;
			}

			if (objSpiritControl.ServicesOwed > intSkillValue && !_objCharacter.IgnoreRules)
			{
				MessageBox.Show(LanguageManager.Instance.GetString("Message_SpiritServices"), LanguageManager.Instance.GetString("MessageTitle_SpiritServices"), MessageBoxButtons.OK, MessageBoxIcon.Information);
				objSpiritControl.ServicesOwed = intSkillValue;
			}

			UpdateCharacterInfo();

			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void objSpirit_DeleteSpirit(Object sender)
		{
			if (!_objFunctions.ConfirmDelete(LanguageManager.Instance.GetString("Message_DeleteSpirit")))
				return;

			// Handle the DeleteSpirit Event for the SpiritControl object.
			SpiritControl objSender = (SpiritControl)sender;
			bool blnFound = false;
			foreach (SpiritControl objSpiritControl in panSpirits.Controls)
			{
				// Set the flag to show that we have found the Spirit.
				if (objSpiritControl == objSender)
					blnFound = true;

				// Once the Spirit has been found, all of the other SpiritControls on the Panel should move up 25 pixels to fill in the gap that deleting this one will cause.
				if (blnFound)
				{
					_objCharacter.Spirits.Remove(objSpiritControl.SpiritObject);
					objSpiritControl.Top -= 25;
				}
			}
			// Remove the SpiritControl that raised the Event.
			panSpirits.Controls.Remove(objSender);
			UpdateCharacterInfo();

			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void objSpirit_FileNameChanged(Object sender)
		{
			// Handle the FileNameChanged Event for the SpritControl object.
			UpdateCharacterInfo();

			_blnIsDirty = true;
			UpdateWindowTitle();
		}
		#endregion

		#region SpriteControl (SpiritControl) Events
		private void objSprite_ForceChanged(Object sender)
		{
			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void objSprite_BoundChanged(Object sender)
		{
			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void objSprite_ServicesOwedChanged(Object sender)
		{
			// Handle the ServicesOwedChanged Event for the SpiritControl object.
			// A Sprite cannot owe more services than the character's Compiling Skill Rating.
			SpiritControl objSpriteControl = (SpiritControl)sender;
			int intSkillValue = 0;

			// Retrieve the character's Compiling Skill Rating.
			foreach (SkillControl objSkillControl in panActiveSkills.Controls)
			{
				if (objSkillControl.SkillName == "Compiling")
					intSkillValue = objSkillControl.SkillRating;
			}

			if (objSpriteControl.ServicesOwed > intSkillValue && !_objCharacter.IgnoreRules)
			{
				MessageBox.Show(LanguageManager.Instance.GetString("Message_SpriteServices"), LanguageManager.Instance.GetString("MessageTitle_SpriteServices"), MessageBoxButtons.OK, MessageBoxIcon.Information);
				objSpriteControl.ServicesOwed = intSkillValue;
			}

			UpdateCharacterInfo();

			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void objSprite_DeleteSpirit(Object sender)
		{
			if (!_objFunctions.ConfirmDelete(LanguageManager.Instance.GetString("Message_DeleteSprite")))
				return;

			// Handle the DeleteSpirit Event for the SpiritControl object.
			SpiritControl objSender = (SpiritControl)sender;
			bool blnFound = false;
			foreach (SpiritControl objSpriteControl in panSprites.Controls)
			{
				// Set the flag to show that we have found the Sprite.
				if (objSpriteControl == objSender)
					blnFound = true;

				// Once the Spirit has been found, all of the other SpiritControls on the Panel should move up 25 pixels to fill in the gap that deleting this one will cause.
				if (blnFound)
				{
					_objCharacter.Spirits.Remove(objSpriteControl.SpiritObject);
					objSpriteControl.Top -= 25;
				}
			}
			// Remove the SpiritControl that raised the Event.
			panSprites.Controls.Remove(objSender);
			UpdateCharacterInfo();

			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void objSprite_FileNameChanged(Object sender)
		{
			// Handle the FileNameChanged Event for the SpiritControl object.
			UpdateCharacterInfo();

			_blnIsDirty = true;
			UpdateWindowTitle();
		}
		#endregion

		#region PowerControl Events
		private void objPower_PowerRatingChanged(Object sender)
		{
			// Handle the PowerRatingChange Event for the PowerControl object.
			PowerControl objPowerControl = (PowerControl)sender;
			if (objPowerControl.PowerLevel > _objCharacter.MAG.TotalValue && !_objCharacter.IgnoreRules)
			{
				MessageBox.Show(LanguageManager.Instance.GetString("Message_PowerLevel"), LanguageManager.Instance.GetString("MessageTitle_PowerLevel"), MessageBoxButtons.OK, MessageBoxIcon.Information);
				objPowerControl.PowerLevel = _objCharacter.MAG.TotalValue;
			}
			else
			{
				// If the Bonus contains "Rating", remove the existing Improvements and create new ones.
				if (objPowerControl.PowerObject.Bonus != null)
				{
					if (objPowerControl.PowerObject.Bonus.InnerXml.Contains("Rating"))
					{
						_objImprovementManager.RemoveImprovements(Improvement.ImprovementSource.Power, objPowerControl.PowerObject.InternalId);
						_objImprovementManager.ForcedValue = objPowerControl.Extra;
						_objImprovementManager.CreateImprovements(Improvement.ImprovementSource.Power, objPowerControl.PowerObject.InternalId, objPowerControl.PowerObject.Bonus, false, Convert.ToInt32(objPowerControl.PowerObject.Rating), objPowerControl.PowerObject.DisplayNameShort);
					}
				}
			}

			UpdateCharacterInfo();

			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void objPower_DeletePower(Object sender)
		{
			if (!_objFunctions.ConfirmDelete(LanguageManager.Instance.GetString("Message_DeletePower")))
				return;
			
			// Handle the DeletePower Event for the PowerControl.
			PowerControl objSender = (PowerControl)sender;
			bool blnFound = false;
			foreach (PowerControl objPowerControl in panPowers.Controls)
			{
				// Set the flag to show that we have found the Power.
				if (objPowerControl == objSender)
					blnFound = true;

				// Once the Power has been found, all of the other PowerControls on the Panel should move up 25 pixels to fill in the gap that deleting this one will cause.
				if (blnFound)
					objPowerControl.Top -= 25;
			}

			// Remove the Improvements that were created by the Power.
			_objImprovementManager.RemoveImprovements(Improvement.ImprovementSource.Power, objSender.PowerObject.InternalId);

			// Remove the Power.
			_objCharacter.Powers.Remove(objSender.PowerObject);

			// Update the Attribute label.
			UpdateCharacterInfo();

			// Remove the PowerControl that raised the Event.
			panPowers.Controls.Remove(objSender);
			CalculatePowerPoints();

			_blnIsDirty = true;
			UpdateWindowTitle();
		}
		#endregion

		#region Martial Tab Control Events
		private void nudMartialArtsRating_ValueChanged(object sender, EventArgs e)
		{
			try
			{
				if (treMartialArts.SelectedNode.Level > 0)
				{
					MartialArt objMartialArt = _objFunctions.FindMartialArt(treMartialArts.SelectedNode.Tag.ToString(), _objCharacter.MartialArts);

					// Make sure the user does not try to set the number lower than the number of Advantages they currently have for the selected Martial Art.
					if (Convert.ToInt32(nudMartialArtsRating.Value) < objMartialArt.Advantages.Count && !_objCharacter.IgnoreRules)
					{
						MessageBox.Show(LanguageManager.Instance.GetString("Message_MartialArtAdvantageLimit").Replace("{0}", objMartialArt.Name), LanguageManager.Instance.GetString("MessageTitle_MartialArtAdvantageLimit"), MessageBoxButtons.OK, MessageBoxIcon.Information);
						nudMartialArtsRating.Value = objMartialArt.Advantages.Count;
						return;
					}

					objMartialArt.Rating = Convert.ToInt32(nudMartialArtsRating.Value);

					if (!_blnSkipRefresh && !_objCharacter.IgnoreRules)
					{
						// Characters may only have 2 Maneuvers per Martial Art Rating.
						int intTotalRating = 0;
						foreach (MartialArt objCharacterMartialArt in _objCharacter.MartialArts)
							intTotalRating += objCharacterMartialArt.Rating * 2;

						if (treMartialArts.Nodes[1].Nodes.Count > intTotalRating)
						{
							MessageBox.Show(LanguageManager.Instance.GetString("Message_MartialArtManeuverLimit"), LanguageManager.Instance.GetString("MessageTitle_MartialArtManeuverLimit"), MessageBoxButtons.OK, MessageBoxIcon.Information);
							nudMartialArtsRating.Value = Math.Ceiling(Convert.ToDecimal(treMartialArts.Nodes[1].Nodes.Count / 2.0, GlobalOptions.Instance.CultureInfo));
							objMartialArt.Rating = Convert.ToInt32(nudMartialArtsRating.Value);
							return;
						}

						// Make sure that adding a new Martial Art will not take the user over 35 BP for Positive Qualities.
						// Start counting from 5 which is the cost for a new Martial Art at Rating 1.
						int intBP = 0;
						foreach (Quality objQuality in _objCharacter.Qualities)
						{
							if (objQuality.Type == QualityType.Positive && objQuality.ContributeToLimit)
								intBP += objQuality.BP;
						}
						// Include free Positive Quality Improvements.
						intBP -= _objImprovementManager.ValueOf(Improvement.ImprovementType.FreePositiveQualities);

						if (_objCharacter.BuildMethod == CharacterBuildMethod.Karma)
							intBP *= _objOptions.KarmaQuality;

						foreach (MartialArt objCharacterMartialArt in _objCharacter.MartialArts)
						{
							if (_objCharacter.BuildMethod == CharacterBuildMethod.BP)
								intBP += objCharacterMartialArt.Rating * 5;
							else
								intBP += objCharacterMartialArt.Rating * 5 * _objOptions.KarmaQuality;
						}

						if (!_objOptions.ExceedPositiveQualities)
						{
							if (_objCharacter.BuildMethod == CharacterBuildMethod.BP)
							{
								if (intBP > 35 && !_blnLoading && !_objCharacter.IgnoreRules)
								{
									MessageBox.Show(LanguageManager.Instance.GetString("Message_PositiveQualityLimit").Replace("{0}", "35 " + LanguageManager.Instance.GetString("String_BP")), LanguageManager.Instance.GetString("MessageTitle_PositiveQualityLimit"), MessageBoxButtons.OK, MessageBoxIcon.Information);
									nudMartialArtsRating.Value -= 1;
									return;
								}
							}
							else
							{
								if (intBP > 70 && !_blnLoading && !_objCharacter.IgnoreRules)
								{
									MessageBox.Show(LanguageManager.Instance.GetString("Message_PositiveQualityLimit").Replace("{0}", "70 " + LanguageManager.Instance.GetString("String_Karma")), LanguageManager.Instance.GetString("MessageTitle_PositiveQualityLimit"), MessageBoxButtons.OK, MessageBoxIcon.Information);
									nudMartialArtsRating.Value -= 1;
									return;
								}
							}
						}
					}

					CalculateBP();

					_blnIsDirty = true;
					UpdateWindowTitle();
				}
			}
			catch
			{
			}
		}

		private void treMartialArts_AfterSelect(object sender, TreeViewEventArgs e)
		{
			try
			{
				// The Rating NUD is only enabled if a Martial Art is currently selected.
				if (treMartialArts.SelectedNode.Level == 1 && treMartialArts.SelectedNode.Parent == treMartialArts.Nodes[0])
				{
					MartialArt objMartialArt = _objFunctions.FindMartialArt(treMartialArts.SelectedNode.Tag.ToString(), _objCharacter.MartialArts);

					_blnSkipRefresh = true;
					nudMartialArtsRating.Enabled = true;
					nudMartialArtsRating.Value = objMartialArt.Rating;
					string strBook = _objOptions.LanguageBookShort(objMartialArt.Source);
					string strPage = objMartialArt.Page;
					lblMartialArtSource.Text = strBook + " " + strPage;
					tipTooltip.SetToolTip(lblMartialArtSource, _objOptions.LanguageBookLong(objMartialArt.Source) + " page " + objMartialArt.Page);
					_blnSkipRefresh = false;
				}
				else
					nudMartialArtsRating.Enabled = false;

				// Display the Martial Art Advantage information.
				if (treMartialArts.SelectedNode.Level == 2)
				{
					MartialArt objMartialArt = new MartialArt(_objCharacter);
					MartialArtAdvantage objAdvantage = _objFunctions.FindMartialArtAdvantage(treMartialArts.SelectedNode.Tag.ToString(), _objCharacter.MartialArts, out objMartialArt);

					string strBook = _objOptions.LanguageBookShort(objMartialArt.Source);
					string strPage = objMartialArt.Page;
					lblMartialArtSource.Text = strBook + " " + strPage;
					tipTooltip.SetToolTip(lblMartialArtSource, _objOptions.LanguageBookLong(objMartialArt.Source) + " page " + objMartialArt.Page);
				}

				// Display the Maneuver information.
				if (treMartialArts.SelectedNode.Level == 1 && treMartialArts.SelectedNode.Parent == treMartialArts.Nodes[1])
				{
					MartialArtManeuver objManeuver = _objFunctions.FindMartialArtManeuver(treMartialArts.SelectedNode.Tag.ToString(), _objCharacter.MartialArtManeuvers);

					string strBook = _objOptions.LanguageBookShort(objManeuver.Source);
					string strPage = objManeuver.Page;
					lblMartialArtSource.Text = strBook + " " + strPage;
					tipTooltip.SetToolTip(lblMartialArtSource, _objOptions.LanguageBookLong(objManeuver.Source) + " page " + objManeuver.Page);
				}
			}
			catch
			{
				nudMartialArtsRating.Enabled = false;
			}
		}
		#endregion

		#region Button Events
		private void cmdAddKnowledgeSkill_Click(object sender, EventArgs e)
		{
			int i = panKnowledgeSkills.Controls.Count;
			Skill objSkill = new Skill(_objCharacter);
			objSkill.Attribute = "LOG";
			objSkill.SkillCategory = "Academic";
			if (_objCharacter.MaxSkillRating > 0)
				objSkill.RatingMaximum = _objCharacter.MaxSkillRating;

			SkillControl objSkillControl = new SkillControl();
			objSkillControl.SkillObject = objSkill;

			// Attach an EventHandler for the RatingChanged and SpecializationChanged Events.
			objSkillControl.RatingChanged += objKnowledgeSkill_RatingChanged;
			objSkillControl.SpecializationChanged += objSkill_SpecializationChanged;
			objSkillControl.DeleteSkill += objKnowledgeSkill_DeleteSkill;

			objSkillControl.KnowledgeSkill = true;
			objSkillControl.AllowDelete = true;
			objSkillControl.SkillRatingMaximum = 6;
			// Set the SkillControl's Location since scrolling the Panel causes it to actually change the child Controls' Locations.
			objSkillControl.Location = new Point(0, objSkillControl.Height * i + panKnowledgeSkills.AutoScrollPosition.Y);
			panKnowledgeSkills.Controls.Add(objSkillControl);

			_objCharacter.Skills.Add(objSkill);

			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void cmdAddContact_Click(object sender, EventArgs e)
		{
			Contact objContact = new Contact(_objCharacter);
			_objCharacter.Contacts.Add(objContact);

			int i = panContacts.Controls.Count;
			ContactControl objContactControl = new ContactControl();
			objContactControl.ContactObject = objContact;
			objContactControl.EntityType = ContactType.Contact;

			// Attach an EventHandler for the ConnectionRatingChanged, LoyaltyRatingChanged, DeleteContact, and FileNameChanged Events.
			objContactControl.ConnectionRatingChanged += objContact_ConnectionRatingChanged;
			objContactControl.ConnectionGroupRatingChanged += objContact_ConnectionGroupRatingChanged;
			objContactControl.LoyaltyRatingChanged += objContact_LoyaltyRatingChanged;
			objContactControl.DeleteContact += objContact_DeleteContact;
			objContactControl.FileNameChanged += objContact_FileNameChanged;

			// Set the ContactControl's Location since scrolling the Panel causes it to actually change the child Controls' Locations.
			objContactControl.Location = new Point(0, objContactControl.Height * i + panContacts.AutoScrollPosition.Y);
			panContacts.Controls.Add(objContactControl);
			UpdateCharacterInfo();

			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void cmdAddEnemy_Click(object sender, EventArgs e)
		{
			// Handle the ConnectionRatingChanged Event for the ContactControl object.
			int intNegativeQualityBP = 0;
			// Calculate the BP used for Negative Qualities.
			foreach (Quality objQuality in _objCharacter.Qualities)
			{
				if (objQuality.Type == QualityType.Negative && objQuality.ContributeToLimit)
					intNegativeQualityBP += objQuality.BP;
			}
			// Include the amount of free Negative Qualities from Improvements.
			intNegativeQualityBP -= _objImprovementManager.ValueOf(Improvement.ImprovementType.FreeNegativeQualities);

			// Adjust for Karma build method.
			if (_objCharacter.BuildMethod == CharacterBuildMethod.Karma)
				intNegativeQualityBP *= _objOptions.KarmaQuality;

			int intBPUsed = 0;
			int intEnemyMax = 0;
			int intQualityMax = 0;
			string strEnemyPoints = "";
			string strQualityPoints = "";
			if (_objCharacter.BuildMethod == CharacterBuildMethod.BP)
			{
				intBPUsed = -2;
				intEnemyMax = 25;
				intQualityMax = 35;
				strEnemyPoints = "25 " + LanguageManager.Instance.GetString("String_BP");
				strQualityPoints = "35 " + LanguageManager.Instance.GetString("String_BP");
			}
			else
			{
				intBPUsed = -2 * _objOptions.KarmaQuality;
				intEnemyMax = 50;
				intQualityMax = 70;
				strEnemyPoints = "50 " + LanguageManager.Instance.GetString("String_Karma");
				strQualityPoints = "70 " +LanguageManager.Instance.GetString("String_Karma");
			}

			foreach (ContactControl objEnemyControl in panEnemies.Controls)
			{
				if (_objCharacter.BuildMethod == CharacterBuildMethod.BP)
					intBPUsed -= (objEnemyControl.ConnectionRating + objEnemyControl.LoyaltyRating);
				else
					intBPUsed -= (objEnemyControl.ConnectionRating + objEnemyControl.LoyaltyRating) * _objOptions.KarmaQuality;
			}

			if (intBPUsed < (intEnemyMax * -1) && !_objCharacter.IgnoreRules)
			{
				MessageBox.Show(LanguageManager.Instance.GetString("Message_EnemyLimit").Replace("{0}", strEnemyPoints), LanguageManager.Instance.GetString("MessageTitle_EnemyLimit"), MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			if (!_objOptions.ExceedNegativeQualities)
			{
				if (intBPUsed + intNegativeQualityBP < (intQualityMax * -1) && !_objCharacter.IgnoreRules)
				{
					MessageBox.Show(LanguageManager.Instance.GetString("Message_NegativeQualityLimit").Replace("{0}", strQualityPoints), LanguageManager.Instance.GetString("MessageTitle_NegativeQualityLimit"), MessageBoxButtons.OK, MessageBoxIcon.Information);
					return;
				}
			}

			Contact objContact = new Contact(_objCharacter);
			_objCharacter.Contacts.Add(objContact);

			int i = panEnemies.Controls.Count;
			ContactControl objContactControl = new ContactControl();
			objContactControl.ContactObject = objContact;
			objContactControl.EntityType = ContactType.Enemy;

			// Attach an EventHandler for the ConnectioNRatingChanged, LoyaltyRatingChanged, DeleteContact, and FileNameChanged Events.
			objContactControl.ConnectionRatingChanged += objEnemy_ConnectionRatingChanged;
			objContactControl.ConnectionGroupRatingChanged += objEnemy_ConnectionGroupRatingChanged;
			objContactControl.LoyaltyRatingChanged += objEnemy_LoyaltyRatingChanged;
			objContactControl.DeleteContact += objEnemy_DeleteContact;
			objContactControl.FileNameChanged += objEnemy_FileNameChanged;

			// Set the ContactControl's Location since scrolling the Panel causes it to actually change the child Controls' Locations.
			objContactControl.Location = new Point(0, objContactControl.Height * i + panEnemies.AutoScrollPosition.Y);
			panEnemies.Controls.Add(objContactControl);

			UpdateCharacterInfo();

			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void cmdAddSpell_Click(object sender, EventArgs e)
		{
			// Count the number of Spells the character currently has and make sure they do not try to select more Spells than they are allowed.
			// The maximum number of Spells a character can start with is 2 x (highest of Spellcasting or Ritual Spellcasting Skill).
			int intSpellCount = 0;
			foreach (TreeNode nodCategory in treSpells.Nodes)
			{
				foreach (TreeNode nodSpell in nodCategory.Nodes)
				{
					intSpellCount++;
				}
			}

			// Run through the list of Active Skills and pick out the two applicable ones.
			int intSkillValue = 0;
			foreach (SkillControl objSkillControl in panActiveSkills.Controls)
			{
				if ((objSkillControl.SkillName == "Spellcasting" || objSkillControl.SkillName == "Ritual Spellcasting") && objSkillControl.SkillRating > intSkillValue)
					intSkillValue = objSkillControl.SkillRating + objSkillControl.SkillObject.RatingModifiers;
			}

			if (intSpellCount >= ((2 * intSkillValue) + _objImprovementManager.ValueOf(Improvement.ImprovementType.SpellLimit)) && !_objCharacter.IgnoreRules)
			{
				MessageBox.Show(LanguageManager.Instance.GetString("Message_SpellLimit"), LanguageManager.Instance.GetString("MessageTitle_SpellLimit"), MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			frmSelectSpell frmPickSpell = new frmSelectSpell(_objCharacter);
			frmPickSpell.ShowDialog(this);
			// Make sure the dialogue window was not canceled.
			if (frmPickSpell.DialogResult == DialogResult.Cancel)
				return;

			// Open the Spells XML file and locate the selected piece.
			XmlDocument objXmlDocument = XmlManager.Instance.Load("spells.xml");

			XmlNode objXmlSpell = objXmlDocument.SelectSingleNode("/chummer/spells/spell[name = \"" + frmPickSpell.SelectedSpell + "\"]");

			Spell objSpell = new Spell(_objCharacter);
			TreeNode objNode = new TreeNode();
			objSpell.Create(objXmlSpell, _objCharacter, objNode, "", frmPickSpell.Limited, frmPickSpell.Extended);
			objNode.ContextMenuStrip = cmsSpell;
			if (objSpell.InternalId == Guid.Empty.ToString())
				return;

			_objCharacter.Spells.Add(objSpell);

			switch (objSpell.Category)
			{
				case "Combat":
					treSpells.Nodes[0].Nodes.Add(objNode);
					treSpells.Nodes[0].Expand();
					break;
				case "Detection":
					treSpells.Nodes[1].Nodes.Add(objNode);
					treSpells.Nodes[1].Expand();
					break;
				case "Health":
					treSpells.Nodes[2].Nodes.Add(objNode);
					treSpells.Nodes[2].Expand();
					break;
				case "Illusion":
					treSpells.Nodes[3].Nodes.Add(objNode);
					treSpells.Nodes[3].Expand();
					break;
				case "Manipulation":
					treSpells.Nodes[4].Nodes.Add(objNode);
					treSpells.Nodes[4].Expand();
					break;
				case "Geomancy Ritual":
					treSpells.Nodes[5].Nodes.Add(objNode);
					treSpells.Nodes[5].Expand();
					break;
			}

			treSpells.SelectedNode = objNode;

			_objFunctions.SortTree(treSpells);
			UpdateCharacterInfo();

			_blnIsDirty = true;
			UpdateWindowTitle();

			if (frmPickSpell.AddAgain)
				cmdAddSpell_Click(sender, e);
		}

		private void cmdDeleteSpell_Click(object sender, EventArgs e)
		{
			// Delete the selected Spell.
			try
			{
				if (treSpells.SelectedNode.Level > 0)
				{
					if (!_objFunctions.ConfirmDelete(LanguageManager.Instance.GetString("Message_DeleteSpell")))
						return;

					// Locate the Spell that is selected in the tree.
					Spell objSpell = _objFunctions.FindSpell(treSpells.SelectedNode.Tag.ToString(), _objCharacter.Spells);

					_objImprovementManager.RemoveImprovements(Improvement.ImprovementSource.Spell, objSpell.InternalId);

					_objCharacter.Spells.Remove(objSpell);
					treSpells.SelectedNode.Remove();
				}
				UpdateCharacterInfo();

				_blnIsDirty = true;
				UpdateWindowTitle();
			}
			catch
			{
			}
		}

		private void cmdAddSpirit_Click(object sender, EventArgs e)
		{
			int i = panSpirits.Controls.Count;

			// The number of bound Spirits cannot exeed the character's CHA.
			if (i >= _objCharacter.CHA.Value && !_objCharacter.IgnoreRules)
			{
				MessageBox.Show(LanguageManager.Instance.GetString("Message_BoundSpiritLimit"), LanguageManager.Instance.GetString("MessageTitle_BoundSpiritLimit"), MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			Spirit objSpirit = new Spirit(_objCharacter);
			_objCharacter.Spirits.Add(objSpirit);

			SpiritControl objSpiritControl = new SpiritControl();
			objSpiritControl.SpiritObject = objSpirit;
			objSpiritControl.EntityType = SpiritType.Spirit;

			// Attach an EventHandler for the ServicesOwedChanged Event.
			objSpiritControl.ServicesOwedChanged += objSpirit_ServicesOwedChanged;
			objSpiritControl.ForceChanged += objSpirit_ForceChanged;
			objSpiritControl.BoundChanged += objSpirit_BoundChanged;
			objSpiritControl.DeleteSpirit += objSpirit_DeleteSpirit;
			objSpiritControl.FileNameChanged += objSpirit_FileNameChanged;

			int intMAG = Convert.ToInt32(_objCharacter.MAG.TotalValue);
			if (_objCharacter.AdeptEnabled && _objCharacter.MagicianEnabled)
			{
				intMAG = _objCharacter.MAGMagician;
			}
			if (_objOptions.SpiritForceBasedOnTotalMAG)
			{
				objSpiritControl.ForceMaximum = _objCharacter.MAG.TotalValue;
				objSpiritControl.Force = _objCharacter.MAG.TotalValue;
			}
			else
			{
				if (intMAG == 0)
					intMAG = 1;

				objSpiritControl.ForceMaximum = intMAG;
				objSpiritControl.Force = intMAG;
			}
			objSpiritControl.RebuildSpiritList(_objCharacter.MagicTradition);

			objSpiritControl.Top = i * objSpiritControl.Height;
			panSpirits.Controls.Add(objSpiritControl);

			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void cmdAddSprite_Click(object sender, EventArgs e)
		{
			int i = panSprites.Controls.Count;

			// The number of registered Sprites cannot exceed the character's CHA.
			if (i >= _objCharacter.CHA.Value && !_objCharacter.IgnoreRules)
			{
				MessageBox.Show(LanguageManager.Instance.GetString("Message_RegisteredSpriteLimit"), LanguageManager.Instance.GetString("MessageTitle_RegisteredSpriteLimit"), MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			Spirit objSprite = new Spirit(_objCharacter);
			_objCharacter.Spirits.Add(objSprite);

			SpiritControl objSpriteControl = new SpiritControl();
			objSpriteControl.SpiritObject = objSprite;
			objSpriteControl.EntityType = SpiritType.Sprite;

			// Attach an EventHandler for the ServicesOwedChanged Event.
			objSpriteControl.ServicesOwedChanged += objSprite_ServicesOwedChanged;
			objSpriteControl.ForceChanged += objSprite_ForceChanged;
			objSpriteControl.BoundChanged += objSprite_BoundChanged;
			objSpriteControl.DeleteSpirit += objSprite_DeleteSpirit;
			objSpriteControl.FileNameChanged += objSprite_FileNameChanged;

			objSpriteControl.ForceMaximum = Convert.ToInt32(nudRES.Value);
			objSpriteControl.Force = Convert.ToInt32(nudRES.Value);
			objSpriteControl.RebuildSpiritList(_objCharacter.TechnomancerStream);

			objSpriteControl.Top = i * objSpriteControl.Height;
			panSprites.Controls.Add(objSpriteControl);

			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void cmdAddPower_Click(object sender, EventArgs e)
		{
			frmSelectPower frmPickPower = new frmSelectPower(_objCharacter);
			frmPickPower.ShowDialog(this);

			// Make sure the dialogue window was not canceled.
			if (frmPickPower.DialogResult == DialogResult.Cancel)
				return;

			int i = panPowers.Controls.Count;

			Power objPower = new Power(_objCharacter);
			_objCharacter.Powers.Add(objPower);

			PowerControl objPowerControl = new PowerControl();
			objPowerControl.PowerObject = objPower;

			// Attach an EventHandler for the PowerRatingChanged Event.
			objPowerControl.PowerRatingChanged += objPower_PowerRatingChanged;
			objPowerControl.DeletePower += objPower_DeletePower;

			objPowerControl.PowerName = frmPickPower.SelectedPower;
			objPowerControl.PointsPerLevel = frmPickPower.PointsPerLevel;
			objPowerControl.LevelEnabled = frmPickPower.LevelEnabled;
			if (frmPickPower.MaxLevels() > 0)
				objPowerControl.MaxLevels = frmPickPower.MaxLevels();

			// Open the Cyberware XML file and locate the selected piece.
			XmlDocument objXmlDocument = XmlManager.Instance.Load("powers.xml");

			XmlNode objXmlPower = objXmlDocument.SelectSingleNode("/chummer/powers/power[name = \"" + frmPickPower.SelectedPower + "\"]");

			objPower.Source = objXmlPower["source"].InnerText;
			objPower.Page = objXmlPower["page"].InnerText;
			if (objXmlPower["doublecost"] != null)
				objPower.DoubleCost = false;

			if (objXmlPower.InnerXml.Contains("bonus"))
			{
				objPower.Bonus = objXmlPower["bonus"];
				if (!_objImprovementManager.CreateImprovements(Improvement.ImprovementSource.Power, objPower.InternalId, objPower.Bonus, false, Convert.ToInt32(objPower.Rating), objPower.DisplayNameShort))
				{
					_objCharacter.Powers.Remove(objPower);
					return;
				}
				objPowerControl.Extra = _objImprovementManager.SelectedValue;
			}

			// Set the control's Maximum.
			objPowerControl.RefreshMaximum(_objCharacter.MAG.TotalValue);
			objPowerControl.Top = i * objPowerControl.Height;
			objPowerControl.RefreshTooltip();
			panPowers.Controls.Add(objPowerControl);

			UpdateCharacterInfo();

			_blnIsDirty = true;
			UpdateWindowTitle();

			if (frmPickPower.AddAgain)
				cmdAddPower_Click(sender, e);
		}

		private void cmdAddCyberware_Click(object sender, EventArgs e)
		{
			// Select the root Cyberware node then open the Select Cyberware window.
			treCyberware.SelectedNode = treCyberware.Nodes[0];
			bool blnAddAgain = PickCyberware();
			if (blnAddAgain)
				cmdAddCyberware_Click(sender, e);
		}

		private void cmdDeleteCyberware_Click(object sender, EventArgs e)
		{
			try
			{
				if (treCyberware.SelectedNode.Level > 0)
				{
					Cyberware objCyberware = new Cyberware(_objCharacter);
					Cyberware objParent = new Cyberware(_objCharacter);
					bool blnFound = false;
					// Locate the piece of Cyberware that is selected in the tree.
					objCyberware = _objFunctions.FindCyberware(treCyberware.SelectedNode.Tag.ToString(), _objCharacter.Cyberware);
					if (objCyberware != null)
					{
						blnFound = true;
						objParent = objCyberware.Parent;
					}

					if (blnFound)
					{
						if (objCyberware.Capacity == "[*]" && treCyberware.SelectedNode.Level == 2 && !_objCharacter.IgnoreRules)
						{
							MessageBox.Show(LanguageManager.Instance.GetString("Message_CannotRemoveCyberware"), LanguageManager.Instance.GetString("MessageTitle_CannotRemoveCyberware"), MessageBoxButtons.OK, MessageBoxIcon.Information);
							return;
						}

						if (objCyberware.SourceType == Improvement.ImprovementSource.Cyberware)
						{
							if (!_objFunctions.ConfirmDelete(LanguageManager.Instance.GetString("Message_DeleteCyberware")))
								return;
						}
						if (objCyberware.SourceType == Improvement.ImprovementSource.Bioware)
						{
							if (!_objFunctions.ConfirmDelete(LanguageManager.Instance.GetString("Message_DeleteBioware")))
								return;
						}

						// Run through the Cyberware's child elements and remove any Improvements and Cyberweapons.
						foreach (Cyberware objChildCyberware in objCyberware.Children)
						{
							_objImprovementManager.RemoveImprovements(objCyberware.SourceType, objChildCyberware.InternalId);
							if (objChildCyberware.WeaponID != Guid.Empty.ToString())
							{
								// Remove the Weapon from the TreeView.
								TreeNode objRemoveNode = new TreeNode();
								foreach (TreeNode objWeaponNode in treWeapons.Nodes[0].Nodes)
								{
									if (objWeaponNode.Tag.ToString() == objChildCyberware.WeaponID)
										objRemoveNode = objWeaponNode;
								}
								treWeapons.Nodes.Remove(objRemoveNode);

								// Remove the Weapon from the Character.
								Weapon objRemoveWeapon = new Weapon(_objCharacter);
								foreach (Weapon objWeapon in _objCharacter.Weapons)
								{
									if (objWeapon.InternalId == objChildCyberware.WeaponID)
										objRemoveWeapon = objWeapon;
								}
								_objCharacter.Weapons.Remove(objRemoveWeapon);
							}
						}
						// Remove the Children.
						objCyberware.Children.Clear();

						// Remove the Cyberweapon created by the Cyberware if applicable.
						if (objCyberware.WeaponID != Guid.Empty.ToString())
						{
							// Remove the Weapon from the TreeView.
							TreeNode objRemoveNode = new TreeNode();
							foreach (TreeNode objWeaponNode in treWeapons.Nodes[0].Nodes)
							{
								if (objWeaponNode.Tag.ToString() == objCyberware.WeaponID)
									objRemoveNode = objWeaponNode;
							}
							treWeapons.Nodes.Remove(objRemoveNode);

							// Remove the Weapon from the Character.
							Weapon objRemoveWeapon = new Weapon(_objCharacter);
							foreach (Weapon objWeapon in _objCharacter.Weapons)
							{
								if (objWeapon.InternalId == objCyberware.WeaponID)
									objRemoveWeapon = objWeapon;
							}
							_objCharacter.Weapons.Remove(objRemoveWeapon);

							// Remove any Gear attached to the Cyberware.
							foreach (Gear objGear in objCyberware.Gear)
								_objFunctions.DeleteGear(objGear, treWeapons, _objImprovementManager);
						}

						// Remove any Gear attached to the Cyberware.
						foreach (Gear objGear in objCyberware.Gear)
							_objFunctions.DeleteGear(objGear, treWeapons, _objImprovementManager);

						// Remove any Improvements created by the piece of Cyberware.
						_objImprovementManager.RemoveImprovements(objCyberware.SourceType, objCyberware.InternalId);
						_objCharacter.Cyberware.Remove(objCyberware);
					}
					else
					{
						// Find and remove the selected piece of Gear.
						Gear objGear = _objFunctions.FindCyberwareGear(treCyberware.SelectedNode.Tag.ToString(), _objCharacter.Cyberware, out objCyberware);
						if (objGear.Parent == null)
							objCyberware.Gear.Remove(objGear);
						else
							objGear.Parent.Children.Remove(objGear);
						_objFunctions.DeleteGear(objGear, treWeapons, _objImprovementManager);
					}

					// Remove the item from the TreeView.
					treCyberware.Nodes.Remove(treCyberware.SelectedNode);

					// If the Parent is populated, remove the item from its Parent.
					if (objParent != null)
						objParent.Children.Remove(objCyberware);
				}
				RefreshSelectedCyberware();
			}
			catch
			{
				return;
			}

			UpdateCharacterInfo();

			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void cmdAddComplexForm_Click(object sender, EventArgs e)
		{
			// The number of Complex Forms cannot exceed twice the character's LOG.
			if (_objCharacter.TechPrograms.Count >= ((_objCharacter.LOG.Value * 2) + _objImprovementManager.ValueOf(Improvement.ImprovementType.ComplexFormLimit)) && !_objCharacter.IgnoreRules)
			{
				MessageBox.Show(LanguageManager.Instance.GetString("Message_ComplexFormLimit"), LanguageManager.Instance.GetString("MessageTitle_ComplexFormLimit"), MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			// Let the user select a Program.
			frmSelectProgram frmPickProgram = new frmSelectProgram(_objCharacter);
			frmPickProgram.ShowDialog(this);

			// Make sure the dialogue window was not canceled.
			if (frmPickProgram.DialogResult == DialogResult.Cancel)
				return;

			XmlDocument objXmlDocument = XmlManager.Instance.Load("programs.xml");

			XmlNode objXmlProgram = objXmlDocument.SelectSingleNode("/chummer/programs/program[name = \"" + frmPickProgram.SelectedProgram + "\"]");

			TreeNode objNode = new TreeNode();
			TechProgram objProgram = new TechProgram(_objCharacter);
			objProgram.Create(objXmlProgram, _objCharacter, objNode);
			if (objProgram.InternalId == Guid.Empty.ToString())
				return;

			_objCharacter.TechPrograms.Add(objProgram);

			if (objProgram.CalculatedCapacity > 0)
				objNode.ContextMenuStrip = cmsComplexForm;

			switch (objProgram.Category)
			{
				case "Advanced":
					treComplexForms.Nodes[0].Nodes.Add(objNode);
					treComplexForms.Nodes[0].Expand();
					break;
				case "ARE Programs":
					treComplexForms.Nodes[1].Nodes.Add(objNode);
					treComplexForms.Nodes[1].Expand();
					break;
				case "Autosoft":
					treComplexForms.Nodes[2].Nodes.Add(objNode);
					treComplexForms.Nodes[2].Expand();
					break;
				case "Common Use":
					treComplexForms.Nodes[3].Nodes.Add(objNode);
					treComplexForms.Nodes[3].Expand();
					break;
				case "Hacking":
					treComplexForms.Nodes[4].Nodes.Add(objNode);
					treComplexForms.Nodes[4].Expand();
					break;
				case "Malware":
					treComplexForms.Nodes[5].Nodes.Add(objNode);
					treComplexForms.Nodes[5].Expand();
					break;
				case "Sensor Software":
					treComplexForms.Nodes[6].Nodes.Add(objNode);
					treComplexForms.Nodes[6].Expand();
					break;
				case "Skillsofts":
					treComplexForms.Nodes[7].Nodes.Add(objNode);
					treComplexForms.Nodes[7].Expand();
					break;
				case "Tactical AR Software":
					treComplexForms.Nodes[8].Nodes.Add(objNode);
					treComplexForms.Nodes[8].Expand();
					break;
			}

			_objFunctions.SortTree(treComplexForms);
			UpdateCharacterInfo();

			_blnIsDirty = true;
			UpdateWindowTitle();

			if (frmPickProgram.AddAgain)
				cmdAddComplexForm_Click(sender, e);
		}

		private void cmdAddArmor_Click(object sender, EventArgs e)
		{
			frmSelectArmor frmPickArmor = new frmSelectArmor(_objCharacter);
			frmPickArmor.ShowDialog(this);

			// Make sure the dialogue window was not canceled.
			if (frmPickArmor.DialogResult == DialogResult.Cancel)
				return;

			// Open the Armor XML file and locate the selected piece.
			XmlDocument objXmlDocument = XmlManager.Instance.Load("armor.xml");

			XmlNode objXmlArmor = objXmlDocument.SelectSingleNode("/chummer/armors/armor[name = \"" + frmPickArmor.SelectedArmor + "\"]");

			TreeNode objNode = new TreeNode();
			Armor objArmor = new Armor(_objCharacter);
			objArmor.Create(objXmlArmor, objNode, cmsArmorMod);
			if (objArmor.InternalId == Guid.Empty.ToString())
				return;

			_objCharacter.Armor.Add(objArmor);

			objNode.ContextMenuStrip = cmsArmor;
			treArmor.Nodes[0].Nodes.Add(objNode);
			treArmor.Nodes[0].Expand();
			treArmor.SelectedNode = objNode;

			UpdateCharacterInfo();

			_blnIsDirty = true;
			UpdateWindowTitle();

			if (frmPickArmor.AddAgain)
				cmdAddArmor_Click(sender, e);
		}

		private void cmdDeleteArmor_Click(object sender, EventArgs e)
		{
			// Delete the selected piece of Armor.
			try
			{
				if (treArmor.SelectedNode.Level == 0)
				{
					if (treArmor.SelectedNode.Text == LanguageManager.Instance.GetString("Node_SelectedArmor"))
						return;

					if (!_objFunctions.ConfirmDelete(LanguageManager.Instance.GetString("Message_DeleteArmorLocation")))
						return;

					// Move all of the child nodes in the current parent to the Selected Armor parent node.
					foreach (TreeNode objNode in treArmor.SelectedNode.Nodes)
					{
						Armor objArmor = _objFunctions.FindArmor(objNode.Tag.ToString(), _objCharacter.Armor);

						// Change the Location for the Armor.
						objArmor.Location = "";

						TreeNode nodNewNode = new TreeNode();
						nodNewNode.Text = objNode.Text;
						nodNewNode.Tag = objNode.Tag;
						nodNewNode.ContextMenuStrip = cmsArmor;

						// Add child nodes.
						foreach (ArmorMod objChild in objArmor.ArmorMods)
						{
							TreeNode nodChildNode = new TreeNode();
							nodChildNode.Text = objChild.DisplayName;
							nodChildNode.Tag = objChild.InternalId;
							nodChildNode.ContextMenuStrip = cmsArmorMod;
							nodNewNode.Nodes.Add(nodChildNode);
							nodNewNode.Expand();
						}

						foreach (Gear objChild in objArmor.Gear)
						{
							TreeNode nodChildNode = new TreeNode();
							nodChildNode.Text = objChild.DisplayName;
							nodChildNode.Tag = objChild.InternalId;
							nodChildNode.ContextMenuStrip = cmsArmorGear;
							nodNewNode.Nodes.Add(nodChildNode);
							nodNewNode.Expand();
						}

						treArmor.Nodes[0].Nodes.Add(nodNewNode);
						treArmor.Nodes[0].Expand();
					}

					// Remove the Location from the character, then remove the selected node.
					_objCharacter.ArmorBundles.Remove(treArmor.SelectedNode.Text);
					treArmor.SelectedNode.Remove();
					return;
				}

				if (!_objFunctions.ConfirmDelete(LanguageManager.Instance.GetString("Message_DeleteArmor")))
					return;

				if (treArmor.SelectedNode.Level == 1)
				{
					Armor objArmor = _objFunctions.FindArmor(treArmor.SelectedNode.Tag.ToString(), _objCharacter.Armor);
					if (objArmor == null)
						return;

					// Remove any Improvements created by the Armor and its children.
					foreach (ArmorMod objMod in objArmor.ArmorMods)
					{
						// Remove the Cyberweapon created by the Mod if applicable.
						if (objMod.WeaponID != Guid.Empty.ToString())
						{
							// Remove the Weapon from the TreeView.
							TreeNode objRemoveNode = new TreeNode();
							foreach (TreeNode objWeaponNode in treWeapons.Nodes[0].Nodes)
							{
								if (objWeaponNode.Tag.ToString() == objMod.WeaponID)
									objRemoveNode = objWeaponNode;
							}
							treWeapons.Nodes.Remove(objRemoveNode);

							// Remove the Weapon from the Character.
							Weapon objRemoveWeapon = new Weapon(_objCharacter);
							foreach (Weapon objWeapon in _objCharacter.Weapons)
							{
								if (objWeapon.InternalId == objMod.WeaponID)
									objRemoveWeapon = objWeapon;
							}
							_objCharacter.Weapons.Remove(objRemoveWeapon);
						}

						_objImprovementManager.RemoveImprovements(Improvement.ImprovementSource.ArmorMod, objMod.InternalId);
					}
					_objImprovementManager.RemoveImprovements(Improvement.ImprovementSource.Armor, objArmor.InternalId);

					// Remove any Improvements created by the Armor's Gear.
					foreach (Gear objGear in objArmor.Gear)
						_objFunctions.DeleteGear(objGear, treWeapons, _objImprovementManager);

					_objCharacter.Armor.Remove(objArmor);
					treArmor.SelectedNode.Remove();
				}
				else if (treArmor.SelectedNode.Level == 2)
				{
					bool blnIsMod = false;
					ArmorMod objMod = _objFunctions.FindArmorMod(treArmor.SelectedNode.Tag.ToString(), _objCharacter.Armor);
					if (objMod != null)
						blnIsMod = true;

					if (blnIsMod)
					{
						// Remove the Cyberweapon created by the Mod if applicable.
						if (objMod.WeaponID != Guid.Empty.ToString())
						{
							// Remove the Weapon from the TreeView.
							TreeNode objRemoveNode = new TreeNode();
							foreach (TreeNode objWeaponNode in treWeapons.Nodes[0].Nodes)
							{
								if (objWeaponNode.Tag.ToString() == objMod.WeaponID)
									objRemoveNode = objWeaponNode;
							}
							treWeapons.Nodes.Remove(objRemoveNode);

							// Remove the Weapon from the Character.
							Weapon objRemoveWeapon = new Weapon(_objCharacter);
							foreach (Weapon objWeapon in _objCharacter.Weapons)
							{
								if (objWeapon.InternalId == objMod.WeaponID)
									objRemoveWeapon = objWeapon;
							}
							_objCharacter.Weapons.Remove(objRemoveWeapon);
						}

						// Remove any Improvements created by the ArmorMod.
						_objImprovementManager.RemoveImprovements(Improvement.ImprovementSource.ArmorMod, objMod.InternalId);
						objMod.Parent.ArmorMods.Remove(objMod);
					}
					else
					{
						Armor objSelectedArmor = new Armor(_objCharacter);
						Gear objGear = _objFunctions.FindArmorGear(treArmor.SelectedNode.Tag.ToString(), _objCharacter.Armor, out objSelectedArmor);
						_objFunctions.DeleteGear(objGear, treWeapons, _objImprovementManager);
						objSelectedArmor.Gear.Remove(objGear);
					}
					treArmor.SelectedNode.Remove();
				}
				else if (treArmor.SelectedNode.Level > 2)
				{
					Armor objSelectedArmor = new Armor(_objCharacter);
					Gear objGear = _objFunctions.FindArmorGear(treArmor.SelectedNode.Tag.ToString(), _objCharacter.Armor, out objSelectedArmor);
					objGear.Parent.Children.Remove(objGear);
					_objFunctions.DeleteGear(objGear, treWeapons, _objImprovementManager);
					objSelectedArmor.Gear.Remove(objGear);
					treArmor.SelectedNode.Remove();
				}
				UpdateCharacterInfo();
				RefreshSelectedArmor();

				_blnIsDirty = true;
				UpdateWindowTitle();
			}
			catch
			{
			}
		}

		private void cmdAddBioware_Click(object sender, EventArgs e)
		{
			// Select the root Bioware node then open the Select Cyberware window.
			treCyberware.SelectedNode = treCyberware.Nodes[1];
			bool blnAddAgain = PickCyberware(Improvement.ImprovementSource.Bioware);
			if (blnAddAgain)
				cmdAddBioware_Click(sender, e);
		}

		private void cmdAddWeapon_Click(object sender, EventArgs e)
		{
			frmSelectWeapon frmPickWeapon = new frmSelectWeapon(_objCharacter);
			frmPickWeapon.ShowDialog(this);

			// Make sure the dialogue window was not canceled.
			if (frmPickWeapon.DialogResult == DialogResult.Cancel)
				return;

			// Open the Weapons XML file and locate the selected piece.
			XmlDocument objXmlDocument = XmlManager.Instance.Load("weapons.xml");

			XmlNode objXmlWeapon = objXmlDocument.SelectSingleNode("/chummer/weapons/weapon[name = \"" + frmPickWeapon.SelectedWeapon + "\"]");

			TreeNode objNode = new TreeNode();
			Weapon objWeapon = new Weapon(_objCharacter);
			objWeapon.Create(objXmlWeapon, _objCharacter, objNode, cmsWeapon, cmsWeaponAccessory, cmsWeaponMod);
			_objCharacter.Weapons.Add(objWeapon);

			objNode.ContextMenuStrip = cmsWeapon;
			treWeapons.Nodes[0].Nodes.Add(objNode);
			treWeapons.Nodes[0].Expand();
			treWeapons.SelectedNode = objNode;

			UpdateCharacterInfo();

			_blnIsDirty = true;
			UpdateWindowTitle();

			if (frmPickWeapon.AddAgain)
				cmdAddWeapon_Click(sender, e);
		}

		private void cmdDeleteWeapon_Click(object sender, EventArgs e)
		{
			// Delete the selected Weapon.
			try
			{
				if (treWeapons.SelectedNode.Level == 0)
				{
					if (treWeapons.SelectedNode.Text == LanguageManager.Instance.GetString("Node_SelectedWeapons"))
						return;

					if (!_objFunctions.ConfirmDelete(LanguageManager.Instance.GetString("Message_DeleteWeaponLocation")))
						return;

					// Move all of the child nodes in the current parent to the Selected Weapons parent node.
					foreach (TreeNode objNode in treWeapons.SelectedNode.Nodes)
					{
						Weapon objWeapon = new Weapon(_objCharacter);
						objWeapon = _objFunctions.FindWeapon(objNode.Tag.ToString(), _objCharacter.Weapons);

						// Change the Location for the Weapon.
						objWeapon.Location = "";
					}

					List<TreeNode> lstMoveNodes = new List<TreeNode>();
					foreach (TreeNode objNode in treWeapons.SelectedNode.Nodes)
						lstMoveNodes.Add(objNode);

					foreach (TreeNode objNode in lstMoveNodes)
					{
						treWeapons.SelectedNode.Nodes.Remove(objNode);
						treWeapons.Nodes[0].Nodes.Add(objNode);
					}

					// Remove the Weapon Location from the character, then remove the selected node.
					_objCharacter.WeaponLocations.Remove(treWeapons.SelectedNode.Text);
					treWeapons.SelectedNode.Remove();
				}

				if (treWeapons.SelectedNode.Level > 0)
				{
					if (!_objFunctions.ConfirmDelete(LanguageManager.Instance.GetString("Message_DeleteWeapon")))
						return;

					if (treWeapons.SelectedNode.Level == 1)
					{
						// Locate the Weapon that is selected in the tree.
						Weapon objWeapon = _objFunctions.FindWeapon(treWeapons.SelectedNode.Tag.ToString(), _objCharacter.Weapons);

						// Cyberweapons cannot be removed through here and must be done by removing the piece of Cyberware.
						if (objWeapon.Category.StartsWith("Cyberware"))
						{
							MessageBox.Show(LanguageManager.Instance.GetString("Message_CannotRemoveCyberweapon"), LanguageManager.Instance.GetString("MessageTitle_CannotRemoveCyberweapon"), MessageBoxButtons.OK, MessageBoxIcon.Information);
							return;
						}
						if (objWeapon.Category == "Gear")
						{
							MessageBox.Show(LanguageManager.Instance.GetString("Message_CannotRemoveGearWeapon"), LanguageManager.Instance.GetString("MessageTitle_CannotRemoveGearWeapon"), MessageBoxButtons.OK, MessageBoxIcon.Information);
							return;
						}
						if (objWeapon.Category.StartsWith("Quality"))
						{
							MessageBox.Show(LanguageManager.Instance.GetString("Message_CannotRemoveQualityWeapon"), LanguageManager.Instance.GetString("MessageTitle_CannotRemoveQualityWeapon"), MessageBoxButtons.OK, MessageBoxIcon.Information);
							return;
						}

						foreach (WeaponAccessory objDelAccessory in objWeapon.WeaponAccessories)
						{
							foreach (Gear objGear in objDelAccessory.Gear)
								_objFunctions.DeleteGear(objGear, treWeapons, _objImprovementManager);
						}
						if (objWeapon.UnderbarrelWeapons.Count > 0)
						{
							foreach (Weapon objUnderbarrelWeapon in objWeapon.UnderbarrelWeapons)
							{
								foreach (WeaponAccessory objDelAccessory in objUnderbarrelWeapon.WeaponAccessories)
								{
									foreach (Gear objGear in objDelAccessory.Gear)
										_objFunctions.DeleteGear(objGear, treWeapons, _objImprovementManager);
								}
							}
						}

						_objCharacter.Weapons.Remove(objWeapon);
						treWeapons.SelectedNode.Remove();
					}
					else
					{
						bool blnAccessory = false;
						// Locate the selected Underbarrel Weapon if applicable.
						foreach (Weapon objCharacterWeapon in _objCharacter.Weapons)
						{
							if (objCharacterWeapon.UnderbarrelWeapons.Count > 0)
							{
								foreach (Weapon objUnderbarrelWeapon in objCharacterWeapon.UnderbarrelWeapons)
								{
									if (objUnderbarrelWeapon.InternalId == treWeapons.SelectedNode.Tag.ToString())
									{
										objCharacterWeapon.UnderbarrelWeapons.Remove(objUnderbarrelWeapon);
										treWeapons.SelectedNode.Remove();
										return;
									}
								}
							}
						}

						Weapon objWeapon = new Weapon(_objCharacter);
						// Locate the Weapon that is selected in the tree.
						foreach (Weapon objCharacterWeapon in _objCharacter.Weapons)
						{
							if (objCharacterWeapon.InternalId == treWeapons.SelectedNode.Parent.Tag.ToString())
							{
								objWeapon = objCharacterWeapon;
								break;
							}
							if (objCharacterWeapon.UnderbarrelWeapons.Count > 0)
							{
								foreach (Weapon objUnderbarrelWeapon in objCharacterWeapon.UnderbarrelWeapons)
								{
									if (objUnderbarrelWeapon.InternalId == treWeapons.SelectedNode.Parent.Tag.ToString())
									{
										objWeapon = objUnderbarrelWeapon;
										break;
									}
								}
							}
						}

						// Locate the Accessory that is selected in the tree.
						WeaponAccessory objAccessory = _objFunctions.FindWeaponAccessory(treWeapons.SelectedNode.Tag.ToString(), _objCharacter.Weapons);
						if (objAccessory != null)
						{
							foreach (Gear objGear in objAccessory.Gear)
								_objFunctions.DeleteGear(objGear, treWeapons, _objImprovementManager);
							objWeapon.WeaponAccessories.Remove(objAccessory);
							treWeapons.SelectedNode.Remove();
							blnAccessory = true;
						}

						if (!blnAccessory)
						{
							// Locate the Mod that is selected in the tree.
							bool blnMod = false;
							WeaponMod objMod = _objFunctions.FindWeaponMod(treWeapons.SelectedNode.Tag.ToString(), _objCharacter.Weapons);
							if (objMod != null)
								blnMod = true;

							if (blnMod)
							{
								objWeapon.WeaponMods.Remove(objMod);
								treWeapons.SelectedNode.Remove();
							}
							else
							{
								// Find the selected Gear.
								Gear objGear = _objFunctions.FindWeaponGear(treWeapons.SelectedNode.Tag.ToString(), _objCharacter.Weapons, out objAccessory);
								_objFunctions.DeleteGear(objGear, treWeapons, _objImprovementManager);
								if (objGear.Parent == null)
									objAccessory.Gear.Remove(objGear);
								else
									objGear.Parent.Children.Remove(objGear);
								treWeapons.SelectedNode.Remove();
							}
						}
					}
				}
				UpdateCharacterInfo();
				RefreshSelectedWeapon();

				_blnIsDirty = true;
				UpdateWindowTitle();
			}
			catch
			{
			}
		}

		private void cmdAddLifestyle_Click(object sender, EventArgs e)
		{
			Lifestyle objLifestyle = new Lifestyle(_objCharacter);
			frmSelectLifestyle frmPickLifestyle = new frmSelectLifestyle(objLifestyle, _objCharacter);
			frmPickLifestyle.ShowDialog(this);

			// Make sure the dialogue window was not canceled.
			if (frmPickLifestyle.DialogResult == DialogResult.Cancel)
				return;

			_objCharacter.Lifestyles.Add(objLifestyle);

			TreeNode objNode = new TreeNode();
			objNode.Text = objLifestyle.DisplayName;
			objNode.Tag = objLifestyle.InternalId;
			objNode.ContextMenuStrip = cmsLifestyleNotes;
			treLifestyles.Nodes[0].Nodes.Add(objNode);
			treLifestyles.Nodes[0].Expand();
			treLifestyles.SelectedNode = objNode;

			UpdateCharacterInfo();

			_blnIsDirty = true;
			UpdateWindowTitle();

			if (frmPickLifestyle.AddAgain)
				cmdAddLifestyle_Click(sender, e);
		}

		private void cmdDeleteLifestyle_Click(object sender, EventArgs e)
		{
			// Delete the selected Lifestyle.
			try
			{
				if (treLifestyles.SelectedNode.Level > 0)
				{
					if (!_objFunctions.ConfirmDelete(LanguageManager.Instance.GetString("Message_DeleteLifestyle")))
						return;

					Lifestyle objLifestyle = _objFunctions.FindLifestyle(treLifestyles.SelectedNode.Tag.ToString(), _objCharacter.Lifestyles);
					if (objLifestyle == null)
						return;

					_objCharacter.Lifestyles.Remove(objLifestyle);
					treLifestyles.SelectedNode.Remove();
				}
				UpdateCharacterInfo();

				_blnIsDirty = true;
				UpdateWindowTitle();
			}
			catch
			{
			}
		}

		private void cmdAddGear_Click(object sender, EventArgs e)
		{
			// Select the root Gear node then open the Select Gear window.
			treGear.SelectedNode = treGear.Nodes[0];
			bool blnAddAgain = PickGear();
			if (blnAddAgain)
				cmdAddGear_Click(sender, e);
			_objController.PopulateFocusList(treFoci);
		}

		private void cmdDeleteGear_Click(object sender, EventArgs e)
		{
			// Delete the selected Gear.
			try
			{
				if (treGear.SelectedNode.Level == 0)
				{
					if (treGear.SelectedNode.Text == LanguageManager.Instance.GetString("Node_SelectedGear"))
						return;

					if (!_objFunctions.ConfirmDelete(LanguageManager.Instance.GetString("Message_DeleteGearLocation")))
						return;

					// Move all of the child nodes in the current parent to the Selected Gear parent node.
					foreach (TreeNode objNode in treGear.SelectedNode.Nodes)
					{
						Gear objGear = new Gear(_objCharacter);
						objGear = _objFunctions.FindGear(objNode.Tag.ToString(), _objCharacter.Gear);

						// Change the Location for the Gear.
						objGear.Location = "";
					}

					List<TreeNode> lstMoveNodes = new List<TreeNode>();
					foreach (TreeNode objNode in treGear.SelectedNode.Nodes)
						lstMoveNodes.Add(objNode);

					foreach (TreeNode objNode in lstMoveNodes)
					{
						treGear.SelectedNode.Nodes.Remove(objNode);
						treGear.Nodes[0].Nodes.Add(objNode);
					}

					// Remove the Location from the character, then remove the selected node.
					_objCharacter.Locations.Remove(treGear.SelectedNode.Text);
					treGear.SelectedNode.Remove();
				}
				if (treGear.SelectedNode.Level > 0)
				{
					if (!_objFunctions.ConfirmDelete(LanguageManager.Instance.GetString("Message_DeleteGear")))
						return;

					Gear objGear = new Gear(_objCharacter);
					objGear = _objFunctions.FindGear(treGear.SelectedNode.Tag.ToString(), _objCharacter.Gear);
					Gear objParent = new Gear(_objCharacter);
					objParent = _objFunctions.FindGear(treGear.SelectedNode.Parent.Tag.ToString(), _objCharacter.Gear);

					_objFunctions.DeleteGear(objGear, treWeapons, _objImprovementManager);

					_objCharacter.Gear.Remove(objGear);
					treGear.SelectedNode.Remove();

					// If the Parent is populated, remove the item from its Parent.
					if (objParent != null)
						objParent.Children.Remove(objGear);
				}
				_objController.PopulateFocusList(treFoci);
				UpdateCharacterInfo();
				RefreshSelectedGear();

				_blnIsDirty = true;
				UpdateWindowTitle();
			}
			catch
			{
			}
		}

		private void cmdAddVehicle_Click(object sender, EventArgs e)
		{
			frmSelectVehicle frmPickVehicle = new frmSelectVehicle(_objCharacter);
			frmPickVehicle.ShowDialog(this);

			// Make sure the dialogue window was not canceled.
			if (frmPickVehicle.DialogResult == DialogResult.Cancel)
				return;

			// Open the Vehicles XML file and locate the selected piece.
			XmlDocument objXmlDocument = XmlManager.Instance.Load("vehicles.xml");

			XmlNode objXmlVehicle = objXmlDocument.SelectSingleNode("/chummer/vehicles/vehicle[name = \"" + frmPickVehicle.SelectedVehicle + "\"]");

			TreeNode objNode = new TreeNode();
			Vehicle objVehicle = new Vehicle(_objCharacter);
			objVehicle.Create(objXmlVehicle, objNode, cmsVehicle, cmsVehicleGear, cmsVehicleWeapon, cmsVehicleWeaponAccessory, cmsVehicleWeaponMod);
			// Update the Used Vehicle information if applicable.
			if (frmPickVehicle.UsedVehicle)
			{
				objVehicle.Avail = frmPickVehicle.UsedAvail;
				objVehicle.Cost = frmPickVehicle.UsedCost.ToString();
			}
			_objCharacter.Vehicles.Add(objVehicle);

			objNode.ContextMenuStrip = cmsVehicle;
			treVehicles.Nodes[0].Nodes.Add(objNode);
			treVehicles.Nodes[0].Expand();
			treVehicles.SelectedNode = objNode;

			UpdateCharacterInfo();
			RefreshSelectedVehicle();

			_blnIsDirty = true;
			UpdateWindowTitle();

			if (frmPickVehicle.AddAgain)
				cmdAddVehicle_Click(sender, e);
		}

		private void cmdDeleteVehicle_Click(object sender, EventArgs e)
		{
			// Delete the selected Vehicle.
			try
			{
				if (treVehicles.SelectedNode.Level == 0)
					return;
			}
			catch
			{
				return;
			}

			if (treVehicles.SelectedNode.Level != 2)
			{
				if (!_objFunctions.ConfirmDelete(LanguageManager.Instance.GetString("Message_DeleteVehicle")))
					return;
			}

			if (treVehicles.SelectedNode.Level == 1)
			{
				// Locate the Vehicle that is selected in the tree.
				Vehicle objVehicle = _objFunctions.FindVehicle(treVehicles.SelectedNode.Tag.ToString(), _objCharacter.Vehicles);

				// Remove any Gear Improvements from the character (primarily those provided by an Emotitoy).
				foreach (Gear objGear in objVehicle.Gear)
					_objImprovementManager.RemoveImprovements(Improvement.ImprovementSource.Gear, objGear.InternalId);

				_objCharacter.Vehicles.Remove(objVehicle);
				treVehicles.SelectedNode.Remove();
			}
			else if (treVehicles.SelectedNode.Level == 2)
			{
				bool blnFound = false;
				// Locate the VehicleMod that is selected in the tree.
				Vehicle objFoundVehicle = new Vehicle(_objCharacter);
				VehicleMod objMod = _objFunctions.FindVehicleMod(treVehicles.SelectedNode.Tag.ToString(), _objCharacter.Vehicles, out objFoundVehicle);
				if (objMod != null)
				{
					blnFound = true;

					if (!_objFunctions.ConfirmDelete(LanguageManager.Instance.GetString("Message_DeleteVehicle")))
						return;

					// Check for Improved Sensor bonus.
					if (objMod.Bonus != null)
					{
						if (objMod.Bonus["improvesensor"] != null)
						{
							ChangeVehicleSensor(objFoundVehicle, false);
						}
					}

					// If this is the Obsolete Mod, the user must select a percentage. This will create an Expense that costs X% of the Vehicle's base cost to remove the special Obsolete Mod.
					if (objMod.Name == "Obsolete" || (objMod.Name == "Obsolescent" && _objOptions.AllowObsolescentUpgrade))
					{
						frmSelectNumber frmModPercent = new frmSelectNumber();
						frmModPercent.Minimum = 0;
						frmModPercent.Maximum = 1000;
						frmModPercent.Description = LanguageManager.Instance.GetString("String_Retrofit");
						frmModPercent.ShowDialog(this);

						if (frmModPercent.DialogResult == DialogResult.Cancel)
							return;

						int intPercentage = frmModPercent.SelectedValue;
						int intVehicleCost = Convert.ToInt32(objFoundVehicle.Cost);

						// Make sure the character has enough Nuyen for the expense.
						int intCost = Convert.ToInt32(Convert.ToDouble(intVehicleCost, GlobalOptions.Instance.CultureInfo) * (Convert.ToDouble(intPercentage, GlobalOptions.Instance.CultureInfo) / 100.0), GlobalOptions.Instance.CultureInfo);
						VehicleMod objRetrofit = new VehicleMod(_objCharacter);

						XmlDocument objVehiclesDoc = XmlManager.Instance.Load("vehicles.xml");
						XmlNode objXmlNode = objVehiclesDoc.SelectSingleNode("/chummer/mods/mod[name = \"Retrofit\"]");
						TreeNode objTreeNode = new TreeNode();
						objRetrofit.Create(objXmlNode, objTreeNode, 0);
						objRetrofit.Cost = intCost.ToString();
						objFoundVehicle.Mods.Add(objRetrofit);
						treVehicles.SelectedNode.Parent.Nodes.Add(objTreeNode);
					}

					objFoundVehicle.Mods.Remove(objMod);
					treVehicles.SelectedNode.Remove();
				}

				if (!blnFound)
				{
					// Locate the Sensor or Ammunition that is selected in the tree.
					foreach (Vehicle objCharacterVehicle in _objCharacter.Vehicles)
					{
						foreach (Gear objGear in objCharacterVehicle.Gear)
						{
							if (objGear.InternalId == treVehicles.SelectedNode.Tag.ToString())
							{
								if (!_objFunctions.ConfirmDelete(LanguageManager.Instance.GetString("Message_DeleteVehicle")))
									return;

								// Remove the Gear Weapon created by the Gear if applicable.
								if (objGear.WeaponID != Guid.Empty.ToString())
								{
									// Remove the Weapon from the TreeView.
									foreach (TreeNode objWeaponNode in treVehicles.SelectedNode.Parent.Nodes)
									{
										if (objWeaponNode.Tag.ToString() == objGear.WeaponID)
										{
											treVehicles.SelectedNode.Parent.Nodes.Remove(objWeaponNode);
											break;
										}
									}

									// Remove the Weapon from the Vehicle.
									Weapon objRemoveWeapon = new Weapon(_objCharacter);
									foreach (Weapon objWeapon in objCharacterVehicle.Weapons)
									{
										if (objWeapon.InternalId == objGear.WeaponID)
											objRemoveWeapon = objWeapon;
									}
									objCharacterVehicle.Weapons.Remove(objRemoveWeapon);
								}

								objCharacterVehicle.Gear.Remove(objGear);
								treVehicles.SelectedNode.Remove();
								blnFound = true;
								break;
							}
						}
					}
				}

				if (!blnFound)
				{
					// Locate the Weapon that is selected in the tree.
					foreach (Vehicle objCharacterVehicle in _objCharacter.Vehicles)
					{
						foreach (Weapon objWeapon in objCharacterVehicle.Weapons)
						{
							if (objWeapon.InternalId == treVehicles.SelectedNode.Tag.ToString())
							{
								blnFound = true;
								MessageBox.Show(LanguageManager.Instance.GetString("Message_CannotRemoveGearWeaponVehicle"), LanguageManager.Instance.GetString("MessageTitle_CannotRemoveGearWeapon"), MessageBoxButtons.OK, MessageBoxIcon.Information);
								break;
							}
						}
					}
				}

				if (!blnFound)
				{
					// This must be a Location, so find it.
					TreeNode objVehicleNode = treVehicles.SelectedNode.Parent;
					Vehicle objVehicle = _objFunctions.FindVehicle(objVehicleNode.Tag.ToString(), _objCharacter.Vehicles);

					if (!_objFunctions.ConfirmDelete(LanguageManager.Instance.GetString("Message_DeleteVehicleLocation")))
						return;

					// Change the Location of the Gear.
					foreach (Gear objGear in objVehicle.Gear)
					{
						if (objGear.Location == treVehicles.SelectedNode.Text)
							objGear.Location = "";
					}

					// Move all of the child nodes in the current parent to the Vehicle.
					List<TreeNode> lstMoveNodes = new List<TreeNode>();
					foreach (TreeNode objNode in treVehicles.SelectedNode.Nodes)
						lstMoveNodes.Add(objNode);

					foreach (TreeNode objNode in lstMoveNodes)
					{
						treVehicles.SelectedNode.Nodes.Remove(objNode);
						objVehicleNode.Nodes.Add(objNode);
					}

					// Remove the Location from the Vehicle, then remove the selected node.
					objVehicle.Locations.Remove(treVehicles.SelectedNode.Text);
					treVehicles.SelectedNode.Remove();
				}
			}
			else if (treVehicles.SelectedNode.Level == 3)
			{
				bool blnFound = false;
				// Locate the selected VehicleWeapon that is selected in the tree.
				foreach (Vehicle objCharacterVehicle in _objCharacter.Vehicles)
				{
					foreach (VehicleMod objMod in objCharacterVehicle.Mods)
					{
						foreach (Weapon objWeapon in objMod.Weapons)
						{
							if (objWeapon.InternalId == treVehicles.SelectedNode.Tag.ToString())
							{
								objMod.Weapons.Remove(objWeapon);
								treVehicles.SelectedNode.Remove();
								blnFound = true;
								break;
							}
						}
					}
				}

				if (!blnFound)
				{
					// Locate the selected Sensor Plugin.
					// Locate the Sensor that is selected in the tree.
					Vehicle objFoundVehicle = new Vehicle(_objCharacter);
					Gear objGear = _objFunctions.FindVehicleGear(treVehicles.SelectedNode.Tag.ToString(), _objCharacter.Vehicles, out objFoundVehicle);
					if (objGear != null)
					{
						blnFound = true;
						objGear.Parent.Children.Remove(objGear);
						treVehicles.SelectedNode.Remove();

						_objFunctions.DeleteVehicleGear(objGear, treVehicles, objFoundVehicle);
					}
				}

				if (!blnFound)
				{
					// Locate the selected Cyberware.
					foreach (Vehicle objCharacterVehicle in _objCharacter.Vehicles)
					{
						foreach (VehicleMod objMod in objCharacterVehicle.Mods)
						{
							foreach (Cyberware objCyberware in objMod.Cyberware)
							{
								if (objCyberware.InternalId == treVehicles.SelectedNode.Tag.ToString())
								{
									// Remove the Cyberweapon created by the Cyberware if applicable.
									if (objCyberware.WeaponID != Guid.Empty.ToString())
									{
										// Remove the Weapon from the TreeView.
										TreeNode objRemoveNode = new TreeNode();
										foreach (TreeNode objWeaponNode in treVehicles.SelectedNode.Parent.Parent.Nodes)
										{
											if (objWeaponNode.Tag.ToString() == objCyberware.WeaponID)
												objRemoveNode = objWeaponNode;
										}
										treWeapons.Nodes.Remove(objRemoveNode);

										// Remove the Weapon from the Vehicle.
										Weapon objRemoveWeapon = new Weapon(_objCharacter);
										foreach (Weapon objWeapon in objCharacterVehicle.Weapons)
										{
											if (objWeapon.InternalId == objCyberware.WeaponID)
												objRemoveWeapon = objWeapon;
										}
										objCharacterVehicle.Weapons.Remove(objRemoveWeapon);
									}

									objMod.Cyberware.Remove(objCyberware);
									treVehicles.SelectedNode.Remove();
									break;
								}
							}
						}
					}
				}
			}
			else if (treVehicles.SelectedNode.Level == 4)
			{
				bool blnFound = false;
				// Locate the selected WeaponAccessory or VehicleWeaponMod that is selected in the tree.
				foreach (Vehicle objCharacterVehicle in _objCharacter.Vehicles)
				{
					foreach (VehicleMod objMod in objCharacterVehicle.Mods)
					{
						foreach (Weapon objWeapon in objMod.Weapons)
						{
							foreach (WeaponAccessory objAccessory in objWeapon.WeaponAccessories)
							{
								if (objAccessory.InternalId == treVehicles.SelectedNode.Tag.ToString())
								{
									objWeapon.WeaponAccessories.Remove(objAccessory);
									treVehicles.SelectedNode.Remove();
									blnFound = true;
									break;
								}
							}
							if (!blnFound)
							{
								foreach (WeaponMod objWeaponMod in objWeapon.WeaponMods)
								{
									if (objWeaponMod.InternalId == treVehicles.SelectedNode.Tag.ToString())
									{
										objWeapon.WeaponMods.Remove(objWeaponMod);
										treVehicles.SelectedNode.Remove();
										blnFound = true;
										break;
									}
								}
							}
							if (!blnFound)
							{
								// Remove the Underbarrel Weapon if the selected item it is one.
								if (objWeapon.UnderbarrelWeapons.Count > 0)
								{
									foreach (Weapon objUnderbarrelWeapon in objWeapon.UnderbarrelWeapons)
									{
										if (objUnderbarrelWeapon.InternalId == treVehicles.SelectedNode.Tag.ToString())
										{
											objWeapon.UnderbarrelWeapons.Remove(objUnderbarrelWeapon);
											treVehicles.SelectedNode.Remove();
											break;
										}
									}
								}
							}
						}
					}
				}

				if (!blnFound)
				{
					// Locate the selected Sensor Plugin.
					// Locate the Sensor that is selected in the tree.
					Vehicle objFoundVehicle = new Vehicle(_objCharacter);
					Gear objGear = _objFunctions.FindVehicleGear(treVehicles.SelectedNode.Tag.ToString(), _objCharacter.Vehicles, out objFoundVehicle);
					if (objGear != null)
					{
						blnFound = true;
						objGear.Parent.Children.Remove(objGear);
						treVehicles.SelectedNode.Remove();

						_objFunctions.DeleteVehicleGear(objGear, treVehicles, objFoundVehicle);
					}
				}
			}
			else if (treVehicles.SelectedNode.Level == 5)
			{
				// Locate the selected WeaponAccessory or VehicleWeaponMod that is selected in the tree.
				bool blnFound = false;
				foreach (Vehicle objCharacterVehicle in _objCharacter.Vehicles)
				{
					foreach (VehicleMod objMod in objCharacterVehicle.Mods)
					{
						foreach (Weapon objWeapon in objMod.Weapons)
						{
							if (objWeapon.UnderbarrelWeapons.Count > 0)
							{
								foreach (Weapon objUnderbarrelWeapon in objWeapon.UnderbarrelWeapons)
								{
									foreach (WeaponAccessory objAccessory in objUnderbarrelWeapon.WeaponAccessories)
									{
										if (objAccessory.InternalId == treVehicles.SelectedNode.Tag.ToString())
										{
											objUnderbarrelWeapon.WeaponAccessories.Remove(objAccessory);
											treVehicles.SelectedNode.Remove();
											blnFound = true;
											break;
										}
									}
									if (!blnFound)
									{
										foreach (WeaponMod objWeaponMod in objUnderbarrelWeapon.WeaponMods)
										{
											if (objWeaponMod.InternalId == treVehicles.SelectedNode.Tag.ToString())
											{
												objUnderbarrelWeapon.WeaponMods.Remove(objWeaponMod);
												treVehicles.SelectedNode.Remove();
												blnFound = true;
												break;
											}
										}
									}
								}
							}
						}
					}
				}

				if (!blnFound)
				{
					Vehicle objFoundVehicle = new Vehicle(_objCharacter);
					Gear objGear = _objFunctions.FindVehicleGear(treVehicles.SelectedNode.Tag.ToString(), _objCharacter.Vehicles, out objFoundVehicle);
					if (objGear != null)
					{
						blnFound = true;
						objGear.Parent.Children.Remove(objGear);
						treVehicles.SelectedNode.Remove();

						_objFunctions.DeleteVehicleGear(objGear, treVehicles, objFoundVehicle);
					}
				}
			}
			else if (treVehicles.SelectedNode.Level > 5)
			{
				Vehicle objFoundVehicle = new Vehicle(_objCharacter);
				Gear objGear = _objFunctions.FindVehicleGear(treVehicles.SelectedNode.Tag.ToString(), _objCharacter.Vehicles, out objFoundVehicle);
				if (objGear != null)
				{
					objGear.Parent.Children.Remove(objGear);
					treVehicles.SelectedNode.Remove();

					_objFunctions.DeleteVehicleGear(objGear, treVehicles, objFoundVehicle);
				}
			}

			UpdateCharacterInfo();
			RefreshSelectedVehicle();

			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void cmdAddMartialArt_Click(object sender, EventArgs e)
		{
			// Make sure that adding a new Martial Art will not take the user over 35 BP for Positive Qualities.
			if (!_objCharacter.IgnoreRules)
			{
				// Count the BP used by Positive Qualities.
				int intBP = 0;
				foreach (Quality objQuality in _objCharacter.Qualities)
				{
					if (objQuality.Type == QualityType.Positive && objQuality.ContributeToLimit)
						intBP += objQuality.BP;
				}
				// Include free Positive Quality Improvements.
				intBP -= _objImprovementManager.ValueOf(Improvement.ImprovementType.FreePositiveQualities);

				if (_objCharacter.BuildMethod == CharacterBuildMethod.Karma)
					intBP *= _objOptions.KarmaQuality;

				// Add 5 which is the cost for a new Martial Art at Rating 1.
				if (_objCharacter.BuildMethod == CharacterBuildMethod.BP)
					intBP += 5;
				else
					intBP += (5 * _objOptions.KarmaQuality);

				foreach (MartialArt objCharacterMartialArt in _objCharacter.MartialArts)
				{
					if (_objCharacter.BuildMethod == CharacterBuildMethod.BP)
						intBP += objCharacterMartialArt.Rating * 5;
					else
						intBP += objCharacterMartialArt.Rating * 5 * _objOptions.KarmaQuality;
				}

				if (!_objOptions.ExceedPositiveQualities)
				{
					if (_objCharacter.BuildMethod == CharacterBuildMethod.BP)
					{
						if (intBP > 35 && !_objCharacter.IgnoreRules)
						{
							MessageBox.Show(LanguageManager.Instance.GetString("Message_PositiveQualityLimit").Replace("{0}", "35 " + LanguageManager.Instance.GetString("String_BP")), LanguageManager.Instance.GetString("MessageTitle_PositiveQualityLimit"), MessageBoxButtons.OK, MessageBoxIcon.Information);
							return;
						}
					}
					else
					{
						if (intBP > 70 && !_objCharacter.IgnoreRules)
						{
							MessageBox.Show(LanguageManager.Instance.GetString("Message_PositiveQualityLimit").Replace("{0}", "70 " + LanguageManager.Instance.GetString("String_Karma")), LanguageManager.Instance.GetString("MessageTitle_PositiveQualityLimit"), MessageBoxButtons.OK, MessageBoxIcon.Information);
							return;
						}
					}
				}
			}

			frmSelectMartialArt frmPickMartialArt = new frmSelectMartialArt(_objCharacter);
			frmPickMartialArt.ShowDialog(this);

			if (frmPickMartialArt.DialogResult == DialogResult.Cancel)
				return;

			// Open the Martial Arts XML file and locate the selected piece.
			XmlDocument objXmlDocument = XmlManager.Instance.Load("martialarts.xml");

			XmlNode objXmlArt = objXmlDocument.SelectSingleNode("/chummer/martialarts/martialart[name = \"" + frmPickMartialArt.SelectedMartialArt + "\"]");

			TreeNode objNode = new TreeNode();
			MartialArt objMartialArt = new MartialArt(_objCharacter);
			objMartialArt.Create(objXmlArt, objNode, _objCharacter);
			_objCharacter.MartialArts.Add(objMartialArt);

			objNode.ContextMenuStrip = cmsMartialArts;

			treMartialArts.Nodes[0].Nodes.Add(objNode);
			treMartialArts.Nodes[0].Expand();

			treMartialArts.SelectedNode = objNode;

			_objFunctions.SortTree(treMartialArts);
			CalculateBP();

			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void cmdDeleteMartialArt_Click(object sender, EventArgs e)
		{
			try
			{
				if (treMartialArts.SelectedNode.Level == 0)
					return;

				if (!_objFunctions.ConfirmDelete(LanguageManager.Instance.GetString("Message_DeleteMartialArt")))
					return;

				if (treMartialArts.SelectedNode.Level == 1)
				{
					if (treMartialArts.SelectedNode.Parent == treMartialArts.Nodes[0])
					{
						// Characters may only have 2 Maneuvers per Martial Art Rating (start at -2 since we're potentially removing one).
						int intTotalRating = -2;
						foreach (MartialArt objCharacterMartialArt in _objCharacter.MartialArts)
							intTotalRating += objCharacterMartialArt.Rating * 2;

						if (treMartialArts.Nodes[1].Nodes.Count > intTotalRating && !_objCharacter.IgnoreRules)
						{
							MessageBox.Show(LanguageManager.Instance.GetString("Message_MartialArtManeuverLimit"), LanguageManager.Instance.GetString("MessageTitle_MartialArtManeuverLimit"), MessageBoxButtons.OK, MessageBoxIcon.Information);
							return;
						}

						// Delete the selected Martial Art.
						MartialArt objMartialArt = _objFunctions.FindMartialArt(treMartialArts.SelectedNode.Tag.ToString(), _objCharacter.MartialArts);

						// Remove the Improvements for any Advantages for the Martial Art that is being removed.
						foreach (MartialArtAdvantage objAdvantage in objMartialArt.Advantages)
						{
							_objImprovementManager.RemoveImprovements(Improvement.ImprovementSource.MartialArtAdvantage, objAdvantage.InternalId);
						}

						_objCharacter.MartialArts.Remove(objMartialArt);
						treMartialArts.SelectedNode.Remove();
					}
					else
					{
						// Delete the selected Martial Art Maenuver.
						MartialArtManeuver objManeuver = _objFunctions.FindMartialArtManeuver(treMartialArts.SelectedNode.Tag.ToString(), _objCharacter.MartialArtManeuvers);

						_objCharacter.MartialArtManeuvers.Remove(objManeuver);
						treMartialArts.SelectedNode.Remove();
					}

					CalculateBP();
					UpdateCharacterInfo();

					_blnIsDirty = true;
					UpdateWindowTitle();
				}
				if (treMartialArts.SelectedNode.Level == 2)
				{
					// Find the selected Advantage object.
					MartialArt objSelectedMartialArt = new MartialArt(_objCharacter);
					MartialArtAdvantage objSelectedAdvantage = _objFunctions.FindMartialArtAdvantage(treMartialArts.SelectedNode.Tag.ToString(), _objCharacter.MartialArts, out objSelectedMartialArt);

					_objImprovementManager.RemoveImprovements(Improvement.ImprovementSource.MartialArtAdvantage, objSelectedAdvantage.InternalId);
					treMartialArts.SelectedNode.Remove();

					objSelectedMartialArt.Advantages.Remove(objSelectedAdvantage);

					CalculateBP();
					UpdateCharacterInfo();

					_blnIsDirty = true;
					UpdateWindowTitle();
				}
			}
			catch
			{
			}
		}

		private void cmdAddManeuver_Click(object sender, EventArgs e)
		{
			// Characters may only have 2 Maneuvers per Martial Art Rating.
			int intTotalRating = 0;
			foreach (MartialArt objMartialArt in _objCharacter.MartialArts)
				intTotalRating += objMartialArt.Rating * 2;

			if (treMartialArts.Nodes[1].Nodes.Count >= intTotalRating && !_objCharacter.IgnoreRules)
			{
				MessageBox.Show(LanguageManager.Instance.GetString("Message_MartialArtManeuverLimit"), LanguageManager.Instance.GetString("MessageTitle_MartialArtManeuverLimit"), MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			frmSelectMartialArtManeuver frmPickMartialArtManeuver = new frmSelectMartialArtManeuver(_objCharacter);
			frmPickMartialArtManeuver.ShowDialog(this);

			if (frmPickMartialArtManeuver.DialogResult == DialogResult.Cancel)
				return;

			// Open the Martial Arts XML file and locate the selected piece.
			XmlDocument objXmlDocument = XmlManager.Instance.Load("martialarts.xml");

			XmlNode objXmlManeuver = objXmlDocument.SelectSingleNode("/chummer/maneuvers/maneuver[name = \"" + frmPickMartialArtManeuver.SelectedManeuver + "\"]");

			TreeNode objNode = new TreeNode();
			MartialArtManeuver objManeuver = new MartialArtManeuver(_objCharacter);
			objManeuver.Create(objXmlManeuver, objNode);
			objNode.ContextMenuStrip = cmsMartialArtManeuver;
			_objCharacter.MartialArtManeuvers.Add(objManeuver);

			treMartialArts.Nodes[1].Nodes.Add(objNode);
			treMartialArts.Nodes[1].Expand();

			treMartialArts.SelectedNode = objNode;

			CalculateBP();
			_objFunctions.SortTree(treMartialArts);
			UpdateCharacterInfo();

			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void cmdAddMugshot_Click(object sender, EventArgs e)
		{
			// Prompt the user to select an image to associate with this character.
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Filter = "All Files (*.*)|*.*";

			if (openFileDialog.ShowDialog(this) == DialogResult.OK)
			{
				MemoryStream objStream = new MemoryStream();
				// Convert the image to a string usinb Base64.
				Image imgMugshot = new Bitmap(openFileDialog.FileName);
				imgMugshot.Save(objStream, imgMugshot.RawFormat);
				string strResult = Convert.ToBase64String(objStream.ToArray());

				_objCharacter.Mugshot = strResult;
				picMugshot.Image = imgMugshot;

				objStream.Close();

				_blnIsDirty = true;
				UpdateWindowTitle();
			}
		}

		private void cmdDeleteMugshot_Click(object sender, EventArgs e)
		{
			_objCharacter.Mugshot = "";
			picMugshot.Image = null;
			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void cmdAddMetamagic_Click(object sender, EventArgs e)
		{
			// Character can only have a number of Metamagics/Echoes equal to their Initiate Grade. Additional ones cost Karma.
			bool blnPayWithKarma = false;
			int intCount = 0;

			// Count the number of free Metamagics the character has and compare it to their Initiate Grade. If they have reached their limit, a new one is added for Karma.
			foreach (Metamagic objCharacterMetamagic in _objCharacter.Metamagics)
			{
				if (!objCharacterMetamagic.PaidWithKarma)
					intCount++;
			}

			// Make sure the character has Initiated.
			if (Convert.ToInt32(lblInitiateGrade.Text) == 0)
			{
				if (_objCharacter.MAGEnabled)
					MessageBox.Show(LanguageManager.Instance.GetString("Message_CannotSelectMetamagic"), LanguageManager.Instance.GetString("MessageTitle_CannotSelectMetamagic"), MessageBoxButtons.OK, MessageBoxIcon.Information);
				else if (_objCharacter.RESEnabled)
					MessageBox.Show(LanguageManager.Instance.GetString("Message_CannotSelectEcho"), LanguageManager.Instance.GetString("MessageTitle_CannotSelectEcho"), MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			// Character can only have MAG/RES + Grade Metamagics. Do not let them go beyond this limit.
			if (intCount + 1 > Convert.ToInt32(lblInitiateGrade.Text))
			{
				string strMessage = "";
				string strTitle = "";
				int intGrade = Convert.ToInt32(lblInitiateGrade.Text);
				int intAttribute = 0;

				if (_objCharacter.MAGEnabled)
					intAttribute = _objCharacter.MAG.TotalValue;
				else
					intAttribute = _objCharacter.RES.TotalValue;

				if (intCount + 1 > intAttribute + intGrade)
				{
					if (_objCharacter.MAGEnabled)
					{
						strMessage = LanguageManager.Instance.GetString("Message_AdditionalMetamagicLimit");
						strTitle = LanguageManager.Instance.GetString("MessageTitle_AdditionalMetamagic");
					}
					else if (_objCharacter.RESEnabled)
					{
						strMessage = LanguageManager.Instance.GetString("Message_AdditionalEchoLimit");
						strTitle = LanguageManager.Instance.GetString("MessageTitle_CannotSelectEcho");
					}

					MessageBox.Show(strMessage, strTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
					return;
				}
			}

			// Additional Metamagics beyond the standard 1 per Grade cost additional Karma, so ask if the user wants to spend the additional Karma.
			if (intCount >= Convert.ToInt32(lblInitiateGrade.Text))
			{
				string strMessage = "";
				string strTitle = "";
				if (_objCharacter.MAGEnabled)
				{
					strMessage = LanguageManager.Instance.GetString("Message_AdditionalMetamagic").Replace("{0}", _objOptions.KarmaMetamagic.ToString());
					strTitle = LanguageManager.Instance.GetString("MessageTitle_AdditionalMetamagic");
				}
				else if (_objCharacter.RESEnabled)
				{
					strMessage = LanguageManager.Instance.GetString("Message_AdditionalEcho").Replace("{0}", _objOptions.KarmaMetamagic.ToString());
					strTitle = LanguageManager.Instance.GetString("MessageTitle_AdditionalEcho");
				}

				if (MessageBox.Show(strMessage, strTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
					return;
				else
					blnPayWithKarma = true;
			}

			frmSelectMetamagic frmPickMetamagic = new frmSelectMetamagic(_objCharacter);
			if (_objCharacter.RESEnabled)
				frmPickMetamagic.WindowMode = frmSelectMetamagic.Mode.Echo;
			frmPickMetamagic.ShowDialog(this);

			// Make sure a value was selected.
			if (frmPickMetamagic.DialogResult == DialogResult.Cancel)
				return;

			string strMetamagic = frmPickMetamagic.SelectedMetamagic;

			XmlDocument objXmlDocument = new XmlDocument();
			XmlNode objXmlMetamagic;

			TreeNode objNode = new TreeNode();
			Metamagic objMetamagic = new Metamagic(_objCharacter);
			Improvement.ImprovementSource objSource;

			if (_objCharacter.MAGEnabled)
			{
				objXmlDocument = XmlManager.Instance.Load("metamagic.xml");
				objXmlMetamagic = objXmlDocument.SelectSingleNode("/chummer/metamagics/metamagic[name = \"" + strMetamagic + "\"]");
				objSource = Improvement.ImprovementSource.Metamagic;
			}
			else
			{
				objXmlDocument = XmlManager.Instance.Load("echoes.xml");
				objXmlMetamagic = objXmlDocument.SelectSingleNode("/chummer/echoes/echo[name = \"" + strMetamagic + "\"]");
				objSource = Improvement.ImprovementSource.Echo;
			}

			objMetamagic.Create(objXmlMetamagic, _objCharacter, objNode, objSource);
			objNode.ContextMenuStrip = cmsMetamagic;
			if (objMetamagic.InternalId == Guid.Empty.ToString())
				return;

			objMetamagic.PaidWithKarma = blnPayWithKarma;
			_objCharacter.Metamagics.Add(objMetamagic);

			treMetamagic.Nodes.Add(objNode);

			UpdateCharacterInfo();

			_blnIsDirty = true;
			UpdateWindowTitle();

			if (frmPickMetamagic.AddAgain)
				cmdAddMetamagic_Click(sender, e);
		}

		private void cmdDeleteMetamagic_Click(object sender, EventArgs e)
		{
			try
			{
				if (treMetamagic.SelectedNode.Level == 0)
				{
					string strMessage = "";
					if (_objCharacter.MAGEnabled)
						strMessage = LanguageManager.Instance.GetString("Message_DeleteMetamagic");
					else if (_objCharacter.RESEnabled)
						strMessage = LanguageManager.Instance.GetString("Message_DeleteEcho");
					if (!_objFunctions.ConfirmDelete(strMessage))
						return;

					// Locate the selected Metamagic.
					Metamagic objMetamagic = _objFunctions.FindMetamagic(treMetamagic.SelectedNode.Tag.ToString(), _objCharacter.Metamagics);

					// Remove the Improvements created by the Metamagic.
					_objImprovementManager.RemoveImprovements(objMetamagic.SourceType, objMetamagic.InternalId);

					// Remove the Metamagic from the character.
					_objCharacter.Metamagics.Remove(objMetamagic);

					treMetamagic.SelectedNode.Remove();

					UpdateCharacterInfo();

					_blnIsDirty = true;
					UpdateWindowTitle();
				}
			}
			catch
			{
			}
		}

		private void cmdAddExoticSkill_Click(object sender, EventArgs e)
		{
			frmSelectExoticSkill frmPickExoticSkill = new frmSelectExoticSkill();
			frmPickExoticSkill.ShowDialog(this);

			if (frmPickExoticSkill.DialogResult == DialogResult.Cancel)
				return;

			XmlDocument objXmlDocument = XmlManager.Instance.Load("skills.xml");

			XmlNode nodSkill = objXmlDocument.SelectSingleNode("/chummer/skills/skill[name = \"" + frmPickExoticSkill.SelectedExoticSkill + "\"]");

			int i = panActiveSkills.Controls.Count;
			Skill objSkill = new Skill(_objCharacter);
			objSkill.Attribute = nodSkill["attribute"].InnerText;
			if (_objCharacter.MaxSkillRating > 0)
				objSkill.RatingMaximum = _objCharacter.MaxSkillRating;

			SkillControl objSkillControl = new SkillControl();
			objSkillControl.SkillObject = objSkill;
			objSkillControl.Width = 510;

			// Attach an EventHandler for the RatingChanged and SpecializationChanged Events.
			objSkillControl.RatingChanged += objActiveSkill_RatingChanged;
			objSkillControl.SpecializationChanged += objSkill_SpecializationChanged;
			objSkillControl.SkillName = frmPickExoticSkill.SelectedExoticSkill;

			objSkillControl.SkillCategory = nodSkill["category"].InnerText;
			if (nodSkill["default"].InnerText == "Yes")
				objSkill.Default = true;
			else
				objSkill.Default = false;

			objSkill.ExoticSkill = true;
			_objCharacter.Skills.Add(objSkill);

			// Populate the Skill's Specializations (if any).
			foreach (XmlNode objXmlSpecialization in nodSkill.SelectNodes("specs/spec"))
			{
				if (objXmlSpecialization.Attributes["translate"] != null)
					objSkillControl.AddSpec(objXmlSpecialization.Attributes["translate"].InnerText);
				else
					objSkillControl.AddSpec(objXmlSpecialization.InnerText);
			}

			// Look through the Weapons file and grab the names of items that are part of the appropriate Exotic Category or use the matching Exoctic Skill.
			XmlDocument objXmlWeaponDocument = XmlManager.Instance.Load("weapons.xml");
			XmlNodeList objXmlWeaponList = objXmlWeaponDocument.SelectNodes("/chummer/weapons/weapon[category = \"" + frmPickExoticSkill.SelectedExoticSkill + "s\" or useskill = \"" + frmPickExoticSkill.SelectedExoticSkill + "s\"]");
			foreach (XmlNode objXmlWeapon in objXmlWeaponList)
			{
				if (objXmlWeapon["translate"] != null)
					objSkillControl.AddSpec(objXmlWeapon["translate"].InnerText);
				else
					objSkillControl.AddSpec(objXmlWeapon["name"].InnerText);
			}

			objSkillControl.SkillRatingMaximum = 6;
			// Set the SkillControl's Location since scrolling the Panel causes it to actually change the child Controls' Locations.
			objSkillControl.Location = new Point(0, objSkillControl.Height * i + panActiveSkills.AutoScrollPosition.Y);
			panActiveSkills.Controls.Add(objSkillControl);

			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void cmdAddCritterPower_Click(object sender, EventArgs e)
		{
			// Make sure the Critter is allowed to have Optional Powers.
			XmlDocument objXmlDocument = XmlManager.Instance.Load("critters.xml");
			XmlNode objXmlCritter = objXmlDocument.SelectSingleNode("/chummer/metatypes/metatype[name = \"" + _objCharacter.Metatype + "\"]");

			if (objXmlCritter == null)
			{
				objXmlDocument = XmlManager.Instance.Load("metatypes.xml");
				objXmlCritter = objXmlDocument.SelectSingleNode("/chummer/metatypes/metatype[name = \"" + _objCharacter.Metatype + "\"]");
			}

			frmSelectCritterPower frmPickCritterPower = new frmSelectCritterPower(_objCharacter);
			frmPickCritterPower.ShowDialog(this);

			if (frmPickCritterPower.DialogResult == DialogResult.Cancel)
				return;
			
			objXmlDocument = XmlManager.Instance.Load("critterpowers.xml");
			XmlNode objXmlPower = objXmlDocument.SelectSingleNode("/chummer/powers/power[name = \"" + frmPickCritterPower.SelectedPower + "\"]");
			TreeNode objNode = new TreeNode();
			CritterPower objPower = new CritterPower(_objCharacter);
			objPower.Create(objXmlPower, _objCharacter, objNode, frmPickCritterPower.SelectedRating);
			objPower.PowerPoints = frmPickCritterPower.PowerPoints;
			objNode.ContextMenuStrip = cmsCritterPowers;
			if (objPower.InternalId == Guid.Empty.ToString())
				return;

			_objCharacter.CritterPowers.Add(objPower);

			if (objPower.Category != "Weakness")
			{
				treCritterPowers.Nodes[0].Nodes.Add(objNode);
				treCritterPowers.Nodes[0].Expand();
			}
			else
			{
				treCritterPowers.Nodes[1].Nodes.Add(objNode);
				treCritterPowers.Nodes[1].Expand();
			}

			_objFunctions.SortTree(treCritterPowers);
			UpdateCharacterInfo();

			_blnIsDirty = true;
			UpdateWindowTitle();

			if (frmPickCritterPower.AddAgain)
				cmdAddCritterPower_Click(sender, e);
		}

		private void cmdDeleteCritterPower_Click(object sender, EventArgs e)
		{
			try
			{
				if (treCritterPowers.SelectedNode.Level == 0)
					return;
			}
			catch
			{
				return;
			}

			if (!_objFunctions.ConfirmDelete(LanguageManager.Instance.GetString("Message_DeleteCritterPower")))
				return;

			// Locate the selected Critter Power.
			CritterPower objPower = _objFunctions.FindCritterPower(treCritterPowers.SelectedNode.Tag.ToString(), _objCharacter.CritterPowers);

			// Remove any Improvements that were created by the Critter Power.
			_objImprovementManager.RemoveImprovements(Improvement.ImprovementSource.CritterPower, objPower.InternalId);

			_objCharacter.CritterPowers.Remove(objPower);
			treCritterPowers.SelectedNode.Remove();

			UpdateCharacterInfo();

			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void cmdDeleteComplexForm_Click(object sender, EventArgs e)
		{
			// Delete the selected Complex Form.
			try
			{
				if (treComplexForms.SelectedNode.Level == 1)
				{
					if (!_objFunctions.ConfirmDelete(LanguageManager.Instance.GetString("Message_DeleteComplexForm")))
						return;

					// Locate the Program that is selected in the tree.
					TechProgram objProgram = _objFunctions.FindTechProgram(treComplexForms.SelectedNode.Tag.ToString(), _objCharacter.TechPrograms);

					_objImprovementManager.RemoveImprovements(Improvement.ImprovementSource.ComplexForm, objProgram.InternalId);

					_objCharacter.TechPrograms.Remove(objProgram);
					treComplexForms.SelectedNode.Remove();
				}
				if (treComplexForms.SelectedNode.Level == 2)
				{
					if (!_objFunctions.ConfirmDelete(LanguageManager.Instance.GetString("Message_DeleteComplexFormOption")))
						return;

					// Locate the Program Option that is selected in the tree.
					TechProgram objProgram = new TechProgram(_objCharacter);
					TechProgramOption objOption = _objFunctions.FindTechProgramOption(treComplexForms.SelectedNode.Tag.ToString(), _objCharacter.TechPrograms, out objProgram);

					objProgram.Options.Remove(objOption);
					treComplexForms.SelectedNode.Remove();
				}
				UpdateCharacterInfo();

				_blnIsDirty = true;
				UpdateWindowTitle();
			}
			catch
			{
			}
		}

		private void cmdAddQuality_Click(object sender, EventArgs e)
		{
			frmSelectQuality frmPickQuality = new frmSelectQuality(_objCharacter);
			frmPickQuality.ShowDialog(this);

			// Don't do anything else if the form was canceled.
			if (frmPickQuality.DialogResult == DialogResult.Cancel)
				return;

			XmlDocument objXmlDocument = XmlManager.Instance.Load("qualities.xml");
			XmlNode objXmlQuality = objXmlDocument.SelectSingleNode("/chummer/qualities/quality[name = \"" + frmPickQuality.SelectedQuality + "\"]");

			TreeNode objNode = new TreeNode();
			List<Weapon> objWeapons = new List<Weapon>();
			List<TreeNode> objWeaponNodes = new List<TreeNode>();
			Quality objQuality = new Quality(_objCharacter);

			objQuality.Create(objXmlQuality, _objCharacter, QualitySource.Selected, objNode, objWeapons, objWeaponNodes);
			objNode.ContextMenuStrip = cmsQuality;
			if (objQuality.InternalId == Guid.Empty.ToString())
				return;

			if (frmPickQuality.FreeCost)
				objQuality.BP = 0;

			// If the item being checked would cause the limit of 35 BP spent on Positive Qualities to be exceed, do not let it be checked and display a message.
			string strAmount = "";
			int intMaxQualityAmount = 0;
			if (_objCharacter.BuildMethod == CharacterBuildMethod.BP)
			{
				strAmount = "35 " + LanguageManager.Instance.GetString("String_BP");
				intMaxQualityAmount = 35;
			}
			else
			{
				strAmount = "70 " + LanguageManager.Instance.GetString("String_Karma");
				intMaxQualityAmount = 70;
			}

			// Make sure that adding the Quality would not cause the character to exceed their BP limits.
			int intBP = 0;
			bool blnAddItem = true;

			// Add the cost of the Quality that is being added.
			if (objQuality.ContributeToLimit)
				intBP += objQuality.BP;

			if (objQuality.Type == QualityType.Negative)
			{
				// Calculate the cost of the current Negative Qualities.
				foreach (Quality objCharacterQuality in _objCharacter.Qualities)
				{
					if (objCharacterQuality.Type == QualityType.Negative && objCharacterQuality.ContributeToLimit)
						intBP += objCharacterQuality.BP;
				}
				if (_objCharacter.BuildMethod == CharacterBuildMethod.Karma)
					intBP *= _objOptions.KarmaQuality;

				// Include the BP used by Enemies.
				if (_objCharacter.BuildMethod == CharacterBuildMethod.BP)
					intBP += int.Parse(lblEnemiesBP.Text.Replace(LanguageManager.Instance.GetString("String_BP"), ""));
				else
				{
					if (lblEnemiesBP.Text.Contains(LanguageManager.Instance.GetString("String_BP")))
						intBP += int.Parse(lblEnemiesBP.Text.Replace(LanguageManager.Instance.GetString("String_BP"), ""));
					else
						intBP += int.Parse(lblEnemiesBP.Text.Replace(" " + LanguageManager.Instance.GetString("String_Karma"), ""));
				}

				// Include the amount from Free Negative Quality BP cost Improvements.
				if (_objCharacter.BuildMethod == CharacterBuildMethod.BP)
					intBP -= _objImprovementManager.ValueOf(Improvement.ImprovementType.FreeNegativeQualities);
				else
					intBP -= (_objImprovementManager.ValueOf(Improvement.ImprovementType.FreeNegativeQualities) * _objOptions.KarmaQuality);

				// Check if adding this Quality would put the character over their limit.
				if (!_objOptions.ExceedNegativeQualities)
				{
					if (intBP < (intMaxQualityAmount * -1) && !_objCharacter.IgnoreRules)
					{
						MessageBox.Show(LanguageManager.Instance.GetString("Message_NegativeQualityLimit").Replace("{0}", strAmount), LanguageManager.Instance.GetString("MessageTitle_NegativeQualityLimit"), MessageBoxButtons.OK, MessageBoxIcon.Information);
						blnAddItem = false;
					}
				}
			}
			else
			{
				// Calculate the cost of the current Positive Qualities.
				foreach (Quality objCharacterQuality in _objCharacter.Qualities)
				{
					if (objCharacterQuality.Type == QualityType.Positive && objCharacterQuality.ContributeToLimit)
						intBP += objCharacterQuality.BP;
				}
				if (_objCharacter.BuildMethod == CharacterBuildMethod.Karma)
					intBP *= _objOptions.KarmaQuality;

				// Include the BP used by Martial Arts. Each Rating costs 5 BP.
				foreach (MartialArt objMartialArt in _objCharacter.MartialArts)
				{
					if (_objCharacter.BuildMethod == CharacterBuildMethod.BP)
						intBP += objMartialArt.Rating * 5;
					else
						intBP += objMartialArt.Rating * 5 * _objOptions.KarmaQuality;
				}

				// Include the amount from Free Negative Quality BP cost Improvements.
				if (_objCharacter.BuildMethod == CharacterBuildMethod.BP)
					intBP -= _objImprovementManager.ValueOf(Improvement.ImprovementType.FreePositiveQualities);
				else
					intBP -= (_objImprovementManager.ValueOf(Improvement.ImprovementType.FreePositiveQualities) * _objOptions.KarmaQuality);

				// Check if adding this Quality would put the character over their limit.
				if (!_objOptions.ExceedPositiveQualities)
				{
					if (intBP > intMaxQualityAmount && !_objCharacter.IgnoreRules)
					{
						MessageBox.Show(LanguageManager.Instance.GetString("Message_PositiveQualityLimit").Replace("{0}", strAmount), LanguageManager.Instance.GetString("MessageTitle_PositiveQualityLimit"), MessageBoxButtons.OK, MessageBoxIcon.Information);
						blnAddItem = false;
					}
				}
			}

			if (blnAddItem)
			{
				// Add the Quality to the appropriate parent node.
				if (objQuality.Type == QualityType.Positive)
				{
					treQualities.Nodes[0].Nodes.Add(objNode);
					treQualities.Nodes[0].Expand();
				}
				else
				{
					treQualities.Nodes[1].Nodes.Add(objNode);
					treQualities.Nodes[1].Expand();
				}
				_objCharacter.Qualities.Add(objQuality);

				// Add any created Weapons to the character.
				foreach (Weapon objWeapon in objWeapons)
					_objCharacter.Weapons.Add(objWeapon);

				// Create the Weapon Node if one exists.
				foreach (TreeNode objWeaponNode in objWeaponNodes)
				{
					objWeaponNode.ContextMenuStrip = cmsWeapon;
					treWeapons.Nodes[0].Nodes.Add(objWeaponNode);
					treWeapons.Nodes[0].Expand();
				}

				// Add any additional Qualities that are forced on the character.
				if (objXmlQuality.SelectNodes("addqualities/addquality").Count > 0)
				{
					foreach (XmlNode objXmlAddQuality in objXmlQuality.SelectNodes("addqualities/addquality"))
					{
						XmlNode objXmlSelectedQuality = objXmlDocument.SelectSingleNode("/chummer/qualities/quality[name = \"" + objXmlAddQuality.InnerText + "\"]");
						string strForceValue = "";
						if (objXmlAddQuality.Attributes["select"] != null)
							strForceValue = objXmlAddQuality.Attributes["select"].InnerText;
						bool blnAddQuality = true;

						// Make sure the character does not yet have this Quality.
						foreach (Quality objCharacterQuality in _objCharacter.Qualities)
						{
							if (objCharacterQuality.Name == objXmlAddQuality.InnerText && objCharacterQuality.Extra == strForceValue)
							{
								blnAddQuality = false;
								break;
							}
						}

						if (blnAddQuality)
						{
							TreeNode objAddQualityNode = new TreeNode();
							List<Weapon> objAddWeapons = new List<Weapon>();
							List<TreeNode> objAddWeaponNodes = new List<TreeNode>();
							Quality objAddQuality = new Quality(_objCharacter);
							objAddQuality.Create(objXmlSelectedQuality, _objCharacter, QualitySource.Selected, objAddQualityNode, objWeapons, objWeaponNodes, strForceValue);

							if (objAddQuality.Type == QualityType.Positive)
							{
								treQualities.Nodes[0].Nodes.Add(objAddQualityNode);
								treQualities.Nodes[0].Expand();
							}
							else
							{
								treQualities.Nodes[1].Nodes.Add(objAddQualityNode);
								treQualities.Nodes[1].Expand();
							}
							_objCharacter.Qualities.Add(objAddQuality);

							// Add any created Weapons to the character.
							foreach (Weapon objWeapon in objAddWeapons)
								_objCharacter.Weapons.Add(objWeapon);

							// Create the Weapon Node if one exists.
							foreach (TreeNode objWeaponNode in objAddWeaponNodes)
							{
								objWeaponNode.ContextMenuStrip = cmsWeapon;
								treWeapons.Nodes[0].Nodes.Add(objWeaponNode);
								treWeapons.Nodes[0].Expand();
							}
						}
					}
				}

				// Add any Critter Powers that are gained through the Quality (Infected).
				if (objXmlQuality.SelectNodes("powers/power").Count > 0)
				{
					objXmlDocument = XmlManager.Instance.Load("critterpowers.xml");
					foreach (XmlNode objXmlPower in objXmlQuality.SelectNodes("powers/power"))
					{
						XmlNode objXmlCritterPower = objXmlDocument.SelectSingleNode("/chummer/powers/power[name = \"" + objXmlPower.InnerText + "\"]");
						TreeNode objPowerNode = new TreeNode();
						CritterPower objPower = new CritterPower(_objCharacter);
						string strForcedValue = "";
						int intRating = 0;

						if (objXmlPower.Attributes["rating"] != null)
							intRating = Convert.ToInt32(objXmlPower.Attributes["rating"].InnerText);
						if (objXmlPower.Attributes["select"] != null)
							strForcedValue = objXmlPower.Attributes["select"].InnerText;

						objPower.Create(objXmlCritterPower, _objCharacter, objPowerNode, intRating, strForcedValue);
						_objCharacter.CritterPowers.Add(objPower);

						if (objPower.Category != "Weakness")
						{
							treCritterPowers.Nodes[0].Nodes.Add(objPowerNode);
							treCritterPowers.Nodes[0].Expand();
						}
						else
						{
							treCritterPowers.Nodes[1].Nodes.Add(objPowerNode);
							treCritterPowers.Nodes[1].Expand();
						}
					}
				}
			}
			else
			{
				// If the Quality could not be added, remove the Improvements that were added during the Quality Creation process.
				_objImprovementManager.RemoveImprovements(Improvement.ImprovementSource.Quality, objQuality.InternalId);
			}

			_objFunctions.SortTree(treQualities);
			UpdateMentorSpirits();
			UpdateCharacterInfo();

			_blnIsDirty = true;
			UpdateWindowTitle();

			if (frmPickQuality.AddAgain)
				cmdAddQuality_Click(sender, e);
		}

		private void cmdDeleteQuality_Click(object sender, EventArgs e)
		{
			// Locate the selected Quality.
			try
			{
				if (treQualities.SelectedNode.Level == 0)
					return;
			}
			catch
			{
				return;
			}

			Quality objQuality = _objFunctions.FindQuality(treQualities.SelectedNode.Tag.ToString(), _objCharacter.Qualities);

			XmlDocument objXmlDocument = XmlManager.Instance.Load("qualities.xml");

			// Qualities that come from a Metatype cannot be removed.
			if (objQuality.OriginSource == QualitySource.Metatype)
			{
				MessageBox.Show(LanguageManager.Instance.GetString("Message_MetavariantQuality"), LanguageManager.Instance.GetString("MessageTitle_MetavariantQuality"), MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}
			else if (objQuality.OriginSource == QualitySource.MetatypeRemovable)
			{
				// Look up the cost of the Quality.
				XmlNode objXmlMetatypeQuality = objXmlDocument.SelectSingleNode("/chummer/qualities/quality[name = \"" + objQuality.Name + "\"]");
				int intBP = Convert.ToInt32(objXmlMetatypeQuality["bp"].InnerText) * -1;
				int intShowBP = intBP;
				if (_objCharacter.BuildMethod == CharacterBuildMethod.Karma)
					intShowBP *= _objOptions.KarmaQuality;
				string strBP = intShowBP.ToString();
				if (_objCharacter.BuildMethod == CharacterBuildMethod.Karma)
					strBP += " " + LanguageManager.Instance.GetString("String_Karma");
				else
					strBP += " " + LanguageManager.Instance.GetString("String_BP");

				if (!_objFunctions.ConfirmDelete(LanguageManager.Instance.GetString("Message_DeleteMetatypeQuality").Replace("{0}", strBP)))
					return;

				// Remove any Improvements that the Quality might have.
				if (objXmlMetatypeQuality["bonus"] != null)
					objXmlMetatypeQuality["bonus"].InnerText = "";

				TreeNode objEmptyNode = new TreeNode();
				List<Weapon> objWeapons = new List<Weapon>();
				List<TreeNode> objWeaponNodes = new List<TreeNode>();
				Quality objReplaceQuality = new Quality(_objCharacter);
				objReplaceQuality.Create(objXmlMetatypeQuality, _objCharacter, QualitySource.Selected, objEmptyNode, objWeapons, objWeaponNodes);
				objReplaceQuality.BP *= -1;
				// If a Negative Quality is being bought off, the replacement one is Positive.
				if (objQuality.Type == QualityType.Positive)
					objQuality.Type = QualityType.Negative;
				else
					objReplaceQuality.Type = QualityType.Positive;
				// The replacement Quality does not count towards the BP limit of the new type, nor should it be printed.
				objReplaceQuality.AllowPrint = false;
				objReplaceQuality.ContributeToLimit = false;
				_objCharacter.Qualities.Add(objReplaceQuality);
			}
			else
			{
				if (!_objFunctions.ConfirmDelete(LanguageManager.Instance.GetString("Message_DeleteQuality")))
					return;
			}

			// Remove the Improvements that were created by the Quality.
			_objImprovementManager.RemoveImprovements(Improvement.ImprovementSource.Quality, objQuality.InternalId);

			XmlNode objXmlDeleteQuality = objXmlDocument.SelectSingleNode("/chummer/qualities/quality[name = \"" + objQuality.Name + "\"]");
			
			// Remove any Critter Powers that are gained through the Quality (Infected).
			if (objXmlDeleteQuality.SelectNodes("powers/power").Count > 0)
			{
				objXmlDocument = XmlManager.Instance.Load("critterpowers.xml");
				foreach (XmlNode objXmlPower in objXmlDeleteQuality.SelectNodes("powers/power"))
				{
					string strExtra = "";
					if (objXmlPower.Attributes["select"] != null)
						strExtra = objXmlPower.Attributes["select"].InnerText;

					foreach (CritterPower objPower in _objCharacter.CritterPowers)
					{
						if (objPower.Name == objXmlPower.InnerText && objPower.Extra == strExtra)
						{
							// Remove any Improvements created by the Critter Power.
							_objImprovementManager.RemoveImprovements(Improvement.ImprovementSource.CritterPower, objPower.InternalId);

							// Remove the Critter Power from the character.
							_objCharacter.CritterPowers.Remove(objPower);
							
							// Remove the Critter Power from the Tree.
							foreach (TreeNode objNode in treCritterPowers.Nodes[0].Nodes)
							{
								if (objNode.Tag.ToString() == objPower.InternalId)
								{
									objNode.Remove();
									break;
								}
							}
							foreach (TreeNode objNode in treCritterPowers.Nodes[1].Nodes)
							{
								if (objNode.Tag.ToString() == objPower.InternalId)
								{
									objNode.Remove();
									break;
								}
							}
							break;
						}
					}
				}
			}

			// Remove any Weapons created by the Quality if applicable.
			if (objQuality.WeaponID != Guid.Empty.ToString())
			{
				// Remove the Weapon from the TreeView.
				TreeNode objRemoveNode = new TreeNode();
				foreach (TreeNode objWeaponNode in treWeapons.Nodes[0].Nodes)
				{
					if (objWeaponNode.Tag.ToString() == objQuality.WeaponID)
						objRemoveNode = objWeaponNode;
				}
				treWeapons.Nodes.Remove(objRemoveNode);

				// Remove the Weapon from the Character.
				Weapon objRemoveWeapon = new Weapon(_objCharacter);
				foreach (Weapon objWeapon in _objCharacter.Weapons)
				{
					if (objWeapon.InternalId == objQuality.WeaponID)
						objRemoveWeapon = objWeapon;
				}
				_objCharacter.Weapons.Remove(objRemoveWeapon);
			}

			_objCharacter.Qualities.Remove(objQuality);
			treQualities.SelectedNode.Remove();

			UpdateMentorSpirits();
			UpdateCharacterInfo();

			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void cmdImproveInitiation_Click(object sender, EventArgs e)
		{
			if (_objCharacter.MAGEnabled)
			{
				// Make sure that the Initiate Grade is not attempting to go above the character's MAG Attribute.
				if (_objCharacter.InitiateGrade + 1 > _objCharacter.MAG.TotalValue)
				{
					MessageBox.Show(LanguageManager.Instance.GetString("Message_CannotIncreaseInitiateGrade"), LanguageManager.Instance.GetString("MessageTitle_CannotIncreaseInitiateGrade"), MessageBoxButtons.OK, MessageBoxIcon.Information);
					return;
				}

				// Create the Initiate Grade object.
				InitiationGrade objGrade = new InitiationGrade(_objCharacter);
				objGrade.Create(_objCharacter.InitiateGrade + 1, _objCharacter.RESEnabled, chkInitiationGroup.Checked, chkInitiationOrdeal.Checked);
				_objCharacter.InitiationGrades.Add(objGrade);

				// Set the character's Initiate Grade.
				_objCharacter.InitiateGrade += 1;

				// Remove any existing Initiation Improvements.
				_objImprovementManager.RemoveImprovements(Improvement.ImprovementSource.Initiation, "Initiation");

				// Create the replacement Improvement.
				_objImprovementManager.CreateImprovement("MAG", Improvement.ImprovementSource.Initiation, "Initiation", Improvement.ImprovementType.Attribute, "", 0, 1, 0, _objCharacter.InitiateGrade);
				_objImprovementManager.Commit();

				// Update any Metamagic Improvements the character might have.
				foreach (Metamagic objMetamagic in _objCharacter.Metamagics)
				{
					if (objMetamagic.Bonus != null)
					{
						// If the Bonus contains "Rating", remove the existing Improvement and create new ones.
						if (objMetamagic.Bonus.InnerXml.Contains("Rating"))
						{
							_objImprovementManager.RemoveImprovements(Improvement.ImprovementSource.Metamagic, objMetamagic.InternalId);
							_objImprovementManager.CreateImprovements(Improvement.ImprovementSource.Metamagic, objMetamagic.InternalId, objMetamagic.Bonus, false, _objCharacter.InitiateGrade, objMetamagic.DisplayNameShort);
						}
					}
				}

				lblInitiateGrade.Text = _objCharacter.InitiateGrade.ToString();
			}
			else if (_objCharacter.RESEnabled)
			{
				// Make sure that the Initiate Grade is not attempting to go above the character's RES Attribute.
				if (_objCharacter.SubmersionGrade + 1 > _objCharacter.RES.TotalValue)
				{
					MessageBox.Show(LanguageManager.Instance.GetString("Message_CannotIncreaseSubmersionGrade"), LanguageManager.Instance.GetString("MessageTitle_CannotIncreaseSubmersionGrade"), MessageBoxButtons.OK, MessageBoxIcon.Information);
					return;
				}

				// Create the Initiate Grade object.
				InitiationGrade objGrade = new InitiationGrade(_objCharacter);
				objGrade.Create(_objCharacter.SubmersionGrade + 1, _objCharacter.RESEnabled, chkInitiationGroup.Checked, chkInitiationOrdeal.Checked);
				_objCharacter.InitiationGrades.Add(objGrade);

				// Set the character's Submersion Grade.
				_objCharacter.SubmersionGrade += 1;

				// Remove any existing Initiation Improvements.
				_objImprovementManager.RemoveImprovements(Improvement.ImprovementSource.Submersion, "Submersion");

				// Create the replacement Improvement.
				_objImprovementManager.CreateImprovement("RES", Improvement.ImprovementSource.Submersion, "Submersion", Improvement.ImprovementType.Attribute, "", 0, 1, 0, _objCharacter.SubmersionGrade);
				_objImprovementManager.Commit();

				// Update any Echo Improvements the character might have.
				foreach (Metamagic objMetamagic in _objCharacter.Metamagics)
				{
					if (objMetamagic.Bonus != null)
					{
						// If the Bonus contains "Rating", remove the existing Improvement and create new ones.
						if (objMetamagic.Bonus.InnerXml.Contains("Rating"))
						{
							_objImprovementManager.RemoveImprovements(Improvement.ImprovementSource.Echo, objMetamagic.InternalId);
							_objImprovementManager.CreateImprovements(Improvement.ImprovementSource.Echo, objMetamagic.InternalId, objMetamagic.Bonus, false, _objCharacter.SubmersionGrade, objMetamagic.DisplayNameShort);
						}
					}
				}

				lblInitiateGrade.Text = _objCharacter.SubmersionGrade.ToString();
			}

			UpdateInitiationGradeList();
			UpdateCharacterInfo();

			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void cmdReduceInitiation_Click(object sender, EventArgs e)
		{
			if (_objCharacter.MAGEnabled)
			{
				// Don't attempt to reduce the Grade if they're already at 0.
				if (_objCharacter.InitiateGrade == 0)
					return;

				_objCharacter.InitiateGrade -= 1;

				// Remove any existing Initiation Improvements.
				_objImprovementManager.RemoveImprovements(Improvement.ImprovementSource.Initiation, "Initiation");

				// Create the replacement Improvement.
				_objImprovementManager.CreateImprovement("MAG", Improvement.ImprovementSource.Initiation, "Initiation", Improvement.ImprovementType.Attribute, "", 0, 1, 0, _objCharacter.InitiateGrade);
				_objImprovementManager.Commit();

				// Update any Metamagic Improvements the character might have.
				foreach (Metamagic objMetamagic in _objCharacter.Metamagics)
				{
					if (objMetamagic.Bonus != null)
					{
						// If the Bonus contains "Rating", remove the existing Improvement and create new ones.
						if (objMetamagic.Bonus.InnerXml.Contains("Rating"))
						{
							_objImprovementManager.RemoveImprovements(Improvement.ImprovementSource.Metamagic, objMetamagic.InternalId);
							_objImprovementManager.CreateImprovements(Improvement.ImprovementSource.Metamagic, objMetamagic.InternalId, objMetamagic.Bonus, false, _objCharacter.InitiateGrade, objMetamagic.DisplayNameShort);
						}
					}
				}

				lblInitiateGrade.Text = _objCharacter.InitiateGrade.ToString();

				// Statement is wrapped in a try since older Initiated character may not have these.
				try
				{
					_objCharacter.InitiationGrades.RemoveAt(_objCharacter.InitiationGrades.Count - 1);
				}
				catch
				{
				}
			}
			else if (_objCharacter.RESEnabled)
			{
				// Don't attempt to reduce the Grade if they're already at 0.
				if (_objCharacter.SubmersionGrade == 0)
					return;

				_objCharacter.SubmersionGrade -= 1;

				// Remove any existing Initiation Improvements.
				_objImprovementManager.RemoveImprovements(Improvement.ImprovementSource.Submersion, "Submersion");

				// Create the replacement Improvement.
				_objImprovementManager.CreateImprovement("RES", Improvement.ImprovementSource.Submersion, "Submersion", Improvement.ImprovementType.Attribute, "", 0, 1, 0, _objCharacter.SubmersionGrade);
				_objImprovementManager.Commit();

				// Update any Echo Improvements the character might have.
				foreach (Metamagic objMetamagic in _objCharacter.Metamagics)
				{
					if (objMetamagic.Bonus != null)
					{
						// If the Bonus contains "Rating", remove the existing Improvement and create new ones.
						if (objMetamagic.Bonus.InnerXml.Contains("Rating"))
						{
							_objImprovementManager.RemoveImprovements(Improvement.ImprovementSource.Echo, objMetamagic.InternalId);
							_objImprovementManager.CreateImprovements(Improvement.ImprovementSource.Echo, objMetamagic.InternalId, objMetamagic.Bonus, false, _objCharacter.SubmersionGrade, objMetamagic.DisplayNameShort);
						}
					}
				}

				lblInitiateGrade.Text = _objCharacter.SubmersionGrade.ToString();

				// Statement is wrapped in a try since older Initiated character may not have these.
				try
				{
					_objCharacter.InitiationGrades.RemoveAt(_objCharacter.InitiationGrades.Count - 1);
				}
				catch
				{
				}
			}

			UpdateInitiationGradeList();
			UpdateCharacterInfo();

			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void cmdAddLocation_Click(object sender, EventArgs e)
		{
			// Add a new location to the Gear Tree.
			frmSelectText frmPickText = new frmSelectText();
			frmPickText.Description = LanguageManager.Instance.GetString("String_AddLocation");
			frmPickText.ShowDialog(this);

			if (frmPickText.DialogResult == DialogResult.Cancel || frmPickText.SelectedValue == "")
				return;

			string strLocation = frmPickText.SelectedValue;
			_objCharacter.Locations.Add(strLocation);

			TreeNode objLocation = new TreeNode();
			objLocation.Tag = strLocation;
			objLocation.Text = strLocation;
			objLocation.ContextMenuStrip = cmsGearLocation;
			treGear.Nodes.Add(objLocation);

			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void cmdAddWeaponLocation_Click(object sender, EventArgs e)
		{
			// Add a new location to the Weapons Tree.
			frmSelectText frmPickText = new frmSelectText();
			frmPickText.Description = LanguageManager.Instance.GetString("String_AddLocation");
			frmPickText.ShowDialog(this);

			if (frmPickText.DialogResult == DialogResult.Cancel || frmPickText.SelectedValue == "")
				return;

			string strLocation = frmPickText.SelectedValue;
			_objCharacter.WeaponLocations.Add(strLocation);

			TreeNode objLocation = new TreeNode();
			objLocation.Tag = strLocation;
			objLocation.Text = strLocation;
			objLocation.ContextMenuStrip = cmsWeaponLocation;
			treWeapons.Nodes.Add(objLocation);

			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void cmdCreateStackedFocus_Click(object sender, EventArgs e)
		{
			int intFree = 0;
			List<Gear> lstGear = new List<Gear>();
			List<Gear> lstStack = new List<Gear>();

			// Run through all of the Foci the character has and count the un-Bonded ones.
			foreach (Gear objGear in _objCharacter.Gear)
			{
				if (objGear.Category == "Foci" || objGear.Category == "Metamagic Foci")
				{
					if (!objGear.Bonded)
					{
						intFree++;
						lstGear.Add(objGear);
					}
				}
			}

			// If the character does not have at least 2 un-Bonded Foci, display an error and leave.
			if (intFree < 2)
			{
				MessageBox.Show(LanguageManager.Instance.GetString("Message_CannotStackFoci"), LanguageManager.Instance.GetString("MessageTitle_CannotStackFoci"), MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			frmSelectItem frmPickItem = new frmSelectItem();

			// Let the character select the Foci they'd like to stack, stopping when they either click Cancel or there are no more items left in the list.
			do
			{
				frmPickItem.Gear = lstGear;
				frmPickItem.AllowAutoSelect = false;
				frmPickItem.Description = LanguageManager.Instance.GetString("String_SelectItemFocus");
				frmPickItem.ShowDialog(this);

				if (frmPickItem.DialogResult == DialogResult.OK)
				{
					// Move the item from the Gear list to the Stack list.
					foreach (Gear objGear in lstGear)
					{
						if (objGear.InternalId == frmPickItem.SelectedItem)
						{
							objGear.Bonded = true;
							lstStack.Add(objGear);
							lstGear.Remove(objGear);
							break;
						}
					}
				}
			} while (lstGear.Count > 0 && frmPickItem.DialogResult != DialogResult.Cancel);

			// Make sure at least 2 Foci were selected.
			if (lstStack.Count < 2)
			{
				MessageBox.Show(LanguageManager.Instance.GetString("Message_StackedFocusMinimum"), LanguageManager.Instance.GetString("MessageTitle_CannotStackFoci"), MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			// Make sure the combined Force of the Foci do not exceed 6.
			if (!_objOptions.AllowHigherStackedFoci)
			{
				int intCombined = 0;
				foreach (Gear objGear in lstStack)
					intCombined += objGear.Rating;
				if (intCombined > 6)
				{
					foreach (Gear objGear in lstStack)
						objGear.Bonded = false;
					MessageBox.Show(LanguageManager.Instance.GetString("Message_StackedFocusForce"), LanguageManager.Instance.GetString("MessageTitle_CannotStackFoci"), MessageBoxButtons.OK, MessageBoxIcon.Information);
					return;
				}
			}

			// Create the Stacked Focus.
			StackedFocus objStack = new StackedFocus(_objCharacter);
			objStack.Gear = lstStack;
			_objCharacter.StackedFoci.Add(objStack);

			// Remove the Gear from the character and replace it with a Stacked Focus item.
			int intCost = 0;
			foreach (Gear objGear in lstStack)
			{
				intCost += objGear.TotalCost;
				_objCharacter.Gear.Remove(objGear);

				// Remove the TreeNode from Gear.
				foreach (TreeNode nodRoot in treGear.Nodes)
				{
					foreach (TreeNode nodItem in nodRoot.Nodes)
					{
						if (nodItem.Tag.ToString() == objGear.InternalId)
						{
							nodRoot.Nodes.Remove(nodItem);
							break;
						}
					}
				}
			}

			Gear objStackItem = new Gear(_objCharacter);
			objStackItem.Category = "Stacked Focus";
			objStackItem.Name = "Stacked Focus: " + objStack.Name;
			objStackItem.MinRating = 0;
			objStackItem.MaxRating = 0;
			objStackItem.Source = "SM";
			objStackItem.Page = "84";
			objStackItem.Cost = intCost.ToString();
			objStackItem.Avail = "0";

			TreeNode nodStackNode = new TreeNode();
			nodStackNode.Text = objStackItem.DisplayNameShort;
			nodStackNode.Tag = objStackItem.InternalId;

			treGear.Nodes[0].Nodes.Add(nodStackNode);

			_objCharacter.Gear.Add(objStackItem);

			objStack.GearId = objStackItem.InternalId;

			_blnIsDirty = true;
			_objController.PopulateFocusList(treFoci);
			UpdateCharacterInfo();
			UpdateWindowTitle();
		}

		private void cmdAddArmorBundle_Click(object sender, EventArgs e)
		{
			// Add a new location to the Armor Tree.
			frmSelectText frmPickText = new frmSelectText();
			frmPickText.Description = LanguageManager.Instance.GetString("String_AddLocation");
			frmPickText.ShowDialog(this);

			if (frmPickText.DialogResult == DialogResult.Cancel || frmPickText.SelectedValue == "")
				return;

			string strLocation = frmPickText.SelectedValue;
			_objCharacter.ArmorBundles.Add(strLocation);

			TreeNode objLocation = new TreeNode();
			objLocation.Tag = strLocation;
			objLocation.Text = strLocation;
			objLocation.ContextMenuStrip = cmsArmorLocation;
			treArmor.Nodes.Add(objLocation);

			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void cmdArmorEquipAll_Click(object sender, EventArgs e)
		{
			// Equip all of the Armor in the Armor Bundle.
			foreach (Armor objArmor in _objCharacter.Armor)
			{
				if (objArmor.Location == treArmor.SelectedNode.Tag.ToString() || (treArmor.SelectedNode == treArmor.Nodes[0] && objArmor.Location == ""))
				{
					objArmor.Equipped = true;
					// Add the Armor's Improevments to the character.
					if (objArmor.Bonus != null)
						_objImprovementManager.CreateImprovements(Improvement.ImprovementSource.Armor, objArmor.InternalId, objArmor.Bonus, false, 1, objArmor.DisplayNameShort);
					// Add the Improvements from any Armor Mods in the Armor.
					foreach (ArmorMod objMod in objArmor.ArmorMods)
					{
						if (objMod.Bonus != null && objMod.Equipped)
							_objImprovementManager.CreateImprovements(Improvement.ImprovementSource.ArmorMod, objMod.InternalId, objMod.Bonus, false, objMod.Rating, objMod.DisplayNameShort);
					}
					// Add the Improvements from any Gear in the Armor.
					foreach (Gear objGear in objArmor.Gear)
					{
						if (objGear.Bonus != null && objGear.Equipped)
							_objImprovementManager.CreateImprovements(Improvement.ImprovementSource.Gear, objGear.InternalId, objGear.Bonus, false, objGear.Rating, objGear.DisplayNameShort);
					}
				}
			}
			RefreshSelectedArmor();

			_blnIsDirty = true;
			UpdateCharacterInfo();
			UpdateWindowTitle();
		}

		private void cmdArmorUnEquipAll_Click(object sender, EventArgs e)
		{
			// En-equip all of the Armor in the Armor Bundle.
			foreach (Armor objArmor in _objCharacter.Armor)
			{
				if (objArmor.Location == treArmor.SelectedNode.Tag.ToString() || (treArmor.SelectedNode == treArmor.Nodes[0] && objArmor.Location == ""))
				{
					objArmor.Equipped = false;
					// Remove any Improvements the Armor created.
					if (objArmor.Bonus != null)
						_objImprovementManager.RemoveImprovements(Improvement.ImprovementSource.Armor, objArmor.InternalId);
					// Remove any Improvements from any Armor Mods in the Armor.
					foreach (ArmorMod objMod in objArmor.ArmorMods)
					{
						if (objMod.Bonus != null)
							_objImprovementManager.RemoveImprovements(Improvement.ImprovementSource.ArmorMod, objMod.InternalId);
					}
					// Remove any Improvements from any Gear in the Armor.
					foreach (Gear objGear in objArmor.Gear)
					{
						if (objGear.Bonus != null)
							_objImprovementManager.RemoveImprovements(Improvement.ImprovementSource.Gear, objGear.InternalId);
					}
				}
			}
			RefreshSelectedArmor();

			_blnIsDirty = true;
			UpdateCharacterInfo();
			UpdateWindowTitle();
		}

		private void cmdAddVehicleLocation_Click(object sender, EventArgs e)
		{
			// Make sure a Vehicle is selected.
			Vehicle objVehicle = new Vehicle(_objCharacter);
			try
			{
				if (treVehicles.SelectedNode.Level == 1)
				{
					objVehicle = _objFunctions.FindVehicle(treVehicles.SelectedNode.Tag.ToString(), _objCharacter.Vehicles);
				}
				else
				{
					MessageBox.Show(LanguageManager.Instance.GetString("Message_SelectVehicleLocation"), LanguageManager.Instance.GetString("MessageTitle_SelectVehicle"), MessageBoxButtons.OK, MessageBoxIcon.Information);
					return;
				}
			}
			catch
			{
				MessageBox.Show(LanguageManager.Instance.GetString("Message_SelectVehicleLocation"), LanguageManager.Instance.GetString("MessageTitle_SelectVehicle"), MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			// Add a new location to the selected Vehicle.
			frmSelectText frmPickText = new frmSelectText();
			frmPickText.Description = LanguageManager.Instance.GetString("String_AddLocation");
			frmPickText.ShowDialog(this);

			if (frmPickText.DialogResult == DialogResult.Cancel || frmPickText.SelectedValue == "")
				return;

			string strLocation = frmPickText.SelectedValue;
			objVehicle.Locations.Add(strLocation);

			TreeNode objLocation = new TreeNode();
			objLocation.Tag = strLocation;
			objLocation.Text = strLocation;
			objLocation.ContextMenuStrip = cmsVehicleLocation;
			treVehicles.SelectedNode.Nodes.Add(objLocation);

			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void cmdAddPet_Click(object sender, EventArgs e)
		{
			Contact objContact = new Contact(_objCharacter);
			objContact.EntityType = ContactType.Pet;
			_objCharacter.Contacts.Add(objContact);

			PetControl objContactControl = new PetControl();
			objContactControl.ContactObject = objContact;

			// Attach an EventHandler for the DeleteContact and FileNameChanged Events.
			objContactControl.DeleteContact += objPet_DeleteContact;
			objContactControl.FileNameChanged += objPet_FileNameChanged;

			// Add the control to the Panel.
			panPets.Controls.Add(objContactControl);
			UpdateCharacterInfo();

			_blnIsDirty = true;
			UpdateWindowTitle();
		}
		#endregion

		#region ContextMenu Events
		private void ContextMenu_Opening(object sender, CancelEventArgs e)
		{
			foreach (ToolStripItem objItem in ((ContextMenuStrip)sender).Items)
			{
				if (objItem.Tag != null)
					objItem.Text = LanguageManager.Instance.GetString(objItem.Tag.ToString());
			}
		}

		private void ContextMenu_DropDownOpening(object sender, EventArgs e)
		{
			foreach (ToolStripItem objItem in ((ToolStripDropDownItem)sender).DropDownItems)
			{
				if (objItem.Tag != null)
					objItem.Text = LanguageManager.Instance.GetString(objItem.Tag.ToString());
			}
		}

		private void tsCyberwareAddAsPlugin_Click(object sender, EventArgs e)
		{
			// Make sure a parent items is selected, then open the Select Cyberware window.
			try
			{
				if (treCyberware.SelectedNode.Level == 0)
				{
					MessageBox.Show(LanguageManager.Instance.GetString("Message_SelectCyberware"), LanguageManager.Instance.GetString("MessageTitle_SelectCyberware"), MessageBoxButtons.OK, MessageBoxIcon.Information);
					return;
				}
			}
			catch
			{
				MessageBox.Show(LanguageManager.Instance.GetString("Message_SelectCyberware"), LanguageManager.Instance.GetString("MessageTitle_SelectCyberware"), MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			if (treCyberware.SelectedNode.Parent == treCyberware.Nodes[1])
			{
				MessageBox.Show(LanguageManager.Instance.GetString("Message_SelectCyberware"), LanguageManager.Instance.GetString("MessageTitle_SelectCyberware"), MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}
			
			bool blnAddAgain = PickCyberware();
			if (blnAddAgain)
			{
				treCyberware.SelectedNode = treCyberware.SelectedNode.Parent;
				tsCyberwareAddAsPlugin_Click(sender, e);
			}
		}

		private void tsWeaponAddAccessory_Click(object sender, EventArgs e)
		{
			// Make sure a parent item is selected, then open the Select Accessory window.
			try
			{
				if (treWeapons.SelectedNode.Level == 0)
				{
					MessageBox.Show(LanguageManager.Instance.GetString("Message_SelectWeaponAccessory"), LanguageManager.Instance.GetString("MessageTitle_SelectWeapon"), MessageBoxButtons.OK, MessageBoxIcon.Information);
					return;
				}
			}
			catch
			{
				MessageBox.Show(LanguageManager.Instance.GetString("Message_SelectWeaponAccessory"), LanguageManager.Instance.GetString("MessageTitle_SelectWeapon"), MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			// Locate the Weapon that is selected in the Tree.
			Weapon objWeapon = _objFunctions.FindWeapon(treWeapons.SelectedNode.Tag.ToString(), _objCharacter.Weapons);

			// Accessories cannot be added to Cyberweapons.
			if (objWeapon.Category.StartsWith("Cyberware"))
			{
				MessageBox.Show(LanguageManager.Instance.GetString("Message_CyberweaponNoAccessory"), LanguageManager.Instance.GetString("MessageTitle_CyberweaponNoAccessory"), MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			// Open the Weapons XML file and locate the selected Weapon.
			XmlDocument objXmlDocument = XmlManager.Instance.Load("weapons.xml");

			XmlNode objXmlWeapon = objXmlDocument.SelectSingleNode("/chummer/weapons/weapon[name = \"" + objWeapon.Name + "\"]");

			frmSelectWeaponAccessory frmPickWeaponAccessory = new frmSelectWeaponAccessory(_objCharacter);

			if (objXmlWeapon == null)
			{
				MessageBox.Show(LanguageManager.Instance.GetString("Message_CannotModifyWeapon"), LanguageManager.Instance.GetString("MessageTitle_CannotModifyWeapon"), MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}
			
			// Make sure the Weapon allows Accessories to be added to it.
			if (!Convert.ToBoolean(objXmlWeapon["allowaccessory"].InnerText))
			{
				MessageBox.Show(LanguageManager.Instance.GetString("Message_CannotModifyWeapon"), LanguageManager.Instance.GetString("MessageTitle_CannotModifyWeapon"), MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}
			else
			{
				XmlNodeList objXmlMountList = objXmlWeapon.SelectNodes("accessorymounts/mount");
				string strMounts = "";
				foreach (XmlNode objXmlMount in objXmlMountList)
					strMounts += objXmlMount.InnerText + "/";

				frmPickWeaponAccessory.AllowedMounts = strMounts;
			}

			frmPickWeaponAccessory.WeaponCost = objWeapon.Cost;
			frmPickWeaponAccessory.AccessoryMultiplier = objWeapon.AccessoryMultiplier;
			frmPickWeaponAccessory.ShowDialog();

			if (frmPickWeaponAccessory.DialogResult == DialogResult.Cancel)
				return;

			// Locate the selected piece.
			objXmlWeapon = objXmlDocument.SelectSingleNode("/chummer/accessories/accessory[name = \"" + frmPickWeaponAccessory.SelectedAccessory + "\"]");

			TreeNode objNode = new TreeNode();
			WeaponAccessory objAccessory = new WeaponAccessory(_objCharacter);
			objAccessory.Create(objXmlWeapon, objNode, frmPickWeaponAccessory.SelectedMount);
			objAccessory.Parent = objWeapon;
			objWeapon.WeaponAccessories.Add(objAccessory);

			objNode.ContextMenuStrip = cmsWeaponAccessory;
			treWeapons.SelectedNode.Nodes.Add(objNode);
			treWeapons.SelectedNode.Expand();

			UpdateCharacterInfo();
			RefreshSelectedWeapon();

			if (frmPickWeaponAccessory.AddAgain)
				tsWeaponAddAccessory_Click(sender, e);
		}

		private void tsWeaponAddModification_Click(object sender, EventArgs e)
		{
			// Make sure a parent item is selected, then open the Select Accessory window.
			try
			{
				if (treWeapons.SelectedNode.Level == 0)
				{
					MessageBox.Show(LanguageManager.Instance.GetString("Message_SelectWeaponMod"), LanguageManager.Instance.GetString("MessageTitle_SelectWeapon"), MessageBoxButtons.OK, MessageBoxIcon.Information);
					return;
				}
			}
			catch
			{
				MessageBox.Show(LanguageManager.Instance.GetString("Message_SelectWeaponMod"), LanguageManager.Instance.GetString("MessageTitle_SelectWeapon"), MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			// Locate the Weapon that is selected in the Tree.
			Weapon objWeapon = _objFunctions.FindWeapon(treWeapons.SelectedNode.Tag.ToString(), _objCharacter.Weapons);

			frmSelectVehicleMod frmPickVehicleMod = new frmSelectVehicleMod(_objCharacter);

			// Make sure the Weapon allows Modifications to be added to it.
			// Open the Weapons XML file and locate the selected Weapon.
			XmlDocument objXmlDocument = XmlManager.Instance.Load("weapons.xml");
			XmlNode objXmlWeapon = objXmlDocument.SelectSingleNode("/chummer/weapons/weapon[name = \"" + objWeapon.Name + "\"]");

			if (objXmlWeapon == null)
			{
				MessageBox.Show(LanguageManager.Instance.GetString("Message_CannotModifyWeaponMod"), LanguageManager.Instance.GetString("MessageTitle_CannotModifyWeapon"), MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			if (objXmlWeapon["allowmod"] != null)
			{
				if (objXmlWeapon["allowmod"].InnerText == "false")
				{
					MessageBox.Show(LanguageManager.Instance.GetString("Message_CannotModifyWeaponMod"), LanguageManager.Instance.GetString("MessageTitle_CannotModifyWeapon"), MessageBoxButtons.OK, MessageBoxIcon.Information);
					return;
				}
			}

			// Set the Weapon properties for the window.
			frmPickVehicleMod.WeaponCost = objWeapon.Cost;
			frmPickVehicleMod.TotalWeaponCost = objWeapon.TotalCost;
			frmPickVehicleMod.ModMultiplier = objWeapon.ModMultiplier;
			frmPickVehicleMod.InputFile = "weapons";

			frmPickVehicleMod.ShowDialog(this);

			// Make sure the dialogue window was not canceled.
			if (frmPickVehicleMod.DialogResult == DialogResult.Cancel)
				return;

			// Locate the selected piece.
			XmlNode objXmlMod = objXmlDocument.SelectSingleNode("/chummer/mods/mod[name = \"" + frmPickVehicleMod.SelectedMod + "\"]");

			TreeNode objNode = new TreeNode();
			WeaponMod objMod = new WeaponMod(_objCharacter);
			objMod.Create(objXmlMod, objNode);
			objMod.Rating = frmPickVehicleMod.SelectedRating;
			objMod.Parent = objWeapon;

			objWeapon.WeaponMods.Add(objMod);
			
			objNode.Text = objMod.DisplayName;
			objNode.ContextMenuStrip = cmsWeaponMod;

			treWeapons.SelectedNode.Nodes.Add(objNode);
			treWeapons.SelectedNode.Expand();

			UpdateCharacterInfo();
			RefreshSelectedWeapon();

			if (frmPickVehicleMod.AddAgain)
				tsWeaponAddModification_Click(sender, e);
		}

		private void tsAddArmorMod_Click(object sender, EventArgs e)
		{
			// Make sure a parent item is selected, then open the Select Accessory window.
			try
			{
				if (treArmor.SelectedNode.Level == 0)
				{
					MessageBox.Show(LanguageManager.Instance.GetString("Message_SelectArmor"), LanguageManager.Instance.GetString("MessageTitle_SelectArmor"), MessageBoxButtons.OK, MessageBoxIcon.Information);
					return;
				}
			}
			catch
			{
				MessageBox.Show(LanguageManager.Instance.GetString("Message_SelectArmor"), LanguageManager.Instance.GetString("MessageTitle_SelectArmor"), MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			if (treArmor.SelectedNode.Level > 1)
				treArmor.SelectedNode = treArmor.SelectedNode.Parent;

			// Locate the Armor that is selected in the tree.
			Armor objArmor = _objFunctions.FindArmor(treArmor.SelectedNode.Tag.ToString(), _objCharacter.Armor);

			// Open the Armor XML file and locate the selected Armor.
			XmlDocument objXmlDocument = XmlManager.Instance.Load("armor.xml");

			XmlNode objXmlArmor = objXmlDocument.SelectSingleNode("/chummer/armors/armor[name = \"" + objArmor.Name + "\"]");

			frmSelectArmorMod frmPickArmorMod = new frmSelectArmorMod(_objCharacter);
			frmPickArmorMod.ArmorCost = objArmor.Cost;
			frmPickArmorMod.AllowedCategories = objArmor.Category + "," + objArmor.Name;
			frmPickArmorMod.CapacityDisplayStyle = objArmor.CapacityDisplayStyle;
			if (objXmlArmor.InnerXml.Contains("<addmodcategory>"))
				frmPickArmorMod.AllowedCategories += "," + objXmlArmor["addmodcategory"].InnerText;

			frmPickArmorMod.ShowDialog(this);

			if (frmPickArmorMod.DialogResult == DialogResult.Cancel)
				return;

			// Locate the selected piece.
			objXmlArmor = objXmlDocument.SelectSingleNode("/chummer/mods/mod[name = \"" + frmPickArmorMod.SelectedArmorMod + "\"]");

			TreeNode objNode = new TreeNode();
			ArmorMod objMod = new ArmorMod(_objCharacter);
			List<Weapon> lstWeapons = new List<Weapon>();
			List<TreeNode> lstWeaponNodes = new List<TreeNode>();
			int intRating = 0;
			if (Convert.ToInt32(objXmlArmor["maxrating"].InnerText) > 1)
				intRating = frmPickArmorMod.SelectedRating;
			
			objMod.Create(objXmlArmor, objNode, intRating, lstWeapons, lstWeaponNodes);
			objMod.Parent = objArmor;
			objNode.ContextMenuStrip = cmsArmorMod;
			if (objMod.InternalId == Guid.Empty.ToString())
				return;

			objArmor.ArmorMods.Add(objMod);

			treArmor.SelectedNode.Nodes.Add(objNode);
			treArmor.SelectedNode.Expand();
			treArmor.SelectedNode = objNode;

			// Add any Weapons created by the Mod.
			foreach (Weapon objWeapon in lstWeapons)
				_objCharacter.Weapons.Add(objWeapon);

			foreach (TreeNode objWeaponNode in lstWeaponNodes)
			{
				objWeaponNode.ContextMenuStrip = cmsWeapon;
				treWeapons.Nodes[0].Nodes.Add(objWeaponNode);
				treWeapons.Nodes[0].Expand();
			}

			UpdateCharacterInfo();
			RefreshSelectedArmor();

			if (frmPickArmorMod.AddAgain)
				tsAddArmorMod_Click(sender, e);
		}

		private void tsGearAddAsPlugin_Click(object sender, EventArgs e)
		{
			// Make sure a parent items is selected, then open the Select Gear window.
			try
			{
				if (treGear.SelectedNode.Level == 0)
				{
					MessageBox.Show(LanguageManager.Instance.GetString("Message_SelectGear"), LanguageManager.Instance.GetString("MessageTitle_SelectGear"), MessageBoxButtons.OK, MessageBoxIcon.Information);
					return;
				}
			}
			catch
			{
				MessageBox.Show(LanguageManager.Instance.GetString("Message_SelectGear"), LanguageManager.Instance.GetString("MessageTitle_SelectGear"), MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}
			
			bool blnAddAgain = PickGear();
			if (blnAddAgain)
				tsGearAddAsPlugin_Click(sender, e);
		}

		private void tsVehicleAddMod_Click(object sender, EventArgs e)
		{
			// Make sure a parent items is selected, then open the Select Vehicle Mod window.
			try
			{
				if (treVehicles.SelectedNode.Level == 0)
				{
					MessageBox.Show(LanguageManager.Instance.GetString("Message_SelectVehicle"), LanguageManager.Instance.GetString("MessageTitle_SelectVehicle"), MessageBoxButtons.OK, MessageBoxIcon.Information);
					return;
				}
			}
			catch
			{
				MessageBox.Show(LanguageManager.Instance.GetString("Message_SelectVehicle"), LanguageManager.Instance.GetString("MessageTitle_SelectVehicle"), MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			if (treVehicles.SelectedNode.Level > 1)
				treVehicles.SelectedNode = treVehicles.SelectedNode.Parent;

			Vehicle objSelectedVehicle = _objFunctions.FindVehicle(treVehicles.SelectedNode.Tag.ToString(), _objCharacter.Vehicles);

			frmSelectVehicleMod frmPickVehicleMod = new frmSelectVehicleMod(_objCharacter);
			// Set the Vehicle properties for the window.
			frmPickVehicleMod.VehicleCost = Convert.ToInt32(objSelectedVehicle.Cost);
			frmPickVehicleMod.Body = objSelectedVehicle.Body;
			frmPickVehicleMod.Speed = objSelectedVehicle.Speed;
			frmPickVehicleMod.AccelRunning = objSelectedVehicle.AccelRunning;
			frmPickVehicleMod.AccelWalking = objSelectedVehicle.AccelWalking;
			frmPickVehicleMod.DeviceRating = objSelectedVehicle.DeviceRating;
			frmPickVehicleMod.HasModularElectronics = objSelectedVehicle.HasModularElectronics();

			frmPickVehicleMod.ShowDialog(this);

			// Make sure the dialogue window was not canceled.
			if (frmPickVehicleMod.DialogResult == DialogResult.Cancel)
				return;

			// Open the Vehicles XML file and locate the selected piece.
			XmlDocument objXmlDocument = XmlManager.Instance.Load("vehicles.xml");
			
			XmlNode objXmlMod = objXmlDocument.SelectSingleNode("/chummer/mods/mod[name = \"" + frmPickVehicleMod.SelectedMod + "\"]");

			TreeNode objNode = new TreeNode();
			VehicleMod objMod = new VehicleMod(_objCharacter);
			objMod.Create(objXmlMod, objNode, frmPickVehicleMod.SelectedRating);

			// Make sure that the Armor Rating does not exceed the maximum allowed by the Vehicle.
			if (objMod.Name.StartsWith("Armor"))
			{
				if (objMod.Rating > objSelectedVehicle.MaxArmor)
				{
					objMod.Rating = objSelectedVehicle.MaxArmor;
					objNode.Text = objMod.DisplayName;
				}
			}

			objSelectedVehicle.Mods.Add(objMod);

			objNode.ContextMenuStrip = cmsVehicle;
			treVehicles.SelectedNode.Nodes.Add(objNode);
			treVehicles.SelectedNode.Expand();
			RefreshSelectedVehicle();

			// Check for Improved Sensor bonus.
			if (objMod.Bonus != null)
			{
				if (objMod.Bonus["selecttext"] != null)
				{
					frmSelectText frmPickText = new frmSelectText();
					frmPickText.Description = LanguageManager.Instance.GetString("String_Improvement_SelectText").Replace("{0}", objMod.DisplayNameShort);
					frmPickText.ShowDialog(this);
					objMod.Extra = frmPickText.SelectedValue;
					objNode.Text = objMod.DisplayName;
				}
				if (objMod.Bonus["improvesensor"] != null)
				{
					ChangeVehicleSensor(objSelectedVehicle, true);
				}
			}

			_blnIsDirty = true;
			UpdateWindowTitle();

			if (frmPickVehicleMod.AddAgain)
				tsVehicleAddMod_Click(sender, e);
		}

		private void tsVehicleAddWeaponWeapon_Click(object sender, EventArgs e)
		{
			VehicleMod objMod = new VehicleMod(_objCharacter);

			// Make sure that a Weapon Mount has been selected.
			try
			{
				// Attempt to locate the selected VehicleMod.
				Vehicle objFoundVehicle = new Vehicle(_objCharacter);
				objMod = _objFunctions.FindVehicleMod(treVehicles.SelectedNode.Tag.ToString(), _objCharacter.Vehicles, out objFoundVehicle);

				if (!objMod.Name.StartsWith("Weapon Mount") && !objMod.Name.StartsWith("Mechanical Arm"))
				{
					MessageBox.Show(LanguageManager.Instance.GetString("Message_CannotAddWeapon"), LanguageManager.Instance.GetString("MessageTitle_CannotAddWeapon"), MessageBoxButtons.OK, MessageBoxIcon.Information);
					return;
				}
			}
			catch
			{
				MessageBox.Show(LanguageManager.Instance.GetString("Message_CannotAddWeapon"), LanguageManager.Instance.GetString("MessageTitle_CannotAddWeapon"), MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			frmSelectWeapon frmPickWeapon = new frmSelectWeapon(_objCharacter);
			frmPickWeapon.ShowDialog();

			if (frmPickWeapon.DialogResult == DialogResult.Cancel)
				return;

			// Open the Weapons XML file and locate the selected piece.
			XmlDocument objXmlDocument = XmlManager.Instance.Load("weapons.xml");

			XmlNode objXmlWeapon = objXmlDocument.SelectSingleNode("/chummer/weapons/weapon[name = \"" + frmPickWeapon.SelectedWeapon + "\"]");

			TreeNode objNode = new TreeNode();
			Weapon objWeapon = new Weapon(_objCharacter);
			objWeapon.Create(objXmlWeapon, _objCharacter, objNode, cmsVehicleWeapon, cmsVehicleWeaponAccessory, cmsVehicleWeaponMod);
			objWeapon.VehicleMounted = true;

			objMod.Weapons.Add(objWeapon);

			objNode.ContextMenuStrip = cmsVehicleWeapon;
			treVehicles.SelectedNode.Nodes.Add(objNode);
			treVehicles.SelectedNode.Expand();

			if (frmPickWeapon.AddAgain)
				tsVehicleAddWeaponWeapon_Click(sender, e);

			UpdateCharacterInfo();
		}

		private void tsVehicleAddWeaponAccessory_Click(object sender, EventArgs e)
		{
			// Attempt to locate the selected VehicleWeapon.
			bool blnWeaponFound = false;
			Vehicle objFoundVehicle = new Vehicle(_objCharacter);
			Weapon objWeapon = _objFunctions.FindVehicleWeapon(treVehicles.SelectedNode.Tag.ToString(), _objCharacter.Vehicles, out objFoundVehicle);
			if (objWeapon != null)
				blnWeaponFound = true;

			if (!blnWeaponFound)
			{
				MessageBox.Show(LanguageManager.Instance.GetString("Message_VehicleWeaponAccessories"), LanguageManager.Instance.GetString("MessageTitle_VehicleWeaponAccessories"), MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			// Open the Weapons XML file and locate the selected Weapon.
			XmlDocument objXmlDocument = XmlManager.Instance.Load("weapons.xml");

			XmlNode objXmlWeapon = objXmlDocument.SelectSingleNode("/chummer/weapons/weapon[name = \"" + treVehicles.SelectedNode.Text + "\"]");

			frmSelectWeaponAccessory frmPickWeaponAccessory = new frmSelectWeaponAccessory(_objCharacter);

			if (objXmlWeapon == null)
			{
				MessageBox.Show(LanguageManager.Instance.GetString("Message_CannotModifyWeapon"), LanguageManager.Instance.GetString("MessageTitle_CannotModifyWeapon"), MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			// Make sure the Weapon allows Accessories to be added to it.
			if (!Convert.ToBoolean(objXmlWeapon["allowaccessory"].InnerText))
			{
				MessageBox.Show(LanguageManager.Instance.GetString("Message_CannotModifyWeapon"), LanguageManager.Instance.GetString("MessageTitle_CannotModifyWeapon"), MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}
			else
			{
				XmlNodeList objXmlMountList = objXmlWeapon.SelectNodes("accessorymounts/mount");
				string strMounts = "";
				foreach (XmlNode objXmlMount in objXmlMountList)
				{
					// Run through the Weapon's currenct Accessories and filter out any used up Mount points.
					bool blnFound = false;
					foreach (WeaponAccessory objCurrentAccessory in objWeapon.WeaponAccessories)
					{
						if (objCurrentAccessory.Mount == objXmlMount.InnerText)
							blnFound = true;
					}
					if (!blnFound)
						strMounts += objXmlMount.InnerText + "/";
				}
				frmPickWeaponAccessory.AllowedMounts = strMounts;
			}

			frmPickWeaponAccessory.WeaponCost = objWeapon.Cost;
			frmPickWeaponAccessory.AccessoryMultiplier = objWeapon.AccessoryMultiplier;
			frmPickWeaponAccessory.ShowDialog();

			if (frmPickWeaponAccessory.DialogResult == DialogResult.Cancel)
				return;

			// Locate the selected piece.
			objXmlWeapon = objXmlDocument.SelectSingleNode("/chummer/accessories/accessory[name = \"" + frmPickWeaponAccessory.SelectedAccessory + "\"]");

			TreeNode objNode = new TreeNode();
			WeaponAccessory objAccessory = new WeaponAccessory(_objCharacter);
			objAccessory.Create(objXmlWeapon, objNode, frmPickWeaponAccessory.SelectedMount);
			objAccessory.Parent = objWeapon;
			objWeapon.WeaponAccessories.Add(objAccessory);

			objNode.ContextMenuStrip = cmsVehicleWeaponAccessory;
			treVehicles.SelectedNode.Nodes.Add(objNode);
			treVehicles.SelectedNode.Expand();

			if (frmPickWeaponAccessory.AddAgain)
				tsVehicleAddWeaponAccessory_Click(sender, e);

			UpdateCharacterInfo();
		}

		private void tsVehicleAddWeaponModification_Click(object sender, EventArgs e)
		{
			// Attempt to locate the selected VehicleWeapon.
			bool blnFound = false;
			Vehicle objFoundVehicle = new Vehicle(_objCharacter);
			Weapon objWeapon = _objFunctions.FindVehicleWeapon(treVehicles.SelectedNode.Tag.ToString(), _objCharacter.Vehicles, out objFoundVehicle);
			if (objWeapon != null)
				blnFound = true;

			if (!blnFound)
			{
				MessageBox.Show(LanguageManager.Instance.GetString("Message_VehicleWeaponMods"), LanguageManager.Instance.GetString("MessageTitle_VehicleWeaponMods"), MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			frmSelectVehicleMod frmPickVehicleMod = new frmSelectVehicleMod(_objCharacter);

			// Make sure the Weapon allows Modifications to be added to it.
			// Open the Weapons XML file and locate the selected Weapon.
			XmlDocument objXmlDocument = XmlManager.Instance.Load("weapons.xml");
			XmlNode objXmlWeapon = objXmlDocument.SelectSingleNode("/chummer/weapons/weapon[name = \"" + objWeapon.Name + "\"]");

			if (objXmlWeapon == null)
			{
				MessageBox.Show(LanguageManager.Instance.GetString("Message_CannotModifyWeaponMod"), LanguageManager.Instance.GetString("MessageTitle_CannotModifyWeapon"), MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			if (objXmlWeapon["allowmod"] != null)
			{
				if (objXmlWeapon["allowmod"].InnerText == "false")
				{
					MessageBox.Show(LanguageManager.Instance.GetString("Message_CannotModifyWeaponMod"), LanguageManager.Instance.GetString("MessageTitle_CannotModifyWeapon"), MessageBoxButtons.OK, MessageBoxIcon.Information);
					return;
				}
			}

			// Set the Weapon properties for the window.
			frmPickVehicleMod.WeaponCost = objWeapon.Cost;
			frmPickVehicleMod.TotalWeaponCost = objWeapon.TotalCost;
			frmPickVehicleMod.ModMultiplier = objWeapon.ModMultiplier;
			frmPickVehicleMod.InputFile = "weapons";

			frmPickVehicleMod.ShowDialog(this);

			// Make sure the dialogue window was not canceled.
			if (frmPickVehicleMod.DialogResult == DialogResult.Cancel)
				return;

			// Locate the selected piece.
			XmlNode objXmlMod = objXmlDocument.SelectSingleNode("/chummer/mods/mod[name = \"" + frmPickVehicleMod.SelectedMod + "\"]");

			TreeNode objNode = new TreeNode();
			WeaponMod objMod = new WeaponMod(_objCharacter);
			objMod.Create(objXmlMod, objNode);
			objMod.Rating = frmPickVehicleMod.SelectedRating;
			objMod.Parent = objWeapon;

			objWeapon.WeaponMods.Add(objMod);
			objNode.Text = objMod.DisplayName;
			objNode.ContextMenuStrip = cmsVehicleWeaponMod;

			treVehicles.SelectedNode.Nodes.Add(objNode);
			treVehicles.SelectedNode.Expand();

			if (frmPickVehicleMod.AddAgain)
				tsVehicleAddWeaponModification_Click(sender, e);

			UpdateCharacterInfo();
		}

		private void tsVehicleAddUnderbarrelWeapon_Click(object sender, EventArgs e)
		{
			// Attempt to locate the selected VehicleWeapon.
			bool blnWeaponFound = false;
			Vehicle objFoundVehicle = new Vehicle(_objCharacter);
			Weapon objSelectedWeapon = _objFunctions.FindVehicleWeapon(treVehicles.SelectedNode.Tag.ToString(), _objCharacter.Vehicles, out objFoundVehicle);
			if (objSelectedWeapon != null)
				blnWeaponFound = true;

			if (!blnWeaponFound)
			{
				MessageBox.Show(LanguageManager.Instance.GetString("Message_VehicleWeaponUnderbarrel"), LanguageManager.Instance.GetString("MessageTitle_VehicleWeaponUnderbarrel"), MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			frmSelectWeapon frmPickWeapon = new frmSelectWeapon(_objCharacter);
			frmPickWeapon.ShowDialog(this);

			// Make sure the dialogue window was not canceled.
			if (frmPickWeapon.DialogResult == DialogResult.Cancel)
				return;

			// Open the Weapons XML file and locate the selected piece.
			XmlDocument objXmlDocument = XmlManager.Instance.Load("weapons.xml");

			XmlNode objXmlWeapon = objXmlDocument.SelectSingleNode("/chummer/weapons/weapon[name = \"" + frmPickWeapon.SelectedWeapon + "\"]");

			TreeNode objNode = new TreeNode();
			Weapon objWeapon = new Weapon(_objCharacter);
			objWeapon.Create(objXmlWeapon, _objCharacter, objNode, cmsVehicleWeapon, cmsVehicleWeaponAccessory, cmsVehicleWeaponMod);
			objWeapon.VehicleMounted = true;
			objWeapon.IsUnderbarrelWeapon = true;
			objSelectedWeapon.UnderbarrelWeapons.Add(objWeapon);

			objNode.ContextMenuStrip = cmsVehicleWeapon;
			treVehicles.SelectedNode.Nodes.Add(objNode);
			treVehicles.SelectedNode.Expand();
			//treWeapons.SelectedNode = objNode;

			UpdateCharacterInfo();
		}

		private void tsVehicleAddWeaponAccessoryAlt_Click(object sender, EventArgs e)
		{
			tsVehicleAddWeaponAccessory_Click(sender, e);
		}

		private void tsVehicleAddWeaponModificationAlt_Click(object sender, EventArgs e)
		{
			tsVehicleAddWeaponModification_Click(sender, e);
		}

		private void tsVehicleAddUnderbarrelWeaponAlt_Click(object sender, EventArgs e)
		{
			tsVehicleAddUnderbarrelWeapon_Click(sender, e);
		}

		private void tsMartialArtsAddAdvantage_Click(object sender, EventArgs e)
		{
			try
			{
				// Select the Martial Arts node if we're currently on a child.
				if (treMartialArts.SelectedNode.Level > 1)
					treMartialArts.SelectedNode = treMartialArts.SelectedNode.Parent;

				MartialArt objMartialArt = _objFunctions.FindMartialArt(treMartialArts.SelectedNode.Tag.ToString(), _objCharacter.MartialArts);

				// Make sure the user is not trying to add more Advantages than they are allowed (1 per Rating for the selected Martial Art).
				if (objMartialArt.Advantages.Count >= objMartialArt.Rating && !_objCharacter.IgnoreRules)
				{
					MessageBox.Show(LanguageManager.Instance.GetString("Message_MartialArtAdvantageLimit").Replace("{0}", objMartialArt.Name), LanguageManager.Instance.GetString("MessageTitle_MartialArtAdvantageLimit"), MessageBoxButtons.OK, MessageBoxIcon.Information);
					return;
				}

				frmSelectMartialArtAdvantage frmPickMartialArtAdvantage = new frmSelectMartialArtAdvantage(_objCharacter);
				frmPickMartialArtAdvantage.MartialArt = objMartialArt.Name;
				frmPickMartialArtAdvantage.ShowDialog(this);

				if (frmPickMartialArtAdvantage.DialogResult == DialogResult.Cancel)
					return;

				// Open the Martial Arts XML file and locate the selected piece.
				XmlDocument objXmlDocument = XmlManager.Instance.Load("martialarts.xml");

				XmlNode objXmlAdvantage = objXmlDocument.SelectSingleNode("/chummer/martialarts/martialart[name = \"" + objMartialArt.Name + "\"]/advantages/advantage[name = \"" + frmPickMartialArtAdvantage.SelectedAdvantage + "\"]");

				// Create the Improvements for the Advantage if there are any.
				TreeNode objNode = new TreeNode();
				MartialArtAdvantage objAdvantage = new MartialArtAdvantage(_objCharacter);
				objAdvantage.Create(objXmlAdvantage, _objCharacter, objNode);
				if (objAdvantage.InternalId == Guid.Empty.ToString())
					return;

				objMartialArt.Advantages.Add(objAdvantage);

				treMartialArts.SelectedNode.Nodes.Add(objNode);
				treMartialArts.SelectedNode.Expand();

				CalculateBP();
				UpdateCharacterInfo();
			}
			catch
			{
				MessageBox.Show(LanguageManager.Instance.GetString("Message_SelectMartialArtAdvantage"), LanguageManager.Instance.GetString("MessageTitle_SelectMartialArtAdvantage"), MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
		}

		private void tsVehicleAddGear_Click(object sender, EventArgs e)
		{
			// Make sure a parent items is selected, then open the Select Gear window.
			try
			{
				if (treVehicles.SelectedNode.Level == 0)
				{
					MessageBox.Show(LanguageManager.Instance.GetString("Message_SelectGearVehicle"), LanguageManager.Instance.GetString("MessageTitle_SelectGearVehicle"), MessageBoxButtons.OK, MessageBoxIcon.Information);
					return;
				}
			}
			catch
			{
				MessageBox.Show(LanguageManager.Instance.GetString("Message_SelectGearVehicle"), LanguageManager.Instance.GetString("MessageTitle_SelectGearVehicle"), MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			if (treVehicles.SelectedNode.Level > 1)
				treVehicles.SelectedNode = treVehicles.SelectedNode.Parent;

			// Locate the selected Vehicle.
			Vehicle objSelectedVehicle = _objFunctions.FindVehicle(treVehicles.SelectedNode.Tag.ToString(), _objCharacter.Vehicles);

			frmSelectGear frmPickGear = new frmSelectGear(_objCharacter);
			frmPickGear.ShowPositiveCapacityOnly = true;
			frmPickGear.ShowDialog(this);

			if (frmPickGear.DialogResult == DialogResult.Cancel)
				return;

			// Open the Gear XML file and locate the selected piece.
			XmlDocument objXmlDocument = XmlManager.Instance.Load("gear.xml");
			XmlNode objXmlGear = objXmlDocument.SelectSingleNode("/chummer/gears/gear[name = \"" + frmPickGear.SelectedGear + "\" and category = \"" + frmPickGear.SelectedCategory + "\"]");

			// Create the new piece of Gear.
			List<Weapon> objWeapons = new List<Weapon>();
			List<TreeNode> objWeaponNodes = new List<TreeNode>();
			TreeNode objNode = new TreeNode();
			Gear objGear = new Gear(_objCharacter);

			switch (frmPickGear.SelectedCategory)
			{
				case "Commlink":
				case "Commlink Upgrade":
					Commlink objCommlink = new Commlink(_objCharacter);
					objCommlink.Create(objXmlGear, _objCharacter, objNode, frmPickGear.SelectedRating, false);
					objCommlink.Quantity = frmPickGear.SelectedQty;

					objGear = objCommlink;
					break;
				case "Commlink Operating System":
				case "Commlink Operating System Upgrade":
					OperatingSystem objOperatingSystem = new OperatingSystem(_objCharacter);
					objOperatingSystem.Create(objXmlGear, _objCharacter, objNode, frmPickGear.SelectedRating, false);
					objOperatingSystem.Quantity = frmPickGear.SelectedQty;

					objGear = objOperatingSystem;
					break;
				default:
					Gear objNewGear = new Gear(_objCharacter);
					objNewGear.Create(objXmlGear, _objCharacter, objNode, frmPickGear.SelectedRating, objWeapons, objWeaponNodes, "", frmPickGear.Hacked, frmPickGear.InherentProgram, false, true, frmPickGear.Aerodynamic);
					objNewGear.Quantity = frmPickGear.SelectedQty;

					objGear = objNewGear;
					break;
			}

			if (objGear.InternalId == Guid.Empty.ToString())
				return;

			// Reduce the cost for Do It Yourself components.
			if (frmPickGear.DoItYourself)
				objGear.Cost = (Convert.ToDouble(objGear.Cost, GlobalOptions.Instance.CultureInfo) * 0.5).ToString();
			// Reduce the cost to 10% for Hacked programs.
			if (frmPickGear.Hacked)
			{
				if (objGear.Cost != "")
					objGear.Cost = "(" + objGear.Cost + ") * 0.1";
				if (objGear.Cost3 != "")
					objGear.Cost3 = "(" + objGear.Cost3 + ") * 0.1";
				if (objGear.Cost6 != "")
					objGear.Cost6 = "(" + objGear.Cost6 + ") * 0.1";
				if (objGear.Cost10 != "")
					objGear.Cost10 = "(" + objGear.Cost10 + ") * 0.1";
				if (objGear.Extra == "")
					objGear.Extra = LanguageManager.Instance.GetString("Label_SelectGear_Hacked");
				else
					objGear.Extra += ", " + LanguageManager.Instance.GetString("Label_SelectGear_Hacked");
			}
			// If the item was marked as free, change its cost.
			if (frmPickGear.FreeCost)
			{
				objGear.Cost = "0";
				objGear.Cost3 = "0";
				objGear.Cost6 = "0";
				objGear.Cost10 = "0";
			}

			objGear.Quantity = frmPickGear.SelectedQty;
			objNode.Text = objGear.DisplayName;
			try
			{
				nudVehicleRating.Increment = objGear.CostFor;
				nudVehicleRating.Minimum = nudGearQty.Increment;
			}
			catch
			{
			}

			// Change the cost of the Sensor itself to 0.
			if (frmPickGear.SelectedCategory == "Sensors")
			{
				objGear.Cost = "0";
				objGear.Cost3 = "0";
				objGear.Cost6 = "0";
				objGear.Cost10 = "0";
			}

			objNode.ContextMenuStrip = cmsVehicleGear;

			bool blnMatchFound = false;
			// If this is Ammunition, see if the character already has it on them.
			if (objGear.Category == "Ammunition")
			{
				foreach (Gear objVehicleGear in objSelectedVehicle.Gear)
				{
					if (objVehicleGear.Name == objGear.Name && objVehicleGear.Category == objGear.Category && objVehicleGear.Rating == objGear.Rating && objVehicleGear.Extra == objGear.Extra)
					{
						// A match was found, so increase the quantity instead.
						objVehicleGear.Quantity += objGear.Quantity;
						blnMatchFound = true;

						foreach (TreeNode objGearNode in treVehicles.SelectedNode.Nodes)
						{
							if (objVehicleGear.InternalId == objGearNode.Tag.ToString())
							{
								objGearNode.Text = objVehicleGear.DisplayName;
								break;
							}
						}

						break;
					}
				}
			}

			if (!blnMatchFound)
			{
				treVehicles.SelectedNode.Nodes.Add(objNode);
				treVehicles.SelectedNode.Expand();

				// Add the Gear to the Vehicle.
				objSelectedVehicle.Gear.Add(objGear);
			}

			if (frmPickGear.AddAgain)
				tsVehicleAddGear_Click(sender, e);

			UpdateCharacterInfo();
			RefreshSelectedVehicle();
		}

		private void tsVehicleSensorAddAsPlugin_Click(object sender, EventArgs e)
		{
			// Make sure a parent items is selected, then open the Select Gear window.
			try
			{
				if (treVehicles.SelectedNode.Level < 2)
				{
					MessageBox.Show(LanguageManager.Instance.GetString("Message_ModifyVehicleGear"), LanguageManager.Instance.GetString("MessageTitle_ModifyVehicleGear"), MessageBoxButtons.OK, MessageBoxIcon.Information);
					return;
				}
			}
			catch
			{
				MessageBox.Show(LanguageManager.Instance.GetString("Message_ModifyVehicleGear"), LanguageManager.Instance.GetString("MessageTitle_ModifyVehicleGear"), MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			// Locate the Vehicle Sensor Gear.
			bool blnFound = false;
			Vehicle objVehicle = new Vehicle(_objCharacter);
			Gear objSensor = _objFunctions.FindVehicleGear(treVehicles.SelectedNode.Tag.ToString(), _objCharacter.Vehicles, out objVehicle);
			if (objSensor != null)
				blnFound = true;

			// Make sure the Gear was found.
			if (!blnFound)
			{
				MessageBox.Show(LanguageManager.Instance.GetString("Message_ModifyVehicleGear"), LanguageManager.Instance.GetString("MessageTitle_ModifyVehicleGear"), MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			XmlDocument objXmlDocument = XmlManager.Instance.Load("gear.xml");

			XmlNode objXmlGear = objXmlDocument.SelectSingleNode("/chummer/gears/gear[name = \"" + objSensor.Name + "\" and category = \"" + objSensor.Category + "\"]");

			frmSelectGear frmPickGear = new frmSelectGear(_objCharacter);
			//frmPickGear.ShowNegativeCapacityOnly = true;

			if (objXmlGear != null)
			{
				if (objXmlGear.InnerXml.Contains("<addoncategory>"))
				{
					string strCategories = "";
					foreach (XmlNode objXmlCategory in objXmlGear.SelectNodes("addoncategory"))
						strCategories += objXmlCategory.InnerText + ",";
					// Remove the trailing comma.
					strCategories = strCategories.Substring(0, strCategories.Length - 1);
					frmPickGear.AddCategory(strCategories);
				}
			}

			if (frmPickGear.AllowedCategories != "")
				frmPickGear.AllowedCategories += objSensor.Category + ",";
			
			frmPickGear.ShowDialog(this);

			if (frmPickGear.DialogResult == DialogResult.Cancel)
				return;

			// Open the Gear XML file and locate the selected piece.
			objXmlGear = objXmlDocument.SelectSingleNode("/chummer/gears/gear[name = \"" + frmPickGear.SelectedGear + "\" and category = \"" + frmPickGear.SelectedCategory + "\"]");

			// Create the new piece of Gear.
			List<Weapon> objWeapons = new List<Weapon>();
			List<TreeNode> objWeaponNodes = new List<TreeNode>();
			TreeNode objNode = new TreeNode();
			Gear objGear = new Gear(_objCharacter);

			switch (frmPickGear.SelectedCategory)
			{
				case "Commlink":
				case "Commlink Upgrade":
					Commlink objCommlink = new Commlink(_objCharacter);
					objCommlink.Create(objXmlGear, _objCharacter, objNode, frmPickGear.SelectedRating, false);
					objCommlink.Quantity = frmPickGear.SelectedQty;
					objNode.Text = objCommlink.DisplayName;

					objGear = objCommlink;
					break;
				case "Commlink Operating System":
				case "Commlink Operating System Upgrade":
					OperatingSystem objOperatingSystem = new OperatingSystem(_objCharacter);
					objOperatingSystem.Create(objXmlGear, _objCharacter, objNode, frmPickGear.SelectedRating, false);
					objOperatingSystem.Quantity = frmPickGear.SelectedQty;
					objNode.Text = objOperatingSystem.DisplayName;

					objGear = objOperatingSystem;
					break;
				default:
					Gear objNewGear = new Gear(_objCharacter);
					objNewGear.Create(objXmlGear, _objCharacter, objNode, frmPickGear.SelectedRating, objWeapons, objWeaponNodes, "", frmPickGear.Hacked, frmPickGear.InherentProgram, false, true, frmPickGear.Aerodynamic);
					objNewGear.Quantity = frmPickGear.SelectedQty;
					objNode.Text = objNewGear.DisplayName;

					objGear = objNewGear;
					break;
			}

			if (objGear.InternalId == Guid.Empty.ToString())
				return;

			// Reduce the cost for Do It Yourself components.
			if (frmPickGear.DoItYourself)
				objGear.Cost = (Convert.ToDouble(objGear.Cost, GlobalOptions.Instance.CultureInfo) * 0.5).ToString();
			// Reduce the cost to 10% for Hacked programs.
			if (frmPickGear.Hacked)
			{
				if (objGear.Cost != "")
					objGear.Cost = "(" + objGear.Cost + ") * 0.1";
				if (objGear.Cost3 != "")
					objGear.Cost3 = "(" + objGear.Cost3 + ") * 0.1";
				if (objGear.Cost6 != "")
					objGear.Cost6 = "(" + objGear.Cost6 + ") * 0.1";
				if (objGear.Cost10 != "")
					objGear.Cost10 = "(" + objGear.Cost10 + ") * 0.1";
				if (objGear.Extra == "")
					objGear.Extra = LanguageManager.Instance.GetString("Label_SelectGear_Hacked");
				else
					objGear.Extra += ", " + LanguageManager.Instance.GetString("Label_SelectGear_Hacked");
			}
			// If the item was marked as free, change its cost.
			if (frmPickGear.FreeCost)
			{
				objGear.Cost = "0";
				objGear.Cost3 = "0";
				objGear.Cost6 = "0";
				objGear.Cost10 = "0";
			}

			objNode.Text = objGear.DisplayName;

			try
			{
				_blnSkipRefresh = true;
				nudVehicleGearQty.Increment = objGear.CostFor;
				//nudVehicleGearQty.Minimum = objGear.CostFor;
				_blnSkipRefresh = false;
			}
			catch
			{
			}

			objGear.Parent = objSensor;
			objNode.ContextMenuStrip = cmsVehicleGear;

			treVehicles.SelectedNode.Nodes.Add(objNode);
			treVehicles.SelectedNode.Expand();

			objSensor.Children.Add(objGear);

			if (frmPickGear.AddAgain)
				tsVehicleSensorAddAsPlugin_Click(sender, e);

			UpdateCharacterInfo();
			RefreshSelectedVehicle();
		}

		private void tsVehicleGearAddAsPlugin_Click(object sender, EventArgs e)
		{
			tsVehicleSensorAddAsPlugin_Click(sender, e);
		}

		private void tsVehicleGearNotes_Click(object sender, EventArgs e)
		{
			try
			{
				bool blnFound = false;
				Vehicle objVehicle = new Vehicle(_objCharacter);
				Gear objGear = _objFunctions.FindVehicleGear(treVehicles.SelectedNode.Tag.ToString(), _objCharacter.Vehicles, out objVehicle);
				if (objGear != null)
					blnFound = true;

				if (blnFound)
				{
					frmNotes frmItemNotes = new frmNotes();
					frmItemNotes.Notes = objGear.Notes;
					string strOldValue = objGear.Notes;
					frmItemNotes.ShowDialog(this);

					if (frmItemNotes.DialogResult == DialogResult.OK)
					{
						objGear.Notes = frmItemNotes.Notes;
						if (objGear.Notes != strOldValue)
						{
							_blnIsDirty = true;
							UpdateWindowTitle();
						}
					}

					if (objGear.Notes != string.Empty)
						treVehicles.SelectedNode.ForeColor = Color.SaddleBrown;
					else
						treVehicles.SelectedNode.ForeColor = SystemColors.WindowText;
					treVehicles.SelectedNode.ToolTipText = objGear.Notes;
				}
			}
			catch
			{
			}
		}

		private void tsAdvancedLifestyle_Click(object sender, EventArgs e)
		{
			Lifestyle objNewLifestyle = new Lifestyle(_objCharacter);
			frmSelectAdvancedLifestyle frmPickLifestyle = new frmSelectAdvancedLifestyle(objNewLifestyle, _objCharacter);
			frmPickLifestyle.ShowDialog(this);

			// Make sure the dialogue window was not canceled.
			if (frmPickLifestyle.DialogResult == DialogResult.Cancel)
				return;

			objNewLifestyle.StyleType = LifestyleType.Advanced;

			_objCharacter.Lifestyles.Add(objNewLifestyle);

			TreeNode objNode = new TreeNode();
			objNode.Text = objNewLifestyle.Name;
			objNode.Tag = objNewLifestyle.InternalId;
			objNode.ContextMenuStrip = cmsAdvancedLifestyle;
			treLifestyles.Nodes[0].Nodes.Add(objNode);
			treLifestyles.Nodes[0].Expand();

			if (frmPickLifestyle.AddAgain)
				tsAdvancedLifestyle_Click(sender, e);

			UpdateCharacterInfo();
		}

		private void tsBoltHole_Click(object sender, EventArgs e)
		{
			Lifestyle objNewLifestyle = new Lifestyle(_objCharacter);
			frmSelectAdvancedLifestyle frmPickLifestyle = new frmSelectAdvancedLifestyle(objNewLifestyle, _objCharacter);
			frmPickLifestyle.StyleType = LifestyleType.BoltHole;
			frmPickLifestyle.ShowDialog(this);

			// Make sure the dialogue window was not canceled.
			if (frmPickLifestyle.DialogResult == DialogResult.Cancel)
				return;

			_objCharacter.Lifestyles.Add(objNewLifestyle);

			TreeNode objNode = new TreeNode();
			objNode.Text = objNewLifestyle.Name;
			objNode.Tag = objNewLifestyle.InternalId;
			objNode.ContextMenuStrip = cmsAdvancedLifestyle;
			treLifestyles.Nodes[0].Nodes.Add(objNode);
			treLifestyles.Nodes[0].Expand();

			if (frmPickLifestyle.AddAgain)
				tsAdvancedLifestyle_Click(sender, e);

			UpdateCharacterInfo();
		}

		private void tsSafehouse_Click(object sender, EventArgs e)
		{
			Lifestyle objNewLifestyle = new Lifestyle(_objCharacter);
			frmSelectAdvancedLifestyle frmPickLifestyle = new frmSelectAdvancedLifestyle(objNewLifestyle, _objCharacter);
			frmPickLifestyle.StyleType = LifestyleType.Safehouse;
			frmPickLifestyle.ShowDialog(this);

			// Make sure the dialogue window was not canceled.
			if (frmPickLifestyle.DialogResult == DialogResult.Cancel)
				return;

			_objCharacter.Lifestyles.Add(objNewLifestyle);

			TreeNode objNode = new TreeNode();
			objNode.Text = objNewLifestyle.Name;
			objNode.Tag = objNewLifestyle.InternalId;
			objNode.ContextMenuStrip = cmsAdvancedLifestyle;
			treLifestyles.Nodes[0].Nodes.Add(objNode);
			treLifestyles.Nodes[0].Expand();

			if (frmPickLifestyle.AddAgain)
				tsAdvancedLifestyle_Click(sender, e);

			UpdateCharacterInfo();
		}

		private void tsWeaponName_Click(object sender, EventArgs e)
		{
			// Make sure a parent item is selected, then open the Select Accessory window.
			try
			{
				if (treWeapons.SelectedNode.Level == 0)
				{
					MessageBox.Show(LanguageManager.Instance.GetString("Message_SelectWeaponName"), LanguageManager.Instance.GetString("MessageTitle_SelectWeapon"), MessageBoxButtons.OK, MessageBoxIcon.Information);
					return;
				}
			}
			catch
			{
				MessageBox.Show(LanguageManager.Instance.GetString("Message_SelectWeaponName"), LanguageManager.Instance.GetString("MessageTitle_SelectWeapon"), MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			if (treWeapons.SelectedNode.Level > 1)
				treWeapons.SelectedNode = treWeapons.SelectedNode.Parent;

			// Get the information for the currently selected Weapon.
			Weapon objWeapon = _objFunctions.FindWeapon(treWeapons.SelectedNode.Tag.ToString(), _objCharacter.Weapons);
			if (objWeapon == null)
				return;

			frmSelectText frmPickText = new frmSelectText();
			frmPickText.Description = LanguageManager.Instance.GetString("String_WeaponName");
			frmPickText.ShowDialog(this);

			if (frmPickText.DialogResult == DialogResult.Cancel)
				return;

			objWeapon.WeaponName = frmPickText.SelectedValue;
			treWeapons.SelectedNode.Text = objWeapon.DisplayName;

			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void tsGearName_Click(object sender, EventArgs e)
		{
			try
			{
				if (treGear.SelectedNode.Level == 0)
				{
					MessageBox.Show(LanguageManager.Instance.GetString("Message_SelectGearName"), LanguageManager.Instance.GetString("MessageTitle_SelectGear"), MessageBoxButtons.OK, MessageBoxIcon.Information);
					return;
				}
			}
			catch
			{
				MessageBox.Show(LanguageManager.Instance.GetString("Message_SelectGearName"), LanguageManager.Instance.GetString("MessageTitle_SelectGear"), MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			// Get the information for the currently selected Gear.
			Gear objGear = _objFunctions.FindGear(treGear.SelectedNode.Tag.ToString(), _objCharacter.Gear);
			if (objGear == null)
				return;

			frmSelectText frmPickText = new frmSelectText();
			frmPickText.Description = LanguageManager.Instance.GetString("String_GearName");
			frmPickText.ShowDialog(this);

			if (frmPickText.DialogResult == DialogResult.Cancel)
				return;

			objGear.GearName = frmPickText.SelectedValue;
			treGear.SelectedNode.Text = objGear.DisplayName;

			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void tsWeaponAddUnderbarrel_Click(object sender, EventArgs e)
		{
			// Make sure a parent item is selected, then open the Select Accessory window.
			try
			{
				if (treWeapons.SelectedNode.Level == 0)
				{
					MessageBox.Show(LanguageManager.Instance.GetString("Message_SelectWeaponUnderbarrel"), LanguageManager.Instance.GetString("MessageTitle_SelectWeapon"), MessageBoxButtons.OK, MessageBoxIcon.Information);
					return;
				}
			}
			catch
			{
				MessageBox.Show(LanguageManager.Instance.GetString("Message_SelectWeaponUnderbarrel"), LanguageManager.Instance.GetString("MessageTitle_SelectWeapon"), MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			if (treWeapons.SelectedNode.Level > 1)
				treWeapons.SelectedNode = treWeapons.SelectedNode.Parent;

			// Get the information for the currently selected Weapon.
			foreach (Weapon objCharacterWeapon in _objCharacter.Weapons)
			{
				if (treWeapons.SelectedNode.Tag.ToString() == objCharacterWeapon.InternalId)
				{
					if (objCharacterWeapon.InternalId == treWeapons.SelectedNode.Tag.ToString())
					{
						if (objCharacterWeapon.Category.StartsWith("Cyberware"))
						{
							MessageBox.Show(LanguageManager.Instance.GetString("Message_CyberwareUnderbarrel"), LanguageManager.Instance.GetString("MessageTitle_WeaponUnderbarrel"), MessageBoxButtons.OK, MessageBoxIcon.Information);
							return;
						}
					}
				}
			}

			// Locate the Weapon that is selected in the tree.
			Weapon objSelectedWeapon = _objFunctions.FindWeapon(treWeapons.SelectedNode.Tag.ToString(), _objCharacter.Weapons);
			if (objSelectedWeapon == null)
				return;

			frmSelectWeapon frmPickWeapon = new frmSelectWeapon(_objCharacter);
			frmPickWeapon.ShowDialog(this);

			// Make sure the dialogue window was not canceled.
			if (frmPickWeapon.DialogResult == DialogResult.Cancel)
				return;

			// Open the Weapons XML file and locate the selected piece.
			XmlDocument objXmlDocument = XmlManager.Instance.Load("weapons.xml");

			XmlNode objXmlWeapon = objXmlDocument.SelectSingleNode("/chummer/weapons/weapon[name = \"" + frmPickWeapon.SelectedWeapon + "\"]");

			TreeNode objNode = new TreeNode();
			Weapon objWeapon = new Weapon(_objCharacter);
			objWeapon.Create(objXmlWeapon, _objCharacter, objNode, cmsWeapon, cmsWeaponAccessory, cmsWeaponMod);
			objWeapon.IsUnderbarrelWeapon = true;
			objSelectedWeapon.UnderbarrelWeapons.Add(objWeapon);

			objNode.ContextMenuStrip = cmsWeapon;
			treWeapons.SelectedNode.Nodes.Add(objNode);
			treWeapons.SelectedNode.Expand();
			//treWeapons.SelectedNode = objNode;

			UpdateCharacterInfo();
			RefreshSelectedWeapon();
		}

		private void tsAddComplexFormOption_Click(object sender, EventArgs e)
		{
			try
			{
				if (treComplexForms.SelectedNode.Level == 0)
				{
					MessageBox.Show(LanguageManager.Instance.GetString("Message_SelectComplexForm"), LanguageManager.Instance.GetString("MessageTitle_SelectComplexForm"), MessageBoxButtons.OK, MessageBoxIcon.Information);
					return;
				}
			}
			catch
			{
				MessageBox.Show(LanguageManager.Instance.GetString("Message_SelectComplexForm"), LanguageManager.Instance.GetString("MessageTitle_SelectComplexForm"), MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			if (treComplexForms.SelectedNode.Level > 1)
				treComplexForms.SelectedNode = treComplexForms.SelectedNode.Parent;

			// Locate the Program that is selected in the tree.
			TechProgram objProgram = _objFunctions.FindTechProgram(treComplexForms.SelectedNode.Tag.ToString(), _objCharacter.TechPrograms);

			if (objProgram.CalculatedCapacity == 0)
			{
				MessageBox.Show(LanguageManager.Instance.GetString("Message_CannotAddComplexFormOption"), LanguageManager.Instance.GetString("MessageTitle_CannotAddComplexFormOption"), MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}
			else
			{
				if (objProgram.Options.Count >= objProgram.CalculatedCapacity && !_objCharacter.IgnoreRules)
				{
					MessageBox.Show(LanguageManager.Instance.GetString("Message_ConntAddComplexFormOptionLimit").Replace("{0}", objProgram.CalculatedCapacity.ToString()), LanguageManager.Instance.GetString("MessageTitle_CannotAddComplexFormOption"), MessageBoxButtons.OK, MessageBoxIcon.Information);
					return;
				}
			}

			frmSelectProgramOption frmPickProgramOption = new frmSelectProgramOption(_objCharacter);
			frmPickProgramOption.ProgramName = objProgram.Name;
			frmPickProgramOption.ProgramCategory = objProgram.Category;
			frmPickProgramOption.ProgramTags = objProgram.Tags;
			frmPickProgramOption.ShowDialog(this);

			if (frmPickProgramOption.DialogResult == DialogResult.Cancel)
				return;

			XmlDocument objXmlDocument = XmlManager.Instance.Load("programs.xml");

			XmlNode objXmlOption = objXmlDocument.SelectSingleNode("/chummer/options/option[name = \"" + frmPickProgramOption.SelectedOption + "\"]");

			TreeNode objNode = new TreeNode();
			TechProgramOption objOption = new TechProgramOption(_objCharacter);
			objOption.Create(objXmlOption, _objCharacter, objNode);
			objNode.ContextMenuStrip = cmsComplexFormPlugin;
			if (objOption.InternalId == Guid.Empty.ToString())
				return;

			objProgram.Options.Add(objOption);

			treComplexForms.SelectedNode.Nodes.Add(objNode);
			treComplexForms.SelectedNode.Expand();

			UpdateCharacterInfo();
		}

		private void tsGearAddNexus_Click(object sender, EventArgs e)
		{
			treGear.SelectedNode = treGear.Nodes[0];

			frmSelectNexus frmPickNexus = new frmSelectNexus(_objCharacter);
			frmPickNexus.ShowDialog(this);

			if (frmPickNexus.DialogResult == DialogResult.Cancel)
				return;

			Gear objGear = new Gear(_objCharacter);
			objGear = frmPickNexus.SelectedNexus;

			TreeNode nodNexus = new TreeNode();
			nodNexus.Text = objGear.Name;
			nodNexus.Tag = objGear.InternalId;
			nodNexus.ContextMenuStrip = cmsGear;

			foreach (Gear objChild in objGear.Children)
			{
				TreeNode nodModule = new TreeNode();
				nodModule.Text = objChild.Name;
				nodModule.Tag = objChild.InternalId;
				nodModule.ContextMenuStrip = cmsGear;
				nodNexus.Nodes.Add(nodModule);
				nodNexus.Expand();
			}

			treGear.Nodes[0].Nodes.Add(nodNexus);
			treGear.Nodes[0].Expand();

			_objCharacter.Gear.Add(objGear);

			UpdateCharacterInfo();
		}

		private void tsGearButtonAddAccessory_Click(object sender, EventArgs e)
		{
			tsGearAddAsPlugin_Click(sender, e);
		}

		private void tsVehicleAddNexus_Click(object sender, EventArgs e)
		{
			// Make sure a parent items is selected, then open the Select Gear window.
			try
			{
				if (treVehicles.SelectedNode.Level == 0)
				{
					MessageBox.Show(LanguageManager.Instance.GetString("Message_SelectGearVehicle"), LanguageManager.Instance.GetString("MessageTitle_SelectGearVehicle"), MessageBoxButtons.OK, MessageBoxIcon.Information);
					return;
				}
			}
			catch
			{
				MessageBox.Show(LanguageManager.Instance.GetString("Message_SelectGearVehicle"), LanguageManager.Instance.GetString("MessageTitle_SelectGearVehicle"), MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			if (treVehicles.SelectedNode.Level > 1)
				treVehicles.SelectedNode = treVehicles.SelectedNode.Parent;

			// Attempt to locate the selected Vehicle.
			Vehicle objSelectedVehicle = _objFunctions.FindVehicle(treVehicles.SelectedNode.Tag.ToString(), _objCharacter.Vehicles);

			frmSelectNexus frmPickNexus = new frmSelectNexus(_objCharacter);
			frmPickNexus.ShowDialog(this);

			if (frmPickNexus.DialogResult == DialogResult.Cancel)
				return;

			Gear objGear = new Gear(_objCharacter);
			objGear = frmPickNexus.SelectedNexus;

			TreeNode nodNexus = new TreeNode();
			nodNexus.Text = objGear.Name;
			nodNexus.Tag = objGear.InternalId;
			nodNexus.ContextMenuStrip = cmsVehicleGear;

			foreach (Gear objChild in objGear.Children)
			{
				TreeNode nodModule = new TreeNode();
				nodModule.Text = objChild.Name;
				nodModule.Tag = objChild.InternalId;
				nodModule.ContextMenuStrip = cmsVehicleGear;
				nodNexus.Nodes.Add(nodModule);
				nodNexus.Expand();
			}

			treVehicles.SelectedNode.Nodes.Add(nodNexus);
			treVehicles.SelectedNode.Expand();

			objSelectedVehicle.Gear.Add(objGear);

			UpdateCharacterInfo();
			RefreshSelectedVehicle();
		}

		private void tsAddArmorGear_Click(object sender, EventArgs e)
		{
			// Make sure a parent items is selected, then open the Select Gear window.
			try
			{
				if (treArmor.SelectedNode.Level != 1)
				{
					MessageBox.Show(LanguageManager.Instance.GetString("Message_SelectArmor"), LanguageManager.Instance.GetString("MessageTitle_SelectArmor"), MessageBoxButtons.OK, MessageBoxIcon.Information);
					return;
				}
			}
			catch
			{
				MessageBox.Show(LanguageManager.Instance.GetString("Message_SelectArmor"), LanguageManager.Instance.GetString("MessageTitle_SelectArmor"), MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			// Select the root Gear node then open the Select Gear window.
			bool blnAddAgain = PickArmorGear(true);
			if (blnAddAgain)
				tsAddArmorGear_Click(sender, e);
		}

		private void tsArmorGearAddAsPlugin_Click(object sender, EventArgs e)
		{
			// Make sure a parent items is selected, then open the Select Gear window.
			try
			{
				if (treArmor.SelectedNode.Level == 0)
				{
					MessageBox.Show(LanguageManager.Instance.GetString("Message_SelectArmor"), LanguageManager.Instance.GetString("MessageTitle_SelectArmor"), MessageBoxButtons.OK, MessageBoxIcon.Information);
					return;
				}
			}
			catch
			{
				MessageBox.Show(LanguageManager.Instance.GetString("Message_SelectArmor"), LanguageManager.Instance.GetString("MessageTitle_SelectArmor"), MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			// Make sure the selected item is another piece of Gear.
			bool blnFound = false;
			Armor objFoundArmor = new Armor(_objCharacter);
			Gear objGear = _objFunctions.FindArmorGear(treArmor.SelectedNode.Tag.ToString(), _objCharacter.Armor, out objFoundArmor);
			if (objGear != null)
				blnFound = true;

			if (!blnFound)
			{
				MessageBox.Show(LanguageManager.Instance.GetString("Message_SelectArmor"), LanguageManager.Instance.GetString("MessageTitle_SelectArmor"), MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			bool blnAddAgain = PickArmorGear();
			if (blnAddAgain)
				tsArmorGearAddAsPlugin_Click(sender, e);
		}

		private void tsArmorNotes_Click(object sender, EventArgs e)
		{
			try
			{
				bool blnFound = false;
				Armor objArmor = _objFunctions.FindArmor(treArmor.SelectedNode.Tag.ToString(), _objCharacter.Armor);
				if (objArmor != null)
					blnFound = true;

				if (blnFound)
				{
					frmNotes frmItemNotes = new frmNotes();
					frmItemNotes.Notes = objArmor.Notes;
					string strOldValue = objArmor.Notes;
					frmItemNotes.ShowDialog(this);

					if (frmItemNotes.DialogResult == DialogResult.OK)
					{
						objArmor.Notes = frmItemNotes.Notes;
						if (objArmor.Notes != strOldValue)
						{
							_blnIsDirty = true;
							UpdateWindowTitle();
						}
					}

					if (objArmor.Notes != string.Empty)
						treArmor.SelectedNode.ForeColor = Color.SaddleBrown;
					else
						treArmor.SelectedNode.ForeColor = SystemColors.WindowText;
					treArmor.SelectedNode.ToolTipText = objArmor.Notes;
				}
			}
			catch
			{
			}
		}

		private void tsArmorModNotes_Click(object sender, EventArgs e)
		{
			try
			{
				bool blnFound = false;
				ArmorMod objArmorMod = _objFunctions.FindArmorMod(treArmor.SelectedNode.Tag.ToString(), _objCharacter.Armor);
				if (objArmorMod != null)
					blnFound = true;

				if (blnFound)
				{
					frmNotes frmItemNotes = new frmNotes();
					frmItemNotes.Notes = objArmorMod.Notes;
					string strOldValue = objArmorMod.Notes;
					frmItemNotes.ShowDialog(this);

					if (frmItemNotes.DialogResult == DialogResult.OK)
					{
						objArmorMod.Notes = frmItemNotes.Notes;
						if (objArmorMod.Notes != strOldValue)
						{
							_blnIsDirty = true;
							UpdateWindowTitle();
						}
					}

					if (objArmorMod.Notes != string.Empty)
						treArmor.SelectedNode.ForeColor = Color.SaddleBrown;
					else
						treArmor.SelectedNode.ForeColor = SystemColors.WindowText;
					treArmor.SelectedNode.ToolTipText = objArmorMod.Notes;
				}
			}
			catch
			{
			}
		}

		private void tsArmorGearNotes_Click(object sender, EventArgs e)
		{
			try
			{
				bool blnFound = false;
				Armor objFoundArmor = new Armor(_objCharacter);
				Gear objArmorGear = _objFunctions.FindArmorGear(treArmor.SelectedNode.Tag.ToString(), _objCharacter.Armor, out objFoundArmor);
				if (objArmorGear != null)
					blnFound = true;

				if (blnFound)
				{
					frmNotes frmItemNotes = new frmNotes();
					frmItemNotes.Notes = objArmorGear.Notes;
					string strOldValue = objArmorGear.Notes;
					frmItemNotes.ShowDialog(this);

					if (frmItemNotes.DialogResult == DialogResult.OK)
					{
						objArmorGear.Notes = frmItemNotes.Notes;
						if (objArmorGear.Notes != strOldValue)
						{
							_blnIsDirty = true;
							UpdateWindowTitle();
						}
					}

					if (objArmorGear.Notes != string.Empty)
						treArmor.SelectedNode.ForeColor = Color.SaddleBrown;
					else
						treArmor.SelectedNode.ForeColor = SystemColors.WindowText;
					treArmor.SelectedNode.ToolTipText = objArmorGear.Notes;
				}
			}
			catch
			{
			}
		}

		private void tsWeaponNotes_Click(object sender, EventArgs e)
		{
			try
			{
				bool blnFound = false;
				Weapon objWeapon = _objFunctions.FindWeapon(treWeapons.SelectedNode.Tag.ToString(), _objCharacter.Weapons);
				if (objWeapon != null)
					blnFound = true;

				if (blnFound)
				{
					frmNotes frmItemNotes = new frmNotes();
					frmItemNotes.Notes = objWeapon.Notes;
					string strOldValue = objWeapon.Notes;
					frmItemNotes.ShowDialog(this);

					if (frmItemNotes.DialogResult == DialogResult.OK)
					{
						objWeapon.Notes = frmItemNotes.Notes;
						if (objWeapon.Notes != strOldValue)
						{
							_blnIsDirty = true;
							UpdateWindowTitle();
						}
					}

					if (objWeapon.Notes != string.Empty)
						treWeapons.SelectedNode.ForeColor = Color.SaddleBrown;
					else
					{
						if (objWeapon.Category.StartsWith("Cyberware") || objWeapon.Category == "Gear")
							treWeapons.SelectedNode.ForeColor = SystemColors.GrayText;
						else
							treWeapons.SelectedNode.ForeColor = SystemColors.WindowText;
					}
					treWeapons.SelectedNode.ToolTipText = objWeapon.Notes;
				}
			}
			catch
			{
			}
		}

		private void tsWeaponModNotes_Click(object sender, EventArgs e)
		{
			WeaponMod objMod = _objFunctions.FindWeaponMod(treWeapons.SelectedNode.Tag.ToString(), _objCharacter.Weapons);

			frmNotes frmItemNotes = new frmNotes();
			frmItemNotes.Notes = objMod.Notes;
			string strOldValue = objMod.Notes;
			frmItemNotes.ShowDialog(this);

			if (frmItemNotes.DialogResult == DialogResult.OK)
			{
				objMod.Notes = frmItemNotes.Notes;
				if (objMod.Notes != strOldValue)
				{
					_blnIsDirty = true;
					UpdateWindowTitle();
				}
			}

			if (objMod.Notes != string.Empty)
				treWeapons.SelectedNode.ForeColor = Color.SaddleBrown;
			else
				treWeapons.SelectedNode.ForeColor = SystemColors.WindowText;
			treWeapons.SelectedNode.ToolTipText = objMod.Notes;
		}

		private void tsWeaponAccessoryNotes_Click(object sender, EventArgs e)
		{
			WeaponAccessory objAccessory = _objFunctions.FindWeaponAccessory(treWeapons.SelectedNode.Tag.ToString(), _objCharacter.Weapons);

			frmNotes frmItemNotes = new frmNotes();
			frmItemNotes.Notes = objAccessory.Notes;
			string strOldValue = objAccessory.Notes;
			frmItemNotes.ShowDialog(this);

			if (frmItemNotes.DialogResult == DialogResult.OK)
			{
				objAccessory.Notes = frmItemNotes.Notes;
				if (objAccessory.Notes != strOldValue)
				{
					_blnIsDirty = true;
					UpdateWindowTitle();
				}
			}

			if (objAccessory.Notes != string.Empty)
				treWeapons.SelectedNode.ForeColor = Color.SaddleBrown;
			else
				treWeapons.SelectedNode.ForeColor = SystemColors.WindowText;
			treWeapons.SelectedNode.ToolTipText = objAccessory.Notes;
		}

		private void tsCyberwareNotes_Click(object sender, EventArgs e)
		{
			try
			{
				bool blnFound = false;
				Cyberware objCyberware = _objFunctions.FindCyberware(treCyberware.SelectedNode.Tag.ToString(), _objCharacter.Cyberware);
				if (objCyberware != null)
					blnFound = true;

				if (blnFound)
				{
					frmNotes frmItemNotes = new frmNotes();
					frmItemNotes.Notes = objCyberware.Notes;
					string strOldValue = objCyberware.Notes;
					frmItemNotes.ShowDialog(this);

					if (frmItemNotes.DialogResult == DialogResult.OK)
					{
						objCyberware.Notes = frmItemNotes.Notes;
						if (objCyberware.Notes != strOldValue)
						{
							_blnIsDirty = true;
							UpdateWindowTitle();
						}
					}

					if (objCyberware.Notes != string.Empty)
						treCyberware.SelectedNode.ForeColor = Color.SaddleBrown;
					else
					{
						if (objCyberware.Capacity == "[*]")
							treCyberware.SelectedNode.ForeColor = SystemColors.GrayText;
						else
							treCyberware.SelectedNode.ForeColor = SystemColors.WindowText;
					}
					treCyberware.SelectedNode.ToolTipText = objCyberware.Notes;
				}
			}
			catch
			{
			}
		}

		private void tsQualityNotes_Click(object sender, EventArgs e)
		{
			try
			{
				bool blnFound = false;
				Quality objQuality = _objFunctions.FindQuality(treQualities.SelectedNode.Tag.ToString(), _objCharacter.Qualities);
				if (objQuality != null)
					blnFound = true;

				if (blnFound)
				{
					frmNotes frmItemNotes = new frmNotes();
					frmItemNotes.Notes = objQuality.Notes;
					string strOldValue = objQuality.Notes;
					frmItemNotes.ShowDialog(this);

					if (frmItemNotes.DialogResult == DialogResult.OK)
					{
						objQuality.Notes = frmItemNotes.Notes;
						if (objQuality.Notes != strOldValue)
						{
							_blnIsDirty = true;
							UpdateWindowTitle();
						}
					}

					if (objQuality.Notes != string.Empty)
						treQualities.SelectedNode.ForeColor = Color.SaddleBrown;
					else
					{
						if (objQuality.OriginSource == QualitySource.Metatype || objQuality.OriginSource == QualitySource.MetatypeRemovable)
							treQualities.SelectedNode.ForeColor = SystemColors.GrayText;
						else
							treQualities.SelectedNode.ForeColor = SystemColors.WindowText;
					}
					treQualities.SelectedNode.ToolTipText = objQuality.Notes;
				}
			}
			catch
			{
			}
		}

		private void tsMartialArtsNotes_Click(object sender, EventArgs e)
		{
			try
			{
				bool blnFound = false;
				MartialArt objMartialArt = _objFunctions.FindMartialArt(treMartialArts.SelectedNode.Tag.ToString(), _objCharacter.MartialArts);
				if (objMartialArt != null)
					blnFound = true;

				if (blnFound)
				{
					frmNotes frmItemNotes = new frmNotes();
					frmItemNotes.Notes = objMartialArt.Notes;
					string strOldValue = objMartialArt.Notes;
					frmItemNotes.ShowDialog(this);

					if (frmItemNotes.DialogResult == DialogResult.OK)
					{
						objMartialArt.Notes = frmItemNotes.Notes;
						if (objMartialArt.Notes != strOldValue)
						{
							_blnIsDirty = true;
							UpdateWindowTitle();
						}
					}

					if (objMartialArt.Notes != string.Empty)
						treMartialArts.SelectedNode.ForeColor = Color.SaddleBrown;
					else
						treMartialArts.SelectedNode.ForeColor = SystemColors.WindowText;
					treMartialArts.SelectedNode.ToolTipText = objMartialArt.Notes;
				}
			}
			catch
			{
			}
		}

		private void tsMartialArtManeuverNotes_Click(object sender, EventArgs e)
		{
			try
			{
				bool blnFound = false;
				MartialArtManeuver objMartialArtManeuver = _objFunctions.FindMartialArtManeuver(treMartialArts.SelectedNode.Tag.ToString(), _objCharacter.MartialArtManeuvers);
				if (objMartialArtManeuver != null)
					blnFound = true;

				if (blnFound)
				{
					frmNotes frmItemNotes = new frmNotes();
					frmItemNotes.Notes = objMartialArtManeuver.Notes;
					string strOldValue = objMartialArtManeuver.Notes;
					frmItemNotes.ShowDialog(this);

					if (frmItemNotes.DialogResult == DialogResult.OK)
					{
						objMartialArtManeuver.Notes = frmItemNotes.Notes;
						if (objMartialArtManeuver.Notes != strOldValue)
						{
							_blnIsDirty = true;
							UpdateWindowTitle();
						}
					}

					if (objMartialArtManeuver.Notes != string.Empty)
						treMartialArts.SelectedNode.ForeColor = Color.SaddleBrown;
					else
						treMartialArts.SelectedNode.ForeColor = SystemColors.WindowText;
					treMartialArts.SelectedNode.ToolTipText = objMartialArtManeuver.Notes;
				}
			}
			catch
			{
			}
		}

		private void tsSpellNotes_Click(object sender, EventArgs e)
		{
			try
			{
				bool blnFound = false;
				Spell objSpell = _objFunctions.FindSpell(treSpells.SelectedNode.Tag.ToString(), _objCharacter.Spells);
				if (objSpell != null)
					blnFound = true;

				if (blnFound)
				{
					frmNotes frmItemNotes = new frmNotes();
					frmItemNotes.Notes = objSpell.Notes;
					string strOldValue = objSpell.Notes;
					frmItemNotes.ShowDialog(this);

					if (frmItemNotes.DialogResult == DialogResult.OK)
					{
						objSpell.Notes = frmItemNotes.Notes;
						if (objSpell.Notes != strOldValue)
						{
							_blnIsDirty = true;
							UpdateWindowTitle();
						}
					}

					if (objSpell.Notes != string.Empty)
						treSpells.SelectedNode.ForeColor = Color.SaddleBrown;
					else
						treSpells.SelectedNode.ForeColor = SystemColors.WindowText;
					treSpells.SelectedNode.ToolTipText = objSpell.Notes;
				}
			}
			catch
			{
			}
		}

		private void tsComplexFormNotes_Click(object sender, EventArgs e)
		{
			try
			{
				bool blnFound = false;
				TechProgram objComplexForm = _objFunctions.FindTechProgram(treComplexForms.SelectedNode.Tag.ToString(), _objCharacter.TechPrograms);
				if (objComplexForm != null)
					blnFound = true;

				if (blnFound)
				{
					frmNotes frmItemNotes = new frmNotes();
					frmItemNotes.Notes = objComplexForm.Notes;
					string strOldValue = objComplexForm.Notes;
					frmItemNotes.ShowDialog(this);

					if (frmItemNotes.DialogResult == DialogResult.OK)
					{
						objComplexForm.Notes = frmItemNotes.Notes;
						if (objComplexForm.Notes != strOldValue)
						{
							_blnIsDirty = true;
							UpdateWindowTitle();
						}
					}

					if (objComplexForm.Notes != string.Empty)
						treComplexForms.SelectedNode.ForeColor = Color.SaddleBrown;
					else
						treComplexForms.SelectedNode.ForeColor = SystemColors.WindowText;
					treComplexForms.SelectedNode.ToolTipText = objComplexForm.Notes;
				}
			}
			catch
			{
			}
		}

		private void tsCritterPowersNotes_Click(object sender, EventArgs e)
		{
			try
			{
				bool blnFound = false;
				CritterPower objCritterPower = _objFunctions.FindCritterPower(treCritterPowers.SelectedNode.Tag.ToString(), _objCharacter.CritterPowers);
				if (objCritterPower != null)
					blnFound = true;

				if (blnFound)
				{
					frmNotes frmItemNotes = new frmNotes();
					frmItemNotes.Notes = objCritterPower.Notes;
					string strOldValue = objCritterPower.Notes;
					frmItemNotes.ShowDialog(this);

					if (frmItemNotes.DialogResult == DialogResult.OK)
					{
						objCritterPower.Notes = frmItemNotes.Notes;
						if (objCritterPower.Notes != strOldValue)
						{
							_blnIsDirty = true;
							UpdateWindowTitle();
						}
					}

					if (objCritterPower.Notes != string.Empty)
						treCritterPowers.SelectedNode.ForeColor = Color.SaddleBrown;
					else
						treCritterPowers.SelectedNode.ForeColor = SystemColors.WindowText;
					treCritterPowers.SelectedNode.ToolTipText = objCritterPower.Notes;
				}
			}
			catch
			{
			}
		}

		private void tsMetamagicNotes_Click(object sender, EventArgs e)
		{
			try
			{
				bool blnFound = false;
				Metamagic objMetamagic = _objFunctions.FindMetamagic(treMetamagic.SelectedNode.Tag.ToString(), _objCharacter.Metamagics);
				if (objMetamagic != null)
					blnFound = true;

				if (blnFound)
				{
					frmNotes frmItemNotes = new frmNotes();
					frmItemNotes.Notes = objMetamagic.Notes;
					string strOldValue = objMetamagic.Notes;
					frmItemNotes.ShowDialog(this);

					if (frmItemNotes.DialogResult == DialogResult.OK)
					{
						objMetamagic.Notes = frmItemNotes.Notes;
						if (objMetamagic.Notes != strOldValue)
						{
							_blnIsDirty = true;
							UpdateWindowTitle();
						}
					}

					if (objMetamagic.Notes != string.Empty)
						treMetamagic.SelectedNode.ForeColor = Color.SaddleBrown;
					else
						treMetamagic.SelectedNode.ForeColor = SystemColors.WindowText;
					treMetamagic.SelectedNode.ToolTipText = objMetamagic.Notes;
				}
			}
			catch
			{
			}
		}

		private void tsGearNotes_Click(object sender, EventArgs e)
		{
			try
			{
				bool blnFound = false;
				Gear objGear = _objFunctions.FindGear(treGear.SelectedNode.Tag.ToString(), _objCharacter.Gear);
				if (objGear != null)
					blnFound = true;

				if (blnFound)
				{
					frmNotes frmItemNotes = new frmNotes();
					frmItemNotes.Notes = objGear.Notes;
					string strOldValue = objGear.Notes;
					frmItemNotes.ShowDialog(this);

					if (frmItemNotes.DialogResult == DialogResult.OK)
					{
						objGear.Notes = frmItemNotes.Notes;
						if (objGear.Notes != strOldValue)
						{
							_blnIsDirty = true;
							UpdateWindowTitle();
						}
					}

					if (objGear.Notes != string.Empty)
						treGear.SelectedNode.ForeColor = Color.SaddleBrown;
					else
						treGear.SelectedNode.ForeColor = SystemColors.WindowText;
					treGear.SelectedNode.ToolTipText = objGear.Notes;
				}
			}
			catch
			{
			}
		}

		private void tsGearPluginNotes_Click(object sender, EventArgs e)
		{
			try
			{
				bool blnFound = false;
				Gear objGear = _objFunctions.FindGear(treGear.SelectedNode.Tag.ToString(), _objCharacter.Gear);
				if (objGear != null)
					blnFound = true;

				if (blnFound)
				{
					frmNotes frmItemNotes = new frmNotes();
					frmItemNotes.Notes = objGear.Notes;
					string strOldValue = objGear.Notes;
					frmItemNotes.ShowDialog(this);

					if (frmItemNotes.DialogResult == DialogResult.OK)
					{
						objGear.Notes = frmItemNotes.Notes;
						if (objGear.Notes != strOldValue)
						{
							_blnIsDirty = true;
							UpdateWindowTitle();
						}
					}

					if (objGear.Notes != string.Empty)
						treGear.SelectedNode.ForeColor = Color.SaddleBrown;
					else
						treGear.SelectedNode.ForeColor = SystemColors.WindowText;
					treGear.SelectedNode.ToolTipText = objGear.Notes;
				}
			}
			catch
			{
			}
		}

		private void tsVehicleNotes_Click(object sender, EventArgs e)
		{
			Vehicle objVehicle = new Vehicle(_objCharacter);
			VehicleMod objMod = new VehicleMod(_objCharacter);
			bool blnFoundVehicle = false;
			bool blnFoundMod = false;
			try
			{
				foreach (Vehicle objCharacterVehicle in _objCharacter.Vehicles)
				{
					if (objCharacterVehicle.InternalId == treVehicles.SelectedNode.Tag.ToString())
					{
						objVehicle = objCharacterVehicle;
						blnFoundVehicle = true;
						break;
					}
					foreach (VehicleMod objVehicleMod in objCharacterVehicle.Mods)
					{
						if (objVehicleMod.InternalId == treVehicles.SelectedNode.Tag.ToString())
						{
							objMod = objVehicleMod;
							blnFoundMod = true;
							break;
						}
					}
				}

				if (blnFoundVehicle)
				{
					frmNotes frmItemNotes = new frmNotes();
					frmItemNotes.Notes = objVehicle.Notes;
					string strOldValue = objVehicle.Notes;
					frmItemNotes.ShowDialog(this);

					if (frmItemNotes.DialogResult == DialogResult.OK)
					{
						objVehicle.Notes = frmItemNotes.Notes;
						if (objVehicle.Notes != strOldValue)
						{
							_blnIsDirty = true;
							UpdateWindowTitle();
						}
					}

					if (objVehicle.Notes != string.Empty)
						treVehicles.SelectedNode.ForeColor = Color.SaddleBrown;
					else
						treVehicles.SelectedNode.ForeColor = SystemColors.WindowText;
					treVehicles.SelectedNode.ToolTipText = objVehicle.Notes;
				}
				if (blnFoundMod)
				{
					frmNotes frmItemNotes = new frmNotes();
					frmItemNotes.Notes = objMod.Notes;
					string strOldValue = objMod.Notes;
					frmItemNotes.ShowDialog(this);

					if (frmItemNotes.DialogResult == DialogResult.OK)
					{
						objMod.Notes = frmItemNotes.Notes;
						if (objMod.Notes != strOldValue)
						{
							_blnIsDirty = true;
							UpdateWindowTitle();
						}
					}

					if (objMod.Notes != string.Empty)
						treVehicles.SelectedNode.ForeColor = Color.SaddleBrown;
					else
					{
						if (objMod.IncludedInVehicle)
							treVehicles.SelectedNode.ForeColor = SystemColors.GrayText;
						else
							treVehicles.SelectedNode.ForeColor = SystemColors.WindowText;
					}
					treVehicles.SelectedNode.ToolTipText = objMod.Notes;
				}
			}
			catch
			{
			}
		}

		private void tsLifestyleNotes_Click(object sender, EventArgs e)
		{
			try
			{
				bool blnFound = false;
				Lifestyle objLifestyle = _objFunctions.FindLifestyle(treLifestyles.SelectedNode.Tag.ToString(), _objCharacter.Lifestyles);
				if (objLifestyle != null)
					blnFound = true;

				if (blnFound)
				{
					frmNotes frmItemNotes = new frmNotes();
					frmItemNotes.Notes = objLifestyle.Notes;
					string strOldValue = objLifestyle.Notes;
					frmItemNotes.ShowDialog(this);

					if (frmItemNotes.DialogResult == DialogResult.OK)
					{
						objLifestyle.Notes = frmItemNotes.Notes;
						if (objLifestyle.Notes != strOldValue)
						{
							_blnIsDirty = true;
							UpdateWindowTitle();
						}
					}

					if (objLifestyle.Notes != string.Empty)
						treLifestyles.SelectedNode.ForeColor = Color.SaddleBrown;
					else
						treLifestyles.SelectedNode.ForeColor = SystemColors.WindowText;
					treLifestyles.SelectedNode.ToolTipText = objLifestyle.Notes;
				}
			}
			catch
			{
			}
		}

		private void tsVehicleWeaponNotes_Click(object sender, EventArgs e)
		{
			bool blnFound = false;
			Vehicle objFoundVehicle = new Vehicle(_objCharacter);
			Weapon objWeapon = _objFunctions.FindVehicleWeapon(treVehicles.SelectedNode.Tag.ToString(), _objCharacter.Vehicles, out objFoundVehicle);
			if (objWeapon != null)
				blnFound = true;

			if (blnFound)
			{
				frmNotes frmItemNotes = new frmNotes();
				frmItemNotes.Notes = objWeapon.Notes;
				string strOldValue = objWeapon.Notes;
				frmItemNotes.ShowDialog(this);

				if (frmItemNotes.DialogResult == DialogResult.OK)
				{
					objWeapon.Notes = frmItemNotes.Notes;
					if (objWeapon.Notes != strOldValue)
					{
						_blnIsDirty = true;
						UpdateWindowTitle();
					}
				}

				if (objWeapon.Notes != string.Empty)
					treVehicles.SelectedNode.ForeColor = Color.SaddleBrown;
				else
				{
					if (objWeapon.Category.StartsWith("Cyberware") || objWeapon.Category == "Gear")
						treVehicles.SelectedNode.ForeColor = SystemColors.GrayText;
					else
						treVehicles.SelectedNode.ForeColor = SystemColors.WindowText;
				}
				treVehicles.SelectedNode.ToolTipText = objWeapon.Notes;
			}
		}

		private void tsComplexFormPluginNotes_Click(object sender, EventArgs e)
		{
			bool blnFound = false;
			TechProgram objFoundProgram = new TechProgram(_objCharacter);
			TechProgramOption objOption = _objFunctions.FindTechProgramOption(treComplexForms.SelectedNode.Tag.ToString(), _objCharacter.TechPrograms, out objFoundProgram);
			if (objOption != null)
				blnFound = true;

			if (blnFound)
			{
				frmNotes frmItemNotes = new frmNotes();
				frmItemNotes.Notes = objOption.Notes;
				string strOldValue = objOption.Notes;
				frmItemNotes.ShowDialog(this);

				if (frmItemNotes.DialogResult == DialogResult.OK)
				{
					objOption.Notes = frmItemNotes.Notes;
					if (objOption.Notes != strOldValue)
					{
						_blnIsDirty = true;
						UpdateWindowTitle();
					}
				}

				if (objOption.Notes != string.Empty)
					treComplexForms.SelectedNode.ForeColor = Color.SaddleBrown;
				else
					treComplexForms.SelectedNode.ForeColor = SystemColors.WindowText;
				treComplexForms.SelectedNode.ToolTipText = objOption.Notes;
			}
		}

		private void tsVehicleName_Click(object sender, EventArgs e)
		{
			// Make sure a parent item is selected.
			try
			{
				if (treVehicles.SelectedNode.Level == 0)
				{
					MessageBox.Show(LanguageManager.Instance.GetString("Message_SelectVehicleName"), LanguageManager.Instance.GetString("MessageTitle_SelectVehicle"), MessageBoxButtons.OK, MessageBoxIcon.Information);
					return;
				}
			}
			catch
			{
				MessageBox.Show(LanguageManager.Instance.GetString("Message_SelectVehicleName"), LanguageManager.Instance.GetString("MessageTitle_SelectVehicle"), MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			while (treVehicles.SelectedNode.Level > 1)
			{
				treVehicles.SelectedNode = treVehicles.SelectedNode.Parent;
			}

			// Get the information for the currently selected Vehicle.
			Vehicle objVehicle = _objFunctions.FindVehicle(treVehicles.SelectedNode.Tag.ToString(), _objCharacter.Vehicles);

			frmSelectText frmPickText = new frmSelectText();
			frmPickText.Description = LanguageManager.Instance.GetString("String_VehicleName");
			frmPickText.ShowDialog(this);

			if (frmPickText.DialogResult == DialogResult.Cancel)
				return;

			objVehicle.VehicleName = frmPickText.SelectedValue;
			treVehicles.SelectedNode.Text = objVehicle.DisplayName;
		}

		private void tsVehicleAddCyberware_Click(object sender, EventArgs e)
		{
			Vehicle objVehicle = new Vehicle(_objCharacter);
			VehicleMod objMod = _objFunctions.FindVehicleMod(treVehicles.SelectedNode.Tag.ToString(), _objCharacter.Vehicles, out objVehicle);

			if (objMod == null)
			{
				MessageBox.Show(LanguageManager.Instance.GetString("Message_VehicleCyberwarePlugin"), LanguageManager.Instance.GetString("MessageTitle_NoCyberware"), MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			if (!objMod.AllowCyberware)
			{
				MessageBox.Show(LanguageManager.Instance.GetString("Message_VehicleCyberwarePlugin"), LanguageManager.Instance.GetString("MessageTitle_NoCyberware"), MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			frmSelectCyberware frmPickCyberware = new frmSelectCyberware(_objCharacter);
			frmPickCyberware.SetGrade = "Standard";
			frmPickCyberware.LockGrade();
			frmPickCyberware.ShowOnlySubsystems = true;
			frmPickCyberware.Subsystems = objMod.Subsystems;
			frmPickCyberware.AllowModularPlugins = objMod.AllowModularPlugins;
			frmPickCyberware.ShowDialog(this);

			if (frmPickCyberware.DialogResult == DialogResult.Cancel)
				return;

			// Open the Cyberware XML file and locate the selected piece.
			XmlDocument objXmlDocument = XmlManager.Instance.Load("cyberware.xml");

			XmlNode objXmlCyberware = objXmlDocument.SelectSingleNode("/chummer/cyberwares/cyberware[name = \"" + frmPickCyberware.SelectedCyberware + "\"]");

			// Create the Cyberware object.
			Cyberware objCyberware = new Cyberware(_objCharacter);
			List<Weapon> objWeapons = new List<Weapon>();
			TreeNode objNode = new TreeNode();
			List<TreeNode> objWeaponNodes = new List<TreeNode>();
			objCyberware.Create(objXmlCyberware, _objCharacter, frmPickCyberware.SelectedGrade, Improvement.ImprovementSource.Cyberware, frmPickCyberware.SelectedRating, objNode, objWeapons, objWeaponNodes, false);
			if (objCyberware.InternalId == Guid.Empty.ToString())
				return;

			if (frmPickCyberware.FreeCost)
				objCyberware.Cost = "0";

			treVehicles.SelectedNode.Nodes.Add(objNode);
			treVehicles.SelectedNode.Expand();
			objMod.Cyberware.Add(objCyberware);

			foreach (Weapon objWeapon in objWeapons)
			{
				objWeapon.VehicleMounted = true;
				objVehicle.Weapons.Add(objWeapon);
			}

			// Create the Weapon Node if one exists.
			foreach (TreeNode objWeaponNode in objWeaponNodes)
			{
				objWeaponNode.ContextMenuStrip = cmsVehicleWeapon;
				treVehicles.SelectedNode.Parent.Nodes.Add(objWeaponNode);
				treVehicles.SelectedNode.Parent.Expand();
			}

			UpdateCharacterInfo();

			if (frmPickCyberware.AddAgain)
				tsVehicleAddCyberware_Click(sender, e);
		}

		private void tsArmorName_Click(object sender, EventArgs e)
		{
			// Make sure a parent item is selected, then open the Select Accessory window.
			try
			{
				if (treArmor.SelectedNode.Level == 0)
				{
					MessageBox.Show(LanguageManager.Instance.GetString("Message_SelectArmorName"), LanguageManager.Instance.GetString("MessageTitle_SelectArmor"), MessageBoxButtons.OK, MessageBoxIcon.Information);
					return;
				}
			}
			catch
			{
				MessageBox.Show(LanguageManager.Instance.GetString("Message_SelectArmorName"), LanguageManager.Instance.GetString("MessageTitle_SelectArmor"), MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			if (treArmor.SelectedNode.Level > 1)
				treArmor.SelectedNode = treArmor.SelectedNode.Parent;

			// Get the information for the currently selected Armor.
			Armor objArmor = _objFunctions.FindArmor(treArmor.SelectedNode.Tag.ToString(), _objCharacter.Armor);

			frmSelectText frmPickText = new frmSelectText();
			frmPickText.Description = LanguageManager.Instance.GetString("String_ArmorName");
			frmPickText.ShowDialog(this);

			if (frmPickText.DialogResult == DialogResult.Cancel)
				return;

			objArmor.ArmorName = frmPickText.SelectedValue;
			treArmor.SelectedNode.Text = objArmor.DisplayName;
		}

		private void tsEditAdvancedLifestyle_Click(object sender, EventArgs e)
		{
			treLifestyles_DoubleClick(sender, e);
		}

		private void tsAdvancedLifestyleNotes_Click(object sender, EventArgs e)
		{
			tsLifestyleNotes_Click(sender, e);
		}

		private void tsEditLifestyle_Click(object sender, EventArgs e)
		{
			treLifestyles_DoubleClick(sender, e);
		}

		private void tsLifestyleName_Click(object sender, EventArgs e)
		{
			try
			{
				if (treLifestyles.SelectedNode.Level == 0)
				{
					MessageBox.Show(LanguageManager.Instance.GetString("Message_SelectLifestyleName"), LanguageManager.Instance.GetString("MessageTitle_SelectLifestyle"), MessageBoxButtons.OK, MessageBoxIcon.Information);
					return;
				}
			}
			catch
			{
				MessageBox.Show(LanguageManager.Instance.GetString("Message_SelectLifestyleName"), LanguageManager.Instance.GetString("MessageTitle_SelectLifestyle"), MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			// Get the information for the currently selected Lifestyle.
			Lifestyle objLifestyle = new Lifestyle(_objCharacter);
			foreach (Lifestyle objSelectedLifestyle in _objCharacter.Lifestyles)
			{
				if (objSelectedLifestyle.InternalId == treLifestyles.SelectedNode.Tag.ToString())
				{
					objLifestyle = objSelectedLifestyle;
					break;
				}
			}

			frmSelectText frmPickText = new frmSelectText();
			frmPickText.Description = LanguageManager.Instance.GetString("String_LifestyleName");
			frmPickText.ShowDialog(this);

			if (frmPickText.DialogResult == DialogResult.Cancel)
				return;

			objLifestyle.LifestyleName = frmPickText.SelectedValue;
			treLifestyles.SelectedNode.Text = objLifestyle.DisplayName;
		}

		private void tsGearRenameLocation_Click(object sender, EventArgs e)
		{
			string strNewLocation = "";
			frmSelectText frmPickText = new frmSelectText();
			frmPickText.Description = LanguageManager.Instance.GetString("String_AddLocation");
			frmPickText.ShowDialog(this);

			if (frmPickText.DialogResult == DialogResult.Cancel)
				return;

			strNewLocation = frmPickText.SelectedValue;

			int i = -1;
			foreach (string strLocation in _objCharacter.Locations)
			{
				i++;
				if (strLocation == treGear.SelectedNode.Text)
				{
					foreach (Gear objGear in _objCharacter.Gear)
					{
						if (objGear.Location == strLocation)
							objGear.Location = strNewLocation;
					}

					_objCharacter.Locations[i] = strNewLocation;
					treGear.SelectedNode.Text = strNewLocation;
					break;
				}
			}

			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void tsWeaponRenameLocation_Click(object sender, EventArgs e)
		{
			string strNewLocation = "";
			frmSelectText frmPickText = new frmSelectText();
			frmPickText.Description = LanguageManager.Instance.GetString("String_AddLocation");
			frmPickText.ShowDialog(this);

			if (frmPickText.DialogResult == DialogResult.Cancel)
				return;

			strNewLocation = frmPickText.SelectedValue;

			int i = -1;
			foreach (string strLocation in _objCharacter.WeaponLocations)
			{
				i++;
				if (strLocation == treWeapons.SelectedNode.Text)
				{
					foreach (Weapon objWeapon in _objCharacter.Weapons)
					{
						if (objWeapon.Location == strLocation)
							objWeapon.Location = strNewLocation;
					}

					_objCharacter.WeaponLocations[i] = strNewLocation;
					treWeapons.SelectedNode.Text = strNewLocation;
					break;
				}
			}

			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void tsCreateSpell_Click(object sender, EventArgs e)
		{
			// Count the number of Spells the character currently has and make sure they do not try to select more Spells than they are allowed.
			// The maximum number of Spells a character can start with is 2 x (highest of Spellcasting or Ritual Spellcasting Skill).
			int intSpellCount = 0;
			foreach (TreeNode nodCategory in treSpells.Nodes)
			{
				foreach (TreeNode nodSpell in nodCategory.Nodes)
				{
					intSpellCount++;
				}
			}

			// Run through the list of Active Skills and pick out the two applicable ones.
			int intSkillValue = 0;
			foreach (SkillControl objSkillControl in panActiveSkills.Controls)
			{
				if ((objSkillControl.SkillName == "Spellcasting" || objSkillControl.SkillName == "Ritual Spellcasting") && objSkillControl.SkillRating > intSkillValue)
					intSkillValue = objSkillControl.SkillRating;
			}

			if (intSpellCount >= ((2 * intSkillValue) + _objImprovementManager.ValueOf(Improvement.ImprovementType.SpellLimit)) && !_objCharacter.IgnoreRules)
			{
				MessageBox.Show(LanguageManager.Instance.GetString("Message_SpellLimit"), LanguageManager.Instance.GetString("MessageTitle_SpellLimit"), MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			// The character is still allowed to add Spells, so show the Create Spell window.
			frmCreateSpell frmSpell = new frmCreateSpell(_objCharacter);
			frmSpell.ShowDialog(this);

			if (frmSpell.DialogResult == DialogResult.Cancel)
				return;

			Spell objSpell = frmSpell.SelectedSpell;
			TreeNode objNode = new TreeNode();
			objNode.Text = objSpell.DisplayName;
			objNode.Tag = objSpell.InternalId;
			objNode.ContextMenuStrip = cmsSpell;

			_objCharacter.Spells.Add(objSpell);

			switch (objSpell.Category)
			{
				case "Combat":
					treSpells.Nodes[0].Nodes.Add(objNode);
					treSpells.Nodes[0].Expand();
					break;
				case "Detection":
					treSpells.Nodes[1].Nodes.Add(objNode);
					treSpells.Nodes[1].Expand();
					break;
				case "Health":
					treSpells.Nodes[2].Nodes.Add(objNode);
					treSpells.Nodes[2].Expand();
					break;
				case "Illusion":
					treSpells.Nodes[3].Nodes.Add(objNode);
					treSpells.Nodes[3].Expand();
					break;
				case "Manipulation":
					treSpells.Nodes[4].Nodes.Add(objNode);
					treSpells.Nodes[4].Expand();
					break;
				case "Geomancy Ritual":
					treSpells.Nodes[5].Nodes.Add(objNode);
					treSpells.Nodes[5].Expand();
					break;
			}

			treSpells.SelectedNode = objNode;

			_objFunctions.SortTree(treSpells);
			UpdateCharacterInfo();

			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void tsArmorRenameLocation_Click(object sender, EventArgs e)
		{
			string strNewLocation = "";
			frmSelectText frmPickText = new frmSelectText();
			frmPickText.Description = LanguageManager.Instance.GetString("String_AddLocation");
			frmPickText.ShowDialog(this);

			if (frmPickText.DialogResult == DialogResult.Cancel)
				return;

			strNewLocation = frmPickText.SelectedValue;

			int i = -1;
			foreach (string strLocation in _objCharacter.ArmorBundles)
			{
				i++;
				if (strLocation == treArmor.SelectedNode.Text)
				{
					foreach (Armor objArmor in _objCharacter.Armor)
					{
						if (objArmor.Location == strLocation)
							objArmor.Location = strNewLocation;
					}

					_objCharacter.ArmorBundles[i] = strNewLocation;
					treArmor.SelectedNode.Text = strNewLocation;
					break;
				}
			}
		}

		private void tsCyberwareAddGear_Click(object sender, EventArgs e)
		{
			// Make sure a parent items is selected, then open the Select Gear window.
			try
			{
				if (treCyberware.SelectedNode.Level == 0)
				{
					MessageBox.Show(LanguageManager.Instance.GetString("Message_SelectCyberware"), LanguageManager.Instance.GetString("MessageTitle_SelectCyberware"), MessageBoxButtons.OK, MessageBoxIcon.Information);
					return;
				}
			}
			catch
			{
				MessageBox.Show(LanguageManager.Instance.GetString("Message_SelectCyberware"), LanguageManager.Instance.GetString("MessageTitle_SelectCyberware"), MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			Cyberware objCyberware = new Cyberware(_objCharacter);
			foreach (Cyberware objCharacterCyberware in _objCharacter.Cyberware)
			{
				if (objCharacterCyberware.InternalId == treCyberware.SelectedNode.Tag.ToString())
				{
					objCyberware = objCharacterCyberware;
					break;
				}

				foreach (Cyberware objChild in objCharacterCyberware.Children)
				{
					if (objChild.InternalId == treCyberware.SelectedNode.Tag.ToString())
					{
						objCyberware = objChild;
						break;
					}
				}
			}

			// Make sure the Cyberware is allowed to accept Gear.
			if (objCyberware.AllowGear == null)
			{
				MessageBox.Show(LanguageManager.Instance.GetString("Message_CyberwareGear"), LanguageManager.Instance.GetString("MessageTitle_CyberwareGear"), MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			frmSelectGear frmPickGear = new frmSelectGear(_objCharacter, false);
			string strCategories = "";
			foreach (XmlNode objXmlCategory in objCyberware.AllowGear)
				strCategories += objXmlCategory.InnerText + ",";
			frmPickGear.AllowedCategories = strCategories;
			frmPickGear.ShowDialog(this);

			if (frmPickGear.DialogResult == DialogResult.Cancel)
				return;

			TreeNode objNode = new TreeNode();

			// Open the Gear XML file and locate the selected piece.
			XmlDocument objXmlDocument = XmlManager.Instance.Load("gear.xml");
			XmlNode objXmlGear = objXmlDocument.SelectSingleNode("/chummer/gears/gear[name = \"" + frmPickGear.SelectedGear + "\" and category = \"" + frmPickGear.SelectedCategory + "\"]");

			// Create the new piece of Gear.
			List<Weapon> objWeapons = new List<Weapon>();
			List<TreeNode> objWeaponNodes = new List<TreeNode>();
			Gear objNewGear = new Gear(_objCharacter);
			switch (frmPickGear.SelectedCategory)
			{
				case "Commlink":
				case "Commlink Upgrade":
					Commlink objCommlink = new Commlink(_objCharacter);
					objCommlink.Create(objXmlGear, _objCharacter, objNode, frmPickGear.SelectedRating);
					objCommlink.Quantity = frmPickGear.SelectedQty;
					try
					{
						_blnSkipRefresh = true;
						nudGearQty.Increment = objCommlink.CostFor;
						//nudGearQty.Minimum = nudGearQty.Increment;
						_blnSkipRefresh = false;
					}
					catch
					{
					}
					objNode.Text = objCommlink.DisplayName;

					objNewGear = objCommlink;
					break;
				case "Commlink Operating System":
				case "Commlink Operating System Upgrade":
					OperatingSystem objOperatingSystem = new OperatingSystem(_objCharacter);
					objOperatingSystem.Create(objXmlGear, _objCharacter, objNode, frmPickGear.SelectedRating);
					objOperatingSystem.Quantity = frmPickGear.SelectedQty;
					try
					{
						_blnSkipRefresh = true;
						nudGearQty.Increment = objOperatingSystem.CostFor;
						//nudGearQty.Minimum = nudGearQty.Increment;
						_blnSkipRefresh = false;
					}
					catch
					{
					}
					objNode.Text = objOperatingSystem.DisplayName;

					objNewGear = objOperatingSystem;
					break;
				default:
					Gear objGear = new Gear(_objCharacter);
					objGear.Create(objXmlGear, _objCharacter, objNode, frmPickGear.SelectedRating, objWeapons, objWeaponNodes, "", frmPickGear.Hacked, frmPickGear.InherentProgram, true, true, frmPickGear.Aerodynamic);
					objGear.Quantity = frmPickGear.SelectedQty;
					try
					{
						_blnSkipRefresh = true;
						nudGearQty.Increment = objGear.CostFor;
						//nudGearQty.Minimum = nudGearQty.Increment;
						_blnSkipRefresh = false;
					}
					catch
					{
					}
					objNode.Text = objGear.DisplayName;

					objNewGear = objGear;
					break;
			}

			if (objNewGear.InternalId == Guid.Empty.ToString())
				return;

			// Reduce the cost for Do It Yourself components.
			if (frmPickGear.DoItYourself)
				objNewGear.Cost = (Convert.ToDouble(objNewGear.Cost, GlobalOptions.Instance.CultureInfo) * 0.5).ToString();
			// Reduce the cost to 10% for Hacked programs.
			if (frmPickGear.Hacked)
			{
				if (objNewGear.Cost != "")
					objNewGear.Cost = "(" + objNewGear.Cost + ") * 0.1";
				if (objNewGear.Cost3 != "")
					objNewGear.Cost3 = "(" + objNewGear.Cost3 + ") * 0.1";
				if (objNewGear.Cost6 != "")
					objNewGear.Cost6 = "(" + objNewGear.Cost6 + ") * 0.1";
				if (objNewGear.Cost10 != "")
					objNewGear.Cost10 = "(" + objNewGear.Cost10 + ") * 0.1";
				if (objNewGear.Extra == "")
					objNewGear.Extra = LanguageManager.Instance.GetString("Label_SelectGear_Hacked");
				else
					objNewGear.Extra += ", " + LanguageManager.Instance.GetString("Label_SelectGear_Hacked");
			}
			// If the item was marked as free, change its cost.
			if (frmPickGear.FreeCost)
			{
				objNewGear.Cost = "0";
				objNewGear.Cost3 = "0";
				objNewGear.Cost6 = "0";
				objNewGear.Cost10 = "0";
			}

			// Create any Weapons that came with this Gear.
			foreach (Weapon objWeapon in objWeapons)
				_objCharacter.Weapons.Add(objWeapon);

			foreach (TreeNode objWeaponNode in objWeaponNodes)
			{
				objWeaponNode.ContextMenuStrip = cmsWeapon;
				treWeapons.Nodes[0].Nodes.Add(objWeaponNode);
				treWeapons.Nodes[0].Expand();
			}

			objCyberware.Gear.Add(objNewGear);

			objNode.ContextMenuStrip = cmsCyberwareGear;
			treCyberware.SelectedNode.Nodes.Add(objNode);
			treCyberware.SelectedNode.Expand();

			UpdateCharacterInfo();

			if (frmPickGear.AddAgain)
				tsCyberwareAddGear_Click(sender, e);
			
			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void tsCyberwareGearAddAsPlugin_Click(object sender, EventArgs e)
		{
			// Make sure a parent items is selected, then open the Select Gear window.
			try
			{
				if (treCyberware.SelectedNode.Level < 2)
				{
					MessageBox.Show(LanguageManager.Instance.GetString("Message_ModifyVehicleGear"), LanguageManager.Instance.GetString("MessageTitle_SelectGear"), MessageBoxButtons.OK, MessageBoxIcon.Information);
					return;
				}
			}
			catch
			{
				MessageBox.Show(LanguageManager.Instance.GetString("Message_ModifyVehicleGear"), LanguageManager.Instance.GetString("MessageTitle_SelectGear"), MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			if (treCyberware.SelectedNode.Level > 3)
				treCyberware.SelectedNode = treCyberware.SelectedNode.Parent;

			// Locate the Vehicle Sensor Gear.
			bool blnFound = false;
			Cyberware objFoundCyber = new Cyberware(_objCharacter);
			Gear objSensor = _objFunctions.FindCyberwareGear(treCyberware.SelectedNode.Tag.ToString(), _objCharacter.Cyberware, out objFoundCyber);
			if (objSensor != null)
				blnFound = true;

			// Make sure the Gear was found.
			if (!blnFound)
			{
				MessageBox.Show(LanguageManager.Instance.GetString("Message_ModifyVehicleGear"), LanguageManager.Instance.GetString("MessageTitle_SelectGear"), MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			XmlDocument objXmlDocument = XmlManager.Instance.Load("gear.xml");

			XmlNode objXmlGear = objXmlDocument.SelectSingleNode("/chummer/gears/gear[name = \"" + objSensor.Name + "\" and category = \"" + objSensor.Category + "\"]");

			frmSelectGear frmPickGear = new frmSelectGear(_objCharacter);
			//frmPickGear.ShowNegativeCapacityOnly = true;

			if (objXmlGear.InnerXml.Contains("<addoncategory>"))
			{
				string strCategories = "";
				foreach (XmlNode objXmlCategory in objXmlGear.SelectNodes("addoncategory"))
					strCategories += objXmlCategory.InnerText + ",";
				// Remove the trailing comma.
				strCategories = strCategories.Substring(0, strCategories.Length - 1);
				frmPickGear.AddCategory(strCategories);
			}

			if (frmPickGear.AllowedCategories != "")
				frmPickGear.AllowedCategories += objSensor.Category + ",";

			frmPickGear.ShowDialog(this);

			if (frmPickGear.DialogResult == DialogResult.Cancel)
				return;

			// Open the Gear XML file and locate the selected piece.
			objXmlGear = objXmlDocument.SelectSingleNode("/chummer/gears/gear[name = \"" + frmPickGear.SelectedGear + "\" and category = \"" + frmPickGear.SelectedCategory + "\"]");

			// Create the new piece of Gear.
			List<Weapon> objWeapons = new List<Weapon>();
			List<TreeNode> objWeaponNodes = new List<TreeNode>();
			TreeNode objNode = new TreeNode();
			Gear objGear = new Gear(_objCharacter);

			switch (frmPickGear.SelectedCategory)
			{
				case "Commlink":
				case "Commlink Upgrade":
					Commlink objCommlink = new Commlink(_objCharacter);
					objCommlink.Create(objXmlGear, _objCharacter, objNode, frmPickGear.SelectedRating);
					objCommlink.Quantity = frmPickGear.SelectedQty;
					objNode.Text = objCommlink.DisplayName;

					objGear = objCommlink;
					break;
				case "Commlink Operating System":
				case "Commlink Operating System Upgrade":
					OperatingSystem objOperatingSystem = new OperatingSystem(_objCharacter);
					objOperatingSystem.Create(objXmlGear, _objCharacter, objNode, frmPickGear.SelectedRating);
					objOperatingSystem.Quantity = frmPickGear.SelectedQty;
					objNode.Text = objOperatingSystem.DisplayName;

					objGear = objOperatingSystem;
					break;
				default:
					Gear objNewGear = new Gear(_objCharacter);
					objNewGear.Create(objXmlGear, _objCharacter, objNode, frmPickGear.SelectedRating, objWeapons, objWeaponNodes, "", frmPickGear.Hacked, frmPickGear.InherentProgram, true, true, frmPickGear.Aerodynamic);
					objNewGear.Quantity = frmPickGear.SelectedQty;
					objNode.Text = objNewGear.DisplayName;

					objGear = objNewGear;
					break;
			}

			if (objGear.InternalId == Guid.Empty.ToString())
				return;

			// Reduce the cost for Do It Yourself components.
			if (frmPickGear.DoItYourself)
				objGear.Cost = (Convert.ToDouble(objGear.Cost, GlobalOptions.Instance.CultureInfo) * 0.5).ToString();
			// Reduce the cost to 10% for Hacked programs.
			if (frmPickGear.Hacked)
			{
				if (objGear.Cost != "")
					objGear.Cost = "(" + objGear.Cost + ") * 0.1";
				if (objGear.Cost3 != "")
					objGear.Cost3 = "(" + objGear.Cost3 + ") * 0.1";
				if (objGear.Cost6 != "")
					objGear.Cost6 = "(" + objGear.Cost6 + ") * 0.1";
				if (objGear.Cost10 != "")
					objGear.Cost10 = "(" + objGear.Cost10 + ") * 0.1";
				if (objGear.Extra == "")
					objGear.Extra = LanguageManager.Instance.GetString("Label_SelectGear_Hacked");
				else
					objGear.Extra += ", " + LanguageManager.Instance.GetString("Label_SelectGear_Hacked");
			}
			// If the item was marked as free, change its cost.
			if (frmPickGear.FreeCost)
			{
				objGear.Cost = "0";
				objGear.Cost3 = "0";
				objGear.Cost6 = "0";
				objGear.Cost10 = "0";
			}

			objNode.Text = objGear.DisplayName;

			if (treCyberware.SelectedNode.Level < 3)
				objNode.ContextMenuStrip = cmsCyberwareGear;

			treCyberware.SelectedNode.Nodes.Add(objNode);
			treCyberware.SelectedNode.Expand();

			objGear.Parent = objSensor;
			objSensor.Children.Add(objGear);

			if (frmPickGear.AddAgain)
				tsCyberwareGearAddAsPlugin_Click(sender, e);

			UpdateCharacterInfo();
			RefreshSelectedCyberware();
		}

		private void tsCyberwareGearMenuAddAsPlugin_Click(object sender, EventArgs e)
		{
			// Make sure a parent items is selected, then open the Select Gear window.
			try
			{
				if (treCyberware.SelectedNode.Level < 2)
				{
					MessageBox.Show(LanguageManager.Instance.GetString("Message_ModifyVehicleGear"), LanguageManager.Instance.GetString("MessageTitle_SelectGear"), MessageBoxButtons.OK, MessageBoxIcon.Information);
					return;
				}
			}
			catch
			{
				MessageBox.Show(LanguageManager.Instance.GetString("Message_ModifyVehicleGear"), LanguageManager.Instance.GetString("MessageTitle_SelectGear"), MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			if (treCyberware.SelectedNode.Level > 3)
				treCyberware.SelectedNode = treCyberware.SelectedNode.Parent;

			// Locate the Vehicle Sensor Gear.
			bool blnFound = false;
			Cyberware objFoundCyber = new Cyberware(_objCharacter);
			Gear objSensor = _objFunctions.FindCyberwareGear(treCyberware.SelectedNode.Tag.ToString(), _objCharacter.Cyberware, out objFoundCyber);
			if (objSensor != null)
				blnFound = true;

			// Make sure the Gear was found.
			if (!blnFound)
			{
				MessageBox.Show(LanguageManager.Instance.GetString("Message_ModifyVehicleGear"), LanguageManager.Instance.GetString("MessageTitle_SelectGear"), MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			XmlDocument objXmlDocument = XmlManager.Instance.Load("gear.xml");

			XmlNode objXmlGear = objXmlDocument.SelectSingleNode("/chummer/gears/gear[name = \"" + objSensor.Name + "\" and category = \"" + objSensor.Category + "\"]");

			frmSelectGear frmPickGear = new frmSelectGear(_objCharacter);
			//frmPickGear.ShowNegativeCapacityOnly = true;

			if (objXmlGear.InnerXml.Contains("<addoncategory>"))
			{
				string strCategories = "";
				foreach (XmlNode objXmlCategory in objXmlGear.SelectNodes("addoncategory"))
					strCategories += objXmlCategory.InnerText + ",";
				// Remove the trailing comma.
				strCategories = strCategories.Substring(0, strCategories.Length - 1);
				frmPickGear.AddCategory(strCategories);
			}

			if (frmPickGear.AllowedCategories != "")
				frmPickGear.AllowedCategories += objSensor.Category + ",";

			frmPickGear.ShowDialog(this);

			if (frmPickGear.DialogResult == DialogResult.Cancel)
				return;

			// Open the Gear XML file and locate the selected piece.
			objXmlGear = objXmlDocument.SelectSingleNode("/chummer/gears/gear[name = \"" + frmPickGear.SelectedGear + "\" and category = \"" + frmPickGear.SelectedCategory + "\"]");

			// Create the new piece of Gear.
			List<Weapon> objWeapons = new List<Weapon>();
			List<TreeNode> objWeaponNodes = new List<TreeNode>();
			TreeNode objNode = new TreeNode();
			Gear objGear = new Gear(_objCharacter);

			switch (frmPickGear.SelectedCategory)
			{
				case "Commlink":
				case "Commlink Upgrade":
					Commlink objCommlink = new Commlink(_objCharacter);
					objCommlink.Create(objXmlGear, _objCharacter, objNode, frmPickGear.SelectedRating);
					objCommlink.Quantity = frmPickGear.SelectedQty;
					objNode.Text = objCommlink.DisplayName;

					objGear = objCommlink;
					break;
				case "Commlink Operating System":
				case "Commlink Operating System Upgrade":
					OperatingSystem objOperatingSystem = new OperatingSystem(_objCharacter);
					objOperatingSystem.Create(objXmlGear, _objCharacter, objNode, frmPickGear.SelectedRating);
					objOperatingSystem.Quantity = frmPickGear.SelectedQty;
					objNode.Text = objOperatingSystem.DisplayName;

					objGear = objOperatingSystem;
					break;
				default:
					Gear objNewGear = new Gear(_objCharacter);
					objNewGear.Create(objXmlGear, _objCharacter, objNode, frmPickGear.SelectedRating, objWeapons, objWeaponNodes, "", frmPickGear.Hacked, frmPickGear.InherentProgram, true, true, frmPickGear.Aerodynamic);
					objNewGear.Quantity = frmPickGear.SelectedQty;
					objNode.Text = objNewGear.DisplayName;

					objGear = objNewGear;
					break;
			}

			if (objGear.InternalId == Guid.Empty.ToString())
				return;

			// Reduce the cost for Do It Yourself components.
			if (frmPickGear.DoItYourself)
				objGear.Cost = (Convert.ToDouble(objGear.Cost, GlobalOptions.Instance.CultureInfo) * 0.5).ToString();
			// Reduce the cost to 10% for Hacked programs.
			if (frmPickGear.Hacked)
			{
				if (objGear.Cost != "")
					objGear.Cost = "(" + objGear.Cost + ") * 0.1";
				if (objGear.Cost3 != "")
					objGear.Cost3 = "(" + objGear.Cost3 + ") * 0.1";
				if (objGear.Cost6 != "")
					objGear.Cost6 = "(" + objGear.Cost6 + ") * 0.1";
				if (objGear.Cost10 != "")
					objGear.Cost10 = "(" + objGear.Cost10 + ") * 0.1";
				if (objGear.Extra == "")
					objGear.Extra = LanguageManager.Instance.GetString("Label_SelectGear_Hacked");
				else
					objGear.Extra += ", " + LanguageManager.Instance.GetString("Label_SelectGear_Hacked");
			}
			// If the item was marked as free, change its cost.
			if (frmPickGear.FreeCost)
			{
				objGear.Cost = "0";
				objGear.Cost3 = "0";
				objGear.Cost6 = "0";
				objGear.Cost10 = "0";
			}

			objNode.Text = objGear.DisplayName;

			if (treCyberware.SelectedNode.Level < 3)
				objNode.ContextMenuStrip = cmsCyberwareGear;

			treCyberware.SelectedNode.Nodes.Add(objNode);
			treCyberware.SelectedNode.Expand();

			objGear.Parent = objSensor;
			objSensor.Children.Add(objGear);

			if (frmPickGear.AddAgain)
				tsCyberwareGearMenuAddAsPlugin_Click(sender, e);

			UpdateCharacterInfo();
			RefreshSelectedCyberware();
		}

		private void tsCyberwarePluginGearAddAsPlugin_Click(object sender, EventArgs e)
		{
			tsCyberwareGearAddAsPlugin_Click(sender, e);
		}

		private void tsWeaponAccessoryAddGear_Click(object sender, EventArgs e)
		{
			WeaponAccessory objAccessory = _objFunctions.FindWeaponAccessory(treWeapons.SelectedNode.Tag.ToString(), _objCharacter.Weapons);

			// Make sure the Weapon Accessory is allowed to accept Gear.
			if (objAccessory.AllowGear == null)
			{
				MessageBox.Show(LanguageManager.Instance.GetString("Message_WeaponGear"), LanguageManager.Instance.GetString("MessageTitle_CyberwareGear"), MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			frmSelectGear frmPickGear = new frmSelectGear(_objCharacter, false);
			string strCategories = "";
			foreach (XmlNode objXmlCategory in objAccessory.AllowGear)
				strCategories += objXmlCategory.InnerText + ",";
			frmPickGear.AllowedCategories = strCategories;
			frmPickGear.ShowDialog(this);

			if (frmPickGear.DialogResult == DialogResult.Cancel)
				return;

			TreeNode objNode = new TreeNode();

			// Open the Gear XML file and locate the selected piece.
			XmlDocument objXmlDocument = XmlManager.Instance.Load("gear.xml");
			XmlNode objXmlGear = objXmlDocument.SelectSingleNode("/chummer/gears/gear[name = \"" + frmPickGear.SelectedGear + "\" and category = \"" + frmPickGear.SelectedCategory + "\"]");

			// Create the new piece of Gear.
			List<Weapon> objWeapons = new List<Weapon>();
			List<TreeNode> objWeaponNodes = new List<TreeNode>();
			Gear objNewGear = new Gear(_objCharacter);
			switch (frmPickGear.SelectedCategory)
			{
				case "Commlink":
				case "Commlink Upgrade":
					Commlink objCommlink = new Commlink(_objCharacter);
					objCommlink.Create(objXmlGear, _objCharacter, objNode, frmPickGear.SelectedRating);
					objCommlink.Quantity = frmPickGear.SelectedQty;
					try
					{
						_blnSkipRefresh = true;
						nudGearQty.Increment = objCommlink.CostFor;
						//nudGearQty.Minimum = nudGearQty.Increment;
						_blnSkipRefresh = false;
					}
					catch
					{
					}
					objNode.Text = objCommlink.DisplayName;

					objNewGear = objCommlink;
					break;
				case "Commlink Operating System":
				case "Commlink Operating System Upgrade":
					OperatingSystem objOperatingSystem = new OperatingSystem(_objCharacter);
					objOperatingSystem.Create(objXmlGear, _objCharacter, objNode, frmPickGear.SelectedRating);
					objOperatingSystem.Quantity = frmPickGear.SelectedQty;
					try
					{
						_blnSkipRefresh = true;
						nudGearQty.Increment = objOperatingSystem.CostFor;
						//nudGearQty.Minimum = nudGearQty.Increment;
						_blnSkipRefresh = false;
					}
					catch
					{
					}
					objNode.Text = objOperatingSystem.DisplayName;

					objNewGear = objOperatingSystem;
					break;
				default:
					Gear objGear = new Gear(_objCharacter);
					objGear.Create(objXmlGear, _objCharacter, objNode, frmPickGear.SelectedRating, objWeapons, objWeaponNodes, "", frmPickGear.Hacked, frmPickGear.InherentProgram, true, true, frmPickGear.Aerodynamic);
					objGear.Quantity = frmPickGear.SelectedQty;
					try
					{
						_blnSkipRefresh = true;
						nudGearQty.Increment = objGear.CostFor;
						//nudGearQty.Minimum = nudGearQty.Increment;
						_blnSkipRefresh = false;
					}
					catch
					{
					}
					objNode.Text = objGear.DisplayName;

					objNewGear = objGear;
					break;
			}

			if (objNewGear.InternalId == Guid.Empty.ToString())
				return;

			// Reduce the cost for Do It Yourself components.
			if (frmPickGear.DoItYourself)
				objNewGear.Cost = (Convert.ToDouble(objNewGear.Cost, GlobalOptions.Instance.CultureInfo) * 0.5).ToString();
			// Reduce the cost to 10% for Hacked programs.
			if (frmPickGear.Hacked)
			{
				if (objNewGear.Cost != "")
					objNewGear.Cost = "(" + objNewGear.Cost + ") * 0.1";
				if (objNewGear.Cost3 != "")
					objNewGear.Cost3 = "(" + objNewGear.Cost3 + ") * 0.1";
				if (objNewGear.Cost6 != "")
					objNewGear.Cost6 = "(" + objNewGear.Cost6 + ") * 0.1";
				if (objNewGear.Cost10 != "")
					objNewGear.Cost10 = "(" + objNewGear.Cost10 + ") * 0.1";
				if (objNewGear.Extra == "")
					objNewGear.Extra = LanguageManager.Instance.GetString("Label_SelectGear_Hacked");
				else
					objNewGear.Extra += ", " + LanguageManager.Instance.GetString("Label_SelectGear_Hacked");
			}
			// If the item was marked as free, change its cost.
			if (frmPickGear.FreeCost)
			{
				objNewGear.Cost = "0";
				objNewGear.Cost3 = "0";
				objNewGear.Cost6 = "0";
				objNewGear.Cost10 = "0";
			}

			// Create any Weapons that came with this Gear.
			foreach (Weapon objWeapon in objWeapons)
				_objCharacter.Weapons.Add(objWeapon);

			foreach (TreeNode objWeaponNode in objWeaponNodes)
			{
				objWeaponNode.ContextMenuStrip = cmsWeapon;
				treWeapons.Nodes[0].Nodes.Add(objWeaponNode);
				treWeapons.Nodes[0].Expand();
			}

			objAccessory.Gear.Add(objNewGear);

			objNode.ContextMenuStrip = cmsWeaponAccessoryGear;
			treWeapons.SelectedNode.Nodes.Add(objNode);
			treWeapons.SelectedNode.Expand();

			UpdateCharacterInfo();

			if (frmPickGear.AddAgain)
				tsWeaponAccessoryAddGear_Click(sender, e);

			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void tsWeaponAccessoryGearMenuAddAsPlugin_Click(object sender, EventArgs e)
		{
			// Locate the Vehicle Sensor Gear.
			bool blnFound = false;
			WeaponAccessory objFoundAccessory = new WeaponAccessory(_objCharacter);
			Gear objSensor = _objFunctions.FindWeaponGear(treWeapons.SelectedNode.Tag.ToString(), _objCharacter.Weapons, out objFoundAccessory);
			if (objSensor != null)
				blnFound = true;

			// Make sure the Gear was found.
			if (!blnFound)
			{
				MessageBox.Show(LanguageManager.Instance.GetString("Message_ModifyVehicleGear"), LanguageManager.Instance.GetString("MessageTitle_SelectGear"), MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			XmlDocument objXmlDocument = XmlManager.Instance.Load("gear.xml");

			XmlNode objXmlGear = objXmlDocument.SelectSingleNode("/chummer/gears/gear[name = \"" + objSensor.Name + "\" and category = \"" + objSensor.Category + "\"]");

			frmSelectGear frmPickGear = new frmSelectGear(_objCharacter);
			//frmPickGear.ShowNegativeCapacityOnly = true;

			if (objXmlGear.InnerXml.Contains("<addoncategory>"))
			{
				string strCategories = "";
				foreach (XmlNode objXmlCategory in objXmlGear.SelectNodes("addoncategory"))
					strCategories += objXmlCategory.InnerText + ",";
				// Remove the trailing comma.
				strCategories = strCategories.Substring(0, strCategories.Length - 1);
				frmPickGear.AddCategory(strCategories);
			}

			if (frmPickGear.AllowedCategories != "")
				frmPickGear.AllowedCategories += objSensor.Category + ",";

			frmPickGear.ShowDialog(this);

			if (frmPickGear.DialogResult == DialogResult.Cancel)
				return;

			// Open the Gear XML file and locate the selected piece.
			objXmlGear = objXmlDocument.SelectSingleNode("/chummer/gears/gear[name = \"" + frmPickGear.SelectedGear + "\" and category = \"" + frmPickGear.SelectedCategory + "\"]");

			// Create the new piece of Gear.
			List<Weapon> objWeapons = new List<Weapon>();
			List<TreeNode> objWeaponNodes = new List<TreeNode>();
			TreeNode objNode = new TreeNode();
			Gear objGear = new Gear(_objCharacter);

			switch (frmPickGear.SelectedCategory)
			{
				case "Commlink":
				case "Commlink Upgrade":
					Commlink objCommlink = new Commlink(_objCharacter);
					objCommlink.Create(objXmlGear, _objCharacter, objNode, frmPickGear.SelectedRating);
					objCommlink.Quantity = frmPickGear.SelectedQty;
					objNode.Text = objCommlink.DisplayName;

					objGear = objCommlink;
					break;
				case "Commlink Operating System":
				case "Commlink Operating System Upgrade":
					OperatingSystem objOperatingSystem = new OperatingSystem(_objCharacter);
					objOperatingSystem.Create(objXmlGear, _objCharacter, objNode, frmPickGear.SelectedRating);
					objOperatingSystem.Quantity = frmPickGear.SelectedQty;
					objNode.Text = objOperatingSystem.DisplayName;

					objGear = objOperatingSystem;
					break;
				default:
					Gear objNewGear = new Gear(_objCharacter);
					objNewGear.Create(objXmlGear, _objCharacter, objNode, frmPickGear.SelectedRating, objWeapons, objWeaponNodes, "", frmPickGear.Hacked, frmPickGear.InherentProgram, true, true, frmPickGear.Aerodynamic);
					objNewGear.Quantity = frmPickGear.SelectedQty;
					objNode.Text = objNewGear.DisplayName;

					objGear = objNewGear;
					break;
			}

			if (objGear.InternalId == Guid.Empty.ToString())
				return;

			// Reduce the cost for Do It Yourself components.
			if (frmPickGear.DoItYourself)
				objGear.Cost = (Convert.ToDouble(objGear.Cost, GlobalOptions.Instance.CultureInfo) * 0.5).ToString();
			// Reduce the cost to 10% for Hacked programs.
			if (frmPickGear.Hacked)
			{
				if (objGear.Cost != "")
					objGear.Cost = "(" + objGear.Cost + ") * 0.1";
				if (objGear.Cost3 != "")
					objGear.Cost3 = "(" + objGear.Cost3 + ") * 0.1";
				if (objGear.Cost6 != "")
					objGear.Cost6 = "(" + objGear.Cost6 + ") * 0.1";
				if (objGear.Cost10 != "")
					objGear.Cost10 = "(" + objGear.Cost10 + ") * 0.1";
				if (objGear.Extra == "")
					objGear.Extra = LanguageManager.Instance.GetString("Label_SelectGear_Hacked");
				else
					objGear.Extra += ", " + LanguageManager.Instance.GetString("Label_SelectGear_Hacked");
			}
			// If the item was marked as free, change its cost.
			if (frmPickGear.FreeCost)
			{
				objGear.Cost = "0";
				objGear.Cost3 = "0";
				objGear.Cost6 = "0";
				objGear.Cost10 = "0";
			}

			objNode.Text = objGear.DisplayName;

			objNode.ContextMenuStrip = cmsWeaponAccessoryGear;

			treWeapons.SelectedNode.Nodes.Add(objNode);
			treWeapons.SelectedNode.Expand();

			objGear.Parent = objSensor;
			objSensor.Children.Add(objGear);

			if (frmPickGear.AddAgain)
				tsWeaponAccessoryGearMenuAddAsPlugin_Click(sender, e);

			UpdateCharacterInfo();
			RefreshSelectedWeapon();
		}

		private void tsVehicleRenameLocation_Click(object sender, EventArgs e)
		{
			string strNewLocation = "";
			frmSelectText frmPickText = new frmSelectText();
			frmPickText.Description = LanguageManager.Instance.GetString("String_AddLocation");
			frmPickText.ShowDialog(this);

			if (frmPickText.DialogResult == DialogResult.Cancel)
				return;

			// Determine if this is a Location.
			TreeNode objVehicleNode = treVehicles.SelectedNode;
			do
			{
				objVehicleNode = objVehicleNode.Parent;
			} while (objVehicleNode.Level > 1);

			// Get a reference to the affected Vehicle.
			Vehicle objVehicle = new Vehicle(_objCharacter);
			foreach (Vehicle objCharacterVehicle in _objCharacter.Vehicles)
			{
				if (objCharacterVehicle.InternalId == objVehicleNode.Tag.ToString())
				{
					objVehicle = objCharacterVehicle;
					break;
				}
			}

			strNewLocation = frmPickText.SelectedValue;

			int i = -1;
			foreach (string strLocation in objVehicle.Locations)
			{
				i++;
				if (strLocation == treVehicles.SelectedNode.Text)
				{
					foreach (Gear objGear in objVehicle.Gear)
					{
						if (objGear.Location == strLocation)
							objGear.Location = strNewLocation;
					}

					objVehicle.Locations[i] = strNewLocation;
					treVehicles.SelectedNode.Text = strNewLocation;
					break;
				}
			}
		}

		private void tsCreateNaturalWeapon_Click(object sender, EventArgs e)
		{
			frmNaturalWeapon frmCreateNaturalWeapon = new frmNaturalWeapon(_objCharacter);
			frmCreateNaturalWeapon.ShowDialog(this);

			if (frmCreateNaturalWeapon.DialogResult == DialogResult.Cancel)
				return;

			Weapon objWeapon = frmCreateNaturalWeapon.SelectedWeapon;
			_objCharacter.Weapons.Add(objWeapon);
			_objFunctions.CreateWeaponTreeNode(objWeapon, treWeapons.Nodes[0], cmsWeapon, cmsWeaponMod, cmsWeaponAccessory, cmsWeaponAccessoryGear);

			_blnIsDirty = true;
			UpdateCharacterInfo();
			UpdateWindowTitle();
		}

		private void tsVehicleWeaponAccessoryNotes_Click(object sender, EventArgs e)
		{
			WeaponAccessory objAccessory = _objFunctions.FindVehicleWeaponAccessory(treVehicles.SelectedNode.Tag.ToString(), _objCharacter.Vehicles);

			frmNotes frmItemNotes = new frmNotes();
			frmItemNotes.Notes = objAccessory.Notes;
			string strOldValue = objAccessory.Notes;
			frmItemNotes.ShowDialog(this);

			if (frmItemNotes.DialogResult == DialogResult.OK)
			{
				objAccessory.Notes = frmItemNotes.Notes;
				if (objAccessory.Notes != strOldValue)
				{
					_blnIsDirty = true;
					UpdateWindowTitle();
				}
			}

			if (objAccessory.Notes != string.Empty)
				treVehicles.SelectedNode.ForeColor = Color.SaddleBrown;
			else
				treVehicles.SelectedNode.ForeColor = SystemColors.WindowText;
			treVehicles.SelectedNode.ToolTipText = objAccessory.Notes;
		}

		private void tsVehicleWeaponModNotes_Click(object sender, EventArgs e)
		{
			WeaponMod objMod = _objFunctions.FindVehicleWeaponMod(treVehicles.SelectedNode.Tag.ToString(), _objCharacter.Vehicles);

			frmNotes frmItemNotes = new frmNotes();
			frmItemNotes.Notes = objMod.Notes;
			string strOldValue = objMod.Notes;
			frmItemNotes.ShowDialog(this);

			if (frmItemNotes.DialogResult == DialogResult.OK)
			{
				objMod.Notes = frmItemNotes.Notes;
				if (objMod.Notes != strOldValue)
				{
					_blnIsDirty = true;
					UpdateWindowTitle();
				}
			}

			if (objMod.Notes != string.Empty)
				treVehicles.SelectedNode.ForeColor = Color.SaddleBrown;
			else
				treVehicles.SelectedNode.ForeColor = SystemColors.WindowText;
			treVehicles.SelectedNode.ToolTipText = objMod.Notes;
		}

		private void tsVehicleWeaponAccessoryGearMenuAddAsPlugin_Click(object sender, EventArgs e)
		{
			// Locate the Vehicle Sensor Gear.
			bool blnFound = false;
			Vehicle objFoundVehicle = new Vehicle(_objCharacter);
			Gear objSensor = _objFunctions.FindVehicleGear(treVehicles.SelectedNode.Tag.ToString(), _objCharacter.Vehicles, out objFoundVehicle);
			if (objSensor != null)
				blnFound = true;

			// Make sure the Gear was found.
			if (!blnFound)
			{
				MessageBox.Show(LanguageManager.Instance.GetString("Message_ModifyVehicleGear"), LanguageManager.Instance.GetString("MessageTitle_SelectGear"), MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			XmlDocument objXmlDocument = XmlManager.Instance.Load("gear.xml");

			XmlNode objXmlGear = objXmlDocument.SelectSingleNode("/chummer/gears/gear[name = \"" + objSensor.Name + "\" and category = \"" + objSensor.Category + "\"]");

			frmSelectGear frmPickGear = new frmSelectGear(_objCharacter);
			//frmPickGear.ShowNegativeCapacityOnly = true;

			if (objXmlGear.InnerXml.Contains("<addoncategory>"))
			{
				string strCategories = "";
				foreach (XmlNode objXmlCategory in objXmlGear.SelectNodes("addoncategory"))
					strCategories += objXmlCategory.InnerText + ",";
				// Remove the trailing comma.
				strCategories = strCategories.Substring(0, strCategories.Length - 1);
				frmPickGear.AddCategory(strCategories);
			}

			if (frmPickGear.AllowedCategories != "")
				frmPickGear.AllowedCategories += objSensor.Category + ",";

			frmPickGear.ShowDialog(this);

			if (frmPickGear.DialogResult == DialogResult.Cancel)
				return;

			// Open the Gear XML file and locate the selected piece.
			objXmlGear = objXmlDocument.SelectSingleNode("/chummer/gears/gear[name = \"" + frmPickGear.SelectedGear + "\" and category = \"" + frmPickGear.SelectedCategory + "\"]");

			// Create the new piece of Gear.
			List<Weapon> objWeapons = new List<Weapon>();
			List<TreeNode> objWeaponNodes = new List<TreeNode>();
			TreeNode objNode = new TreeNode();
			Gear objGear = new Gear(_objCharacter);

			switch (frmPickGear.SelectedCategory)
			{
				case "Commlink":
				case "Commlink Upgrade":
					Commlink objCommlink = new Commlink(_objCharacter);
					objCommlink.Create(objXmlGear, _objCharacter, objNode, frmPickGear.SelectedRating, false);
					objCommlink.Quantity = frmPickGear.SelectedQty;
					objNode.Text = objCommlink.DisplayName;

					objGear = objCommlink;
					break;
				case "Commlink Operating System":
				case "Commlink Operating System Upgrade":
					OperatingSystem objOperatingSystem = new OperatingSystem(_objCharacter);
					objOperatingSystem.Create(objXmlGear, _objCharacter, objNode, frmPickGear.SelectedRating, false);
					objOperatingSystem.Quantity = frmPickGear.SelectedQty;
					objNode.Text = objOperatingSystem.DisplayName;

					objGear = objOperatingSystem;
					break;
				default:
					Gear objNewGear = new Gear(_objCharacter);
					objNewGear.Create(objXmlGear, _objCharacter, objNode, frmPickGear.SelectedRating, objWeapons, objWeaponNodes, "", frmPickGear.Hacked, frmPickGear.InherentProgram, false, true, frmPickGear.Aerodynamic);
					objNewGear.Quantity = frmPickGear.SelectedQty;
					objNode.Text = objNewGear.DisplayName;

					objGear = objNewGear;
					break;
			}

			if (objGear.InternalId == Guid.Empty.ToString())
				return;

			// Reduce the cost for Do It Yourself components.
			if (frmPickGear.DoItYourself)
				objGear.Cost = (Convert.ToDouble(objGear.Cost, GlobalOptions.Instance.CultureInfo) * 0.5).ToString();
			// Reduce the cost to 10% for Hacked programs.
			if (frmPickGear.Hacked)
			{
				if (objGear.Cost != "")
					objGear.Cost = "(" + objGear.Cost + ") * 0.1";
				if (objGear.Cost3 != "")
					objGear.Cost3 = "(" + objGear.Cost3 + ") * 0.1";
				if (objGear.Cost6 != "")
					objGear.Cost6 = "(" + objGear.Cost6 + ") * 0.1";
				if (objGear.Cost10 != "")
					objGear.Cost10 = "(" + objGear.Cost10 + ") * 0.1";
				if (objGear.Extra == "")
					objGear.Extra = LanguageManager.Instance.GetString("Label_SelectGear_Hacked");
				else
					objGear.Extra += ", " + LanguageManager.Instance.GetString("Label_SelectGear_Hacked");
			}
			// If the item was marked as free, change its cost.
			if (frmPickGear.FreeCost)
			{
				objGear.Cost = "0";
				objGear.Cost3 = "0";
				objGear.Cost6 = "0";
				objGear.Cost10 = "0";
			}

			objNode.Text = objGear.DisplayName;

			objNode.ContextMenuStrip = cmsVehicleWeaponAccessoryGear;

			treVehicles.SelectedNode.Nodes.Add(objNode);
			treVehicles.SelectedNode.Expand();

			objGear.Parent = objSensor;
			objSensor.Children.Add(objGear);

			if (frmPickGear.AddAgain)
				tsVehicleWeaponAccessoryGearMenuAddAsPlugin_Click(sender, e);

			UpdateCharacterInfo();
			RefreshSelectedVehicle();
		}

		private void tsVehicleWeaponAccessoryAddGear_Click(object sender, EventArgs e)
		{
			WeaponAccessory objAccessory = _objFunctions.FindVehicleWeaponAccessory(treVehicles.SelectedNode.Tag.ToString(), _objCharacter.Vehicles);

			// Make sure the Weapon Accessory is allowed to accept Gear.
			if (objAccessory.AllowGear == null)
			{
				MessageBox.Show(LanguageManager.Instance.GetString("Message_WeaponGear"), LanguageManager.Instance.GetString("MessageTitle_CyberwareGear"), MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			frmSelectGear frmPickGear = new frmSelectGear(_objCharacter, false);
			string strCategories = "";
			foreach (XmlNode objXmlCategory in objAccessory.AllowGear)
				strCategories += objXmlCategory.InnerText + ",";
			frmPickGear.AllowedCategories = strCategories;
			frmPickGear.ShowDialog(this);

			if (frmPickGear.DialogResult == DialogResult.Cancel)
				return;

			TreeNode objNode = new TreeNode();

			// Open the Gear XML file and locate the selected piece.
			XmlDocument objXmlDocument = XmlManager.Instance.Load("gear.xml");
			XmlNode objXmlGear = objXmlDocument.SelectSingleNode("/chummer/gears/gear[name = \"" + frmPickGear.SelectedGear + "\" and category = \"" + frmPickGear.SelectedCategory + "\"]");

			// Create the new piece of Gear.
			List<Weapon> objWeapons = new List<Weapon>();
			List<TreeNode> objWeaponNodes = new List<TreeNode>();
			Gear objNewGear = new Gear(_objCharacter);
			switch (frmPickGear.SelectedCategory)
			{
				case "Commlink":
				case "Commlink Upgrade":
					Commlink objCommlink = new Commlink(_objCharacter);
					objCommlink.Create(objXmlGear, _objCharacter, objNode, frmPickGear.SelectedRating, false);
					objCommlink.Quantity = frmPickGear.SelectedQty;
					try
					{
						_blnSkipRefresh = true;
						nudGearQty.Increment = objCommlink.CostFor;
						//nudGearQty.Minimum = nudGearQty.Increment;
						_blnSkipRefresh = false;
					}
					catch
					{
					}
					objNode.Text = objCommlink.DisplayName;

					objNewGear = objCommlink;
					break;
				case "Commlink Operating System":
				case "Commlink Operating System Upgrade":
					OperatingSystem objOperatingSystem = new OperatingSystem(_objCharacter);
					objOperatingSystem.Create(objXmlGear, _objCharacter, objNode, frmPickGear.SelectedRating, false);
					objOperatingSystem.Quantity = frmPickGear.SelectedQty;
					try
					{
						_blnSkipRefresh = true;
						nudGearQty.Increment = objOperatingSystem.CostFor;
						//nudGearQty.Minimum = nudGearQty.Increment;
						_blnSkipRefresh = false;
					}
					catch
					{
					}
					objNode.Text = objOperatingSystem.DisplayName;

					objNewGear = objOperatingSystem;
					break;
				default:
					Gear objGear = new Gear(_objCharacter);
					objGear.Create(objXmlGear, _objCharacter, objNode, frmPickGear.SelectedRating, objWeapons, objWeaponNodes, "", frmPickGear.Hacked, frmPickGear.InherentProgram, false, true, frmPickGear.Aerodynamic);
					objGear.Quantity = frmPickGear.SelectedQty;
					try
					{
						_blnSkipRefresh = true;
						nudGearQty.Increment = objGear.CostFor;
						//nudGearQty.Minimum = nudGearQty.Increment;
						_blnSkipRefresh = false;
					}
					catch
					{
					}
					objNode.Text = objGear.DisplayName;

					objNewGear = objGear;
					break;
			}

			if (objNewGear.InternalId == Guid.Empty.ToString())
				return;

			// Reduce the cost for Do It Yourself components.
			if (frmPickGear.DoItYourself)
				objNewGear.Cost = (Convert.ToDouble(objNewGear.Cost, GlobalOptions.Instance.CultureInfo) * 0.5).ToString();
			// Reduce the cost to 10% for Hacked programs.
			if (frmPickGear.Hacked)
			{
				if (objNewGear.Cost != "")
					objNewGear.Cost = "(" + objNewGear.Cost + ") * 0.1";
				if (objNewGear.Cost3 != "")
					objNewGear.Cost3 = "(" + objNewGear.Cost3 + ") * 0.1";
				if (objNewGear.Cost6 != "")
					objNewGear.Cost6 = "(" + objNewGear.Cost6 + ") * 0.1";
				if (objNewGear.Cost10 != "")
					objNewGear.Cost10 = "(" + objNewGear.Cost10 + ") * 0.1";
				if (objNewGear.Extra == "")
					objNewGear.Extra = LanguageManager.Instance.GetString("Label_SelectGear_Hacked");
				else
					objNewGear.Extra += ", " + LanguageManager.Instance.GetString("Label_SelectGear_Hacked");
			}
			// If the item was marked as free, change its cost.
			if (frmPickGear.FreeCost)
			{
				objNewGear.Cost = "0";
				objNewGear.Cost3 = "0";
				objNewGear.Cost6 = "0";
				objNewGear.Cost10 = "0";
			}

			objAccessory.Gear.Add(objNewGear);

			objNode.ContextMenuStrip = cmsVehicleWeaponAccessoryGear;
			treVehicles.SelectedNode.Nodes.Add(objNode);
			treVehicles.SelectedNode.Expand();

			UpdateCharacterInfo();

			if (frmPickGear.AddAgain)
				tsVehicleWeaponAccessoryAddGear_Click(sender, e);

			_blnIsDirty = true;
			UpdateWindowTitle();
		}
		#endregion

		#region Additional Common Tab Control Events
		private void treQualities_AfterSelect(object sender, TreeViewEventArgs e)
		{
			// Locate the selected Quality.
			lblQualitySource.Text = "";
			tipTooltip.SetToolTip(lblQualitySource, null);
			try
			{
				if (treQualities.SelectedNode.Level == 0)
					return;
			}
			catch
			{
				return;
			}

			Quality objQuality = _objFunctions.FindQuality(treQualities.SelectedNode.Tag.ToString(), _objCharacter.Qualities);

			string strBook = _objOptions.LanguageBookShort(objQuality.Source);
			string strPage = objQuality.Page;
			lblQualitySource.Text = strBook + " " + strPage;
			tipTooltip.SetToolTip(lblQualitySource, _objOptions.LanguageBookLong(objQuality.Source) + " " + LanguageManager.Instance.GetString("String_Page") + " " + objQuality.Page);
			if (_objCharacter.BuildMethod == CharacterBuildMethod.BP)
				lblQualityBP.Text = objQuality.BP.ToString() + " " + LanguageManager.Instance.GetString("String_BP");
			else
				lblQualityBP.Text = (objQuality.BP * _objOptions.KarmaQuality).ToString() + " " + LanguageManager.Instance.GetString("String_Karma");
		}
		#endregion

		#region Additional Cyberware Tab Control Events
		private void treCyberware_AfterSelect(object sender, TreeViewEventArgs e)
		{
			RefreshSelectedCyberware();
		}

		private void cboCyberwareGrade_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (!_blnSkipRefresh && !_blnLoading)
			{
				// Locate the selected piece of Cyberware.
				Cyberware objCyberware = _objFunctions.FindCyberware(treCyberware.SelectedNode.Tag.ToString(), _objCharacter.Cyberware);
				if (objCyberware == null)
					return;

				GradeList objGradeList;
				if (objCyberware.SourceType == Improvement.ImprovementSource.Bioware)
					objGradeList = GlobalOptions.BiowareGrades;
				else
					objGradeList = GlobalOptions.CyberwareGrades;

				// Updated the selected Cyberware Grade.
				objCyberware.Grade = objGradeList.GetGrade(cboCyberwareGrade.SelectedValue.ToString());

				// Run through all of the child pieces and make sure their Grade matches.
				foreach (Cyberware objChildCyberware in objCyberware.Children)
				{
					objChildCyberware.Grade = objCyberware.Grade;
				}

				RefreshSelectedCyberware();

				_blnIsDirty = true;
				UpdateWindowTitle();
			}
		}

		private void nudCyberwareRating_ValueChanged(object sender, EventArgs e)
		{
			if (!_blnSkipRefresh)
			{
				// Locate the selected piece of Cyberware.
				bool blnFound = false;
				Cyberware objCyberware = _objFunctions.FindCyberware(treCyberware.SelectedNode.Tag.ToString(), _objCharacter.Cyberware);
				if (objCyberware != null)
					blnFound = true;

				if (blnFound)
				{
					// Update the selected Cyberware Rating.
					objCyberware.Rating = Convert.ToInt32(nudCyberwareRating.Value);

					// See if a Bonus node exists.
					if (objCyberware.Bonus != null)
					{
						// If the Bonus contains "Rating", remove the existing Improvements and create new ones.
						if (objCyberware.Bonus.InnerXml.Contains("Rating"))
						{
							_objImprovementManager.RemoveImprovements(objCyberware.SourceType, objCyberware.InternalId);
							_objImprovementManager.CreateImprovements(objCyberware.SourceType, objCyberware.InternalId, objCyberware.Bonus, false, objCyberware.Rating, objCyberware.DisplayNameShort);
						}
					}

					treCyberware.SelectedNode.Text = objCyberware.DisplayName;
				}
				else
				{
					// Find the selected piece of Gear.
					Cyberware objFoundCyberware = new Cyberware(_objCharacter);
					Gear objGear = _objFunctions.FindCyberwareGear(treCyberware.SelectedNode.Tag.ToString(), _objCharacter.Cyberware, out objFoundCyberware);

					objGear.Rating = Convert.ToInt32(nudCyberwareRating.Value);

					// See if a Bonus node exists.
					if (objGear.Bonus != null)
					{
						_objImprovementManager.RemoveImprovements(Improvement.ImprovementSource.Gear, objGear.InternalId);
						if (objGear.Extra != "")
						{
							_objImprovementManager.ForcedValue = objGear.Extra;
							if (objGear.Extra.EndsWith(", Hacked"))
								_objImprovementManager.ForcedValue = objGear.Extra.Replace(", Hacked", string.Empty);
						}
						_objImprovementManager.CreateImprovements(Improvement.ImprovementSource.Gear, objGear.InternalId, objGear.Bonus, false, objGear.Rating, objGear.DisplayNameShort);
					}

					treCyberware.SelectedNode.Text = objGear.DisplayName;
				}

				RefreshSelectedCyberware();

				_blnIsDirty = true;
				UpdateWindowTitle();
			}
			UpdateCharacterInfo();
		}

		private void chkCyberwareBlackMarketDiscount_CheckedChanged(object sender, EventArgs e)
		{
			if (_blnSkipRefresh)
				return;

			// Locate the selected Cyberware.
			try
			{
				Cyberware objCyberware = _objFunctions.FindCyberware(treCyberware.SelectedNode.Tag.ToString(), _objCharacter.Cyberware);
				if (objCyberware != null)
					objCyberware.DiscountCost = chkCyberwareBlackMarketDiscount.Checked;

				Gear objGear = _objFunctions.FindCyberwareGear(treCyberware.SelectedNode.Tag.ToString(), _objCharacter.Cyberware, out objCyberware);
				if (objGear != null)
					objGear.DiscountCost = chkCyberwareBlackMarketDiscount.Checked;

				RefreshSelectedCyberware();
				UpdateCharacterInfo();

				_blnIsDirty = true;
				UpdateWindowTitle();
			}
			catch
			{
			}
		}
		#endregion

		#region Additional Street Gear Tab Control Events
		private void treWeapons_AfterSelect(object sender, TreeViewEventArgs e)
		{
			RefreshSelectedWeapon();
			RefreshPasteStatus();
		}

		private void treWeapons_ItemDrag(object sender, ItemDragEventArgs e)
		{
			try
			{
				if (treWeapons.SelectedNode.Level != 1 && treWeapons.SelectedNode.Level != 0)
					return;

				// Do not allow the root element to be moved.
				if (treWeapons.SelectedNode.Tag.ToString() == "Node_SelectedWeapons")
					return;
			}
			catch
			{
				return;
			}
			_intDragLevel = treWeapons.SelectedNode.Level;
			DoDragDrop(e.Item, DragDropEffects.Move);
		}

		private void treWeapons_DragEnter(object sender, DragEventArgs e)
		{
			e.Effect = DragDropEffects.Move;
		}

		private void treWeapons_DragDrop(object sender, DragEventArgs e)
		{
			Point pt = ((TreeView)sender).PointToClient(new Point(e.X, e.Y));
			TreeNode nodDestination = ((TreeView)sender).GetNodeAt(pt);

			int intNewIndex = 0;
			try
			{
				intNewIndex = nodDestination.Index;
			}
			catch
			{
				intNewIndex = treWeapons.Nodes[treWeapons.Nodes.Count - 1].Nodes.Count;
				nodDestination = treWeapons.Nodes[treWeapons.Nodes.Count - 1];
			}

			if (treWeapons.SelectedNode.Level == 1)
				_objController.MoveWeaponNode(intNewIndex, nodDestination, treWeapons);
			else
				_objController.MoveWeaponRoot(intNewIndex, nodDestination, treWeapons);

			// Clear the background color for all Nodes.
			_objFunctions.ClearNodeBackground(treWeapons, null);

			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void treWeapons_DragOver(object sender, DragEventArgs e)
		{
			Point pt = ((TreeView)sender).PointToClient(new Point(e.X, e.Y));
			TreeNode objNode = ((TreeView)sender).GetNodeAt(pt);

			if (objNode == null)
				return;

			// Highlight the Node that we're currently dragging over, provided it is of the same level or higher.
			if (objNode.Level <= _intDragLevel)
				objNode.BackColor = SystemColors.ControlDark;

			// Clear the background colour for all other Nodes.
			_objFunctions.ClearNodeBackground(treWeapons, objNode);
		}

		private void treArmor_AfterSelect(object sender, TreeViewEventArgs e)
		{
			if (treArmor.SelectedNode.Level == 0)
			{
				cmdArmorEquipAll.Visible = true;
				cmdArmorUnEquipAll.Visible = true;
				lblArmorEquippedLabel.Visible = true;
				lblArmorEquipped.Visible = true;
			}
			else
			{
				cmdArmorEquipAll.Visible = false;
				cmdArmorUnEquipAll.Visible = false;
				lblArmorEquippedLabel.Visible = false;
				lblArmorEquipped.Visible = false;
			}

			RefreshSelectedArmor();
			RefreshPasteStatus();
		}

		private void treArmor_ItemDrag(object sender, ItemDragEventArgs e)
		{
			try
			{
				if (treArmor.SelectedNode.Level != 1)
					return;
			}
			catch
			{
				return;
			}
			_intDragLevel = treArmor.SelectedNode.Level;
			DoDragDrop(e.Item, DragDropEffects.Move);
		}

		private void treArmor_DragEnter(object sender, DragEventArgs e)
		{
			e.Effect = DragDropEffects.Move;
		}

		private void treArmor_DragDrop(object sender, DragEventArgs e)
		{
			Point pt = ((TreeView)sender).PointToClient(new Point(e.X, e.Y));
			TreeNode nodDestination = ((TreeView)sender).GetNodeAt(pt);

			int intNewIndex = 0;
			try
			{
				intNewIndex = nodDestination.Index;
			}
			catch
			{
				intNewIndex = treArmor.Nodes[treArmor.Nodes.Count - 1].Nodes.Count;
				nodDestination = treArmor.Nodes[treArmor.Nodes.Count - 1];
			}

			_objController.MoveArmorNode(intNewIndex, nodDestination, treArmor);

			// Clear the background color for all Nodes.
			_objFunctions.ClearNodeBackground(treArmor, null);

			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void treArmor_DragOver(object sender, DragEventArgs e)
		{
			Point pt = ((TreeView)sender).PointToClient(new Point(e.X, e.Y));
			TreeNode objNode = ((TreeView)sender).GetNodeAt(pt);

			if (objNode == null)
				return;

			// Highlight the Node that we're currently dragging over, provided it is of the same level or higher.
			if (objNode.Level <= _intDragLevel)
				objNode.BackColor = SystemColors.ControlDark;

			// Clear the background colour for all other Nodes.
			_objFunctions.ClearNodeBackground(treArmor, objNode);
		}

		private void treLifestyles_AfterSelect(object sender, TreeViewEventArgs e)
		{
			RefreshSelectedLifestyle();
			RefreshPasteStatus();
		}

		private void treLifestyles_DoubleClick(object sender, EventArgs e)
		{
			try
			{
				if (treLifestyles.SelectedNode.Level == 0)
					return;
			}
			catch
			{
				return;
			}

			// Locate the selected Lifestyle.
			Lifestyle objLifestyle = new Lifestyle(_objCharacter);
			string strGuid = "";
			int intMonths = 0;
			int intPosition = -1;
			foreach (Lifestyle objCharacterLifestyle in _objCharacter.Lifestyles)
			{
				intPosition++;
				if (objCharacterLifestyle.InternalId == treLifestyles.SelectedNode.Tag.ToString())
				{
					objLifestyle = objCharacterLifestyle;
					strGuid = objLifestyle.InternalId;
					intMonths = objLifestyle.Months;
					break;
				}
			}

			Lifestyle objNewLifestyle = new Lifestyle(_objCharacter);
			if (objLifestyle.Comforts != "")
			{
				// Edit Advanced Lifestyle.
				frmSelectAdvancedLifestyle frmPickLifestyle = new frmSelectAdvancedLifestyle(objNewLifestyle, _objCharacter);
				frmPickLifestyle.SetLifestyle(objLifestyle);
				frmPickLifestyle.ShowDialog(this);

				if (frmPickLifestyle.DialogResult == DialogResult.Cancel)
					return;

				// Update the selected Lifestyle and refresh the list.
				objLifestyle = frmPickLifestyle.SelectedLifestyle;
				objLifestyle.SetInternalId(strGuid);
				objLifestyle.Months = intMonths;
				_objCharacter.Lifestyles[intPosition] = objLifestyle;
				treLifestyles.SelectedNode.Text = objLifestyle.DisplayNameShort;
				RefreshSelectedLifestyle();
				UpdateCharacterInfo();
			}
			else
			{
				// Edit Basic Lifestyle.
				frmSelectLifestyle frmPickLifestyle = new frmSelectLifestyle(objNewLifestyle, _objCharacter);
				frmPickLifestyle.SetLifestyle(objLifestyle);
				frmPickLifestyle.ShowDialog(this);

				if (frmPickLifestyle.DialogResult == DialogResult.Cancel)
					return;

				// Update the selected Lifestyle and refresh the list.
				objLifestyle = frmPickLifestyle.SelectedLifestyle;
				objLifestyle.SetInternalId(strGuid);
				objLifestyle.Months = intMonths;
				_objCharacter.Lifestyles[intPosition] = objLifestyle;
				treLifestyles.SelectedNode.Text = objLifestyle.DisplayName;
				RefreshSelectedLifestyle();
				UpdateCharacterInfo();
			}
		}

		private void treLifestyles_ItemDrag(object sender, ItemDragEventArgs e)
		{
			try
			{
				if (treLifestyles.SelectedNode.Level != 1)
					return;
			}
			catch
			{
				return;
			}
			_intDragLevel = treLifestyles.SelectedNode.Level;
			DoDragDrop(e.Item, DragDropEffects.Move);
		}

		private void treLifestyles_DragEnter(object sender, DragEventArgs e)
		{
			e.Effect = DragDropEffects.Move;
		}

		private void treLifestyles_DragDrop(object sender, DragEventArgs e)
		{
			Point pt = ((TreeView)sender).PointToClient(new Point(e.X, e.Y));
			TreeNode nodDestination = ((TreeView)sender).GetNodeAt(pt);

			int intNewIndex = 0;
			try
			{
				intNewIndex = nodDestination.Index;
			}
			catch
			{
				intNewIndex = treLifestyles.Nodes[treLifestyles.Nodes.Count - 1].Nodes.Count;
				nodDestination = treLifestyles.Nodes[treLifestyles.Nodes.Count - 1];
			}

			_objController.MoveLifestyleNode(intNewIndex, nodDestination, treLifestyles);

			// Clear the background color for all Nodes.
			_objFunctions.ClearNodeBackground(treLifestyles, null);
			
			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void treLifestyles_DragOver(object sender, DragEventArgs e)
		{
			Point pt = ((TreeView)sender).PointToClient(new Point(e.X, e.Y));
			TreeNode objNode = ((TreeView)sender).GetNodeAt(pt);

			if (objNode == null)
				return;

			// Highlight the Node that we're currently dragging over, provided it is of the same level or higher.
			if (objNode.Level <= _intDragLevel)
				objNode.BackColor = SystemColors.ControlDark;

			// Clear the background colour for all other Nodes.
			_objFunctions.ClearNodeBackground(treLifestyles, objNode);
		}

		private void nudLifestyleMonths_ValueChanged(object sender, EventArgs e)
		{
			if (treLifestyles.SelectedNode.Level > 0)
			{
				_blnSkipRefresh = true;

				// Locate the selected Lifestyle.
				Lifestyle objLifestyle = _objFunctions.FindLifestyle(treLifestyles.SelectedNode.Tag.ToString(), _objCharacter.Lifestyles);
				if (objLifestyle == null)
					return;

				objLifestyle.Months = Convert.ToInt32(nudLifestyleMonths.Value);

				_blnSkipRefresh = false;
				UpdateCharacterInfo();
				RefreshSelectedLifestyle();

				_blnIsDirty = true;
				UpdateWindowTitle();
			}
		}

		private void treGear_AfterSelect(object sender, TreeViewEventArgs e)
		{
			RefreshSelectedGear();
			RefreshPasteStatus();
		}

		private void nudGearRating_ValueChanged(object sender, EventArgs e)
		{
			if (_blnSkipRefresh)
				return;

			if (treGear.SelectedNode.Level > 0)
			{
				Gear objGear = new Gear(_objCharacter);
				objGear = _objFunctions.FindGear(treGear.SelectedNode.Tag.ToString(), _objCharacter.Gear);

				objGear.Rating = Convert.ToInt32(nudGearRating.Value);
				if (objGear.Bonus != null)
				{
					_objImprovementManager.RemoveImprovements(Improvement.ImprovementSource.Gear, objGear.InternalId);
					if (objGear.Extra != "")
					{
						_objImprovementManager.ForcedValue = objGear.Extra;
						if (objGear.Extra.EndsWith(", Hacked"))
							_objImprovementManager.ForcedValue = objGear.Extra.Replace(", Hacked", string.Empty);
					}
					_objImprovementManager.CreateImprovements(Improvement.ImprovementSource.Gear, objGear.InternalId, objGear.Bonus, false, objGear.Rating, objGear.DisplayNameShort);
				}

				_objController.PopulateFocusList(treFoci);
				RefreshSelectedGear();
				UpdateCharacterInfo();

				_blnIsDirty = true;
				UpdateWindowTitle();
			}
		}

		private void nudGearQty_ValueChanged(object sender, EventArgs e)
		{
			if (_blnSkipRefresh)
				return;

			Gear objSelectedGear = new Gear(_objCharacter);

			// Attempt to locate the selected piece of Gear.
			try
			{
				if (treGear.SelectedNode.Level == 1)
					objSelectedGear = _objFunctions.FindGear(treGear.SelectedNode.Tag.ToString(), _objCharacter.Gear);

				objSelectedGear.Quantity = Convert.ToInt32(nudGearQty.Value);
				RefreshSelectedGear();
				UpdateCharacterInfo();

				_blnIsDirty = true;
				UpdateWindowTitle();
			}
			catch
			{
			}
		}

		private void chkArmorEquipped_CheckedChanged(object sender, EventArgs e)
		{
			if (_blnSkipRefresh)
				return;

			// Locate the selected Armor or Armor Mod.
			try
			{
				if (treArmor.SelectedNode.Level == 1)
				{
					Armor objArmor = _objFunctions.FindArmor(treArmor.SelectedNode.Tag.ToString(), _objCharacter.Armor);
					if (objArmor != null)
					{
						objArmor.Equipped = chkArmorEquipped.Checked;
						if (chkArmorEquipped.Checked)
						{
							// Add the Armor's Improevments to the character.
							if (objArmor.Bonus != null)
								_objImprovementManager.CreateImprovements(Improvement.ImprovementSource.Armor, objArmor.InternalId, objArmor.Bonus, false, 1, objArmor.DisplayNameShort);
							// Add the Improvements from any Armor Mods in the Armor.
							foreach (ArmorMod objMod in objArmor.ArmorMods)
							{
								if (objMod.Bonus != null && objMod.Equipped)
									_objImprovementManager.CreateImprovements(Improvement.ImprovementSource.ArmorMod, objMod.InternalId, objMod.Bonus, false, objMod.Rating, objMod.DisplayNameShort);
							}
							// Add the Improvements from any Gear in the Armor.
							foreach (Gear objGear in objArmor.Gear)
							{
								if (objGear.Bonus != null && objGear.Equipped)
									_objImprovementManager.CreateImprovements(Improvement.ImprovementSource.Gear, objGear.InternalId, objGear.Bonus, false, objGear.Rating, objGear.DisplayNameShort);
							}
						}
						else
						{
							// Remove any Improvements the Armor created.
							if (objArmor.Bonus != null)
								_objImprovementManager.RemoveImprovements(Improvement.ImprovementSource.Armor, objArmor.InternalId);
							// Remove any Improvements from any Armor Mods in the Armor.
							foreach (ArmorMod objMod in objArmor.ArmorMods)
							{
								if (objMod.Bonus != null)
									_objImprovementManager.RemoveImprovements(Improvement.ImprovementSource.ArmorMod, objMod.InternalId);
							}
							// Remove any Improvements from any Gear in the Armor.
							foreach (Gear objGear in objArmor.Gear)
							{
								if (objGear.Bonus != null)
									_objImprovementManager.RemoveImprovements(Improvement.ImprovementSource.Gear, objGear.InternalId);
							}
						}
					}
				}
				else if (treArmor.SelectedNode.Level > 1)
				{
					ArmorMod objMod = _objFunctions.FindArmorMod(treArmor.SelectedNode.Tag.ToString(), _objCharacter.Armor);
					if (objMod != null)
					{
						objMod.Equipped = chkArmorEquipped.Checked;
						if (chkArmorEquipped.Checked)
						{
							// Add the Mod's Improevments to the character.
							if (objMod.Bonus != null && objMod.Parent.Equipped)
								_objImprovementManager.CreateImprovements(Improvement.ImprovementSource.ArmorMod, objMod.InternalId, objMod.Bonus, false, objMod.Rating, objMod.DisplayNameShort);
						}
						else
						{
							// Remove any Improvements the Mod created.
							if (objMod.Bonus != null)
								_objImprovementManager.RemoveImprovements(Improvement.ImprovementSource.ArmorMod, objMod.InternalId);
						}
					}

					Armor objFoundArmor = new Armor(_objCharacter);
					Gear objGear = _objFunctions.FindArmorGear(treArmor.SelectedNode.Tag.ToString(), _objCharacter.Armor, out objFoundArmor);
					if (objGear != null)
					{
						objGear.Equipped = chkArmorEquipped.Checked;
						if (chkArmorEquipped.Checked)
						{
							// Add the Gear's Improevments to the character.
							if (objGear.Bonus != null && objFoundArmor.Equipped)
								_objImprovementManager.CreateImprovements(Improvement.ImprovementSource.Gear, objGear.InternalId, objGear.Bonus, false, objGear.Rating, objGear.DisplayNameShort);
						}
						else
						{
							// Remove any Improvements the Gear created.
							if (objGear.Bonus != null)
								_objImprovementManager.RemoveImprovements(Improvement.ImprovementSource.Gear, objGear.InternalId);
						}
					}
				}
				RefreshSelectedArmor();
				UpdateCharacterInfo();

				_blnIsDirty = true;
				UpdateWindowTitle();
			}
			catch
			{
			}
		}

		private void chkWeaponAccessoryInstalled_CheckedChanged(object sender, EventArgs e)
		{
			bool blnAccessory = false;

			// Locate the selected Weapon Accessory or Modification.
			WeaponAccessory objAccessory = _objFunctions.FindWeaponAccessory(treWeapons.SelectedNode.Tag.ToString(), _objCharacter.Weapons);
			if (objAccessory != null)
				blnAccessory = true;

			if (blnAccessory)
			{
				objAccessory.Installed = chkWeaponAccessoryInstalled.Checked;
			}
			else
			{
				// Locate the selected Weapon Modification.
				bool blnMod = false;
				WeaponMod objMod = _objFunctions.FindWeaponMod(treWeapons.SelectedNode.Tag.ToString(), _objCharacter.Weapons);
				if (objMod != null)
					blnMod = true;

				if (blnMod)
					objMod.Installed = chkWeaponAccessoryInstalled.Checked;
				else
				{
					// Determine if this is an Underbarrel Weapon.
					bool blnUnderbarrel = false;
					Weapon objWeapon = _objFunctions.FindWeapon(treWeapons.SelectedNode.Tag.ToString(), _objCharacter.Weapons);
					if (objWeapon != null)
					{
						objWeapon.Installed = chkWeaponAccessoryInstalled.Checked;
						blnUnderbarrel = true;
					}

					if (!blnUnderbarrel)
					{
						// Find the selected Gear.
						Gear objSelectedGear = new Gear(_objCharacter);

						try
						{
							objSelectedGear = _objFunctions.FindWeaponGear(treWeapons.SelectedNode.Tag.ToString(), _objCharacter.Weapons, out objAccessory);
							objSelectedGear.Equipped = chkWeaponAccessoryInstalled.Checked;

							ChangeGearEquippedStatus(objSelectedGear, chkWeaponAccessoryInstalled.Checked);

							UpdateCharacterInfo();
						}
						catch
						{
						}
					}
				}
			}

			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void chkIncludedInWeapon_CheckedChanged(object sender, EventArgs e)
		{
			bool blnAccessory = false;

			// Locate the selected Weapon Accessory or Modification.
			WeaponAccessory objAccessory = _objFunctions.FindWeaponAccessory(treWeapons.SelectedNode.Tag.ToString(), _objCharacter.Weapons);
			if (objAccessory != null)
				blnAccessory = true;

			if (blnAccessory)
			{
				objAccessory.IncludedInWeapon = chkIncludedInWeapon.Checked;
			}
			else
			{
				// Locate the selected Weapon Modification.
				WeaponMod objMod = _objFunctions.FindWeaponMod(treWeapons.SelectedNode.Tag.ToString(), _objCharacter.Weapons);
				if (objMod != null)
					objMod.IncludedInWeapon = chkIncludedInWeapon.Checked;
			}

			_blnIsDirty = true;
			UpdateWindowTitle();
			UpdateCharacterInfo();
		}

		private void treGear_ItemDrag(object sender, ItemDragEventArgs e)
		{
			try
			{
				if (e.Button == MouseButtons.Left)
				{
					if (treGear.SelectedNode.Level != 1 && treGear.SelectedNode.Level != 0)
						return;
					_objDragButton = MouseButtons.Left;
				}
				else
				{
					if (treGear.SelectedNode.Level == 0)
						return;
					_objDragButton = MouseButtons.Right;
				}

				// Do not allow the root element to be moved.
				if (treGear.SelectedNode.Tag.ToString() == "Node_SelectedGear")
					return;
			}
			catch
			{
				return;
			}
			_intDragLevel = treGear.SelectedNode.Level;
			DoDragDrop(e.Item, DragDropEffects.Move);
		}

		private void treGear_DragEnter(object sender, DragEventArgs e)
		{
			e.Effect = DragDropEffects.Move;
		}

		private void treGear_DragDrop(object sender, DragEventArgs e)
		{
			Point pt = ((TreeView)sender).PointToClient(new Point(e.X, e.Y));
			TreeNode nodDestination = ((TreeView)sender).GetNodeAt(pt);

			int intNewIndex = 0;
			try
			{
				intNewIndex = nodDestination.Index;
			}
			catch
			{
				intNewIndex = treGear.Nodes[treGear.Nodes.Count - 1].Nodes.Count;
				nodDestination = treGear.Nodes[treGear.Nodes.Count - 1];
			}

			// If the item was moved using the left mouse button, change the order of things.
			if (_objDragButton == MouseButtons.Left)
			{
				if (treGear.SelectedNode.Level == 1)
					_objController.MoveGearNode(intNewIndex, nodDestination, treGear);
				else
					_objController.MoveGearRoot(intNewIndex, nodDestination, treGear);
			}
			if (_objDragButton == MouseButtons.Right)
				_objController.MoveGearParent(intNewIndex, nodDestination, treGear, cmsGear);

			// Clear the background color for all Nodes.
			_objFunctions.ClearNodeBackground(treGear, null);

			_objDragButton = MouseButtons.None;

			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void treGear_DragOver(object sender, DragEventArgs e)
		{
			Point pt = ((TreeView)sender).PointToClient(new Point(e.X, e.Y));
			TreeNode objNode = ((TreeView)sender).GetNodeAt(pt);

			if (objNode == null)
				return;

			// Highlight the Node that we're currently dragging over, provided it is of the same level or higher.
			if (_objDragButton == MouseButtons.Left)
			{
				if (objNode.Level <= _intDragLevel)
					objNode.BackColor = SystemColors.ControlDark;
			}
			else
				objNode.BackColor = SystemColors.ControlDark;

			// Clear the background colour for all other Nodes.
			_objFunctions.ClearNodeBackground(treGear, objNode);
		}

		private void chkGearEquipped_CheckedChanged(object sender, EventArgs e)
		{
			if (_blnSkipRefresh)
				return;

			Gear objSelectedGear = new Gear(_objCharacter);

			// Attempt to locate the selected piece of Gear.
			try
			{
				objSelectedGear = _objFunctions.FindGear(treGear.SelectedNode.Tag.ToString(), _objCharacter.Gear);
				objSelectedGear.Equipped = chkGearEquipped.Checked;

				ChangeGearEquippedStatus(objSelectedGear, chkGearEquipped.Checked);

				RefreshSelectedGear();
				UpdateCharacterInfo();

				_blnIsDirty = true;
				UpdateWindowTitle();
			}
			catch
			{
			}
		}

		private void chkGearHomeNode_CheckedChanged(object sender, EventArgs e)
		{
			Commlink objGear = new Commlink(_objCharacter);
			objGear = (Commlink)_objFunctions.FindGear(treGear.SelectedNode.Tag.ToString(), _objCharacter.Gear);
			objGear.HomeNode = chkGearHomeNode.Checked;
			RefreshSelectedGear();
			UpdateCharacterInfo();

			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void chkArmorBlackMarketDiscount_CheckedChanged(object sender, EventArgs e)
		{
			if (_blnSkipRefresh)
				return;

			// Locate the selected Armor or Armor Mod.
			try
			{
				Armor objArmor = _objFunctions.FindArmor(treArmor.SelectedNode.Tag.ToString(), _objCharacter.Armor);
				if (objArmor != null)
					objArmor.DiscountCost = chkArmorBlackMarketDiscount.Checked;

				ArmorMod objMod = _objFunctions.FindArmorMod(treArmor.SelectedNode.Tag.ToString(), _objCharacter.Armor);
				if (objMod != null)
					objMod.DiscountCost = chkArmorBlackMarketDiscount.Checked;

				Gear objGear = _objFunctions.FindArmorGear(treArmor.SelectedNode.Tag.ToString(), _objCharacter.Armor, out objArmor);
				if (objGear != null)
					objGear.DiscountCost = chkArmorBlackMarketDiscount.Checked;

				RefreshSelectedArmor();
				UpdateCharacterInfo();

				_blnIsDirty = true;
				UpdateWindowTitle();
			}
			catch
			{
			}
		}

		private void chkGearBlackMarketDiscount_CheckedChanged(object sender, EventArgs e)
		{
			if (_blnSkipRefresh)
				return;

			// Located the selected Gear.
			try
			{
				Gear objGear = _objFunctions.FindGear(treGear.SelectedNode.Tag.ToString(), _objCharacter.Gear);
				if (objGear != null)
					objGear.DiscountCost = chkGearBlackMarketDiscount.Checked;

				RefreshSelectedGear();
				UpdateCharacterInfo();

				_blnIsDirty = true;
				UpdateWindowTitle();
			}
			catch
			{
			}
		}

		private void chkWeaponBlackMarketDiscount_CheckedChanged(object sender, EventArgs e)
		{
			if (_blnSkipRefresh)
				return;

			// Locate the selected Weapon, Weapon Accessory, Weapon Mod, or Gear.
			try
			{
				Weapon objWeapon = _objFunctions.FindWeapon(treWeapons.SelectedNode.Tag.ToString(), _objCharacter.Weapons);
				if (objWeapon != null)
					objWeapon.DiscountCost = chkWeaponBlackMarketDiscount.Checked;

				WeaponAccessory objAccessory = _objFunctions.FindWeaponAccessory(treWeapons.SelectedNode.Tag.ToString(), _objCharacter.Weapons);
				if (objAccessory != null)
					objAccessory.DiscountCost = chkWeaponBlackMarketDiscount.Checked;

				WeaponMod objMod = _objFunctions.FindWeaponMod(treWeapons.SelectedNode.Tag.ToString(), _objCharacter.Weapons);
				if (objMod != null)
					objMod.DiscountCost = chkWeaponBlackMarketDiscount.Checked;

				Gear objGear = _objFunctions.FindWeaponGear(treWeapons.SelectedNode.Tag.ToString(), _objCharacter.Weapons, out objAccessory);
				if (objGear != null)
					objGear.DiscountCost = chkWeaponBlackMarketDiscount.Checked;

				RefreshSelectedWeapon();
				UpdateCharacterInfo();

				_blnIsDirty = true;
				UpdateWindowTitle();
			}
			catch
			{
			}
		}

		private void chkIncludedInArmor_CheckedChanged(object sender, EventArgs e)
		{
			if (_blnSkipRefresh)
				return;

			// Locate the selected Armor Modification.
			ArmorMod objMod = _objFunctions.FindArmorMod(treArmor.SelectedNode.Tag.ToString(), _objCharacter.Armor);
			if (objMod != null)
				objMod.IncludedInArmor = chkIncludedInArmor.Checked;

			_blnIsDirty = true;
			UpdateWindowTitle();
			UpdateCharacterInfo();
		}

		private void chkCommlinks_CheckedChanged(object sender, EventArgs e)
		{
			PopulateGearList();
		}

		private void chkActiveCommlink_CheckedChanged(object sender, EventArgs e)
		{
			if (_blnSkipRefresh)
				return;

			Gear objSelectedGear = new Gear(_objCharacter);

			// Attempt to locate the selected piece of Gear.
			try
			{
				objSelectedGear = _objFunctions.FindGear(treGear.SelectedNode.Tag.ToString(), _objCharacter.Gear);

				if (objSelectedGear.GetType() != typeof(Commlink))
					return;

				Commlink objCommlink = (Commlink)objSelectedGear;
				objCommlink.IsActive = chkActiveCommlink.Checked;

				ChangeActiveCommlink(objCommlink);

				RefreshSelectedGear();
				UpdateCharacterInfo();

				_blnIsDirty = true;
				UpdateWindowTitle();
			}
			catch
			{
			}
		}
		#endregion

		#region Additional Vehicle Tab Control Events
		private void treVehicles_AfterSelect(object sender, TreeViewEventArgs e)
		{
			RefreshSelectedVehicle();
			RefreshPasteStatus();
		}

		private void treVehicles_ItemDrag(object sender, ItemDragEventArgs e)
		{
			try
			{
				if (treVehicles.SelectedNode.Level != 1)
				{
					// Determine if this is a piece of Gear. If not, don't let the user drag the Node.
					Vehicle objVehicle = new Vehicle(_objCharacter);
					Gear objGear = _objFunctions.FindVehicleGear(treVehicles.SelectedNode.Tag.ToString(), _objCharacter.Vehicles, out objVehicle);
					if (objGear != null)
					{
						_objDragButton = e.Button;
						_blnDraggingGear = true;
					}
					else
					{
						return;
					}
				}
			}
			catch
			{
				return;
			}
			_intDragLevel = treVehicles.SelectedNode.Level;
			DoDragDrop(e.Item, DragDropEffects.Move);
		}

		private void treVehicles_DragEnter(object sender, DragEventArgs e)
		{
			e.Effect = DragDropEffects.Move;
		}

		private void treVehicles_DragDrop(object sender, DragEventArgs e)
		{
			Point pt = ((TreeView)sender).PointToClient(new Point(e.X, e.Y));
			TreeNode nodDestination = ((TreeView)sender).GetNodeAt(pt);

			int intNewIndex = 0;
			try
			{
				intNewIndex = nodDestination.Index;
			}
			catch
			{
				intNewIndex = treVehicles.Nodes[treVehicles.Nodes.Count - 1].Nodes.Count;
				nodDestination = treVehicles.Nodes[treVehicles.Nodes.Count - 1];
			}

			if (!_blnDraggingGear)
				_objController.MoveVehicleNode(intNewIndex, nodDestination, treVehicles);
			else
			{
				if (_objDragButton == MouseButtons.Left)
					return;
				else
					_objController.MoveVehicleGearParent(intNewIndex, nodDestination, treVehicles, cmsVehicleGear);
			}

			// Clear the background color for all Nodes.
			_objFunctions.ClearNodeBackground(treVehicles, null);

			_blnDraggingGear = false;

			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void treVehicles_DragOver(object sender, DragEventArgs e)
		{
			Point pt = ((TreeView)sender).PointToClient(new Point(e.X, e.Y));
			TreeNode objNode = ((TreeView)sender).GetNodeAt(pt);

			if (objNode == null)
				return;

			// Highlight the Node that we're currently dragging over, provided it is of the same level or higher.
			if (_objDragButton == MouseButtons.Left)
			{
				if (objNode.Level <= _intDragLevel)
					objNode.BackColor = SystemColors.ControlDark;
			}
			else
				objNode.BackColor = SystemColors.ControlDark;

			// Clear the background colour for all other Nodes.
			_objFunctions.ClearNodeBackground(treVehicles, objNode);
		}

		private void nudVehicleRating_ValueChanged(object sender, EventArgs e)
		{
			if (_blnSkipRefresh)
				return;

			if (treVehicles.SelectedNode.Level == 2)
			{
				bool blnFound = false;

				// Locate the currently selected VehicleMod.
				Vehicle objFoundVehicle = new Vehicle(_objCharacter);
				VehicleMod objMod = _objFunctions.FindVehicleMod(treVehicles.SelectedNode.Tag.ToString(), _objCharacter.Vehicles, out objFoundVehicle);
				if (objMod != null)
					blnFound = true;

				if (blnFound)
				{
					objMod.Rating = Convert.ToInt32(nudVehicleRating.Value);
					treVehicles.SelectedNode.Text = objMod.DisplayName;
					UpdateCharacterInfo();
					RefreshSelectedVehicle();
				}
				else
				{
					// Locate the currently selected Vehicle Gear,.
					Gear objGear = _objFunctions.FindVehicleGear(treVehicles.SelectedNode.Tag.ToString(), _objCharacter.Vehicles, out objFoundVehicle);

					objGear.Rating = Convert.ToInt32(nudVehicleRating.Value);
					treVehicles.SelectedNode.Text = objGear.DisplayName;
					UpdateCharacterInfo();
					RefreshSelectedVehicle();
				}
			}
			else if (treVehicles.SelectedNode.Level > 2)
			{
				bool blnGear = false;
				Vehicle objFoundVehicle = new Vehicle(_objCharacter);
				Gear objGear = new Gear(_objCharacter);
				// Locate the currently selected Vehicle Sensor Plugin.
				objGear = _objFunctions.FindVehicleGear(treVehicles.SelectedNode.Tag.ToString(), _objCharacter.Vehicles, out objFoundVehicle);
				if (objGear != null)
					blnGear = true;

				if (blnGear)
				{
					objGear.Rating = Convert.ToInt32(nudVehicleRating.Value);
					treVehicles.SelectedNode.Text = objGear.DisplayName;
					UpdateCharacterInfo();
					RefreshSelectedVehicle();
				}
				else
				{
					// See if this is a piece of Cyberware.
					bool blnCyberware = false;
					Cyberware objCyberware = _objFunctions.FindVehicleCyberware(treVehicles.SelectedNode.Tag.ToString(), _objCharacter.Vehicles);
					if (objCyberware != null)
						blnCyberware = true;

					if (blnCyberware)
					{
						objCyberware.Rating = Convert.ToInt32(nudVehicleRating.Value);
						treVehicles.SelectedNode.Text = objCyberware.DisplayName;
					}
				}
				UpdateCharacterInfo();
				RefreshSelectedVehicle();
			}

			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void chkVehicleWeaponAccessoryInstalled_CheckedChanged(object sender, EventArgs e)
		{
			bool blnAccessory = false;

			// Locate the the Selected Vehicle Weapon Accessory of Modification.
			WeaponAccessory objAccessory = _objFunctions.FindVehicleWeaponAccessory(treVehicles.SelectedNode.Tag.ToString(), _objCharacter.Vehicles);
			if (objAccessory != null)
				blnAccessory = true;

			if (blnAccessory)
				objAccessory.Installed = chkVehicleWeaponAccessoryInstalled.Checked;
			else
			{
				bool blnWeaponMod = false;
				// Locate the selected Vehicle Weapon Modification.
				WeaponMod objMod = _objFunctions.FindVehicleWeaponMod(treVehicles.SelectedNode.Tag.ToString(), _objCharacter.Vehicles);
				if (objMod != null)
					blnWeaponMod = true;

				if (blnWeaponMod)
					objMod.Installed = chkVehicleWeaponAccessoryInstalled.Checked;
				else
				{
					// If this isn't a Weapon Mod, then it must be a Vehicle Mod.
					Vehicle objFoundVehicle = new Vehicle(_objCharacter);
					VehicleMod objVehicleMod = _objFunctions.FindVehicleMod(treVehicles.SelectedNode.Tag.ToString(), _objCharacter.Vehicles, out objFoundVehicle);
					if (objVehicleMod != null)
						objVehicleMod.Installed = chkVehicleWeaponAccessoryInstalled.Checked;
					else
					{
						// If everything else has failed, we're left with a Vehicle Weapon.
						Weapon objWeapon = _objFunctions.FindVehicleWeapon(treVehicles.SelectedNode.Tag.ToString(), _objCharacter.Vehicles, out objFoundVehicle);
						objWeapon.Installed = chkVehicleWeaponAccessoryInstalled.Checked;
					}
				}
			}

			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void nudVehicleGearQty_ValueChanged(object sender, EventArgs e)
		{
			if (_blnSkipRefresh)
				return;

			Vehicle objFoundVehicle = new Vehicle(_objCharacter);
			Gear objGear = _objFunctions.FindVehicleGear(treVehicles.SelectedNode.Tag.ToString(), _objCharacter.Vehicles, out objFoundVehicle);

			objGear.Quantity = Convert.ToInt32(nudVehicleGearQty.Value);
			treVehicles.SelectedNode.Text = objGear.DisplayName;
			UpdateCharacterInfo();
			RefreshSelectedVehicle();

			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void chkVehicleHomeNode_CheckedChanged(object sender, EventArgs e)
		{
			if (treVehicles.SelectedNode.Level == 1)
			{
				Vehicle objVehicle = _objFunctions.FindVehicle(treVehicles.SelectedNode.Tag.ToString(), _objCharacter.Vehicles);
				if (objVehicle == null)
					return;

				objVehicle.HomeNode = chkVehicleHomeNode.Checked;
			}
			else
			{
				Commlink objGear = new Commlink(_objCharacter);
				Vehicle objSelectedVehicle = new Vehicle(_objCharacter);
				objGear = (Commlink)_objFunctions.FindVehicleGear(treVehicles.SelectedNode.Tag.ToString(), _objCharacter.Vehicles, out objSelectedVehicle);
				objGear.HomeNode = chkVehicleHomeNode.Checked;
			}

			RefreshSelectedVehicle();
			UpdateCharacterInfo();

			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void chkVehicleBlackMarketDiscount_CheckedChanged(object sender, EventArgs e)
		{
			if (_blnSkipRefresh)
				return;

			// Locate the selected Vehicle piece.
			try
			{
				Vehicle objVehicle = _objFunctions.FindVehicle(treVehicles.SelectedNode.Tag.ToString(), _objCharacter.Vehicles);
				if (objVehicle != null)
					objVehicle.DiscountCost = chkVehicleBlackMarketDiscount.Checked;

				VehicleMod objVehicleMod = _objFunctions.FindVehicleMod(treVehicles.SelectedNode.Tag.ToString(), _objCharacter.Vehicles, out objVehicle);
				if (objVehicleMod != null)
					objVehicleMod.DiscountCost = chkVehicleBlackMarketDiscount.Checked;

				Gear objVehicleGear = _objFunctions.FindVehicleGear(treVehicles.SelectedNode.Tag.ToString(), _objCharacter.Vehicles, out objVehicle);
				if (objVehicleGear != null)
					objVehicleGear.DiscountCost = chkVehicleBlackMarketDiscount.Checked;

				Weapon objWeapon = _objFunctions.FindVehicleWeapon(treVehicles.SelectedNode.Tag.ToString(), _objCharacter.Vehicles, out objVehicle);
				if (objWeapon != null)
					objWeapon.DiscountCost = chkVehicleBlackMarketDiscount.Checked;

				WeaponAccessory objWeaponAccessory = _objFunctions.FindVehicleWeaponAccessory(treVehicles.SelectedNode.Tag.ToString(), _objCharacter.Vehicles);
				if (objWeaponAccessory != null)
					objWeaponAccessory.DiscountCost = chkVehicleBlackMarketDiscount.Checked;

				WeaponMod objWeaponMod = _objFunctions.FindVehicleWeaponMod(treVehicles.SelectedNode.Tag.ToString(), _objCharacter.Vehicles);
				if (objWeaponMod != null)
					objWeaponMod.DiscountCost = chkVehicleBlackMarketDiscount.Checked;

				Cyberware objCyberware = _objFunctions.FindVehicleCyberware(treVehicles.SelectedNode.Tag.ToString(), _objCharacter.Vehicles);
				if (objCyberware != null)
					objCyberware.DiscountCost = chkVehicleBlackMarketDiscount.Checked;

				RefreshSelectedVehicle();
				UpdateCharacterInfo();

				_blnIsDirty = true;
				UpdateWindowTitle();
			}
			catch
			{
			}
		}
		#endregion

		#region Additional Spells and Spirits Tab Control Events
		private void treSpells_AfterSelect(object sender, TreeViewEventArgs e)
		{
			if (treSpells.SelectedNode.Level > 0)
			{
				_blnSkipRefresh = true;

				// Locate the selected Spell.
				Spell objSpell = _objFunctions.FindSpell(e.Node.Tag.ToString(), _objCharacter.Spells);

				lblSpellDescriptors.Text = objSpell.DisplayDescriptors;
				lblSpellCategory.Text = objSpell.DisplayCategory;
				lblSpellType.Text = objSpell.DisplayType;
				lblSpellRange.Text = objSpell.DisplayRange;
				lblSpellDamage.Text = objSpell.DisplayDamage;
				lblSpellDuration.Text = objSpell.DisplayDuration;
				lblSpellDV.Text = objSpell.DisplayDV;
				string strBook = _objOptions.LanguageBookShort(objSpell.Source);
				string strPage = objSpell.Page;
				lblSpellSource.Text = strBook + " " + strPage;
				tipTooltip.SetToolTip(lblSpellSource, _objOptions.LanguageBookLong(objSpell.Source) + " " + LanguageManager.Instance.GetString("String_Page") + " " + objSpell.Page);

				// Determine the size of the Spellcasting Dice Pool.
				string strPoolTip = "";
				int intDicePool = 0;
				foreach (Skill objSkill in _objCharacter.Skills)
				{
					if (objSkill.Name == "Spellcasting")
					{
						intDicePool = objSkill.TotalRating;
						strPoolTip = objSkill.DisplayName + " (" + objSkill.TotalRating.ToString() + ")";
						// Add any Specialization bonus if applicable.
						if (objSkill.Specialization == objSpell.Category)
						{
							intDicePool += 2;
							strPoolTip += " + " + objSpell.Category + " (2)";
						}
					}
				}
				// Include any Improvements to the Spell Category.
				if (_objImprovementManager.ValueOf(Improvement.ImprovementType.SpellCategory, false, objSpell.Category) != 0)
					strPoolTip += " + " + objSpell.DisplayCategory + " (" + _objImprovementManager.ValueOf(Improvement.ImprovementType.SpellCategory, false, objSpell.Category).ToString() + ")";
				intDicePool += _objImprovementManager.ValueOf(Improvement.ImprovementType.SpellCategory, false, objSpell.Category);
				lblSpellDicePool.Text = intDicePool.ToString();
				tipTooltip.SetToolTip(lblSpellDicePool, strPoolTip);

				// Build the DV tooltip.
				string strTip = LanguageManager.Instance.GetString("Tip_SpellDrainBase");
				int intMAG = _objCharacter.MAG.TotalValue;

				if (_objCharacter.AdeptEnabled && _objCharacter.MagicianEnabled)
				{
					if (_objOptions.SpiritForceBasedOnTotalMAG)
						intMAG = _objCharacter.MAG.TotalValue;
					else
						intMAG = _objCharacter.MAGMagician;
				}

				XmlDocument objXmlDocument = new XmlDocument();
				XPathNavigator nav = objXmlDocument.CreateNavigator();
				XPathExpression xprDV;

				try
				{
					for (int i = 1; i <= intMAG * 2; i++)
					{
						// Calculate the Spell's Drain for the current Force.
						xprDV = nav.Compile(objSpell.DV.Replace("F", i.ToString()).Replace("/", " div "));
						decimal decDV = Convert.ToDecimal(nav.Evaluate(xprDV).ToString());
						decDV = Math.Floor(decDV);
						int intDV = Convert.ToInt32(decDV);
						// Drain cannot be lower than 1.
						if (intDV < 1)
							intDV = 1;
						strTip += "\n   " + LanguageManager.Instance.GetString("String_Force") + " " + i.ToString() + ": " + intDV.ToString();
					}
				}
				catch
				{
					strTip = LanguageManager.Instance.GetString("Tip_SpellDrainSeeDescription");
				}
				tipTooltip.SetToolTip(lblSpellDV, strTip);

				// Update the Drain Attribute Value.
				if (_objCharacter.MAGEnabled && lblDrainAttributes.Text != "")
				{
					try
					{
						strTip = "";
						objXmlDocument = new XmlDocument();
						nav = objXmlDocument.CreateNavigator();
						string strDrain = lblDrainAttributes.Text.Replace(LanguageManager.Instance.GetString("String_AttributeBODShort"), _objCharacter.BOD.TotalValue.ToString());
						strDrain = strDrain.Replace(LanguageManager.Instance.GetString("String_AttributeAGIShort"), _objCharacter.AGI.TotalValue.ToString());
						strDrain = strDrain.Replace(LanguageManager.Instance.GetString("String_AttributeREAShort"), _objCharacter.REA.TotalValue.ToString());
						strDrain = strDrain.Replace(LanguageManager.Instance.GetString("String_AttributeSTRShort"), _objCharacter.STR.TotalValue.ToString());
						strDrain = strDrain.Replace(LanguageManager.Instance.GetString("String_AttributeCHAShort"), _objCharacter.CHA.TotalValue.ToString());
						strDrain = strDrain.Replace(LanguageManager.Instance.GetString("String_AttributeINTShort"), _objCharacter.INT.TotalValue.ToString());
						strDrain = strDrain.Replace(LanguageManager.Instance.GetString("String_AttributeLOGShort"), _objCharacter.LOG.TotalValue.ToString());
						strDrain = strDrain.Replace(LanguageManager.Instance.GetString("String_AttributeWILShort"), _objCharacter.WIL.TotalValue.ToString());
						strDrain = strDrain.Replace(LanguageManager.Instance.GetString("String_AttributeMAGShort"), _objCharacter.MAG.TotalValue.ToString());
						XPathExpression xprDrain = nav.Compile(strDrain);
						int intDrain = Convert.ToInt32(nav.Evaluate(xprDrain).ToString());
						intDrain += _objImprovementManager.ValueOf(Improvement.ImprovementType.DrainResistance);

						strTip = lblDrainAttributes.Text.Replace(LanguageManager.Instance.GetString("String_AttributeBODShort"), LanguageManager.Instance.GetString("String_AttributeBODShort") + " (" + _objCharacter.BOD.TotalValue.ToString() + ")");
						strTip = strTip.Replace(LanguageManager.Instance.GetString("String_AttributeAGIShort"), LanguageManager.Instance.GetString("String_AttributeAGIShort") + " (" + _objCharacter.AGI.TotalValue.ToString() + ")");
						strTip = strTip.Replace(LanguageManager.Instance.GetString("String_AttributeREAShort"), LanguageManager.Instance.GetString("String_AttributeREAShort") + " (" + _objCharacter.REA.TotalValue.ToString() + ")");
						strTip = strTip.Replace(LanguageManager.Instance.GetString("String_AttributeSTRShort"), LanguageManager.Instance.GetString("String_AttributeSTRShort") + " (" + _objCharacter.STR.TotalValue.ToString() + ")");
						strTip = strTip.Replace(LanguageManager.Instance.GetString("String_AttributeCHAShort"), LanguageManager.Instance.GetString("String_AttributeCHAShort") + " (" + _objCharacter.CHA.TotalValue.ToString() + ")");
						strTip = strTip.Replace(LanguageManager.Instance.GetString("String_AttributeINTShort"), LanguageManager.Instance.GetString("String_AttributeINTShort") + " (" + _objCharacter.INT.TotalValue.ToString() + ")");
						strTip = strTip.Replace(LanguageManager.Instance.GetString("String_AttributeLOGShort"), LanguageManager.Instance.GetString("String_AttributeLOGShort") + " (" + _objCharacter.LOG.TotalValue.ToString() + ")");
						strTip = strTip.Replace(LanguageManager.Instance.GetString("String_AttributeWILShort"), LanguageManager.Instance.GetString("String_AttributeWILShort") + " (" + _objCharacter.WIL.TotalValue.ToString() + ")");
						strTip = strTip.Replace(LanguageManager.Instance.GetString("String_AttributeMAGShort"), LanguageManager.Instance.GetString("String_AttributeMAGShort") + " (" + _objCharacter.CHA.TotalValue.ToString() + ")");
						
						if (_objImprovementManager.ValueOf(Improvement.ImprovementType.DrainResistance) != 0)
							strTip += " + " + LanguageManager.Instance.GetString("Tip_Skill_DicePoolModifiers") + " (" + _objImprovementManager.ValueOf(Improvement.ImprovementType.DrainResistance).ToString() + ")";
						if (objSpell.Limited)
						{
							intDrain += 2;
							strTip += " + " + LanguageManager.Instance.GetString("String_SpellLimited") + " (2)";
						}
						lblDrainAttributesValue.Text = intDrain.ToString();
						tipTooltip.SetToolTip(lblDrainAttributesValue, strTip);
					}
					catch
					{
					}
				}

				_blnSkipRefresh = false;
			}
			else
			{
				lblSpellDescriptors.Text = "";
				lblSpellCategory.Text = "";
				lblSpellType.Text = "";
				lblSpellRange.Text = "";
				lblSpellDamage.Text = "";
				lblSpellDuration.Text = "";
				lblSpellDV.Text = "";
				lblSpellSource.Text = "";
				lblSpellDicePool.Text = "";
				tipTooltip.SetToolTip(lblSpellSource, null);
				tipTooltip.SetToolTip(lblSpellDV, null);
			}
		}

		private void treFoci_AfterCheck(object sender, TreeViewEventArgs e)
		{
			if (e.Node.Checked)
			{
				// Locate the Focus that is being touched.
				Gear objSelectedFocus = new Gear(_objCharacter);
				objSelectedFocus = _objFunctions.FindGear(e.Node.Tag.ToString(), _objCharacter.Gear);

				if (objSelectedFocus != null)
				{
					Focus objFocus = new Focus();
					objFocus.Name = e.Node.Text;
					objFocus.Rating = objSelectedFocus.Rating;
					objFocus.GearId = e.Node.Tag.ToString();
					_objCharacter.Foci.Add(objFocus);

					// Mark the Gear and Bonded and create an Improvements.
					objSelectedFocus.Bonded = true;
					if (objSelectedFocus.Equipped)
					{
						if (objSelectedFocus.Bonus != null)
						{
							if (objSelectedFocus.Extra != "")
								_objImprovementManager.ForcedValue = objSelectedFocus.Extra;
							_objImprovementManager.CreateImprovements(Improvement.ImprovementSource.Gear, objSelectedFocus.InternalId, objSelectedFocus.Bonus, false, objSelectedFocus.Rating, objSelectedFocus.DisplayNameShort);
						}
					}
				}
				else
				{
					// This is a Stacked Focus.
					StackedFocus objStack = new StackedFocus(_objCharacter);
					foreach (StackedFocus objCharacterFocus in _objCharacter.StackedFoci)
					{
						if (e.Node.Tag.ToString() == objCharacterFocus.InternalId)
						{
							objStack = objCharacterFocus;
							break;
						}
					}

					objStack.Bonded = true;
					Gear objStackGear = _objFunctions.FindGear(objStack.GearId, _objCharacter.Gear);
					if (objStackGear.Equipped)
					{
						foreach (Gear objGear in objStack.Gear)
						{
							if (objGear.Bonus != null)
							{
								if (objGear.Extra != "")
									_objImprovementManager.ForcedValue = objGear.Extra;
								_objImprovementManager.CreateImprovements(Improvement.ImprovementSource.StackedFocus, objStack.InternalId, objGear.Bonus, false, objGear.Rating, objGear.DisplayNameShort);
							}
						}
					}
				}
			}
			else
			{
				Focus objFocus = new Focus();
				foreach (Focus objCharacterFocus in _objCharacter.Foci)
				{
					if (objCharacterFocus.GearId == e.Node.Tag.ToString())
					{
						objFocus = objCharacterFocus;
						break;
					}
				}

				// Mark the Gear as not Bonded and remove any Improvements.
				Gear objGear = new Gear(_objCharacter);
				objGear = _objFunctions.FindGear(objFocus.GearId, _objCharacter.Gear);

				if (objGear != null)
				{
					objGear.Bonded = false;
					_objImprovementManager.RemoveImprovements(Improvement.ImprovementSource.Gear, objGear.InternalId);
					_objCharacter.Foci.Remove(objFocus);
				}
				else
				{
					// This is a Stacked Focus.
					StackedFocus objStack = new StackedFocus(_objCharacter);
					foreach (StackedFocus objCharacterFocus in _objCharacter.StackedFoci)
					{
						if (e.Node.Tag.ToString() == objCharacterFocus.InternalId)
						{
							objStack = objCharacterFocus;
							break;
						}
					}

					objStack.Bonded = false;
					foreach (Gear objFocusGear in objStack.Gear)
					{
						_objImprovementManager.RemoveImprovements(Improvement.ImprovementSource.StackedFocus, objStack.InternalId);
					}
				}
			}

			UpdateCharacterInfo();

			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void treFoci_BeforeCheck(object sender, TreeViewCancelEventArgs e)
		{
			// Don't bother to do anything since a node is being unchecked.
			if (e.Node.Checked)
				return;

			// Locate the Focus that is being touched.
			Gear objSelectedFocus = new Gear(_objCharacter);
			objSelectedFocus = _objFunctions.FindGear(e.Node.Tag.ToString(), _objCharacter.Gear);

			// Set the Focus count to 1 and get its current Rating (Force). This number isn't used in the following loops because it isn't yet checked or unchecked.
			int intFociCount = 1;
			int intFociTotal = 0;

			if (objSelectedFocus != null)
				intFociTotal = objSelectedFocus.Rating;
			else
			{
				// This is a Stacked Focus.
				StackedFocus objStack = new StackedFocus(_objCharacter);
				foreach (StackedFocus objCharacterFocus in _objCharacter.StackedFoci)
				{
					if (e.Node.Tag.ToString() == objCharacterFocus.InternalId)
					{
						objStack = objCharacterFocus;
						break;
					}
				}
				intFociTotal = objStack.TotalForce;
			}

			// Run through the list of items. Count the number of Foci the character would have bonded including this one, plus the total Force of all checked Foci.
			foreach (TreeNode objNode in treFoci.Nodes)
			{
				if (objNode.Checked)
				{
					intFociCount++;
					foreach (Gear objCharacterFocus in _objCharacter.Gear)
					{
						if (objNode.Tag.ToString() == objCharacterFocus.InternalId)
						{
							intFociTotal += objCharacterFocus.Rating;
							break;
						}
					}

					foreach (StackedFocus objStack in _objCharacter.StackedFoci)
					{
						if (objNode.Tag.ToString() == objStack.InternalId)
						{
							if (objStack.Bonded)
							{
								intFociTotal += objStack.TotalForce;
								break;
							}
						}
					}
				}
			}

			if (intFociTotal > _objCharacter.MAG.TotalValue * 5 && !_objCharacter.IgnoreRules)
			{
				MessageBox.Show(LanguageManager.Instance.GetString("Message_FocusMaximumForce"), LanguageManager.Instance.GetString("MessageTitle_FocusMaximum"), MessageBoxButtons.OK, MessageBoxIcon.Information);
				e.Cancel = true;
				return;
			}

			if (intFociCount > _objCharacter.MAG.TotalValue && !_objCharacter.IgnoreRules)
			{
				MessageBox.Show(LanguageManager.Instance.GetString("Message_FocusMaximumNumber"), LanguageManager.Instance.GetString("MessageTitle_FocusMaximum"), MessageBoxButtons.OK, MessageBoxIcon.Information);
				e.Cancel = true;
			}
		}

		private void nudArmorRating_ValueChanged(object sender, EventArgs e)
		{
			if (_blnSkipRefresh)
				return;

			// Locate the selected ArmorMod.
			bool blnIsMod = false;
			ArmorMod objMod = _objFunctions.FindArmorMod(treArmor.SelectedNode.Tag.ToString(), _objCharacter.Armor);
			if (objMod != null)
				blnIsMod = true;

			if (blnIsMod)
			{
				objMod.Rating = Convert.ToInt32(nudArmorRating.Value);
				treArmor.SelectedNode.Text = objMod.DisplayName;

				// See if a Bonus node exists.
				if (objMod.Bonus != null)
				{
					// If the Bonus contains "Rating", remove the existing Improvements and create new ones.
					if (objMod.Bonus.InnerXml.Contains("Rating"))
					{
						_objImprovementManager.RemoveImprovements(Improvement.ImprovementSource.ArmorMod, objMod.InternalId);
						_objImprovementManager.CreateImprovements(Improvement.ImprovementSource.ArmorMod, objMod.InternalId, objMod.Bonus, false, objMod.Rating, objMod.DisplayNameShort);
					}
				}
			}
			else
			{
				Armor objSelectedArmor = new Armor(_objCharacter);
				Gear objGear = _objFunctions.FindArmorGear(treArmor.SelectedNode.Tag.ToString(), _objCharacter.Armor, out objSelectedArmor);

				objGear.Rating = Convert.ToInt32(nudArmorRating.Value);
				treArmor.SelectedNode.Text = objGear.DisplayName;

				// See if a Bonus node exists.
				if (objGear.Bonus != null)
				{
					// If the Bonus contains "Rating", remove the existing Improvements and create new ones.
					if (objGear.Bonus.InnerXml.Contains("Rating"))
					{
						_objImprovementManager.RemoveImprovements(Improvement.ImprovementSource.Gear, objGear.InternalId);
						_objImprovementManager.CreateImprovements(Improvement.ImprovementSource.Gear, objGear.InternalId, objGear.Bonus, false, objGear.Rating, objGear.DisplayNameShort);
					}
				}
			}

			RefreshSelectedArmor();
			UpdateCharacterInfo();
			CalculateNuyen();

			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void cboTradition_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (_blnLoading || cboTradition.SelectedValue.ToString() == string.Empty)
				return;

			XmlDocument objXmlDocument = XmlManager.Instance.Load("traditions.xml");

			XmlNode objXmlTradition = objXmlDocument.SelectSingleNode("/chummer/traditions/tradition[name = \"" + cboTradition.SelectedValue + "\"]");
			lblDrainAttributes.Text = objXmlTradition["drain"].InnerText;
			lblDrainAttributes.Text = lblDrainAttributes.Text.Replace("BOD", LanguageManager.Instance.GetString("String_AttributeBODShort"));
			lblDrainAttributes.Text = lblDrainAttributes.Text.Replace("AGI", LanguageManager.Instance.GetString("String_AttributeAGIShort"));
			lblDrainAttributes.Text = lblDrainAttributes.Text.Replace("REA", LanguageManager.Instance.GetString("String_AttributeREAShort"));
			lblDrainAttributes.Text = lblDrainAttributes.Text.Replace("STR", LanguageManager.Instance.GetString("String_AttributeSTRShort"));
			lblDrainAttributes.Text = lblDrainAttributes.Text.Replace("CHA", LanguageManager.Instance.GetString("String_AttributeCHAShort"));
			lblDrainAttributes.Text = lblDrainAttributes.Text.Replace("INT", LanguageManager.Instance.GetString("String_AttributeINTShort"));
			lblDrainAttributes.Text = lblDrainAttributes.Text.Replace("LOG", LanguageManager.Instance.GetString("String_AttributeLOGShort"));
			lblDrainAttributes.Text = lblDrainAttributes.Text.Replace("WIL", LanguageManager.Instance.GetString("String_AttributeWILShort"));
			lblDrainAttributes.Text = lblDrainAttributes.Text.Replace("MAG", LanguageManager.Instance.GetString("String_AttributeMAGShort"));
			_objCharacter.MagicTradition = cboTradition.SelectedValue.ToString();

			foreach (SpiritControl objSpiritControl in panSpirits.Controls)
				objSpiritControl.RebuildSpiritList(cboTradition.SelectedValue.ToString());

			UpdateCharacterInfo();

			_blnIsDirty = true;
			UpdateWindowTitle();
		}
		#endregion

		#region Additional Sprites and Complex Forms Tab Control Events
		private void treComplexForms_AfterSelect(object sender, TreeViewEventArgs e)
		{
			try
			{
				if (treComplexForms.SelectedNode.Level == 1)
				{
					// Locate the Program that is selected in the tree.
					TechProgram objProgram = _objFunctions.FindTechProgram(treComplexForms.SelectedNode.Tag.ToString(), _objCharacter.TechPrograms);

					_blnSkipRefresh = true;
					nudComplexFormRating.Enabled = true;
					if (objProgram.MaxRating == 0)
						nudComplexFormRating.Maximum = _objCharacter.RES.TotalValue;
					else
						nudComplexFormRating.Maximum = Math.Min(objProgram.MaxRating, _objCharacter.RES.TotalValue);
					nudComplexFormRating.Value = objProgram.Rating;
					_blnSkipRefresh = false;
					lblComplexFormSkill.Text = objProgram.DisplaySkill;
					string strBook = _objOptions.LanguageBookShort(objProgram.Source);
					string strPage = objProgram.Page;
					lblComplexFormSource.Text = strBook + " " + strPage;
					tipTooltip.SetToolTip(lblComplexFormSource, _objOptions.LanguageBookLong(objProgram.Source) + " " + LanguageManager.Instance.GetString("String_Page") + " " + objProgram.Page);
				}
				else if (treComplexForms.SelectedNode.Level == 2)
				{
					// Locate the selected Option.
					TechProgram objProgram = new TechProgram(_objCharacter);
					TechProgramOption objOption = _objFunctions.FindTechProgramOption(treComplexForms.SelectedNode.Tag.ToString(), _objCharacter.TechPrograms, out objProgram);

					_blnSkipRefresh = true;
					nudComplexFormRating.Enabled = true;
					if (objOption.MaxRating > 0)
					{
						nudComplexFormRating.Maximum = objOption.MaxRating;
						nudComplexFormRating.Minimum = 1;
					}
					else
					{
						nudComplexFormRating.Minimum = 0;
						nudComplexFormRating.Maximum = 0;
					}
					nudComplexFormRating.Value = objOption.Rating;
					_blnSkipRefresh = false;
					lblComplexFormSource.Text = objOption.Source + " " + objOption.Page;
					tipTooltip.SetToolTip(lblComplexFormSource, _objOptions.BookFromCode(objOption.Source) + " " + LanguageManager.Instance.GetString("String_Page") + " " + objOption.Page);
				}
				else
					nudComplexFormRating.Enabled = false;
			}
			catch
			{
			}
		}

		private void nudComplexFormRating_ValueChanged(object sender, EventArgs e)
		{
			if (_blnSkipRefresh)
				return;

			try
			{
				if (treComplexForms.SelectedNode.Level == 1)
				{
					// Locate the Program that is selected in the tree.
					TechProgram objProgram = _objFunctions.FindTechProgram(treComplexForms.SelectedNode.Tag.ToString(), _objCharacter.TechPrograms);

					objProgram.Rating = Convert.ToInt32(nudComplexFormRating.Value);
					treComplexForms.SelectedNode.Text = objProgram.DisplayName;
					UpdateCharacterInfo();

					_blnIsDirty = true;
					UpdateWindowTitle();
				}
				else if (treComplexForms.SelectedNode.Level == 2)
				{
					// Locate the selected Option.
					TechProgram objProgram = new TechProgram(_objCharacter);
					TechProgramOption objOption = _objFunctions.FindTechProgramOption(treComplexForms.SelectedNode.Tag.ToString(), _objCharacter.TechPrograms, out objProgram);

					objOption.Rating = Convert.ToInt32(nudComplexFormRating.Value);
					treComplexForms.SelectedNode.Text = objOption.DisplayName;
					UpdateCharacterInfo();

					_blnIsDirty = true;
					UpdateWindowTitle();
				}
			}
			catch
			{
			}
		}

		private void cboStream_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (_blnLoading || cboStream.SelectedValue.ToString() == string.Empty)
				return;

			XmlDocument objXmlDocument = XmlManager.Instance.Load("streams.xml");

			XmlNode objXmlTradition = objXmlDocument.SelectSingleNode("/chummer/traditions/tradition[name = \"" + cboStream.SelectedValue + "\"]");
			lblFadingAttributes.Text = objXmlTradition["drain"].InnerText;
			_objCharacter.TechnomancerStream = cboStream.SelectedValue.ToString();

			foreach (SpiritControl objSpriteControl in panSprites.Controls)
				objSpriteControl.RebuildSpiritList(cboStream.SelectedValue.ToString());

			UpdateCharacterInfo();

			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void treComplexForms_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Delete)
			{
				cmdDeleteComplexForm_Click(sender, e);
			}
		}
		#endregion

		#region Additional Initiation Tab Control Events
		private void chkInitiationGroup_CheckedChanged(object sender, EventArgs e)
		{
			UpdateCharacterInfo();
		}

		private void chkInitiationOrdeal_CheckedChanged(object sender, EventArgs e)
		{
			UpdateCharacterInfo();
		}

		private void chkJoinGroup_CheckedChanged(object sender, EventArgs e)
		{
			_objCharacter.GroupMember = chkJoinGroup.Checked;
			UpdateCharacterInfo();

			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void treMetamagic_AfterSelect(object sender, TreeViewEventArgs e)
		{
			// Locate the selected Metamagic.
			Metamagic objMetamagic = _objFunctions.FindMetamagic(treMetamagic.SelectedNode.Tag.ToString(), _objCharacter.Metamagics);

			string strBook = _objOptions.LanguageBookShort(objMetamagic.Source);
			string strPage = objMetamagic.Page;
			lblMetamagicSource.Text = strBook + " " + strPage;
			tipTooltip.SetToolTip(lblMetamagicSource, _objOptions.LanguageBookLong(objMetamagic.Source) + " " + LanguageManager.Instance.GetString("String_Page") + " " + objMetamagic.Page);
		}

		private void txtGroupName_TextChanged(object sender, EventArgs e)
		{
			_objCharacter.GroupName = txtGroupName.Text;
			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void txtGroupNotes_TextChanged(object sender, EventArgs e)
		{
			_objCharacter.GroupNotes = txtGroupNotes.Text;
			_blnIsDirty = true;
			UpdateWindowTitle();
		}
		#endregion

		#region Additional Critter Powers Tab Control Events
		private void treCritterPowers_AfterSelect(object sender, TreeViewEventArgs e)
		{
			// Look for the selected Critter Power.
			lblCritterPowerName.Text = "";
			lblCritterPowerCategory.Text = "";
			lblCritterPowerType.Text = "";
			lblCritterPowerAction.Text = "";
			lblCritterPowerRange.Text = "";
			lblCritterPowerDuration.Text = "";
			lblCritterPowerSource.Text = "";
			tipTooltip.SetToolTip(lblCritterPowerSource, null);
			lblCritterPowerPointCost.Visible = false;
			lblCritterPowerPointCostLabel.Visible = false;
			try
			{
				if (treCritterPowers.SelectedNode.Level > 0)
				{
					CritterPower objPower = _objFunctions.FindCritterPower(treCritterPowers.SelectedNode.Tag.ToString(), _objCharacter.CritterPowers);

					lblCritterPowerName.Text = objPower.DisplayName;
					lblCritterPowerCategory.Text = objPower.DisplayCategory;
					lblCritterPowerType.Text = objPower.DisplayType;
					lblCritterPowerAction.Text = objPower.DisplayAction;
					lblCritterPowerRange.Text = objPower.DisplayRange;
					lblCritterPowerDuration.Text = objPower.DisplayDuration;
					chkCritterPowerCount.Checked = objPower.CountTowardsLimit;
					string strBook = _objOptions.LanguageBookShort(objPower.Source);
					string strPage = objPower.Page;
					lblCritterPowerSource.Text = strBook + " " + strPage;
					tipTooltip.SetToolTip(lblCritterPowerSource, _objOptions.LanguageBookLong(objPower.Source) + " " + LanguageManager.Instance.GetString("String_Page") + " " + objPower.Page);
					if (objPower.PowerPoints > 0)
					{
						lblCritterPowerPointCost.Text = objPower.PowerPoints.ToString();
						lblCritterPowerPointCost.Visible = true;
						lblCritterPowerPointCostLabel.Visible = true;
					}
				}
			}
			catch
			{
			}
		}

		private void chkCritterPowerCount_CheckedChanged(object sender, EventArgs e)
		{
			try
			{
				if (treCritterPowers.SelectedNode.Level == 0)
					return;
			}
			catch
			{
				return;
			}

			// Locate the selected Critter Power.
			CritterPower objPower = _objFunctions.FindCritterPower(treCritterPowers.SelectedNode.Tag.ToString(), _objCharacter.CritterPowers);

			objPower.CountTowardsLimit = chkCritterPowerCount.Checked;

			UpdateCharacterInfo();

			_blnIsDirty = true;
			UpdateWindowTitle();
		}
		#endregion

		#region Character Info Tab Event
		private void txtSex_TextChanged(object sender, EventArgs e)
		{
			_objCharacter.Sex = txtSex.Text;
			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void txtAge_TextChanged(object sender, EventArgs e)
		{
			_objCharacter.Age = txtAge.Text;
			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void txtEyes_TextChanged(object sender, EventArgs e)
		{
			_objCharacter.Eyes = txtEyes.Text;
			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void txtHair_TextChanged(object sender, EventArgs e)
		{
			_objCharacter.Hair = txtHair.Text;
			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void txtHeight_TextChanged(object sender, EventArgs e)
		{
			_objCharacter.Height = txtHeight.Text;
			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void txtWeight_TextChanged(object sender, EventArgs e)
		{
			_objCharacter.Weight = txtWeight.Text;
			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void txtSkin_TextChanged(object sender, EventArgs e)
		{
			_objCharacter.Skin = txtSkin.Text;
			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void txtDescription_TextChanged(object sender, EventArgs e)
		{
			_objCharacter.Description = txtDescription.Text;
			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void txtBackground_TextChanged(object sender, EventArgs e)
		{
			_objCharacter.Background = txtBackground.Text;
			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void txtConcept_TextChanged(object sender, EventArgs e)
		{
			_objCharacter.Concept = txtConcept.Text;
			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void txtNotes_TextChanged(object sender, EventArgs e)
		{
			_objCharacter.Notes = txtNotes.Text;
			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void txtPlayerName_TextChanged(object sender, EventArgs e)
		{
			_objCharacter.PlayerName = txtPlayerName.Text;
			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void txtAlias_TextChanged(object sender, EventArgs e)
		{
			_objCharacter.Alias = txtAlias.Text;
			_blnIsDirty = true;
			UpdateWindowTitle(false);
		}
		#endregion

		#region Tree KeyDown Events
		private void treQualities_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Delete)
			{
				cmdDeleteQuality_Click(sender, e);
			}
		}

		private void treSpells_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Delete)
			{
				cmdDeleteSpell_Click(sender, e);
			}
		}

		private void treCyberware_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Delete)
			{
				cmdDeleteCyberware_Click(sender, e);
			}
		}

		private void treLifestyles_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Delete)
			{
				cmdDeleteLifestyle_Click(sender, e);
			}
		}

		private void treArmor_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Delete)
			{
				cmdDeleteArmor_Click(sender, e);
			}
		}

		private void treWeapons_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Delete)
			{
				cmdDeleteWeapon_Click(sender, e);
			}
		}

		private void treGear_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Delete)
			{
				cmdDeleteGear_Click(sender, e);
			}
		}

		private void treVehicles_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Delete)
			{
				cmdDeleteVehicle_Click(sender, e);
			}
		}

		private void treMartialArts_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Delete)
			{
				cmdDeleteMartialArt_Click(sender, e);
			}
		}

		private void treCritterPowers_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Delete)
			{
				cmdDeleteCritterPower_Click(sender, e);
			}
		}

		private void treMetamagic_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Delete)
			{
				cmdDeleteMetamagic_Click(sender, e);
			}
		}
		#endregion

		#region Splitter Resize Events
		private void splitSkills_Panel1_Resize(object sender, EventArgs e)
		{
			panActiveSkills.Height = splitSkills.Panel1.Height - panActiveSkills.Top;
			panSkillGroups.Height = splitSkills.Panel1.Height - panSkillGroups.Top;
			panActiveSkills.Width = splitSkills.Panel1.Width - panActiveSkills.Left;
			panSkillGroups.Width = panActiveSkills.Left - 6 - panSkillGroups.Left;

			cmdAddExoticSkill.Left = panActiveSkills.Left + panActiveSkills.Width - cmdAddExoticSkill.Width - 3;
			cboSkillFilter.Left = cmdAddExoticSkill.Left - cboSkillFilter.Width - 6;
		}

		private void splitSkills_Panel2_Resize(object sender, EventArgs e)
		{
			panKnowledgeSkills.Width = splitSkills.Panel2.Width - 3;
			panKnowledgeSkills.Height = splitSkills.Panel2.Height - panKnowledgeSkills.Top;
		}

		private void splitContacts_Panel1_Resize(object sender, EventArgs e)
		{
			panContacts.Width = splitContacts.Panel1.Width - 3;
			panContacts.Height = splitContacts.Panel1.Height - panContacts.Top;
		}

		private void splitContacts_Panel2_Resize(object sender, EventArgs e)
		{
			panEnemies.Width = splitContacts.Panel2.Width - 3;
			panEnemies.Height = splitContacts.Panel2.Height - panEnemies.Top;
		}
		#endregion

		#region Other Control Events
		private void nudNuyen_ValueChanged(object sender, EventArgs e)
		{
			// Calculate the amount of Nuyen for the selected BP cost.
			_objCharacter.NuyenBP = nudNuyen.Value;
			UpdateCharacterInfo();

			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void txtCharacterName_TextChanged(object sender, EventArgs e)
		{
			_objCharacter.Name = txtCharacterName.Text;
			_blnIsDirty = true;
			UpdateWindowTitle();
		}

		private void cboSkillFilter_SelectedIndexChanged(object sender, EventArgs e)
		{
			// Hide the Panel so it redraws faster.
			panActiveSkills.Visible = false;
			switch (cboSkillFilter.SelectedValue.ToString())
			{
				case "0":
					foreach (SkillControl objSkillControl in panActiveSkills.Controls)
					{
						objSkillControl.Visible = true;
					}
					break;
				case "1":
					foreach (SkillControl objSkillControl in panActiveSkills.Controls)
					{
						if (objSkillControl.SkillObject.Rating > 0)
							objSkillControl.Visible = true;
						else
							objSkillControl.Visible = false;
					}
					break;
				case "2":
					foreach (SkillControl objSkillControl in panActiveSkills.Controls)
					{
						if (objSkillControl.SkillObject.TotalRating > 0)
							objSkillControl.Visible = true;
						else
							objSkillControl.Visible = false;
					}
					break;
				case "3":
					foreach (SkillControl objSkillControl in panActiveSkills.Controls)
					{
						if (objSkillControl.SkillObject.Rating == 0)
							objSkillControl.Visible = true;
						else
							objSkillControl.Visible = false;
					}
					break;
				case "BOD":
					foreach (SkillControl objSkillControl in panActiveSkills.Controls)
					{
						if (objSkillControl.SkillObject.Attribute == "BOD")
							objSkillControl.Visible = true;
						else
							objSkillControl.Visible = false;
					}
					break;
				case "AGI":
					foreach (SkillControl objSkillControl in panActiveSkills.Controls)
					{
						if (objSkillControl.SkillObject.Attribute == "AGI")
							objSkillControl.Visible = true;
						else
							objSkillControl.Visible = false;
					}
					break;
				case "REA":
					foreach (SkillControl objSkillControl in panActiveSkills.Controls)
					{
						if (objSkillControl.SkillObject.Attribute == "REA")
							objSkillControl.Visible = true;
						else
							objSkillControl.Visible = false;
					}
					break;
				case "STR":
					foreach (SkillControl objSkillControl in panActiveSkills.Controls)
					{
						if (objSkillControl.SkillObject.Attribute == "STR")
							objSkillControl.Visible = true;
						else
							objSkillControl.Visible = false;
					}
					break;
				case "CHA":
					foreach (SkillControl objSkillControl in panActiveSkills.Controls)
					{
						if (objSkillControl.SkillObject.Attribute == "CHA")
							objSkillControl.Visible = true;
						else
							objSkillControl.Visible = false;
					}
					break;
				case "INT":
					foreach (SkillControl objSkillControl in panActiveSkills.Controls)
					{
						if (objSkillControl.SkillObject.Attribute == "INT")
							objSkillControl.Visible = true;
						else
							objSkillControl.Visible = false;
					}
					break;
				case "LOG":
					foreach (SkillControl objSkillControl in panActiveSkills.Controls)
					{
						if (objSkillControl.SkillObject.Attribute == "LOG")
							objSkillControl.Visible = true;
						else
							objSkillControl.Visible = false;
					}
					break;
				case "WIL":
					foreach (SkillControl objSkillControl in panActiveSkills.Controls)
					{
						if (objSkillControl.SkillObject.Attribute == "WIL")
							objSkillControl.Visible = true;
						else
							objSkillControl.Visible = false;
					}
					break;
				case "MAG":
					foreach (SkillControl objSkillControl in panActiveSkills.Controls)
					{
						if (objSkillControl.SkillObject.Attribute == "MAG")
							objSkillControl.Visible = true;
						else
							objSkillControl.Visible = false;
					}
					break;
				case "RES":
					foreach (SkillControl objSkillControl in panActiveSkills.Controls)
					{
						if (objSkillControl.SkillObject.Attribute == "RES")
							objSkillControl.Visible = true;
						else
							objSkillControl.Visible = false;
					}
					break;
				default:
					if (cboSkillFilter.SelectedValue.ToString().StartsWith("GROUP:"))
					{
						string strGroup = cboSkillFilter.SelectedValue.ToString().Replace("GROUP:", string.Empty);
						foreach (SkillControl objSkillControl in panActiveSkills.Controls)
						{
							if (objSkillControl.SkillGroup == strGroup)
								objSkillControl.Visible = true;
							else
								objSkillControl.Visible = false;
						}
					}
					else
					{
						foreach (SkillControl objSkillControl in panActiveSkills.Controls)
						{
							if (objSkillControl.SkillCategory == cboSkillFilter.SelectedValue.ToString())
								objSkillControl.Visible = true;
							else
								objSkillControl.Visible = false;
						}
					}
					break;
			}
			panActiveSkills.Visible = true;
		}

		private void tabCharacterTabs_SelectedIndexChanged(object sender, EventArgs e)
		{
			RefreshPasteStatus();
		}

		private void tabStreetGearTabs_SelectedIndexChanged(object sender, EventArgs e)
		{
			RefreshPasteStatus();
		}
		#endregion

		#region Clear Tab Contents
		/// <summary>
		/// Clear the contents of the Spells and Spirits Tab.
		/// </summary>
		private void ClearSpellTab()
		{
			_objController.ClearSpellTab(treSpells);

			// Remove the Spirits.
			panSpirits.Controls.Clear();

			_blnIsDirty = true;
			UpdateCharacterInfo();
		}

		/// <summary>
		/// Clear the contents of the Adept Powers Tab.
		/// </summary>
		private void ClearAdeptTab()
		{
			_objController.ClearAdeptTab();

			// Remove all of the Adept Powers from the panel.
			panPowers.Controls.Clear();

			_blnIsDirty = true;
			UpdateCharacterInfo();
		}

		/// <summary>
		/// Clear the contents of the Sprites and Complex Forms Tab.
		/// </summary>
		private void ClearTechnomancerTab()
		{
			_objController.ClearTechnomancerTab(treComplexForms);

			// Remove the Sprites.
			panSprites.Controls.Clear();

			_blnIsDirty = true;
			UpdateCharacterInfo();
		}

		/// <summary>
		/// Clear the conents of the Critter Powers Tab.
		/// </summary>
		private void ClearCritterTab()
		{
			_objController.ClearCritterTab(treCritterPowers);

			_blnIsDirty = true;
			UpdateCharacterInfo();
		}

		/// <summary>
		/// Clear the content of the Initiation Tab.
		/// </summary>
		private void ClearInitiationTab()
		{
			_objController.ClearInitiationTab(treMetamagic);

			lblInitiateGrade.Text = "0";

			_blnIsDirty = true;
			UpdateCharacterInfo();
		}
		#endregion

		#region Sourcebook Label Events
		private void lblWeaponSource_Click(object sender, EventArgs e)
		{
			CommonFunctions objCommon = new CommonFunctions(_objCharacter);
			_objFunctions.OpenPDF(lblWeaponSource.Text);
		}

		private void lblMetatypeSource_Click(object sender, EventArgs e)
		{
			CommonFunctions objCommon = new CommonFunctions(_objCharacter);
			_objFunctions.OpenPDF(lblMetatypeSource.Text);
		}

		private void lblQualitySource_Click(object sender, EventArgs e)
		{
			CommonFunctions objCommon = new CommonFunctions(_objCharacter);
			_objFunctions.OpenPDF(lblQualitySource.Text);
		}

		private void lblMartialArtSource_Click(object sender, EventArgs e)
		{
			CommonFunctions objCommon = new CommonFunctions(_objCharacter);
			_objFunctions.OpenPDF(lblMartialArtSource.Text);
		}

		private void lblSpellSource_Click(object sender, EventArgs e)
		{
			CommonFunctions objCommon = new CommonFunctions(_objCharacter);
			_objFunctions.OpenPDF(lblSpellSource.Text);
		}

		private void lblComplexFormSource_Click(object sender, EventArgs e)
		{
			CommonFunctions objCommon = new CommonFunctions(_objCharacter);
			_objFunctions.OpenPDF(lblComplexFormSource.Text);
		}

		private void lblCritterPowerSource_Click(object sender, EventArgs e)
		{
			CommonFunctions objCommon = new CommonFunctions(_objCharacter);
			_objFunctions.OpenPDF(lblCritterPowerSource.Text);
		}

		private void lblMetamagicSource_Click(object sender, EventArgs e)
		{
			CommonFunctions objCommon = new CommonFunctions(_objCharacter);
			_objFunctions.OpenPDF(lblMetamagicSource.Text);
		}

		private void lblCyberwareSource_Click(object sender, EventArgs e)
		{
			CommonFunctions objCommon = new CommonFunctions(_objCharacter);
			_objFunctions.OpenPDF(lblCyberwareSource.Text);
		}

		private void lblLifestyleSource_Click(object sender, EventArgs e)
		{
			CommonFunctions objCommon = new CommonFunctions(_objCharacter);
			_objFunctions.OpenPDF(lblLifestyleSource.Text);
		}

		private void lblArmorSource_Click(object sender, EventArgs e)
		{
			CommonFunctions objCommon = new CommonFunctions(_objCharacter);
			_objFunctions.OpenPDF(lblArmorSource.Text);
		}

		private void lblGearSource_Click(object sender, EventArgs e)
		{
			CommonFunctions objCommon = new CommonFunctions(_objCharacter);
			_objFunctions.OpenPDF(lblGearSource.Text);
		}

		private void lblVehicleSource_Click(object sender, EventArgs e)
		{
			CommonFunctions objCommon = new CommonFunctions(_objCharacter);
			_objFunctions.OpenPDF(lblVehicleSource.Text);
		}
		#endregion

		#region Custom Methods
		/// <summary>
		/// Show the dialogue that notifies the user that characters cannot have more than 1 Attribute at its maximum value during character creation.
		/// </summary>
		public void ShowAttributeRule()
		{
			MessageBox.Show(LanguageManager.Instance.GetString("Message_AttributeMaximum"), LanguageManager.Instance.GetString("MessageTitle_Attribute"), MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		/// <summary>
		/// Show the message that describes the number of BP/Karma the character can spend on Primary Attributes.
		/// </summary>
		public void ShowAttributeBPRule()
		{
			int intPoints = 0;
			string strMethod = "";
			if (_objCharacter.BuildMethod == CharacterBuildMethod.BP)
			{
				intPoints = _objCharacter.BuildPoints / 2;
				strMethod = LanguageManager.Instance.GetString("String_BP");
			}
			else
			{
				intPoints = (_objCharacter.BuildKarma / 2) + (_objCharacter.MetatypeBP * 2);
				strMethod = LanguageManager.Instance.GetString("String_Karma");
			}
			strMethod = intPoints.ToString() + " " + strMethod;
			MessageBox.Show(LanguageManager.Instance.GetString("Message_AttributeBuildPoints").Replace("{0}", strMethod), LanguageManager.Instance.GetString("MessageTitle_Attribute"), MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		/// <summary>
		/// Let the application know that a Metatype has been selected.
		/// </summary>
		public void MetatypeSelected()
		{
			// Set the Minimum and Maximum values for each Attribute based on the selected MetaType.
			// Also update the Maximum and Augmented Maximum values displayed.
			_blnSkipUpdate = true;

			int intEssenceLoss = 0;
			if (!_objOptions.ESSLossReducesMaximumOnly)
				intEssenceLoss = _objCharacter.EssencePenalty;

			nudBOD.Maximum = _objCharacter.BOD.TotalMaximum;
			nudAGI.Maximum = _objCharacter.AGI.TotalMaximum;
			nudREA.Maximum = _objCharacter.REA.TotalMaximum;
			nudSTR.Maximum = _objCharacter.STR.TotalMaximum;
			nudCHA.Maximum = _objCharacter.CHA.TotalMaximum;
			nudINT.Maximum = _objCharacter.INT.TotalMaximum;
			nudLOG.Maximum = _objCharacter.LOG.TotalMaximum;
			nudWIL.Maximum = _objCharacter.WIL.TotalMaximum;
			nudEDG.Maximum = _objCharacter.EDG.TotalMaximum;
			nudMAG.Maximum = _objCharacter.MAG.TotalMaximum + intEssenceLoss;
			nudRES.Maximum = _objCharacter.RES.TotalMaximum + intEssenceLoss;

			nudBOD.Value = _objCharacter.BOD.Value;
			nudAGI.Value = _objCharacter.AGI.Value;
			nudREA.Value = _objCharacter.REA.Value;
			nudSTR.Value = _objCharacter.STR.Value;
			nudCHA.Value = _objCharacter.CHA.Value;
			nudINT.Value = _objCharacter.INT.Value;
			nudLOG.Value = _objCharacter.LOG.Value;
			nudWIL.Value = _objCharacter.WIL.Value;
			nudEDG.Value = _objCharacter.EDG.Value;
			nudMAG.Value = _objCharacter.MAG.Value;
			nudRES.Value = _objCharacter.RES.Value;

			nudBOD.Minimum = _objCharacter.BOD.MetatypeMinimum;
			nudAGI.Minimum = _objCharacter.AGI.MetatypeMinimum;
			nudREA.Minimum = _objCharacter.REA.MetatypeMinimum;
			nudSTR.Minimum = _objCharacter.STR.MetatypeMinimum;
			nudCHA.Minimum = _objCharacter.CHA.MetatypeMinimum;
			nudINT.Minimum = _objCharacter.INT.MetatypeMinimum;
			nudLOG.Minimum = _objCharacter.LOG.MetatypeMinimum;
			nudWIL.Minimum = _objCharacter.WIL.MetatypeMinimum;
			nudEDG.Minimum = _objCharacter.EDG.MetatypeMinimum;
			nudMAG.Minimum = _objCharacter.MAG.MetatypeMinimum;
			nudRES.Minimum = _objCharacter.RES.MetatypeMinimum;

			// If we're working with Karma, the Metatype doesn't cost anything.
			if (_objCharacter.BuildMethod == CharacterBuildMethod.BP)
				lblMetatypeBP.Text = _objCharacter.MetatypeBP.ToString() + " " + LanguageManager.Instance.GetString("String_BP");
			else if (_objCharacter.BuildMethod == CharacterBuildMethod.Karma && _objOptions.MetatypeCostsKarma)
				lblMetatypeBP.Text = (_objCharacter.MetatypeBP * _objOptions.MetatypeCostsKarmaMultiplier).ToString() + " " + LanguageManager.Instance.GetString("String_Karma");
			else
				lblMetatypeBP.Text = "0 " + LanguageManager.Instance.GetString("String_Karma");

			string strToolTip = _objCharacter.Metatype;
			if (_objCharacter.Metavariant != "")
				strToolTip += " (" + _objCharacter.Metavariant + ")";
			strToolTip += " (" + _objCharacter.MetatypeBP.ToString() + ")";
			tipTooltip.SetToolTip(lblMetatypeBP, strToolTip);

			_blnSkipUpdate = false;

			UpdateCharacterInfo();
		}

		/// <summary>
		/// Check if any other Attribute is already at its Metatype Maximum.
		/// </summary>
		/// <param name="strAttribute">NumericUpDown Control that is calling this function.</param>
		private bool CanImproveAttribute(string strAttribute)
		{
			bool blnAtMaximum = false;
			foreach (Control objControl in panAttributes.Controls)
			{
				if (objControl is NumericUpDown && objControl.Enabled)
				{
					// Edge, Magic, and Resonance are not part of the 200 BP rule do not roll those into the BP used for Attributes. They do, however, count towards the total for Karma.
					NumericUpDown nudAttribute = (NumericUpDown)objControl;
					if (nudAttribute.Name != strAttribute && nudAttribute.Name != "nudEDG" && nudAttribute.Name != "nudMAG" && nudAttribute.Name != "nudRES")
					{
						if (nudAttribute.Value == nudAttribute.Maximum && nudAttribute.Maximum != 0)
							blnAtMaximum = true;
					}
				}
			}
			return !blnAtMaximum;
		}

		/// <summary>
		/// Check if any other Skill is already at its maximum value (1 at 6 or 2 at 5).
		/// </summary>
		/// <param name="strSkill"></param>
		/// <param name="panSkillPanel">Skills Panel to check.</param>
		private bool CanImproveSkill(string strSkill, Panel panSkillPanel)
		{
			int intSkillsAtFive = 0;
			int intSkillsAtSix = 0;

			foreach (SkillControl objSkillControl in panSkillPanel.Controls)
			{
				if (objSkillControl.SkillName != strSkill)
				{
					// If a Skill's Rating is at 5 or 6 (or 7 with Aptitude Postive Quality), increment the count of Skills at that Rating.
					if (objSkillControl.SkillRating >= 6)
						intSkillsAtSix++;
					if (objSkillControl.SkillRating == 5)
						intSkillsAtFive++;
				}
			}

			// Check the current Skill's Rating and make sure it isn't trying to become 6 if at least one 5 already exists.
			foreach (SkillControl objSkillControl in panSkillPanel.Controls)
			{
				if (objSkillControl.SkillName == strSkill)
				{
					// If the Skill Rating has just been bumped to 6 and there is at least one other Skill with a Skill Rating of 5, set this Skill's Skill Rating to 5 instead.
					// Then return true which turns the handling back over to the SkillRatingChanged event to determine whether or not the Skill Rating needs to be changed any more.
					if (objSkillControl.SkillRating == 6 && intSkillsAtFive > 0)
					{
						objSkillControl.SkillRating = 5;
						return true;
					}
				}
			}
			return !(intSkillsAtSix == 1 | intSkillsAtFive == 2);
		}

		/// <summary>
		/// Calculate the BP used by Primary Attributes.
		/// </summary>
		private int CalculatePrimaryAttributeBP()
		{
			// Primary and Special Attributes are calculated separately since you can only spend a maximum of 1/2 your BP allotment on Primary Attributes.
			// Special Attributes are not subject to the 1/2 of max BP rule.
			int intBP = 0;
			string strTooltip = "";
			string strBOD = "";
			string strAGI = "";
			string strREA = "";
			string strSTR = "";
			string strCHA = "";
			string strINT = "";
			string strLOG = "";
			string strWIL = "";

			foreach (NumericUpDown objControl in panAttributes.Controls.OfType<NumericUpDown>())
			{
				int intThisBP = 0;
				if (objControl.Name != nudEDG.Name && objControl.Name != nudMAG.Name && objControl.Name != nudRES.Name)
				{
					string strAttribute = "";
					NumericUpDown nudAttribute = objControl;
					if (_objCharacter.BuildMethod == CharacterBuildMethod.BP)
					{
						// Each Attribute point costs 10 BP (the first point is free).
						intBP += ((int)nudAttribute.Value - (int)nudAttribute.Minimum) * _objOptions.BPAttribute;
						intThisBP += ((int)nudAttribute.Value - (int)nudAttribute.Minimum) * _objOptions.BPAttribute;
						// Having an Attribute at its racial maximum costs 25 BP instead of 10 (+15 BP total).
						// Attributes with a maximum of 0 are inactive and should not impact the BP.
						if (nudAttribute.Value == nudAttribute.Maximum && nudAttribute.Maximum > 0)
						{
							bool blnAddPoints = true;
							// This is skipped if the Metatype's minimum and maximum are the same.
							if (nudAttribute.Minimum == nudAttribute.Maximum)
								blnAddPoints = false;

							if (blnAddPoints)
							{
								intBP += _objOptions.BPAttributeMax;
								intThisBP += _objOptions.BPAttributeMax;
							}
						}
					}
					else
					{
						if (_objOptions.AlternateMetatypeAttributeKarma)
						{
							// Weird house rule method that treats the Metatype's minimum as being 1 for the purpose of calculating Karma costs.
							for (int i = 1; i <= (int)nudAttribute.Value - (int)nudAttribute.Minimum; i++)
							{
								intBP += (i + 1) * _objOptions.KarmaAttribute;
								intThisBP += (i + 1) * _objOptions.KarmaAttribute;
							}
						}
						else
						{
							// Karma calculation starts from the minimum score + 1 and steps through each up to the current score. At each step, the current number is multplied by the Karma Cost to
							// give us the cost of at each step.
							for (int i = (int)nudAttribute.Minimum + 1; i <= (int)nudAttribute.Value; i++)
							{
								intBP += i * _objOptions.KarmaAttribute;
								intThisBP += i * _objOptions.KarmaAttribute;
							}
						}
					}
					strAttribute = objControl.Name.Replace("nud", string.Empty) + "\t" + intThisBP.ToString();

					switch (objControl.Name)
					{
						case "nudBOD":
							strBOD = strAttribute;
							break;
						case "nudAGI":
							strAGI = strAttribute;
							break;
						case "nudREA":
							strREA = strAttribute;
							break;
						case "nudSTR":
							strSTR = strAttribute;
							break;
						case "nudCHA":
							strCHA = strAttribute;
							break;
						case "nudINT":
							strINT = strAttribute;
							break;
						case "nudLOG":
							strLOG = strAttribute;
							break;
						case "nudWIL":
							strWIL = strAttribute;
							break;
					}
				}
			}

			strTooltip = strBOD + "\n" + strAGI + "\n" + strREA + "\n" + strSTR + "\n" + strCHA + "\n" + strINT + "\n" + strLOG + "\n" + strWIL;
			tipTooltip.SetToolTip(lblAttributesBP, strTooltip);
			return intBP;
		}

		/// <summary>
		/// Calculate the BP used by Special Attributes.
		/// </summary>
		private int CalculateSpecialAttributeBP()
		{
			// Primary and Special Attributes are calculated separately since you can only spend a maximum of 1/2 your BP allotment on Primary Attributes.
			// Special Attributes are not subject to the 1/2 of max BP rule.
			int intBP = 0;
			string strTooltip = "";
			string strEDG = "";
			string strMAG = "";
			string strRES = "";

			// Find the character's Essence Loss. This applies unless the house rule to have ESS Loss only affect the Maximum of the Attribute is turned on.
			int intEssenceLoss = 0;
			if (!_objOptions.ESSLossReducesMaximumOnly)
				intEssenceLoss = _objCharacter.EssencePenalty;

			foreach (NumericUpDown objControl in panAttributes.Controls.OfType<NumericUpDown>())
			{
				int intThisBP = 0;
				// Don't apply the ESS loss penalty to EDG.
				int intUseEssenceLoss = intEssenceLoss;
				if (objControl.Name == nudEDG.Name)
					intUseEssenceLoss = 0;

				if (objControl.Name == nudEDG.Name || objControl.Name == nudMAG.Name || objControl.Name == nudRES.Name)
				{
					string strAttribute = "";
					NumericUpDown nudAttribute = objControl;
					// Disabled Attributes should not be included.
					if (nudAttribute.Enabled)
					{
						if (_objCharacter.BuildMethod == CharacterBuildMethod.BP)
						{
							// If the Essence Loss in use is not 0, reduce it by 1 to correct the BP cost since the first point of Special Attributes is always free.
							if (intUseEssenceLoss != 0)
								intUseEssenceLoss -= 1;

							if ((nudAttribute.Minimum == 0 && nudAttribute.Maximum == 0) || nudAttribute.Value == 0)
							{
								intBP += 0;
								intThisBP += 0;
							}
							else
							{
								// Each Attribute point costs 10 BP (the first point is free).
								intBP += ((int)nudAttribute.Value + intUseEssenceLoss - (int)nudAttribute.Minimum) * _objOptions.BPAttribute;
								intThisBP += ((int)nudAttribute.Value + intUseEssenceLoss - (int)nudAttribute.Minimum) * _objOptions.BPAttribute;
								// Having an Attribute at its racial maximum costs 25 BP instead of 10 (+15 BP total).
								// Attributes with a maximum of 0 are inactive and should not impact the BP.
								if (nudAttribute.Value == nudAttribute.Maximum && nudAttribute.Maximum > 0)
								{
									bool blnAddPoints = true;
									// This is skipped if the Metatype's minimum and maximum are the same.
									if (nudAttribute.Minimum == nudAttribute.Maximum)
										blnAddPoints = false;

									if (blnAddPoints)
									{
										intBP += _objOptions.BPAttributeMax;
										intThisBP += _objOptions.BPAttributeMax;
									}
								}
							}
						}
						else
						{
							// If the character has an ESS penalty, the minimum needs to be bumped up by 1 so that the cost calculation is correct.
							int intMinModifier = 0;
							if (intUseEssenceLoss > 0)
								intMinModifier = 1;

							if (nudAttribute.Minimum == 0 && nudAttribute.Maximum == 0)
							{
								intBP += 0;
								intThisBP += 0;
							}
							else
							{
								// Karma calculation starts from the minimum score + 1 and steps through each up to the current score. At each step, the current number is multplied by the Karma Cost to
								// give us the cost of at each step.
								for (int i = (int)nudAttribute.Minimum + 1 + intMinModifier; i <= (int)nudAttribute.Value + intUseEssenceLoss; i++)
								{
									intBP += i * _objOptions.KarmaAttribute;
									intThisBP += i * _objOptions.KarmaAttribute;
								}
							}
						}
						strAttribute = objControl.Name.Replace("nud", string.Empty) + "\t" + intThisBP.ToString();

						switch (objControl.Name)
						{
							case "nudEDG":
								strEDG = strAttribute;
								break;
							case "nudMAG":
								strMAG = strAttribute;
								break;
							case "nudRES":
								strRES = strAttribute;
								break;
						}
					}
				}
			}

			strTooltip = strEDG + "\n" + strMAG + "\n" + strRES;
			tipTooltip.SetToolTip(lblSpecialAttributesBP, strTooltip);
			return intBP;
		}

		/// <summary>
		/// Calculate the number of Build Points the character has remaining.
		/// </summary>
		private int CalculateBP()
		{
			int intPointsRemain = 0;
			int intPointsUsed = 0;
			int intEnemyPoints = 0;
			int intNegativePoints = 0;
			int intFreestyleBPMin = 0;
			int intFreestyleBP = 0;
			string strPoints = "";

			// Determine if cost strings should end in "BP" or "Karma" based on the Build Method being used.
			if (_objCharacter.BuildMethod == CharacterBuildMethod.BP)
			{
				intPointsRemain = _objCharacter.BuildPoints;
				strPoints = LanguageManager.Instance.GetString("String_BP");
			}
			else
			{
				intPointsRemain = _objCharacter.BuildKarma;
				strPoints = LanguageManager.Instance.GetString("String_Karma");
			}

			// Metatype/Metavariant only cost points when working with BP (or when the Metatype Costs Karma option is enabled when working with Karma).
			if (_objCharacter.BuildMethod == CharacterBuildMethod.BP || (_objCharacter.BuildMethod == CharacterBuildMethod.Karma && _objOptions.MetatypeCostsKarma))
			{
				// Subtract the BP used for Metatype.
				if (_objCharacter.BuildMethod == CharacterBuildMethod.BP)
					intPointsRemain -= _objCharacter.MetatypeBP;
				else
					intPointsRemain -= (_objCharacter.MetatypeBP * _objOptions.MetatypeCostsKarmaMultiplier);
			}

			// Calculate the BP used by Contacts.
			intPointsUsed = 0;
			foreach (ContactControl objContactControl in panContacts.Controls)
			{
				if (!objContactControl.Free)
				{
					if (_objCharacter.BuildMethod == CharacterBuildMethod.BP)
					{
						// The Contact's BP cost = their Connection + Loyalty Rating.
						intPointsRemain -= (objContactControl.ConnectionRating + objContactControl.GroupRating + objContactControl.LoyaltyRating) * _objOptions.BPContact;
						intPointsUsed += (objContactControl.ConnectionRating + objContactControl.GroupRating + objContactControl.LoyaltyRating) * _objOptions.BPContact;
					}
					else
					{
						// The Contact's Karma cost = their (Connection + Loyalty Rating ) x Karma multiplier.
						intPointsRemain -= (objContactControl.ConnectionRating + objContactControl.GroupRating + objContactControl.LoyaltyRating) * _objOptions.KarmaContact;
						intPointsUsed += (objContactControl.ConnectionRating + objContactControl.GroupRating + objContactControl.LoyaltyRating) * _objOptions.KarmaContact;
					}
				}
			}

			// If the option for CHA * X free points of Contacts is enabled, deduct that amount of points (or as many points have been spent if not the full amount).
			if (_objOptions.FreeContacts)
			{
				int intFreePoints = (_objCharacter.CHA.TotalValue * _objOptions.FreeContactsMultiplier);
				if (_objCharacter.BuildMethod == CharacterBuildMethod.Karma)
					intFreePoints *= _objOptions.KarmaContact;

				if (intPointsUsed >= intFreePoints)
				{
					intPointsUsed -= intFreePoints;
					intPointsRemain += intFreePoints;
				}
				else
				{
					intPointsRemain += intPointsUsed;
					intPointsUsed = 0;
				}
			}
			// If the option for free Contacts is enabled, deduct that amount of points (or as many points have been spent if not the full amount).
			if (_objOptions.FreeContactsFlat)
			{
				int intFreePoints = _objOptions.FreeContactsFlatNumber;
				if (_objCharacter.BuildMethod == CharacterBuildMethod.Karma)
					intFreePoints *= _objOptions.KarmaContact;

				if (intPointsUsed >= intFreePoints)
				{
					intPointsUsed -= intFreePoints;
					intPointsRemain += intFreePoints;
				}
				else
				{
					intPointsRemain += intPointsUsed;
					intPointsUsed = 0;
				}
			}
			lblContactsBP.Text = String.Format("{0} " + strPoints, intPointsUsed.ToString());
			intFreestyleBP += intPointsUsed;

			// Calculate the BP used by Enemies. These are added to the BP since they are tehnically
			// a Negative Quality.
			intPointsUsed = 0;
			foreach (ContactControl objContactControl in panEnemies.Controls)
			{
				if (!objContactControl.Free)
				{
					if (_objCharacter.BuildMethod == CharacterBuildMethod.BP)
					{
						// The Enemy's BP cost = their Connection + Loyalty Rating.
						intPointsRemain += (objContactControl.ConnectionRating + objContactControl.GroupRating + objContactControl.LoyaltyRating) * _objOptions.BPContact;
						intPointsUsed -= (objContactControl.ConnectionRating + objContactControl.GroupRating + objContactControl.LoyaltyRating) * _objOptions.BPContact;
						intEnemyPoints += (objContactControl.ConnectionRating + objContactControl.GroupRating + objContactControl.LoyaltyRating) * _objOptions.BPContact;
						intNegativePoints += intPointsUsed;
					}
					else
					{
						// The Enemy's Karma cost = their (Connection + Loyalty Rating) x Karma multiplier.
						intPointsRemain += (objContactControl.ConnectionRating + objContactControl.GroupRating + objContactControl.LoyaltyRating) * _objOptions.KarmaContact;
						intPointsUsed -= (objContactControl.ConnectionRating + objContactControl.GroupRating + objContactControl.LoyaltyRating) * _objOptions.KarmaContact;
						intEnemyPoints += (objContactControl.ConnectionRating + objContactControl.GroupRating + objContactControl.LoyaltyRating) * _objOptions.KarmaContact;
						intNegativePoints += intPointsUsed;
					}
				}
			}
			lblEnemiesBP.Text = String.Format("{0} " + strPoints, intPointsUsed.ToString());
			intFreestyleBP += intPointsUsed;

			// Calculate the BP used by Positive Qualities.
			intPointsUsed = 0;
			foreach (Quality objQuality in _objCharacter.Qualities)
			{
				if (objQuality.Type == QualityType.Positive && objQuality.ContributeToBP)
				{
					if (_objCharacter.BuildMethod == CharacterBuildMethod.BP)
					{
						intPointsRemain -= objQuality.BP;
						intPointsUsed += objQuality.BP;
					}
					else
					{
						intPointsRemain -= (objQuality.BP * _objOptions.KarmaQuality);
						intPointsUsed += (objQuality.BP * _objOptions.KarmaQuality);
					}
				}
			}

			// Deduct the amount for free Qualities.
			if (_objCharacter.BuildMethod == CharacterBuildMethod.BP)
			{
				intPointsRemain += _objImprovementManager.ValueOf(Improvement.ImprovementType.FreePositiveQualities);
				intPointsUsed -= _objImprovementManager.ValueOf(Improvement.ImprovementType.FreePositiveQualities);
			}
			else
			{
				intPointsRemain += _objImprovementManager.ValueOf(Improvement.ImprovementType.FreePositiveQualities) * _objOptions.KarmaQuality;
				intPointsUsed -= _objImprovementManager.ValueOf(Improvement.ImprovementType.FreePositiveQualities) * _objOptions.KarmaQuality;
			}

			// Include the BP used by Martial Arts. Each Rating costs 5 BP.
			foreach (MartialArt objMartialArt in _objCharacter.MartialArts)
			{
				if (_objCharacter.BuildMethod == CharacterBuildMethod.BP)
				{
					intPointsUsed += objMartialArt.Rating * _objOptions.BPMartialArt;
					intPointsRemain -= objMartialArt.Rating * _objOptions.BPMartialArt;
				}
				else
				{
					intPointsUsed += (objMartialArt.Rating * 5) * _objOptions.KarmaQuality;
					intPointsRemain -= (objMartialArt.Rating * 5) * _objOptions.KarmaQuality;
				}
			}
			lblPositiveQualitiesBP.Text = String.Format("{0} " + strPoints, intPointsUsed.ToString());
			intFreestyleBP += intPointsUsed;

			// Calculate the BP used for Negative Qualities.
			intPointsUsed = 0;
			foreach (Quality objQuality in _objCharacter.Qualities)
			{
				if (objQuality.Type == QualityType.Negative && objQuality.ContributeToBP)
				{
					if (_objCharacter.BuildMethod == CharacterBuildMethod.BP)
					{
						intPointsRemain -= objQuality.BP;
						intPointsUsed += objQuality.BP;
						intNegativePoints += objQuality.BP;
					}
					else
					{
						intPointsRemain -= (objQuality.BP * _objOptions.KarmaQuality);
						intPointsUsed += (objQuality.BP * _objOptions.KarmaQuality);
						intNegativePoints += (objQuality.BP * _objOptions.KarmaQuality);
					}
				}
			}

			// Deduct the amount for free Qualities.
			if (_objCharacter.BuildMethod == CharacterBuildMethod.BP)
			{
				intPointsRemain += _objImprovementManager.ValueOf(Improvement.ImprovementType.FreeNegativeQualities);
				intPointsUsed -= _objImprovementManager.ValueOf(Improvement.ImprovementType.FreeNegativeQualities);
				intNegativePoints -= _objImprovementManager.ValueOf(Improvement.ImprovementType.FreeNegativeQualities);
			}
			else
			{
				intPointsRemain += _objImprovementManager.ValueOf(Improvement.ImprovementType.FreeNegativeQualities) * _objOptions.KarmaQuality;
				intPointsUsed -= _objImprovementManager.ValueOf(Improvement.ImprovementType.FreeNegativeQualities) * _objOptions.KarmaQuality;
				intNegativePoints -= _objImprovementManager.ValueOf(Improvement.ImprovementType.FreeNegativeQualities) * _objOptions.KarmaQuality;
			}
			lblNegativeQualitiesBP.Text = String.Format("{0} " + strPoints, intPointsUsed.ToString());
			intFreestyleBP += intPointsUsed;

			// If the character is only allowed to gain 35 BP (70 Karma) from Negative Qualities but allowed to take as many as they'd like, limit their refunded points.
			if (_objOptions.ExceedNegativeQualitiesLimit)
			{
				if (_objCharacter.BuildMethod == CharacterBuildMethod.BP && intNegativePoints < -35)
				{
					intNegativePoints += 35;
					intPointsRemain += intNegativePoints;
				}
				if (_objCharacter.BuildMethod == CharacterBuildMethod.Karma && intNegativePoints < -70)
				{
					intNegativePoints += 70;
					intPointsRemain += intNegativePoints;
				}
			}

			// Update Primary Attributes and Special Attributes values.
			intPointsRemain -= CalculatePrimaryAttributeBP();
			intPointsUsed = CalculatePrimaryAttributeBP();
			intFreestyleBPMin = CalculatePrimaryAttributeBP() * 2;
			if (_objCharacter.BuildMethod == CharacterBuildMethod.Karma)
				intFreestyleBPMin -= (_objCharacter.MetatypeBP * 2);
			intFreestyleBP += intPointsUsed;
			lblAttributesBP.Text = String.Format("{0} " + strPoints, intPointsUsed.ToString());
			intPointsRemain -= CalculateSpecialAttributeBP();
			intPointsUsed = CalculateSpecialAttributeBP();
			lblSpecialAttributesBP.Text = String.Format("{0} " + strPoints, intPointsUsed.ToString());
			intFreestyleBP += intPointsUsed;

			// Calculate the BP used by Skill Groups.
			intPointsUsed = 0;
			foreach (SkillGroupControl objGroupControl in panSkillGroups.Controls)
			{
				if (objGroupControl.GroupRating > 0)
				{
					if (_objCharacter.BuildMethod == CharacterBuildMethod.BP)
					{
						// Each Group Rating costs 10 BP.
						intPointsRemain -= objGroupControl.GroupRating * _objOptions.BPSkillGroup;
						intPointsUsed += objGroupControl.GroupRating * _objOptions.BPSkillGroup;
					}
					else
					{
						// The first point in a Skill Group costs KaramNewSkillGroup.
						// Each additional beyond 1 costs i x KarmaImproveSkillGroup.
						intPointsRemain -= _objOptions.KarmaNewSkillGroup;
						intPointsUsed += _objOptions.KarmaNewSkillGroup;
						for (int i = 2; i <= objGroupControl.GroupRating; i++)
						{
							intPointsRemain -= i * _objOptions.KarmaImproveSkillGroup;
							intPointsUsed += i * _objOptions.KarmaImproveSkillGroup;
						}
					}
				}

				// If the Skill Group has been broken, get the Rating value for the lowest Skill in the Group.
				if (objGroupControl.Broken && _objOptions.BreakSkillGroupsInCreateMode)
				{
					int intMin = 999;
					foreach (Skill objSkill in _objCharacter.Skills)
					{
						if (objSkill.SkillGroup == objGroupControl.GroupName)
						{
							if (objSkill.Rating < intMin)
								intMin = objSkill.Rating;
						}
					}

					if (_objCharacter.BuildMethod == CharacterBuildMethod.BP)
					{
						intPointsRemain -= intMin * _objOptions.BPSkillGroup;
						intPointsUsed += intMin * _objOptions.BPSkillGroup;
					}
					else
					{
						intPointsRemain -= _objOptions.KarmaNewSkillGroup;
						intPointsUsed += _objOptions.KarmaNewSkillGroup;
						for (int i = 2; i <= intMin; i++)
						{
							intPointsRemain -= i * _objOptions.KarmaImproveSkillGroup;
							intPointsUsed += i * _objOptions.KarmaImproveSkillGroup;
						}
					}
				}
			}
			lblSkillGroupsBP.Text = String.Format("{0} " + strPoints, intPointsUsed.ToString());
			intFreestyleBP += intPointsUsed;

			// Calculate the BP used by Active Skills.
			intPointsUsed = 0;
			foreach (SkillControl objSkillControl in panActiveSkills.Controls)
			{
				if (objSkillControl.SkillRating > 0 && !objSkillControl.IsGrouped)
				{
					if (_objCharacter.BuildMethod == CharacterBuildMethod.BP)
					{
						int intSkillRating = objSkillControl.SkillRating;

						// Each Skill Rating costs 4 BP (only look at Skills that are not part of a Skill Group). (doubled if Uneducated and Technical Active, Uncouth and Social Active, or Inform and Physical Active)
						if ((_objCharacter.Uneducated && objSkillControl.SkillCategory == "Technical Active") || (_objCharacter.Uncouth && objSkillControl.SkillCategory == "Social Active") || (_objCharacter.Infirm && objSkillControl.SkillCategory == "Physical Active"))
						{
							intPointsRemain -= (objSkillControl.SkillRating * _objOptions.BPActiveSkill) * 2;
							intPointsUsed += (objSkillControl.SkillRating * _objOptions.BPActiveSkill) * 2;
							// BP cost is doubled when increasing a Skill's Rating above 6.
							if (objSkillControl.SkillRating > 6)
							{
								intPointsRemain -= (objSkillControl.SkillRating - 6) * (_objOptions.BPActiveSkill * 2);
								intPointsUsed += (objSkillControl.SkillRating - 6) * (_objOptions.BPActiveSkill * 2);
							}
						}
						else
						{
							intPointsRemain -= objSkillControl.SkillRating * _objOptions.BPActiveSkill;
							intPointsUsed += objSkillControl.SkillRating * _objOptions.BPActiveSkill;
							// BP cost is doubled when increasing a Skill's Rating above 6.
							if (objSkillControl.SkillRating > 6)
							{
								intPointsRemain -= (objSkillControl.SkillRating - 6) * _objOptions.BPActiveSkill;
								intPointsUsed += (objSkillControl.SkillRating - 6) * _objOptions.BPActiveSkill;
							}
						}

						// If the ability to break Skill Groups is enabled, refund the cost of the first X points of the Skill, where X is the minimum Rating for all Skill that used to a part of the Group.
						if (_objOptions.BreakSkillGroupsInCreateMode)
						{
							int intMin = 999;
							bool blnApplyModifier = false;

							// Find the matching Skill Group.
							foreach (SkillGroup objGroup in _objCharacter.SkillGroups)
							{
								if (objGroup.Broken && objGroup.Name == objSkillControl.SkillGroup)
								{
									// Determine the lowest Rating amongst the Skills in the Groups.
									foreach (Skill objSkill in _objCharacter.Skills)
									{
										if (objSkill.SkillGroup == objGroup.Name)
										{
											if (objSkill.Rating < intMin)
											{
												intMin = objSkill.Rating;
												blnApplyModifier = true;
											}
										}
									}
									break;
								}
							}

							if (blnApplyModifier)
							{
								intPointsRemain += intMin * _objOptions.BPActiveSkill;
								intPointsUsed -= intMin * _objOptions.BPActiveSkill;
							}
						}
					}
					else
					{
						// The first point in a Skill costs KarmaNewActiveSkill.
						// Each additional beyond 1 costs i x KarmaImproveActiveSkill.
						if ((_objCharacter.Uneducated && objSkillControl.SkillCategory == "Technical Active") || (_objCharacter.Uncouth && objSkillControl.SkillCategory == "Social Active") || (_objCharacter.Infirm && objSkillControl.SkillCategory == "Physical Active"))
						{
							intPointsRemain -= _objOptions.KarmaNewActiveSkill * 2;
							intPointsUsed += _objOptions.KarmaNewActiveSkill * 2;
						}
						else
						{
							intPointsRemain -= _objOptions.KarmaNewActiveSkill;
							intPointsUsed += _objOptions.KarmaNewActiveSkill;
						}
						for (int i = 2; i <= objSkillControl.SkillRating; i++)
						{
							if ((_objCharacter.Uneducated && objSkillControl.SkillCategory == "Technical Active") || (_objCharacter.Uncouth && objSkillControl.SkillCategory == "Social Active") || (_objCharacter.Infirm && objSkillControl.SkillCategory == "Physical Active"))
							{
								intPointsRemain -= (i * _objOptions.KarmaImproveActiveSkill) * 2;
								intPointsUsed += (i * _objOptions.KarmaImproveActiveSkill * 2);
								// Karma cost is doubled when increasing a Skill's Rating above 6.
								if (i > 6)
								{
									intPointsRemain -= (i * _objOptions.KarmaImproveActiveSkill) * 2;
									intPointsUsed += (i * _objOptions.KarmaImproveActiveSkill) * 2;
								}
							}
							else
							{
								intPointsRemain -= i * _objOptions.KarmaImproveActiveSkill;
								intPointsUsed += i * _objOptions.KarmaImproveActiveSkill;
								// Karma cost is doubled when increasing a Skill's Rating above 6.
								if (i > 6)
								{
									intPointsRemain -= i * _objOptions.KarmaImproveActiveSkill;
									intPointsUsed += i * _objOptions.KarmaImproveActiveSkill;
								}
							}
						}

						// If the ability to break Skill Groups is enabled, refund the cost of the first X points of the Skill, where X is the minimum Rating for all Skill that used to a part of the Group.
						if (_objOptions.BreakSkillGroupsInCreateMode)
						{
							int intMin = 999;
							bool blnApplyModifier = false;

							// Find the matching Skill Group.
							foreach (SkillGroup objGroup in _objCharacter.SkillGroups)
							{
								if (objGroup.Broken && objGroup.Name == objSkillControl.SkillGroup)
								{
									// Determine the lowest Rating amongst the Skills in the Groups.
									foreach (Skill objSkill in _objCharacter.Skills)
									{
										if (objSkill.SkillGroup == objGroup.Name)
										{
											if (objSkill.Rating < intMin)
											{
												intMin = objSkill.Rating;
												blnApplyModifier = true;
											}
										}
									}
									break;
								}
							}

							if (blnApplyModifier)
							{
								// Refund the first X points of Karma cost for the Skill.
								if (intMin >= 1)
								{
									intPointsRemain += _objOptions.KarmaNewActiveSkill;
									intPointsUsed -= _objOptions.KarmaNewActiveSkill;
								}
								if (intMin > 1)
								{
									for (int i = 2; i <= intMin; i++)
									{
										intPointsRemain += i * _objOptions.KarmaImproveActiveSkill;
										intPointsUsed -= i * _objOptions.KarmaImproveActiveSkill;
									}
								}
							}
						}
					}
				}

				// Specialization Cost (Exotic skills do not count since their "Spec" is actually what the Skill is being used for and cannot be Specialized).
				if (objSkillControl.SkillSpec.Trim() != string.Empty && !objSkillControl.SkillObject.ExoticSkill)
				{
					if (_objCharacter.BuildMethod == CharacterBuildMethod.BP)
					{
						// Each Specialization costs an additional 2 BP.
						intPointsRemain -= _objOptions.BPActiveSkillSpecialization;
						intPointsUsed += _objOptions.BPActiveSkillSpecialization;
					}
					else
					{
						// Each Specialization costs KarmaSpecialization.
						intPointsRemain -= _objOptions.KarmaSpecialization;
						intPointsUsed += _objOptions.KarmaSpecialization;
					}
				}
			}
			lblActiveSkillsBP.Text = String.Format("{0} " + strPoints, intPointsUsed.ToString());
			intFreestyleBP += intPointsUsed;

			// Calculate the BP used by Knowledge Skills.
			int intPointsInKnowledgeSkills = 0;
			intPointsUsed = 0;
			int intSpecCount = 0;
			foreach (SkillControl objSkillControl in panKnowledgeSkills.Controls)
			{
				// Add the current Skill's SkillRating to the counter.
				intPointsInKnowledgeSkills += objSkillControl.SkillRating;
				
				// The cost is double if the character is Uneducated and is an Academic or Professional Skill.
				if (_objCharacter.Uneducated && (objSkillControl.SkillCategory == "Academic" || objSkillControl.SkillCategory == "Professional"))
					intPointsInKnowledgeSkills += objSkillControl.SkillRating;

				// The Linguistics Adept Power gives 1 free point in Languages.
				if (_objImprovementManager.ValueOf(Improvement.ImprovementType.AdeptLinguistics) > 0 && objSkillControl.SkillCategory == "Language" && objSkillControl.SkillRating > 0)
					intPointsInKnowledgeSkills--;
				
				if (objSkillControl.SkillSpec.Trim() != string.Empty)
				{
					if (_objCharacter.BuildMethod == CharacterBuildMethod.BP)
						intPointsInKnowledgeSkills++;
					else
						intPointsInKnowledgeSkills += _objOptions.KarmaSpecialization;
					intSpecCount++;
				}
			}

			// Specializations do not count towards free Knowledge Skills in Karma Build mode.
			if (_objCharacter.BuildMethod == CharacterBuildMethod.Karma && _objOptions.FreeKarmaKnowledge)
				intPointsInKnowledgeSkills -= (intSpecCount * _objOptions.KarmaSpecialization);

			// If the number of points in Knowledge Skills exceeds the free amount, the remaining amount is deducted from BP.
			if (intPointsInKnowledgeSkills > _objCharacter.KnowledgeSkillPoints)
			{
				if (_objCharacter.BuildMethod == CharacterBuildMethod.BP)
				{
					// Each additional Knowledge Skill costs 2 BP.
					intPointsRemain -= (intPointsInKnowledgeSkills - _objCharacter.KnowledgeSkillPoints) * _objOptions.BPKnowledgeSkill;
					intPointsUsed += (intPointsInKnowledgeSkills - _objCharacter.KnowledgeSkillPoints) * _objOptions.BPKnowledgeSkill;
				}
				else
				{
					// Working with Karma is different. Go through all of the Knowledge Skills and calculate their costs individually.
					foreach (SkillControl objSkillControl in panKnowledgeSkills.Controls)
					{
						// The first point in a Knowledge Skill costs KarmaNewKnowledgeSkill.
						// Each additional beyond 1 costs i x KarmaImproveKnowledgeSkill.
						if (objSkillControl.SkillRating > 0)
						{
							// Adepts with the Linguist Power get the first point of a Language for free.
							if (!(_objImprovementManager.ValueOf(Improvement.ImprovementType.AdeptLinguistics) > 0 && objSkillControl.SkillCategory == "Language" && objSkillControl.SkillRating > 0))
							{
								intPointsRemain -= _objOptions.KarmaNewKnowledgeSkill;
								intPointsUsed += _objOptions.KarmaNewKnowledgeSkill;
							}
							for (int i = 2; i <= objSkillControl.SkillRating; i++)
							{
								intPointsRemain -= i * _objOptions.KarmaImproveKnowledgeSkill;
								intPointsUsed += i * _objOptions.KarmaImproveKnowledgeSkill;
							}
							if (objSkillControl.SkillSpec.Trim() != string.Empty)
							{
								intPointsRemain -= _objOptions.KarmaSpecialization;
								intPointsUsed += _objOptions.KarmaSpecialization;
							}
						}
					}

					if (_objCharacter.BuildMethod == CharacterBuildMethod.Karma && _objOptions.FreeKarmaKnowledge)
					{
						// Working with Karma and free Knowledge Skills.
						int intKnowledgePointsUsed = intPointsInKnowledgeSkills;
						if (intKnowledgePointsUsed > _objCharacter.KnowledgeSkillPoints)
							intKnowledgePointsUsed = _objCharacter.KnowledgeSkillPoints;

						// Go back through the controls and start adjusting costs for free points.
						int intRating = 0;
						do
						{
							intRating++;
							foreach (SkillControl objSkillControl in panKnowledgeSkills.Controls)
							{
								if (intRating <= objSkillControl.SkillRating)
								{
									intKnowledgePointsUsed--;
									if (intRating == 1)
									{
										intPointsRemain += _objOptions.KarmaNewKnowledgeSkill;
										intPointsUsed -= _objOptions.KarmaNewKnowledgeSkill;
									}
									else
									{
										intPointsRemain += intRating * _objOptions.KarmaImproveKnowledgeSkill;
										intPointsUsed -= intRating * _objOptions.KarmaImproveKnowledgeSkill;
									}
									if (intKnowledgePointsUsed == 0)
										break;
								}
							}
						}while (intKnowledgePointsUsed > 0);
					}
				}
				// Update the label that displays the number of free Knowledge Skill points remaining.
				lblKnowledgeSkillPoints.Text = String.Format("0 " + LanguageManager.Instance.GetString("String_Of") + " {0}", _objCharacter.KnowledgeSkillPoints.ToString());
			}
			else
			{
				// Update the label that displays the number of free Knowledge Skill points remaining.
				lblKnowledgeSkillPoints.Text = String.Format("{0} " + LanguageManager.Instance.GetString("String_Of") + " {1}", (_objCharacter.KnowledgeSkillPoints - intPointsInKnowledgeSkills).ToString(), _objCharacter.KnowledgeSkillPoints.ToString());
			}
			lblKnowledgeSkillsBP.Text = String.Format("{0} " + strPoints, intPointsUsed.ToString());
			intFreestyleBP += intPointsUsed;

			// Calculate the BP used by Resources/Nuyen.
			intPointsRemain -= (int)nudNuyen.Value;
			lblNuyenBP.Text = nudNuyen.Value.ToString() + " " + strPoints;
			intFreestyleBP += (int)nudNuyen.Value;

			// Calculate the BP used by Spells.
			intPointsUsed = 0;
			if (_objCharacter.MagicianEnabled)
			{
				// Count the number of Spells the character currently has and make sure they do not try to select more Spells than they are allowed.
				// The maximum number of Spells a character can start with is 2 x (highest of Spellcasting or Ritual Spellcasting Skill).
				int intSpellCount = 0;
				foreach (TreeNode nodCategory in treSpells.Nodes)
				{
					foreach (TreeNode nodSpell in nodCategory.Nodes)
					{
						intSpellCount++;
					}
				}

				if (_objCharacter.BuildMethod == CharacterBuildMethod.BP)
				{
					// Each spell costs 3 BP.
					intPointsRemain -= intSpellCount * _objOptions.BPSpell;
					intPointsUsed += intSpellCount * _objOptions.BPSpell;
					tipTooltip.SetToolTip(lblSpellsBP, intSpellCount.ToString() + " x 3 " + LanguageManager.Instance.GetString("String_BP") + " = " + intPointsUsed.ToString() + " " + LanguageManager.Instance.GetString("String_BP"));
				}
				else
				{
					// Each spell costs KarmaSpell.
					intPointsRemain -= intSpellCount * _objOptions.KarmaSpell;
					intPointsUsed += intSpellCount * _objOptions.KarmaSpell;
					tipTooltip.SetToolTip(lblSpellsBP, intSpellCount.ToString() + " x " + _objOptions.KarmaSpell + " " + LanguageManager.Instance.GetString("String_Karma") + " = " + intPointsUsed.ToString() + " " + LanguageManager.Instance.GetString("String_Karma"));
				}
			}
			lblSpellsBP.Text = String.Format("{0} " + strPoints, intPointsUsed.ToString());
			intFreestyleBP += intPointsUsed;

			// Calculate the BP used by Foci.
			intPointsUsed = 0;
			foreach (Focus objFocus in _objCharacter.Foci)
			{
				if (_objCharacter.BuildMethod == CharacterBuildMethod.BP)
				{
					// Each Focus costs a number of BP equal to their Force.
					intPointsRemain -= objFocus.Rating * _objOptions.BPFocus;
					intPointsUsed += objFocus.Rating * _objOptions.BPFocus;
				}
				else
				{
					// Each Focus costs an amount of Karma equal to their Force x speicific Karma cost.
					string strFocusName = objFocus.Name;
					int intPosition = strFocusName.IndexOf("(");
					if (intPosition > -1)
						strFocusName = strFocusName.Substring(0, intPosition - 1);
					int intKarmaMultiplier = 0;
					switch (strFocusName)
					{
						case "Symbolic Link Focus":
							intKarmaMultiplier = _objOptions.KarmaSymbolicLinkFocus;
							break;
						case "Sustaining Focus":
							intKarmaMultiplier = _objOptions.KarmaSustainingFocus;
							break;
						case "Counterspelling Focus":
							intKarmaMultiplier = _objOptions.KarmaCounterspellingFocus;
							break;
						case "Banishing Focus":
							intKarmaMultiplier = _objOptions.KarmaBanishingFocus;
							break;
						case "Binding Focus":
							intKarmaMultiplier = _objOptions.KarmaBindingFocus;
							break;
						case "Weapon Focus":
							intKarmaMultiplier = _objOptions.KarmaWeaponFocus;
							break;
						case "Spellcasting Focus":
							intKarmaMultiplier = _objOptions.KarmaSpellcastingFocus;
							break;
						case "Summoning Focus":
							intKarmaMultiplier = _objOptions.KarmaSummoningFocus;
							break;
						case "Anchoring Focus":
							intKarmaMultiplier = _objOptions.KarmaAnchoringFocus;
							break;
						case "Centering Focus":
							intKarmaMultiplier = _objOptions.KarmaCenteringFocus;
							break;
						case "Masking Focus":
							intKarmaMultiplier = _objOptions.KarmaMaskingFocus;
							break;
						case "Shielding Focus":
							intKarmaMultiplier = _objOptions.KarmaShieldingFocus;
							break;
						case "Power Focus":
							intKarmaMultiplier = _objOptions.KarmaPowerFocus;
							break;
						case "Divining Focus":
							intKarmaMultiplier = _objOptions.KarmaDiviningFocus;
							break;
						case "Dowsing Focus":
							intKarmaMultiplier = _objOptions.KarmaDowsingFocus;
							break;
						case "Infusion Focus":
							intKarmaMultiplier = _objOptions.KarmaInfusionFocus;
							break;
						default:
							intKarmaMultiplier = 1;
							break;
					}
					intPointsRemain -= objFocus.Rating * intKarmaMultiplier;
					intPointsUsed += objFocus.Rating * intKarmaMultiplier;
				}
			}

			// Calculate the BP used by Stacked Foci.
			foreach (StackedFocus objFocus in _objCharacter.StackedFoci)
			{
				if (objFocus.Bonded)
				{
					if (_objCharacter.BuildMethod == CharacterBuildMethod.BP)
					{
						intPointsRemain -= objFocus.TotalForce * _objOptions.BPFocus;
						intPointsUsed += objFocus.TotalForce * _objOptions.BPFocus;
					}
					else
					{
						intPointsRemain -= objFocus.BindingCost;
						intPointsUsed += objFocus.BindingCost;
					}
				}
			}

			lblFociBP.Text = String.Format("{0} " + strPoints, intPointsUsed.ToString());
			intFreestyleBP += intPointsUsed;

			// Calculate the BP used by Spirits.
			intPointsUsed = 0;
			foreach (SpiritControl objSpiritControl in panSpirits.Controls)
			{
				if (_objCharacter.BuildMethod == CharacterBuildMethod.BP)
				{
					// BP = the number of services owed.
					intPointsRemain -= objSpiritControl.ServicesOwed * _objOptions.BPSpirit;
					intPointsUsed += objSpiritControl.ServicesOwed * _objOptions.BPSpirit;
				}
				else
				{
					// Each Spirit costs KarmaSpirit x Services Owed.
					intPointsRemain -= objSpiritControl.ServicesOwed * _objOptions.KarmaSpirit;
					intPointsUsed += objSpiritControl.ServicesOwed * _objOptions.KarmaSpirit;
				}
			}
			lblSpiritsBP.Text = String.Format("{0} " + strPoints, intPointsUsed.ToString());
			intFreestyleBP += intPointsUsed;

			// Calculate the BP used by Sprites.
			intPointsUsed = 0;
			foreach (SpiritControl objSpriteControl in panSprites.Controls)
			{
				if (_objCharacter.BuildMethod == CharacterBuildMethod.BP)
				{
					// BP = the number of tasks owed.
					intPointsRemain -= objSpriteControl.ServicesOwed * _objOptions.BPSpirit;
					intPointsUsed += objSpriteControl.ServicesOwed * _objOptions.BPSpirit;
				}
				else
				{
					// Each Sprite costs KarmaSpirit x Services Owed.
					intPointsRemain -= objSpriteControl.ServicesOwed * _objOptions.KarmaSpirit;
					intPointsUsed += objSpriteControl.ServicesOwed * _objOptions.KarmaSpirit;
				}
			}
			lblSpritesBP.Text = String.Format("{0} " + strPoints, intPointsUsed.ToString());
			intFreestyleBP += intPointsUsed;

			// Calculate the BP used by Complex Forms.
			intPointsUsed = 0;
			foreach (TechProgram objProgram in _objCharacter.TechPrograms)
			{
				if (_objCharacter.BuildMethod == CharacterBuildMethod.BP)
				{
					if (!_objOptions.AlternateComplexFormCost)
					{
						// BP = the program rating.
						intPointsRemain -= objProgram.Rating * _objOptions.BPComplexForm;
						intPointsUsed += objProgram.Rating * _objOptions.BPComplexForm;
					}
					else
					{
						// With the optional rule, BP = the same cost as Spells.
						intPointsRemain -= _objOptions.BPSpell;
						intPointsUsed += _objOptions.BPSpell;
					}
				}
				else
				{
					if (!_objOptions.AlternateComplexFormCost)
					{
						// The first point in a Complex Form costs KarmaNewComplexForm. (Skillsofts only cost KarmaImproveComplexForm for the first point)
						// Each additional beyond 1 costs i x KarmaImproveComplexForm.
						if (objProgram.Category == "Skillsofts")
						{
							intPointsRemain -= _objOptions.KarmaComplexFormSkillsoft * objProgram.Rating;
							intPointsUsed += _objOptions.KarmaComplexFormSkillsoft * objProgram.Rating;
						}
						else
						{
							intPointsRemain -= _objOptions.KarmaNewComplexForm;
							intPointsUsed += _objOptions.KarmaNewComplexForm;

							for (int i = 2; i <= objProgram.Rating; i++)
							{
								intPointsRemain -= i * _objOptions.KarmaImproveComplexForm;
								intPointsUsed += i * _objOptions.KarmaImproveComplexForm;
							}
						}
					}
					else
					{
						// With the optional rule, Complex Forms cost the same amount as Spells.
						intPointsRemain -= _objOptions.KarmaSpell;
						intPointsUsed += _objOptions.KarmaSpell;
					}
				}

				// Include any Program Options attached to the Complex Form.
				foreach (TechProgramOption objOption in objProgram.Options)
				{
					if (_objCharacter.BuildMethod == CharacterBuildMethod.BP)
					{
						if (objOption.Rating == 0)
						{
							intPointsRemain -= _objOptions.BPComplexFormOption;
							intPointsUsed += _objOptions.BPComplexFormOption;
						}
						else
						{
							intPointsRemain -= objOption.Rating * _objOptions.BPComplexFormOption;
							intPointsUsed += objOption.Rating * _objOptions.BPComplexFormOption;
						}
					}
					else
					{
						if (objOption.Rating == 0)
						{
							// Skillsofts Options only cost KarmaImproveComplexForm Karma.
							if (objProgram.Category == "Skillsofts")
							{
								intPointsRemain -= _objOptions.KarmaComplexFormSkillsoft;
								intPointsUsed += _objOptions.KarmaComplexFormSkillsoft;
							}
							else
							{
								intPointsRemain -= _objOptions.KarmaComplexFormOption;
								intPointsUsed += _objOptions.KarmaComplexFormOption;
							}
						}
						else
						{
							if (objProgram.Category == "Skillsofts")
							{
								intPointsRemain -= (_objOptions.KarmaComplexFormSkillsoft * objOption.Rating);
								intPointsUsed += (_objOptions.KarmaComplexFormSkillsoft * objOption.Rating);
							}
							else
							{
								intPointsRemain -= (_objOptions.KarmaComplexFormOption * objOption.Rating);
								intPointsUsed += (_objOptions.KarmaComplexFormOption * objOption.Rating);
							}
						}
					}
				}
			}
			lblComplexFormsBP.Text = String.Format("{0} " + strPoints, intPointsUsed.ToString());
			intFreestyleBP += intPointsUsed;

			// Calculate the BP used by Martial Art Maneuvers.
			if (_objCharacter.BuildMethod == CharacterBuildMethod.BP)
			{
				// BP = Maneuvers * 2.
				intPointsRemain -= _objCharacter.MartialArtManeuvers.Count * _objOptions.BPMartialArtManeuver;
				intPointsUsed = _objCharacter.MartialArtManeuvers.Count * _objOptions.BPMartialArtManeuver;
			}
			else
			{
				// Each Maneuver costs KarmaManeuver.
				intPointsRemain -= _objCharacter.MartialArtManeuvers.Count * _objOptions.KarmaManeuver;
				intPointsUsed = _objCharacter.MartialArtManeuvers.Count * _objOptions.KarmaManeuver;
			}
			lblManeuversBP.Text = String.Format("{0} " + strPoints, intPointsUsed.ToString());
			intFreestyleBP += intPointsUsed;

			// Calculate the BP used by Initiation.
			intPointsUsed = 0;
			int intInitiationPoints = 0;
			foreach (InitiationGrade objGrade in _objCharacter.InitiationGrades)
				intInitiationPoints += objGrade.KarmaCost;

			// Add the Karma cost of extra Metamagic/Echoes to the Initiation cost.
			foreach (Metamagic objMetamagic in _objCharacter.Metamagics)
			{
				if (objMetamagic.PaidWithKarma)
					intInitiationPoints += _objOptions.KarmaMetamagic;
			}

			// Check to see if the character is a member of a Group.
			if (_objCharacter.GroupMember && _objCharacter.MAGEnabled)
				intInitiationPoints += _objOptions.KarmaJoinGroup;

			intPointsRemain -= intInitiationPoints;
			intPointsUsed += intInitiationPoints;
			lblInitiationBP.Text = string.Format("{0} " + strPoints, intPointsUsed.ToString());
			intFreestyleBP += intPointsUsed;

			// Update the number of BP remaining in the StatusBar.
			if (_objCharacter.BuildMethod == CharacterBuildMethod.BP)
				tssBP.Text = _objCharacter.BuildPoints.ToString();
			else
				tssBP.Text = _objCharacter.BuildKarma.ToString();
			tssBPRemain.Text = intPointsRemain.ToString();

			if (_blnFreestyle)
			{
				tssBP.Text = Math.Max(intFreestyleBP, intFreestyleBPMin).ToString();
				if (intFreestyleBP < intFreestyleBPMin)
					tssBP.ForeColor = Color.OrangeRed;
				else
					tssBP.ForeColor = SystemColors.ControlText;
			}

			return Convert.ToInt32(tssBPRemain.Text);
		}

		/// <summary>
		/// Calculate the number of Adept Power Points used.
		/// </summary>
		private void CalculatePowerPoints()
		{
			decimal decPowerPoints = 0;

			foreach (PowerControl objPowerControl in panPowers.Controls)
			{
				decPowerPoints += objPowerControl.PowerPoints;
				objPowerControl.UpdatePointsPerLevel();
			}

			int intMAG = 0;
			if (_objCharacter.AdeptEnabled && _objCharacter.MagicianEnabled)
			{
				// If both Adept and Magician are enabled, this is a Mystic Adept, so use the MAG amount assigned to this portion.
				intMAG = _objCharacter.MAGAdept;
			}
			else
			{
				// The character is just an Adept, so use the full value.
				intMAG = _objCharacter.MAG.TotalValue;
			}

			// Add any Power Point Improvements to MAG.
			intMAG += _objImprovementManager.ValueOf(Improvement.ImprovementType.AdeptPowerPoints);

			string strRemain = (intMAG - decPowerPoints).ToString();
			while (strRemain.EndsWith("0") && strRemain.Length > 4)
				strRemain = strRemain.Substring(0, strRemain.Length - 1);

			lblPowerPoints.Text = String.Format("{1} ({0} " + LanguageManager.Instance.GetString("String_Remaining") + ")", strRemain, intMAG);
		}

		/// <summary>
		/// Calculate the number of Free Spirit Power Points used.
		/// </summary>
		private void CalculateFreeSpiritPowerPoints()
		{
			if (_objCharacter.Metatype == "Free Spirit" && !_objCharacter.IsCritter)
			{
				// PC Free Spirit.
				double dblPowerPoints = 0;

				foreach (CritterPower objPower in _objCharacter.CritterPowers)
				{
					if (objPower.CountTowardsLimit)
						dblPowerPoints += objPower.PowerPoints;
				}

				int intPowerPoints = _objCharacter.EDG.TotalValue + _objImprovementManager.ValueOf(Improvement.ImprovementType.FreeSpiritPowerPoints);

				// If the house rule to base Power Points on the character's MAG value instead, use the character's MAG.
				if (_objCharacter.Options.FreeSpiritPowerPointsMAG)
					intPowerPoints = _objCharacter.MAG.TotalValue + _objImprovementManager.ValueOf(Improvement.ImprovementType.FreeSpiritPowerPoints);

				lblCritterPowerPointsLabel.Visible = true;
				lblCritterPowerPoints.Visible = true;
				lblCritterPowerPoints.Text = String.Format("{1} ({0} " + LanguageManager.Instance.GetString("String_Remaining") + ")", intPowerPoints - dblPowerPoints, intPowerPoints);
			}
			else
			{
				int intPowerPoints = 0;

				if (_objCharacter.Metatype == "Free Spirit")
				{
					// Critter Free Spirits have a number of Power Points equal to their EDG plus any Free Spirit Power Points Improvements.
					intPowerPoints = _objCharacter.EDG.Value + _objImprovementManager.ValueOf(Improvement.ImprovementType.FreeSpiritPowerPoints); ;
				}
				else if (_objCharacter.Metatype == "Ally Spirit")
				{
					// Ally Spirits get a number of Power Points equal to their MAG.
					intPowerPoints = _objCharacter.MAG.TotalValue;
				}
				else
				{
					// Spirits get 1 Power Point for every 3 full points of Force (MAG) they possess.
					double dblMAG = Convert.ToDouble(_objCharacter.MAG.TotalValue, GlobalOptions.Instance.CultureInfo);
					intPowerPoints = Convert.ToInt32(Math.Floor(dblMAG / 3.0));
				}

				int intUsed = 0;// _objCharacter.CritterPowers.Count - intExisting;
				foreach (CritterPower objPower in _objCharacter.CritterPowers)
				{
					if (objPower.Category != "Weakness" && objPower.CountTowardsLimit)
						intUsed++;
				}

				lblCritterPowerPointsLabel.Visible = true;
				lblCritterPowerPoints.Visible = true;
				lblCritterPowerPoints.Text = String.Format("{1} ({0} " + LanguageManager.Instance.GetString("String_Remaining") + ")", intPowerPoints - intUsed, intPowerPoints);
			}
		}

		/// <summary>
		/// Calculate the number of Free Sprite Power Points used.
		/// </summary>
		private void CalculateFreeSpritePowerPoints()
		{
			// Free Sprite Power Points.
			double dblPowerPoints = 0;

			foreach (CritterPower objPower in _objCharacter.CritterPowers)
			{
				if (objPower.CountTowardsLimit)
					dblPowerPoints += 1;
			}

			int intPowerPoints = _objCharacter.EDG.TotalValue + _objImprovementManager.ValueOf(Improvement.ImprovementType.FreeSpiritPowerPoints);

			lblCritterPowerPointsLabel.Visible = true;
			lblCritterPowerPoints.Visible = true;
			lblCritterPowerPoints.Text = String.Format("{1} ({0} " + LanguageManager.Instance.GetString("String_Remaining") + ")", intPowerPoints - dblPowerPoints, intPowerPoints);
		}

		/// <summary>
		/// Update the Character information.
		/// </summary>
		public void UpdateCharacterInfo()
		{
			if (_blnLoading)
				return;

			if (!_blnSkipUpdate)
			{
				string strTip = "";
				_blnSkipUpdate = true;

				// If the character is an A.I., set the Edge MetatypeMaximum to their Rating.
				if (_objCharacter.Metatype.EndsWith("A.I.") || _objCharacter.MetatypeCategory == "Technocritters" || _objCharacter.MetatypeCategory == "Protosapients")
					_objCharacter.EDG.MetatypeMaximum = _objCharacter.Rating;

				// Calculate Free Knowledge Skill Points. Free points = (INT + LOG) * 3.
				// Characters built using the Karma system do not get free Knowledge Skills.
				if (_objCharacter.BuildMethod == CharacterBuildMethod.BP || (_objCharacter.BuildMethod == CharacterBuildMethod.Karma && _objOptions.FreeKarmaKnowledge))
					_objCharacter.KnowledgeSkillPoints = (int)(nudINT.Value + nudLOG.Value) * 3;
				else
					_objCharacter.KnowledgeSkillPoints = 0;
				lblKnowledgeSkillPoints.Text = String.Format("{0} " + LanguageManager.Instance.GetString("String_Of") + " {1}", _objCharacter.KnowledgeSkillPoints.ToString(), _objCharacter.KnowledgeSkillPoints.ToString());

				// Update the character's Skill information.
				foreach (SkillControl objSkillControl in panActiveSkills.Controls)
				{
					objSkillControl.SkillRatingMaximum = objSkillControl.SkillObject.RatingMaximum;
					objSkillControl.RefreshControl();
				}

				// Update the character's Knowledge Skill information.
				foreach (SkillControl objSkillControl in panKnowledgeSkills.Controls)
				{
					objSkillControl.SkillRatingMaximum = objSkillControl.SkillObject.RatingMaximum;
					objSkillControl.RefreshControl();
				}

				// Condition Monitor.
				double dblBOD = _objCharacter.BOD.TotalValue;
				double dblWIL = _objCharacter.WIL.TotalValue;
				int intCMPhysical = _objCharacter.PhysicalCM;
				int intCMStun = _objCharacter.StunCM;

				// Update the Condition Monitor labels.
				lblCMPhysical.Text = intCMPhysical.ToString();
				lblCMStun.Text = intCMStun.ToString();
				string strCM = "8 + (BOD/2)(" + ((int)Math.Ceiling(dblBOD / 2)).ToString() + ")";
				if (_objImprovementManager.ValueOf(Improvement.ImprovementType.PhysicalCM) != 0)
					strCM += " + " + LanguageManager.Instance.GetString("Tip_Modifiers") + " (" + _objImprovementManager.ValueOf(Improvement.ImprovementType.PhysicalCM).ToString() + ")";
				tipTooltip.SetToolTip(lblCMPhysical, strCM);
				strCM = "8 + (WIL/2)(" + ((int)Math.Ceiling(dblWIL / 2)).ToString() + ")";
				if (_objImprovementManager.ValueOf(Improvement.ImprovementType.StunCM) != 0)
					strCM += " + " + LanguageManager.Instance.GetString("Tip_Modifiers") + " (" + _objImprovementManager.ValueOf(Improvement.ImprovementType.StunCM).ToString() + ")";
				tipTooltip.SetToolTip(lblCMStun, strCM);

				// Armor Ratings.
				lblBallisticArmor.Text = _objCharacter.TotalBallisticArmorRating.ToString();
				lblImpactArmor.Text = _objCharacter.TotalImpactArmorRating.ToString();
				string strArmorToolTip = "";
				strArmorToolTip = LanguageManager.Instance.GetString("Tip_Armor") + " (" + _objCharacter.BallisticArmorRating.ToString() + ")";
				if (_objCharacter.BallisticArmorRating != _objCharacter.TotalBallisticArmorRating)
					strArmorToolTip += " + " + LanguageManager.Instance.GetString("Tip_Modifiers") + " (" + (_objCharacter.TotalBallisticArmorRating - _objCharacter.BallisticArmorRating).ToString() + ")";
				tipTooltip.SetToolTip(lblBallisticArmor, strArmorToolTip);
				strArmorToolTip = LanguageManager.Instance.GetString("Tip_Armor") + " (" + _objCharacter.ImpactArmorRating.ToString() + ")";
				if (_objCharacter.ImpactArmorRating != _objCharacter.TotalImpactArmorRating)
					strArmorToolTip += " + " + LanguageManager.Instance.GetString("Tip_Modifiers") + " (" + (_objCharacter.TotalImpactArmorRating - _objCharacter.ImpactArmorRating).ToString() + ")";
				tipTooltip.SetToolTip(lblImpactArmor, strArmorToolTip);

				// Remove any Improvements from Armor Encumbrance.
				_objImprovementManager.RemoveImprovements(Improvement.ImprovementSource.ArmorEncumbrance, "Ballistic Encumbrance");
				_objImprovementManager.RemoveImprovements(Improvement.ImprovementSource.ArmorEncumbrance, "Impact Encumbrance");
				// Create the Armor Encumbrance Improvements.
				if (_objCharacter.BallisticArmorEncumbrance < 0)
				{
					_objImprovementManager.CreateImprovement("AGI", Improvement.ImprovementSource.ArmorEncumbrance, "Ballistic Encumbrance", Improvement.ImprovementType.Attribute, "", 0, 1, 0, 0, _objCharacter.BallisticArmorEncumbrance);
					_objImprovementManager.CreateImprovement("REA", Improvement.ImprovementSource.ArmorEncumbrance, "Ballistic Encumbrance", Improvement.ImprovementType.Attribute, "", 0, 1, 0, 0, _objCharacter.BallisticArmorEncumbrance);
				}
				if (_objCharacter.ImpactArmorEncumbrance < 0)
				{
					_objImprovementManager.CreateImprovement("AGI", Improvement.ImprovementSource.ArmorEncumbrance, "Impact Encumbrance", Improvement.ImprovementType.Attribute, "", 0, 1, 0, 0, _objCharacter.ImpactArmorEncumbrance);
					_objImprovementManager.CreateImprovement("REA", Improvement.ImprovementSource.ArmorEncumbrance, "Impact Encumbrance", Improvement.ImprovementType.Attribute, "", 0, 1, 0, 0, _objCharacter.ImpactArmorEncumbrance);
				}

				// Nuyen can be affected by Qualities, so adjust the total amount available to the character.
				if (!_objCharacter.IgnoreRules)
					nudNuyen.Maximum = _objCharacter.NuyenMaximumBP;
				else
					nudNuyen.Maximum = 100000;

				int intNuyen;
				if (_objCharacter.BuildMethod == CharacterBuildMethod.BP)
					intNuyen = Convert.ToInt32(nudNuyen.Value) * _objOptions.NuyenPerBP;
				else
					intNuyen = Convert.ToInt32(nudNuyen.Value) * _objOptions.KarmaNuyenPer;
				intNuyen += Convert.ToInt32(_objImprovementManager.ValueOf(Improvement.ImprovementType.Nuyen));

				lblNuyenTotal.Text = String.Format("= {0:###,###,##0¥}", intNuyen);

				string strFormat;
				if (_objCharacter.Options.EssenceDecimals == 4)
					strFormat = "{0:0.0000}";
				else
					strFormat = "{0:0.00}";
				decimal decESS = _objCharacter.Essence;
				lblESSMax.Text = decESS.ToString();
				tssEssence.Text = string.Format(strFormat, decESS);

				lblCyberwareESS.Text = string.Format(strFormat, _objCharacter.CyberwareEssence);
				lblBiowareESS.Text = string.Format(strFormat, _objCharacter.BiowareEssence);
				lblEssenceHoleESS.Text = string.Format(strFormat, _objCharacter.EssenceHole);

				// Reduce a character's MAG and RES from Essence Loss.
				int intReduction = _objCharacter.ESS.MetatypeMaximum - Convert.ToInt32(Math.Floor(decESS));

				// Remove any Improvements from MAG and RES from Essence Loss.
				_objImprovementManager.RemoveImprovements(Improvement.ImprovementSource.EssenceLoss, "Essence Loss");

				// Create the Essence Loss Improvements which reduce the Maximum of MAG/RES.
				if (intReduction > 0)
				{
					_objImprovementManager.CreateImprovement("MAG", Improvement.ImprovementSource.EssenceLoss, "Essence Loss", Improvement.ImprovementType.Attribute, "", 0, 1, 0, intReduction * -1);
					_objImprovementManager.CreateImprovement("RES", Improvement.ImprovementSource.EssenceLoss, "Essence Loss", Improvement.ImprovementType.Attribute, "", 0, 1, 0, intReduction * -1);
				}

				// If the character is Cyberzombie, adjust their Attributes based on their Essence.
				if (_objCharacter.MetatypeCategory == "Cyberzombie")
				{
					int intESSModifier = _objCharacter.EssencePenalty - Convert.ToInt32(_objCharacter.EssenceMaximum);
					_objImprovementManager.RemoveImprovements(Improvement.ImprovementSource.Cyberzombie, "Cyberzombie Attributes");
					_objImprovementManager.CreateImprovement("BOD", Improvement.ImprovementSource.Cyberzombie, "Cyberzombie Attributes", Improvement.ImprovementType.Attribute, "", 0, 1, 0, intESSModifier);
					_objImprovementManager.CreateImprovement("AGI", Improvement.ImprovementSource.Cyberzombie, "Cyberzombie Attributes", Improvement.ImprovementType.Attribute, "", 0, 1, 0, intESSModifier);
					_objImprovementManager.CreateImprovement("REA", Improvement.ImprovementSource.Cyberzombie, "Cyberzombie Attributes", Improvement.ImprovementType.Attribute, "", 0, 1, 0, intESSModifier);
					_objImprovementManager.CreateImprovement("STR", Improvement.ImprovementSource.Cyberzombie, "Cyberzombie Attributes", Improvement.ImprovementType.Attribute, "", 0, 1, 0, intESSModifier);
					_objImprovementManager.CreateImprovement("CHA", Improvement.ImprovementSource.Cyberzombie, "Cyberzombie Attributes", Improvement.ImprovementType.Attribute, "", 0, 1, 0, intESSModifier);
					_objImprovementManager.CreateImprovement("INT", Improvement.ImprovementSource.Cyberzombie, "Cyberzombie Attributes", Improvement.ImprovementType.Attribute, "", 0, 1, 0, intESSModifier);
					_objImprovementManager.CreateImprovement("LOG", Improvement.ImprovementSource.Cyberzombie, "Cyberzombie Attributes", Improvement.ImprovementType.Attribute, "", 0, 1, 0, intESSModifier);
					_objImprovementManager.CreateImprovement("WIL", Improvement.ImprovementSource.Cyberzombie, "Cyberzombie Attributes", Improvement.ImprovementType.Attribute, "", 0, 1, 0, intESSModifier);
				}

				// Update the Attribute information.
				// Attribute: Body.
				lblBODMetatype.Text = string.Format("{0} / {1} ({2})", _objCharacter.BOD.TotalMinimum, _objCharacter.BOD.TotalMaximum, _objCharacter.BOD.TotalAugmentedMaximum);
				nudBOD.Minimum = _objCharacter.BOD.TotalMinimum;
				nudBOD.Maximum = _objCharacter.BOD.TotalMaximum;
				if (_objCharacter.BOD.HasModifiers)
				{
					lblBODAug.Text = string.Format("({0})", _objCharacter.BOD.TotalValue);
					tipTooltip.SetToolTip(lblBODAug, _objCharacter.BOD.ToolTip());
				}
				else
				{
					lblBODAug.Text = "";
					tipTooltip.SetToolTip(lblBODAug, "");
				}

				// Attribute: Agility.
				lblAGIMetatype.Text = string.Format("{0} / {1} ({2})", _objCharacter.AGI.TotalMinimum, _objCharacter.AGI.TotalMaximum, _objCharacter.AGI.TotalAugmentedMaximum);
				nudAGI.Minimum = _objCharacter.AGI.TotalMinimum;
				nudAGI.Maximum = _objCharacter.AGI.TotalMaximum;
				if (_objCharacter.AGI.HasModifiers)
				{
					lblAGIAug.Text = string.Format("({0})", _objCharacter.AGI.TotalValue);
					tipTooltip.SetToolTip(lblAGIAug, _objCharacter.AGI.ToolTip());
				}
				else
				{
					lblAGIAug.Text = "";
					tipTooltip.SetToolTip(lblAGIAug, "");
				}

				// Attribute: Reaction.
				lblREAMetatype.Text = string.Format("{0} / {1} ({2})", _objCharacter.REA.TotalMinimum, _objCharacter.REA.TotalMaximum, _objCharacter.REA.TotalAugmentedMaximum);
				nudREA.Minimum = _objCharacter.REA.TotalMinimum;
				nudREA.Maximum = _objCharacter.REA.TotalMaximum;
				if (_objCharacter.REA.HasModifiers)
				{
					lblREAAug.Text = string.Format("({0})", _objCharacter.REA.TotalValue);
					tipTooltip.SetToolTip(lblREAAug, _objCharacter.REA.ToolTip());
				}
				else
				{
					lblREAAug.Text = "";
					tipTooltip.SetToolTip(lblREAAug, "");
				}

				// Attribute: Strength.
				lblSTRMetatype.Text = string.Format("{0} / {1} ({2})", _objCharacter.STR.TotalMinimum, _objCharacter.STR.TotalMaximum, _objCharacter.STR.TotalAugmentedMaximum);
				nudSTR.Minimum = _objCharacter.STR.TotalMinimum;
				nudSTR.Maximum = _objCharacter.STR.TotalMaximum;
				if (_objCharacter.STR.HasModifiers)
				{
					lblSTRAug.Text = string.Format("({0})", _objCharacter.STR.TotalValue);
					tipTooltip.SetToolTip(lblSTRAug, _objCharacter.STR.ToolTip());
				}
				else
				{
					lblSTRAug.Text = "";
					tipTooltip.SetToolTip(lblSTRAug, "");
				}

				// Attribute: Charisma.
				lblCHAMetatype.Text = string.Format("{0} / {1} ({2})", _objCharacter.CHA.TotalMinimum, _objCharacter.CHA.TotalMaximum, _objCharacter.CHA.TotalAugmentedMaximum);
				nudCHA.Minimum = _objCharacter.CHA.TotalMinimum;
				nudCHA.Maximum = _objCharacter.CHA.TotalMaximum;
				if (_objCharacter.CHA.HasModifiers)
				{
					lblCHAAug.Text = string.Format("({0})", _objCharacter.CHA.TotalValue);
					tipTooltip.SetToolTip(lblCHAAug, _objCharacter.CHA.ToolTip());
				}
				else
				{
					lblCHAAug.Text = "";
					tipTooltip.SetToolTip(lblCHAAug, "");
				}

				// Attribute: Intuition.
				lblINTMetatype.Text = string.Format("{0} / {1} ({2})", _objCharacter.INT.TotalMinimum, _objCharacter.INT.TotalMaximum, _objCharacter.INT.TotalAugmentedMaximum);
				nudINT.Minimum = _objCharacter.INT.TotalMinimum;
				nudINT.Maximum = _objCharacter.INT.TotalMaximum;
				if (_objCharacter.INT.HasModifiers)
				{
					lblINTAug.Text = string.Format("({0})", _objCharacter.INT.TotalValue);
					tipTooltip.SetToolTip(lblINTAug, _objCharacter.INT.ToolTip());
				}
				else
				{
					lblINTAug.Text = "";
					tipTooltip.SetToolTip(lblINTAug, "");
				}

				// Attribute: Logic.
				lblLOGMetatype.Text = string.Format("{0} / {1} ({2})", _objCharacter.LOG.TotalMinimum, _objCharacter.LOG.TotalMaximum, _objCharacter.LOG.TotalAugmentedMaximum);
				nudLOG.Minimum = _objCharacter.LOG.TotalMinimum;
				nudLOG.Maximum = _objCharacter.LOG.TotalMaximum;
				if (_objCharacter.LOG.HasModifiers)
				{
					lblLOGAug.Text = string.Format("({0})", _objCharacter.LOG.TotalValue);
					tipTooltip.SetToolTip(lblLOGAug, _objCharacter.LOG.ToolTip());
				}
				else
				{
					lblLOGAug.Text = "";
					tipTooltip.SetToolTip(lblLOGAug, "");
				}

				// Attribute: Willpower.
				lblWILMetatype.Text = string.Format("{0} / {1} ({2})", _objCharacter.WIL.TotalMinimum, _objCharacter.WIL.TotalMaximum, _objCharacter.WIL.TotalAugmentedMaximum);
				nudWIL.Minimum = _objCharacter.WIL.TotalMinimum;
				nudWIL.Maximum = _objCharacter.WIL.TotalMaximum;
				if (_objCharacter.WIL.HasModifiers)
				{
					lblWILAug.Text = string.Format("({0})", _objCharacter.WIL.TotalValue);
					tipTooltip.SetToolTip(lblWILAug, _objCharacter.WIL.ToolTip());
				}
				else
				{
					lblWILAug.Text = "";
					tipTooltip.SetToolTip(lblWILAug, "");
				}

				// Attribute: Edge.
				lblEDGMetatype.Text = string.Format("{0} / {1} ({2})", _objCharacter.EDG.TotalMinimum, _objCharacter.EDG.TotalMaximum, _objCharacter.EDG.TotalAugmentedMaximum);
				nudEDG.Minimum = _objCharacter.EDG.TotalMinimum;
				nudEDG.Maximum = _objCharacter.EDG.TotalMaximum;
				if (_objCharacter.EDG.HasModifiers)
				{
					lblEDGAug.Text = string.Format("({0})", _objCharacter.EDG.TotalValue);
					tipTooltip.SetToolTip(lblEDGAug, _objCharacter.EDG.ToolTip());
				}
				else
				{
					lblEDGAug.Text = "";
					tipTooltip.SetToolTip(lblEDGAug, "");
				}

				// Attribute: Magic.
				lblMAGMetatype.Text = string.Format("{0} / {1} ({2})", _objCharacter.MAG.TotalMinimum, _objCharacter.MAG.TotalMaximum, _objCharacter.MAG.TotalAugmentedMaximum);
				nudMAG.Minimum = _objCharacter.MAG.TotalMinimum;
				nudMAG.Maximum = _objCharacter.MAG.TotalMaximum;
				if (_objCharacter.MAG.HasModifiers)
				{
					lblMAGAug.Text = string.Format("({0})", _objCharacter.MAG.TotalValue);
					tipTooltip.SetToolTip(lblMAGAug, _objCharacter.MAG.ToolTip());
				}
				else
				{
					lblMAGAug.Text = "";
					tipTooltip.SetToolTip(lblMAGAug, "");
				}

				// Attribute: Resonance.
				lblRESMetatype.Text = string.Format("{0} / {1} ({2})", _objCharacter.RES.TotalMinimum, _objCharacter.RES.TotalMaximum, _objCharacter.RES.TotalAugmentedMaximum);
				nudRES.Minimum = _objCharacter.RES.TotalMinimum;
				nudRES.Maximum = _objCharacter.RES.TotalMaximum;
				if (_objCharacter.RES.HasModifiers)
				{
					lblRESAug.Text = string.Format("({0})", _objCharacter.RES.TotalValue);
					tipTooltip.SetToolTip(lblRESAug, _objCharacter.RES.ToolTip());
				}
				else
				{
					lblRESAug.Text = "";
					tipTooltip.SetToolTip(lblRESAug, "");
				}

				// Update the MAG pseudo-Attributes if applicable.
				if (_objCharacter.AdeptEnabled && _objCharacter.MagicianEnabled)
				{
					_objCharacter.MAGAdept = Convert.ToInt32(_objCharacter.MAG.TotalValue - nudMysticAdeptMAGMagician.Value);
					lblMysticAdeptMAGAdept.Text = _objCharacter.MAGAdept.ToString();
				}

				// If MAG is enabled, update the Force for Spirits (equal to Magician MAG Rating) and Adept Powers.
				if (_objCharacter.MAGEnabled)
				{
					int intMAG = Convert.ToInt32(_objCharacter.MAG.TotalValue);
					if (_objCharacter.AdeptEnabled && _objCharacter.MagicianEnabled)
						intMAG = _objCharacter.MAGMagician;

					foreach (SpiritControl objSpiritControl in panSpirits.Controls)
					{
						if (_objOptions.SpiritForceBasedOnTotalMAG)
						{
							objSpiritControl.ForceMaximum = _objCharacter.MAG.TotalValue;
							objSpiritControl.Force = _objCharacter.MAG.TotalValue;
						}
						else
						{
							int intLocalMAG = intMAG;
							if (intLocalMAG == 0)
								intLocalMAG = 1;
							
							objSpiritControl.ForceMaximum = intLocalMAG;
							objSpiritControl.Force = intLocalMAG;
						}
						objSpiritControl.RebuildSpiritList(_objCharacter.MagicTradition);
					}

					foreach (PowerControl objPowerControl in panPowers.Controls)
					{
						// Maximum Power Level for Mystic Adepts is based on their total MAG.
						objPowerControl.RefreshMaximum(_objCharacter.MAG.TotalValue);
						objPowerControl.RefreshTooltip();
					}
				}

				// If RES is enabled, update the Rating for Sprites (equal to Technomancer RES Rating).
				if (_objCharacter.RESEnabled)
				{
					foreach (SpiritControl objSpiritControl in panSprites.Controls)
					{
						objSpiritControl.ForceMaximum = _objCharacter.RES.TotalValue;
						objSpiritControl.Force = Convert.ToInt32(_objCharacter.RES.TotalValue);
						objSpiritControl.RebuildSpiritList(_objCharacter.TechnomancerStream);
					}

					// Make sure none of the Copmlex Forms are above the current RES.
					foreach (TechProgram objProgram in _objCharacter.TechPrograms)
					{
						if (objProgram.Rating > _objCharacter.RES.TotalValue)
							objProgram.Rating = _objCharacter.RES.TotalValue;
					}
				}

				// Update the Drain Attribute Value.
				if (_objCharacter.MAGEnabled && lblDrainAttributes.Text != "")
				{
					try
					{
						XmlDocument objXmlDocument = new XmlDocument();
						XPathNavigator nav = objXmlDocument.CreateNavigator();
						string strDrain = lblDrainAttributes.Text.Replace(LanguageManager.Instance.GetString("String_AttributeBODShort"), _objCharacter.BOD.TotalValue.ToString());
						strDrain = strDrain.Replace(LanguageManager.Instance.GetString("String_AttributeAGIShort"), _objCharacter.AGI.TotalValue.ToString());
						strDrain = strDrain.Replace(LanguageManager.Instance.GetString("String_AttributeREAShort"), _objCharacter.REA.TotalValue.ToString());
						strDrain = strDrain.Replace(LanguageManager.Instance.GetString("String_AttributeSTRShort"), _objCharacter.STR.TotalValue.ToString());
						strDrain = strDrain.Replace(LanguageManager.Instance.GetString("String_AttributeCHAShort"), _objCharacter.CHA.TotalValue.ToString());
						strDrain = strDrain.Replace(LanguageManager.Instance.GetString("String_AttributeINTShort"), _objCharacter.INT.TotalValue.ToString());
						strDrain = strDrain.Replace(LanguageManager.Instance.GetString("String_AttributeLOGShort"), _objCharacter.LOG.TotalValue.ToString());
						strDrain = strDrain.Replace(LanguageManager.Instance.GetString("String_AttributeWILShort"), _objCharacter.WIL.TotalValue.ToString());
						strDrain = strDrain.Replace(LanguageManager.Instance.GetString("String_AttributeMAGShort"), _objCharacter.MAG.TotalValue.ToString());
						XPathExpression xprDrain = nav.Compile(strDrain);
						int intDrain = Convert.ToInt32(nav.Evaluate(xprDrain).ToString());
						intDrain += _objImprovementManager.ValueOf(Improvement.ImprovementType.DrainResistance);
						lblDrainAttributesValue.Text = intDrain.ToString();
					}
					catch
					{
					}
				}

				// Update the Fading Attribute Value.
				if (_objCharacter.RESEnabled && lblFadingAttributes.Text != "")
				{
					try
					{
						XmlDocument objXmlDocument = new XmlDocument();
						XPathNavigator nav = objXmlDocument.CreateNavigator();
						string strFading = lblFadingAttributes.Text.Replace("BOD", _objCharacter.BOD.TotalValue.ToString());
						strFading = strFading.Replace("AGI", _objCharacter.AGI.TotalValue.ToString());
						strFading = strFading.Replace("REA", _objCharacter.REA.TotalValue.ToString());
						strFading = strFading.Replace("STR", _objCharacter.STR.TotalValue.ToString());
						strFading = strFading.Replace("CHA", _objCharacter.CHA.TotalValue.ToString());
						strFading = strFading.Replace("INT", _objCharacter.INT.TotalValue.ToString());
						strFading = strFading.Replace("LOG", _objCharacter.LOG.TotalValue.ToString());
						strFading = strFading.Replace("WIL", _objCharacter.WIL.TotalValue.ToString());
						strFading = strFading.Replace("RES", _objCharacter.RES.TotalValue.ToString());
						XPathExpression xprFading = nav.Compile(strFading);
						int intFading = Convert.ToInt32(nav.Evaluate(xprFading).ToString());
						intFading += _objImprovementManager.ValueOf(Improvement.ImprovementType.FadingResistance);
						lblFadingAttributesValue.Text = intFading.ToString();

						strTip = lblFadingAttributes.Text.Replace(LanguageManager.Instance.GetString("String_AttributeBODShort"), LanguageManager.Instance.GetString("String_AttributeBODShort") + " (" + _objCharacter.BOD.TotalValue.ToString() + ")");
						strTip = strTip.Replace(LanguageManager.Instance.GetString("String_AttributeAGIShort"), LanguageManager.Instance.GetString("String_AttributeAGIShort") + " (" + _objCharacter.AGI.TotalValue.ToString() + ")");
						strTip = strTip.Replace(LanguageManager.Instance.GetString("String_AttributeREAShort"), LanguageManager.Instance.GetString("String_AttributeREAShort") + " (" + _objCharacter.REA.TotalValue.ToString() + ")");
						strTip = strTip.Replace(LanguageManager.Instance.GetString("String_AttributeSTRShort"), LanguageManager.Instance.GetString("String_AttributeSTRShort") + " (" + _objCharacter.STR.TotalValue.ToString() + ")");
						strTip = strTip.Replace(LanguageManager.Instance.GetString("String_AttributeCHAShort"), LanguageManager.Instance.GetString("String_AttributeCHAShort") + " (" + _objCharacter.CHA.TotalValue.ToString() + ")");
						strTip = strTip.Replace(LanguageManager.Instance.GetString("String_AttributeINTShort"), LanguageManager.Instance.GetString("String_AttributeINTShort") + " (" + _objCharacter.INT.TotalValue.ToString() + ")");
						strTip = strTip.Replace(LanguageManager.Instance.GetString("String_AttributeLOGShort"), LanguageManager.Instance.GetString("String_AttributeLOGShort") + " (" + _objCharacter.LOG.TotalValue.ToString() + ")");
						strTip = strTip.Replace(LanguageManager.Instance.GetString("String_AttributeWILShort"), LanguageManager.Instance.GetString("String_AttributeWILShort") + " (" + _objCharacter.WIL.TotalValue.ToString() + ")");
						strTip = strTip.Replace(LanguageManager.Instance.GetString("String_AttributeRESShort"), LanguageManager.Instance.GetString("String_AttributeRESShort") + " (" + _objCharacter.RES.TotalValue.ToString() + ")");
						tipTooltip.SetToolTip(lblFadingAttributesValue, strTip);
					}
					catch
					{
					}
				}

				// Update Living Persona values.
				if (_objCharacter.RESEnabled)
				{
					string strPersonaTip = "";
					int intFirewall = _objCharacter.WIL.TotalValue + _objImprovementManager.ValueOf(Improvement.ImprovementType.LivingPersonaFirewall);
					int intResponse = _objCharacter.INT.TotalValue + _objImprovementManager.ValueOf(Improvement.ImprovementType.LivingPersonaResponse);
					int intSignal = Convert.ToInt32(Math.Ceiling((Convert.ToDecimal(_objCharacter.RES.TotalValue, GlobalOptions.Instance.CultureInfo) / 2))) + _objImprovementManager.ValueOf(Improvement.ImprovementType.LivingPersonaSignal);
					int intSystem = _objCharacter.LOG.TotalValue + _objImprovementManager.ValueOf(Improvement.ImprovementType.LivingPersonaSystem);
					int intBiofeedback = _objCharacter.CHA.TotalValue + _objImprovementManager.ValueOf(Improvement.ImprovementType.LivingPersonaBiofeedback);

					// If this is a Technocritter, their Matrix Attributes always equal their RES.
					if (_objCharacter.MetatypeCategory == "Technocritters")
					{
						intFirewall = _objCharacter.RES.TotalValue;
						intSystem = _objCharacter.RES.TotalValue;
						intResponse = _objCharacter.RES.TotalValue;
						intSignal = _objCharacter.RES.TotalValue;
						intBiofeedback = _objCharacter.RES.TotalValue;
					}

					// Make sure none of the Attributes exceed the Technomancer's RES.
					intFirewall = Math.Min(intFirewall, _objCharacter.RES.TotalValue);
					intResponse = Math.Min(intResponse, _objCharacter.RES.TotalValue);
					intSignal = Math.Min(intSignal, _objCharacter.RES.TotalValue);
					intSystem = Math.Min(intSystem, _objCharacter.RES.TotalValue);

					lblLivingPersonaFirewall.Text = intFirewall.ToString();
					if (_objCharacter.MetatypeCategory != "Technocritters")
						strPersonaTip = "WIL (" + _objCharacter.WIL.TotalValue.ToString() + ")";
					else
						strPersonaTip = "RES (" + _objCharacter.RES.TotalValue.ToString() + ")";
					if (_objImprovementManager.ValueOf(Improvement.ImprovementType.LivingPersonaFirewall) != 0)
						strPersonaTip += " + " + LanguageManager.Instance.GetString("Tip_Modifiers") + " (" + _objImprovementManager.ValueOf(Improvement.ImprovementType.LivingPersonaFirewall).ToString() + ")";
					tipTooltip.SetToolTip(lblLivingPersonaFirewall, strPersonaTip);

					lblLivingPersonaResponse.Text = intResponse.ToString();
					if (_objCharacter.MetatypeCategory != "Technocritters")
						strPersonaTip = "INT (" + _objCharacter.INT.TotalValue.ToString() + ")";
					else
						strPersonaTip = "RES (" + _objCharacter.RES.TotalValue.ToString() + ")";
					if (_objImprovementManager.ValueOf(Improvement.ImprovementType.LivingPersonaResponse) != 0)
						strPersonaTip += " + " + LanguageManager.Instance.GetString("Tip_Modifiers") + " (" + _objImprovementManager.ValueOf(Improvement.ImprovementType.LivingPersonaResponse).ToString() + ")";
					tipTooltip.SetToolTip(lblLivingPersonaResponse, strPersonaTip);

					lblLivingPersonaSignal.Text = intSignal.ToString();
					if (_objCharacter.MetatypeCategory != "Technocritters")
						strPersonaTip = "RES/2 (" + Convert.ToInt32(Math.Ceiling((Convert.ToDecimal(_objCharacter.RES.TotalValue, GlobalOptions.Instance.CultureInfo) / 2))).ToString() + ")";
					else
						strPersonaTip = "RES (" + _objCharacter.RES.TotalValue.ToString() + ")";
					if (_objImprovementManager.ValueOf(Improvement.ImprovementType.LivingPersonaSignal) != 0)
						strPersonaTip += " + " + LanguageManager.Instance.GetString("Tip_Modifiers") + " (" + _objImprovementManager.ValueOf(Improvement.ImprovementType.LivingPersonaSignal).ToString() + ")";
					tipTooltip.SetToolTip(lblLivingPersonaSignal, strPersonaTip);

					lblLivingPersonaSystem.Text = intSystem.ToString();
					if (_objCharacter.MetatypeCategory != "Technocritters")
						strPersonaTip = "LOG (" + _objCharacter.LOG.TotalValue.ToString() + ")";
					else
						strPersonaTip = "RES (" + _objCharacter.RES.TotalValue.ToString() + ")";
					if (_objImprovementManager.ValueOf(Improvement.ImprovementType.LivingPersonaSystem) != 0)
						strPersonaTip += " + " + LanguageManager.Instance.GetString("Tip_Modifiers") + " (" + _objImprovementManager.ValueOf(Improvement.ImprovementType.LivingPersonaSystem).ToString() + ")";
					tipTooltip.SetToolTip(lblLivingPersonaSystem, strPersonaTip);

					lblLivingPersonaBiofeedbackFilter.Text = intBiofeedback.ToString();
					if (_objCharacter.MetatypeCategory != "Technocritters")
						strPersonaTip = "CHA (" + _objCharacter.CHA.TotalValue.ToString() + ")";
					else
						strPersonaTip = "RES (" + _objCharacter.RES.TotalValue.ToString() + ")";
					if (_objImprovementManager.ValueOf(Improvement.ImprovementType.LivingPersonaBiofeedback) != 0)
						strPersonaTip += " + " + LanguageManager.Instance.GetString("Tip_Modifiers") + " (" + _objImprovementManager.ValueOf(Improvement.ImprovementType.LivingPersonaBiofeedback).ToString() + ")";
					tipTooltip.SetToolTip(lblLivingPersonaBiofeedbackFilter, strPersonaTip);
				}

				// Initiative.
				lblINI.Text = _objCharacter.Initiative;
				string strInit = "REA (" + _objCharacter.REA.Value.ToString() + ") + INT (" + _objCharacter.INT.Value.ToString() + ")";
				if (_objCharacter.INI.AttributeModifiers > 0 || _objImprovementManager.ValueOf(Improvement.ImprovementType.Initiative) > 0 || _objCharacter.INT.AttributeModifiers > 0 || _objCharacter.REA.AttributeModifiers > 0)
					strInit += " + " + LanguageManager.Instance.GetString("Tip_Modifiers") + " (" + (_objCharacter.INI.AttributeModifiers + _objImprovementManager.ValueOf(Improvement.ImprovementType.Initiative) + _objCharacter.INT.AttributeModifiers + _objCharacter.REA.AttributeModifiers).ToString() + ")";
				tipTooltip.SetToolTip(lblINI, strInit);

				// Initiative Passes.
				lblIP.Text = _objCharacter.InitiativePasses;
				string strIPTip = "";
				strIPTip = "1";
				if (Convert.ToInt32(_objImprovementManager.ValueOf(Improvement.ImprovementType.InitiativePass)) > 0)
					strIPTip += " + " + LanguageManager.Instance.GetString("Tip_Modifiers") + " (" + _objImprovementManager.ValueOf(Improvement.ImprovementType.InitiativePass).ToString() + ")";
				tipTooltip.SetToolTip(lblIP, strIPTip);

				// Astral Initiative.
				if (_objCharacter.MAGEnabled)
				{
					lblAstralINI.Text = _objCharacter.AstralInitiative;
					lblAstralIP.Text = _objCharacter.AstralInitiativePasses;
					tipTooltip.SetToolTip(lblAstralINI, "INT (" + _objCharacter.INT.TotalValue.ToString() + ") x 2");
					tipTooltip.SetToolTip(lblAstralIP, "3");
				}

				// Matrix Initiative.
				int intCommlinkResponse = 0;

				// Retrieve the highest Response in case the Character has more than 1 Commlink.
				foreach (Commlink objCommlink in _objCharacter.Gear.OfType<Commlink>())
				{
					if (objCommlink.TotalResponse > intCommlinkResponse)
						intCommlinkResponse = objCommlink.TotalResponse;
				}

				lblMatrixINI.Text = _objCharacter.MatrixInitiative;
				lblMatrixIP.Text = _objCharacter.MatrixInitiativePasses;
				if (!_objCharacter.TechnomancerEnabled)
				{
					tipTooltip.SetToolTip(lblMatrixINI, "INT (" + _objCharacter.INT.TotalValue.ToString() + ") + " + LanguageManager.Instance.GetString("Tip_CommlinkResponse") + " (" + intCommlinkResponse.ToString() + ")");
					strIPTip = "1";
					if (_objImprovementManager.ValueOf(Improvement.ImprovementType.MatrixInitiativePass) > 0)
						strIPTip += " + " + LanguageManager.Instance.GetString("Tip_Modifiers") + " (" + _objImprovementManager.ValueOf(Improvement.ImprovementType.MatrixInitiativePass).ToString() + ")";
					tipTooltip.SetToolTip(lblMatrixIP, strIPTip);
				}
				else
				{
					strInit = "INT x 2 (" + _objCharacter.INT.TotalValue.ToString() + ") + 1";
					if (_objImprovementManager.ValueOf(Improvement.ImprovementType.LivingPersonaResponse) > 0)
						strInit += " + " + LanguageManager.Instance.GetString("Tip_Modifiers") + " (" + _objImprovementManager.ValueOf(Improvement.ImprovementType.LivingPersonaResponse).ToString() + ")";
					tipTooltip.SetToolTip(lblMatrixINI, strInit);
					strIPTip = "3";
					if (_objImprovementManager.ValueOf(Improvement.ImprovementType.MatrixInitiativePass) > 0)
						strIPTip += " + " + LanguageManager.Instance.GetString("Tip_Modifiers") + " (" + _objImprovementManager.ValueOf(Improvement.ImprovementType.MatrixInitiativePass).ToString() + ")";
					tipTooltip.SetToolTip(lblMatrixIP, strIPTip);
				}

				// Calculate the number of Build Points remaining.
				CalculateBP();
				CalculateNuyen();
				if (_objCharacter.AdeptEnabled)
					CalculatePowerPoints();
				if ((_objCharacter.Metatype == "Free Spirit" && !_objCharacter.IsCritter) || _objCharacter.MetatypeCategory.EndsWith("Spirits"))
					CalculateFreeSpiritPowerPoints();
				if (_objCharacter.IsFreeSprite)
					CalculateFreeSpritePowerPoints();

				// Movement.
				lblMovement.Text = _objCharacter.Movement;
				lblSwim.Text = _objCharacter.Swim;
				lblFly.Text = _objCharacter.Fly;

				// Special Attribute-Only Test.
				lblComposure.Text = _objCharacter.Composure.ToString();
				strTip = "WIL (" + _objCharacter.WIL.TotalValue.ToString() + ") + CHA (" + _objCharacter.CHA.TotalValue.ToString() + ")";
				tipTooltip.SetToolTip(lblComposure, strTip);
				lblJudgeIntentions.Text = _objCharacter.JudgeIntentions.ToString();
				strTip = "INT (" + _objCharacter.INT.TotalValue.ToString() + ") + CHA (" + _objCharacter.CHA.TotalValue.ToString() + ")";
				if (_objImprovementManager.ValueOf(Improvement.ImprovementType.JudgeIntentions) != 0)
					strTip += " + " + LanguageManager.Instance.GetString("Tip_Modifiers") + " (" + _objImprovementManager.ValueOf(Improvement.ImprovementType.JudgeIntentions).ToString() + ")";
				tipTooltip.SetToolTip(lblJudgeIntentions, strTip);
				lblLiftCarry.Text = _objCharacter.LiftAndCarry.ToString();
				strTip = "STR (" + _objCharacter.STR.TotalValue.ToString() + ") + BOD (" + _objCharacter.BOD.TotalValue.ToString() + ")";
				if (_objImprovementManager.ValueOf(Improvement.ImprovementType.LiftAndCarry) != 0)
					strTip += " + " + LanguageManager.Instance.GetString("Tip_Modifiers") + " (" + _objImprovementManager.ValueOf(Improvement.ImprovementType.LiftAndCarry).ToString() + ")";
				strTip += "\n" + LanguageManager.Instance.GetString("Tip_LiftAndCarry").Replace("{0}", (_objCharacter.STR.TotalValue * 15).ToString()).Replace("{1}", (_objCharacter.STR.TotalValue * 10).ToString());
				tipTooltip.SetToolTip(lblLiftCarry, strTip);
				lblMemory.Text = _objCharacter.Memory.ToString();
				strTip = "LOG (" + _objCharacter.LOG.TotalValue.ToString() + ") + WIL (" + _objCharacter.WIL.TotalValue.ToString() + ")";
				if (_objImprovementManager.ValueOf(Improvement.ImprovementType.Memory) != 0)
					strTip += " + " + LanguageManager.Instance.GetString("Tip_Modifiers") + " (" + _objImprovementManager.ValueOf(Improvement.ImprovementType.Memory).ToString() + ")";
				tipTooltip.SetToolTip(lblMemory, strTip);

				// Update Initiate Grade if applicable.
				if (_objCharacter.MAGEnabled)
					lblInitiateGrade.Text = _objCharacter.InitiateGrade.ToString();
				if (_objCharacter.RESEnabled)
					lblInitiateGrade.Text = _objCharacter.SubmersionGrade.ToString();

				// Update A.I. Attributes.
				if (_objCharacter.Metatype.EndsWith("A.I.") || _objCharacter.MetatypeCategory == "Technocritters" || _objCharacter.MetatypeCategory == "Protosapients")
				{
					lblRating.Text = _objCharacter.Rating.ToString();
					lblSystem.Text = _objCharacter.System.ToString();
					lblFirewall.Text = _objCharacter.Firewall.ToString();
				}

				// If this is a Mutant Critter, determine their Essence loss: -1 for each of: Every 2 points added to a Skill, Each Quality (or Quality Rating). This can be offset by a Negative Quality (or Quality Rating)
				if (_objCharacter.MetatypeCategory == "Mutant Critters")
				{
					// Remove any current Essence Improvements from MutantCritter.
					_objImprovementManager.RemoveImprovements(Improvement.ImprovementSource.MutantCritter, "");

					int intEssencePenalty = 0;
					int intIntuitionPenalty = 0;
					int intAttributeDiff = 0;
					int intQualityPoints = 0;
					int intSkillPoints = 0;

					intAttributeDiff += (_objCharacter.BOD.Value - _objCharacter.BOD.MetatypeMinimum);
					intAttributeDiff += (_objCharacter.AGI.Value - _objCharacter.AGI.MetatypeMinimum);
					intAttributeDiff += (_objCharacter.REA.Value - _objCharacter.REA.MetatypeMinimum);
					intAttributeDiff += (_objCharacter.STR.Value - _objCharacter.STR.MetatypeMinimum);
					intAttributeDiff += (_objCharacter.CHA.Value - _objCharacter.CHA.MetatypeMinimum);
					intAttributeDiff += (_objCharacter.INT.Value - _objCharacter.INT.MetatypeMinimum);
					intAttributeDiff += (_objCharacter.LOG.Value - _objCharacter.LOG.MetatypeMinimum);
					intAttributeDiff += (_objCharacter.WIL.Value - _objCharacter.WIL.MetatypeMinimum);
					intAttributeDiff += (_objCharacter.EDG.Value - _objCharacter.EDG.MetatypeMinimum);
					intAttributeDiff += (_objCharacter.MAG.Value - _objCharacter.MAG.MetatypeMinimum);
					intAttributeDiff += (_objCharacter.RES.Value - _objCharacter.RES.MetatypeMinimum);

					// -1 Essence for every 2 points spent on Attributes.
					intEssencePenalty += Convert.ToInt32(Math.Ceiling(Convert.ToDouble(intAttributeDiff, GlobalOptions.Instance.CultureInfo) / 2));

					// Run through the Qualities the Critter has and add up their Mutant Points.
					foreach (Quality objQuality in _objCharacter.Qualities)
					{
						if (objQuality.OriginSource != QualitySource.Metatype && objQuality.OriginSource != QualitySource.MetatypeRemovable)
							intQualityPoints += objQuality.MutantPoints;
					}
					// Negative Qualities can only offset the cost of Positive Qualities, so set this back to 0 if there are more Negative than Positive.
					if (intQualityPoints < 0)
						intQualityPoints = 0;

					// Run through the Skills the Critter has.
					foreach (Skill objSkill in _objCharacter.Skills)
						intSkillPoints += objSkill.Rating;

					// Subtract the number fo Skill points the Critter had when it mutated.
					intSkillPoints -= _objCharacter.MutantCritterBaseSkills;

					// Make sure this doesn't go below 0.
					if (intSkillPoints < 0)
						intSkillPoints = 0;

					// Every 2 points causes another point of Essence loss.
					intEssencePenalty += Convert.ToInt32(Math.Ceiling(Convert.ToDouble(intSkillPoints, GlobalOptions.Instance.CultureInfo) / 2));

					intEssencePenalty += intQualityPoints;

					// Essence cannot go below 1 from these changes, so make sure we don't go over ESS - 1.
					if (intEssencePenalty > _objCharacter.ESS.MetatypeMaximum - 1)
						intEssencePenalty = _objCharacter.ESS.MetatypeMaximum - 1;
					// INT is reduced for every 2 points of ESS lost to a minimum of 1.
					intIntuitionPenalty = Convert.ToInt32(Math.Ceiling(Convert.ToDouble(intEssencePenalty, GlobalOptions.Instance.CultureInfo) / 2));
					if (_objCharacter.INT.TotalValue - intIntuitionPenalty < 1)
						intIntuitionPenalty = _objCharacter.INT.TotalValue - 1;

					// Now that everything is calculated, create the Essence Loss and Intuition Penalty Improvements.
					intEssencePenalty *= -1;
					intIntuitionPenalty *= -1;
					if (intEssencePenalty < 0)
						_objImprovementManager.CreateImprovement("", Improvement.ImprovementSource.MutantCritter, "", Improvement.ImprovementType.Essence, "", intEssencePenalty);
					if (intIntuitionPenalty < 0)
						_objImprovementManager.CreateImprovement("INT", Improvement.ImprovementSource.MutantCritter, "", Improvement.ImprovementType.Attribute, "", 0, 1, 0, 0, intIntuitionPenalty);
					if (_objCharacter.INT.TotalValue != nudINT.Value)
						lblINTAug.Text = string.Format("({0})", _objCharacter.INT.TotalValue);
					else
						lblINTAug.Text = "";

					// Refresh the Essence values.
					decESS = _objCharacter.Essence;
					lblESSMax.Text = decESS.ToString();
					tssEssence.Text = string.Format("{0:0.00}", decESS);
				}

				_blnSkipUpdate = false;

				_objImprovementManager.Commit();

				// If the Viewer window is open for this character, call its RefreshView method which updates it asynchronously
				if (_objCharacter.PrintWindow != null)
					_objCharacter.PrintWindow.RefreshView();
			}
			RefreshImprovements();
			UpdateReputation();
		}

		/// <summary>
		/// Calculate the amount of Nuyen the character has remaining.
		/// </summary>
		private int CalculateNuyen()
		{
			int intNuyen;
			if (_objCharacter.BuildMethod == CharacterBuildMethod.BP)
				intNuyen = Convert.ToInt32(nudNuyen.Value) * _objOptions.NuyenPerBP;
			else
				intNuyen = Convert.ToInt32(nudNuyen.Value) * _objOptions.KarmaNuyenPer;
			intNuyen += Convert.ToInt32(_objImprovementManager.ValueOf(Improvement.ImprovementType.Nuyen));

			int intDeductions = 0;

			// Cyberware/Bioware cost.
			foreach (Cyberware objCyberware in _objCharacter.Cyberware)
				intDeductions += objCyberware.TotalCost;

			// Armor cost.
			foreach (Armor objArmor in _objCharacter.Armor)
				intDeductions += objArmor.TotalCost;

			// Weapon cost.
			foreach (Weapon objWeapon in _objCharacter.Weapons)
				intDeductions += objWeapon.TotalCost;

			// Gear cost.
			foreach (Gear objGear in _objCharacter.Gear)
				intDeductions += objGear.TotalCost;

			// Lifestyle cost.
			foreach (Lifestyle objLifestyle in _objCharacter.Lifestyles)
				intDeductions += objLifestyle.TotalCost;

			// Vehicle cost.
			foreach (Vehicle objVehcile in _objCharacter.Vehicles)
				intDeductions += objVehcile.TotalCost;
			
			lblRemainingNuyen.Text = String.Format("{0:###,###,##0¥}", intNuyen - intDeductions);
			tssNuyenRemaining.Text = String.Format("{0:###,###,##0¥}", intNuyen - intDeductions);

			return intNuyen - intDeductions;
		}

		/// <summary>
		/// Refresh the information for the currently displayed piece of Cyberware.
		/// </summary>
		public void RefreshSelectedCyberware()
		{
			bool blnClear = false;
			try
			{
				if (treCyberware.SelectedNode.Level == 0)
					blnClear = true;
			}
			catch
			{
				blnClear = true;
			}
			if (blnClear)
			{
				nudCyberwareRating.Enabled = false;
				cboCyberwareGrade.Enabled = false;
				lblCyberwareName.Text = "";
				lblCyberwareCategory.Text = "";
				lblCyberwareAvail.Text = "";
				lblCyberwareCost.Text = "";
				lblCyberwareCapacity.Text = "";
				lblCyberwareEssence.Text = "";
				lblCyberwareSource.Text = "";
				tipTooltip.SetToolTip(lblCyberwareSource, null);
				return;
			}

			// Locate the selected piece of Cyberware.
			bool blnChild = false;
			bool blnFound = false;
			Cyberware objCyberware = _objFunctions.FindCyberware(treCyberware.SelectedNode.Tag.ToString(), _objCharacter.Cyberware);
			if (objCyberware != null)
			{
				blnFound = true;
				if (objCyberware.Parent != null)
					blnChild = true;
			}

			if (blnFound)
			{
				_blnSkipRefresh = true;
				lblCyberwareName.Text = objCyberware.DisplayNameShort;
				lblCyberwareCategory.Text = objCyberware.DisplayCategory;
				string strBook = _objOptions.LanguageBookShort(objCyberware.Source);
				string strPage = objCyberware.Page;
				lblCyberwareSource.Text = strBook + " " + strPage;
				tipTooltip.SetToolTip(lblCyberwareSource, _objOptions.LanguageBookLong(objCyberware.Source) + " " + LanguageManager.Instance.GetString("String_Page") + " " + objCyberware.Page);
				// Enable and set the Rating values as needed.
				if (objCyberware.Rating == 0)
				{
					nudCyberwareRating.Maximum = Convert.ToDecimal(objCyberware.MaxRating, GlobalOptions.Instance.CultureInfo);
					nudCyberwareRating.Minimum = 0;
					nudCyberwareRating.Value = Convert.ToDecimal(objCyberware.Rating, GlobalOptions.Instance.CultureInfo);
					nudCyberwareRating.Enabled = false;
				}
				else
				{
					nudCyberwareRating.Maximum = Convert.ToDecimal(objCyberware.MaxRating, GlobalOptions.Instance.CultureInfo);
					nudCyberwareRating.Minimum = Convert.ToDecimal(objCyberware.MinRating, GlobalOptions.Instance.CultureInfo);
					nudCyberwareRating.Value = Convert.ToDecimal(objCyberware.Rating, GlobalOptions.Instance.CultureInfo);
					nudCyberwareRating.Enabled = true;
				}

				bool blnIgnoreSecondHand = false;
				if (objCyberware.Category == "Cultured")
					blnIgnoreSecondHand = true;
				PopulateCyberwareGradeList(objCyberware.SourceType == Improvement.ImprovementSource.Bioware, blnIgnoreSecondHand);

				cboCyberwareGrade.SelectedValue = objCyberware.Grade.Name;

				chkCyberwareBlackMarketDiscount.Checked = objCyberware.DiscountCost;

				// Cyberware Grade is only available on root-level items (sub-components cannot have a different Grade than the piece they belong to).
				if (!blnChild)
					if (!objCyberware.Suite)
						cboCyberwareGrade.Enabled = true;
					else
						cboCyberwareGrade.Enabled = false;
				else
					cboCyberwareGrade.Enabled = false;

				// Cyberware Grade is not available for Genetech items.
				if (objCyberware.Category.StartsWith("Genetech:") || objCyberware.Category == "Symbiont" || objCyberware.Category == "Genetic Infusions")
					cboCyberwareGrade.Enabled = false;

				_blnSkipRefresh = false;

				lblCyberwareAvail.Text = objCyberware.TotalAvail;
				lblCyberwareCost.Text = String.Format("{0:###,###,##0¥}", objCyberware.TotalCost);
				lblCyberwareCapacity.Text = objCyberware.CalculatedCapacity + " (" + objCyberware.CapacityRemaining.ToString() + " " + LanguageManager.Instance.GetString("String_Remaining") + ")";
				lblCyberwareEssence.Text = objCyberware.CalculatedESS.ToString();
				UpdateCharacterInfo();
			}
			else
			{
				// Locate the piece of Gear.
				Cyberware objFoundCyberware = new Cyberware(_objCharacter);
				Gear objGear = _objFunctions.FindCyberwareGear(treCyberware.SelectedNode.Tag.ToString(), _objCharacter.Cyberware, out objFoundCyberware);

				_blnSkipRefresh = true;
				lblCyberwareName.Text = objGear.DisplayNameShort;
				lblCyberwareCategory.Text = objGear.DisplayCategory;
				lblCyberwareAvail.Text = objGear.TotalAvail(true);
				lblCyberwareCost.Text = String.Format("{0:###,###,##0¥}", objGear.TotalCost);
				lblCyberwareCapacity.Text = objGear.CalculatedCapacity + " (" + objGear.CapacityRemaining.ToString() + " " + LanguageManager.Instance.GetString("String_Remaining") + ")";
				lblCyberwareEssence.Text = "0";
				cboCyberwareGrade.Enabled = false;
				chkCyberwareBlackMarketDiscount.Checked = objGear.DiscountCost;
				string strBook = _objOptions.LanguageBookShort(objGear.Source);
				string strPage = objGear.Page;
				lblCyberwareSource.Text = strBook + " " + strPage;
				tipTooltip.SetToolTip(lblCyberwareSource, _objOptions.LanguageBookLong(objGear.Source) + " " + LanguageManager.Instance.GetString("String_Page") + " " + objGear.Page);

				if (objGear.MaxRating > 0)
				{
					if (objGear.MinRating > 0)
						nudCyberwareRating.Minimum = objGear.MinRating;
					else if (objGear.MinRating == 0 && objGear.Name.Contains("Credstick,"))
						nudCyberwareRating.Minimum = 0;
					else
						nudCyberwareRating.Minimum = 1;
					nudCyberwareRating.Maximum = objGear.MaxRating;
					nudCyberwareRating.Value = objGear.Rating;
					if (nudCyberwareRating.Minimum == nudCyberwareRating.Maximum)
						nudCyberwareRating.Enabled = false;
					else
						nudCyberwareRating.Enabled = true;
				}
				else
				{
					nudCyberwareRating.Minimum = 0;
					nudCyberwareRating.Maximum = 0;
					nudCyberwareRating.Enabled = false;
				}
				
				_blnSkipRefresh = false;
			}
		}

		/// <summary>
		/// Refresh the information for the currently displayed Weapon.
		/// </summary>
		public void RefreshSelectedWeapon()
		{
			bool blnClear = false;
			try
			{
				if (treWeapons.SelectedNode.Level == 0)
					blnClear = true;
			}
			catch
			{
				blnClear = true;
			}
			if (blnClear)
			{
				lblWeaponName.Text = "";
				lblWeaponCategory.Text = "";
				lblWeaponAvail.Text = "";
				lblWeaponCost.Text = "";
				lblWeaponConceal.Text = "";
				lblWeaponDamage.Text = "";
				lblWeaponRC.Text = "";
				lblWeaponAP.Text = "";
				lblWeaponReach.Text = "";
				lblWeaponMode.Text = "";
				lblWeaponAmmo.Text = "";
				lblWeaponSource.Text = "";
				tipTooltip.SetToolTip(lblWeaponSource, null);
				chkWeaponAccessoryInstalled.Enabled = false;
				chkIncludedInWeapon.Enabled = false;
				chkIncludedInWeapon.Checked = false;

				// Hide Weapon Ranges.
				lblWeaponRangeShort.Text = "";
				lblWeaponRangeMedium.Text = "";
				lblWeaponRangeLong.Text = "";
				lblWeaponRangeExtreme.Text = "";
				return;
			}
			
			lblWeaponDicePool.Text = "";
			tipTooltip.SetToolTip(lblWeaponDicePool, "");

			// Locate the selected Weapon.
			if (treWeapons.SelectedNode.Level == 1)
			{
				Weapon objWeapon = _objFunctions.FindWeapon(treWeapons.SelectedNode.Tag.ToString(), _objCharacter.Weapons);
				if (objWeapon == null)
					return;

				// If this is a Cyberweapon, grab the STR of the limb.
				int intUseSTR = 0;
				if (objWeapon.Category.StartsWith("Cyberware"))
				{
					foreach (Cyberware objCyberware in _objCharacter.Cyberware)
					{
						foreach (Cyberware objPlugin in objCyberware.Children)
						{
							if (objPlugin.WeaponID == objWeapon.InternalId)
							{
								intUseSTR = objCyberware.TotalStrength;
								break;
							}
						}
					}
				}

				_blnSkipRefresh = true;
				lblWeaponName.Text = objWeapon.DisplayNameShort;
				lblWeaponCategory.Text = objWeapon.DisplayCategory;
				string strBook = _objOptions.LanguageBookShort(objWeapon.Source);
				string strPage = objWeapon.Page;
				lblWeaponSource.Text = strBook + " " + strPage;
				tipTooltip.SetToolTip(lblWeaponSource, _objOptions.LanguageBookLong(objWeapon.Source) + " " + LanguageManager.Instance.GetString("String_Page") + " " + objWeapon.Page);
				chkWeaponAccessoryInstalled.Enabled = false;
				chkIncludedInWeapon.Enabled = false;
				chkIncludedInWeapon.Checked = false;
				chkWeaponBlackMarketDiscount.Checked = objWeapon.DiscountCost;

				// Show the Weapon Ranges.
				lblWeaponRangeShort.Text = objWeapon.RangeShort;
				lblWeaponRangeMedium.Text = objWeapon.RangeMedium;
				lblWeaponRangeLong.Text = objWeapon.RangeLong;
				lblWeaponRangeExtreme.Text = objWeapon.RangeExtreme;
				_blnSkipRefresh = false;

				lblWeaponAvail.Text = objWeapon.TotalAvail;
				lblWeaponCost.Text = String.Format("{0:###,###,##0¥}", objWeapon.TotalCost);
				lblWeaponConceal.Text = objWeapon.CalculatedConcealability();
				lblWeaponDamage.Text = objWeapon.CalculatedDamage(intUseSTR);
				lblWeaponRC.Text = objWeapon.TotalRC;
				lblWeaponAP.Text = objWeapon.TotalAP;
				lblWeaponReach.Text = objWeapon.TotalReach.ToString();
				lblWeaponMode.Text = objWeapon.CalculatedMode;
				lblWeaponAmmo.Text = objWeapon.CalculatedAmmo();
				lblWeaponSlots.Text = "6 (" + objWeapon.SlotsRemaining.ToString() + " " + LanguageManager.Instance.GetString("String_Remaining") + ")";
				lblWeaponDicePool.Text = objWeapon.DicePool;
				tipTooltip.SetToolTip(lblWeaponDicePool, objWeapon.DicePoolTooltip);

				UpdateCharacterInfo();
			}
			else
			{
				// See if this is an Underbarrel Weapon.
				bool blnUnderbarrel = false;
				Weapon objWeapon = _objFunctions.FindWeapon(treWeapons.SelectedNode.Tag.ToString(), _objCharacter.Weapons);
				if (objWeapon != null)
				{
					if (objWeapon.IsUnderbarrelWeapon)
						blnUnderbarrel = true;
				}

				if (blnUnderbarrel)
				{
					_blnSkipRefresh = true;
					lblWeaponName.Text = objWeapon.DisplayNameShort;
					lblWeaponCategory.Text = objWeapon.DisplayCategory;
					string strBook = _objOptions.LanguageBookShort(objWeapon.Source);
					string strPage = objWeapon.Page;
					lblWeaponSource.Text = strBook + " " + strPage;
					tipTooltip.SetToolTip(lblWeaponSource, _objOptions.LanguageBookLong(objWeapon.Source) + " " + LanguageManager.Instance.GetString("String_Page") + " " + objWeapon.Page);
					chkWeaponAccessoryInstalled.Enabled = true;
					chkWeaponAccessoryInstalled.Checked = objWeapon.Installed;
					chkIncludedInWeapon.Enabled = false;
					chkIncludedInWeapon.Checked = objWeapon.IncludedInWeapon;
					chkWeaponBlackMarketDiscount.Checked = objWeapon.DiscountCost;

					// Show the Weapon Ranges.
					lblWeaponRangeShort.Text = objWeapon.RangeShort;
					lblWeaponRangeMedium.Text = objWeapon.RangeMedium;
					lblWeaponRangeLong.Text = objWeapon.RangeLong;
					lblWeaponRangeExtreme.Text = objWeapon.RangeExtreme;
					_blnSkipRefresh = false;

					lblWeaponAvail.Text = objWeapon.TotalAvail;
					lblWeaponCost.Text = String.Format("{0:###,###,##0¥}", objWeapon.TotalCost);
					lblWeaponConceal.Text = "+4";
					lblWeaponDamage.Text = objWeapon.CalculatedDamage();
					lblWeaponRC.Text = objWeapon.TotalRC;
					lblWeaponAP.Text = objWeapon.TotalAP;
					lblWeaponReach.Text = objWeapon.TotalReach.ToString();
					lblWeaponMode.Text = objWeapon.CalculatedMode;
					lblWeaponAmmo.Text = objWeapon.CalculatedAmmo();
					lblWeaponSlots.Text = "6 (" + objWeapon.SlotsRemaining.ToString() + " " + LanguageManager.Instance.GetString("String_Remaining") + ")";
					lblWeaponDicePool.Text = objWeapon.DicePool;
					tipTooltip.SetToolTip(lblWeaponDicePool, objWeapon.DicePoolTooltip);

					UpdateCharacterInfo();
				}
				else
				{
					bool blnAccessory = false;
					Weapon objSelectedWeapon = new Weapon(_objCharacter);
					WeaponAccessory objSelectedAccessory = _objFunctions.FindWeaponAccessory(treWeapons.SelectedNode.Tag.ToString(), _objCharacter.Weapons);
					if (objSelectedAccessory != null)
					{
						blnAccessory = true;
						objSelectedWeapon = objSelectedAccessory.Parent;
					}

					if (blnAccessory)
					{
						lblWeaponName.Text = objSelectedAccessory.DisplayNameShort;
						lblWeaponCategory.Text = LanguageManager.Instance.GetString("String_WeaponAccessory");
						lblWeaponAvail.Text = objSelectedAccessory.TotalAvail;
						lblWeaponCost.Text = String.Format("{0:###,###,##0¥}", Convert.ToInt32(objSelectedAccessory.TotalCost));
						lblWeaponConceal.Text = objSelectedAccessory.Concealability.ToString();
						lblWeaponDamage.Text = "";
						lblWeaponRC.Text = objSelectedAccessory.RC;
						lblWeaponAP.Text = "";
						lblWeaponReach.Text = "";
						lblWeaponMode.Text = "";
						lblWeaponAmmo.Text = "";

						string[] strMounts = objSelectedAccessory.Mount.Split('/');
						string strMount = "";
						foreach (string strCurrentMount in strMounts)
						{
							if (strCurrentMount != "")
								strMount += LanguageManager.Instance.GetString("String_Mount" + strCurrentMount) + "/";
						}
						// Remove the trailing /
						if (strMount != "" && strMount.Contains('/'))
							strMount = strMount.Substring(0, strMount.Length - 1);

						lblWeaponSlots.Text = strMount;
						string strBook = _objOptions.LanguageBookShort(objSelectedAccessory.Source);
						string strPage = objSelectedAccessory.Page;
						lblWeaponSource.Text = strBook + " " + strPage;
						tipTooltip.SetToolTip(lblWeaponSource, _objOptions.BookFromCode(objSelectedAccessory.Source) + " " + LanguageManager.Instance.GetString("String_Page") + " " + objSelectedAccessory.Page);
						chkWeaponAccessoryInstalled.Enabled = true;
						chkWeaponAccessoryInstalled.Checked = objSelectedAccessory.Installed;
						chkIncludedInWeapon.Enabled = _objOptions.AllowEditPartOfBaseWeapon;
						chkIncludedInWeapon.Checked = objSelectedAccessory.IncludedInWeapon;
						chkWeaponBlackMarketDiscount.Checked = objSelectedAccessory.DiscountCost;
						UpdateCharacterInfo();
					}
					else
					{
						bool blnMod = false;
						WeaponMod objSelectedMod = _objFunctions.FindWeaponMod(treWeapons.SelectedNode.Tag.ToString(), _objCharacter.Weapons);
						if (objSelectedMod != null)
						{
							blnMod = true;
							objSelectedWeapon = objSelectedMod.Parent;
						}

						if (blnMod)
						{
							lblWeaponName.Text = objSelectedMod.DisplayNameShort;
							lblWeaponCategory.Text = LanguageManager.Instance.GetString("String_WeaponModification");
							lblWeaponAvail.Text = objSelectedMod.TotalAvail;
							lblWeaponCost.Text = String.Format("{0:###,###,##0¥}", Convert.ToInt32(objSelectedMod.TotalCost));
							lblWeaponConceal.Text = objSelectedMod.Concealability.ToString();
							lblWeaponDamage.Text = "";
							lblWeaponRC.Text = objSelectedMod.RC;
							lblWeaponAP.Text = "";
							lblWeaponReach.Text = "";
							lblWeaponMode.Text = "";
							lblWeaponAmmo.Text = "";
							lblWeaponSlots.Text = objSelectedMod.Slots.ToString();
							string strBook = _objOptions.LanguageBookShort(objSelectedMod.Source);
							string strPage = objSelectedMod.Page;
							lblWeaponSource.Text = strBook + " " + strPage;
							tipTooltip.SetToolTip(lblWeaponSource, _objOptions.BookFromCode(objSelectedMod.Source) + " " + LanguageManager.Instance.GetString("String_Page") + " " + objSelectedMod.Page);
							chkWeaponAccessoryInstalled.Enabled = true;
							chkWeaponAccessoryInstalled.Checked = objSelectedMod.Installed;
							chkIncludedInWeapon.Enabled = _objOptions.AllowEditPartOfBaseWeapon;
							chkIncludedInWeapon.Checked = objSelectedMod.IncludedInWeapon;
							chkWeaponBlackMarketDiscount.Checked = objSelectedMod.DiscountCost;
							UpdateCharacterInfo();
						}
						else
						{
							// Find the selected Gear.
							_blnSkipRefresh = true;
							WeaponAccessory objAccessory = new WeaponAccessory(_objCharacter);
							Gear objGear = _objFunctions.FindWeaponGear(treWeapons.SelectedNode.Tag.ToString(), _objCharacter.Weapons, out objAccessory);
							lblWeaponName.Text = objGear.DisplayNameShort;
							lblWeaponCategory.Text = objGear.DisplayCategory;
							lblWeaponAvail.Text = objGear.TotalAvail(true);
							lblWeaponCost.Text = String.Format("{0:###,###,##0¥}", objGear.TotalCost);
							lblWeaponConceal.Text = "";
							lblWeaponDamage.Text = "";
							lblWeaponRC.Text = "";
							lblWeaponAP.Text = "";
							lblWeaponReach.Text = "";
							lblWeaponMode.Text = "";
							lblWeaponAmmo.Text = "";
							lblWeaponSlots.Text = "";
							string strBook = _objOptions.LanguageBookShort(objGear.Source);
							string strPage = objGear.Page;
							lblWeaponSource.Text = strBook + " " + strPage;
							tipTooltip.SetToolTip(lblWeaponSource, _objOptions.BookFromCode(objGear.Source) + " " + LanguageManager.Instance.GetString("String_Page") + " " + objGear.Page);
							chkWeaponAccessoryInstalled.Enabled = true;
							chkWeaponAccessoryInstalled.Checked = objGear.Equipped;
							chkIncludedInWeapon.Enabled = false;
							chkIncludedInWeapon.Checked = false;
							chkWeaponBlackMarketDiscount.Checked = objGear.DiscountCost;
							_blnSkipRefresh = false;
						}
					}

					// Show the Weapon Ranges.
					lblWeaponRangeShort.Text = objSelectedWeapon.RangeShort;
					lblWeaponRangeMedium.Text = objSelectedWeapon.RangeMedium;
					lblWeaponRangeLong.Text = objSelectedWeapon.RangeLong;
					lblWeaponRangeExtreme.Text = objSelectedWeapon.RangeExtreme;
				}
			}
		}

		/// <summary>
		/// Refresh the information for the currently displayed Armor.
		/// </summary>
		public void RefreshSelectedArmor()
		{
			if (treArmor.SelectedNode.Level == 0)
			{
				lblArmorEquipped.Text = "";
				foreach (Armor objArmor in _objCharacter.Armor)
				{
					if (objArmor.Equipped && (objArmor.Location == treArmor.SelectedNode.Text || objArmor.Location == string.Empty && treArmor.SelectedNode == treArmor.Nodes[0]))
						lblArmorEquipped.Text += objArmor.DisplayName + " (" + objArmor.TotalBallistic.ToString() + "/" + objArmor.TotalImpact.ToString() + ")\n";
				}
				if (lblArmorEquipped.Text == string.Empty)
					lblArmorEquipped.Text = LanguageManager.Instance.GetString("String_None");

				lblArmorEquipped.Visible = true;

				_blnSkipRefresh = true;
				chkIncludedInArmor.Enabled = false;
				chkIncludedInArmor.Checked = false;
				chkIncludedInArmor.Enabled = false;
				chkIncludedInArmor.Checked = false;
				_blnSkipRefresh = false;
			}
			else
				lblArmorEquipped.Visible = false;

			if (treArmor.SelectedNode.Level == 1)
			{
				_blnSkipRefresh = true;

				// Loclate the selected Armor
				Armor objArmor = _objFunctions.FindArmor(treArmor.SelectedNode.Tag.ToString(), _objCharacter.Armor);
				if (objArmor == null)
					return;

				lblArmorBallistic.Text = objArmor.TotalBallistic.ToString();
				lblArmorImpact.Text = objArmor.TotalImpact.ToString();
				lblArmorAvail.Text = objArmor.TotalAvail;
				lblArmorCapacity.Text = objArmor.CalculatedCapacity + " (" + objArmor.CapacityRemaining.ToString() + " " + LanguageManager.Instance.GetString("String_Remaining") + ")";
				lblArmorCost.Text = String.Format("{0:###,###,##0¥}", objArmor.TotalCost);
				string strBook = _objOptions.LanguageBookShort(objArmor.Source);
				string strPage = objArmor.Page;
				lblArmorSource.Text = strBook + " " + strPage;
				tipTooltip.SetToolTip(lblArmorSource, _objOptions.LanguageBookLong(objArmor.Source) + " " + LanguageManager.Instance.GetString("String_Page") + " " + objArmor.Page);
				chkArmorEquipped.Enabled = true;
				chkArmorEquipped.Checked = objArmor.Equipped;
				chkArmorBlackMarketDiscount.Checked = objArmor.DiscountCost;
				nudArmorRating.Enabled = false;

				_blnSkipRefresh = false;
			}
			else if (treArmor.SelectedNode.Level == 2)
			{
				bool blnIsMod = false;
				Armor objSelectedArmor = new Armor(_objCharacter);
				ArmorMod objSelectedMod = _objFunctions.FindArmorMod(treArmor.SelectedNode.Tag.ToString(), _objCharacter.Armor);
				if (objSelectedMod != null)
				{
					blnIsMod = true;
					objSelectedArmor = objSelectedMod.Parent;
				}

				if (blnIsMod)
				{
					lblArmorBallistic.Text = objSelectedMod.Ballistic.ToString();
					lblArmorImpact.Text = objSelectedMod.Impact.ToString();
					lblArmorAvail.Text = objSelectedMod.TotalAvail;
					if (objSelectedArmor.CapacityDisplayStyle == CapacityStyle.Standard)
						lblArmorCapacity.Text = objSelectedMod.CalculatedCapacity;
					else if (objSelectedArmor.CapacityDisplayStyle == CapacityStyle.Zero)
						lblArmorCapacity.Text = "[0]";
					else if (objSelectedArmor.CapacityDisplayStyle == CapacityStyle.PerRating)
					{
						if (objSelectedMod.Rating > 0)
							lblArmorCapacity.Text = "[" + objSelectedMod.Rating.ToString() + "]";
						else
							lblArmorCapacity.Text = "[1]";
					}
					lblArmorCost.Text = String.Format("{0:###,###,##0¥}", objSelectedMod.TotalCost);

					string strBook = _objOptions.LanguageBookShort(objSelectedMod.Source);
					string strPage = objSelectedMod.Page;
					lblArmorSource.Text = strBook + " " + strPage;
					tipTooltip.SetToolTip(lblArmorSource, _objOptions.LanguageBookLong(objSelectedMod.Source) + " " + LanguageManager.Instance.GetString("String_Page") + " " + objSelectedMod.Page);
					chkArmorEquipped.Enabled = true;
					chkArmorEquipped.Checked = objSelectedMod.Equipped;
					chkArmorBlackMarketDiscount.Checked = objSelectedMod.DiscountCost;
					if (objSelectedMod.MaximumRating > 1)
					{
						_blnSkipRefresh = true;
						nudArmorRating.Maximum = objSelectedMod.MaximumRating;
						nudArmorRating.Enabled = true;
						nudArmorRating.Value = objSelectedMod.Rating;
						_blnSkipRefresh = false;
					}
					else
					{
						_blnSkipRefresh = true;
						nudArmorRating.Maximum = 1;
						nudArmorRating.Enabled = false;
						nudArmorRating.Value = 1;
						_blnSkipRefresh = false;
					}

					_blnSkipRefresh = true;
					chkIncludedInArmor.Enabled = true;
					chkIncludedInArmor.Checked = objSelectedMod.IncludedInArmor;
					_blnSkipRefresh = false;
				}
				else
				{
					Gear objSelectedGear = _objFunctions.FindArmorGear(treArmor.SelectedNode.Tag.ToString(), _objCharacter.Armor, out objSelectedArmor);

					lblArmorBallistic.Text = "";
					lblArmorImpact.Text = "";
					lblArmorAvail.Text = objSelectedGear.TotalAvail(true);
					if (objSelectedArmor.CapacityDisplayStyle == CapacityStyle.Standard)
						lblArmorCapacity.Text = objSelectedGear.CalculatedCapacity;
					else if (objSelectedArmor.CapacityDisplayStyle == CapacityStyle.Zero)
						lblArmorCapacity.Text = "[0]";
					else if (objSelectedArmor.CapacityDisplayStyle == CapacityStyle.PerRating)
					{
						if (objSelectedGear.Rating > 0)
							lblArmorCapacity.Text = "[" + objSelectedGear.Rating.ToString() + "]";
						else
							lblArmorCapacity.Text = "[1]";
					}
					try
					{
						lblArmorCost.Text = String.Format("{0:###,###,##0¥}", objSelectedGear.TotalCost);
					}
					catch
					{
						lblArmorCost.Text = String.Format("{0:###,###,##0¥}", objSelectedGear.Cost);
					}
					string strBook = _objOptions.LanguageBookShort(objSelectedGear.Source);
					string strPage = objSelectedGear.Page;
					lblArmorSource.Text = strBook + " " + strPage;
					tipTooltip.SetToolTip(lblArmorSource, _objOptions.LanguageBookLong(objSelectedGear.Source) + " " + LanguageManager.Instance.GetString("String_Page") + " " + objSelectedGear.Page);
					chkArmorEquipped.Enabled = true;
					chkArmorEquipped.Checked = objSelectedGear.Equipped;
					chkArmorBlackMarketDiscount.Checked = objSelectedGear.DiscountCost;
					if (objSelectedGear.MaxRating > 1)
					{
						_blnSkipRefresh = true;
						nudArmorRating.Maximum = objSelectedGear.MaxRating;
						nudArmorRating.Enabled = true;
						nudArmorRating.Value = objSelectedGear.Rating;
						_blnSkipRefresh = false;
					}
					else
					{
						_blnSkipRefresh = true;
						nudArmorRating.Maximum = 1;
						nudArmorRating.Enabled = false;
						nudArmorRating.Value = 1;
						_blnSkipRefresh = false;
					}

					_blnSkipRefresh = true;
					chkIncludedInArmor.Enabled = false;
					chkIncludedInArmor.Checked = false;
					_blnSkipRefresh = false;
				}
			}
			else if (treArmor.SelectedNode.Level > 2)
			{
				Armor objSelectedArmor = new Armor(_objCharacter);
				Gear objSelectedGear = _objFunctions.FindArmorGear(treArmor.SelectedNode.Tag.ToString(), _objCharacter.Armor, out objSelectedArmor);

				lblArmorBallistic.Text = "";
				lblArmorImpact.Text = "";
				lblArmorAvail.Text = objSelectedGear.TotalAvail(true);
				lblArmorCapacity.Text = objSelectedGear.CalculatedArmorCapacity;
				try
				{
					lblArmorCost.Text = String.Format("{0:###,###,##0¥}", objSelectedGear.TotalCost);
				}
				catch
				{
					lblArmorCost.Text = String.Format("{0:###,###,##0¥}", objSelectedGear.Cost);
				}
				string strBook = _objOptions.LanguageBookShort(objSelectedGear.Source);
				string strPage = objSelectedGear.Page;
				lblArmorSource.Text = strBook + " " + strPage;
				tipTooltip.SetToolTip(lblArmorSource, _objOptions.LanguageBookLong(objSelectedGear.Source) + " " + LanguageManager.Instance.GetString("String_Page") + " " + objSelectedGear.Page);
				chkArmorEquipped.Enabled = true;
				chkArmorEquipped.Checked = objSelectedGear.Equipped;
				chkArmorBlackMarketDiscount.Checked = objSelectedGear.DiscountCost;
				if (objSelectedGear.MaxRating > 1)
				{
					_blnSkipRefresh = true;
					nudArmorRating.Maximum = objSelectedGear.MaxRating;
					nudArmorRating.Enabled = true;
					nudArmorRating.Value = objSelectedGear.Rating;
					_blnSkipRefresh = false;
				}
				else
				{
					_blnSkipRefresh = true;
					nudArmorRating.Maximum = 1;
					nudArmorRating.Enabled = false;
					nudArmorRating.Value = 1;
					_blnSkipRefresh = false;
				}
			}
			else
			{
				lblArmorBallistic.Text = "";
				lblArmorImpact.Text = "";
				lblArmorAvail.Text = "";
				lblArmorCost.Text = "";
				lblArmorSource.Text = "";
				tipTooltip.SetToolTip(lblArmorSource, null);
				chkArmorEquipped.Enabled = false;
				nudArmorRating.Enabled = false;
				chkArmorBlackMarketDiscount.Checked = false;
			}
		}

		/// <summary>
		/// Refresh the information for the currently displayed Gear.
		/// </summary>
		public void RefreshSelectedGear()
		{
			bool blnClear = false;
			try
			{
				if (treGear.SelectedNode.Level == 0)
					blnClear = true;
			}
			catch
			{
				blnClear = true;
			}
			if (blnClear)
			{
				_blnSkipRefresh = true;
				nudGearRating.Minimum = 0;
				nudGearRating.Maximum = 0;
				nudGearRating.Enabled = false;
				nudGearQty.Enabled = false;
				chkGearEquipped.Text = LanguageManager.Instance.GetString("Checkbox_Equipped");
				chkGearEquipped.Visible = false;
				chkActiveCommlink.Visible = false;
				_blnSkipRefresh = false;
				return;
			}
			chkGearHomeNode.Visible = false;

			if (treGear.SelectedNode.Level > 0)
			{
				Gear objGear = new Gear(_objCharacter);
				objGear = _objFunctions.FindGear(treGear.SelectedNode.Tag.ToString(), _objCharacter.Gear);

				lblGearName.Text = objGear.DisplayNameShort;
				lblGearCategory.Text = objGear.DisplayCategory;
				lblGearAvail.Text = objGear.TotalAvail(true);
				try
				{
					lblGearCost.Text = String.Format("{0:###,###,##0¥}", objGear.TotalCost);
				}
				catch
				{
					lblGearCost.Text = objGear.Cost;
				}
				lblGearCapacity.Text = objGear.CalculatedCapacity + " (" + objGear.CapacityRemaining.ToString() + " " + LanguageManager.Instance.GetString("String_Remaining") + ")";
				string strBook = _objOptions.LanguageBookShort(objGear.Source);
				string strPage = objGear.Page;
				lblGearSource.Text = strBook + " " + strPage;
				tipTooltip.SetToolTip(lblGearSource, _objOptions.LanguageBookLong(objGear.Source) + " " + LanguageManager.Instance.GetString("String_Page") + " " + objGear.Page);

				if (objGear.GetType() == typeof(Commlink))
				{
					Commlink objCommlink = (Commlink)objGear;
					lblGearResponse.Text = objCommlink.TotalResponse.ToString();
					lblGearSignal.Text = objCommlink.TotalSignal.ToString();
					if (objCommlink.Category != "Commlink Upgrade")
					{
						lblGearSystem.Text = objCommlink.TotalSystem.ToString();
						lblGearFirewall.Text = objCommlink.TotalFirewall.ToString();
					}
					else
					{
						lblGearSystem.Text = "";
						lblGearFirewall.Text = "";
					}

					_blnSkipRefresh = true;
					chkActiveCommlink.Checked = objCommlink.IsActive;
					_blnSkipRefresh = false;

					if (objCommlink.Category != "Commlink Upgrade")
						chkActiveCommlink.Visible = true;
				}
				else if (objGear.GetType() == typeof(OperatingSystem))
				{
					OperatingSystem objOS = (OperatingSystem)objGear;
					lblGearResponse.Text = "";
					lblGearSignal.Text = "";
					lblGearSystem.Text = objOS.System.ToString();
					lblGearFirewall.Text = objOS.Firewall.ToString();
					chkActiveCommlink.Visible = false;
				}
				else
				{
					lblGearResponse.Text = objGear.Response.ToString();
					lblGearSignal.Text = objGear.Signal.ToString();
					lblGearSystem.Text = objGear.System.ToString();
					lblGearFirewall.Text = objGear.Firewall.ToString();
					chkActiveCommlink.Visible = false;
				}

				if (objGear.MaxRating > 0)
				{
					_blnSkipRefresh = true;
					if (objGear.MinRating > 0)
						nudGearRating.Minimum = objGear.MinRating;
					else if (objGear.MinRating == 0 && objGear.Name.Contains("Credstick,"))
						nudGearRating.Minimum = 0;
					else
						nudGearRating.Minimum = 1;
					nudGearRating.Maximum = objGear.MaxRating;
					nudGearRating.Value = objGear.Rating;
					if (nudGearRating.Minimum == nudGearRating.Maximum)
						nudGearRating.Enabled = false;
					else
						nudGearRating.Enabled = true;
					_blnSkipRefresh = false;
				}
				else
				{
					_blnSkipRefresh = true;
					nudGearRating.Minimum = 0;
					nudGearRating.Maximum = 0;
					nudGearRating.Enabled = false;
					_blnSkipRefresh = false;
				}

				try
				{
					_blnSkipRefresh = true;
					chkGearBlackMarketDiscount.Checked = objGear.DiscountCost;
					//nudGearQty.Minimum = objGear.CostFor;
					nudGearQty.Increment = objGear.CostFor;
					nudGearQty.Value = objGear.Quantity;
					_blnSkipRefresh = false;
				}
				catch
				{
				}

				if (treGear.SelectedNode.Level == 1)
				{
					_blnSkipRefresh = true;
					nudGearQty.Enabled = true;
					nudGearQty.Increment = objGear.CostFor;
					//nudGearQty.Minimum = objGear.CostFor;
					chkGearEquipped.Visible = true;
					chkGearEquipped.Checked = objGear.Equipped;
					_blnSkipRefresh = false;
				}
				else
				{
					nudGearQty.Enabled = false;
					_blnSkipRefresh = true;
					chkGearEquipped.Visible = true;
					chkGearEquipped.Checked = objGear.Equipped;

					// If this is a Program, determine if its parent Gear (if any) is a Commlink. If so, show the Equipped checkbox.
					if (objGear.IsProgram && _objOptions.CalculateCommlinkResponse)
					{
						Gear objParent = new Gear(_objCharacter);
						objParent = objGear.Parent;
						if (objParent.Category != string.Empty)
						{
							if (objParent.Category == "Commlink" || objParent.Category == "Nexus")
								chkGearEquipped.Text = LanguageManager.Instance.GetString("Checkbox_SoftwareRunning");
						}
					}
					_blnSkipRefresh = false;
				}

				// Show the Weapon Bonus information if it's available.
				if (objGear.WeaponBonus != null)
				{
					lblGearDamageLabel.Visible = true;
					lblGearDamage.Visible = true;
					lblGearAPLabel.Visible = true;
					lblGearAP.Visible = true;
					lblGearDamage.Text = objGear.WeaponBonusDamage();
					lblGearAP.Text = objGear.WeaponBonusAP;
				}
				else
				{
					lblGearDamageLabel.Visible = false;
					lblGearDamage.Visible = false;
					lblGearAPLabel.Visible = false;
					lblGearAP.Visible = false;
				}

				if ((_objCharacter.Metatype.EndsWith("A.I.") || _objCharacter.MetatypeCategory == "Technocritters" || _objCharacter.MetatypeCategory == "Protosapients") && objGear.GetType() == typeof(Commlink))
				{
					chkGearHomeNode.Visible = true;
					chkGearHomeNode.Checked = objGear.HomeNode;
				}

				treGear.SelectedNode.Text = objGear.DisplayName;
			}
		}

		/// <summary>
		/// Update the Window title to show the Character's name and unsaved changes status.
		/// </summary>
		private void UpdateWindowTitle(bool blnCanSkip = true)
		{
			if (this.Text.EndsWith("*") && blnCanSkip)
				return;

			this.Text = "";
			if (txtAlias.Text != "")
				 this.Text += txtAlias.Text + " - ";
			this.Text += LanguageManager.Instance.GetString("Title_CreateNewCharacter");
			this.Text += " (" + _objCharacter.Options.Name + ")";
			if (_blnIsDirty)
				this.Text += "*";
		}

		/// <summary>
		/// Save the Character.
		/// </summary>
		private bool SaveCharacter()
		{
			bool blnSaved = false;

			// If the Character does not have a file name, trigger the Save As menu item instead.
			if (_objCharacter.FileName == "")
				blnSaved = SaveCharacterAs(true);
			else
			{
				// If the Created is checked, make sure the user wants to actually save this character.
				if (chkCharacterCreated.Checked)
				{
					if (!ConfirmSaveCreatedCharacter())
					{
						chkCharacterCreated.Checked = false;
						return false;
					}
				}

				_objCharacter.Save();
				_blnIsDirty = false;
				blnSaved = true;
				GlobalOptions.Instance.AddToMRUList(_objCharacter.FileName);
			}
			UpdateWindowTitle(false);

			// If this character has just been saved as Created, close this form and re-open the character which will open it in the Career window instead.
			if (blnSaved && chkCharacterCreated.Checked)
			{
				// If the character was built with Karma, record their staring Karma amount (if any).
				if (_objCharacter.BuildMethod == CharacterBuildMethod.Karma)
				{
					if (_objCharacter.Karma > 0)
					{
						ExpenseLogEntry objKarma = new ExpenseLogEntry();
						objKarma.Create(_objCharacter.Karma, "Starting Karma", ExpenseType.Karma, DateTime.Now);
						_objCharacter.ExpenseEntries.Add(objKarma);

						// Create an Undo entry so that the starting Karma amount can be modified if needed.
						ExpenseUndo objKarmaUndo = new ExpenseUndo();
						objKarmaUndo.CreateKarma(KarmaExpenseType.ManualAdd, "");
						objKarma.Undo = objKarmaUndo;
					}
				}

				// Create an Expense Entry for Starting Nuyen.
				ExpenseLogEntry objNuyen = new ExpenseLogEntry();
				objNuyen.Create(_objCharacter.Nuyen, "Starting Nuyen", ExpenseType.Nuyen, DateTime.Now);
				_objCharacter.ExpenseEntries.Add(objNuyen);

				// Create an Undo entry so that the Starting Nuyen amount can be modified if needed.
				ExpenseUndo objNuyenUndo = new ExpenseUndo();
				objNuyenUndo.CreateNuyen(NuyenExpenseType.ManualAdd, "");
				objNuyen.Undo = objNuyenUndo;

				_blnSkipToolStripRevert = true;
				_objCharacter.Save();

				GlobalOptions.Instance.MainForm.LoadCharacter(_objCharacter.FileName, false);
				this.Close();
			}

			return blnSaved;
		}

		/// <summary>
		/// Save the Character using the Save As dialogue box.
		/// </summary>
		private bool SaveCharacterAs(bool blnEscapeAfterSave = false)
		{
			bool blnSaved = false;

			// If the Created is checked, make sure the user wants to actually save this character.
			if (chkCharacterCreated.Checked)
			{
				if (!ConfirmSaveCreatedCharacter())
				{
					chkCharacterCreated.Checked = false;
					return false;
				}
			}

			SaveFileDialog saveFileDialog = new SaveFileDialog();
			saveFileDialog.Filter = "Chummer Files (*.chum)|*.chum|All Files (*.*)|*.*";

			string strShowFileName = "";
			string[] strFile = _objCharacter.FileName.Split(Path.DirectorySeparatorChar);
			strShowFileName = strFile[strFile.Length - 1];

			if (strShowFileName == "")
				strShowFileName = _objCharacter.Alias;

			saveFileDialog.FileName = strShowFileName;

			if (saveFileDialog.ShowDialog(this) == DialogResult.OK)
			{
				string strFileName = saveFileDialog.FileName;
				_objCharacter.FileName = strFileName;
				_objCharacter.Save();
				_blnIsDirty = false;
				blnSaved = true;
				GlobalOptions.Instance.AddToMRUList(_objCharacter.FileName);
			}
			if (blnEscapeAfterSave)
				return blnSaved;
			UpdateWindowTitle(false);

			// If this character has just been saved as Created, close this form and re-open the character which will open it in the Career window instead.
			if (blnSaved && chkCharacterCreated.Checked)
			{
				// If the character was built with Karma, record their staring Karma amount (if any).
				if (_objCharacter.BuildMethod == CharacterBuildMethod.Karma)
				{
					if (_objCharacter.Karma > 0)
					{
						ExpenseLogEntry objKarma = new ExpenseLogEntry();
						objKarma.Create(_objCharacter.Karma, "Starting Karma", ExpenseType.Karma, DateTime.Now);
						_objCharacter.ExpenseEntries.Add(objKarma);
					}
				}

				// Create an Expense Entry for Starting Nuyen.
				ExpenseLogEntry objNuyen = new ExpenseLogEntry();
				objNuyen.Create(_objCharacter.Nuyen, "Starting Nuyen", ExpenseType.Nuyen, DateTime.Now);
				_objCharacter.ExpenseEntries.Add(objNuyen);
				_blnSkipToolStripRevert = true;

				frmCareer frmCharacter = new frmCareer(_objCharacter);
				frmCharacter.MdiParent = this.MdiParent;
				frmCharacter.WindowState = FormWindowState.Maximized;
				frmCharacter.Loading = true;
				frmCharacter.Show();
				this.Close();
			}

			return blnSaved;
		}

		/// <summary>
		/// Open the Select Cyberware window and handle adding to the Tree and Character.
		/// </summary>
		private bool PickCyberware(Improvement.ImprovementSource objSource = Improvement.ImprovementSource.Cyberware)
		{
			Cyberware objSelectedCyberware = new Cyberware(_objCharacter);
			int intNode = 0;
			if (objSource == Improvement.ImprovementSource.Bioware)
				intNode = 1;

			// Attempt to locate the selected piece of Cyberware.
			try
			{
				if (treCyberware.SelectedNode.Level > 0)
					objSelectedCyberware = _objFunctions.FindCyberware(treCyberware.SelectedNode.Tag.ToString(), _objCharacter.Cyberware);
			}
			catch
			{
			}

			frmSelectCyberware frmPickCyberware = new frmSelectCyberware(_objCharacter);
			double dblMultiplier = 1;
			// Apply the character's Cyberware Essence cost multiplier if applicable.
			if (_objImprovementManager.ValueOf(Improvement.ImprovementType.CyberwareEssCost) != 0 && objSource == Improvement.ImprovementSource.Cyberware)
			{
				foreach (Improvement objImprovement in _objCharacter.Improvements)
				{
					if (objImprovement.ImproveType == Improvement.ImprovementType.CyberwareEssCost && objImprovement.Enabled)
						dblMultiplier -= (1 - (Convert.ToDouble(objImprovement.Value, GlobalOptions.Instance.CultureInfo) / 100));
				}
				frmPickCyberware.CharacterESSMultiplier = dblMultiplier;
			}
			
			// Apply the character's Bioware Essence cost multiplier if applicable.
			if (_objImprovementManager.ValueOf(Improvement.ImprovementType.BiowareEssCost) != 0 && objSource == Improvement.ImprovementSource.Bioware)
			{
				foreach (Improvement objImprovement in _objCharacter.Improvements)
				{
					if (objImprovement.ImproveType == Improvement.ImprovementType.BiowareEssCost && objImprovement.Enabled)
						dblMultiplier -= (1 - (Convert.ToDouble(objImprovement.Value, GlobalOptions.Instance.CultureInfo) / 100));
				}
				frmPickCyberware.CharacterESSMultiplier = dblMultiplier;
			}

			// Apply the character's Basic Bioware Essence cost multiplier if applicable.
			if (_objImprovementManager.ValueOf(Improvement.ImprovementType.BasicBiowareEssCost) != 0 && objSource == Improvement.ImprovementSource.Bioware)
			{
				double dblBasicMultiplier = 1;
				foreach (Improvement objImprovement in _objCharacter.Improvements)
				{
					if (objImprovement.ImproveType == Improvement.ImprovementType.BasicBiowareEssCost && objImprovement.Enabled)
						dblBasicMultiplier -= (1 - (Convert.ToDouble(objImprovement.Value, GlobalOptions.Instance.CultureInfo) / 100));
				}
				frmPickCyberware.BasicBiowareESSMultiplier = dblBasicMultiplier;
			}

			// Genetech Cost multiplier.
			if (_objImprovementManager.ValueOf(Improvement.ImprovementType.GenetechCostMultiplier) != 0 && objSource == Improvement.ImprovementSource.Bioware)
			{
				dblMultiplier = 1;
				foreach (Improvement objImprovement in _objCharacter.Improvements)
				{
					if (objImprovement.ImproveType == Improvement.ImprovementType.GenetechCostMultiplier && objImprovement.Enabled)
						dblMultiplier -= (1 - (Convert.ToDouble(objImprovement.Value, GlobalOptions.Instance.CultureInfo) / 100));
				}
				frmPickCyberware.GenetechCostMultiplier = dblMultiplier;
			}

			// Transgenics Cost multiplier.
			if (_objImprovementManager.ValueOf(Improvement.ImprovementType.TransgenicsBiowareCost) != 0 && objSource == Improvement.ImprovementSource.Bioware)
			{
				dblMultiplier = 1;
				foreach (Improvement objImprovement in _objCharacter.Improvements)
				{
					if (objImprovement.ImproveType == Improvement.ImprovementType.TransgenicsBiowareCost && objImprovement.Enabled)
						dblMultiplier -= (1 - (Convert.ToDouble(objImprovement.Value, GlobalOptions.Instance.CultureInfo) / 100));
				}
				frmPickCyberware.TransgenicsBiowareCostMultiplier = dblMultiplier;
			}

			try
			{
				if (treCyberware.SelectedNode.Level > 0)
				{
					frmPickCyberware.SetGrade = cboCyberwareGrade.SelectedValue.ToString();
					frmPickCyberware.LockGrade();
					// If the Cyberware has a Capacity with no brackets (meaning it grants Capacity), show only Subsystems (those that conume Capacity).
					if (!objSelectedCyberware.Capacity.Contains('['))
					{
						frmPickCyberware.ShowOnlySubsystems = true;
						frmPickCyberware.Subsystems = objSelectedCyberware.Subsytems;
						frmPickCyberware.MaximumCapacity = objSelectedCyberware.CapacityRemaining;
					}
				}
			}
			catch
			{
			}

			if (objSource == Improvement.ImprovementSource.Bioware)
				frmPickCyberware.WindowMode = frmSelectCyberware.Mode.Bioware;

			frmPickCyberware.AllowModularPlugins = objSelectedCyberware.AllowModularPlugins;

			frmPickCyberware.ShowDialog(this);

			// Make sure the dialogue window was not canceled.
			if (frmPickCyberware.DialogResult == DialogResult.Cancel)
				return false;

			// Open the Cyberware XML file and locate the selected piece.
			XmlDocument objXmlDocument = new XmlDocument();
			if (objSource == Improvement.ImprovementSource.Bioware)
				objXmlDocument = XmlManager.Instance.Load("bioware.xml");
			else
				objXmlDocument = XmlManager.Instance.Load("cyberware.xml");

			XmlNode objXmlCyberware;
			if (objSource == Improvement.ImprovementSource.Bioware)
				objXmlCyberware = objXmlDocument.SelectSingleNode("/chummer/biowares/bioware[name = \"" + frmPickCyberware.SelectedCyberware + "\"]");
			else
				objXmlCyberware = objXmlDocument.SelectSingleNode("/chummer/cyberwares/cyberware[name = \"" + frmPickCyberware.SelectedCyberware + "\"]");

			// Create the Cyberware object.
			Cyberware objCyberware = new Cyberware(_objCharacter);
			List<Weapon> objWeapons = new List<Weapon>();
			TreeNode objNode = new TreeNode();
			List<TreeNode> objWeaponNodes = new List<TreeNode>();
			objCyberware.Create(objXmlCyberware, _objCharacter, frmPickCyberware.SelectedGrade, objSource, frmPickCyberware.SelectedRating, objNode, objWeapons, objWeaponNodes);
			if (objCyberware.InternalId == Guid.Empty.ToString())
				return false;

			// Force the item to be Transgenic if selected.
			if (frmPickCyberware.ForceTransgenic)
				objCyberware.Category = "Genetech: Transgenics";

			// Apply the ESS discount if applicable.
			if (_objOptions.AllowCyberwareESSDiscounts)
				objCyberware.ESSDiscount = frmPickCyberware.SelectedESSDiscount;

			if (frmPickCyberware.FreeCost)
				objCyberware.Cost = "0";

			try
			{
				if (treCyberware.SelectedNode.Level > 0)
				{
					treCyberware.SelectedNode.Nodes.Add(objNode);
					treCyberware.SelectedNode.Expand();
					objSelectedCyberware.Children.Add(objCyberware);
					objCyberware.Parent = objSelectedCyberware;
				}
				else
				{
					treCyberware.Nodes[intNode].Nodes.Add(objNode);
					treCyberware.Nodes[intNode].Expand();
					_objCharacter.Cyberware.Add(objCyberware);
				}
			}
			catch
			{
				treCyberware.Nodes[intNode].Nodes.Add(objNode);
				treCyberware.Nodes[intNode].Expand();
				_objCharacter.Cyberware.Add(objCyberware);
			}

			// Select the node that was just added.
			_blnSkipRefresh = true;
			if (objSource == Improvement.ImprovementSource.Cyberware)
				objNode.ContextMenuStrip = cmsCyberware;
			else if (objSource == Improvement.ImprovementSource.Bioware)
				objNode.ContextMenuStrip = cmsBioware;
			_blnSkipRefresh = true;

			foreach (Weapon objWeapon in objWeapons)
				_objCharacter.Weapons.Add(objWeapon);

			// Create the Weapon Node if one exists.
			foreach (TreeNode objWeaponNode in objWeaponNodes)
			{
				objWeaponNode.ContextMenuStrip = cmsWeapon;
				treWeapons.Nodes[0].Nodes.Add(objWeaponNode);
				treWeapons.Nodes[0].Expand();
			}

			_objFunctions.SortTree(treCyberware);
			treCyberware.SelectedNode = objNode;
			_blnSkipRefresh = true;
			PopulateCyberwareGradeList();
			UpdateCharacterInfo();
			RefreshSelectedCyberware();
			_blnSkipRefresh = false;

			_blnIsDirty = true;
			PopulateGearList();
			UpdateWindowTitle();

			return frmPickCyberware.AddAgain;
		}

		/// <summary>
		/// Select a piece of Gear to be added to the character.
		/// </summary>
		private bool PickGear()
		{
			bool blnNullParent = false;
			Gear objSelectedGear = _objFunctions.FindGear(treGear.SelectedNode.Tag.ToString(), _objCharacter.Gear);
			if (objSelectedGear == null)
			{
				objSelectedGear = new Gear(_objCharacter);
				blnNullParent = true;
			}

			// Open the Gear XML file and locate the selected Gear.
			XmlDocument objXmlDocument = XmlManager.Instance.Load("gear.xml");

			XmlNode objXmlGear = objXmlDocument.SelectSingleNode("/chummer/gears/gear[name = \"" + objSelectedGear.Name + "\" and category = \"" + objSelectedGear.Category + "\"]");

			bool blnFakeCareerMode = false;
			if (_objCharacter.Metatype == "A.I." || _objCharacter.MetatypeCategory == "Technocritters" || _objCharacter.MetatypeCategory == "Protosapients")
				blnFakeCareerMode = true;
			frmSelectGear frmPickGear = new frmSelectGear(_objCharacter, blnFakeCareerMode, objSelectedGear.ChildAvailModifier, objSelectedGear.ChildCostMultiplier);
			try
			{
				if (treGear.SelectedNode.Level > 0)
				{
					if (objXmlGear.InnerXml.Contains("<addoncategory>"))
					{
						string strCategories = "";
						foreach (XmlNode objXmlCategory in objXmlGear.SelectNodes("addoncategory"))
							strCategories += objXmlCategory.InnerText + ",";
						// Remove the trailing comma.
						strCategories = strCategories.Substring(0, strCategories.Length - 1);
						frmPickGear.AddCategory(strCategories);
					}

					if (frmPickGear.AllowedCategories != "")
						frmPickGear.AllowedCategories += objSelectedGear.Category + ",";

					// If the Gear has a Capacity with no brackets (meaning it grants Capacity), show only Subsystems (those that conume Capacity).
					if (!objSelectedGear.Capacity.Contains('['))
						frmPickGear.MaximumCapacity = objSelectedGear.CapacityRemaining;

					if (objSelectedGear.Category == "Commlink")
					{
						Commlink objCommlink = (Commlink)objSelectedGear;
						frmPickGear.CommlinkResponse = objCommlink.Response;
						frmPickGear.CommlinkSignal = objCommlink.Signal;
						frmPickGear.CommlinkFirewall = objCommlink.Firewall;
						frmPickGear.CommlinkSystem = objCommlink.System;
					}
					if (objSelectedGear.Category == "Commlink Operating System")
					{
						OperatingSystem objOS = (OperatingSystem)objSelectedGear;
						frmPickGear.CommlinkFirewall = objOS.Firewall;
						frmPickGear.CommlinkSystem = objOS.System; 
					}
				}
			}
			catch
			{
			}

			frmPickGear.ShowDialog(this);

			// Make sure the dialogue window was not canceled.
			if (frmPickGear.DialogResult == DialogResult.Cancel)
				return false;

			TreeNode objNode = new TreeNode();

			// Open the Cyberware XML file and locate the selected piece.
			objXmlDocument = XmlManager.Instance.Load("gear.xml");
			objXmlGear = objXmlDocument.SelectSingleNode("/chummer/gears/gear[name = \"" + frmPickGear.SelectedGear + "\" and category = \"" + frmPickGear.SelectedCategory + "\"]");

			// Create the new piece of Gear.
			List<Weapon> objWeapons = new List<Weapon>();
			List<TreeNode> objWeaponNodes = new List<TreeNode>();
			Gear objNewGear = new Gear(_objCharacter);
			switch (frmPickGear.SelectedCategory)
			{
				case "Commlink":
				case "Commlink Upgrade":
					Commlink objCommlink = new Commlink(_objCharacter);
					objCommlink.Create(objXmlGear, _objCharacter, objNode, frmPickGear.SelectedRating);
					objCommlink.Quantity = frmPickGear.SelectedQty;
					try
					{
						_blnSkipRefresh = true;
						nudGearQty.Increment = objCommlink.CostFor;
						//nudGearQty.Minimum = nudGearQty.Increment;
						_blnSkipRefresh = false;
					}
					catch
					{
					}
					objNode.Text = objCommlink.DisplayName;

					// If a Commlink has just been added, see if the character already has one. If not, make it the active Commlink.
					if (_objFunctions.FindCharacterCommlinks(_objCharacter.Gear).Count == 0 && frmPickGear.SelectedCategory == "Commlink")
						objCommlink.IsActive = true;

					objNewGear = objCommlink;
					break;
				case "Commlink Operating System":
				case "Commlink Operating System Upgrade":
					OperatingSystem objOperatingSystem = new OperatingSystem(_objCharacter);
					objOperatingSystem.Create(objXmlGear, _objCharacter, objNode, frmPickGear.SelectedRating);
					objOperatingSystem.Quantity = frmPickGear.SelectedQty;
					try
					{
						_blnSkipRefresh = true;
						nudGearQty.Increment = objOperatingSystem.CostFor;
						//nudGearQty.Minimum = nudGearQty.Increment;
						_blnSkipRefresh = false;
					}
					catch
					{
					}
					objNode.Text = objOperatingSystem.DisplayName;

					objNewGear = objOperatingSystem;
					break;
				default:
					Gear objGear = new Gear(_objCharacter);
					objGear.Create(objXmlGear, _objCharacter, objNode, frmPickGear.SelectedRating, objWeapons, objWeaponNodes, "", frmPickGear.Hacked, frmPickGear.InherentProgram, true, true, frmPickGear.Aerodynamic);
					objGear.Quantity = frmPickGear.SelectedQty;
					try
					{
						_blnSkipRefresh = true;
						nudGearQty.Increment = objGear.CostFor;
						//nudGearQty.Minimum = nudGearQty.Increment;
						_blnSkipRefresh = false;
					}
					catch
					{
					}
					objNode.Text = objGear.DisplayName;

					objNewGear = objGear;
					break;
			}

			if (objNewGear.InternalId == Guid.Empty.ToString())
				return false;

			// Reduce the cost for Do It Yourself components.
			if (frmPickGear.DoItYourself)
				objNewGear.Cost = (Convert.ToDouble(objNewGear.Cost, GlobalOptions.Instance.CultureInfo) * 0.5).ToString();
			// Reduce the cost to 10% for Hacked programs.
			if (frmPickGear.Hacked)
			{
				if (objNewGear.Cost != "")
					objNewGear.Cost = "(" + objNewGear.Cost + ") * 0.1";
				if (objNewGear.Cost3 != "")
					objNewGear.Cost3 = "(" + objNewGear.Cost3 + ") * 0.1";
				if (objNewGear.Cost6 != "")
					objNewGear.Cost6 = "(" + objNewGear.Cost6 + ") * 0.1";
				if (objNewGear.Cost10 != "")
					objNewGear.Cost10 = "(" + objNewGear.Cost10 + ") * 0.1";
				if (objNewGear.Extra == "")
					objNewGear.Extra = LanguageManager.Instance.GetString("Label_SelectGear_Hacked");
				else
					objNewGear.Extra += ", " + LanguageManager.Instance.GetString("Label_SelectGear_Hacked");
			}
			// If the item was marked as free, change its cost.
			if (frmPickGear.FreeCost)
			{
				objNewGear.Cost = "0";
				objNewGear.Cost3 = "0";
				objNewGear.Cost6 = "0";
				objNewGear.Cost10 = "0";
			}

			// Create any Weapons that came with this Gear.
			foreach (Weapon objWeapon in objWeapons)
				_objCharacter.Weapons.Add(objWeapon);

			foreach (TreeNode objWeaponNode in objWeaponNodes)
			{
				objWeaponNode.ContextMenuStrip = cmsWeapon;
				treWeapons.Nodes[0].Nodes.Add(objWeaponNode);
				treWeapons.Nodes[0].Expand();
			}

			objNewGear.Parent = objSelectedGear;
			if (blnNullParent)
				objNewGear.Parent = null;

			try
			{
				if (treGear.SelectedNode.Level > 0)
				{
					objNode.ContextMenuStrip = cmsGear;
					treGear.SelectedNode.Nodes.Add(objNode);
					treGear.SelectedNode.Expand();
					objSelectedGear.Children.Add(objNewGear);
				}
				else
				{
					objNode.ContextMenuStrip = cmsGear;
					treGear.Nodes[0].Nodes.Add(objNode);
					treGear.Nodes[0].Expand();
					_objCharacter.Gear.Add(objNewGear);
				}
			}
			catch
			{
				treGear.Nodes[0].Nodes.Add(objNode);
				treGear.Nodes[0].Expand();
				_objCharacter.Gear.Add(objNewGear);
			}

			// Select the node that was just added.
			if (objNode.Level < 2)
				treGear.SelectedNode = objNode;

			UpdateCharacterInfo();
			RefreshSelectedGear();

			_blnIsDirty = true;
			UpdateWindowTitle();

			return frmPickGear.AddAgain;
		}

		/// <summary>
		/// Select a piece of Gear and add it to a piece of Armor.
		/// </summary>
		/// <param name="blnShowArmorCapacityOnly">Whether or not only items that consume capacity should be shown.</param>
		private bool PickArmorGear(bool blnShowArmorCapacityOnly = false)
		{
			bool blnNullParent = true;
			Gear objSelectedGear = new Gear(_objCharacter);
			Armor objSelectedArmor = new Armor(_objCharacter);

			foreach (Armor objArmor in _objCharacter.Armor)
			{
				if (objArmor.InternalId == treArmor.SelectedNode.Tag.ToString())
					objSelectedArmor = objArmor;
			}

			if (treArmor.SelectedNode.Level > 1)
			{
				objSelectedGear = _objFunctions.FindArmorGear(treArmor.SelectedNode.Tag.ToString(), _objCharacter.Armor, out objSelectedArmor);
				if (objSelectedGear != null)
					blnNullParent = false;
			}

			// Open the Gear XML file and locate the selected Gear.
			XmlDocument objXmlDocument = XmlManager.Instance.Load("gear.xml");

			XmlNode objXmlGear = objXmlDocument.SelectSingleNode("/chummer/gears/gear[name = \"" + objSelectedGear.Name + "\" and category = \"" + objSelectedGear.Category + "\"]");

			bool blnFakeCareerMode = false;
			if (_objCharacter.Metatype == "A.I." || _objCharacter.MetatypeCategory == "Technocritters" || _objCharacter.MetatypeCategory == "Protosapients")
				blnFakeCareerMode = true;
			frmSelectGear frmPickGear = new frmSelectGear(_objCharacter, blnFakeCareerMode);
			frmPickGear.ShowArmorCapacityOnly = blnShowArmorCapacityOnly;
			frmPickGear.CapacityDisplayStyle = objSelectedArmor.CapacityDisplayStyle;
			try
			{
				if (treArmor.SelectedNode.Level > 1)
				{
					if (objXmlGear.InnerXml.Contains("<addoncategory>"))
					{
						string strCategories = "";
						foreach (XmlNode objXmlCategory in objXmlGear.SelectNodes("addoncategory"))
							strCategories += objXmlCategory.InnerText + ",";
						// Remove the trailing comma.
						strCategories = strCategories.Substring(0, strCategories.Length - 1);
						frmPickGear.AddCategory(strCategories);
					}

					if (frmPickGear.AllowedCategories != "")
						frmPickGear.AllowedCategories += objSelectedGear.Category + ",";

					// If the Gear has a Capacity with no brackets (meaning it grants Capacity), show only Subsystems (those that conume Capacity).
					if (!objSelectedGear.Capacity.Contains('['))
						frmPickGear.MaximumCapacity = objSelectedGear.CapacityRemaining;

					if (objSelectedGear.Category == "Commlink")
					{
						Commlink objCommlink = (Commlink)objSelectedGear;
						frmPickGear.CommlinkResponse = objCommlink.Response;
						frmPickGear.CommlinkSignal = objCommlink.Signal;
						frmPickGear.CommlinkFirewall = objCommlink.Firewall;
						frmPickGear.CommlinkSystem = objCommlink.System;
					}
				}
				else if (treArmor.SelectedNode.Level == 1)
				{
					// Open the Armor XML file and locate the selected Gear.
					objXmlDocument = XmlManager.Instance.Load("armor.xml");
					objXmlGear = objXmlDocument.SelectSingleNode("/chummer/armors/armor[name = \"" + objSelectedArmor.Name + "\"]");

					if (objXmlGear.InnerXml.Contains("<addoncategory>"))
					{
						string strCategories = "";
						foreach (XmlNode objXmlCategory in objXmlGear.SelectNodes("addoncategory"))
							strCategories += objXmlCategory.InnerText + ",";
						// Remove the trailing comma.
						strCategories = strCategories.Substring(0, strCategories.Length - 1);
						frmPickGear.AddCategory(strCategories);
					}
				}
			}
			catch
			{
			}

			frmPickGear.ShowDialog(this);

			// Make sure the dialogue window was not canceled.
			if (frmPickGear.DialogResult == DialogResult.Cancel)
				return false;

			TreeNode objNode = new TreeNode();

			// Open the Cyberware XML file and locate the selected piece.
			objXmlDocument = XmlManager.Instance.Load("gear.xml");
			objXmlGear = objXmlDocument.SelectSingleNode("/chummer/gears/gear[name = \"" + frmPickGear.SelectedGear + "\" and category = \"" + frmPickGear.SelectedCategory + "\"]");

			// Create the new piece of Gear.
			List<Weapon> objWeapons = new List<Weapon>();
			List<TreeNode> objWeaponNodes = new List<TreeNode>();
			Gear objNewGear = new Gear(_objCharacter);
			switch (frmPickGear.SelectedCategory)
			{
				case "Commlink":
				case "Commlink Upgrade":
					Commlink objCommlink = new Commlink(_objCharacter);
					objCommlink.Create(objXmlGear, _objCharacter, objNode, frmPickGear.SelectedRating);
					objCommlink.Quantity = frmPickGear.SelectedQty;
					try
					{
						nudGearQty.Increment = objCommlink.CostFor;
						//nudGearQty.Minimum = nudGearQty.Increment;
					}
					catch
					{
					}

					objNewGear = objCommlink;
					break;
				case "Commlink Operating System":
				case "Commlink Operating System Upgrade":
					OperatingSystem objOperatingSystem = new OperatingSystem(_objCharacter);
					objOperatingSystem.Create(objXmlGear, _objCharacter, objNode, frmPickGear.SelectedRating);
					objOperatingSystem.Quantity = frmPickGear.SelectedQty;
					try
					{
						nudGearQty.Increment = objOperatingSystem.CostFor;
						//nudGearQty.Minimum = nudGearQty.Increment;
					}
					catch
					{
					}

					objNewGear = objOperatingSystem;
					break;
				default:
					Gear objGear = new Gear(_objCharacter);
					objGear.Create(objXmlGear, _objCharacter, objNode, frmPickGear.SelectedRating, objWeapons, objWeaponNodes, "", false, false, true, true, frmPickGear.Aerodynamic);
					objGear.Quantity = frmPickGear.SelectedQty;
					try
					{
						nudGearQty.Increment = objGear.CostFor;
						//nudGearQty.Minimum = nudGearQty.Increment;
					}
					catch
					{
					}

					objNewGear = objGear;
					break;
			}

			if (objNewGear.InternalId == Guid.Empty.ToString())
				return false;

			if (!blnNullParent)
				objNewGear.Parent = objSelectedGear;

			// Reduce the cost for Do It Yourself components.
			if (frmPickGear.DoItYourself)
				objNewGear.Cost = (Convert.ToDouble(objNewGear.Cost, GlobalOptions.Instance.CultureInfo) * 0.5).ToString();
			// If the item was marked as free, change its cost.
			if (frmPickGear.FreeCost)
			{
				objNewGear.Cost = "0";
				objNewGear.Cost3 = "0";
				objNewGear.Cost6 = "0";
				objNewGear.Cost10 = "0";
			}

			// Create any Weapons that came with this Gear.
			foreach (Weapon objWeapon in objWeapons)
				_objCharacter.Weapons.Add(objWeapon);

			foreach (TreeNode objWeaponNode in objWeaponNodes)
			{
				objWeaponNode.ContextMenuStrip = cmsWeapon;
				treWeapons.Nodes[0].Nodes.Add(objWeaponNode);
				treWeapons.Nodes[0].Expand();
			}

			bool blnMatchFound = false;
			// If this is Ammunition, see if the character already has it on them.
			if (objNewGear.Category == "Ammunition")
			{
				foreach (Gear objCharacterGear in _objCharacter.Gear)
				{
					if (objCharacterGear.Name == objNewGear.Name && objCharacterGear.Category == objNewGear.Category && objCharacterGear.Rating == objNewGear.Rating && objCharacterGear.Extra == objNewGear.Extra)
					{
						// A match was found, so increase the quantity instead.
						objCharacterGear.Quantity += objNewGear.Quantity;
						blnMatchFound = true;

						foreach (TreeNode objGearNode in treGear.Nodes[0].Nodes)
						{
							if (objCharacterGear.InternalId == objGearNode.Tag.ToString())
							{
								objGearNode.Text = objCharacterGear.DisplayName;
								treGear.SelectedNode = objGearNode;
								break;
							}
						}

						break;
					}
				}
			}

			// Add the Gear.
			if (!blnMatchFound)
			{
				if (objSelectedGear.Name == string.Empty)
				{
					objNode.ContextMenuStrip = cmsArmorGear;
					treArmor.SelectedNode.Nodes.Add(objNode);
					treArmor.SelectedNode.Expand();
					objSelectedArmor.Gear.Add(objNewGear);
				}
				else
				{
					objNode.ContextMenuStrip = cmsArmorGear;
					treArmor.SelectedNode.Nodes.Add(objNode);
					treArmor.SelectedNode.Expand();
					objSelectedGear.Children.Add(objNewGear);
				}

				// Select the node that was just added.
				treGear.SelectedNode = objNode;
			}

			UpdateCharacterInfo();
			RefreshSelectedArmor();

			_blnIsDirty = true;
			UpdateWindowTitle();

			return frmPickGear.AddAgain;
		}

		/// <summary>
		/// Refresh the currently-selected Lifestyle.
		/// </summary>
		private void RefreshSelectedLifestyle()
		{
			bool blnClear = false;
			try
			{
				if (treLifestyles.SelectedNode.Level == 0)
					blnClear = true;
			}
			catch
			{
				blnClear = true;
			}
			if (blnClear)
			{
				lblLifestyleCost.Text = "";
				lblLifestyleTotalCost.Text = "";
				lblLifestyleSource.Text = "";
				tipTooltip.SetToolTip(lblLifestyleSource, null);
				lblLifestyleComforts.Text = "";
				lblLifestyleEntertainment.Text = "";
				lblLifestyleNecessities.Text = "";
				lblLifestyleNeighborhood.Text = "";
				lblLifestyleSecurity.Text = "";
				lblLifestyleQualities.Text = "";
				nudLifestyleMonths.Enabled = false;
				return;
			}

			if (treLifestyles.SelectedNode.Level > 0)
			{
				_blnSkipRefresh = true;

				nudLifestyleMonths.Enabled = true;

				// Locate the selected Lifestyle.
				Lifestyle objLifestyle = _objFunctions.FindLifestyle(treLifestyles.SelectedNode.Tag.ToString(), _objCharacter.Lifestyles);
				if (objLifestyle == null)
					return;

				decimal decMultiplier = 1.0m;
				decimal decModifier = Convert.ToDecimal(_objImprovementManager.ValueOf(Improvement.ImprovementType.LifestyleCost), GlobalOptions.Instance.CultureInfo);
				if (objLifestyle.StyleType == LifestyleType.Standard)
					decModifier += Convert.ToDecimal(_objImprovementManager.ValueOf(Improvement.ImprovementType.BasicLifestyleCost), GlobalOptions.Instance.CultureInfo);
				decMultiplier = 1.0m + Convert.ToDecimal(decModifier / 100, GlobalOptions.Instance.CultureInfo);

				lblLifestyleCost.Text = String.Format("{0:###,###,##0¥}", objLifestyle.TotalMonthlyCost);
				nudLifestyleMonths.Value = Convert.ToDecimal(objLifestyle.Months, GlobalOptions.Instance.CultureInfo);
				lblLifestyleStartingNuyen.Text = objLifestyle.Dice.ToString() + "D6 x " + String.Format("{0:###,###,##0¥}", objLifestyle.Multiplier);
				string strBook = _objOptions.LanguageBookShort(objLifestyle.Source);
				string strPage = objLifestyle.Page;
				lblLifestyleSource.Text = strBook + " " + strPage;
				tipTooltip.SetToolTip(lblLifestyleSource, _objOptions.LanguageBookLong(objLifestyle.Source) + " " + LanguageManager.Instance.GetString("String_Page") + " " + objLifestyle.Page);
				lblLifestyleTotalCost.Text = String.Format("= {0:###,###,##0¥}", objLifestyle.TotalCost);

				// Change the Cost/Month label.
				if (objLifestyle.StyleType == LifestyleType.Safehouse)
					lblLifestyleCostLabel.Text = LanguageManager.Instance.GetString("Label_SelectLifestyle_CostPerWeek");
				else
					lblLifestyleCostLabel.Text = LanguageManager.Instance.GetString("Label_SelectLifestyle_CostPerMonth");

				if (objLifestyle.Comforts != "")
				{
					XmlDocument objXmlDocument = XmlManager.Instance.Load("lifestyles.xml");
					string strComforts = "";
					string strEntertainment = "";
					string strNecessities = "";
					string strNeighborhood = "";
					string strSecurity = "";
					string strQualities = "";

					lblLifestyleQualities.Text = "";
					XmlNode objNode = objXmlDocument.SelectSingleNode("/chummer/comforts/comfort[name = \"" + objLifestyle.Comforts + "\"]");
					if (objNode["translate"] != null)
						strComforts = objNode["translate"].InnerText;
					else
						strComforts = objNode["name"].InnerText;
					objNode = objXmlDocument.SelectSingleNode("/chummer/entertainments/entertainment[name = \"" + objLifestyle.Entertainment + "\"]");
					if (objNode["translate"] != null)
						strEntertainment = objNode["translate"].InnerText;
					else
						strEntertainment = objNode["name"].InnerText;
					objNode = objXmlDocument.SelectSingleNode("/chummer/necessities/necessity[name = \"" + objLifestyle.Necessities + "\"]");
					if (objNode["translate"] != null)
						strNecessities = objNode["translate"].InnerText;
					else
						strNecessities = objNode["name"].InnerText;
					objNode = objXmlDocument.SelectSingleNode("/chummer/neighborhoods/neighborhood[name = \"" + objLifestyle.Neighborhood + "\"]");
					if (objNode["translate"] != null)
						strNeighborhood = objNode["translate"].InnerText;
					else
						strNeighborhood = objNode["name"].InnerText;
					objNode = objXmlDocument.SelectSingleNode("/chummer/securities/security[name = \"" + objLifestyle.Security + "\"]");
					if (objNode["translate"] != null)
						strSecurity = objNode["translate"].InnerText;
					else
						strSecurity = objNode["name"].InnerText;

					foreach (string strQuality in objLifestyle.Qualities)
					{
						string strQualityName = strQuality.Substring(0, strQuality.IndexOf('[') - 1);
						objNode = objXmlDocument.SelectSingleNode("/chummer/qualities/quality[name = \"" + strQualityName + "\"]");
						if (objNode["translate"] != null)
							strQualities += objNode["translate"].InnerText;
						else
							strQualities += objNode["name"].InnerText;
						strQualities += " [" + objNode["lp"].InnerText + "LP]\n";
					}

					lblLifestyleComforts.Text = strComforts;
					lblLifestyleEntertainment.Text = strEntertainment;
					lblLifestyleNecessities.Text = strNecessities;
					lblLifestyleNeighborhood.Text = strNeighborhood;
					lblLifestyleSecurity.Text = strSecurity;
					lblLifestyleQualities.Text += strQualities;
				}
				else
				{
					lblLifestyleComforts.Text = "";
					lblLifestyleEntertainment.Text = "";
					lblLifestyleNecessities.Text = "";
					lblLifestyleNeighborhood.Text = "";
					lblLifestyleSecurity.Text = "";
					lblLifestyleQualities.Text = "";
				}

				_blnSkipRefresh = false;
			}
		}

		/// <summary>
		/// Refresh the currently-selected Vehicle.
		/// </summary>
		private void RefreshSelectedVehicle()
		{
			bool blnClear = false;

			try
			{
				if (treVehicles.SelectedNode.Level == 0)
					blnClear = true;
			}
			catch
			{
				blnClear = true;
			}
			if (blnClear)
			{
				_blnSkipRefresh = true;
				nudVehicleRating.Enabled = false;
				nudVehicleGearQty.Enabled = false;

				lblVehicleWeaponName.Text = "";
				lblVehicleWeaponCategory.Text = "";
				lblVehicleWeaponAP.Text = "";
				lblVehicleWeaponDamage.Text = "";
				lblVehicleWeaponMode.Text = "";
				lblVehicleWeaponAmmo.Text = "";

				lblVehicleWeaponRangeShort.Text = "";
				lblVehicleWeaponRangeMedium.Text = "";
				lblVehicleWeaponRangeLong.Text = "";
				lblVehicleWeaponRangeExtreme.Text = "";

				_blnSkipRefresh = false;
				chkVehicleWeaponAccessoryInstalled.Enabled = false;
				return;
			}
			nudVehicleGearQty.Enabled = false;
			chkVehicleHomeNode.Visible = false;
			
			// Locate the selected Vehicle.
			if (treVehicles.SelectedNode.Level == 1)
			{
				Vehicle objVehicle = _objFunctions.FindVehicle(treVehicles.SelectedNode.Tag.ToString(), _objCharacter.Vehicles);
				if (objVehicle == null)
					return;

				_blnSkipRefresh = true;
				lblVehicleRatingLabel.Text = LanguageManager.Instance.GetString("Label_Rating");
				nudVehicleRating.Minimum = 0;
				nudVehicleRating.Maximum = 0;
				nudVehicleRating.Enabled = false;
				chkVehicleBlackMarketDiscount.Checked = objVehicle.DiscountCost;
				_blnSkipRefresh = false;

				lblVehicleName.Text = objVehicle.DisplayNameShort;
				lblVehicleCategory.Text = objVehicle.DisplayCategory;
				lblVehicleAvail.Text = objVehicle.CalculatedAvail;
				lblVehicleCost.Text = String.Format("{0:###,###,##0¥}", objVehicle.TotalCost);
				lblVehicleHandling.Text = objVehicle.TotalHandling.ToString();
				lblVehicleAccel.Text = objVehicle.TotalAccel;
				lblVehicleSpeed.Text = objVehicle.TotalSpeed.ToString();
				lblVehicleDevice.Text = objVehicle.DeviceRating.ToString();
				lblVehiclePilot.Text = objVehicle.Pilot.ToString();
				lblVehicleBody.Text = objVehicle.TotalBody.ToString();
				lblVehicleArmor.Text = objVehicle.TotalArmor.ToString();
				lblVehicleFirewall.Text = objVehicle.Firewall.ToString();
				lblVehicleSignal.Text = objVehicle.Signal.ToString();
				lblVehicleResponse.Text = objVehicle.Response.ToString();
				lblVehicleSystem.Text = objVehicle.System.ToString();
				if (_objOptions.UseCalculatedVehicleSensorRatings)
					lblVehicleSensor.Text = objVehicle.CalculatedSensor.ToString() + " (" + LanguageManager.Instance.GetString("Label_Signal") + " " + objVehicle.SensorSignal.ToString() + ")";
				else
					lblVehicleSensor.Text = objVehicle.Sensor.ToString() + " (" + LanguageManager.Instance.GetString("Label_Signal") + " " + objVehicle.SensorSignal.ToString() + ")";
				lblVehicleSlots.Text = objVehicle.Slots.ToString() + " (" + (objVehicle.Slots - objVehicle.SlotsUsed).ToString() + " " + LanguageManager.Instance.GetString("String_Remaining") + ")";
				string strBook = _objOptions.LanguageBookShort(objVehicle.Source);
				string strPage = objVehicle.Page;
				lblVehicleSource.Text = strBook + " " + strPage;
				tipTooltip.SetToolTip(lblVehicleSource, _objOptions.LanguageBookLong(objVehicle.Source) + " " + LanguageManager.Instance.GetString("String_Page") + " " + objVehicle.Page);
				chkVehicleWeaponAccessoryInstalled.Enabled = false;
				chkVehicleIncludedInWeapon.Checked = false;

				if (_objCharacter.Metatype.EndsWith("A.I.") || _objCharacter.MetatypeCategory == "Technocritters" || _objCharacter.MetatypeCategory == "Protosapients")
				{
					chkVehicleHomeNode.Visible = true;
					chkVehicleHomeNode.Checked = objVehicle.HomeNode;
				}

				UpdateCharacterInfo();
			}
			else if (treVehicles.SelectedNode.Level == 2)
			{
				// If this is a Vehicle Location, don't do anything.
				foreach (Vehicle objVehicle in _objCharacter.Vehicles)
				{
					if (objVehicle.InternalId == treVehicles.SelectedNode.Parent.Tag.ToString())
					{
						foreach (string strLocation in objVehicle.Locations)
						{
							if (strLocation == treVehicles.SelectedNode.Tag.ToString())
							{
								lblVehicleName.Text = "";
								lblVehicleCategory.Text = "";
								lblVehicleSource.Text = "";
								lblVehicleHandling.Text = "";
								lblVehicleAccel.Text = "";
								lblVehicleSpeed.Text = "";
								lblVehicleDevice.Text = "";
								lblVehiclePilot.Text = "";
								lblVehicleBody.Text = "";
								lblVehicleArmor.Text = "";
								lblVehicleSensor.Text = "";
								lblVehicleFirewall.Text = "";
								lblVehicleSignal.Text = "";
								lblVehicleResponse.Text = "";
								lblVehicleSystem.Text = "";
								lblVehicleAvail.Text = "";
								lblVehicleCost.Text = "";
								lblVehicleSlots.Text = "";
								return;
							}
						}
					}
				}

				bool blnVehicleMod = false;

				// Locate the selected VehicleMod.
				Vehicle objSelectedVehicle = new Vehicle(_objCharacter);
				VehicleMod objMod = _objFunctions.FindVehicleMod(treVehicles.SelectedNode.Tag.ToString(), _objCharacter.Vehicles, out objSelectedVehicle);
				if (objMod != null)
					blnVehicleMod = true;

				if (blnVehicleMod)
				{
					if (objMod.MaxRating != "qty")
					{
						if (Convert.ToInt32(objMod.MaxRating) > 0)
						{
							_blnSkipRefresh = true;
							lblVehicleRatingLabel.Text = LanguageManager.Instance.GetString("Label_Rating");
							// If the Mod is Armor, use the lower of the Mod's maximum Rating and MaxArmor value for the Vehicle instead.
							if (objMod.Name.StartsWith("Armor,"))
								nudVehicleRating.Maximum = Math.Min(Convert.ToInt32(objMod.MaxRating), objSelectedVehicle.MaxArmor);
							else
								nudVehicleRating.Maximum = Convert.ToInt32(objMod.MaxRating);
							nudVehicleRating.Minimum = 1;
							nudVehicleRating.Value = objMod.Rating;
							nudVehicleRating.Increment = 1;
							nudVehicleRating.Enabled = !objMod.IncludedInVehicle;
							chkVehicleBlackMarketDiscount.Checked = objMod.DiscountCost;
							_blnSkipRefresh = false;
						}
						else
						{
							_blnSkipRefresh = true;
							lblVehicleRatingLabel.Text = LanguageManager.Instance.GetString("Label_Rating");
							nudVehicleRating.Minimum = 0;
							nudVehicleRating.Increment = 1;
							nudVehicleRating.Maximum = 0;
							nudVehicleRating.Enabled = false;
							chkVehicleBlackMarketDiscount.Checked = objMod.DiscountCost;
							_blnSkipRefresh = false;
						}
					}
					else
					{
						_blnSkipRefresh = true;
						lblVehicleRatingLabel.Text = LanguageManager.Instance.GetString("Label_Qty");
						nudVehicleRating.Minimum = 1;
						nudVehicleRating.Maximum = 20;
						nudVehicleRating.Value = objMod.Rating;
						nudVehicleRating.Increment = 1;
						nudVehicleRating.Enabled = !objMod.IncludedInVehicle;
						chkVehicleBlackMarketDiscount.Checked = objMod.DiscountCost;
						_blnSkipRefresh = false;
					}

					lblVehicleName.Text = objMod.DisplayNameShort;
					lblVehicleCategory.Text = LanguageManager.Instance.GetString("String_VehicleModification");
					lblVehicleAvail.Text = objMod.TotalAvail;
					lblVehicleCost.Text = String.Format("{0:###,###,##0¥}", objMod.TotalCost);
					lblVehicleHandling.Text = "";
					lblVehicleAccel.Text = "";
					lblVehicleSpeed.Text = "";
					lblVehicleDevice.Text = "";
					lblVehiclePilot.Text = "";
					lblVehicleBody.Text = "";
					lblVehicleArmor.Text = "";
					lblVehicleSensor.Text = "";
					lblVehicleFirewall.Text = "";
					lblVehicleSignal.Text = "";
					lblVehicleResponse.Text = "";
					lblVehicleSystem.Text = "";
					lblVehicleSlots.Text = objMod.CalculatedSlots.ToString();
					string strBook = _objOptions.LanguageBookShort(objMod.Source);
					string strPage = objMod.Page;
					lblVehicleSource.Text = strBook + " " + strPage;
					tipTooltip.SetToolTip(lblVehicleSource, _objOptions.LanguageBookLong(objMod.Source) + " " + LanguageManager.Instance.GetString("String_Page") + " " + objMod.Page);
				}
				else
				{
					bool blnFound = false;
					// If it's not a Vehicle Mod then it must be a Sensor.
					Gear objGear = _objFunctions.FindVehicleGear(treVehicles.SelectedNode.Tag.ToString(), _objCharacter.Vehicles, out objSelectedVehicle);
					if (objGear != null)
						blnFound = true;

					if (blnFound)
					{
						_blnSkipRefresh = true;
						nudVehicleRating.Enabled = false;
						nudVehicleGearQty.Enabled = true;
						nudVehicleGearQty.Maximum = 100000;
						//nudVehicleGearQty.Minimum = objGear.CostFor;
						nudVehicleGearQty.Value = objGear.Quantity;
						nudVehicleGearQty.Increment = objGear.CostFor;
						chkVehicleBlackMarketDiscount.Checked = objGear.DiscountCost;

						if (objGear.MaxRating > 0)
						{
							lblVehicleRatingLabel.Text = LanguageManager.Instance.GetString("Label_Rating");
							nudVehicleRating.Enabled = true;
							nudVehicleRating.Maximum = objGear.MaxRating;
							nudVehicleRating.Value = objGear.Rating;
						}
						_blnSkipRefresh = false;

						lblVehicleName.Text = objGear.DisplayNameShort;
						lblVehicleCategory.Text = objGear.DisplayCategory;
						lblVehicleAvail.Text = objGear.TotalAvail(true);
						lblVehicleCost.Text = String.Format("{0:###,###,##0¥}", objGear.TotalCost);
						lblVehicleHandling.Text = "";
						lblVehicleAccel.Text = "";
						lblVehicleSpeed.Text = "";
						lblVehicleDevice.Text = "";
						lblVehiclePilot.Text = "";
						lblVehicleBody.Text = "";
						lblVehicleArmor.Text = "";
						lblVehicleSensor.Text = "";
						lblVehicleFirewall.Text = "";
						lblVehicleSignal.Text = "";
						lblVehicleResponse.Text = "";
						lblVehicleSystem.Text = "";
						lblVehicleSlots.Text = objGear.CalculatedCapacity + " (" + objGear.CapacityRemaining.ToString() + " " + LanguageManager.Instance.GetString("String_Remaining") + ")";
						string strBook = _objOptions.LanguageBookShort(objGear.Source);
						string strPage = objGear.Page;
						lblVehicleSource.Text = strBook + " " + strPage;
						tipTooltip.SetToolTip(lblVehicleSource, _objOptions.LanguageBookLong(objGear.Source) + " " + LanguageManager.Instance.GetString("String_Page") + " " + objGear.Page);

						if ((_objCharacter.Metatype.EndsWith("A.I.") || _objCharacter.MetatypeCategory == "Technocritters" || _objCharacter.MetatypeCategory == "Protosapients") && objGear.GetType() == typeof(Commlink))
						{
							chkVehicleHomeNode.Visible = true;
							chkVehicleHomeNode.Checked = objGear.HomeNode;
						}
					}
					else
					{
						// Look for the selected Vehicle Weapon.
						Weapon objWeapon = new Weapon(_objCharacter);

						foreach (Vehicle objVehicle in _objCharacter.Vehicles)
						{
							objWeapon = _objFunctions.FindWeapon(treVehicles.SelectedNode.Tag.ToString(), objVehicle.Weapons);
							if (objWeapon != null)
								break;
						}

						nudVehicleRating.Enabled = false;

						lblVehicleWeaponName.Text = objWeapon.DisplayNameShort;
						lblVehicleWeaponCategory.Text = objWeapon.DisplayCategory;
						lblVehicleWeaponDamage.Text = objWeapon.CalculatedDamage();
						lblVehicleWeaponAP.Text = objWeapon.TotalAP;
						lblVehicleWeaponAmmo.Text = objWeapon.CalculatedAmmo();
						lblVehicleWeaponMode.Text = objWeapon.CalculatedMode;

						lblVehicleWeaponRangeShort.Text = objWeapon.RangeShort;
						lblVehicleWeaponRangeMedium.Text = objWeapon.RangeMedium;
						lblVehicleWeaponRangeLong.Text = objWeapon.RangeLong;
						lblVehicleWeaponRangeExtreme.Text = objWeapon.RangeExtreme;

						lblVehicleName.Text = objWeapon.DisplayNameShort;
						lblVehicleCategory.Text = LanguageManager.Instance.GetString("String_VehicleWeapon");
						lblVehicleAvail.Text = objWeapon.TotalAvail;
						lblVehicleCost.Text = String.Format("{0:###,###,##0¥}", objWeapon.TotalCost);
						lblVehicleHandling.Text = "";
						lblVehicleAccel.Text = "";
						lblVehicleSpeed.Text = "";
						lblVehicleDevice.Text = "";
						lblVehiclePilot.Text = "";
						lblVehicleBody.Text = "";
						lblVehicleArmor.Text = "";
						lblVehicleSensor.Text = "";
						lblVehicleFirewall.Text = "";
						lblVehicleSignal.Text = "";
						lblVehicleResponse.Text = "";
						lblVehicleSystem.Text = "";
						lblVehicleSlots.Text = "6 (" + objWeapon.SlotsRemaining.ToString() + " " + LanguageManager.Instance.GetString("String_Remaining") + ")";
						string strBook = _objOptions.LanguageBookShort(objWeapon.Source);
						string strPage = objWeapon.Page;
						lblVehicleSource.Text = strBook + " " + strPage;
						_blnSkipRefresh = true;
						chkVehicleBlackMarketDiscount.Checked = objWeapon.DiscountCost;
						_blnSkipRefresh = false;
						tipTooltip.SetToolTip(lblVehicleSource, _objOptions.LanguageBookLong(objWeapon.Source) + " " + LanguageManager.Instance.GetString("String_Page") + " " + objWeapon.Page);
					}
				}
				if (blnVehicleMod)
				{
					chkVehicleWeaponAccessoryInstalled.Enabled = true;
					chkVehicleWeaponAccessoryInstalled.Checked = objMod.Installed;
				}
				else
					chkVehicleWeaponAccessoryInstalled.Enabled = false;
				chkVehicleIncludedInWeapon.Checked = false;
			}
			else if (treVehicles.SelectedNode.Level == 3)
			{
				bool blnSensorPlugin = false;
				Vehicle objSelectedVehicle = new Vehicle(_objCharacter);
				Gear objGear = _objFunctions.FindVehicleGear(treVehicles.SelectedNode.Tag.ToString(), _objCharacter.Vehicles, out objSelectedVehicle);
				if (objGear != null)
					blnSensorPlugin = true;

				if (blnSensorPlugin)
				{
					if (objGear.MaxRating > 0)
					{
						_blnSkipRefresh = true;
						lblVehicleRatingLabel.Text = LanguageManager.Instance.GetString("Label_Rating");
						nudVehicleRating.Minimum = 1;
						nudVehicleRating.Maximum = objGear.MaxRating;
						nudVehicleRating.Value = objGear.Rating;
						nudVehicleRating.Enabled = true;
						chkVehicleBlackMarketDiscount.Checked = objGear.DiscountCost;
						_blnSkipRefresh = false;
					}
					else
					{
						_blnSkipRefresh = true;
						lblVehicleRatingLabel.Text = LanguageManager.Instance.GetString("Label_Rating");
						nudVehicleRating.Minimum = 0;
						nudVehicleRating.Maximum = 0;
						nudVehicleRating.Enabled = false;
						chkVehicleBlackMarketDiscount.Checked = objGear.DiscountCost;
						_blnSkipRefresh = false;
					}

					lblVehicleName.Text = objGear.DisplayNameShort;
					lblVehicleCategory.Text = objGear.DisplayCategory;
					lblVehicleAvail.Text = objGear.TotalAvail(true);
					lblVehicleCost.Text = String.Format("{0:###,###,##0¥}", objGear.TotalCost);
					lblVehicleHandling.Text = "";
					lblVehicleAccel.Text = "";
					lblVehicleSpeed.Text = "";
					lblVehicleDevice.Text = "";
					lblVehiclePilot.Text = "";
					lblVehicleBody.Text = "";
					lblVehicleArmor.Text = "";
					lblVehicleSensor.Text = "";
					lblVehicleFirewall.Text = "";
					lblVehicleSignal.Text = "";
					lblVehicleResponse.Text = "";
					lblVehicleSystem.Text = "";
					lblVehicleSlots.Text = objGear.CalculatedCapacity + " (" + objGear.CapacityRemaining.ToString() + " " + LanguageManager.Instance.GetString("String_Remaining") + ")";
					string strBook = _objOptions.LanguageBookShort(objGear.Source);
					string strPage = objGear.Page;
					lblVehicleSource.Text = strBook + " " + strPage;
					tipTooltip.SetToolTip(lblVehicleSource, _objOptions.LanguageBookLong(objGear.Source) + " " + LanguageManager.Instance.GetString("String_Page") + " " + objGear.Page);

					if ((_objCharacter.Metatype.EndsWith("A.I.") || _objCharacter.MetatypeCategory == "Technocritters" || _objCharacter.MetatypeCategory == "Protosapients") && objGear.GetType() == typeof(Commlink))
					{
						chkVehicleHomeNode.Visible = true;
						chkVehicleHomeNode.Checked = objGear.HomeNode;
					}
				}
				else
				{
					// Look for the selected Vehicle Weapon.
					Weapon objWeapon = new Weapon(_objCharacter);
					bool blnWeapon = false;

					objWeapon = _objFunctions.FindVehicleWeapon(treVehicles.SelectedNode.Tag.ToString(), _objCharacter.Vehicles, out objSelectedVehicle);
					if (objWeapon != null)
						blnWeapon = true;

					if (blnWeapon)
					{
						lblVehicleWeaponName.Text = objWeapon.DisplayNameShort;
						lblVehicleWeaponCategory.Text = objWeapon.DisplayCategory;
						lblVehicleWeaponDamage.Text = objWeapon.CalculatedDamage();
						lblVehicleWeaponAP.Text = objWeapon.TotalAP;
						lblVehicleWeaponAmmo.Text = objWeapon.CalculatedAmmo();
						lblVehicleWeaponMode.Text = objWeapon.CalculatedMode;

						lblVehicleWeaponRangeShort.Text = objWeapon.RangeShort;
						lblVehicleWeaponRangeMedium.Text = objWeapon.RangeMedium;
						lblVehicleWeaponRangeLong.Text = objWeapon.RangeLong;
						lblVehicleWeaponRangeExtreme.Text = objWeapon.RangeExtreme;

						lblVehicleName.Text = objWeapon.DisplayNameShort;
						lblVehicleCategory.Text = LanguageManager.Instance.GetString("String_VehicleWeapon");
						lblVehicleAvail.Text = objWeapon.TotalAvail;
						lblVehicleCost.Text = String.Format("{0:###,###,##0¥}", objWeapon.TotalCost);
						lblVehicleHandling.Text = "";
						lblVehicleAccel.Text = "";
						lblVehicleSpeed.Text = "";
						lblVehicleDevice.Text = "";
						lblVehiclePilot.Text = "";
						lblVehicleBody.Text = "";
						lblVehicleArmor.Text = "";
						lblVehicleSensor.Text = "";
						lblVehicleFirewall.Text = "";
						lblVehicleSignal.Text = "";
						lblVehicleResponse.Text = "";
						lblVehicleSystem.Text = "";
						lblVehicleSlots.Text = "6 (" + objWeapon.SlotsRemaining.ToString() + " " + LanguageManager.Instance.GetString("String_Remaining") + ")";
						string strBook = _objOptions.LanguageBookShort(objWeapon.Source);
						string strPage = objWeapon.Page;
						lblVehicleSource.Text = strBook + " " + strPage;
						_blnSkipRefresh = true;
						chkVehicleBlackMarketDiscount.Checked = objWeapon.DiscountCost;
						_blnSkipRefresh = false;
						tipTooltip.SetToolTip(lblVehicleSource, _objOptions.LanguageBookLong(objWeapon.Source) + " " + LanguageManager.Instance.GetString("String_Page") + " " + objWeapon.Page);
					}
					else
					{
						bool blnCyberware = false;
						// See if this is a piece of Cyberware.
						Cyberware objCyberware = _objFunctions.FindVehicleCyberware(treVehicles.SelectedNode.Tag.ToString(), _objCharacter.Vehicles);
						if (objCyberware != null)
							blnCyberware = true;

						if (blnCyberware)
						{
							_blnSkipRefresh = true;
							lblVehicleName.Text = objCyberware.DisplayNameShort;
							lblVehicleRatingLabel.Text = LanguageManager.Instance.GetString("Label_Rating");
							nudVehicleRating.Minimum = objCyberware.MinRating;
							nudVehicleRating.Maximum = objCyberware.MaxRating;
							nudVehicleRating.Value = objCyberware.Rating;
							chkVehicleBlackMarketDiscount.Checked = objCyberware.DiscountCost;
							_blnSkipRefresh = false;

							lblVehicleName.Text = objCyberware.DisplayNameShort;
							lblVehicleCategory.Text = LanguageManager.Instance.GetString("String_VehicleModification");
							lblVehicleAvail.Text = objCyberware.TotalAvail;
							lblVehicleCost.Text = String.Format("{0:###,###,##0¥}", objCyberware.TotalCost);
							lblVehicleHandling.Text = "";
							lblVehicleAccel.Text = "";
							lblVehicleSpeed.Text = "";
							lblVehicleDevice.Text = "";
							lblVehiclePilot.Text = "";
							lblVehicleBody.Text = "";
							lblVehicleArmor.Text = "";
							lblVehicleSensor.Text = "";
							lblVehicleSlots.Text = "";
							lblVehicleFirewall.Text = "";
							lblVehicleSignal.Text = "";
							lblVehicleResponse.Text = "";
							lblVehicleSystem.Text = "";
							string strBook = _objOptions.LanguageBookShort(objCyberware.Source);
							string strPage = objCyberware.Page;
							lblVehicleSource.Text = strBook + " " + strPage;
							tipTooltip.SetToolTip(lblVehicleSource, _objOptions.LanguageBookLong(objCyberware.Source) + " " + LanguageManager.Instance.GetString("String_Page") + " " + objCyberware.Page);
						}
					}
				}
				chkVehicleWeaponAccessoryInstalled.Enabled = false;
				chkVehicleIncludedInWeapon.Checked = false;
			}
			else if (treVehicles.SelectedNode.Level == 4)
			{
				bool blnSensorPlugin = false;
				Vehicle objSelectedVehicle = new Vehicle(_objCharacter);
				Gear objGear = _objFunctions.FindVehicleGear(treVehicles.SelectedNode.Tag.ToString(), _objCharacter.Vehicles, out objSelectedVehicle);
				if (objGear != null)
					blnSensorPlugin = true;

				if (blnSensorPlugin)
				{
					if (objGear.MaxRating > 0)
					{
						_blnSkipRefresh = true;
						lblVehicleRatingLabel.Text = LanguageManager.Instance.GetString("Label_Rating");
						nudVehicleRating.Minimum = 1;
						nudVehicleRating.Maximum = objGear.MaxRating;
						nudVehicleRating.Value = objGear.Rating;
						nudVehicleRating.Enabled = true;
						chkVehicleBlackMarketDiscount.Checked = objGear.DiscountCost;
						_blnSkipRefresh = false;
					}
					else
					{
						_blnSkipRefresh = true;
						lblVehicleRatingLabel.Text = LanguageManager.Instance.GetString("Label_Rating");
						nudVehicleRating.Minimum = 0;
						nudVehicleRating.Maximum = 0;
						nudVehicleRating.Enabled = false;
						chkVehicleBlackMarketDiscount.Checked = objGear.DiscountCost;
						_blnSkipRefresh = false;
					}

					lblVehicleName.Text = objGear.DisplayNameShort;
					lblVehicleCategory.Text = objGear.DisplayCategory;
					lblVehicleAvail.Text = objGear.TotalAvail(true);
					lblVehicleCost.Text = String.Format("{0:###,###,##0¥}", objGear.TotalCost);
					lblVehicleHandling.Text = "";
					lblVehicleAccel.Text = "";
					lblVehicleSpeed.Text = "";
					lblVehicleDevice.Text = "";
					lblVehiclePilot.Text = "";
					lblVehicleBody.Text = "";
					lblVehicleArmor.Text = "";
					lblVehicleSensor.Text = "";
					lblVehicleFirewall.Text = "";
					lblVehicleSignal.Text = "";
					lblVehicleResponse.Text = "";
					lblVehicleSystem.Text = "";
					lblVehicleSlots.Text = objGear.CalculatedCapacity + " (" + objGear.CapacityRemaining.ToString() + " " + LanguageManager.Instance.GetString("String_Remaining") + ")";
					string strBook = _objOptions.LanguageBookShort(objGear.Source);
					string strPage = objGear.Page;
					lblVehicleSource.Text = strBook + " " + strPage;
					tipTooltip.SetToolTip(lblVehicleSource, _objOptions.LanguageBookLong(objGear.Source) + " " + LanguageManager.Instance.GetString("String_Page") + " " + objGear.Page);

					if ((_objCharacter.Metatype.EndsWith("A.I.") || _objCharacter.MetatypeCategory == "Technocritters" || _objCharacter.MetatypeCategory == "Protosapients") && objGear.GetType() == typeof(Commlink))
					{
						chkVehicleHomeNode.Visible = true;
						chkVehicleHomeNode.Checked = objGear.HomeNode;
					}
				}
				else
				{
					bool blnAccessory = false;

					// Locate the the Selected Vehicle Weapon Accessory of Modification.
					Weapon objWeapon = new Weapon(_objCharacter);
					WeaponAccessory objAccessory = _objFunctions.FindVehicleWeaponAccessory(treVehicles.SelectedNode.Tag.ToString(), _objCharacter.Vehicles);
					if (objAccessory != null)
					{
						objWeapon = objAccessory.Parent;
						blnAccessory = true;
					}

					if (blnAccessory)
					{
						lblVehicleName.Text = objAccessory.DisplayNameShort;
						lblVehicleCategory.Text = LanguageManager.Instance.GetString("String_VehicleWeaponAccessory");
						lblVehicleAvail.Text = objAccessory.TotalAvail;
						lblVehicleCost.Text = String.Format("{0:###,###,##0¥}", Convert.ToInt32(objAccessory.TotalCost));
						lblVehicleHandling.Text = "";
						lblVehicleAccel.Text = "";
						lblVehicleSpeed.Text = "";
						lblVehicleDevice.Text = "";
						lblVehiclePilot.Text = "";
						lblVehicleBody.Text = "";
						lblVehicleArmor.Text = "";
						lblVehicleSensor.Text = "";
						lblVehicleFirewall.Text = "";
						lblVehicleSignal.Text = "";
						lblVehicleResponse.Text = "";
						lblVehicleSystem.Text = "";
						_blnSkipRefresh = true;
						chkVehicleBlackMarketDiscount.Checked = objAccessory.DiscountCost;
						_blnSkipRefresh = false;

						string[] strMounts = objAccessory.Mount.Split('/');
						string strMount = "";
						foreach (string strCurrentMount in strMounts)
						{
							if (strCurrentMount != "")
								strMount += LanguageManager.Instance.GetString("String_Mount" + strCurrentMount) + "/";
						}
						// Remove the trailing /
						if (strMount != "" && strMount.Contains('/'))
							strMount = strMount.Substring(0, strMount.Length - 1);

						lblVehicleSlots.Text = strMount;
						string strBook = _objOptions.LanguageBookShort(objAccessory.Source);
						string strPage = objAccessory.Page;
						lblVehicleSource.Text = strBook + " " + strPage;
						tipTooltip.SetToolTip(lblVehicleSource, _objOptions.LanguageBookLong(objAccessory.Source) + " " + LanguageManager.Instance.GetString("String_Page") + " " + objAccessory.Page);
						chkVehicleWeaponAccessoryInstalled.Enabled = true;
						chkVehicleWeaponAccessoryInstalled.Checked = objAccessory.Installed;
						chkVehicleIncludedInWeapon.Checked = objAccessory.IncludedInWeapon;
					}
					else
					{
						bool blnMod = false;
						// Locate the selected Vehicle Weapon Modification.
						WeaponMod objMod = _objFunctions.FindVehicleWeaponMod(treVehicles.SelectedNode.Tag.ToString(), _objCharacter.Vehicles);
						if (objMod != null)
						{
							objWeapon = objMod.Parent;
							blnMod = true;
						}

						if (blnMod)
						{
							lblVehicleName.Text = objMod.DisplayNameShort;
							lblVehicleCategory.Text = LanguageManager.Instance.GetString("String_VehicleWeaponModification");
							lblVehicleAvail.Text = objMod.TotalAvail;
							lblVehicleCost.Text = String.Format("{0:###,###,##0¥}", Convert.ToInt32(objMod.TotalCost));
							lblVehicleHandling.Text = "";
							lblVehicleAccel.Text = "";
							lblVehicleSpeed.Text = "";
							lblVehicleDevice.Text = "";
							lblVehiclePilot.Text = "";
							lblVehicleBody.Text = "";
							lblVehicleArmor.Text = "";
							lblVehicleSensor.Text = "";
							lblVehicleFirewall.Text = "";
							lblVehicleSignal.Text = "";
							lblVehicleResponse.Text = "";
							lblVehicleSystem.Text = "";
							lblVehicleSlots.Text = objMod.Slots.ToString();
							string strBook = _objOptions.LanguageBookShort(objMod.Source);
							string strPage = objMod.Page;
							lblVehicleSource.Text = strBook + " " + strPage;
							tipTooltip.SetToolTip(lblVehicleSource, _objOptions.LanguageBookLong(objMod.Source) + " " + LanguageManager.Instance.GetString("String_Page") + " " + objMod.Page);
							chkVehicleWeaponAccessoryInstalled.Enabled = true;
							chkVehicleWeaponAccessoryInstalled.Checked = objMod.Installed;
							chkVehicleIncludedInWeapon.Checked = objMod.IncludedInWeapon;
							_blnSkipRefresh = true;
							chkVehicleBlackMarketDiscount.Checked = objMod.DiscountCost;
							_blnSkipRefresh = false;
						}
						else
						{
							// If it's none of these, it must be an Underbarrel Weapon.
							Vehicle objFoundVehicle = new Vehicle(_objCharacter);
							objWeapon = _objFunctions.FindVehicleWeapon(treVehicles.SelectedNode.Tag.ToString(), _objCharacter.Vehicles, out objFoundVehicle);

							lblVehicleName.Text = objWeapon.DisplayNameShort;
							lblVehicleCategory.Text = LanguageManager.Instance.GetString("String_VehicleWeapon");
							lblVehicleAvail.Text = objWeapon.TotalAvail;
							lblVehicleCost.Text = String.Format("{0:###,###,##0¥}", objWeapon.TotalCost);
							lblVehicleHandling.Text = "";
							lblVehicleAccel.Text = "";
							lblVehicleSpeed.Text = "";
							lblVehicleDevice.Text = "";
							lblVehiclePilot.Text = "";
							lblVehicleBody.Text = "";
							lblVehicleArmor.Text = "";
							lblVehicleSensor.Text = "";
							lblVehicleFirewall.Text = "";
							lblVehicleSignal.Text = "";
							lblVehicleResponse.Text = "";
							lblVehicleSystem.Text = "";
							lblVehicleSlots.Text = "6 (" + objWeapon.SlotsRemaining.ToString() + " " + LanguageManager.Instance.GetString("String_Remaining") + ")";
							string strBook = _objOptions.LanguageBookShort(objWeapon.Source);
							string strPage = objWeapon.Page;
							lblVehicleSource.Text = strBook + " " + strPage;
							_blnSkipRefresh = true;
							chkVehicleBlackMarketDiscount.Checked = objWeapon.DiscountCost;
							chkVehicleWeaponAccessoryInstalled.Enabled = true;
							chkVehicleWeaponAccessoryInstalled.Checked = objWeapon.Installed;
							_blnSkipRefresh = false;
							tipTooltip.SetToolTip(lblVehicleSource, _objOptions.LanguageBookLong(objWeapon.Source) + " " + LanguageManager.Instance.GetString("String_Page") + " " + objWeapon.Page);
						}
					}
				}
			}
			else if (treVehicles.SelectedNode.Level == 5)
			{
				bool blnFound = false;

				// Locate the the Selected Vehicle Underbarrel Weapon Accessory or Modification.
				Weapon objWeapon = new Weapon(_objCharacter);
				WeaponAccessory objAccessory = _objFunctions.FindVehicleWeaponAccessory(treVehicles.SelectedNode.Tag.ToString(), _objCharacter.Vehicles);
				if (objAccessory != null)
				{
					blnFound = true;
					objWeapon = objAccessory.Parent;
				}

				if (blnFound)
				{
					lblVehicleName.Text = objAccessory.DisplayNameShort;
					lblVehicleCategory.Text = LanguageManager.Instance.GetString("String_VehicleWeaponAccessory");
					lblVehicleAvail.Text = objAccessory.TotalAvail;
					lblVehicleCost.Text = String.Format("{0:###,###,##0¥}", Convert.ToInt32(objAccessory.TotalCost));
					lblVehicleHandling.Text = "";
					lblVehicleAccel.Text = "";
					lblVehicleSpeed.Text = "";
					lblVehicleDevice.Text = "";
					lblVehiclePilot.Text = "";
					lblVehicleBody.Text = "";
					lblVehicleArmor.Text = "";
					lblVehicleSensor.Text = "";
					lblVehicleFirewall.Text = "";
					lblVehicleSignal.Text = "";
					lblVehicleResponse.Text = "";
					lblVehicleSystem.Text = "";

					string[] strMounts = objAccessory.Mount.Split('/');
					string strMount = "";
					foreach (string strCurrentMount in strMounts)
					{
						if (strCurrentMount != "")
							strMount += LanguageManager.Instance.GetString("String_Mount" + strCurrentMount) + "/";
					}
					// Remove the trailing /
					if (strMount != "" && strMount.Contains('/'))
						strMount = strMount.Substring(0, strMount.Length - 1);

					lblVehicleSlots.Text = strMount;
					string strBook = _objOptions.LanguageBookShort(objAccessory.Source);
					string strPage = objAccessory.Page;
					lblVehicleSource.Text = strBook + " " + strPage;
					tipTooltip.SetToolTip(lblVehicleSource, _objOptions.LanguageBookLong(objAccessory.Source) + " " + LanguageManager.Instance.GetString("String_Page") + " " + objAccessory.Page);
					chkVehicleWeaponAccessoryInstalled.Enabled = true;
					chkVehicleWeaponAccessoryInstalled.Checked = objAccessory.Installed;
					chkVehicleIncludedInWeapon.Checked = objAccessory.IncludedInWeapon;
					_blnSkipRefresh = true;
					chkVehicleBlackMarketDiscount.Checked = objAccessory.DiscountCost;
					_blnSkipRefresh = false;
				}
				else
				{
					// Locate the selected Vehicle Weapon Modification.
					WeaponMod objMod = _objFunctions.FindVehicleWeaponMod(treVehicles.SelectedNode.Tag.ToString(), _objCharacter.Vehicles);
					if (objMod != null)
					{
						blnFound = true;
						objWeapon = objMod.Parent;
					}

					if (blnFound)
					{
						lblVehicleName.Text = objMod.DisplayNameShort;
						lblVehicleCategory.Text = LanguageManager.Instance.GetString("String_VehicleWeaponModification");
						lblVehicleAvail.Text = objMod.TotalAvail;
						lblVehicleCost.Text = String.Format("{0:###,###,##0¥}", Convert.ToInt32(objMod.TotalCost));
						lblVehicleHandling.Text = "";
						lblVehicleAccel.Text = "";
						lblVehicleSpeed.Text = "";
						lblVehicleDevice.Text = "";
						lblVehiclePilot.Text = "";
						lblVehicleBody.Text = "";
						lblVehicleArmor.Text = "";
						lblVehicleSensor.Text = "";
						lblVehicleFirewall.Text = "";
						lblVehicleSignal.Text = "";
						lblVehicleResponse.Text = "";
						lblVehicleSystem.Text = "";
						lblVehicleSlots.Text = objMod.Slots.ToString();
						string strBook = _objOptions.LanguageBookShort(objMod.Source);
						string strPage = objMod.Page;
						lblVehicleSource.Text = strBook + " " + strPage;
						tipTooltip.SetToolTip(lblVehicleSource, _objOptions.LanguageBookLong(objMod.Source) + " " + LanguageManager.Instance.GetString("String_Page") + " " + objMod.Page);
						chkVehicleWeaponAccessoryInstalled.Enabled = true;
						chkVehicleWeaponAccessoryInstalled.Checked = objMod.Installed;
						chkVehicleIncludedInWeapon.Checked = objMod.IncludedInWeapon;
						_blnSkipRefresh = true;
						chkVehicleBlackMarketDiscount.Checked = objMod.DiscountCost;
						_blnSkipRefresh = false;
					}
					else
					{
						Vehicle objSelectedVehicle = new Vehicle(_objCharacter);
						Gear objGear = _objFunctions.FindVehicleGear(treVehicles.SelectedNode.Tag.ToString(), _objCharacter.Vehicles, out objSelectedVehicle);

						if (objGear.MaxRating > 0)
						{
							_blnSkipRefresh = true;
							lblVehicleRatingLabel.Text = LanguageManager.Instance.GetString("Label_Rating");
							nudVehicleRating.Minimum = 1;
							nudVehicleRating.Maximum = objGear.MaxRating;
							nudVehicleRating.Value = objGear.Rating;
							nudVehicleRating.Enabled = true;
							chkVehicleBlackMarketDiscount.Checked = objGear.DiscountCost;
							_blnSkipRefresh = false;
						}
						else
						{
							_blnSkipRefresh = true;
							lblVehicleRatingLabel.Text = LanguageManager.Instance.GetString("Label_Rating");
							nudVehicleRating.Minimum = 0;
							nudVehicleRating.Maximum = 0;
							nudVehicleRating.Enabled = false;
							chkVehicleBlackMarketDiscount.Checked = objGear.DiscountCost;
							_blnSkipRefresh = false;
						}

						lblVehicleName.Text = objGear.DisplayNameShort;
						lblVehicleCategory.Text = objGear.DisplayCategory;
						lblVehicleAvail.Text = objGear.TotalAvail(true);
						lblVehicleCost.Text = String.Format("{0:###,###,##0¥}", objGear.TotalCost);
						lblVehicleHandling.Text = "";
						lblVehicleAccel.Text = "";
						lblVehicleSpeed.Text = "";
						lblVehicleDevice.Text = "";
						lblVehiclePilot.Text = "";
						lblVehicleBody.Text = "";
						lblVehicleArmor.Text = "";
						lblVehicleSensor.Text = "";
						lblVehicleFirewall.Text = "";
						lblVehicleSignal.Text = "";
						lblVehicleResponse.Text = "";
						lblVehicleSystem.Text = "";
						lblVehicleSlots.Text = objGear.CalculatedCapacity + " (" + objGear.CapacityRemaining.ToString() + " " + LanguageManager.Instance.GetString("String_Remaining") + ")";
						string strBook = _objOptions.LanguageBookShort(objGear.Source);
						string strPage = objGear.Page;
						lblVehicleSource.Text = strBook + " " + strPage;
						tipTooltip.SetToolTip(lblVehicleSource, _objOptions.LanguageBookLong(objGear.Source) + " " + LanguageManager.Instance.GetString("String_Page") + " " + objGear.Page);

						if ((_objCharacter.Metatype.EndsWith("A.I.") || _objCharacter.MetatypeCategory == "Technocritters" || _objCharacter.MetatypeCategory == "Protosapients") && objGear.GetType() == typeof(Commlink))
						{
							chkVehicleHomeNode.Visible = true;
							chkVehicleHomeNode.Checked = objGear.HomeNode;
						}
					}
				}
			}
			else if (treVehicles.SelectedNode.Level > 5)
			{
				Vehicle objSelectedVehicle = new Vehicle(_objCharacter);
				Gear objGear = _objFunctions.FindVehicleGear(treVehicles.SelectedNode.Tag.ToString(), _objCharacter.Vehicles, out objSelectedVehicle);

				if (objGear.MaxRating > 0)
				{
					_blnSkipRefresh = true;
					lblVehicleRatingLabel.Text = LanguageManager.Instance.GetString("Label_Rating");
					nudVehicleRating.Minimum = 1;
					nudVehicleRating.Maximum = objGear.MaxRating;
					nudVehicleRating.Value = objGear.Rating;
					nudVehicleRating.Enabled = true;
					_blnSkipRefresh = false;
				}
				else
				{
					_blnSkipRefresh = true;
					lblVehicleRatingLabel.Text = LanguageManager.Instance.GetString("Label_Rating");
					nudVehicleRating.Minimum = 0;
					nudVehicleRating.Maximum = 0;
					nudVehicleRating.Enabled = false;
					_blnSkipRefresh = false;
				}

				lblVehicleName.Text = objGear.DisplayNameShort;
				lblVehicleCategory.Text = objGear.DisplayCategory;
				lblVehicleAvail.Text = objGear.TotalAvail(true);
				lblVehicleCost.Text = String.Format("{0:###,###,##0¥}", objGear.TotalCost);
				lblVehicleHandling.Text = "";
				lblVehicleAccel.Text = "";
				lblVehicleSpeed.Text = "";
				lblVehicleDevice.Text = "";
				lblVehiclePilot.Text = "";
				lblVehicleBody.Text = "";
				lblVehicleArmor.Text = "";
				lblVehicleSensor.Text = "";
				lblVehicleFirewall.Text = "";
				lblVehicleSignal.Text = "";
				lblVehicleResponse.Text = "";
				lblVehicleSystem.Text = "";
				lblVehicleSlots.Text = objGear.CalculatedCapacity + " (" + objGear.CapacityRemaining.ToString() + " " + LanguageManager.Instance.GetString("String_Remaining") + ")";
				string strBook = _objOptions.LanguageBookShort(objGear.Source);
				string strPage = objGear.Page;
				lblVehicleSource.Text = strBook + " " + strPage;
				_blnSkipRefresh = true;
				chkVehicleBlackMarketDiscount.Checked = objGear.DiscountCost;
				_blnSkipRefresh = false;
				tipTooltip.SetToolTip(lblVehicleSource, _objOptions.LanguageBookLong(objGear.Source) + " " + LanguageManager.Instance.GetString("String_Page") + " " + objGear.Page);

				if ((_objCharacter.Metatype.EndsWith("A.I.") || _objCharacter.MetatypeCategory == "Technocritters" || _objCharacter.MetatypeCategory == "Protosapients") && objGear.GetType() == typeof(Commlink))
				{
					chkVehicleHomeNode.Visible = true;
					chkVehicleHomeNode.Checked = objGear.HomeNode;
				}
			}
		}

		/// <summary>
		/// Add or remove the Adapsin Cyberware Grade categories.
		/// </summary>
		public void PopulateCyberwareGradeList(bool blnBioware = false, bool blnIgnoreSecondHand = false)
		{
			// Load the Cyberware information.
			GradeList objGradeList;
			if (blnBioware)
				objGradeList = GlobalOptions.BiowareGrades;
			else
				objGradeList = GlobalOptions.CyberwareGrades;
			List<ListItem> lstCyberwareGrades = new List<ListItem>();

			foreach (Grade objWareGrade in objGradeList)
			{
				bool blnAddItem = true;

				ListItem objItem = new ListItem();
				objItem.Value = objWareGrade.Name;
				objItem.Name = objWareGrade.DisplayName;

				if (blnIgnoreSecondHand && objWareGrade.SecondHand)
					blnAddItem = false;
				if (!_objCharacter.AdapsinEnabled && objWareGrade.Adapsin)
					blnAddItem = false;

				if (blnAddItem)
					lstCyberwareGrades.Add(objItem);
			}
			//cboCyberwareGrade.DataSource = null;
			cboCyberwareGrade.ValueMember = "Value";
			cboCyberwareGrade.DisplayMember = "Name";
			cboCyberwareGrade.DataSource = lstCyberwareGrades;
		}

		/// <summary>
		/// Check the character and determine if it has broken any of the rules.
		/// </summary>
		public bool ValidateCharacter()
		{
			bool blnValid = true;
			string strMessage = LanguageManager.Instance.GetString("Message_InvalidBeginning");

			// Number of items over the specified Availability the character is allowed to have (typically from the Restricted Gear Quality).
			int intRestrictedAllowed = _objImprovementManager.ValueOf(Improvement.ImprovementType.RestrictedItemCount);
			int intRestrictedCount = 0;
			string strAvailItems = "";

			// Check if the character has gone over the Build Point total.
			int intBuildPoints = CalculateBP();
			if (intBuildPoints < 0 && !_blnFreestyle)
			{
				blnValid = false;
				if (_objCharacter.BuildMethod == CharacterBuildMethod.BP)
					strMessage += "\n\t" + LanguageManager.Instance.GetString("Message_InvalidPointExcess").Replace("{0}", (intBuildPoints * -1).ToString() + " " + LanguageManager.Instance.GetString("String_BP"));
				else
					strMessage += "\n\t" + LanguageManager.Instance.GetString("Message_InvalidPointExcess").Replace("{0}", (intBuildPoints * -1).ToString() + " " + LanguageManager.Instance.GetString("String_Karma"));
			}

			// Check if the character has gone over the Nuyen limit.
			int intNuyen = CalculateNuyen();
			if (intNuyen < 0)
			{
				blnValid = false;
				strMessage += "\n\t" + LanguageManager.Instance.GetString("Message_InvalidNuyenExcess").Replace("{0}", String.Format("{0:###,###,##0¥}", (intNuyen * -1)));
			}

			// Check if the character's Essence is above 0.
			decimal decEss = _objCharacter.Essence;
			if (decEss < 0.01m && _objCharacter.ESS.MetatypeMaximum > 0)
			{
				blnValid = false;
				strMessage += "\n\t" + LanguageManager.Instance.GetString("Message_InvalidEssenceExcess").Replace("{0}", ((decEss - 0.01m) * -1).ToString());
			}

			// If the character has Magician enabled, make sure a Tradition has been selected.
			if (_objCharacter.MagicianEnabled && _objCharacter.MagicTradition == "")
			{
				blnValid = false;
				strMessage += "\n\t" + LanguageManager.Instance.GetString("Message_InvalidNoTradition");
			}

			// If the character has RES enabled, make sure a Stream has been selected.
			if (_objCharacter.RESEnabled && _objCharacter.TechnomancerStream == "")
			{
				blnValid = false;
				strMessage += "\n\t" + LanguageManager.Instance.GetString("Message_InvalidNoStream");
			}

			// Check the character's equipment and make sure nothing goes over their set Maximum Availability.
			// Gear Availability.
			foreach (Gear objGear in _objCharacter.Gear)
			{
				if (GetAvailInt(objGear.TotalAvail(true)) > _objCharacter.MaximumAvailability)
				{
					intRestrictedCount++;
					strAvailItems += "\n\t\t" + objGear.DisplayNameShort;
				}
				foreach (Gear objChild in objGear.Children)
				{
					if (!objChild.TotalAvail().StartsWith("+"))
					{
						if (GetAvailInt(objChild.TotalAvail(true)) > _objCharacter.MaximumAvailability)
						{
							intRestrictedCount++;
							strAvailItems += "\n\t\t" + objChild.DisplayNameShort;
						}
						foreach (Gear objSubChild in objChild.Children)
						{
							if (!objSubChild.TotalAvail().StartsWith("+"))
							{
								if (GetAvailInt(objSubChild.TotalAvail(true)) > _objCharacter.MaximumAvailability)
								{
									intRestrictedCount++;
									strAvailItems += "\n\t\t" + objSubChild.DisplayNameShort;
								}
							}
						}
					}
				}
			}
			
			// Cyberware Availability.
			foreach (Cyberware objCyberware in _objCharacter.Cyberware)
			{
				if (GetAvailInt(objCyberware.TotalAvail) > _objCharacter.MaximumAvailability)
				{
					intRestrictedCount++;
					strAvailItems += "\n\t\t" + objCyberware.DisplayNameShort;
				}
				foreach (Cyberware objPlugin in objCyberware.Children)
				{
					if (!objPlugin.TotalAvail.StartsWith("+"))
					{
						if (GetAvailInt(objPlugin.TotalAvail) > _objCharacter.MaximumAvailability)
						{
							intRestrictedCount++;
							strAvailItems += "\n\t\t" + objPlugin.DisplayNameShort;
						}
					}

					foreach (Gear objGear in objPlugin.Gear)
					{
						if (!objGear.TotalAvail().StartsWith("+"))
						{
							if (GetAvailInt(objGear.TotalAvail(true)) > _objCharacter.MaximumAvailability)
							{
								intRestrictedCount++;
								strAvailItems += "\n\t\t" + objGear.DisplayNameShort;
							}
						}
						foreach (Gear objChild in objGear.Children)
						{
							if (!objChild.TotalAvail().StartsWith("+"))
							{
								if (GetAvailInt(objChild.TotalAvail(true)) > _objCharacter.MaximumAvailability)
								{
									intRestrictedCount++;
									strAvailItems += "\n\t\t" + objChild.DisplayNameShort;
								}
							}
							foreach (Gear objSubChild in objChild.Children)
							{
								if (!objSubChild.TotalAvail().StartsWith("+"))
								{
									if (GetAvailInt(objSubChild.TotalAvail(true)) > _objCharacter.MaximumAvailability)
									{
										intRestrictedCount++;
										strAvailItems += "\n\t\t" + objSubChild.DisplayNameShort;
									}
								}
							}
						}
					}
				}

				foreach (Gear objGear in objCyberware.Gear)
				{
					if (!objGear.TotalAvail().StartsWith("+"))
					{
						if (GetAvailInt(objGear.TotalAvail(true)) > _objCharacter.MaximumAvailability)
						{
							intRestrictedCount++;
							strAvailItems += "\n\t\t" + objGear.DisplayNameShort;
						}
					}
					foreach (Gear objChild in objGear.Children)
					{
						if (!objChild.TotalAvail().StartsWith("+"))
						{
							if (GetAvailInt(objChild.TotalAvail(true)) > _objCharacter.MaximumAvailability)
							{
								intRestrictedCount++;
								strAvailItems += "\n\t\t" + objChild.DisplayNameShort;
							}
						}
						foreach (Gear objSubChild in objChild.Children)
						{
							if (!objSubChild.TotalAvail().StartsWith("+"))
							{
								if (GetAvailInt(objSubChild.TotalAvail(true)) > _objCharacter.MaximumAvailability)
								{
									intRestrictedCount++;
									strAvailItems += "\n\t\t" + objSubChild.DisplayNameShort;
								}
							}
						}
					}
				}
			}

			// Armor Availability.
			foreach (Armor objArmor in _objCharacter.Armor)
			{
				if (GetAvailInt(objArmor.TotalAvail) > _objCharacter.MaximumAvailability)
				{
					intRestrictedCount++;
					strAvailItems += "\n\t\t" + objArmor.DisplayNameShort;
				}
				foreach (ArmorMod objMod in objArmor.ArmorMods)
				{
					if (!objMod.TotalAvail.StartsWith("+"))
					{
						if (GetAvailInt(objMod.TotalAvail) > _objCharacter.MaximumAvailability)
						{
							intRestrictedCount++;
							strAvailItems += "\n\t\t" + objMod.DisplayNameShort;
						}
					}
				}
				foreach (Gear objGear in objArmor.Gear)
				{
					if (!objGear.TotalAvail().StartsWith("+"))
					{
						if (GetAvailInt(objGear.TotalAvail(true)) > _objCharacter.MaximumAvailability)
						{
							intRestrictedCount++;
							strAvailItems += "\n\t\t" + objGear.DisplayNameShort;
						}
					}
					foreach (Gear objChild in objGear.Children)
					{
						if (!objChild.TotalAvail().StartsWith("+"))
						{
							if (GetAvailInt(objChild.TotalAvail(true)) > _objCharacter.MaximumAvailability)
							{
								intRestrictedCount++;
								strAvailItems += "\n\t\t" + objChild.DisplayNameShort;
							}
						}
						foreach (Gear objSubChild in objChild.Children)
						{
							if (!objSubChild.TotalAvail().StartsWith("+"))
							{
								if (GetAvailInt(objSubChild.TotalAvail(true)) > _objCharacter.MaximumAvailability)
								{
									intRestrictedCount++;
									strAvailItems += "\n\t\t" + objSubChild.DisplayNameShort;
								}
							}
						}
					}
				}
			}

			// Weapon Availability.
			foreach (Weapon objWeapon in _objCharacter.Weapons)
			{
				if (GetAvailInt(objWeapon.TotalAvail) > _objCharacter.MaximumAvailability)
				{
					intRestrictedCount++;
					strAvailItems += "\n\t\t" + objWeapon.DisplayNameShort;
				}
				foreach (WeaponMod objMod in objWeapon.WeaponMods)
				{
					if (!objMod.TotalAvail.StartsWith("+"))
					{
						if (GetAvailInt(objMod.TotalAvail) > _objCharacter.MaximumAvailability && !objMod.IncludedInWeapon)
						{
							intRestrictedCount++;
							strAvailItems += "\n\t\t" + objMod.DisplayNameShort;
						}
					}
				}
				foreach (WeaponAccessory objAccessory in objWeapon.WeaponAccessories)
				{
					if (!objAccessory.TotalAvail.StartsWith("+"))
					{
						if (GetAvailInt(objAccessory.TotalAvail) > _objCharacter.MaximumAvailability && !objAccessory.IncludedInWeapon)
						{
							intRestrictedCount++;
							strAvailItems += "\n\t\t" + objAccessory.DisplayNameShort;
						}
					}

					foreach (Gear objGear in objAccessory.Gear)
					{
						if (!objGear.TotalAvail().StartsWith("+"))
						{
							if (GetAvailInt(objGear.TotalAvail(true)) > _objCharacter.MaximumAvailability)
							{
								intRestrictedCount++;
								strAvailItems += "\n\t\t" + objGear.DisplayNameShort;
							}
						}
						foreach (Gear objChild in objGear.Children)
						{
							if (!objChild.TotalAvail().StartsWith("+"))
							{
								if (GetAvailInt(objChild.TotalAvail(true)) > _objCharacter.MaximumAvailability)
								{
									intRestrictedCount++;
									strAvailItems += "\n\t\t" + objChild.DisplayNameShort;
								}
							}
							foreach (Gear objSubChild in objChild.Children)
							{
								if (!objSubChild.TotalAvail().StartsWith("+"))
								{
									if (GetAvailInt(objSubChild.TotalAvail(true)) > _objCharacter.MaximumAvailability)
									{
										intRestrictedCount++;
										strAvailItems += "\n\t\t" + objSubChild.DisplayNameShort;
									}
								}
							}
						}
					}
				}
			}

			// Vehicle Availability.
			foreach (Vehicle objVehicle in _objCharacter.Vehicles)
			{
				if (GetAvailInt(objVehicle.CalculatedAvail) > _objCharacter.MaximumAvailability)
				{
					intRestrictedCount++;
					strAvailItems += "\n\t\t" + objVehicle.DisplayNameShort;
				}
				foreach (VehicleMod objVehicleMod in objVehicle.Mods)
				{
					if (!objVehicleMod.TotalAvail.StartsWith("+"))
					{
						if (GetAvailInt(objVehicleMod.TotalAvail) > _objCharacter.MaximumAvailability && !objVehicleMod.IncludedInVehicle)
						{
							intRestrictedCount++;
							strAvailItems += "\n\t\t" + objVehicleMod.DisplayNameShort;
						}
					}
					foreach (Weapon objWeapon in objVehicleMod.Weapons)
					{
						if (GetAvailInt(objWeapon.TotalAvail) > _objCharacter.MaximumAvailability)
						{
							intRestrictedCount++;
							strAvailItems += "\n\t\t" + objWeapon.DisplayNameShort;
						}
						foreach (WeaponMod objMod in objWeapon.WeaponMods)
						{
							if (!objMod.TotalAvail.StartsWith("+"))
							{
								if (GetAvailInt(objMod.TotalAvail) > _objCharacter.MaximumAvailability && !objMod.IncludedInWeapon)
								{
									intRestrictedCount++;
									strAvailItems += "\n\t\t" + objMod.DisplayNameShort;
								}
							}
						}
						foreach (WeaponAccessory objAccessory in objWeapon.WeaponAccessories)
						{
							if (!objAccessory.TotalAvail.StartsWith("+"))
							{
								if (GetAvailInt(objAccessory.TotalAvail) > _objCharacter.MaximumAvailability && !objAccessory.IncludedInWeapon)
								{
									intRestrictedCount++;
									strAvailItems += "\n\t\t" + objAccessory.DisplayNameShort;
								}
							}
						}
					}
				}
				foreach (Gear objGear in objVehicle.Gear)
				{
					if (!objGear.TotalAvail().StartsWith("+"))
					{
						if (GetAvailInt(objGear.TotalAvail(true)) > _objCharacter.MaximumAvailability)
						{
							intRestrictedCount++;
							strAvailItems += "\n\t\t" + objGear.DisplayNameShort;
						}
					}
					foreach (Gear objChild in objGear.Children)
					{
						if (!objChild.TotalAvail().StartsWith("+"))
						{
							if (GetAvailInt(objChild.TotalAvail(true)) > _objCharacter.MaximumAvailability)
							{
								intRestrictedCount++;
								strAvailItems += "\n\t\t" + objChild.DisplayNameShort;
							}
						}
						foreach (Gear objSubChild in objChild.Children)
						{
							if (!objSubChild.TotalAvail().StartsWith("+"))
							{
								if (GetAvailInt(objSubChild.TotalAvail(true)) > _objCharacter.MaximumAvailability)
								{
									intRestrictedCount++;
									strAvailItems += "\n\t\t" + objSubChild.DisplayNameShort;
								}
							}
						}
					}
				}
			}

			// Make sure the character is not carrying more items over the allowed Avail than they are allowed.
			if (intRestrictedCount > intRestrictedAllowed)
			{
				blnValid = false;
				strMessage += "\n\t" + LanguageManager.Instance.GetString("Message_InvalidAvail").Replace("{0}", (intRestrictedCount - intRestrictedAllowed).ToString()).Replace("{1}", _objCharacter.MaximumAvailability.ToString());
				strMessage += strAvailItems;
			}

			// Check item Capacities if the option is enabled.
			if (_objOptions.EnforceCapacity)
			{
				bool blnOverCapacity = false;
				int intCapacityOver = 0;
				// Armor Capacity.
				foreach (Armor objArmor in _objCharacter.Armor)
				{
					if (objArmor.CapacityRemaining < 0)
					{
						blnOverCapacity = true;
						intCapacityOver++;
					}
				}

				// Weapon Capacity.
				foreach (Weapon objWeapon in _objCharacter.Weapons)
				{
					if (objWeapon.SlotsRemaining < 0)
					{
						blnOverCapacity = true;
						intCapacityOver++;
					}
					// Check Underbarrel Weapons.
					if (objWeapon.UnderbarrelWeapons.Count > 0)
					{
						foreach (Weapon objUnderbarrelWeapon in objWeapon.UnderbarrelWeapons)
						{
							if (objUnderbarrelWeapon.SlotsRemaining < 0)
							{
								blnOverCapacity = true;
								intCapacityOver++;
							}
						}
					}
				}

				// Gear Capacity.
				foreach (Gear objGear in _objCharacter.Gear)
				{
					if (objGear.CapacityRemaining < 0)
					{
						blnOverCapacity = true;
						intCapacityOver++;
					}
					// Child Gear.
					foreach (Gear objChild in objGear.Children)
					{
						if (objChild.CapacityRemaining < 0)
						{
							blnOverCapacity = true;
							intCapacityOver++;
						}
					}
				}

				// Cyberware Capacity.
				foreach (Cyberware objCyberware in _objCharacter.Cyberware)
				{
					if (objCyberware.CapacityRemaining < 0)
					{
						blnOverCapacity = true;
						intCapacityOver++;
					}
					// Check plugins.
					foreach (Cyberware objChild in objCyberware.Children)
					{
						if (objChild.CapacityRemaining < 0)
						{
							blnOverCapacity = true;
							intCapacityOver++;
						}
					}
				}

				// Vehicle Capacity.
				foreach (Vehicle objVehicle in _objCharacter.Vehicles)
				{
					if (objVehicle.Slots - objVehicle.SlotsUsed < 0)
					{
						blnOverCapacity = true;
						intCapacityOver++;
					}
					// Check Vehicle Weapons.
					foreach (Weapon objWeapon in objVehicle.Weapons)
					{
						if (objWeapon.SlotsRemaining < 0)
						{
							blnOverCapacity = true;
							intCapacityOver++;
						}
						// Check Underbarrel Weapons.
						if (objWeapon.UnderbarrelWeapons.Count > 0)
						{
							foreach (Weapon objUnderbarrelWeapon in objWeapon.UnderbarrelWeapons)
							{
								if (objUnderbarrelWeapon.SlotsRemaining < 0)
								{
									blnOverCapacity = true;
									intCapacityOver++;
								}
							}
						}
					}
					// Check Vehicle Gear.
					foreach (Gear objGear in objVehicle.Gear)
					{
						if (objGear.CapacityRemaining < 0)
						{
							blnOverCapacity = true;
							intCapacityOver++;
						}
						// Check Child Gear.
						foreach (Gear objChild in objGear.Children)
						{
							if (objChild.CapacityRemaining < 0)
							{
								blnOverCapacity = true;
								intCapacityOver++;
							}
						}
					}
					// Check Vehicle Mods.
					foreach (VehicleMod objMod in objVehicle.Mods)
					{
						// Check Weapons.
						foreach (Weapon objWeapon in objMod.Weapons)
						{
							if (objWeapon.SlotsRemaining < 0)
							{
								blnOverCapacity = true;
								intCapacityOver++;
							}
							// Check Underbarrel Weapons.
							if (objWeapon.UnderbarrelWeapons.Count > 0)
							{
								foreach (Weapon objUnderbarrelWeapon in objWeapon.UnderbarrelWeapons)
								{
									if (objUnderbarrelWeapon.SlotsRemaining < 0)
									{
										blnOverCapacity = true;
										intCapacityOver++;
									}
								}
							}
						}
					}
				}

				if (blnOverCapacity)
				{
					blnValid = false;
					strMessage += "\n\t" + LanguageManager.Instance.GetString("Message_CapacityReachedValidate").Replace("{0}", intCapacityOver.ToString());
				}
			}

			if (!_objCharacter.IgnoreRules)
			{
				if (!blnValid)
					MessageBox.Show(strMessage, LanguageManager.Instance.GetString("MessageTitle_Invalid"), MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			else
				blnValid = true;

			if (blnValid)
			{
				if (_objOptions.CreateBackupOnCareer && chkCharacterCreated.Checked)
				{
					// Create a pre-Career Mode backup of the character.
					// Make sure the backup directory exists.
					if (!Directory.Exists(Path.Combine(Application.StartupPath, "saves", "backup")))
						Directory.CreateDirectory(Path.Combine(Application.StartupPath, "saves", "backup"));

					string strFileName = _objCharacter.FileName;
					string[] strParts = strFileName.Split(Path.DirectorySeparatorChar);
					string strNewName = strParts[strParts.Length - 1].Replace(".chum", " (" + LanguageManager.Instance.GetString("Title_CreateMode") + ").chum");
					if (strNewName == string.Empty)
					{
						strNewName = _objCharacter.Alias;
						if (strNewName == string.Empty)
							strNewName = _objCharacter.Name;
						if (strNewName == string.Empty)
							strNewName = Guid.NewGuid().ToString().Substring(0, 13).Replace("-", string.Empty);
						strNewName += " (" + LanguageManager.Instance.GetString("Title_CreateMode") + ").chum";
					}
					
					strNewName = Path.Combine(Application.StartupPath, "saves", "backup", strNewName);

					_objCharacter.FileName = strNewName;
					_objCharacter.Save();
					_objCharacter.FileName = strFileName;
				}

				// See if the character has any Karma remaining.
				if (_objCharacter.BuildMethod == CharacterBuildMethod.Karma)
				{
					if (intBuildPoints > _objOptions.KarmaCarryover)
					{
						if (MessageBox.Show(LanguageManager.Instance.GetString("Message_ExtraKarma").Replace("{0}", intBuildPoints.ToString()).Replace("{1}", _objOptions.KarmaCarryover.ToString()), LanguageManager.Instance.GetString("MessageTitle_ExtraKarma"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No)
							blnValid = false;
						else
							_objCharacter.Karma = _objOptions.KarmaCarryover;
					}
				}

				// Determine the highest Lifestyle the character has.
				Lifestyle objLifestyle = new Lifestyle(_objCharacter);
				foreach (Lifestyle objCharacterLifestyle in _objCharacter.Lifestyles)
				{
					if (objCharacterLifestyle.Multiplier > objLifestyle.Multiplier)
						objLifestyle = objCharacterLifestyle;
				}

				// If the character does not have any Lifestyles, give them the Street Lifestyle.
				if (_objCharacter.Lifestyles.Count == 0)
				{
					Lifestyle objStreet = new Lifestyle(_objCharacter);
					XmlDocument objXmlDocument = XmlManager.Instance.Load("lifestyles.xml");
					XmlNode objXmlLifestyle = objXmlDocument.SelectSingleNode("/chummer/lifestyles/lifestyle[name = \"Street\"]");
					TreeNode objNode = new TreeNode();

					objStreet.Create(objXmlLifestyle, objNode);
					treLifestyles.Nodes[0].Nodes.Add(objNode);
					treLifestyles.Nodes[0].Expand();

					_objCharacter.Lifestyles.Add(objStreet);
					
					objLifestyle = objStreet;
				}

				int intNuyenDice = objLifestyle.Dice;

				// Characters get a +1 bonus to the roll for every 100 Nueyn they have left over, up to a maximum of 3X the number of dice rolled for the Lifestyle.
				int intNuyenBonus = Convert.ToInt32(Math.Floor((Convert.ToDouble(intNuyen, GlobalOptions.Instance.CultureInfo)) / 100.0));
				if (intNuyenBonus > intNuyenDice * 3)
					intNuyenBonus = intNuyenDice * 3;

				frmLifestyleNuyen frmStartingNuyen = new frmLifestyleNuyen();
				frmStartingNuyen.Dice = intNuyenDice;
				frmStartingNuyen.Multiplier = objLifestyle.Multiplier;
				frmStartingNuyen.Extra = intNuyenBonus;

				frmStartingNuyen.ShowDialog(this);

				// Assign the starting Nuyen amount.
				int intStartingNuyen = frmStartingNuyen.StartingNuyen;
				if (intStartingNuyen < 0)
					intStartingNuyen = 0;
				_objCharacter.Nuyen = intStartingNuyen;

				// Break any Skill Groups if any of their associated Skills have a Rating while that does not match the Group's.
				foreach (SkillGroup objGroup in _objCharacter.SkillGroups)
				{
					foreach (Skill objSkill in _objCharacter.Skills)
					{
						if (objSkill.Rating != objGroup.Rating && objSkill.SkillGroup == objGroup.Name)
						{
							objGroup.Broken = true;
							break;
						}
					}
				}
			}

			return blnValid;
		}

		/// <summary>
		/// Verify that the user wants to save this character as Created.
		/// </summary>
		public bool ConfirmSaveCreatedCharacter()
		{
			if (MessageBox.Show(LanguageManager.Instance.GetString("Message_ConfirmCreate"), LanguageManager.Instance.GetString("MessageTitle_ConfirmCreate"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
				return false;

			if (!ValidateCharacter())
				return false;

			// The user has confirmed that the character should be Create.
			_objCharacter.Created = true;

			return true;
		}

		/// <summary>
		/// Create Cyberware from a Cyberware Suite.
		/// </summary>
		/// <param name="objXmlNode">XmlNode for the Cyberware to add.</param>
		/// <param name="objGrade">CyberwareGrade to add the item as.</param>
		/// <param name="intRating">Rating of the Cyberware.</param>
		/// <param name="blnAddToCharacter">Whether or not the Cyberware should be added directly to the character.</param>
		/// <param name="objParent">Parent Cyberware if the item is not being added directly to the character.</param>
		private TreeNode CreateSuiteCyberware(XmlNode objXmlItem, XmlNode objXmlNode, Grade objGrade, int intRating, bool blnAddToCharacter, Improvement.ImprovementSource objSource, string strType, Cyberware objParent = null)
		{
			// Create the Cyberware object.
			List<Weapon> objWeapons = new List<Weapon>();
			List<TreeNode> objWeaponNodes = new List<TreeNode>();
			TreeNode objNode = new TreeNode();
			Cyberware objCyberware = new Cyberware(_objCharacter);
			string strForced = "";

			if (objXmlItem["name"].Attributes["select"] != null)
				strForced = objXmlItem["name"].Attributes["select"].InnerText;

			objCyberware.Create(objXmlNode, _objCharacter, objGrade, objSource, intRating, objNode, objWeapons, objWeaponNodes, true, true, strForced);
			objCyberware.Suite = true;

			foreach (Weapon objWeapon in objWeapons)
				_objCharacter.Weapons.Add(objWeapon);

			foreach (TreeNode objWeaponNode in objWeaponNodes)
			{
				treWeapons.Nodes[0].Nodes.Add(objWeaponNode);
				treWeapons.Nodes[0].Expand();
			}

			if (blnAddToCharacter)
				_objCharacter.Cyberware.Add(objCyberware);
			else
				objParent.Children.Add(objCyberware);

			foreach (XmlNode objXmlChild in objXmlItem.SelectNodes(strType + "s/" + strType))
			{
				XmlDocument objXmlDocument = XmlManager.Instance.Load(strType + ".xml");
				XmlNode objXmlChildCyberware = objXmlDocument.SelectSingleNode("/chummer/" + strType + "s/" + strType + "[name = \"" + objXmlChild["name"].InnerText + "\"]");
				TreeNode objChildNode = new TreeNode();
				int intChildRating = 0;

				if (objXmlChild["rating"] != null)
					intChildRating = Convert.ToInt32(objXmlChild["rating"].InnerText);

				objChildNode = CreateSuiteCyberware(objXmlChild, objXmlChildCyberware, objGrade, intChildRating, false, objSource, strType, objCyberware);
				objNode.Nodes.Add(objChildNode);
				objNode.Expand();
			}

			return objNode;
		}

		/// <summary>
		/// Add a PACKS Kit to the character.
		/// </summary>
		public void AddPACKSKit()
		{
			frmSelectPACKSKit frmPickPACKSKit = new frmSelectPACKSKit(_objCharacter);
			frmPickPACKSKit.ShowDialog(this);

			bool blnCreateChildren = true;

			// If the form was canceled, don't do anything.
			if (frmPickPACKSKit.DialogResult == DialogResult.Cancel)
				return;

			XmlDocument objXmlDocument = XmlManager.Instance.Load("packs.xml");

			// Do not create child items for Gear if the chosen Kit is in the Custom category since these items will contain the exact plugins desired.
			if (frmPickPACKSKit.SelectedCategory == "Custom")
				blnCreateChildren = false;

			XmlNode objXmlKit = objXmlDocument.SelectSingleNode("/chummer/packs/pack[name = \"" + frmPickPACKSKit.SelectedKit + "\" and category = \"" + frmPickPACKSKit.SelectedCategory + "\"]");
			// Update Qualities.
			if (objXmlKit["qualities"] != null)
			{
				XmlDocument objXmlQualityDocument = XmlManager.Instance.Load("qualities.xml");

				// Positive Qualities.
				foreach (XmlNode objXmlQuality in objXmlKit.SelectNodes("qualities/positive/quality"))
				{
					XmlNode objXmlQualityNode = objXmlQualityDocument.SelectSingleNode("/chummer/qualities/quality[name = \"" + objXmlQuality.InnerText + "\"]");

					TreeNode objNode = new TreeNode();
					List<Weapon> objWeapons = new List<Weapon>();
					List<TreeNode> objWeaponNodes = new List<TreeNode>();
					Quality objQuality = new Quality(_objCharacter);
					string strForceValue = "";

					if (objXmlQuality.Attributes["select"] != null)
						strForceValue = objXmlQuality.Attributes["select"].InnerText;

					objQuality.Create(objXmlQualityNode, _objCharacter, QualitySource.Selected, objNode, objWeapons, objWeaponNodes, strForceValue);
					_objCharacter.Qualities.Add(objQuality);

					treQualities.Nodes[0].Nodes.Add(objNode);
					treQualities.Nodes[0].Expand();

					// Add any created Weapons to the character.
					foreach (Weapon objWeapon in objWeapons)
						_objCharacter.Weapons.Add(objWeapon);

					// Create the Weapon Node if one exists.
					foreach (TreeNode objWeaponNode in objWeaponNodes)
					{
						objWeaponNode.ContextMenuStrip = cmsWeapon;
						treWeapons.Nodes[0].Nodes.Add(objWeaponNode);
						treWeapons.Nodes[0].Expand();
					}
				}

				// Negative Qualities.
				foreach (XmlNode objXmlQuality in objXmlKit.SelectNodes("qualities/negative/quality"))
				{
					XmlNode objXmlQualityNode = objXmlQualityDocument.SelectSingleNode("/chummer/qualities/quality[name = \"" + objXmlQuality.InnerText + "\"]");

					TreeNode objNode = new TreeNode();
					List<Weapon> objWeapons = new List<Weapon>();
					List<TreeNode> objWeaponNodes = new List<TreeNode>();
					Quality objQuality = new Quality(_objCharacter);
					string strForceValue = "";

					if (objXmlQuality.Attributes["select"] != null)
						strForceValue = objXmlQuality.Attributes["select"].InnerText;

					objQuality.Create(objXmlQualityNode, _objCharacter, QualitySource.Selected, objNode, objWeapons, objWeaponNodes, strForceValue);
					_objCharacter.Qualities.Add(objQuality);

					treQualities.Nodes[1].Nodes.Add(objNode);
					treQualities.Nodes[1].Expand();

					// Add any created Weapons to the character.
					foreach (Weapon objWeapon in objWeapons)
						_objCharacter.Weapons.Add(objWeapon);

					// Create the Weapon Node if one exists.
					foreach (TreeNode objWeaponNode in objWeaponNodes)
					{
						objWeaponNode.ContextMenuStrip = cmsWeapon;
						treWeapons.Nodes[0].Nodes.Add(objWeaponNode);
						treWeapons.Nodes[0].Expand();
					}
				}
			}

			// Update Attributes.
			if (objXmlKit["attributes"] != null)
			{
				// Reset all Attributes back to 1 so we don't go over any BP limits.
				nudBOD.Value = nudBOD.Minimum;
				nudAGI.Value = nudAGI.Minimum;
				nudREA.Value = nudREA.Minimum;
				nudSTR.Value = nudSTR.Minimum;
				nudCHA.Value = nudCHA.Minimum;
				nudINT.Value = nudINT.Minimum;
				nudLOG.Value = nudLOG.Minimum;
				nudWIL.Value = nudWIL.Minimum;
				nudEDG.Value = nudEDG.Minimum;
				nudMAG.Value = nudMAG.Minimum;
				nudRES.Value = nudRES.Minimum;
				foreach (XmlNode objXmlAttribute in objXmlKit["attributes"])
				{
					// The Attribute is calculated as given value - (6 - Metatype Maximum) so that each Metatype has the values from the file adjusted correctly.
					switch (objXmlAttribute.Name)
					{
						case "bod":
							nudBOD.Value = Convert.ToInt32(objXmlAttribute.InnerText) - (6 - _objCharacter.BOD.MetatypeMaximum);
							break;
						case "agi":
							nudAGI.Value = Convert.ToInt32(objXmlAttribute.InnerText) - (6 - _objCharacter.AGI.MetatypeMaximum);
							break;
						case "rea":
							nudREA.Value = Convert.ToInt32(objXmlAttribute.InnerText) - (6 - _objCharacter.REA.MetatypeMaximum);
							break;
						case "str":
							nudSTR.Value = Convert.ToInt32(objXmlAttribute.InnerText) - (6 - _objCharacter.STR.MetatypeMaximum);
							break;
						case "cha":
							nudCHA.Value = Convert.ToInt32(objXmlAttribute.InnerText) - (6 - _objCharacter.CHA.MetatypeMaximum);
							break;
						case "int":
							nudINT.Value = Convert.ToInt32(objXmlAttribute.InnerText) - (6 - _objCharacter.INT.MetatypeMaximum);
							break;
						case "log":
							nudLOG.Value = Convert.ToInt32(objXmlAttribute.InnerText) - (6 - _objCharacter.LOG.MetatypeMaximum);
							break;
						case "wil":
							nudWIL.Value = Convert.ToInt32(objXmlAttribute.InnerText) - (6 - _objCharacter.WIL.MetatypeMaximum);
							break;
						case "mag":
							nudMAG.Value = Convert.ToInt32(objXmlAttribute.InnerText) - (6 - _objCharacter.MAG.MetatypeMaximum);
							break;
						case "res":
							nudRES.Value = Convert.ToInt32(objXmlAttribute.InnerText) - (6 - _objCharacter.RES.MetatypeMaximum);
							break;
						default:
							nudEDG.Value = Convert.ToInt32(objXmlAttribute.InnerText) - (6 - _objCharacter.EDG.MetatypeMaximum);
							break;
					}
				}
			}

			// Update Skills.
			if (objXmlKit["skills"] != null)
			{
				// Active Skills.
				foreach (XmlNode objXmlSkill in objXmlKit.SelectNodes("skills/skill"))
				{
					if (objXmlSkill["name"].InnerText.Contains("Exotic"))
					{
						int i = panActiveSkills.Controls.Count;
						Skill objSkill = new Skill(_objCharacter);

						SkillControl objSkillControl = new SkillControl();
						objSkillControl.SkillObject = objSkill;
						objSkillControl.Width = 510;

						// Attach an EventHandler for the RatingChanged and SpecializationChanged Events.
						objSkillControl.RatingChanged += objActiveSkill_RatingChanged;
						objSkillControl.SpecializationChanged += objSkill_SpecializationChanged;
						objSkillControl.SkillName = objXmlSkill["name"].InnerText;

						switch (objXmlSkill["name"].InnerText)
						{
							case "Exotic Ranged Weapon":
							case "Exotic Melee Weapon":
								objSkill.Attribute = "AGI";
								objSkillControl.SkillCategory = "Combat Active";
								objSkill.Default = true;
								break;
							default:
								objSkill.Attribute = "REA";
								objSkillControl.SkillCategory = "Vehicle Active";
								objSkill.Default = false;
								break;
						}
						objSkill.ExoticSkill = true;
						_objCharacter.Skills.Add(objSkill);

						objSkillControl.SkillRatingMaximum = 6;

						// Make sure it's not going above the maximum number.
						if (Convert.ToInt32(objXmlSkill["rating"].InnerText) > objSkillControl.SkillRatingMaximum)
							objSkillControl.SkillRating = objSkillControl.SkillRatingMaximum;
						else
							objSkillControl.SkillRating = Convert.ToInt32(objXmlSkill["rating"].InnerText);

						if (objXmlSkill["spec"] != null)
							objSkillControl.SkillSpec = objXmlSkill["spec"].InnerText;
						else
							objSkillControl.SkillSpec = "";

						// Set the SkillControl's Location since scrolling the Panel causes it to actually change the child Controls' Locations.
						objSkillControl.Location = new Point(0, objSkillControl.Height * i + panActiveSkills.AutoScrollPosition.Y);
						panActiveSkills.Controls.Add(objSkillControl);
					}
					else
					{
						// Find the correct Skill Control.
						SkillControl objSkillControl = new SkillControl();
						foreach (SkillControl objControl in panActiveSkills.Controls)
						{
							if (objControl.SkillName == objXmlSkill["name"].InnerText)
							{
								objSkillControl = objControl;
								break;
							}
						}

						// Make sure it's not going above the maximum number.
						if (Convert.ToInt32(objXmlSkill["rating"].InnerText) > objSkillControl.SkillRatingMaximum)
							objSkillControl.SkillRating = objSkillControl.SkillRatingMaximum;
						else
							objSkillControl.SkillRating = Convert.ToInt32(objXmlSkill["rating"].InnerText);

						if (objXmlSkill["spec"] != null)
							objSkillControl.SkillSpec = objXmlSkill["spec"].InnerText;
						else
							objSkillControl.SkillSpec = "";
					}
				}

				// Skill Groups.
				foreach (XmlNode objXmlGroup in objXmlKit.SelectNodes("skills/skillgroup"))
				{
					// Find the correct SkillGroupControl.
					SkillGroupControl objSkillGroupControl = new SkillGroupControl(_objCharacter.Options);
					foreach (SkillGroupControl objControl in panSkillGroups.Controls)
					{
						if (objControl.GroupName == objXmlGroup["name"].InnerText)
						{
							objSkillGroupControl = objControl;
							break;
						}
					}
					
					// Make sure it's not going above the maximum number.
					if (Convert.ToInt32(objXmlGroup["rating"].InnerText) > objSkillGroupControl.GroupRatingMaximum)
						objSkillGroupControl.GroupRating = objSkillGroupControl.GroupRatingMaximum;
					else
						objSkillGroupControl.GroupRating = Convert.ToInt32(objXmlGroup["rating"].InnerText);
				}
			}

			// Update Knowledge Skills.
			if (objXmlKit["knowledgeskills"] != null)
			{
				foreach (XmlNode objXmlSkill in objXmlKit.SelectNodes("knowledgeskills/skill"))
				{
					int i = panKnowledgeSkills.Controls.Count;
					Skill objSkill = new Skill(_objCharacter);
					objSkill.Name = objXmlSkill["name"].InnerText;

					SkillControl objSkillControl = new SkillControl();
					objSkillControl.SkillObject = objSkill;

					// Attach an EventHandler for the RatingChanged and SpecializationChanged Events.
					objSkillControl.RatingChanged += objKnowledgeSkill_RatingChanged;
					objSkillControl.SpecializationChanged += objSkill_SpecializationChanged;
					objSkillControl.DeleteSkill += objKnowledgeSkill_DeleteSkill;

					objSkillControl.KnowledgeSkill = true;
					objSkillControl.AllowDelete = true;
					objSkillControl.SkillRatingMaximum = 6;
					// Set the SkillControl's Location since scrolling the Panel causes it to actually change the child Controls' Locations.
					objSkillControl.Location = new Point(0, objSkillControl.Height * i + panKnowledgeSkills.AutoScrollPosition.Y);
					panKnowledgeSkills.Controls.Add(objSkillControl);

					objSkillControl.SkillName = objXmlSkill["name"].InnerText;

					// Make sure it's not going above the maximum number.
					if (Convert.ToInt32(objXmlSkill["rating"].InnerText) > objSkillControl.SkillRatingMaximum)
						objSkillControl.SkillRating = objSkillControl.SkillRatingMaximum;
					else
						objSkillControl.SkillRating = Convert.ToInt32(objXmlSkill["rating"].InnerText);

					if (objXmlSkill["spec"] != null)
						objSkillControl.SkillSpec = objXmlSkill["spec"].InnerText;
					else
						objSkillControl.SkillSpec = "";

					if (objXmlSkill["category"] != null)
						objSkillControl.SkillCategory = objXmlSkill["category"].InnerText;

					_objCharacter.Skills.Add(objSkill);
				}
			}

			// Select a Martial Art.
			if (objXmlKit["selectmartialart"] != null)
			{
				string strForcedValue = "";
				int intRating = 1;
				if (objXmlKit["selectmartialart"].Attributes["select"] != null)
					strForcedValue = objXmlKit["selectmartialart"].Attributes["select"].InnerText;
				if (objXmlKit["selectmartialart"].Attributes["rating"] != null)
					intRating = Convert.ToInt32(objXmlKit["selectmartialart"].Attributes["rating"].InnerText);

				frmSelectMartialArt frmPickMartialArt = new frmSelectMartialArt(_objCharacter);
				frmPickMartialArt.ForcedValue = strForcedValue;
				frmPickMartialArt.ShowDialog(this);

				if (frmPickMartialArt.DialogResult != DialogResult.Cancel)
				{
					// Open the Martial Arts XML file and locate the selected piece.
					XmlDocument objXmlMartialArtDocument = XmlManager.Instance.Load("martialarts.xml");

					XmlNode objXmlArt = objXmlMartialArtDocument.SelectSingleNode("/chummer/martialarts/martialart[name = \"" + frmPickMartialArt.SelectedMartialArt + "\"]");

					TreeNode objNode = new TreeNode();
					MartialArt objMartialArt = new MartialArt(_objCharacter);
					objMartialArt.Create(objXmlArt, objNode, _objCharacter);
					objMartialArt.Rating = intRating;
					_objCharacter.MartialArts.Add(objMartialArt);

					objNode.ContextMenuStrip = cmsMartialArts;

					treMartialArts.Nodes[0].Nodes.Add(objNode);
					treMartialArts.Nodes[0].Expand();

					treMartialArts.SelectedNode = objNode;
				}
			}

			// Update Martial Arts.
			if (objXmlKit["martialarts"] != null)
			{
				// Open the Martial Arts XML file and locate the selected art.
				XmlDocument objXmlMartialArtDocument = XmlManager.Instance.Load("martialarts.xml");

				foreach (XmlNode objXmlArt in objXmlKit.SelectNodes("martialarts/martialart"))
				{
					TreeNode objNode = new TreeNode();
					MartialArt objArt = new MartialArt(_objCharacter);
					XmlNode objXmlArtNode = objXmlMartialArtDocument.SelectSingleNode("/chummer/martialarts/martialart[name = \"" + objXmlArt["name"].InnerText + "\"]");
					objArt.Create(objXmlArtNode, objNode, _objCharacter);
					objArt.Rating = Convert.ToInt32(objXmlArt["rating"].InnerText);
					_objCharacter.MartialArts.Add(objArt);

					// Check for Advantages.
					foreach (XmlNode objXmlAdvantage in objXmlArt.SelectNodes("advantages/advantage"))
					{
						TreeNode objChildNode = new TreeNode();
						MartialArtAdvantage objAdvantage = new MartialArtAdvantage(_objCharacter);
						XmlNode objXmlAdvantageNode = objXmlMartialArtDocument.SelectSingleNode("/chummer/martialarts/martialart[name = \"" + objXmlArt["name"].InnerText + "\"]/advantages/advantage[. = \"" + objXmlAdvantage.InnerText + "\"]");
						objAdvantage.Create(objXmlAdvantageNode, _objCharacter, objChildNode);
						objArt.Advantages.Add(objAdvantage);

						objNode.Nodes.Add(objChildNode);
						objNode.Expand();
					}

					treMartialArts.Nodes[0].Nodes.Add(objNode);
					treMartialArts.Nodes[0].Expand();
				}

				// Maneuvers.
				foreach (XmlNode objXmlManeuver in objXmlKit.SelectNodes("martialarts/maneuver"))
				{
					TreeNode objNode = new TreeNode();
					MartialArtManeuver objManeuver = new MartialArtManeuver(_objCharacter);
					XmlNode objXmlManeuverNode = objXmlMartialArtDocument.SelectSingleNode("/chummer/maneuvers/maneuver[name = \"" + objXmlManeuver.InnerText + "\"]");
					objManeuver.Create(objXmlManeuverNode, objNode);
					objNode.ContextMenuStrip = cmsMartialArtManeuver;
					_objCharacter.MartialArtManeuvers.Add(objManeuver);

					treMartialArts.Nodes[1].Nodes.Add(objNode);
					treMartialArts.Nodes[1].Expand();
				}
			}

			// Update Adept Powers.
			if (objXmlKit["powers"] != null)
			{	
				// Open the Powers XML file and locate the selected power.
				XmlDocument objXmlPowerDocument = XmlManager.Instance.Load("powers.xml");

				foreach (XmlNode objXmlPower in objXmlKit.SelectNodes("powers/power"))
				{
					XmlNode objXmlPowerNode = objXmlPowerDocument.SelectSingleNode("/chummer/powers/power[name = \"" + objXmlPower["name"].InnerText + "\"]");

					int i = panPowers.Controls.Count;

					Power objPower = new Power(_objCharacter);
					_objCharacter.Powers.Add(objPower);

					PowerControl objPowerControl = new PowerControl();
					objPowerControl.PowerObject = objPower;

					// Attach an EventHandler for the PowerRatingChanged Event.
					objPowerControl.PowerRatingChanged += objPower_PowerRatingChanged;
					objPowerControl.DeletePower += objPower_DeletePower;

					objPowerControl.PowerName = objXmlPowerNode["name"].InnerText;
					objPowerControl.PointsPerLevel = Convert.ToDecimal(objXmlPowerNode["points"].InnerText, GlobalOptions.Instance.CultureInfo);
					if (objXmlPowerNode["levels"].InnerText == "no")
					{
						objPowerControl.LevelEnabled = false;
					}
					else
					{
						objPowerControl.LevelEnabled = true;
						if (objXmlPowerNode["levels"].InnerText != "yes")
							objPower.MaxLevels = Convert.ToInt32(objXmlPowerNode["levels"].InnerText);
					}

					objPower.Source = objXmlPowerNode["source"].InnerText;
					objPower.Page = objXmlPowerNode["page"].InnerText;
					if (objXmlPowerNode["doublecost"] != null)
						objPower.DoubleCost = false;

					if (objXmlPowerNode.InnerXml.Contains("bonus"))
					{
						objPower.Bonus = objXmlPowerNode["bonus"];

						if (objXmlPower["name"].Attributes["select"] != null)
							_objImprovementManager.ForcedValue = objXmlPower["name"].Attributes["select"].InnerText;

						_objImprovementManager.CreateImprovements(Improvement.ImprovementSource.Power, objPower.InternalId, objPower.Bonus, false, Convert.ToInt32(objPower.Rating), objPower.DisplayNameShort);
						objPowerControl.Extra = _objImprovementManager.SelectedValue;
					}

					objPowerControl.Top = i * objPowerControl.Height;
					panPowers.Controls.Add(objPowerControl);

					// Set the Rating of the Power if applicable.
					if (objXmlPower["rating"] != null)
						objPowerControl.PowerLevel = Convert.ToInt32(objXmlPower["rating"].InnerText);
				}
			}

			// Update Complex Forms.
			if (objXmlKit["programs"] != null)
			{
				// Open the Programs XML file and locate the selected program.
				XmlDocument objXmlProgramDocument = XmlManager.Instance.Load("programs.xml");

				foreach (XmlNode objXmlProgram in objXmlKit.SelectNodes("programs/program"))
				{
					XmlNode objXmlProgramNode = objXmlProgramDocument.SelectSingleNode("/chummer/programs/program[name = \"" + objXmlProgram["name"].InnerText + "\"]");

					string strForceValue = "";
					if (objXmlProgram.Attributes["select"] != null)
						strForceValue = objXmlProgram.Attributes["select"].InnerText;

					TreeNode objNode = new TreeNode();
					TechProgram objProgram = new TechProgram(_objCharacter);
					objProgram.Create(objXmlProgramNode, _objCharacter, objNode, strForceValue);

					// Set the Rating of the Power if applicable.
					if (objXmlProgram["rating"] != null)
						objProgram.Rating = Convert.ToInt32(objXmlProgram["rating"].InnerText);

					// Check for Program Options.
					foreach (XmlNode objXmlOption in objXmlProgram.SelectNodes("options/option"))
					{
						string strChildForceValue = "";
						if (objXmlOption.Attributes["select"] != null)
							strChildForceValue = objXmlOption.Attributes["select"].InnerText;

						XmlNode objXmlOptionNode = objXmlProgramDocument.SelectSingleNode("/chummer/options/option[name = \"" + objXmlOption["name"].InnerText + "\"]");

						TreeNode objChildNode = new TreeNode();
						TechProgramOption objOption = new TechProgramOption(_objCharacter);
						objOption.Create(objXmlOptionNode, _objCharacter, objChildNode, strChildForceValue);
						objChildNode.ContextMenuStrip = cmsComplexFormPlugin;

						// Set the Rating of the Option if applicable.
						if (objXmlOption["rating"] != null)
							objOption.Rating = Convert.ToInt32(objXmlOption["rating"].InnerText);

						objProgram.Options.Add(objOption);
						objNode.Nodes.Add(objChildNode);
						objNode.Expand();
					}

					switch (objProgram.Category)
					{
						case "Advanced":
							treComplexForms.Nodes[0].Nodes.Add(objNode);
							treComplexForms.Nodes[0].Expand();
							break;
						case "ARE Programs":
							treComplexForms.Nodes[1].Nodes.Add(objNode);
							treComplexForms.Nodes[1].Expand();
							break;
						case "Autosoft":
							treComplexForms.Nodes[2].Nodes.Add(objNode);
							treComplexForms.Nodes[2].Expand();
							break;
						case "Common Use":
							treComplexForms.Nodes[3].Nodes.Add(objNode);
							treComplexForms.Nodes[3].Expand();
							break;
						case "Hacking":
							treComplexForms.Nodes[4].Nodes.Add(objNode);
							treComplexForms.Nodes[4].Expand();
							break;
						case "Malware":
							treComplexForms.Nodes[5].Nodes.Add(objNode);
							treComplexForms.Nodes[5].Expand();
							break;
						case "Sensor Software":
							treComplexForms.Nodes[6].Nodes.Add(objNode);
							treComplexForms.Nodes[6].Expand();
							break;
						case "Skillsofts":
							treComplexForms.Nodes[7].Nodes.Add(objNode);
							treComplexForms.Nodes[7].Expand();
							break;
						case "Tactical AR Software":
							treComplexForms.Nodes[8].Nodes.Add(objNode);
							treComplexForms.Nodes[8].Expand();
							break;
					}

					_objCharacter.TechPrograms.Add(objProgram);

					_objFunctions.SortTree(treComplexForms);
				}
			}

			// Update Spells.
			if (objXmlKit["spells"] != null)
			{
				XmlDocument objXmlSpellDocument = XmlManager.Instance.Load("spells.xml");

				foreach (XmlNode objXmlSpell in objXmlKit.SelectNodes("spells/spell"))
				{
					// Make sure the Spell has not already been added to the character.
					bool blnFound = false;
					foreach (TreeNode nodSpell in treSpells.Nodes[0].Nodes)
					{
						if (nodSpell.Text == objXmlSpell.InnerText)
						{
							blnFound = true;
							break;
						}
					}

					// The Spell is not in the list, so add it.
					if (!blnFound)
					{
						string strForceValue = "";
						if (objXmlSpell.Attributes["select"] != null)
							strForceValue = objXmlSpell.Attributes["select"].InnerText;

						XmlNode objXmlSpellNode = objXmlSpellDocument.SelectSingleNode("/chummer/spells/spell[name = \"" + objXmlSpell.InnerText + "\"]");

						Spell objSpell = new Spell(_objCharacter);
						TreeNode objNode = new TreeNode();
						objSpell.Create(objXmlSpellNode, _objCharacter, objNode, strForceValue);
						objNode.ContextMenuStrip = cmsSpell;
						_objCharacter.Spells.Add(objSpell);

						switch (objSpell.Category)
						{
							case "Combat":
								treSpells.Nodes[0].Nodes.Add(objNode);
								treSpells.Nodes[0].Expand();
								break;
							case "Detection":
								treSpells.Nodes[1].Nodes.Add(objNode);
								treSpells.Nodes[1].Expand();
								break;
							case "Health":
								treSpells.Nodes[2].Nodes.Add(objNode);
								treSpells.Nodes[2].Expand();
								break;
							case "Illusion":
								treSpells.Nodes[3].Nodes.Add(objNode);
								treSpells.Nodes[3].Expand();
								break;
							case "Manipulation":
								treSpells.Nodes[4].Nodes.Add(objNode);
								treSpells.Nodes[4].Expand();
								break;
							case "Geomancy Ritual":
								treSpells.Nodes[5].Nodes.Add(objNode);
								treSpells.Nodes[5].Expand();
								break;
						}

						_objFunctions.SortTree(treSpells);
					}
				}
			}

			// Update Spirits.
			if (objXmlKit["spirits"] != null)
			{
				foreach (XmlNode objXmlSpirit in objXmlKit.SelectNodes("spirits/spirit"))
				{
					int i = panSpirits.Controls.Count;

					Spirit objSpirit = new Spirit(_objCharacter);
					_objCharacter.Spirits.Add(objSpirit);

					SpiritControl objSpiritControl = new SpiritControl();
					objSpiritControl.SpiritObject = objSpirit;
					objSpiritControl.EntityType = SpiritType.Spirit;

					// Attach an EventHandler for the ServicesOwedChanged Event.
					objSpiritControl.ServicesOwedChanged += objSpirit_ServicesOwedChanged;
					objSpiritControl.ForceChanged += objSpirit_ForceChanged;
					objSpiritControl.BoundChanged += objSpirit_BoundChanged;
					objSpiritControl.DeleteSpirit += objSpirit_DeleteSpirit;

					objSpiritControl.Name = objXmlSpirit["name"].InnerText;
					objSpiritControl.Force = Convert.ToInt32(objXmlSpirit["force"].InnerText);
					objSpiritControl.ServicesOwed = Convert.ToInt32(objXmlSpirit["services"].InnerText);

					objSpiritControl.Top = i * objSpiritControl.Height;
					panSpirits.Controls.Add(objSpiritControl);
				}
			}

			// Update Lifestyles.
			if (objXmlKit["lifestyles"] != null)
			{
				XmlDocument objXmlLifestyleDocument = XmlManager.Instance.Load("lifestyles.xml");

				foreach (XmlNode objXmlLifestyle in objXmlKit.SelectNodes("lifestyles/lifestyle"))
				{
					string strName = objXmlLifestyle["name"].InnerText;
					int intMonths = Convert.ToInt32(objXmlLifestyle["months"].InnerText);

					// Create the Lifestyle.
					TreeNode objNode = new TreeNode();
					Lifestyle objLifestyle = new Lifestyle(_objCharacter);

					XmlNode objXmlLifestyleNode = objXmlLifestyleDocument.SelectSingleNode("/chummer/lifestyles/lifestyle[name = \"" + strName + "\"]");
					if (objXmlLifestyleNode != null)
					{
						// This is a standard Lifestyle, so just use the Create method.
						objLifestyle.Create(objXmlLifestyleNode, objNode);
						objLifestyle.Months = intMonths;
					}
					else
					{
						// This is an Advanced Lifestyle, so build it manually.
						objLifestyle.Name = strName;
						objLifestyle.Months = intMonths;
						objLifestyle.Cost = Convert.ToInt32(objXmlLifestyle["cost"].InnerText);
						objLifestyle.Dice = Convert.ToInt32(objXmlLifestyle["dice"].InnerText);
						objLifestyle.Multiplier = Convert.ToInt32(objXmlLifestyle["multiplier"].InnerText);
						objLifestyle.Comforts = objXmlLifestyle["comforts"].InnerText;
						objLifestyle.Entertainment = objXmlLifestyle["entertainment"].InnerText;
						objLifestyle.Necessities = objXmlLifestyle["necessities"].InnerText;
						objLifestyle.Neighborhood = objXmlLifestyle["neighborhood"].InnerText;
						objLifestyle.Security = objXmlLifestyle["security"].InnerText;
						objLifestyle.Source = "RC";
						objLifestyle.Page = "154";

						foreach (XmlNode objXmlQuality in objXmlLifestyle.SelectNodes("qualities/quality"))
							objLifestyle.Qualities.Add(objXmlQuality.InnerText);

						objNode.Text = strName;
					}

					// Add the Lifestyle to the character and Lifestyle Tree.
					if (objLifestyle.Comforts != "")
						objNode.ContextMenuStrip = cmsAdvancedLifestyle;
					else
						objNode.ContextMenuStrip = cmsLifestyleNotes;
					_objCharacter.Lifestyles.Add(objLifestyle);
					treLifestyles.Nodes[0].Nodes.Add(objNode);
					treLifestyles.Nodes[0].Expand();
				}
			}

			// Update NuyenBP.
			if (objXmlKit["nuyenbp"] != null)
			{
				int intAmount = Convert.ToInt32(objXmlKit["nuyenbp"].InnerText);
				if (_objCharacter.BuildMethod == CharacterBuildMethod.Karma)
					intAmount *= 2;

				// Make sure we don't go over the field's maximum which would throw an Exception.
				if (nudNuyen.Value + intAmount > nudNuyen.Maximum)
					nudNuyen.Value = nudNuyen.Maximum;
				else
					nudNuyen.Value += intAmount;
			}

			// Update Armor.
			if (objXmlKit["armors"] != null)
			{
				XmlDocument objXmlArmorDocument = XmlManager.Instance.Load("armor.xml");

				foreach (XmlNode objXmlArmor in objXmlKit.SelectNodes("armors/armor"))
				{
					XmlNode objXmlArmorNode = objXmlArmorDocument.SelectSingleNode("/chummer/armors/armor[name = \"" + objXmlArmor["name"].InnerText + "\"]");

					Armor objArmor = new Armor(_objCharacter);
					TreeNode objNode = new TreeNode();
					objArmor.Create(objXmlArmorNode, objNode, cmsArmorMod, false, blnCreateChildren);
					_objCharacter.Armor.Add(objArmor);

					// Look for Armor Mods.
					if (objXmlArmor["mods"] != null)
					{
						foreach (XmlNode objXmlMod in objXmlArmor.SelectNodes("mods/mod"))
						{
							List<Weapon> lstWeapons = new List<Weapon>();
							List<TreeNode> lstWeaponNodes = new List<TreeNode>();
							XmlNode objXmlModNode = objXmlArmorDocument.SelectSingleNode("/chummer/mods/mod[name = \"" + objXmlMod["name"].InnerText + "\"]");
							ArmorMod objMod = new ArmorMod(_objCharacter);
							TreeNode objModNode = new TreeNode();
							int intRating = 0;
							if (objXmlMod["rating"] != null)
								intRating = Convert.ToInt32(objXmlMod["rating"].InnerText);
							objMod.Create(objXmlModNode, objModNode, intRating, lstWeapons, lstWeaponNodes);
							objModNode.ContextMenuStrip = cmsArmorMod;
							objMod.Parent = objArmor;

							objArmor.ArmorMods.Add(objMod);

							objNode.Nodes.Add(objModNode);
							objNode.Expand();

							// Add any Weapons created by the Mod.
							foreach (Weapon objWeapon in lstWeapons)
								_objCharacter.Weapons.Add(objWeapon);

							foreach (TreeNode objWeaponNode in lstWeaponNodes)
							{
								objWeaponNode.ContextMenuStrip = cmsWeapon;
								treWeapons.Nodes[0].Nodes.Add(objWeaponNode);
								treWeapons.Nodes[0].Expand();
							}
						}
					}

					XmlDocument objXmlGearDocument = XmlManager.Instance.Load("gear.xml");
					foreach (XmlNode objXmlGear in objXmlArmor.SelectNodes("gears/gear"))
						AddPACKSGear(objXmlGearDocument, objXmlGear, objNode, objArmor, cmsArmorGear, blnCreateChildren);

					objNode.ContextMenuStrip = cmsArmor;
					treArmor.Nodes[0].Nodes.Add(objNode);
					treArmor.Nodes[0].Expand();
				}
			}

			// Update Weapons.
			if (objXmlKit["weapons"] != null)
			{
				XmlDocument objXmlWeaponDocument = XmlManager.Instance.Load("weapons.xml");

				pgbProgress.Visible = true;
				pgbProgress.Value = 0;
				pgbProgress.Maximum = objXmlKit.SelectNodes("weapons/weapon").Count;
				int i = 0;
				foreach (XmlNode objXmlWeapon in objXmlKit.SelectNodes("weapons/weapon"))
				{
					i++;
					pgbProgress.Value = i;
					Application.DoEvents();

					XmlNode objXmlWeaponNode = objXmlWeaponDocument.SelectSingleNode("/chummer/weapons/weapon[name = \"" + objXmlWeapon["name"].InnerText + "\"]");

					Weapon objWeapon = new Weapon(_objCharacter);
					TreeNode objNode = new TreeNode();
					objWeapon.Create(objXmlWeaponNode, _objCharacter, objNode, cmsWeapon, cmsWeaponAccessory, cmsWeaponMod, blnCreateChildren);
					_objCharacter.Weapons.Add(objWeapon);

					// Look for Weapon Accessories.
					if (objXmlWeapon["accessories"] != null)
					{
						foreach (XmlNode objXmlAccessory in objXmlWeapon.SelectNodes("accessories/accessory"))
						{
							XmlNode objXmlAccessoryNode = objXmlWeaponDocument.SelectSingleNode("/chummer/accessories/accessory[name = \"" + objXmlAccessory["name"].InnerText + "\"]");
							WeaponAccessory objMod = new WeaponAccessory(_objCharacter);
							TreeNode objModNode = new TreeNode();
							string strMount = "";
							if (objXmlAccessory["mount"] != null)
								strMount = objXmlAccessory["mount"].InnerText;
							objMod.Create(objXmlAccessoryNode, objModNode, strMount);
							objModNode.ContextMenuStrip = cmsWeaponAccessory;
							objMod.Parent = objWeapon;

							objWeapon.WeaponAccessories.Add(objMod);

							XmlDocument objXmlGearDocument = XmlManager.Instance.Load("gear.xml");
							foreach (XmlNode objXmlGear in objXmlAccessory.SelectNodes("gears/gear"))
								AddPACKSGear(objXmlGearDocument, objXmlGear, objModNode, objMod, cmsWeaponAccessoryGear, blnCreateChildren);

							objNode.Nodes.Add(objModNode);
							objNode.Expand();
						}
					}

					// Look for Weapon Mods.
					if (objXmlWeapon["mods"] != null)
					{
						foreach (XmlNode objXmlMod in objXmlWeapon.SelectNodes("mods/mod"))
						{
							XmlNode objXmlModNode = objXmlWeaponDocument.SelectSingleNode("/chummer/mods/mod[name = \"" + objXmlMod["name"].InnerText + "\"]");
							WeaponMod objMod = new WeaponMod(_objCharacter);
							TreeNode objModNode = new TreeNode();
							objMod.Create(objXmlModNode, objModNode);
							objModNode.ContextMenuStrip = cmsWeaponMod;
							objMod.Parent = objWeapon;

							objWeapon.WeaponMods.Add(objMod);

							objNode.Nodes.Add(objModNode);
							objNode.Expand();
						}
					}

					// Look for an Underbarrel Weapon.
					if (objXmlWeapon["underbarrel"] != null)
					{
						XmlNode objXmlUnderbarrelNode = objXmlWeaponDocument.SelectSingleNode("/chummer/weapons/weapon[name = \"" + objXmlWeapon["underbarrel"].InnerText + "\"]");

						Weapon objUnderbarrelWeapon = new Weapon(_objCharacter);
						TreeNode objUnderbarrelNode = new TreeNode();
						objUnderbarrelWeapon.Create(objXmlUnderbarrelNode, _objCharacter, objUnderbarrelNode, cmsWeapon, cmsWeaponAccessory, cmsWeaponMod, blnCreateChildren);
						objWeapon.UnderbarrelWeapons.Add(objUnderbarrelWeapon);
						objNode.Nodes.Add(objUnderbarrelNode);
						objNode.Expand();
					}

					objNode.ContextMenuStrip = cmsWeapon;
					treWeapons.Nodes[0].Nodes.Add(objNode);
					treWeapons.Nodes[0].Expand();

					Application.DoEvents();
				}
			}

			// Update Cyberware.
			if (objXmlKit["cyberwares"] != null)
			{
				XmlDocument objXmlCyberwareDocument = XmlManager.Instance.Load("cyberware.xml");
				XmlDocument objXmlGearDocument = XmlManager.Instance.Load("gear.xml");

				pgbProgress.Visible = true;
				pgbProgress.Value = 0;
				pgbProgress.Maximum = objXmlKit.SelectNodes("cyberwares/cyberware").Count;
				int i = 0;
				foreach (XmlNode objXmlCyberware in objXmlKit.SelectNodes("cyberwares/cyberware"))
				{
					i++;
					pgbProgress.Value = i;
					Application.DoEvents();

					List<Weapon> objWeapons = new List<Weapon>();
					List<TreeNode> objWeaponNodes = new List<TreeNode>();
					TreeNode objNode = new TreeNode();
					Cyberware objCyberware = new Cyberware(_objCharacter);
					Grade objGrade = objCyberware.ConvertToCyberwareGrade(objXmlCyberware["grade"].InnerText, Improvement.ImprovementSource.Cyberware);

					int intRating = 0;
					if (objXmlCyberware["rating"] != null)
						intRating = Convert.ToInt32(objXmlCyberware["rating"].InnerText);

					XmlNode objXmlCyberwareNode = objXmlCyberwareDocument.SelectSingleNode("/chummer/cyberwares/cyberware[name = \"" + objXmlCyberware["name"].InnerText + "\"]");
					objCyberware.Create(objXmlCyberwareNode, _objCharacter, objGrade, Improvement.ImprovementSource.Cyberware, intRating, objNode, objWeapons, objWeaponNodes, true, blnCreateChildren);
					_objCharacter.Cyberware.Add(objCyberware);

					// Add any children.
					if (objXmlCyberware["cyberwares"] != null)
					{
						foreach (XmlNode objXmlChild in objXmlCyberware.SelectNodes("cyberwares/cyberware"))
						{
							TreeNode objChildNode = new TreeNode();
							Cyberware objChildCyberware = new Cyberware(_objCharacter);

							int intChildRating = 0;
							if (objXmlChild["rating"] != null)
								intChildRating = Convert.ToInt32(objXmlChild["rating"].InnerText);

							XmlNode objXmlChildNode = objXmlCyberwareDocument.SelectSingleNode("/chummer/cyberwares/cyberware[name = \"" + objXmlChild["name"].InnerText + "\"]");
							objChildCyberware.Create(objXmlChildNode, _objCharacter, objGrade, Improvement.ImprovementSource.Cyberware, intChildRating, objChildNode, objWeapons, objWeaponNodes, true, blnCreateChildren);
							objCyberware.Children.Add(objChildCyberware);
							objChildNode.ContextMenuStrip = cmsCyberware;

							foreach (XmlNode objXmlGear in objXmlChild.SelectNodes("gears/gear"))
								AddPACKSGear(objXmlGearDocument, objXmlGear, objChildNode, objChildCyberware, cmsCyberwareGear, blnCreateChildren);

							objNode.Nodes.Add(objChildNode);
							objNode.Expand();
						}
					}

					foreach (XmlNode objXmlGear in objXmlCyberware.SelectNodes("gears/gear"))
						AddPACKSGear(objXmlGearDocument, objXmlGear, objNode, objCyberware, cmsCyberwareGear, blnCreateChildren);

					objNode.ContextMenuStrip = cmsCyberware;
					treCyberware.Nodes[0].Nodes.Add(objNode);
					treCyberware.Nodes[0].Expand();

					// Add any Weapons created by the Gear.
					foreach (Weapon objWeapon in objWeapons)
						_objCharacter.Weapons.Add(objWeapon);

					foreach (TreeNode objWeaponNode in objWeaponNodes)
					{
						objWeaponNode.ContextMenuStrip = cmsWeapon;
						treWeapons.Nodes[0].Nodes.Add(objWeaponNode);
						treWeapons.Nodes[0].Expand();
					}

					Application.DoEvents();
				}

				_objFunctions.SortTree(treCyberware);
			}

			// Update Bioware.
			if (objXmlKit["biowares"] != null)
			{
				XmlDocument objXmlBiowareDocument = XmlManager.Instance.Load("bioware.xml");

				pgbProgress.Visible = true;
				pgbProgress.Value = 0;
				pgbProgress.Maximum = objXmlKit.SelectNodes("biowares/bioware").Count;
				int i = 0;

				foreach (XmlNode objXmlBioware in objXmlKit.SelectNodes("biowares/bioware"))
				{
					i++;
					pgbProgress.Value = i;
					Application.DoEvents();

					List<Weapon> objWeapons = new List<Weapon>();
					List<TreeNode> objWeaponNodes = new List<TreeNode>();
					TreeNode objNode = new TreeNode();
					Cyberware objCyberware = new Cyberware(_objCharacter);
					Grade objGrade = objCyberware.ConvertToCyberwareGrade(objXmlBioware["grade"].InnerText, Improvement.ImprovementSource.Bioware);

					int intRating = 0;
					if (objXmlBioware["rating"] != null)
						intRating = Convert.ToInt32(objXmlBioware["rating"].InnerText);

					XmlNode objXmlBiowareNode = objXmlBiowareDocument.SelectSingleNode("/chummer/biowares/bioware[name = \"" + objXmlBioware["name"].InnerText + "\"]");
					objCyberware.Create(objXmlBiowareNode, _objCharacter, objGrade, Improvement.ImprovementSource.Bioware, intRating, objNode, objWeapons, objWeaponNodes, true, blnCreateChildren);
					_objCharacter.Cyberware.Add(objCyberware);

					objNode.ContextMenuStrip = cmsBioware;
					treCyberware.Nodes[1].Nodes.Add(objNode);
					treCyberware.Nodes[1].Expand();

					// Add any Weapons created by the Gear.
					foreach (Weapon objWeapon in objWeapons)
						_objCharacter.Weapons.Add(objWeapon);

					foreach (TreeNode objWeaponNode in objWeaponNodes)
					{
						objWeaponNode.ContextMenuStrip = cmsWeapon;
						treWeapons.Nodes[0].Nodes.Add(objWeaponNode);
						treWeapons.Nodes[0].Expand();
					}
					
					Application.DoEvents();
				}

				_objFunctions.SortTree(treCyberware);
			}

			// Update Gear.
			if (objXmlKit["gears"] != null)
			{
				XmlDocument objXmlGearDocument = XmlManager.Instance.Load("gear.xml");

				pgbProgress.Visible = true;
				pgbProgress.Value = 0;
				pgbProgress.Maximum = objXmlKit.SelectNodes("gears/gear").Count;
				int i = 0;

				foreach (XmlNode objXmlGear in objXmlKit.SelectNodes("gears/gear"))
				{
					i++;
					pgbProgress.Value = i;
					Application.DoEvents();

					AddPACKSGear(objXmlGearDocument, objXmlGear, treGear.Nodes[0], _objCharacter, cmsGear, blnCreateChildren);

					Application.DoEvents();
				}
			}

			// Update Vehicles.
			if (objXmlKit["vehicles"] != null)
			{
				XmlDocument objXmlVehicleDocument = XmlManager.Instance.Load("vehicles.xml");

				pgbProgress.Visible = true;
				pgbProgress.Value = 0;
				pgbProgress.Maximum = objXmlKit.SelectNodes("vehicles/vehicle").Count;
				int i = 0;

				foreach (XmlNode objXmlVehicle in objXmlKit.SelectNodes("vehicles/vehicle"))
				{
					i++;
					pgbProgress.Value = i;
					Application.DoEvents();

					Gear objDefaultSensor = new Gear(_objCharacter);

					TreeNode objNode = new TreeNode();
					Vehicle objVehicle = new Vehicle(_objCharacter);

					XmlNode objXmlVehicleNode = objXmlVehicleDocument.SelectSingleNode("/chummer/vehicles/vehicle[name = \"" + objXmlVehicle["name"].InnerText + "\"]");
					objVehicle.Create(objXmlVehicleNode, objNode, cmsVehicle, cmsVehicleGear, cmsVehicleWeapon, cmsVehicleWeaponAccessory, cmsVehicleWeaponMod, blnCreateChildren);
					_objCharacter.Vehicles.Add(objVehicle);

					// Grab the default Sensor that comes with the Vehicle.
					foreach (Gear objSensorGear in objVehicle.Gear)
					{
						if (objSensorGear.Category == "Sensors" && objSensorGear.Cost == "0" && objSensorGear.Rating == 0)
						{
							objDefaultSensor = objSensorGear;
							break;
						}
					}

					// Add any Vehicle Mods.
					if (objXmlVehicle["mods"] != null)
					{
						foreach (XmlNode objXmlMod in objXmlVehicle.SelectNodes("mods/mod"))
						{
							TreeNode objModNode = new TreeNode();
							VehicleMod objMod = new VehicleMod(_objCharacter);

							int intRating = 0;
							if (objXmlMod["rating"] != null)
								intRating = Convert.ToInt32(objXmlMod["rating"].InnerText);

							XmlNode objXmlModNode = objXmlVehicleDocument.SelectSingleNode("/chummer/mods/mod[name = \"" + objXmlMod["name"].InnerText + "\"]");
							objMod.Create(objXmlModNode, objModNode, intRating);
							objVehicle.Mods.Add(objMod);

							objNode.Nodes.Add(objModNode);
							objNode.Expand();
						}
					}

					// Add any Vehicle Gear.
					if (objXmlVehicle["gears"] != null)
					{
						XmlDocument objXmlGearDocument = XmlManager.Instance.Load("gear.xml");

						foreach (XmlNode objXmlGear in objXmlVehicle.SelectNodes("gears/gear"))
						{
							List<Weapon> objWeapons = new List<Weapon>();
							List<TreeNode> objWeaponNodes = new List<TreeNode>();
							TreeNode objGearNode = new TreeNode();
							Gear objGear = new Gear(_objCharacter);
							int intQty = 1;

							int intRating = 0;
							if (objXmlGear["rating"] != null)
								intRating = Convert.ToInt32(objXmlGear["rating"].InnerText);
							string strForceValue = "";
							if (objXmlGear["name"].Attributes["select"] != null)
								strForceValue = objXmlGear["name"].Attributes["select"].InnerText;
							if (objXmlGear["qty"] != null)
								intQty = Convert.ToInt32(objXmlGear["qty"].InnerText);

							XmlNode objXmlGearNode = objXmlGearDocument.SelectSingleNode("/chummer/gears/gear[name = \"" + objXmlGear["name"].InnerText + "\"]");
							objGear.Create(objXmlGearNode, _objCharacter, objGearNode, intRating, objWeapons, objWeaponNodes, strForceValue, false, false, false, blnCreateChildren, false);
							objGear.Quantity = intQty;
							objGearNode.Text = objGear.DisplayName;
							objVehicle.Gear.Add(objGear);

							// Look for child components.
							if (objXmlGear["gears"] != null)
							{
								foreach (XmlNode objXmlChild in objXmlGear.SelectNodes("gears/gear"))
								{
									AddPACKSGear(objXmlGearDocument, objXmlChild, objGearNode, objGear, cmsVehicleGear, blnCreateChildren);
								}
							}

							objGearNode.Expand();
							objGearNode.ContextMenuStrip = cmsVehicleGear;
							objNode.Nodes.Add(objGearNode);
							objNode.Expand();

							// If this is a Sensor, it will replace the Vehicle's base sensor, so remove it.
							if (objGear.Category == "Sensors" && objGear.Cost == "0" && objGear.Rating == 0)
							{
								objVehicle.Gear.Remove(objDefaultSensor);
								foreach (TreeNode objSensorNode in objNode.Nodes)
								{
									if (objSensorNode.Tag.ToString() == objDefaultSensor.InternalId)
									{
										objSensorNode.Remove();
										break;
									}
								}
							}
						}
					}

					// Add any Vehicle Weapons.
					if (objXmlVehicle["weapons"] != null)
					{
						XmlDocument objXmlWeaponDocument = XmlManager.Instance.Load("weapons.xml");

						foreach (XmlNode objXmlWeapon in objXmlVehicle.SelectNodes("weapons/weapon"))
						{
							TreeNode objWeaponNode = new TreeNode();
							Weapon objWeapon = new Weapon(_objCharacter);

							XmlNode objXmlWeaponNode = objXmlWeaponDocument.SelectSingleNode("/chummer/weapons/weapon[name = \"" + objXmlWeapon["name"].InnerText + "\"]");
							objWeapon.Create(objXmlWeaponNode, _objCharacter, objWeaponNode, cmsVehicleWeapon, cmsVehicleWeaponAccessory, cmsVehicleWeaponMod, blnCreateChildren);
							objWeapon.VehicleMounted = true;
							
							// Find the first Weapon Mount in the Vehicle.
							foreach (VehicleMod objMod in objVehicle.Mods)
							{
								if (objMod.Name.StartsWith("Weapon Mount"))
								{
									objMod.Weapons.Add(objWeapon);
									foreach (TreeNode objModNode in objNode.Nodes)
									{
										if (objModNode.Tag.ToString() == objMod.InternalId)
										{
											objWeaponNode.ContextMenuStrip = cmsVehicleWeapon;
											objModNode.Nodes.Add(objWeaponNode);
											objModNode.Expand();
											break;
										}
									}
									break;
								}
							}

							// Look for Weapon Accessories.
							if (objXmlWeapon["accessories"] != null)
							{
								foreach (XmlNode objXmlAccessory in objXmlWeapon.SelectNodes("accessories/accessory"))
								{
									XmlNode objXmlAccessoryNode = objXmlWeaponDocument.SelectSingleNode("/chummer/accessories/accessory[name = \"" + objXmlAccessory["name"].InnerText + "\"]");
									WeaponAccessory objMod = new WeaponAccessory(_objCharacter);
									TreeNode objModNode = new TreeNode();
									string strMount = "";
									if (objXmlAccessory["mount"] != null)
										strMount = objXmlAccessory["mount"].InnerText;
									objMod.Create(objXmlAccessoryNode, objModNode, strMount);
									objModNode.ContextMenuStrip = cmsWeaponAccessory;
									objMod.Parent = objWeapon;

									objWeapon.WeaponAccessories.Add(objMod);

									objWeaponNode.Nodes.Add(objModNode);
									objWeaponNode.Expand();
								}
							}

							// Look for Weapon Mods.
							if (objXmlWeapon["mods"] != null)
							{
								foreach (XmlNode objXmlMod in objXmlWeapon.SelectNodes("mods/mod"))
								{
									XmlNode objXmlModNode = objXmlWeaponDocument.SelectSingleNode("/chummer/mods/mod[name = \"" + objXmlMod["name"].InnerText + "\"]");
									WeaponMod objMod = new WeaponMod(_objCharacter);
									TreeNode objModNode = new TreeNode();
									objMod.Create(objXmlModNode, objModNode);
									objModNode.ContextMenuStrip = cmsVehicleWeaponMod;
									objMod.Parent = objWeapon;

									objWeapon.WeaponMods.Add(objMod);

									objWeaponNode.Nodes.Add(objModNode);
									objWeaponNode.Expand();
								}
							}
						}
					}

					objNode.ContextMenuStrip = cmsVehicle;
					treVehicles.Nodes[0].Nodes.Add(objNode);
					treVehicles.Nodes[0].Expand();

					Application.DoEvents();
				}
			}

			pgbProgress.Visible = false;

			if (frmPickPACKSKit.AddAgain)
				AddPACKSKit();

			PopulateGearList();
			UpdateCharacterInfo();
		}

		/// <summary>
		/// Create a PACKS Kit from the character.
		/// </summary>
		public void CreatePACKSKit()
		{
			frmCreatePACKSKit frmBuildPACKSKit = new frmCreatePACKSKit(_objCharacter);
			frmBuildPACKSKit.ShowDialog(this);
		}

		/// <summary>
		/// Dummy method to trap the Options MRUChanged Event.
		/// </summary>
		public void PopulateMRU()
		{
		}

		/// <summary>
		/// Update the contents of the Initiation Grade list.
		/// </summary>
		public void UpdateInitiationGradeList()
		{
			lstInitiation.Items.Clear();
			foreach (InitiationGrade objGrade in _objCharacter.InitiationGrades)
				lstInitiation.Items.Add(objGrade.Text);
		}

		/// <summary>
		/// Change the character's Metatype.
		/// </summary>
		public void ChangeMetatype()
		{
			// Determine if the character has any chosen Qualities that depend on their current Metatype. If so, don't let the change happen.
			XmlDocument objXmlDocument = XmlManager.Instance.Load("qualities.xml");
			string strQualities = "";
			foreach (Quality objQuality in _objCharacter.Qualities)
			{
				if (objQuality.OriginSource != QualitySource.Metatype && objQuality.OriginSource != QualitySource.MetatypeRemovable)
				{
					XmlNode objXmlQuality = objXmlDocument.SelectSingleNode("/chummer/qualities/quality[name = \"" + objQuality.Name + "\"]");
					if (objXmlQuality.SelectNodes("required/oneof/metatype[. = \"" + _objCharacter.Metatype + "\"]").Count > 0 || objXmlQuality.SelectNodes("required/oneof/metavariant[. = \"" + _objCharacter.Metavariant + "\"]").Count > 0)
						strQualities += "\n\t" + objQuality.DisplayNameShort;
					if (objXmlQuality.SelectNodes("required/allof/metatype[. = \"" + _objCharacter.Metatype + "\"]").Count > 0 || objXmlQuality.SelectNodes("required/allof/metavariant[. = \"" + _objCharacter.Metavariant + "\"]").Count > 0)
						strQualities += "\n\t" + objQuality.DisplayNameShort;
				}
			}
			if (strQualities != "")
			{
				MessageBox.Show(LanguageManager.Instance.GetString("Message_CannotChangeMetatype") + strQualities, LanguageManager.Instance.GetString("MessageTitle_CannotChangeMetatype"), MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			int intEssenceLoss = 0;
			if (!_objOptions.ESSLossReducesMaximumOnly)
				intEssenceLoss = _objCharacter.EssencePenalty;

			// Determine the number of points that have been put into Attributes.
			int intBOD = _objCharacter.BOD.Value - _objCharacter.BOD.MetatypeMinimum;
			int intAGI = _objCharacter.AGI.Value - _objCharacter.AGI.MetatypeMinimum;
			int intREA = _objCharacter.REA.Value - _objCharacter.REA.MetatypeMinimum;
			int intSTR = _objCharacter.STR.Value - _objCharacter.STR.MetatypeMinimum;
			int intCHA = _objCharacter.CHA.Value - _objCharacter.CHA.MetatypeMinimum;
			int intINT = _objCharacter.INT.Value - _objCharacter.INT.MetatypeMinimum;
			int intLOG = _objCharacter.LOG.Value - _objCharacter.LOG.MetatypeMinimum;
			int intWIL = _objCharacter.WIL.Value - _objCharacter.WIL.MetatypeMinimum;
			int intEDG = _objCharacter.EDG.Value - _objCharacter.EDG.MetatypeMinimum;
			int intMAG = _objCharacter.MAG.Value - _objCharacter.MAG.MetatypeMinimum;
			int intRES = _objCharacter.RES.Value - _objCharacter.RES.MetatypeMinimum;

			// Build a list of the current Metatype's Improvements to remove if the Metatype changes.
			List<Improvement> lstImprovement = new List<Improvement>();
			foreach (Improvement objImprovement in _objCharacter.Improvements)
			{
				if (objImprovement.ImproveSource == Improvement.ImprovementSource.Metatype || objImprovement.ImproveSource == Improvement.ImprovementSource.Metavariant)
					lstImprovement.Add(objImprovement);
			}

			// Build a list of the current Metatype's Qualities to remove if the Metatype changes.
			List<Quality> lstRemoveQuality = new List<Quality>();
			foreach (Quality objQuality in _objCharacter.Qualities)
			{
				if (objQuality.OriginSource == QualitySource.Metatype || objQuality.OriginSource == QualitySource.MetatypeRemovable)
					lstRemoveQuality.Add(objQuality);
			}

			frmMetatype frmSelectMetatype = new frmMetatype(_objCharacter);
			frmSelectMetatype.ShowDialog(this);

			if (frmSelectMetatype.DialogResult == DialogResult.Cancel)
				return;

			// Remove any Improvements the character received from their Metatype.
			foreach (Improvement objImprovement in lstImprovement)
			{
				_objImprovementManager.RemoveImprovements(objImprovement.ImproveSource, objImprovement.SourceName);
				_objCharacter.Improvements.Remove(objImprovement);
			}

			// Remove any Qualities the character received from their Metatype, then remove the Quality.
			foreach (Quality objQuality in lstRemoveQuality)
			{
				_objImprovementManager.RemoveImprovements(Improvement.ImprovementSource.Quality, objQuality.InternalId);
				_objCharacter.Qualities.Remove(objQuality);
			}

			// Populate the Qualities list.
			treQualities.Nodes[0].Nodes.Clear();
			treQualities.Nodes[1].Nodes.Clear();
			foreach (Quality objQuality in _objCharacter.Qualities)
			{
				TreeNode objNode = new TreeNode();
				objNode.Text = objQuality.DisplayName;
				objNode.Tag = objQuality.InternalId;
				if (objQuality.OriginSource == QualitySource.Metatype || objQuality.OriginSource == QualitySource.MetatypeRemovable)
					objNode.ForeColor = SystemColors.GrayText;

				if (objQuality.Type == QualityType.Positive)
				{
					treQualities.Nodes[0].Nodes.Add(objNode);
					treQualities.Nodes[0].Expand();
				}
				else
				{
					treQualities.Nodes[1].Nodes.Add(objNode);
					treQualities.Nodes[1].Expand();
				}
			}

			_blnSkipUpdate = true;
			nudBOD.Maximum = _objCharacter.BOD.TotalMaximum;
			nudAGI.Maximum = _objCharacter.AGI.TotalMaximum;
			nudREA.Maximum = _objCharacter.REA.TotalMaximum;
			nudSTR.Maximum = _objCharacter.STR.TotalMaximum;
			nudCHA.Maximum = _objCharacter.CHA.TotalMaximum;
			nudINT.Maximum = _objCharacter.INT.TotalMaximum;
			nudLOG.Maximum = _objCharacter.LOG.TotalMaximum;
			nudWIL.Maximum = _objCharacter.WIL.TotalMaximum;
			nudEDG.Maximum = _objCharacter.EDG.TotalMaximum;
			nudMAG.Maximum = _objCharacter.MAG.TotalMaximum + intEssenceLoss;
			nudRES.Maximum = _objCharacter.RES.TotalMaximum + intEssenceLoss;

			nudBOD.Minimum = _objCharacter.BOD.MetatypeMinimum;
			nudAGI.Minimum = _objCharacter.AGI.MetatypeMinimum;
			nudREA.Minimum = _objCharacter.REA.MetatypeMinimum;
			nudSTR.Minimum = _objCharacter.STR.MetatypeMinimum;
			nudCHA.Minimum = _objCharacter.CHA.MetatypeMinimum;
			nudINT.Minimum = _objCharacter.INT.MetatypeMinimum;
			nudLOG.Minimum = _objCharacter.LOG.MetatypeMinimum;
			nudWIL.Minimum = _objCharacter.WIL.MetatypeMinimum;
			nudEDG.Minimum = _objCharacter.EDG.MetatypeMinimum;
			nudMAG.Minimum = _objCharacter.MAG.MetatypeMinimum;
			nudRES.Minimum = _objCharacter.RES.MetatypeMinimum;

			_objCharacter.BOD.Value = _objCharacter.BOD.MetatypeMinimum + intBOD;
			_objCharacter.AGI.Value = _objCharacter.AGI.MetatypeMinimum + intAGI;
			_objCharacter.REA.Value = _objCharacter.REA.MetatypeMinimum + intREA;
			_objCharacter.STR.Value = _objCharacter.STR.MetatypeMinimum + intSTR;
			_objCharacter.CHA.Value = _objCharacter.CHA.MetatypeMinimum + intCHA;
			_objCharacter.INT.Value = _objCharacter.INT.MetatypeMinimum + intINT;
			_objCharacter.LOG.Value = _objCharacter.LOG.MetatypeMinimum + intLOG;
			_objCharacter.WIL.Value = _objCharacter.WIL.MetatypeMinimum + intWIL;
			_objCharacter.EDG.Value = _objCharacter.EDG.MetatypeMinimum + intEDG;
			_objCharacter.MAG.Value = _objCharacter.MAG.MetatypeMinimum + intMAG;
			_objCharacter.RES.Value = _objCharacter.RES.MetatypeMinimum + intRES;

			nudBOD.Value = _objCharacter.BOD.Value;
			nudAGI.Value = _objCharacter.AGI.Value;
			nudREA.Value = _objCharacter.REA.Value;
			nudSTR.Value = _objCharacter.STR.Value;
			nudCHA.Value = _objCharacter.CHA.Value;
			nudINT.Value = _objCharacter.INT.Value;
			nudLOG.Value = _objCharacter.LOG.Value;
			nudWIL.Value = _objCharacter.WIL.Value;
			nudEDG.Value = _objCharacter.EDG.Value;
			if (_objCharacter.MAG.Value < 1)
				nudMAG.Value = 1;
			else
				nudMAG.Value = _objCharacter.MAG.Value;
			if (_objCharacter.RES.Value < 1)
				nudRES.Value = 1;
			else
				nudRES.Value = _objCharacter.RES.Value;
			_blnSkipUpdate = false;

			XmlDocument objMetatypeDoc = new XmlDocument();
			XmlNode objMetatypeNode;
			string strMetatype = "";
			string strBook = "";
			string strPage = "";

			objMetatypeDoc = XmlManager.Instance.Load("metatypes.xml");
			{
				objMetatypeNode = objMetatypeDoc.SelectSingleNode("/chummer/metatypes/metatype[name = \"" + _objCharacter.Metatype + "\"]");
				if (objMetatypeNode == null)
					objMetatypeDoc = XmlManager.Instance.Load("critters.xml");
				objMetatypeNode = objMetatypeDoc.SelectSingleNode("/chummer/metatypes/metatype[name = \"" + _objCharacter.Metatype + "\"]");

				if (objMetatypeNode["translate"] != null)
					strMetatype = objMetatypeNode["translate"].InnerText;
				else
					strMetatype = _objCharacter.Metatype;

				strBook = _objOptions.LanguageBookShort(objMetatypeNode["source"].InnerText);
				if (objMetatypeNode["altpage"] != null)
					strPage = objMetatypeNode["altpage"].InnerText;
				else
					strPage = objMetatypeNode["page"].InnerText;

				if (_objCharacter.Metavariant != "")
				{
					objMetatypeNode = objMetatypeNode.SelectSingleNode("metavariants/metavariant[name = \"" + _objCharacter.Metavariant + "\"]");

					if (objMetatypeNode["translate"] != null)
						strMetatype += " (" + objMetatypeNode["translate"].InnerText + ")";
					else
						strMetatype += " (" + _objCharacter.Metavariant + ")";

					strBook = _objOptions.LanguageBookShort(objMetatypeNode["source"].InnerText);
					if (objMetatypeNode["altpage"] != null)
						strPage = objMetatypeNode["altpage"].InnerText;
					else
						strPage = objMetatypeNode["page"].InnerText;
				}
			}
			lblMetatype.Text = strMetatype;
			lblMetatypeSource.Text = strBook + " " + strPage;
			tipTooltip.SetToolTip(lblMetatypeSource, _objOptions.LanguageBookLong(objMetatypeNode["source"].InnerText) + " " + LanguageManager.Instance.GetString("String_Page") + " " + strPage);

			// If we're working with Karma, the Metatype doesn't cost anything.
			if (_objCharacter.BuildMethod == CharacterBuildMethod.BP)
				lblMetatypeBP.Text = _objCharacter.MetatypeBP.ToString() + " " + LanguageManager.Instance.GetString("String_BP");
			else if (_objCharacter.BuildMethod == CharacterBuildMethod.Karma && _objOptions.MetatypeCostsKarma)
				lblMetatypeBP.Text = (_objCharacter.MetatypeBP * _objOptions.MetatypeCostsKarmaMultiplier).ToString() + " " + LanguageManager.Instance.GetString("String_Karma");
			else
				lblMetatypeBP.Text = "0 " + LanguageManager.Instance.GetString("String_Karma");

			string strToolTip = _objCharacter.Metatype;
			if (_objCharacter.Metavariant != "")
				strToolTip += " (" + _objCharacter.Metavariant + ")";
			strToolTip += " (" + _objCharacter.MetatypeBP.ToString() + ")";
			tipTooltip.SetToolTip(lblMetatypeBP, strToolTip);

			UpdateCharacterInfo();
		}

		/// <summary>
		/// Update the character's Mentor Spirit/Paragon information.
		/// </summary>
		private void UpdateMentorSpirits()
		{
			MentorSpirit objMentor = _objController.MentorInformation(MainController.MentorType.Mentor);
			MentorSpirit objParagon = _objController.MentorInformation(MainController.MentorType.Paragon);

			if (objMentor == null)
			{
				lblMentorSpiritLabel.Visible = false;
				lblMentorSpirit.Visible = false;
				lblMentorSpiritInformation.Visible = false;
			}
			else
			{
				lblMentorSpiritLabel.Visible = true;
				lblMentorSpirit.Visible = true;
				lblMentorSpiritInformation.Visible = true;
				lblMentorSpirit.Text = objMentor.Name;
				lblMentorSpiritInformation.Text = objMentor.Advantages;
			}

			if (objParagon == null)
			{
				lblParagonLabel.Visible = false;
				lblParagon.Visible = false;
				lblParagonInformation.Visible = false;
			}
			else
			{
				lblParagonLabel.Visible = true;
				lblParagon.Visible = true;
				lblParagonInformation.Visible = true;
				lblParagon.Text = objParagon.Name;
				lblParagonInformation.Text = objParagon.Advantages;
			}
		}

		/// <summary>
		/// Determine the integer portion of an item's Availability.
		/// </summary>
		/// <param name="strAvail">Availability string to parse.</param>
		private int GetAvailInt(string strAvail)
		{
			string strReturn = strAvail;
			if (strAvail.StartsWith("+"))
				strReturn = strReturn.Replace("+", string.Empty);
			if (strAvail.EndsWith(LanguageManager.Instance.GetString("String_AvailRestricted")))
				strReturn = strReturn.Replace(LanguageManager.Instance.GetString("String_AvailRestricted"), string.Empty);
			if (strAvail.EndsWith(LanguageManager.Instance.GetString("String_AvailForbidden")))
				strReturn = strReturn.Replace(LanguageManager.Instance.GetString("String_AvailForbidden"), string.Empty);

			return Convert.ToInt32(strReturn);
		}

		/// <summary>
		/// Create a Cyberware Suite from the Cyberware the character currently has.
		/// </summary>
		private void CreateCyberwareSuite(Improvement.ImprovementSource objSource)
		{
			// Make sure all of the Cyberware the character has is of the same grade.
			string strGrade = "";
			bool blnOK = true;
			foreach (Cyberware objCyberware in _objCharacter.Cyberware)
			{
				if (objCyberware.SourceType == objSource)
				{
					if (strGrade == "")
						strGrade = objCyberware.Grade.ToString();
					else
					{
						if (strGrade != objCyberware.Grade.ToString())
						{
							blnOK = false;
							break;
						}
					}
				}
			}
			if (!blnOK)
			{
				MessageBox.Show(LanguageManager.Instance.GetString("Message_CyberwareGradeMismatch"), LanguageManager.Instance.GetString("MessageTitle_CyberwareGradeMismatch"), MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}
			// The character has no Cyberware!
			if (strGrade == "")
			{
				MessageBox.Show(LanguageManager.Instance.GetString("Message_NoCyberware"), LanguageManager.Instance.GetString("MessageTitle_NoCyberware"), MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}

			frmCreateCyberwareSuite frmBuildCyberwareSuite = new frmCreateCyberwareSuite(_objCharacter, objSource);
			frmBuildCyberwareSuite.ShowDialog(this);
		}

		/// <summary>
		/// Set the ToolTips from the Language file.
		/// </summary>
		private void SetTooltips()
		{
			// Common Tab.
			tipTooltip.SetToolTip(lblAttributes, LanguageManager.Instance.GetString("Tip_CommonAttributes"));
			tipTooltip.SetToolTip(lblAttributesBase, LanguageManager.Instance.GetString("Tip_CommonAttributesBase"));
			tipTooltip.SetToolTip(lblAttributesAug, LanguageManager.Instance.GetString("Tip_CommonAttributesAug"));
			tipTooltip.SetToolTip(lblAttributesMetatype, LanguageManager.Instance.GetString("Tip_CommonAttributesMetatypeLimits"));
			tipTooltip.SetToolTip(lblNuyen, LanguageManager.Instance.GetString("Tip_CommonNuyen"));
			tipTooltip.SetToolTip(lblRatingLabel, LanguageManager.Instance.GetString("Tip_CommonAIRating"));
			tipTooltip.SetToolTip(lblSystemLabel, LanguageManager.Instance.GetString("Tip_CommonAISystem"));
			tipTooltip.SetToolTip(lblFirewallLabel, LanguageManager.Instance.GetString("Tip_CommonAIFirewall"));
			tipTooltip.SetToolTip(lblResponseLabel, LanguageManager.Instance.GetString("Tip_CommonAIResponse"));
			tipTooltip.SetToolTip(lblSignalLabel, LanguageManager.Instance.GetString("Tip_CommonAISignal"));
			tipTooltip.SetToolTip(lblContacts, LanguageManager.Instance.GetString("Tip_CommonContacts"));
			tipTooltip.SetToolTip(lblEnemies, LanguageManager.Instance.GetString("Tip_CommonEnemies"));
			// Skills Tab.
			tipTooltip.SetToolTip(lblSkillGroups, LanguageManager.Instance.GetString("Tip_SkillsSkillGroups"));
			tipTooltip.SetToolTip(lblActiveSkills, LanguageManager.Instance.GetString("Tip_SkillsActiveSkills"));
			tipTooltip.SetToolTip(lblKnowledgeSkills, LanguageManager.Instance.GetString("Tip_SkillsKnowledgeSkills"));
			// Spells Tab.
			tipTooltip.SetToolTip(lblSelectedSpells, LanguageManager.Instance.GetString("Tip_SpellsSelectedSpells"));
			tipTooltip.SetToolTip(lblSpirits, LanguageManager.Instance.GetString("Tip_SpellsSpirits"));
			// Complex Forms Tab.
			tipTooltip.SetToolTip(lblComplexForms, LanguageManager.Instance.GetString("Tip_TechnomancerComplexForms"));
			tipTooltip.SetToolTip(lblSprites, LanguageManager.Instance.GetString("Tip_TechnomancerSprites"));
			tipTooltip.SetToolTip(lblLivingPersonaResponseLabel, LanguageManager.Instance.GetString("Tip_TechnomancerResponse"));
			tipTooltip.SetToolTip(lblLivingPersonaSignalLabel, LanguageManager.Instance.GetString("Tip_TechnomancerSignal"));
			tipTooltip.SetToolTip(lblLivingPersonaSystemLabel, LanguageManager.Instance.GetString("Tip_TechnomancerSystem"));
			tipTooltip.SetToolTip(lblLivingPersonaFirewallLabel, LanguageManager.Instance.GetString("Tip_TechnomancerFirewall"));
			tipTooltip.SetToolTip(lblLivingPersonaBiofeedbackFilterLabel, LanguageManager.Instance.GetString("Tip_TechnomancerBiofeedbackFilter"));
			// Armor Tab.
			tipTooltip.SetToolTip(chkArmorEquipped, LanguageManager.Instance.GetString("Tip_ArmorEquipped"));
			// Weapon Tab.
			tipTooltip.SetToolTip(chkWeaponAccessoryInstalled, LanguageManager.Instance.GetString("Tip_WeaponInstalled"));
			// Gear Tab.
			tipTooltip.SetToolTip(chkActiveCommlink, LanguageManager.Instance.GetString("Tip_ActiveCommlink"));
			// Vehicles Tab.
			tipTooltip.SetToolTip(chkVehicleWeaponAccessoryInstalled, LanguageManager.Instance.GetString("Tip_WeaponInstalled"));
			// Character Info Tab.
			tipTooltip.SetToolTip(chkCharacterCreated, LanguageManager.Instance.GetString("Tip_CharacterCreated"));
			// Build Point Summary Tab.
			tipTooltip.SetToolTip(lblBuildPrimaryAttributes, LanguageManager.Instance.GetString("Tip_CommonAttributes"));
			tipTooltip.SetToolTip(lblBuildSpecialAttributes, LanguageManager.Instance.GetString("Tip_BuildSpecialAttributes"));
			tipTooltip.SetToolTip(lblBuildPositiveQualities, LanguageManager.Instance.GetString("Tip_BuildPositiveQualities"));
			tipTooltip.SetToolTip(lblBuildNegativeQualities, LanguageManager.Instance.GetString("Tip_BuildNegativeQualities"));
			tipTooltip.SetToolTip(lblBuildContacts, LanguageManager.Instance.GetString("Tip_CommonContacts").Replace("{0}", _objOptions.BPContact.ToString()));
			tipTooltip.SetToolTip(lblBuildEnemies, LanguageManager.Instance.GetString("Tip_CommonEnemies"));
			tipTooltip.SetToolTip(lblBuildNuyen, LanguageManager.Instance.GetString("Tip_CommonNuyen").Replace("{0}", String.Format("{0:###,###,##0}", _objOptions.NuyenPerBP)));
			tipTooltip.SetToolTip(lblBuildSkillGroups, LanguageManager.Instance.GetString("Tip_SkillsSkillGroups").Replace("{0}", _objOptions.BPSkillGroup.ToString()));
			tipTooltip.SetToolTip(lblBuildActiveSkills, LanguageManager.Instance.GetString("Tip_SkillsActiveSkills").Replace("{0}", _objOptions.BPActiveSkill.ToString()).Replace("{1}", _objOptions.BPActiveSkillSpecialization.ToString()));
			tipTooltip.SetToolTip(lblBuildKnowledgeSkills, LanguageManager.Instance.GetString("Tip_SkillsKnowledgeSkills").Replace("{0}", _objOptions.BPKnowledgeSkill.ToString()));
			tipTooltip.SetToolTip(lblBuildSpells, LanguageManager.Instance.GetString("Tip_SpellsSelectedSpells").Replace("{0}", _objOptions.BPSpell.ToString()));
			tipTooltip.SetToolTip(lblBuildFoci, LanguageManager.Instance.GetString("Tip_BuildFoci").Replace("{0}", _objOptions.BPFocus.ToString()));
			tipTooltip.SetToolTip(lblBuildSpirits, LanguageManager.Instance.GetString("Tip_SpellsSpirits").Replace("{0}", _objOptions.BPSpirit.ToString()));
			tipTooltip.SetToolTip(lblBuildSprites, LanguageManager.Instance.GetString("Tip_TechnomancerSprites").Replace("{0}", _objOptions.BPSpirit.ToString()));
			tipTooltip.SetToolTip(lblBuildComplexForms, LanguageManager.Instance.GetString("Tip_TechnomancerComplexForms").Replace("{0}", _objOptions.BPComplexForm.ToString()));
			tipTooltip.SetToolTip(lblBuildManeuvers, LanguageManager.Instance.GetString("Tip_BuildManeuvers").Replace("{0}", _objOptions.BPMartialArtManeuver.ToString()));
			// Other Info Tab.
			tipTooltip.SetToolTip(lblCMPhysicalLabel, LanguageManager.Instance.GetString("Tip_OtherCMPhysical"));
			tipTooltip.SetToolTip(lblCMStunLabel, LanguageManager.Instance.GetString("Tip_OtherCMStun"));
			tipTooltip.SetToolTip(lblINILabel, LanguageManager.Instance.GetString("Tip_OtherInitiative"));
			tipTooltip.SetToolTip(lblIPLabel, LanguageManager.Instance.GetString("Tip_OtherInitiativePasses"));
			tipTooltip.SetToolTip(lblMatrixINILabel, LanguageManager.Instance.GetString("Tip_OtherMatrixInitiative"));
			tipTooltip.SetToolTip(lblMatrixIPLabel, LanguageManager.Instance.GetString("Tip_OtherMatrixInitiativePasses"));
			tipTooltip.SetToolTip(lblAstralINILabel, LanguageManager.Instance.GetString("Tip_OtherAstralInitiative"));
			tipTooltip.SetToolTip(lblAstralIPLabel, LanguageManager.Instance.GetString("Tip_OtherAstralInitiativePasses"));
			tipTooltip.SetToolTip(lblBallisticArmorLabel, LanguageManager.Instance.GetString("Tip_OtherBallisticArmor"));
			tipTooltip.SetToolTip(lblImpactArmorLabel, LanguageManager.Instance.GetString("Tip_OtherImpactArmor"));
			tipTooltip.SetToolTip(lblESS, LanguageManager.Instance.GetString("Tip_OtherEssence"));
			tipTooltip.SetToolTip(lblRemainingNuyenLabel, LanguageManager.Instance.GetString("Tip_OtherNuyen"));
			tipTooltip.SetToolTip(lblMovementLabel, LanguageManager.Instance.GetString("Tip_OtherMovement"));
			tipTooltip.SetToolTip(lblSwimLabel, LanguageManager.Instance.GetString("Tip_OtherSwim"));
			tipTooltip.SetToolTip(lblFlyLabel, LanguageManager.Instance.GetString("Tip_OtherFly"));
			tipTooltip.SetToolTip(lblComposureLabel, LanguageManager.Instance.GetString("Tip_OtherComposure"));
			tipTooltip.SetToolTip(lblJudgeIntentionsLabel, LanguageManager.Instance.GetString("Tip_OtherJudgeIntentions"));
			tipTooltip.SetToolTip(lblLiftCarryLabel, LanguageManager.Instance.GetString("Tip_OtherLiftAndCarry"));
			tipTooltip.SetToolTip(lblMemoryLabel, LanguageManager.Instance.GetString("Tip_OtherMemory"));

			// Attribute Labels.
			lblBODLabel.Text = LanguageManager.Instance.GetString("String_AttributeBODLong") + " (" + LanguageManager.Instance.GetString("String_AttributeBODShort") + ")";
			lblAGILabel.Text = LanguageManager.Instance.GetString("String_AttributeAGILong") + " (" + LanguageManager.Instance.GetString("String_AttributeAGIShort") + ")";
			lblREALabel.Text = LanguageManager.Instance.GetString("String_AttributeREALong") + " (" + LanguageManager.Instance.GetString("String_AttributeREAShort") + ")";
			lblSTRLabel.Text = LanguageManager.Instance.GetString("String_AttributeSTRLong") + " (" + LanguageManager.Instance.GetString("String_AttributeSTRShort") + ")";
			lblCHALabel.Text = LanguageManager.Instance.GetString("String_AttributeCHALong") + " (" + LanguageManager.Instance.GetString("String_AttributeCHAShort") + ")";
			lblINTLabel.Text = LanguageManager.Instance.GetString("String_AttributeINTLong") + " (" + LanguageManager.Instance.GetString("String_AttributeINTShort") + ")";
			lblLOGLabel.Text = LanguageManager.Instance.GetString("String_AttributeLOGLong") + " (" + LanguageManager.Instance.GetString("String_AttributeLOGShort") + ")";
			lblWILLabel.Text = LanguageManager.Instance.GetString("String_AttributeWILLong") + " (" + LanguageManager.Instance.GetString("String_AttributeWILShort") + ")";
			lblEDGLabel.Text = LanguageManager.Instance.GetString("String_AttributeEDGLong") + " (" + LanguageManager.Instance.GetString("String_AttributeEDGShort") + ")";
			lblMAGLabel.Text = LanguageManager.Instance.GetString("String_AttributeMAGLong") + " (" + LanguageManager.Instance.GetString("String_AttributeMAGShort") + ")";
			lblRESLabel.Text = LanguageManager.Instance.GetString("String_AttributeRESLong") + " (" + LanguageManager.Instance.GetString("String_AttributeRESShort") + ")";

			// Reposition controls based on their new sizes.
			// Common Tab.
			txtAlias.Left = lblAlias.Left + lblAlias.Width + 6;
			txtAlias.Width = lblMetatypeLabel.Left - 6 - txtAlias.Left;
			cmdDeleteQuality.Left = cmdAddQuality.Left + cmdAddQuality.Width + 6;
			// Skills Tab.
			cboSkillFilter.Left = cmdAddExoticSkill.Left - cboSkillFilter.Width - 6;
			// Martial Arts Tab.
			cmdAddManeuver.Left = cmdAddMartialArt.Left + cmdAddMartialArt.Width + 6;
			cmdDeleteMartialArt.Left = cmdAddManeuver.Left + cmdAddManeuver.Width + 6;
			// Magician Tab.
			cmdDeleteSpell.Left = cmdAddSpell.Left + cmdAddSpell.Width + 6;
			// Technomancer Tab.
			cmdDeleteComplexForm.Left = cmdAddComplexForm.Left + cmdAddComplexForm.Width + 6;
			// Critter Powers Tab.
			cmdDeleteCritterPower.Left = cmdAddCritterPower.Left + cmdAddCritterPower.Width + 6;
			// Initiation Tab.
			cmdDeleteMetamagic.Left = cmdAddMetamagic.Left + cmdAddMetamagic.Width + 6;
			// Cyberware Tab.
			cmdAddBioware.Left = cmdAddCyberware.Left + cmdAddCyberware.Width + 6;
			cmdDeleteCyberware.Left = cmdAddBioware.Left + cmdAddBioware.Width + 6;
			// Lifestyle Tab.
			cmdDeleteLifestyle.Left = cmdAddLifestyle.Left + cmdAddLifestyle.Width + 6;
			// Armor Tab.
			cmdDeleteArmor.Left = cmdAddArmor.Left + cmdAddArmor.Width + 6;
			cmdAddArmorBundle.Left = cmdDeleteArmor.Left + cmdDeleteArmor.Width + 6;
			cmdArmorEquipAll.Left = chkArmorEquipped.Left + chkArmorEquipped.Width + 6;
			cmdArmorUnEquipAll.Left = cmdArmorEquipAll.Left + cmdArmorEquipAll.Width + 6;
			// Weapons Tab.
			cmdDeleteWeapon.Left = cmdAddWeapon.Left + cmdAddWeapon.Width + 6;
			cmdAddWeaponLocation.Left = cmdDeleteWeapon.Left + cmdDeleteWeapon.Width + 6;
			// Gear Tab.
			cmdDeleteGear.Left = cmdAddGear.Left + cmdAddGear.Width + 6;
			cmdAddLocation.Left = cmdDeleteGear.Left + cmdDeleteGear.Width + 6;
			// Vehicle Tab.
			cmdDeleteVehicle.Left = cmdAddVehicle.Left + cmdAddVehicle.Width + 6;
			cmdAddVehicleLocation.Left = cmdDeleteVehicle.Left + cmdDeleteVehicle.Width + 6;
		}

		/// <summary>
		/// Refresh the list of Improvements.
		/// </summary>
		private void RefreshImprovements()
		{
		}

		private void MoveControls()
		{
			int intWidth = 0;

			// Common tab.
			lblAlias.Left = Math.Max(288, cmdDeleteQuality.Left + cmdDeleteQuality.Width + 6);
			txtAlias.Left = lblAlias.Left + lblAlias.Width + 6;
			txtAlias.Width = lblMetatypeLabel.Left - txtAlias.Left - 6;
			nudNuyen.Left = lblNuyen.Left + lblNuyen.Width + 6;
			lblNuyenTotal.Left = nudNuyen.Left + nudNuyen.Width + 6;

			intWidth = Math.Max(lblRatingLabel.Width, lblSystemLabel.Width);
			intWidth = Math.Max(intWidth, lblFirewallLabel.Width);
			intWidth = Math.Max(intWidth, lblResponseLabel.Width);
			intWidth = Math.Max(intWidth, lblSignalLabel.Width);

			lblRating.Left = lblRatingLabel.Left + intWidth + 6;
			lblSystem.Left = lblSystemLabel.Left + intWidth + 6;
			lblFirewall.Left = lblFirewallLabel.Left + intWidth + 6;
			nudResponse.Left = lblResponseLabel.Left + intWidth + 6;
			nudSignal.Left = lblSignalLabel.Left + intWidth + 6;

			// Skills tab.
			lblKnowledgeSkillPoints.Left = lblKnowledgeSkillPointsTitle.Left + lblKnowledgeSkillPointsTitle.Width + 6;

			// Martial Arts tab.
			intWidth = Math.Max(lblMartialArtsRatingLabel.Width, lblMartialArtSourceLabel.Width);
			nudMartialArtsRating.Left = lblMartialArtsRatingLabel.Left + intWidth + 6;
			lblMartialArtSource.Left = lblMartialArtSourceLabel.Left + intWidth + 6;

			// Spells and Spirits tab.
			intWidth = Math.Max(lblSpellDescriptorsLabel.Width, lblSpellCategoryLabel.Width);
			intWidth = Math.Max(intWidth, lblSpellRangeLabel.Width);
			intWidth = Math.Max(intWidth, lblSpellDurationLabel.Width);
			intWidth = Math.Max(intWidth, lblSpellSourceLabel.Width);

			lblSpellDescriptors.Left = lblSpellDescriptorsLabel.Left + intWidth + 6;
			lblSpellCategory.Left = lblSpellCategoryLabel.Left + intWidth + 6;
			lblSpellRange.Left = lblSpellRangeLabel.Left + intWidth + 6;
			lblSpellDuration.Left = lblSpellDurationLabel.Left + intWidth + 6;
			lblSpellSource.Left = lblSpellSourceLabel.Left + intWidth + 6;

			intWidth = Math.Max(lblSpellTypeLabel.Width, lblSpellDamageLabel.Width);
			intWidth = Math.Max(intWidth, lblSpellDVLabel.Width);
			lblSpellTypeLabel.Left = lblSpellCategoryLabel.Left + 179;
			lblSpellType.Left = lblSpellTypeLabel.Left + intWidth + 6;
			lblSpellDamageLabel.Left = lblSpellRangeLabel.Left + 179;
			lblSpellDamage.Left = lblSpellDamageLabel.Left + intWidth + 6;
			lblSpellDVLabel.Left = lblSpellDurationLabel.Left + 179;
			lblSpellDV.Left = lblSpellDVLabel.Left + intWidth + 6;

			intWidth = Math.Max(lblTraditionLabel.Width, lblDrainAttributesLabel.Width);
			intWidth = Math.Max(intWidth, lblMentorSpiritLabel.Width);
			cboTradition.Left = lblTraditionLabel.Left + intWidth + 6;
			lblDrainAttributes.Left = lblDrainAttributesLabel.Left + intWidth + 6;
			lblDrainAttributesValue.Left = lblDrainAttributes.Left + 91;
			lblMentorSpirit.Left = lblMentorSpiritLabel.Left + intWidth + 6;

			// Adept Powers tab.
			lblPowerPoints.Left = lblPowerPointsLabel.Left + lblPowerPointsLabel.Width + 6;

			// Sprites and Complex Forms tab.
			intWidth = Math.Max(lblComplexFormRatingLabel.Width, lblComplexFormSkillLabel.Width);
			intWidth = Math.Max(intWidth, lblComplexFormSourceLabel.Width);
			nudComplexFormRating.Left = lblComplexFormRatingLabel.Left + intWidth + 6;
			lblComplexFormSkill.Left = lblComplexFormSkillLabel.Left + intWidth + 6;
			lblComplexFormSource.Left = lblComplexFormSourceLabel.Left + intWidth + 6;

			intWidth = Math.Max(lblStreamLabel.Width, lblFadingAttributesLabel.Width);
			intWidth = Math.Max(intWidth, lblParagonLabel.Width);
			cboStream.Left = lblStreamLabel.Left + intWidth + 6;
			lblFadingAttributes.Left = lblFadingAttributesLabel.Left + intWidth + 6;
			lblFadingAttributesValue.Left = lblFadingAttributes.Left + 91;
			lblParagon.Left = lblParagonLabel.Left + intWidth + 6;

			lblLivingPersonaResponse.Left = lblLivingPersonaResponseLabel.Left + lblLivingPersonaResponseLabel.Width + 6;
			lblLivingPersonaSignalLabel.Left = lblLivingPersonaResponse.Left + 35;
			lblLivingPersonaSignal.Left = lblLivingPersonaSignalLabel.Left + lblLivingPersonaSignalLabel.Width + 6;
			lblLivingPersonaSystemLabel.Left = lblLivingPersonaSignal.Left + 35;
			lblLivingPersonaSystem.Left = lblLivingPersonaSystemLabel.Left + lblLivingPersonaSystemLabel.Width + 6;
			lblLivingPersonaFirewallLabel.Left = lblLivingPersonaSystem.Left + 35;
			lblLivingPersonaFirewall.Left = lblLivingPersonaFirewallLabel.Left + lblLivingPersonaFirewallLabel.Width + 6;
			lblLivingPersonaBiofeedbackFilterLabel.Left = lblLivingPersonaFirewall.Left + 35;
			lblLivingPersonaBiofeedbackFilter.Left = lblLivingPersonaBiofeedbackFilterLabel.Left + lblLivingPersonaBiofeedbackFilterLabel.Width + 6;

			if (_objOptions.AlternateComplexFormCost)
			{
				lblComplexFormRatingLabel.Visible = false;
				nudComplexFormRating.Visible = false;
			}

			// Critter Powers tab.
			lblCritterPowerPointsLabel.Left = cmdDeleteCritterPower.Left + cmdDeleteCritterPower.Width + 16;
			lblCritterPowerPoints.Left = lblCritterPowerPointsLabel.Left + lblCritterPowerPointsLabel.Width + 6;

			intWidth = Math.Max(lblCritterPowerNameLabel.Width, lblCritterPowerCategoryLabel.Width);
			intWidth = Math.Max(intWidth, lblCritterPowerTypeLabel.Width);
			intWidth = Math.Max(intWidth, lblCritterPowerActionLabel.Width);
			intWidth = Math.Max(intWidth, lblCritterPowerRangeLabel.Width);
			intWidth = Math.Max(intWidth, lblCritterPowerDurationLabel.Width);
			intWidth = Math.Max(intWidth, lblCritterPowerSourceLabel.Width);
			intWidth = Math.Max(intWidth, lblCritterPowerPointCostLabel.Width);

			lblCritterPowerName.Left = lblCritterPowerNameLabel.Left + intWidth + 6;
			lblCritterPowerCategory.Left = lblCritterPowerCategoryLabel.Left + intWidth + 6;
			lblCritterPowerType.Left = lblCritterPowerTypeLabel.Left + intWidth + 6;
			lblCritterPowerAction.Left = lblCritterPowerActionLabel.Left + intWidth + 6;
			lblCritterPowerRange.Left = lblCritterPowerRangeLabel.Left + intWidth + 6;
			lblCritterPowerDuration.Left = lblCritterPowerDurationLabel.Left + intWidth + 6;
			lblCritterPowerSource.Left = lblCritterPowerSourceLabel.Left + intWidth + 6;
			lblCritterPowerPointCost.Left = lblCritterPowerPointCostLabel.Left + intWidth + 6;

			// Initiation and Submersion tab.

			// Cyberware and Bioware tab.
			intWidth = Math.Max(lblCyberwareNameLabel.Width, lblCyberwareCategoryLabel.Width);
			intWidth = Math.Max(intWidth, lblCyberwareGradeLabel.Width);
			intWidth = Math.Max(intWidth, lblCyberwareEssenceLabel.Width);
			intWidth = Math.Max(intWidth, lblCyberwareAvailLabel.Width);
			intWidth = Math.Max(intWidth, lblCyberwareSourceLabel.Width);

			lblCyberwareName.Left = lblCyberwareNameLabel.Left + intWidth + 6;
			lblCyberwareCategory.Left = lblCyberwareCategoryLabel.Left + intWidth + 6;
			cboCyberwareGrade.Left = lblCyberwareGradeLabel.Left + intWidth + 6;
			lblCyberwareEssence.Left = lblCyberwareEssenceLabel.Left + intWidth + 6;
			lblCyberwareAvail.Left = lblCyberwareAvailLabel.Left + intWidth + 6;
			lblCyberwareSource.Left = lblCyberwareSourceLabel.Left + intWidth + 6;

			intWidth = lblEssenceHoleESSLabel.Width;
			lblCyberwareESS.Left = lblEssenceHoleESSLabel.Left + intWidth + 6;
			lblBiowareESS.Left = lblEssenceHoleESSLabel.Left + intWidth + 6;
			lblEssenceHoleESS.Left = lblEssenceHoleESSLabel.Left + intWidth + 6;

			intWidth = Math.Max(lblCyberwareRatingLabel.Width, lblCyberwareCapacityLabel.Width);
			intWidth = Math.Max(intWidth, lblCyberwareCostLabel.Width);

			lblCyberwareRatingLabel.Left = cboCyberwareGrade.Left + cboCyberwareGrade.Width + 16;
			nudCyberwareRating.Left = lblCyberwareRatingLabel.Left + intWidth + 6;
			lblCyberwareCapacityLabel.Left = cboCyberwareGrade.Left + cboCyberwareGrade.Width + 16;
			lblCyberwareCapacity.Left = lblCyberwareCapacityLabel.Left + intWidth + 6;
			lblCyberwareCostLabel.Left = cboCyberwareGrade.Left + cboCyberwareGrade.Width + 16;
			lblCyberwareCost.Left = lblCyberwareCostLabel.Left + intWidth + 6;
			chkCyberwareBlackMarketDiscount.Left = lblCyberwareCostLabel.Left;

			// Street Gear tab.
			// Lifestyles tab.
			lblLifestyleCost.Left = lblLifestyleCostLabel.Left + lblLifestyleCostLabel.Width + 6;
			lblLifestyleSource.Left = lblLifestyleSourceLabel.Left + lblLifestyleSourceLabel.Width + 6;
			lblLifestyleTotalCost.Left = lblLifestyleMonthsLabel.Left + lblLifestyleMonthsLabel.Width + 6;
			lblLifestyleStartingNuyen.Left = lblLifestyleStartingNuyenLabel.Left + lblLifestyleStartingNuyenLabel.Width + 6;

			intWidth = Math.Max(lblLifestyleComfortsLabel.Width, lblLifestyleEntertainmentLabel.Width);
			intWidth = Math.Max(intWidth, lblLifestyleNecessitiesLabel.Width);
			intWidth = Math.Max(intWidth, lblLifestyleNeighborhoodLabel.Width);
			intWidth = Math.Max(intWidth, lblLifestyleSecurityLabel.Width);

			lblLifestyleComforts.Left = lblLifestyleComfortsLabel.Left + intWidth + 6;
			lblLifestyleEntertainment.Left = lblLifestyleEntertainmentLabel.Left + intWidth + 6;
			lblLifestyleNecessities.Left = lblLifestyleNecessitiesLabel.Left + intWidth + 6;
			lblLifestyleNeighborhood.Left = lblLifestyleNeighborhoodLabel.Left + intWidth + 6;
			lblLifestyleSecurity.Left = lblLifestyleSecurityLabel.Left + intWidth + 6;

			lblLifestyleQualitiesLabel.Left = lblLifestyleComforts.Left + 132;
			lblLifestyleQualities.Left = lblLifestyleQualitiesLabel.Left + 14;
			lblLifestyleQualities.Width = tabLifestyle.Width - lblLifestyleQualities.Left - 10;

			// Armor tab.
			intWidth = Math.Max(lblArmorBallisticLabel.Width, lblArmorImpactLabel.Width);
			intWidth = Math.Max(intWidth, lblArmorRatingLabel.Width);
			intWidth = Math.Max(intWidth, lblArmorCapacityLabel.Width);
			intWidth = Math.Max(intWidth, lblArmorSourceLabel.Width);

			lblArmorBallistic.Left = lblArmorBallisticLabel.Left + intWidth + 6;
			lblArmorImpact.Left = lblArmorImpactLabel.Left + intWidth + 6;
			nudArmorRating.Left = lblArmorRatingLabel.Left + intWidth + 6;
			lblArmorCapacity.Left = lblArmorCapacityLabel.Left + intWidth + 6;
			lblArmorSource.Left = lblArmorSourceLabel.Left + intWidth + 6;

			lblArmorAvailLabel.Left = nudArmorRating.Left + nudArmorRating.Width + 6;
			lblArmorAvail.Left = lblArmorAvailLabel.Left + lblArmorAvailLabel.Width + 6;

			lblArmorCostLabel.Left = lblArmorAvail.Left + lblArmorAvail.Width + 6;
			lblArmorCost.Left = lblArmorCostLabel.Left + lblArmorCostLabel.Width + 6;
			chkArmorBlackMarketDiscount.Left = lblArmorCostLabel.Left;

			// Weapons tab.
			lblWeaponName.Left = lblWeaponNameLabel.Left + lblWeaponNameLabel.Width + 6;
			lblWeaponCategory.Left = lblWeaponCategoryLabel.Left + lblWeaponCategoryLabel.Width + 6;

			intWidth = Math.Max(lblWeaponNameLabel.Width, lblWeaponCategoryLabel.Width);
			intWidth = Math.Max(intWidth, lblWeaponDamageLabel.Width);
			intWidth = Math.Max(intWidth, lblWeaponReachLabel.Width);
			intWidth = Math.Max(intWidth, lblWeaponAvailLabel.Width);
			intWidth = Math.Max(intWidth, lblWeaponSlotsLabel.Width);
			intWidth = Math.Max(intWidth, lblWeaponSourceLabel.Width);

			lblWeaponName.Left = lblWeaponNameLabel.Left + intWidth + 6;
			lblWeaponCategory.Left = lblWeaponCategoryLabel.Left + intWidth + 6;
			lblWeaponDamage.Left = lblWeaponDamageLabel.Left + intWidth + 6;
			lblWeaponReach.Left = lblWeaponReachLabel.Left + intWidth + 6;
			lblWeaponAvail.Left = lblWeaponAvailLabel.Left + intWidth + 6;
			lblWeaponSlots.Left = lblWeaponSlotsLabel.Left + intWidth + 6;
			lblWeaponSource.Left = lblWeaponSourceLabel.Left + intWidth + 6;

			intWidth = Math.Max(lblWeaponRCLabel.Width, lblWeaponModeLabel.Width);
			intWidth = Math.Max(intWidth, lblWeaponCostLabel.Width);

			lblWeaponRCLabel.Left = lblWeaponDamageLabel.Left + 176;
			lblWeaponRC.Left = lblWeaponRCLabel.Left + intWidth + 6;
			lblWeaponModeLabel.Left = lblWeaponDamageLabel.Left + 176;
			lblWeaponMode.Left = lblWeaponModeLabel.Left + intWidth + 6;
			lblWeaponCostLabel.Left = lblWeaponDamageLabel.Left + 176;
			lblWeaponCost.Left = lblWeaponCostLabel.Left + intWidth + 6;
			chkIncludedInWeapon.Left = lblWeaponDamageLabel.Left + 176;
			chkWeaponBlackMarketDiscount.Left = chkIncludedInWeapon.Left;

			intWidth = Math.Max(lblWeaponAPLabel.Width, lblWeaponAmmoLabel.Width);
			intWidth = Math.Max(intWidth, lblWeaponConcealLabel.Width);

			lblWeaponAPLabel.Left = lblWeaponRC.Left + 95;
			lblWeaponAP.Left = lblWeaponAPLabel.Left + intWidth + 6;
			lblWeaponAmmoLabel.Left = lblWeaponRC.Left + 95;
			lblWeaponAmmo.Left = lblWeaponAmmoLabel.Left + intWidth + 6;
			lblWeaponConcealLabel.Left = lblWeaponRC.Left + 95;
			lblWeaponConceal.Left = lblWeaponConcealLabel.Left + intWidth + 6;
			chkWeaponAccessoryInstalled.Left = lblWeaponRC.Left + 95;

			lblWeaponDicePool.Left = lblWeaponDicePoolLabel.Left + intWidth + 6;

			// Gear tab.
			intWidth = Math.Max(lblGearNameLabel.Width, lblGearCategoryLabel.Width);
			intWidth = Math.Max(intWidth, lblGearRatingLabel.Width);
			intWidth = Math.Max(intWidth, lblGearCapacityLabel.Width);
			intWidth = Math.Max(intWidth, lblGearQtyLabel.Width);

			chkCommlinks.Left = cmdAddLocation.Left + cmdAddLocation.Width + 16;

			lblGearName.Left = lblGearNameLabel.Left + intWidth + 6;
			lblGearCategory.Left = lblGearCategoryLabel.Left + intWidth + 6;
			nudGearRating.Left = lblGearRatingLabel.Left + intWidth + 6;
			lblGearCapacity.Left = lblGearCapacityLabel.Left + intWidth + 6;
			nudGearQty.Left = lblGearQtyLabel.Left + intWidth + 6;

			lblGearAvailLabel.Left = nudGearRating.Left + 52;
			lblGearAvail.Left = lblGearAvailLabel.Left + lblGearAvailLabel.Width + 6;
			lblGearCostLabel.Left = lblGearAvail.Left + 75;
			lblGearCost.Left = lblGearCostLabel.Left + lblGearCostLabel.Width + 6;
			chkGearBlackMarketDiscount.Left = lblGearCostLabel.Left;

			intWidth = Math.Max(lblGearResponseLabel.Width, lblGearDamageLabel.Width);
			lblGearResponse.Left = lblGearResponseLabel.Left + intWidth + 6;
			lblGearDamage.Left = lblGearDamageLabel.Left + intWidth + 6;

			intWidth = Math.Max(lblGearSignalLabel.Width, lblGearAPLabel.Width);
			lblGearSignalLabel.Left = lblGearResponse.Left + lblGearResponse.Width + 16;
			lblGearSignal.Left = lblGearSignalLabel.Left + intWidth + 6;
			lblGearAPLabel.Left = lblGearResponse.Left + lblGearResponse.Width + 16;
			lblGearAP.Left = lblGearAPLabel.Left + intWidth + 6;

			lblGearSystemLabel.Left = lblGearSignal.Left + lblGearSignal.Width + 16;
			lblGearSystem.Left = lblGearSystemLabel.Left + lblGearSystemLabel.Width + 6;
			lblGearFirewallLabel.Left = lblGearSystem.Left + lblGearSystem.Width + 16;
			lblGearFirewall.Left = lblGearFirewallLabel.Left + lblGearFirewallLabel.Width + 6;

			lblGearSource.Left = lblGearSourceLabel.Left + lblGearSourceLabel.Width + 6;
			chkGearHomeNode.Left = chkGearEquipped.Left + chkGearEquipped.Width + 16;

			// Vehicles and Drones tab.
			intWidth = Math.Max(lblVehicleNameLabel.Width, lblVehicleCategoryLabel.Width);
			intWidth = Math.Max(intWidth, lblVehicleHandlingLabel.Width);
			intWidth = Math.Max(intWidth, lblVehiclePilotLabel.Width);
			intWidth = Math.Max(intWidth, lblVehicleFirewallLabel.Width);
			intWidth = Math.Max(intWidth, lblVehicleAvailLabel.Width);
			intWidth = Math.Max(intWidth, lblVehicleRatingLabel.Width);
			intWidth = Math.Max(intWidth, lblVehicleGearQtyLabel.Width);
			intWidth = Math.Max(intWidth, lblVehicleSourceLabel.Width);

			lblVehicleName.Left = lblVehicleNameLabel.Left + intWidth + 6;
			lblVehicleCategory.Left = lblVehicleCategoryLabel.Left + intWidth + 6;
			lblVehicleHandling.Left = lblVehicleHandlingLabel.Left + intWidth + 6;
			lblVehiclePilot.Left = lblVehiclePilotLabel.Left + intWidth + 6;
			lblVehicleFirewall.Left = lblVehicleFirewallLabel.Left + intWidth + 6;
			lblVehicleAvail.Left = lblVehicleAvailLabel.Left + intWidth + 6;
			nudVehicleRating.Left = lblVehicleRatingLabel.Left + intWidth + 6;
			nudVehicleGearQty.Left = lblVehicleGearQtyLabel.Left + intWidth + 6;
			lblVehicleSource.Left = lblVehicleSourceLabel.Left + intWidth + 6;
			lblVehicleWeaponName.Left = lblVehicleWeaponNameLabel.Left + intWidth + 6;
			lblVehicleWeaponCategory.Left = lblVehicleWeaponCategoryLabel.Left + intWidth + 6;
			lblVehicleWeaponDamage.Left = lblVehicleWeaponDamageLabel.Left + intWidth + 6;

			intWidth = Math.Max(lblVehicleAccelLabel.Width, lblVehicleBodyLabel.Width);
			intWidth = Math.Max(intWidth, lblVehicleSignalLabel.Width);
			intWidth = Math.Max(intWidth, lblVehicleCostLabel.Width);

			lblVehicleAccelLabel.Left = lblVehicleHandling.Left + 47;
			lblVehicleAccel.Left = lblVehicleAccelLabel.Left + intWidth + 6;
			lblVehicleBodyLabel.Left = lblVehicleHandling.Left + 47;
			lblVehicleBody.Left = lblVehicleBodyLabel.Left + intWidth + 6;
			lblVehicleSignalLabel.Left = lblVehicleHandling.Left + 47;
			lblVehicleSignal.Left = lblVehicleSignalLabel.Left + intWidth + 6;
			lblVehicleCostLabel.Left = lblVehicleHandling.Left + 47;
			lblVehicleCost.Left = lblVehicleCostLabel.Left + intWidth + 6;

			chkVehicleIncludedInWeapon.Left = lblVehicleAccel.Left;
			chkVehicleHomeNode.Left = lblVehicleAccel.Left;
			chkVehicleBlackMarketDiscount.Left = lblVehicleAccel.Left;

			intWidth = Math.Max(lblVehicleSpeedLabel.Width, lblVehicleArmorLabel.Width);
			intWidth = Math.Max(intWidth, lblVehicleResponseLabel.Width);

			lblVehicleSpeedLabel.Left = lblVehicleAccel.Left + 53;
			lblVehicleSpeed.Left = lblVehicleSpeedLabel.Left + intWidth + 6;
			lblVehicleArmorLabel.Left = lblVehicleAccel.Left + 53;
			lblVehicleArmor.Left = lblVehicleArmorLabel.Left + intWidth + 6;
			lblVehicleResponseLabel.Left = lblVehicleAccel.Left + 53;
			lblVehicleResponse.Left = lblVehicleResponseLabel.Left + intWidth + 6;

			intWidth = Math.Max(lblVehicleDeviceLabel.Width, lblVehicleSensorLabel.Width);
			intWidth = Math.Max(intWidth, lblVehicleSystemLabel.Width);

			lblVehicleDeviceLabel.Left = lblVehicleSpeed.Left + 35;
			lblVehicleDevice.Left = lblVehicleDeviceLabel.Left + intWidth + 6;
			lblVehicleSensorLabel.Left = lblVehicleSpeed.Left + 35;
			lblVehicleSensor.Left = lblVehicleSensorLabel.Left + intWidth + 6;
			lblVehicleSystemLabel.Left = lblVehicleSpeed.Left + 35;
			lblVehicleSystem.Left = lblVehicleSystemLabel.Left + intWidth + 6;

			lblVehicleSlotsLabel.Left = lblVehicleCost.Left + 94;
			lblVehicleSlots.Left = lblVehicleSlotsLabel.Left + lblVehicleSlotsLabel.Width + 6;
			chkVehicleWeaponAccessoryInstalled.Left = lblVehicleDeviceLabel.Left;

			// Character Info.
			intWidth = Math.Max(lblSex.Width, lblHeight.Width);
			txtSex.Left = lblSex.Left + intWidth + 6;
			txtSex.Width = lblAge.Left - txtSex.Left - 16;
			txtHeight.Left = lblHeight.Left + intWidth + 6;
			txtHeight.Width = lblWeight.Left - txtHeight.Left - 16;

			intWidth = Math.Max(lblAge.Width, lblWeight.Width);
			txtAge.Left = lblAge.Left + intWidth + 6;
			txtAge.Width = lblEyes.Left - txtAge.Left - 16;
			txtWeight.Left = lblWeight.Left + intWidth + 6;
			txtWeight.Width = lblSkin.Left - txtWeight.Left - 16;

			intWidth = Math.Max(lblEyes.Width, lblSkin.Width);
			txtEyes.Left = lblEyes.Left + intWidth + 6;
			txtEyes.Width = lblHair.Left - txtEyes.Left - 16;
			txtSkin.Left = lblSkin.Left + intWidth + 6;
			txtSkin.Width = lblCharacterName.Left - txtSkin.Left - 16;

			intWidth = Math.Max(lblHair.Width, lblCharacterName.Width);
			txtHair.Left = lblHair.Left + intWidth + 6;
			txtHair.Width = lblPlayerName.Left - txtHair.Left - 16;
			txtCharacterName.Left = lblCharacterName.Left + intWidth + 6;
			txtCharacterName.Width = lblPlayerName.Left - txtCharacterName.Left - 16;

			txtPlayerName.Left = lblPlayerName.Left + lblPlayerName.Width + 6;
			txtPlayerName.Width = tabCharacterInfo.Width - txtPlayerName.Left - 16;

			intWidth = Math.Max(lblStreetCred.Width, lblNotoriety.Width);
			intWidth = Math.Max(intWidth, lblPublicAware.Width);
			lblStreetCredTotal.Left = lblStreetCred.Left + intWidth + 6;
			lblNotorietyTotal.Left = lblNotoriety.Left + intWidth + 6;
			lblPublicAwareTotal.Left = lblPublicAware.Left + intWidth + 6;

			// Improvements tab.

			// Other Info tab.
			intWidth = Math.Max(lblCMPhysicalLabel.Width, lblCMStunLabel.Width);
			intWidth = Math.Max(intWidth, lblINILabel.Width);
			intWidth = Math.Max(intWidth, lblIPLabel.Width);
			intWidth = Math.Max(intWidth, lblMatrixINILabel.Width);
			intWidth = Math.Max(intWidth, lblMatrixIPLabel.Width);
			intWidth = Math.Max(intWidth, lblAstralINILabel.Width);
			intWidth = Math.Max(intWidth, lblAstralIPLabel.Width);
			intWidth = Math.Max(intWidth, lblBallisticArmorLabel.Width);
			intWidth = Math.Max(intWidth, lblImpactArmorLabel.Width);
			intWidth = Math.Max(intWidth, lblESS.Width);
			intWidth = Math.Max(intWidth, lblRemainingNuyenLabel.Width);
			intWidth = Math.Max(intWidth, lblComposureLabel.Width);
			intWidth = Math.Max(intWidth, lblJudgeIntentionsLabel.Width);
			intWidth = Math.Max(intWidth, lblLiftCarryLabel.Width);
			intWidth = Math.Max(intWidth, lblMemoryLabel.Width);
			intWidth = Math.Max(intWidth, lblMovementLabel.Width);
			intWidth = Math.Max(intWidth, lblSwimLabel.Width);
			intWidth = Math.Max(intWidth, lblFlyLabel.Width);

			lblCMPhysical.Left = lblCMPhysicalLabel.Left + intWidth + 6;
			lblCMStun.Left = lblCMPhysical.Left;
			lblINI.Left = lblCMPhysical.Left;
			lblIP.Left = lblCMPhysical.Left;
			lblMatrixINI.Left = lblCMPhysical.Left;
			lblMatrixIP.Left = lblCMPhysical.Left;
			lblAstralINI.Left = lblCMPhysical.Left;
			lblAstralIP.Left = lblCMPhysical.Left;
			lblBallisticArmor.Left = lblCMPhysical.Left;
			lblImpactArmor.Left = lblCMPhysical.Left;
			lblESSMax.Left = lblCMPhysical.Left;
			lblRemainingNuyen.Left = lblCMPhysical.Left;
			lblComposure.Left = lblCMPhysical.Left;
			lblJudgeIntentions.Left = lblCMPhysical.Left;
			lblLiftCarry.Left = lblCMPhysical.Left;
			lblMemory.Left = lblCMPhysical.Left;
			lblMovement.Left = lblCMPhysical.Left;
			lblSwim.Left = lblCMPhysical.Left;
			lblFly.Left = lblCMPhysical.Left;
		}

		/// <summary>
		/// Change the size of a Vehicle's Sensor
		/// </summary>
		/// <param name="objVehicle">Vehicle to modify.</param>
		/// <param name="blnIncrease">True if the Sensor should increase in size, False if it should decrease.</param>
		private void ChangeVehicleSensor(Vehicle objVehicle, bool blnIncrease)
		{
			XmlDocument objXmlDocument = XmlManager.Instance.Load("gear.xml");
			XmlNode objNewNode;
			bool blnFound = false;

			Gear objSensor = new Gear(_objCharacter);
			Gear objNewSensor = new Gear(_objCharacter);

			TreeNode objTreeNode = new TreeNode();
			List<Weapon> lstWeapons = new List<Weapon>();
			List<TreeNode> lstWeaponNodes = new List<TreeNode>();
			foreach (Gear objCurrentGear in objVehicle.Gear)
			{
				if (objCurrentGear.Name == "Microdrone Sensor")
				{
					if (blnIncrease)
					{
						objNewNode = objXmlDocument.SelectSingleNode("/chummer/gears/gear[name = \"Minidrone Sensor\" and category = \"Sensors\"]");
						objNewSensor.Create(objNewNode, _objCharacter, objTreeNode, 0, lstWeapons, lstWeaponNodes);
						objSensor = objCurrentGear;
						blnFound = true;
					}
					break;
				}
				else if (objCurrentGear.Name == "Minidrone Sensor")
				{
					if (blnIncrease)
						objNewNode = objXmlDocument.SelectSingleNode("/chummer/gears/gear[name = \"Small Drone Sensor\" and category = \"Sensors\"]");
					else
						objNewNode = objXmlDocument.SelectSingleNode("/chummer/gears/gear[name = \"Microdrone Sensor\" and category = \"Sensors\"]");
					objNewSensor.Create(objNewNode, _objCharacter, objTreeNode, 0, lstWeapons, lstWeaponNodes);
					objSensor = objCurrentGear;
					blnFound = true;
					break;
				}
				else if (objCurrentGear.Name == "Small Drone Sensor")
				{
					if (blnIncrease)
						objNewNode = objXmlDocument.SelectSingleNode("/chummer/gears/gear[name = \"Medium Drone Sensor\" and category = \"Sensors\"]");
					else
						objNewNode = objXmlDocument.SelectSingleNode("/chummer/gears/gear[name = \"Minidrone Sensor\" and category = \"Sensors\"]");
					objNewSensor.Create(objNewNode, _objCharacter, objTreeNode, 0, lstWeapons, lstWeaponNodes);
					objSensor = objCurrentGear;
					blnFound = true;
					break;
				}
				else if (objCurrentGear.Name == "Medium Drone Sensor")
				{
					if (blnIncrease)
						objNewNode = objXmlDocument.SelectSingleNode("/chummer/gears/gear[name = \"Large Drone Sensor\" and category = \"Sensors\"]");
					else
						objNewNode = objXmlDocument.SelectSingleNode("/chummer/gears/gear[name = \"Small Drone Sensor\" and category = \"Sensors\"]");
					objNewSensor.Create(objNewNode, _objCharacter, objTreeNode, 0, lstWeapons, lstWeaponNodes);
					objSensor = objCurrentGear;
					blnFound = true;
					break;
				}
				else if (objCurrentGear.Name == "Large Drone Sensor")
				{
					if (blnIncrease)
						objNewNode = objXmlDocument.SelectSingleNode("/chummer/gears/gear[name = \"Vehicle Sensor\" and category = \"Sensors\"]");
					else
						objNewNode = objXmlDocument.SelectSingleNode("/chummer/gears/gear[name = \"Medium Drone Sensor\" and category = \"Sensors\"]");
					objNewSensor.Create(objNewNode, _objCharacter, objTreeNode, 0, lstWeapons, lstWeaponNodes);
					objSensor = objCurrentGear;
					blnFound = true;
					break;
				}
				else if (objCurrentGear.Name == "Vehicle Sensor")
				{
					if (blnIncrease)
						objNewNode = objXmlDocument.SelectSingleNode("/chummer/gears/gear[name = \"Extra-Large Vehicle Sensor\" and category = \"Sensors\"]");
					else
						objNewNode = objXmlDocument.SelectSingleNode("/chummer/gears/gear[name = \"Large Drone Sensor\" and category = \"Sensors\"]");
					objNewSensor.Create(objNewNode, _objCharacter, objTreeNode, 0, lstWeapons, lstWeaponNodes);
					objSensor = objCurrentGear;
					blnFound = true;
					break;
				}
				else if (objCurrentGear.Name == "Extra-Large Vehicle Sensor")
				{
					if (!blnIncrease)
					{
						objNewNode = objXmlDocument.SelectSingleNode("/chummer/gears/gear[name = \"Vehicle Sensor\" and category = \"Sensors\"]");
						objNewSensor.Create(objNewNode, _objCharacter, objTreeNode, 0, lstWeapons, lstWeaponNodes);
						objSensor = objCurrentGear;
						blnFound = true;
					}
					break;
				}
			}

			// If the item was found, update the Vehicle Sensor information.
			if (blnFound)
			{
				objSensor.Name = objNewSensor.Name;
				objSensor.Rating = objNewSensor.Rating;
				objSensor.Capacity = objNewSensor.Capacity;
				objSensor.Signal = objNewSensor.Signal;
				objSensor.Avail = objNewSensor.Avail;
				objSensor.Cost = objNewSensor.Cost;
				objSensor.Source = objNewSensor.Source;
				objSensor.Page = objNewSensor.Page;

				// Update the name of the item in the TreeView.
				TreeNode objNode = _objFunctions.FindNode(objSensor.InternalId, treVehicles);
				objNode.Text = objSensor.DisplayNameShort;
			}
		}

		/// <summary>
		/// Update the Reputation fields.
		/// </summary>
		private void UpdateReputation()
		{
			lblStreetCredTotal.Text = _objCharacter.CalculatedStreetCred.ToString();
			lblNotorietyTotal.Text = _objCharacter.CalculatedNotoriety.ToString();
			lblPublicAwareTotal.Text = _objCharacter.CalculatedPublicAwareness.ToString();

			tipTooltip.SetToolTip(lblStreetCredTotal, _objCharacter.StreetCredTooltip);
			tipTooltip.SetToolTip(lblNotorietyTotal, _objCharacter.NotorietyTooltip);
			tipTooltip.SetToolTip(lblPublicAwareTotal, _objCharacter.PublicAwarenessTooltip);
		}

		/// <summary>
		/// Change the Equipped status of a piece of Gear and all of its children.
		/// </summary>
		/// <param name="objGear">Gear object to change.</param>
		/// <param name="blnEquipped">Whether or not the Gear should be marked as Equipped.</param>
		private void ChangeGearEquippedStatus(Gear objGear, bool blnEquipped)
		{
			if (blnEquipped)
			{
				// Add any Improvements from the Gear.
				if (objGear.Bonus != null)
				{
					bool blnAddImprovement = true;
					// If this is a Focus which is not bonded, don't do anything.
					if (objGear.Category != "Stacked Focus")
					{
						if (objGear.Category.EndsWith("Foci"))
							blnAddImprovement = objGear.Bonded;

						if (blnAddImprovement)
						{
							if (objGear.Extra != string.Empty)
								_objImprovementManager.ForcedValue = objGear.Extra;
							_objImprovementManager.CreateImprovements(Improvement.ImprovementSource.Gear, objGear.InternalId, objGear.Bonus, false, objGear.Rating, objGear.DisplayNameShort);
						}
					}
					else
					{
						// Stacked Foci need to be handled a little differently.
						foreach (StackedFocus objStack in _objCharacter.StackedFoci)
						{
							if (objStack.GearId == objGear.InternalId)
							{
								if (objStack.Bonded)
								{
									foreach (Gear objFociGear in objStack.Gear)
									{
										if (objFociGear.Extra != string.Empty)
											_objImprovementManager.ForcedValue = objFociGear.Extra;
										_objImprovementManager.CreateImprovements(Improvement.ImprovementSource.StackedFocus, objStack.InternalId, objFociGear.Bonus, false, objFociGear.Rating, objFociGear.DisplayNameShort);
									}
								}
							}
						}
					}
				}
			}
			else
			{
				// Remove any Improvements from the Gear.
				if (objGear.Bonus != null)
				{
					if (objGear.Category != "Stacked Focus")
						_objImprovementManager.RemoveImprovements(Improvement.ImprovementSource.Gear, objGear.InternalId);
					else
					{
						// Stacked Foci need to be handled a little differetnly.
						foreach (StackedFocus objStack in _objCharacter.StackedFoci)
						{
							if (objStack.GearId == objGear.InternalId)
							{
								foreach (Gear objFociGear in objStack.Gear)
									_objImprovementManager.RemoveImprovements(Improvement.ImprovementSource.StackedFocus, objStack.InternalId);
							}
						}
					}
				}
			}

			if (objGear.Children.Count > 0)
				ChangeGearEquippedStatus(objGear.Children, blnEquipped);
		}

		/// <summary>
		/// Change the Equipped status of all Gear plugins. This should only be called from the other ChangeGearEquippedStatus and never used directly.
		/// </summary>
		/// <param name="lstGear">List of child Gear to change.</param>
		/// <param name="blnEquipped">Whether or not the children should be marked as Equipped.</param>
		private void ChangeGearEquippedStatus(List<Gear> lstGear, bool blnEquipped)
		{
			foreach (Gear objGear in lstGear)
			{
				ChangeGearEquippedStatus(objGear, blnEquipped);
			}
		}

		/// <summary>
		/// Enable/Disable the Paste Menu and ToolStrip items as appropriate.
		/// </summary>
		private void RefreshPasteStatus()
		{
			bool blnPasteEnabled = false;
			bool blnCopyEnabled = false;

			if (tabCharacterTabs.SelectedTab == tabStreetGear)
			{
				// Lifestyle Tab.
				if (tabStreetGearTabs.SelectedTab == tabLifestyle)
				{
					if (GlobalOptions.Instance.ClipboardContentType == ClipboardContentType.Lifestyle)
						blnPasteEnabled = true;

					try
					{
						foreach (Lifestyle objLifestyle in _objCharacter.Lifestyles)
						{
							if (objLifestyle.InternalId == treLifestyles.SelectedNode.Tag.ToString())
							{
								blnCopyEnabled = true;
								break;
							}
						}
					}
					catch
					{
					}
				}

				// Armor Tab.
				if (tabStreetGearTabs.SelectedTab == tabArmor)
				{
					if (GlobalOptions.Instance.ClipboardContentType == ClipboardContentType.Armor)
						blnPasteEnabled = true;
					if (GlobalOptions.Instance.ClipboardContentType == ClipboardContentType.Gear || GlobalOptions.Instance.ClipboardContentType == ClipboardContentType.Commlink || GlobalOptions.Instance.ClipboardContentType == ClipboardContentType.OperatingSystem)
					{
						// Gear can only be pasted into Armor, not Armor Mods.
						try
						{
							foreach (Armor objArmor in _objCharacter.Armor)
							{
								if (objArmor.InternalId == treArmor.SelectedNode.Tag.ToString())
								{
									blnPasteEnabled = true;
									break;
								}
							}
						}
						catch
						{
						}
					}

					try
					{
						foreach (Armor objArmor in _objCharacter.Armor)
						{
							if (objArmor.InternalId == treArmor.SelectedNode.Tag.ToString())
							{
								blnCopyEnabled = true;
								break;
							}
						}
					}
					catch
					{
					}
					try
					{
						Armor objArmor = new Armor(_objCharacter);
						Gear objGear = _objFunctions.FindArmorGear(treArmor.SelectedNode.Tag.ToString(), _objCharacter.Armor, out objArmor);
						if (objGear != null)
							blnCopyEnabled = true;
					}
					catch
					{
					}
				}

				// Weapons Tab.
				if (tabStreetGearTabs.SelectedTab == tabWeapons)
				{
					if (GlobalOptions.Instance.ClipboardContentType == ClipboardContentType.Weapon)
						blnPasteEnabled = true;
					if (GlobalOptions.Instance.ClipboardContentType == ClipboardContentType.Gear || GlobalOptions.Instance.ClipboardContentType == ClipboardContentType.Commlink || GlobalOptions.Instance.ClipboardContentType == ClipboardContentType.OperatingSystem)
					{
						// Check if the copied Gear can be pasted into the selected Weapon Accessory.
						Gear objGear = new Gear(_objCharacter);
						XmlNode objXmlNode = GlobalOptions.Instance.Clipboard.SelectSingleNode("/character/gear");
						if (objXmlNode != null)
						{
							switch (objXmlNode["category"].InnerText)
							{
								case "Commlink":
								case "Commlink Upgrade":
									Commlink objCommlink = new Commlink(_objCharacter);
									objCommlink.Load(objXmlNode, true);
									objGear = objCommlink;
									break;
								case "Commlink Operating System":
								case "Commlink Operating System Upgrade":
									OperatingSystem objOperatingSystem = new OperatingSystem(_objCharacter);
									objOperatingSystem.Load(objXmlNode, true);
									objGear = objOperatingSystem;
									break;
								default:
									Gear objNewGear = new Gear(_objCharacter);
									objNewGear.Load(objXmlNode, true);
									objGear = objNewGear;
									break;
							}

							objGear.Parent = null;

							// Make sure that a Weapon Accessory is selected and that it allows Gear of the item's Category.
							WeaponAccessory objAccessory = new WeaponAccessory(_objCharacter);
							try
							{
								foreach (Weapon objCharacterWeapon in _objCharacter.Weapons)
								{
									foreach (WeaponAccessory objWeaponAccessory in objCharacterWeapon.WeaponAccessories)
									{
										if (objWeaponAccessory.InternalId == treWeapons.SelectedNode.Tag.ToString())
										{
											objAccessory = objWeaponAccessory;
											break;
										}
									}
								}
								if (objAccessory.AllowGear != null)
								{
									foreach (XmlNode objAllowed in objAccessory.AllowGear.SelectNodes("gearcategory"))
									{
										if (objAllowed.InnerText == objGear.Category)
										{
											blnPasteEnabled = true;
											break;
										}
									}
								}
							}
							catch
							{
							}
						}
					}

					try
					{
						foreach (Weapon objWeapon in _objCharacter.Weapons)
						{
							if (objWeapon.InternalId == treWeapons.SelectedNode.Tag.ToString())
							{
								blnCopyEnabled = true;
								break;
							}
						}
					}
					catch
					{
					}
					try
					{
						WeaponAccessory objAccessory = new WeaponAccessory(_objCharacter);
						Gear objGear = _objFunctions.FindWeaponGear(treWeapons.SelectedNode.Tag.ToString(), _objCharacter.Weapons, out objAccessory);
						if (objGear != null)
							blnCopyEnabled = true;
					}
					catch
					{
					}
				}

				// Gear Tab.
				if (tabStreetGearTabs.SelectedTab == tabGear)
				{
					if (GlobalOptions.Instance.ClipboardContentType == ClipboardContentType.Gear || GlobalOptions.Instance.ClipboardContentType == ClipboardContentType.Commlink || GlobalOptions.Instance.ClipboardContentType == ClipboardContentType.OperatingSystem)
						blnPasteEnabled = true;

					try
					{
						Gear objGear = _objFunctions.FindGear(treGear.SelectedNode.Tag.ToString(), _objCharacter.Gear);
						if (objGear != null)
							blnCopyEnabled = true;
					}
					catch
					{
					}
				}
			}

			// Vehicles Tab.
			if (tabCharacterTabs.SelectedTab == tabVehicles)
			{
				if (GlobalOptions.Instance.ClipboardContentType == ClipboardContentType.Vehicle)
					blnPasteEnabled = true;
				if (GlobalOptions.Instance.ClipboardContentType == ClipboardContentType.Gear || GlobalOptions.Instance.ClipboardContentType == ClipboardContentType.Commlink || GlobalOptions.Instance.ClipboardContentType == ClipboardContentType.OperatingSystem)
				{
					// Gear can only be pasted into Vehicles and Vehicle Gear.
					try
					{
						foreach (Vehicle objVehicle in _objCharacter.Vehicles)
						{
							if (objVehicle.InternalId == treVehicles.SelectedNode.Tag.ToString())
							{
								blnPasteEnabled = true;
								break;
							}
						}
					}
					catch
					{
					}
					try
					{
						Vehicle objVehicle = new Vehicle(_objCharacter);
						Gear objGear = _objFunctions.FindVehicleGear(treVehicles.SelectedNode.Tag.ToString(), _objCharacter.Vehicles, out objVehicle);
						if (objGear != null)
							blnPasteEnabled = true;
					}
					catch
					{
					}
				}
				if (GlobalOptions.Instance.ClipboardContentType == ClipboardContentType.Weapon)
				{
					// Weapons can only be pasted into Vehicle Mods that allow them (Weapon Mounts and Mechanical Arms).
					try
					{
						VehicleMod objMod = new VehicleMod(_objCharacter);
						foreach (Vehicle objVehicle in _objCharacter.Vehicles)
						{
							foreach (VehicleMod objVehicleMod in objVehicle.Mods)
							{
								if (objVehicleMod.InternalId == treVehicles.SelectedNode.Tag.ToString())
								{
									if (objVehicleMod.Name.StartsWith("Weapon Mount") || objVehicleMod.Name.StartsWith("Mechanical Arm"))
									{
										blnPasteEnabled = true;
										break;
									}
								}
							}
						}
					}
					catch
					{
					}
				}

				try
				{
					foreach (Vehicle objVehicle in _objCharacter.Vehicles)
					{
						if (objVehicle.InternalId == treVehicles.SelectedNode.Tag.ToString())
						{
							blnCopyEnabled = true;
							break;
						}
					}
				}
				catch
				{
				}
				try
				{
					Vehicle objVehicle = new Vehicle(_objCharacter);
					Gear objGear = _objFunctions.FindVehicleGear(treVehicles.SelectedNode.Tag.ToString(), _objCharacter.Vehicles, out objVehicle);
					if (objGear != null)
						blnCopyEnabled = true;
				}
				catch
				{
				}
				try
				{
					foreach (Vehicle objVehicle in _objCharacter.Vehicles)
					{
						foreach (VehicleMod objMod in objVehicle.Mods)
						{
							foreach (Weapon objWeapon in objMod.Weapons)
							{
								if (objWeapon.InternalId == treVehicles.SelectedNode.Tag.ToString())
								{
									blnCopyEnabled = true;
									break;
								}
							}
						}
					}
				}
				catch
				{
				}
			}

			mnuEditPaste.Enabled = blnPasteEnabled;
			tsbPaste.Enabled = blnPasteEnabled;
			mnuEditCopy.Enabled = blnCopyEnabled;
			tsbCopy.Enabled = blnCopyEnabled;
		}

		private void AddCyberwareSuite(Improvement.ImprovementSource objSource)
		{
			frmSelectCyberwareSuite frmPickCyberwareSuite = new frmSelectCyberwareSuite(objSource, _objCharacter);
			frmPickCyberwareSuite.ShowDialog(this);

			if (frmPickCyberwareSuite.DialogResult == DialogResult.Cancel)
				return;

			string strType = "";
			int intParentNode = 0;
			if (objSource == Improvement.ImprovementSource.Cyberware)
			{
				strType = "cyberware";
				intParentNode = 0;
			}
			else
			{
				strType = "bioware";
				intParentNode = 1;
			}
			XmlDocument objXmlDocument = XmlManager.Instance.Load(strType + ".xml");

			XmlNode objXmlSuite = objXmlDocument.SelectSingleNode("/chummer/suites/suite[name = \"" + frmPickCyberwareSuite.SelectedSuite + "\"]");
			Cyberware objTemp = new Cyberware(_objCharacter);
			Grade objGrade = new Grade();
			objGrade = objTemp.ConvertToCyberwareGrade(objXmlSuite["grade"].InnerText, objSource);

			// Run through each of the items in the Suite and add them to the character.
			foreach (XmlNode objXmlItem in objXmlSuite.SelectNodes(strType + "s/" + strType))
			{
				XmlNode objXmlCyberware = objXmlDocument.SelectSingleNode("/chummer/" + strType + "s/" + strType + "[name = \"" + objXmlItem["name"].InnerText + "\"]");
				TreeNode objNode = new TreeNode();
				int intRating = 0;

				if (objXmlItem["rating"] != null)
					intRating = Convert.ToInt32(objXmlItem["rating"].InnerText);

				objNode = CreateSuiteCyberware(objXmlItem, objXmlCyberware, objGrade, intRating, true, objSource, strType, null);

				objNode.Expand();
				treCyberware.Nodes[intParentNode].Nodes.Add(objNode);
				treCyberware.Nodes[intParentNode].Expand();
			}

			_blnIsDirty = true;
			UpdateWindowTitle();
			UpdateCharacterInfo();
		}

		/// <summary>
		/// Add a piece of Gear that was found in a PACKS Kit.
		/// </summary>
		/// <param name="objXmlGearDocument">XmlDocument that contains the Gear.</param>
		/// <param name="objXmlGear">XmlNode of the Gear to add.</param>
		/// <param name="objParent">TreeNode to attach the created items to.</param>
		/// <param name="objParentObject">Object to associate the newly-created items with.</param>
		/// <param name="cmsContextMenu">ContextMenuStrip to assign to the TreeNodes created.</param>
		/// <param name="blnCreateChildren">Whether or not the default plugins for the Gear should be created.</param>
		private void AddPACKSGear(XmlDocument objXmlGearDocument, XmlNode objXmlGear, TreeNode objParent, Object objParentObject, ContextMenuStrip cmsContextMenu, bool blnCreateChildren)
		{
			int intRating = 0;
			if (objXmlGear["rating"] != null)
				intRating = Convert.ToInt32(objXmlGear["rating"].InnerText);
			int intQty = 1;
			if (objXmlGear["qty"] != null)
				intQty = Convert.ToInt32(objXmlGear["qty"].InnerText);

			XmlNode objXmlGearNode;
			if (objXmlGear["category"] != null)
				objXmlGearNode = objXmlGearDocument.SelectSingleNode("/chummer/gears/gear[name = \"" + objXmlGear["name"].InnerText + "\" and category = \"" + objXmlGear["category"].InnerText + "\"]");
			else
				objXmlGearNode = objXmlGearDocument.SelectSingleNode("/chummer/gears/gear[name = \"" + objXmlGear["name"].InnerText + "\"]");

			List<Weapon> objWeapons = new List<Weapon>();
			List<TreeNode> objWeaponNodes = new List<TreeNode>();
			TreeNode objNode = new TreeNode();
			string strForceValue = "";
			if (objXmlGear["name"].Attributes["select"] != null)
				strForceValue = objXmlGear["name"].Attributes["select"].InnerText;

			Gear objNewGear = new Gear(_objCharacter);
			switch (objXmlGearNode["category"].InnerText)
			{
				case "Commlink":
				case "Commlink Upgrade":
					Commlink objCommlink = new Commlink(_objCharacter);
					objCommlink.Create(objXmlGearNode, _objCharacter, objNode, intRating, true, blnCreateChildren);
					objCommlink.Quantity = intQty;
					objNewGear = objCommlink;
					break;
				case "Commlink Operating System":
				case "Commlink Operating System Upgrade":
					OperatingSystem objOperatingSystem = new OperatingSystem(_objCharacter);
					objOperatingSystem.Create(objXmlGearNode, _objCharacter, objNode, intRating, true, blnCreateChildren);
					objOperatingSystem.Quantity = intQty;
					objNewGear = objOperatingSystem;
					break;
				default:
					Gear objGear = new Gear(_objCharacter);
					objGear.Create(objXmlGearNode, _objCharacter, objNode, intRating, objWeapons, objWeaponNodes, strForceValue, false, false, true, blnCreateChildren);
					objGear.Quantity = intQty;
					objNode.Text = objGear.DisplayName;
					objNewGear = objGear;
					break;
			}

			if (objParentObject.GetType() == typeof(Character))
				((Character)objParentObject).Gear.Add(objNewGear);
			if (objParentObject.GetType() == typeof(Gear) || objParentObject.GetType() == typeof(Commlink) || objParentObject.GetType() == typeof(OperatingSystem))
			{
				((Gear)objParentObject).Children.Add(objNewGear);
				objNewGear.Parent = (Gear)objParentObject;
			}
			if (objParentObject.GetType() == typeof(Armor))
				((Armor)objParentObject).Gear.Add(objNewGear);
			if (objParentObject.GetType() == typeof(WeaponAccessory))
				((WeaponAccessory)objParentObject).Gear.Add(objNewGear);
			if (objParentObject.GetType() == typeof(Cyberware))
				((Cyberware)objParentObject).Gear.Add(objNewGear);

			// Look for child components.
			if (objXmlGear["gears"] != null)
			{
				foreach (XmlNode objXmlChild in objXmlGear.SelectNodes("gears/gear"))
				{
					AddPACKSGear(objXmlGearDocument, objXmlChild, objNode, objNewGear, cmsContextMenu, blnCreateChildren);
				}
			}

			objParent.Nodes.Add(objNode);
			objParent.Expand();

			objNode.ContextMenuStrip = cmsContextMenu;
			objNode.Text = objNewGear.DisplayName;

			// Add any Weapons created by the Gear.
			foreach (Weapon objWeapon in objWeapons)
				_objCharacter.Weapons.Add(objWeapon);

			foreach (TreeNode objWeaponNode in objWeaponNodes)
			{
				objWeaponNode.ContextMenuStrip = cmsWeapon;
				treWeapons.Nodes[0].Nodes.Add(objWeaponNode);
				treWeapons.Nodes[0].Expand();
			}

		}

		/// <summary>
		/// Populate the TreeView that contains all of the character's Gear.
		/// </summary>
		private void PopulateGearList()
		{
			// Populate Gear.
			// Create the root node.
			treGear.Nodes.Clear();
			TreeNode objRoot = new TreeNode();
			objRoot.Tag = "Node_SelectedGear";
			objRoot.Text = LanguageManager.Instance.GetString("Node_SelectedGear");
			treGear.Nodes.Add(objRoot);

			// Start by populating Locations.
			foreach (string strLocation in _objCharacter.Locations)
			{
				TreeNode objLocation = new TreeNode();
				objLocation.Tag = strLocation;
				objLocation.Text = strLocation;
				objLocation.ContextMenuStrip = cmsGearLocation;
				treGear.Nodes.Add(objLocation);
			}

			// Add Locations for the character's bits that can hold Commlinks.
			// Populate the list of Commlink Locations.
			foreach (Cyberware objCyberware in _objCharacter.Cyberware)
			{
				if (objCyberware.AllowGear != null)
				{
					if (objCyberware.AllowGear["gearcategory"] != null)
					{
						if (objCyberware.AllowGear["gearcategory"].InnerText == "Commlink")
						{
							TreeNode objNode = new TreeNode();
							objNode.Tag = objCyberware.InternalId.ToString();
							objNode.Text = objCyberware.DisplayCategory + ": " + objCyberware.DisplayName;
							treGear.Nodes.Add(objNode);
						}
					}
				}
				foreach (Cyberware objPlugin in objCyberware.Children)
				{
					if (objPlugin.AllowGear != null)
					{
						if (objPlugin.AllowGear["gearcategory"] != null)
						{
							TreeNode objNode = new TreeNode();
							objNode.Tag = objPlugin.InternalId.ToString();
							objNode.Text = objPlugin.DisplayCategory + ": " + objPlugin.DisplayName;
							treGear.Nodes.Add(objNode);
						}
					}
				}
			}
			foreach (Weapon objWeapon in _objCharacter.Weapons)
			{
				foreach (WeaponAccessory objAccessory in objWeapon.WeaponAccessories)
				{
					if (objAccessory.AllowGear != null)
					{
						if (objAccessory.AllowGear["gearcategory"] != null)
						{
							if (objAccessory.AllowGear["gearcategory"].InnerText == "Commlink")
							{
								TreeNode objNode = new TreeNode();
								objNode.Tag = objAccessory.InternalId.ToString();
								objNode.Text = objWeapon.DisplayName + ": " + objAccessory.DisplayName;
								treGear.Nodes.Add(objNode);
							}
						}
					}
				}
				foreach (Weapon objUnderbarrel in objWeapon.UnderbarrelWeapons)
				{
					foreach (WeaponAccessory objUnderbarrelAccessory in objUnderbarrel.WeaponAccessories)
					{
						if (objUnderbarrelAccessory.AllowGear != null)
						{
							if (objUnderbarrelAccessory.AllowGear["gearcategory"] != null)
							{
								if (objUnderbarrelAccessory.AllowGear["gearcategory"].InnerText == "Commlink")
								{
									TreeNode objNode = new TreeNode();
									objNode.Tag = objUnderbarrelAccessory.InternalId.ToString();
									objNode.Text = objUnderbarrel.DisplayName + ": " + objUnderbarrelAccessory.DisplayName;
									treGear.Nodes.Add(objNode);
								}
							}
						}
					}
				}
			}

			foreach (Gear objGear in _objCharacter.Gear)
			{
				bool blnAdd = true;
				if (chkCommlinks.Checked && objGear.Category != "Commlink")
					blnAdd = false;

				if (blnAdd)
				{
					TreeNode objNode = new TreeNode();
					objNode.Text = objGear.DisplayName;
					objNode.Tag = objGear.InternalId;
					if (objGear.Notes != string.Empty)
						objNode.ForeColor = Color.SaddleBrown;
					objNode.ToolTipText = objGear.Notes;

					_objFunctions.BuildGearTree(objGear, objNode, cmsGear);

					objNode.ContextMenuStrip = cmsGear;

					TreeNode objParent = new TreeNode();
					if (objGear.Location == "")
						objParent = treGear.Nodes[0];
					else
					{
						foreach (TreeNode objFind in treGear.Nodes)
						{
							if (objFind.Text == objGear.Location)
							{
								objParent = objFind;
								break;
							}
						}
					}
					objParent.Nodes.Add(objNode);
					objParent.Expand();
				}
			}
		}

		/// <summary>
		/// Populate the TreeView that contains all of the character's Cyberware and Bioware.
		/// </summary>
		private void PopulateCyberwareList()
		{
			// Populate Cyberware.
			foreach (Cyberware objCyberware in _objCharacter.Cyberware)
			{
				if (objCyberware.SourceType == Improvement.ImprovementSource.Cyberware)
				{
					_objFunctions.BuildCyberwareTree(objCyberware, treCyberware.Nodes[0], cmsCyberware, cmsCyberwareGear);
				}
			}

			// Populate Bioware.
			foreach (Cyberware objCyberware in _objCharacter.Cyberware)
			{
				if (objCyberware.SourceType == Improvement.ImprovementSource.Bioware)
				{
					_objFunctions.BuildCyberwareTree(objCyberware, treCyberware.Nodes[1], cmsBioware, cmsCyberwareGear);
				}
			}
		}

		/// <summary>
		/// Change the active Commlink for the Character.
		/// </summary>
		/// <param name="objActiveCommlink"></param>
		private void ChangeActiveCommlink(Commlink objActiveCommlink)
		{
			List<Commlink> lstCommlinks = _objFunctions.FindCharacterCommlinks(_objCharacter.Gear);

			foreach (Commlink objCommlink in lstCommlinks)
			{
				if (objCommlink.InternalId != objActiveCommlink.InternalId)
					objCommlink.IsActive = false;
			}
		}
		#endregion
	}
}