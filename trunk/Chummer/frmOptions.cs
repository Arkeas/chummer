using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Xml;
using Microsoft.Win32;

namespace Chummer
{
	public partial class frmOptions : Form
	{
		private readonly CharacterOptions _objOptions = new CharacterOptions();

		#region Form Events
		public frmOptions()
		{
			InitializeComponent();
			LanguageManager.Instance.Load(GlobalOptions.Instance.Language, this);

			// Populate the Build Method list.
			List<ListItem> lstBuildMethod = new List<ListItem>();
			ListItem objBP = new ListItem();
			objBP.Value = "BP";
			objBP.Name = LanguageManager.Instance.GetString("String_BP");

			ListItem objKarma = new ListItem();
			objKarma.Value = "Karma";
			objKarma.Name = LanguageManager.Instance.GetString("String_Karma");

			lstBuildMethod.Add(objBP);
			lstBuildMethod.Add(objKarma);
			cboBuildMethod.DataSource = lstBuildMethod;
			cboBuildMethod.ValueMember = "Value";
			cboBuildMethod.DisplayMember = "Name";

			// Populate the Essence Decimals list.
			List<ListItem> lstDecimals = new List<ListItem>();
			ListItem objTwo = new ListItem();
			objTwo.Value = "2";
			objTwo.Name = "2";

			ListItem objFour = new ListItem();
			objFour.Value = "4";
			objFour.Name = "4";

			lstDecimals.Add(objTwo);
			lstDecimals.Add(objFour);
			cboEssenceDecimals.DataSource = lstDecimals;
			cboEssenceDecimals.ValueMember = "Value";
			cboEssenceDecimals.DisplayMember = "Name";

			// Populate the Limb Count list.
			List<ListItem> lstLimbCount = new List<ListItem>();
			ListItem objLimbCount6 = new ListItem();
			objLimbCount6.Value = "all";
			objLimbCount6.Name = LanguageManager.Instance.GetString("String_LimbCount6");

			ListItem objLimbCount5Torso = new ListItem();
			objLimbCount5Torso.Value = "torso";
			objLimbCount5Torso.Name = LanguageManager.Instance.GetString("String_LimbCount5Torso");

			ListItem objLimbCount5Skull = new ListItem();
			objLimbCount5Skull.Value = "skull";
			objLimbCount5Skull.Name = LanguageManager.Instance.GetString("String_LimbCount5Skull");

			lstLimbCount.Add(objLimbCount6);
			lstLimbCount.Add(objLimbCount5Torso);
			lstLimbCount.Add(objLimbCount5Skull);
			cboLimbCount.DataSource = lstLimbCount;
			cboLimbCount.ValueMember = "Value";
			cboLimbCount.DisplayMember = "Name";

			lblMetatypeCostsKarma.Left = chkMetatypeCostsKarma.Left + chkMetatypeCostsKarma.Width + 6;
			nudMetatypeCostsKarmaMultiplier.Left = lblMetatypeCostsKarma.Left + lblMetatypeCostsKarma.Width + 6;

			MoveControls();
		}

		private void frmOptions_Load(object sender, EventArgs e)
		{
			// Populate the list of Settings.
			List<ListItem> lstSettings = new List<ListItem>();
			foreach (string strFileName in Directory.GetFiles(Path.Combine(Application.StartupPath, "settings"), "*.xml"))
			{
				// Remove the path from the file name.
				string strSettingsFile = strFileName;
				strSettingsFile = strSettingsFile.Replace(Path.Combine(Application.StartupPath, "settings"), string.Empty);
				strSettingsFile = strSettingsFile.Replace(Path.DirectorySeparatorChar, ' ').Trim();

				// Load the file so we can get the Setting name.
				XmlDocument objXmlSetting = new XmlDocument();
				objXmlSetting.Load(strFileName);
				string strSettingsName = objXmlSetting.SelectSingleNode("/settings/name").InnerText;

				ListItem objItem = new ListItem();
				objItem.Value = strSettingsFile;
				objItem.Name = strSettingsName;

				lstSettings.Add(objItem);
			}
			cboSetting.DataSource = lstSettings;
			cboSetting.ValueMember = "Value";
			cboSetting.DisplayMember = "Name";
			cboSetting.SelectedIndex = 0;

			// Attempt to make default.xml the default one. If it could not be found in the list, select the first item instead.
			cboSetting.SelectedIndex = cboSetting.FindStringExact("Default Settings");
			if (cboSetting.SelectedIndex == -1)
				cboSetting.SelectedIndex = 0;
			
			bool blnAutomaticUpdates = false;
			try
			{
				blnAutomaticUpdates = GlobalOptions.Instance.AutomaticUpdate;
			}
			catch
			{
			}
			chkAutomaticUpdate.Checked = blnAutomaticUpdates;

			bool blnStartupFullscreen = false;
			try
			{
				blnStartupFullscreen = GlobalOptions.Instance.StartupFullscreen;
			}
			catch
			{
			}
			chkStartupFullscreen.Checked = blnStartupFullscreen;

			bool blnSingleDiceRoller = true;
			try
			{
				blnSingleDiceRoller = GlobalOptions.Instance.SingleDiceRoller;
			}
			catch
			{
			}
			chkSingleDiceRoller.Checked = blnSingleDiceRoller;

			// Populate the Language List.
			string strPath = Path.Combine(Application.StartupPath, "lang");
			List<ListItem> lstLanguages = new List<ListItem>();
			foreach (string strFile in Directory.GetFiles(strPath, "*.xml"))
			{
				XmlDocument objXmlDocument = new XmlDocument();
				try
				{
					objXmlDocument.Load(strFile);
					ListItem objItem = new ListItem();
					string strFileName = strFile.Replace(strPath, string.Empty).Replace(".xml", string.Empty).Replace(Path.DirectorySeparatorChar, ' ').Trim();
					objItem.Value = strFileName;
					objItem.Name = objXmlDocument.SelectSingleNode("/chummer/name").InnerText;
					lstLanguages.Add(objItem);
				}
				catch
				{
				}
			}

			SortListItem objSort = new SortListItem();
			lstLanguages.Sort(objSort.Compare);
			cboLanguage.DataSource = lstLanguages;
			cboLanguage.ValueMember = "Value";
			cboLanguage.DisplayMember = "Name";
			try
			{
				cboLanguage.SelectedValue = GlobalOptions.Instance.Language;
			}
			catch
			{
			}
			if (cboLanguage.SelectedIndex == -1)
				cboLanguage.SelectedValue = "en-us";

			List<ListItem> lstFiles = new List<ListItem>();
			// Populate the XSLT list with all of the XSL files found in the sheets directory.
			foreach (string strFile in Directory.GetFiles(Application.StartupPath + Path.DirectorySeparatorChar + "sheets"))
			{
				// Only show files that end in .xsl. Do not include files that end in .xslt since they are used as "hidden" reference sheets (hidden because they are partial templates that cannot be used on their own).
				if (!strFile.EndsWith(".xslt") && strFile.EndsWith(".xsl"))
				{
					string strFileName = strFile.Replace(Application.StartupPath + Path.DirectorySeparatorChar + "sheets" + Path.DirectorySeparatorChar, string.Empty).Replace(".xsl", string.Empty);
					ListItem objItem = new ListItem();
					objItem.Value = strFileName;
					objItem.Name = strFileName;
					lstFiles.Add(objItem);

					//cboXSLT.Items.Add(strFileName);
				}
			}

			try
			{
				// Populate the XSL list with all of the XSL files found in the sheets\[language] directory.
				if (GlobalOptions.Instance.Language != "en-us")
				{
					XmlDocument objLanguageDocument = LanguageManager.Instance.XmlDoc;
					string strLanguage = objLanguageDocument.SelectSingleNode("/chummer/name").InnerText;

					foreach (string strFile in Directory.GetFiles(Application.StartupPath + Path.DirectorySeparatorChar + "sheets" + Path.DirectorySeparatorChar + GlobalOptions.Instance.Language))
					{
						// Only show files that end in .xsl. Do not include files that end in .xslt since they are used as "hidden" reference sheets (hidden because they are partial templates that cannot be used on their own).
						if (!strFile.EndsWith(".xslt") && strFile.EndsWith(".xsl"))
						{
							string strFileName = strFile.Replace(Application.StartupPath + Path.DirectorySeparatorChar + "sheets" + Path.DirectorySeparatorChar + GlobalOptions.Instance.Language + Path.DirectorySeparatorChar, string.Empty).Replace(".xsl", string.Empty);
							ListItem objItem = new ListItem();
							objItem.Value = GlobalOptions.Instance.Language + Path.DirectorySeparatorChar + strFileName;
							objItem.Name = strLanguage + ": " + strFileName;
							lstFiles.Add(objItem);
						}
					}
				}
			}
			catch
			{
			}

			try
			{
				// Populate the XSLT list with all of the XSL files found in the sheets\omae directory.
				foreach (string strFile in Directory.GetFiles(Application.StartupPath + Path.DirectorySeparatorChar + "sheets" + Path.DirectorySeparatorChar + "omae"))
				{
					// Only show files that end in .xsl. Do not include files that end in .xslt since they are used as "hidden" reference sheets (hidden because they are partial templates that cannot be used on their own).
					if (!strFile.EndsWith(".xslt") && strFile.EndsWith(".xsl"))
					{
						string strFileName = strFile.Replace(Application.StartupPath + Path.DirectorySeparatorChar + "sheets" + Path.DirectorySeparatorChar + "omae" + Path.DirectorySeparatorChar, string.Empty).Replace(".xsl", string.Empty);
						ListItem objItem = new ListItem();
						objItem.Value = "omae" + Path.DirectorySeparatorChar + strFileName;
						objItem.Name = LanguageManager.Instance.GetString("Menu_Main_Omae") + ": " + strFileName;
						lstFiles.Add(objItem);
					}
				}
			}
			catch
			{
			}

			cboXSLT.ValueMember = "Value";
			cboXSLT.DisplayMember = "Name";
			cboXSLT.DataSource = lstFiles;

			cboXSLT.SelectedValue = GlobalOptions.Instance.DefaultCharacterSheet;
		}
		#endregion

		#region Control Events
		private void cmdOK_Click(object sender, EventArgs e)
		{
			// Make sure the current Setting has a name.
			if (txtSettingName.Text.Trim() == "")
			{
				MessageBox.Show("You must give your Settings a name.", "Chummer Settings", MessageBoxButtons.OK, MessageBoxIcon.Information);
				txtSettingName.Focus();
				return;
			}

			// Set Registry values.
			GlobalOptions.Instance.AutomaticUpdate = chkAutomaticUpdate.Checked;
			GlobalOptions.Instance.Language = cboLanguage.SelectedValue.ToString();
			GlobalOptions.Instance.StartupFullscreen = chkStartupFullscreen.Checked;
			GlobalOptions.Instance.SingleDiceRoller = chkSingleDiceRoller.Checked;
			GlobalOptions.Instance.DefaultCharacterSheet = cboXSLT.SelectedValue.ToString();
			RegistryKey objRegistry = Registry.CurrentUser.CreateSubKey("Software\\Chummer");
			objRegistry.SetValue("autoupdate", chkAutomaticUpdate.Checked.ToString());
			objRegistry.SetValue("language", cboLanguage.SelectedValue.ToString());
			objRegistry.SetValue("startupfullscreen", chkStartupFullscreen.Checked.ToString());
			objRegistry.SetValue("singlediceroller", chkSingleDiceRoller.Checked.ToString());

			// Set Settings file value.
			// Build the list of Books (SR4 is always included as it's required for everything).
			_objOptions.Books.Clear();
			_objOptions.Books.Add("SR4");

			foreach (TreeNode objNode in treSourcebook.Nodes)
			{
				if (objNode.Checked)
					_objOptions.Books.Add(objNode.Tag.ToString());
			}

			_objOptions.ConfirmDelete = chkConfirmDelete.Checked;
			_objOptions.ConfirmKarmaExpense = chkConfirmKarmaExpense.Checked;
			_objOptions.PrintSkillsWithZeroRating = chkPrintSkillsWithZeroRating.Checked;
			_objOptions.MoreLethalGameplay = chkMoreLethalGameplay.Checked;
			_objOptions.SpiritForceBasedOnTotalMAG = chkSpiritForceBasedOnTotalMAG.Checked;
			_objOptions.SkillDefaultingIncludesModifiers = chkSkillDefaultingIncludesModifiers.Checked;
			_objOptions.EnforceMaximumSkillRatingModifier = chkEnforceSkillMaximumModifiedRating.Checked;
			_objOptions.CapSkillRating = chkCapSkillRating.Checked;
			_objOptions.PrintExpenses = chkPrintExpenses.Checked;
			_objOptions.FreeContacts = chkFreeKarmaContacts.Checked;
			_objOptions.FreeContactsMultiplier = Convert.ToInt32(nudFreeKarmaContactsMultiplier.Value);
			_objOptions.FreeContactsFlat = chkFreeContactsFlat.Checked;
			_objOptions.FreeContactsFlatNumber = Convert.ToInt32(nudFreeContactsFlatNumber.Value);
			_objOptions.FreeKarmaKnowledge = chkFreeKarmaKnowledge.Checked;
			_objOptions.NuyenPerBP = Convert.ToInt32(nudNuyenPerBP.Value);
			_objOptions.EssenceDecimals = Convert.ToInt32(cboEssenceDecimals.SelectedValue);
			_objOptions.NoSingleArmorEncumbrance = chkNoSingleArmorEncumbrance.Checked;
			_objOptions.IgnoreArmorEncumbrance = chkIgnoreArmorEncumbrance.Checked;
			_objOptions.AlternateArmorEncumbrance = chkAlternateArmorEncumbrance.Checked;
			_objOptions.AllowCyberwareESSDiscounts = chkAllowCyberwareESSDiscounts.Checked;
			_objOptions.ESSLossReducesMaximumOnly = chkESSLossReducesMaximumOnly.Checked;
			_objOptions.AllowSkillRegrouping = chkAllowSkillRegrouping.Checked;
			_objOptions.MetatypeCostsKarma = chkMetatypeCostsKarma.Checked;
			_objOptions.MetatypeCostsKarmaMultiplier = Convert.ToInt32(nudMetatypeCostsKarmaMultiplier.Value);
			_objOptions.StrengthAffectsRecoil = chkStrengthAffectsRecoil.Checked;
			_objOptions.MaximumArmorModifications = chkMaximumArmorModifications.Checked;
			_objOptions.ArmorSuitCapacity = chkArmorSuitCapacity.Checked;
			_objOptions.ArmorDegradation = chkArmorDegradation.Checked;
			_objOptions.AutomaticCopyProtection = chkAutomaticCopyProtection.Checked;
			_objOptions.AutomaticRegistration = chkAutomaticRegistration.Checked;
			_objOptions.EnforceCapacity = chkEnforceCapacity.Checked;
			_objOptions.ExceedNegativeQualities = chkExceedNegativeQualities.Checked;
			_objOptions.ExceedNegativeQualitiesLimit = chkExceedNegativeQualitiesLimit.Checked;
			_objOptions.ExceedPositiveQualities = chkExceedPositiveQualities.Checked;
			_objOptions.UseCalculatedVehicleSensorRatings = chkUseCalculatedVehicleSensorRatings.Checked;
			_objOptions.AlternateMatrixAttribute = chkAlternateMatrixAttribute.Checked;
			_objOptions.AlternateComplexFormCost = chkAlternateComplexFormCost.Checked;
			_objOptions.AllowCustomTransgenics = chkAllowCustomTransgenics.Checked;
			_objOptions.RestrictRecoil = chkRestrictRecoil.Checked;
			_objOptions.MultiplyRestrictedCost = chkMultiplyRestrictedCost.Checked;
			_objOptions.MultiplyForbiddenCost = chkMultiplyForbiddenCost.Checked;
			_objOptions.RestrictedCostMultiplier = Convert.ToInt32(nudRestrictedCostMultiplier.Value);
			_objOptions.ForbiddenCostMultiplier = Convert.ToInt32(nudForbiddenCostMultiplier.Value);
			_objOptions.AllowExceedAttributeBP = chkAllowExceedAttributeBP.Checked;
			_objOptions.UnrestrictedNuyen = chkUnrestrictedNuyen.Checked;
			_objOptions.CalculateCommlinkResponse = chkCalculateCommlinkResponse.Checked;
			_objOptions.ErgonomicProgramLimit = chkErgonomicProgramLimit.Checked;
			_objOptions.AllowHigherStackedFoci = chkAllowHigherStackedFoci.Checked;
			_objOptions.AllowEditPartOfBaseWeapon = chkAllowEditPartOfBaseWeapon.Checked;
			_objOptions.AlternateMetatypeAttributeKarma = chkAlternateMetatypeAttributeKarma.Checked;
			_objOptions.AllowObsolescentUpgrade = chkAllowObsolescentUpgrade.Checked;
			_objOptions.AllowBiowareSuites = chkAllowBiowareSuites.Checked;
			_objOptions.FreeSpiritPowerPointsMAG = chkFreeSpiritsPowerPointsMAG.Checked;
			_objOptions.SpecialAttributeKarmaLimit = chkSpecialAttributeKarmaLimit.Checked;
			_objOptions.TechnomancerAllowAutosoft = chkTechnomancerAllowAutosoft.Checked;
			_objOptions.AllowSkillDiceRolling = chkAllowSkillDiceRolling.Checked;
			_objOptions.CreateBackupOnCareer = chkCreateBackupOnCareer.Checked;
			_objOptions.PrintLeadershipAlternates = chkPrintLeadershipAlternates.Checked;
			_objOptions.PrintArcanaAlternates = chkPrintArcanaAlternates.Checked;
			_objOptions.PrintNotes = chkPrintNotes.Checked;
			switch (cboLimbCount.SelectedValue.ToString())
			{
				case "torso":
					_objOptions.LimbCount = 5;
					_objOptions.ExcludeLimbSlot = "skull";
					break;
				case "skull":
					_objOptions.LimbCount = 5;
					_objOptions.ExcludeLimbSlot = "torso";
					break;
				default:
					_objOptions.LimbCount = 6;
					_objOptions.ExcludeLimbSlot = "";
					break;
			}

			// BP options.
			_objOptions.BPAttribute = Convert.ToInt32(nudBPAttribute.Value);
			_objOptions.BPAttributeMax = Convert.ToInt32(nudBPAttributeMax.Value);
			_objOptions.BPContact = Convert.ToInt32(nudBPContact.Value);
			_objOptions.BPMartialArt = Convert.ToInt32(nudBPMartialArt.Value);
			_objOptions.BPMartialArtManeuver = Convert.ToInt32(nudBPMartialArtManeuver.Value);
			_objOptions.BPSkillGroup = Convert.ToInt32(nudBPSkillGroup.Value);
			_objOptions.BPActiveSkill = Convert.ToInt32(nudBPActiveSkill.Value);
			_objOptions.BPActiveSkillSpecialization = Convert.ToInt32(nudBPSkillSpecialization.Value);
			_objOptions.BPKnowledgeSkill = Convert.ToInt32(nudBPKnowledgeSkill.Value);
			_objOptions.BPSpell = Convert.ToInt32(nudBPSpell.Value);
			_objOptions.BPFocus = Convert.ToInt32(nudBPFocus.Value);
			_objOptions.BPSpirit= Convert.ToInt32(nudBPSpirit.Value);
			_objOptions.BPComplexForm = Convert.ToInt32(nudBPComplexForm.Value);
			_objOptions.BPComplexFormOption = Convert.ToInt32(nudBPComplexFormOption.Value);

			// Karma options.
			_objOptions.KarmaAttribute = Convert.ToInt32(nudKarmaAttribute.Value);
			_objOptions.KarmaQuality = Convert.ToInt32(nudKarmaQuality.Value);
			_objOptions.KarmaSpecialization = Convert.ToInt32(nudKarmaSpecialization.Value);
			_objOptions.KarmaNewKnowledgeSkill = Convert.ToInt32(nudKarmaNewKnowledgeSkill.Value);
			_objOptions.KarmaNewActiveSkill = Convert.ToInt32(nudKarmaNewActiveSkill.Value);
			_objOptions.KarmaNewSkillGroup = Convert.ToInt32(nudKarmaNewSkillGroup.Value);
			_objOptions.KarmaImproveKnowledgeSkill = Convert.ToInt32(nudKarmaImproveKnowledgeSkill.Value);
			_objOptions.KarmaImproveActiveSkill = Convert.ToInt32(nudKarmaImproveActiveSkill.Value);
			_objOptions.KarmaImproveSkillGroup = Convert.ToInt32(nudKarmaImproveSkillGroup.Value);
			_objOptions.KarmaSpell = Convert.ToInt32(nudKarmaSpell.Value);
			_objOptions.KarmaNewComplexForm = Convert.ToInt32(nudKarmaNewComplexForm.Value);
			_objOptions.KarmaImproveComplexForm = Convert.ToInt32(nudKarmaImproveComplexForm.Value);
			_objOptions.KarmaMetamagic = Convert.ToInt32(nudKarmaMetamagic.Value);
			_objOptions.KarmaNuyenPer = Convert.ToInt32(nudKarmaNuyenPer.Value);
			_objOptions.KarmaContact = Convert.ToInt32(nudKarmaContact.Value);
			_objOptions.KarmaCarryover = Convert.ToInt32(nudKarmaCarryover.Value);
			_objOptions.KarmaSpirit = Convert.ToInt32(nudKarmaSpirit.Value);
			_objOptions.KarmaManeuver = Convert.ToInt32(nudKarmaManeuver.Value);
			_objOptions.KarmaInitiation = Convert.ToInt32(nudKarmaInitiation.Value);
			_objOptions.KarmaComplexFormOption = Convert.ToInt32(nudKarmaComplexFormOption.Value);
			_objOptions.KarmaComplexFormSkillsoft = Convert.ToInt32(nudKarmaComplexFormSkillsoft.Value);
			_objOptions.KarmaJoinGroup = Convert.ToInt32(nudKarmaJoinGroup.Value);
			_objOptions.KarmaLeaveGroup = Convert.ToInt32(nudKarmaLeaveGroup.Value);

			// Foci options.
			_objOptions.KarmaAnchoringFocus = Convert.ToInt32(nudKarmaAnchoringFocus.Value);
			_objOptions.KarmaBanishingFocus = Convert.ToInt32(nudKarmaBanishingFocus.Value);
			_objOptions.KarmaBindingFocus = Convert.ToInt32(nudKarmaBindingFocus.Value);
			_objOptions.KarmaCenteringFocus = Convert.ToInt32(nudKarmaCenteringFocus.Value);
			_objOptions.KarmaCounterspellingFocus = Convert.ToInt32(nudKarmaCounterspellingFocus.Value);
			_objOptions.KarmaDiviningFocus = Convert.ToInt32(nudKarmaDiviningFocus.Value);
			_objOptions.KarmaDowsingFocus = Convert.ToInt32(nudKarmaDowsingFocus.Value);
			_objOptions.KarmaInfusionFocus = Convert.ToInt32(nudKarmaInfusionFocus.Value);
			_objOptions.KarmaMaskingFocus = Convert.ToInt32(nudKarmaMaskingFocus.Value);
			_objOptions.KarmaPowerFocus = Convert.ToInt32(nudKarmaPowerFocus.Value);
			_objOptions.KarmaShieldingFocus = Convert.ToInt32(nudKarmaShieldingFocus.Value);
			_objOptions.KarmaSpellcastingFocus = Convert.ToInt32(nudKarmaSpellcastingFocus.Value);
			_objOptions.KarmaSummoningFocus = Convert.ToInt32(nudKarmaSummoningFocus.Value);
			_objOptions.KarmaSustainingFocus = Convert.ToInt32(nudKarmaSustainingFocus.Value);
			_objOptions.KarmaSymbolicLinkFocus = Convert.ToInt32(nudKarmaSymbolicLinkFocus.Value);
			_objOptions.KarmaWeaponFocus = Convert.ToInt32(nudKarmaWeaponFocus.Value);

			// Build method options.
			_objOptions.BuildMethod = cboBuildMethod.SelectedValue.ToString();
			_objOptions.BuildPoints = Convert.ToInt32(nudBP.Value);
			_objOptions.Availability = Convert.ToInt32(nudMaxAvail.Value);

			_objOptions.Name = txtSettingName.Text;

			_objOptions.Save();

			this.DialogResult = DialogResult.OK;
		}

		private void cboBuildMethod_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (cboBuildMethod.SelectedValue.ToString() == "BP")
				nudBP.Value = 400;
			else
				nudBP.Value = 750;
		}

		private void cboSetting_SelectedIndexChanged(object sender, EventArgs e)
		{
			ListItem objItem = (ListItem)cboSetting.SelectedItem;
			if (!objItem.Value.Contains(".xml"))
				return;

			_objOptions.Load(objItem.Value);
			PopulateOptions();
		}

		private void cboLanguage_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (cboLanguage.SelectedValue.ToString() == "en-us")
			{
				cmdVerify.Enabled = false;
				cmdVerifyData.Enabled = false;
			}
			else
			{
				cmdVerify.Enabled = true;
				cmdVerifyData.Enabled = true;
			}
		}

		private void cmdVerify_Click(object sender, EventArgs e)
		{
			LanguageManager.Instance.VerifyStrings(cboLanguage.SelectedValue.ToString());
		}

		private void cmdVerifyData_Click(object sender, EventArgs e)
		{
			// Build a list of Sourcebooks that will be passed to the Verify method.
			// This is done since not all of the books are available in every language or the user may only wish to verify the content of certain books.
			List<string> lstBooks = new List<string>();
			lstBooks.Add("SR4");
			foreach (TreeNode objNode in treSourcebook.Nodes)
			{
				if (objNode.Checked)
					lstBooks.Add(objNode.Tag.ToString());
			}

			XmlManager.Instance.Verify(cboLanguage.SelectedValue.ToString(), lstBooks);

			string strFilePath = Path.Combine(Application.StartupPath, "lang");
			strFilePath = Path.Combine(strFilePath, "results_" + cboLanguage.SelectedValue + ".xml");
			MessageBox.Show("Results were written to " + strFilePath, "Validation Results", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void chkExceedNegativeQualities_CheckedChanged(object sender, EventArgs e)
		{
			chkExceedNegativeQualitiesLimit.Enabled = chkExceedNegativeQualities.Checked;
			if (!chkExceedNegativeQualitiesLimit.Enabled)
				chkExceedNegativeQualitiesLimit.Checked = false;
		}

		private void cmdRestoreDefaultsBP_Click(object sender, EventArgs e)
		{
			// Verify that the user wants to reset these values.
			if (MessageBox.Show(LanguageManager.Instance.GetString("Message_Options_RestoreDefaults"), LanguageManager.Instance.GetString("MessageTitle_Options_RestoreDefaults"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
				return;

			// Restore the default BP values.
			nudBPAttribute.Value = 10;
			nudBPAttributeMax.Value = 15;
			nudBPContact.Value = 1;
			nudBPMartialArt.Value = 5;
			nudBPMartialArtManeuver.Value = 2;
			nudBPSkillGroup.Value = 10;
			nudBPActiveSkill.Value = 4;
			nudBPSkillSpecialization.Value = 2;
			nudBPKnowledgeSkill.Value = 2;
			nudBPSpell.Value = 3;
			nudBPFocus.Value = 1;
			nudBPSpirit.Value = 1;
			nudBPComplexForm.Value = 1;
			nudBPComplexFormOption.Value = 1;
		}

		private void cmdRestoreDefaultsKarma_Click(object sender, EventArgs e)
		{
			// Verify that the user wants to reset these values.
			if (MessageBox.Show(LanguageManager.Instance.GetString("Message_Options_RestoreDefaults"), LanguageManager.Instance.GetString("MessageTitle_Options_RestoreDefaults"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
				return;

			// Restore the default Karma values.
			nudKarmaAttribute.Value = 5;
			nudKarmaQuality.Value = 2;
			nudKarmaSpecialization.Value = 2;
			nudKarmaNewKnowledgeSkill.Value = 2;
			nudKarmaNewActiveSkill.Value = 4;
			nudKarmaNewSkillGroup.Value = 10;
			nudKarmaImproveKnowledgeSkill.Value = 1;
			nudKarmaImproveActiveSkill.Value = 2;
			nudKarmaImproveSkillGroup.Value = 5;
			nudKarmaSpell.Value = 5;
			nudKarmaNewComplexForm.Value = 2;
			nudKarmaImproveComplexForm.Value = 1;
			nudKarmaComplexFormOption.Value = 2;
			nudKarmaComplexFormSkillsoft.Value = 1;
			nudKarmaNuyenPer.Value = 2500;
			nudKarmaContact.Value = 2;
			nudKarmaCarryover.Value = 5;
			nudKarmaSpirit.Value = 2;
			nudKarmaManeuver.Value = 4;
			nudKarmaInitiation.Value = 3;
			nudKarmaMetamagic.Value = 15;
			nudKarmaJoinGroup.Value = 5;
			nudKarmaLeaveGroup.Value = 1;

			// Restore the default Karma Foci values.
			nudKarmaAnchoringFocus.Value = 6;
			nudKarmaBanishingFocus.Value = 3;
			nudKarmaBindingFocus.Value = 3;
			nudKarmaCenteringFocus.Value = 6;
			nudKarmaCounterspellingFocus.Value = 3;
			nudKarmaDiviningFocus.Value = 6;
			nudKarmaDowsingFocus.Value = 6;
			nudKarmaInfusionFocus.Value = 3;
			nudKarmaMaskingFocus.Value = 6;
			nudKarmaPowerFocus.Value = 6;
			nudKarmaShieldingFocus.Value = 6;
			nudKarmaSpellcastingFocus.Value = 4;
			nudKarmaSummoningFocus.Value = 4;
			nudKarmaSustainingFocus.Value = 2;
			nudKarmaSymbolicLinkFocus.Value = 1;
			nudKarmaWeaponFocus.Value = 3;
		}
		#endregion

		#region Methods
		private void MoveControls()
		{
			cboSetting.Left = lblSetting.Left + lblSetting.Width + 6;
			lblSettingName.Left = cboSetting.Left + cboSetting.Width + 6;
			txtSettingName.Left = lblSettingName.Left + lblSettingName.Width + 6;
			cboLimbCount.Left = lblLimbCount.Left + lblLimbCount.Width + 6;
			nudNuyenPerBP.Left = lblNuyenPerBP.Left + lblNuyenPerBP.Width + 6;
			lblMetatypeCostsKarma.Left = chkMetatypeCostsKarma.Left + chkMetatypeCostsKarma.Width;
			nudMetatypeCostsKarmaMultiplier.Left = lblMetatypeCostsKarma.Left + lblMetatypeCostsKarma.Width;
			nudFreeKarmaContactsMultiplier.Left = chkFreeKarmaContacts.Left + chkFreeKarmaContacts.Width;
			nudFreeContactsFlatNumber.Left = chkFreeContactsFlat.Left + chkFreeContactsFlat.Width;
			nudRestrictedCostMultiplier.Left = chkMultiplyRestrictedCost.Left + chkMultiplyRestrictedCost.Width;
			nudForbiddenCostMultiplier.Left = chkMultiplyForbiddenCost.Left + chkMultiplyForbiddenCost.Width;
			cboEssenceDecimals.Left = lblEssenceDecimals.Left + lblEssenceDecimals.Width + 6;

			int intWidth = Math.Max(lblLanguage.Width, lblXSLT.Width);
			cboLanguage.Left = lblLanguage.Left + intWidth + 6;
			cmdVerify.Left = cboLanguage.Left + cboLanguage.Width + 6;
			cmdVerifyData.Left = cmdVerify.Left + cmdVerify.Width + 6;
			cboXSLT.Left = lblXSLT.Left + intWidth + 6;

			// BP fields.
			intWidth = Math.Max(lblBPAttribute.Width, lblBPAttributeMax.Width);
			intWidth = Math.Max(intWidth, lblBPContact.Width);
			intWidth = Math.Max(intWidth, lblBPMartialArt.Width);
			intWidth = Math.Max(intWidth, lblBPMartialArtManeuver.Width);
			intWidth = Math.Max(intWidth, lblBPSkillGroup.Width);
			intWidth = Math.Max(intWidth, lblBPActiveSkill.Width);
			intWidth = Math.Max(intWidth, lblBPSkillSpecialization.Width);
			intWidth = Math.Max(intWidth, lblBPKnowledgeSkill.Width);
			intWidth = Math.Max(intWidth, lblBPSpell.Width);
			intWidth = Math.Max(intWidth, lblBPFocus.Width);
			intWidth = Math.Max(intWidth, lblBPSpirit.Width);
			intWidth = Math.Max(intWidth, lblBPComplexForm.Width);
			intWidth = Math.Max(intWidth, lblBPComplexFormOption.Width);

			nudBPAttribute.Left = lblBPAttribute.Left + intWidth + 6;
			nudBPAttributeMax.Left = nudBPAttribute.Left;
			nudBPContact.Left = nudBPAttribute.Left;
			nudBPMartialArt.Left = nudBPAttribute.Left;
			nudBPMartialArtManeuver.Left = nudBPAttribute.Left;
			nudBPSkillGroup.Left = nudBPAttribute.Left;
			nudBPActiveSkill.Left = nudBPAttribute.Left;
			nudBPSkillSpecialization.Left = nudBPAttribute.Left;
			nudBPKnowledgeSkill.Left = nudBPAttribute.Left;
			nudBPSpell.Left = nudBPAttribute.Left;
			nudBPFocus.Left = nudBPAttribute.Left;
			nudBPSpirit.Left = nudBPAttribute.Left;
			nudBPComplexForm.Left = nudBPAttribute.Left;
			nudBPComplexFormOption.Left = nudBPAttribute.Left;

			// Karma fields.
			nudKarmaSpecialization.Left = lblKarmaImproveKnowledgeSkill.Left + lblKarmaImproveKnowledgeSkill.Width + 6;
			nudKarmaNewKnowledgeSkill.Left = lblKarmaImproveKnowledgeSkill.Left + lblKarmaImproveKnowledgeSkill.Width + 6;
			nudKarmaNewActiveSkill.Left = lblKarmaImproveKnowledgeSkill.Left + lblKarmaImproveKnowledgeSkill.Width + 6;
			nudKarmaNewSkillGroup.Left = lblKarmaImproveKnowledgeSkill.Left + lblKarmaImproveKnowledgeSkill.Width + 6;
			nudKarmaImproveKnowledgeSkill.Left = lblKarmaImproveKnowledgeSkill.Left + lblKarmaImproveKnowledgeSkill.Width + 6;
			lblKarmaImproveKnowledgeSkillExtra.Left = nudKarmaImproveKnowledgeSkill.Left + nudKarmaImproveKnowledgeSkill.Width + 6;
			nudKarmaImproveActiveSkill.Left = lblKarmaImproveKnowledgeSkill.Left + lblKarmaImproveKnowledgeSkill.Width + 6;
			lblKarmaImproveActiveSkillExtra.Left = nudKarmaImproveActiveSkill.Left + nudKarmaImproveActiveSkill.Width + 6;
			nudKarmaImproveSkillGroup.Left = lblKarmaImproveKnowledgeSkill.Left + lblKarmaImproveKnowledgeSkill.Width + 6;
			lblKarmaImproveSkillGroupExtra.Left = nudKarmaImproveSkillGroup.Left + nudKarmaImproveSkillGroup.Width + 6;
			nudKarmaAttribute.Left = lblKarmaImproveKnowledgeSkill.Left + lblKarmaImproveKnowledgeSkill.Width + 6;
			lblKarmaAttributeExtra.Left = nudKarmaAttribute.Left + nudKarmaAttribute.Width + 6;
			nudKarmaQuality.Left = lblKarmaImproveKnowledgeSkill.Left + lblKarmaImproveKnowledgeSkill.Width + 6;
			lblKarmaQualityExtra.Left = nudKarmaQuality.Left + nudKarmaQuality.Width + 6;
			nudKarmaSpell.Left = lblKarmaImproveKnowledgeSkill.Left + lblKarmaImproveKnowledgeSkill.Width + 6;
			nudKarmaNewComplexForm.Left = lblKarmaImproveKnowledgeSkill.Left + lblKarmaImproveKnowledgeSkill.Width + 6;
			nudKarmaImproveComplexForm.Left = lblKarmaImproveKnowledgeSkill.Left + lblKarmaImproveKnowledgeSkill.Width + 6;
			lblKarmaImproveComplexFormExtra.Left = nudKarmaImproveComplexForm.Left + nudKarmaImproveComplexForm.Width + 6;
			nudKarmaComplexFormOption.Left = lblKarmaImproveKnowledgeSkill.Left + lblKarmaImproveKnowledgeSkill.Width + 6;
			lblKarmaComplexFormOptionExtra.Left = nudKarmaComplexFormOption.Left + nudKarmaComplexFormOption.Width + 6;
			nudKarmaComplexFormSkillsoft.Left = lblKarmaImproveKnowledgeSkill.Left + lblKarmaImproveKnowledgeSkill.Width + 6;
			lblKarmaComplexFormSkillsoftExtra.Left = nudKarmaComplexFormSkillsoft.Left + nudKarmaComplexFormSkillsoft.Width + 6;
			nudKarmaSpirit.Left = lblKarmaImproveKnowledgeSkill.Left + lblKarmaImproveKnowledgeSkill.Width + 6;
			lblKarmaSpiritExtra.Left = nudKarmaSpirit.Left + nudKarmaSpirit.Width + 6;
			nudKarmaManeuver.Left = lblKarmaImproveKnowledgeSkill.Left + lblKarmaImproveKnowledgeSkill.Width + 6;
			nudKarmaNuyenPer.Left = lblKarmaImproveKnowledgeSkill.Left + lblKarmaImproveKnowledgeSkill.Width + 6;
			lblKarmaNuyenPerExtra.Left = nudKarmaNuyenPer.Left + nudKarmaNuyenPer.Width + 6;
			nudKarmaContact.Left = lblKarmaImproveKnowledgeSkill.Left + lblKarmaImproveKnowledgeSkill.Width + 6;
			lblKarmaContactExtra.Left = nudKarmaContact.Left + nudKarmaContact.Width + 6;
			nudKarmaCarryover.Left = lblKarmaImproveKnowledgeSkill.Left + lblKarmaImproveKnowledgeSkill.Width + 6;
			lblKarmaCarryoverExtra.Left = nudKarmaCarryover.Left + nudKarmaCarryover.Width + 6;
			nudKarmaInitiation.Left = lblKarmaImproveKnowledgeSkill.Left + lblKarmaImproveKnowledgeSkill.Width + 6;
			lblKarmaInitiationBracket.Left = nudKarmaInitiation.Left - lblKarmaInitiationBracket.Width;
			lblKarmaInitiationExtra.Left = nudKarmaInitiation.Left + nudKarmaInitiation.Width + 6;

			intWidth = Math.Max(lblKarmaMetamagic.Width, lblKarmaJoinGroup.Width);
			intWidth = Math.Max(intWidth, lblKarmaLeaveGroup.Width);
			intWidth = Math.Max(intWidth, lblKarmaAnchoringFocus.Width);
			intWidth = Math.Max(intWidth, lblKarmaBanishingFocus.Width);
			intWidth = Math.Max(intWidth, lblKarmaBindingFocus.Width);
			intWidth = Math.Max(intWidth, lblKarmaCenteringFocus.Width);
			intWidth = Math.Max(intWidth, lblKarmaCounterspellingFocus.Width);
			intWidth = Math.Max(intWidth, lblKarmaDiviningFocus.Width);
			intWidth = Math.Max(intWidth, lblKarmaDowsingFocus.Width);
			intWidth = Math.Max(intWidth, lblKarmaInfusionFocus.Width);
			intWidth = Math.Max(intWidth, lblKarmaMaskingFocus.Width);
			intWidth = Math.Max(intWidth, lblKarmaPowerFocus.Width);
			intWidth = Math.Max(intWidth, lblKarmaShieldingFocus.Width);
			intWidth = Math.Max(intWidth, lblKarmaSpellcastingFocus.Width);
			intWidth = Math.Max(intWidth, lblKarmaSummoningFocus.Width);
			intWidth = Math.Max(intWidth, lblKarmaSustainingFocus.Width);
			intWidth = Math.Max(intWidth, lblKarmaSymbolicLinkFocus.Width);
			intWidth = Math.Max(intWidth, lblKarmaWeaponFocus.Width);

			nudKarmaMetamagic.Left = lblKarmaMetamagic.Left + intWidth + 6;
			nudKarmaJoinGroup.Left = nudKarmaMetamagic.Left;
			nudKarmaLeaveGroup.Left = nudKarmaMetamagic.Left;
			nudKarmaAnchoringFocus.Left = nudKarmaMetamagic.Left;
			nudKarmaBanishingFocus.Left = nudKarmaMetamagic.Left;
			nudKarmaBindingFocus.Left = nudKarmaMetamagic.Left;
			nudKarmaCenteringFocus.Left = nudKarmaMetamagic.Left;
			nudKarmaCounterspellingFocus.Left = nudKarmaMetamagic.Left;
			nudKarmaDiviningFocus.Left = nudKarmaMetamagic.Left;
			nudKarmaDowsingFocus.Left = nudKarmaMetamagic.Left;
			nudKarmaInfusionFocus.Left = nudKarmaMetamagic.Left;
			nudKarmaMaskingFocus.Left = nudKarmaMetamagic.Left;
			nudKarmaPowerFocus.Left = nudKarmaMetamagic.Left;
			nudKarmaShieldingFocus.Left = nudKarmaMetamagic.Left;
			nudKarmaSpellcastingFocus.Left = nudKarmaMetamagic.Left;
			nudKarmaSummoningFocus.Left = nudKarmaMetamagic.Left;
			nudKarmaSustainingFocus.Left = nudKarmaMetamagic.Left;
			nudKarmaSymbolicLinkFocus.Left = nudKarmaMetamagic.Left;
			nudKarmaWeaponFocus.Left = nudKarmaMetamagic.Left;

			lblKarmaAnchoringFocusExtra.Left = nudKarmaAnchoringFocus.Left + nudKarmaAnchoringFocus.Width + 6;
			lblKarmaBanishingFocusExtra.Left = lblKarmaAnchoringFocusExtra.Left;
			lblKarmaBindingFocusExtra.Left = lblKarmaAnchoringFocusExtra.Left;
			lblKarmaCenteringFocusExtra.Left = lblKarmaAnchoringFocusExtra.Left;
			lblKarmaCounterspellingFocusExtra.Left = lblKarmaAnchoringFocusExtra.Left;
			lblKarmaDiviningFocusExtra.Left = lblKarmaAnchoringFocusExtra.Left;
			lblKarmaDowsingFocusExtra.Left = lblKarmaAnchoringFocusExtra.Left;
			lblKarmaInfusionFocusExtra.Left = lblKarmaAnchoringFocusExtra.Left;
			lblKarmaMaskingFocusExtra.Left = lblKarmaAnchoringFocusExtra.Left;
			lblKarmaPowerFocusExtra.Left = lblKarmaAnchoringFocusExtra.Left;
			lblKarmaShieldingFocusExtra.Left = lblKarmaAnchoringFocusExtra.Left;
			lblKarmaSpellcastingFocusExtra.Left = lblKarmaAnchoringFocusExtra.Left;
			lblKarmaSummoningFocusExtra.Left = lblKarmaAnchoringFocusExtra.Left;
			lblKarmaSustainingFocusExtra.Left = lblKarmaAnchoringFocusExtra.Left;
			lblKarmaSymbolicLinkFocusExtra.Left = lblKarmaAnchoringFocusExtra.Left;
			lblKarmaWeaponFocusExtra.Left = lblKarmaAnchoringFocusExtra.Left;

			// Determine where the widest control ends so we can change the window with to accommodate it.
			foreach (Control objControl in tabGeneral.Controls)
				intWidth = Math.Max(intWidth, objControl.Left + objControl.Width);
			foreach (Control objControl in tabKarmaCosts.Controls)
				intWidth = Math.Max(intWidth, objControl.Left + objControl.Width);
			foreach (Control objControl in tabOptionalRules.Controls)
				intWidth = Math.Max(intWidth, objControl.Left + objControl.Width);
			foreach (Control objControl in tabHouseRules.Controls)
				intWidth = Math.Max(intWidth, objControl.Left + objControl.Width);

			// Change the window size.
			this.Width = intWidth + 29;
			this.Height = tabControl1.Top + tabControl1.Height + cmdOK.Height + 55;
			// Centre the OK button.
			cmdOK.Left = (this.Width / 2) - (cmdOK.Width / 2);
		}

		/// <summary>
		/// Set the values for all of the controls based on the Options for the selected Setting.
		/// </summary>
		private void PopulateOptions()
		{
			// Load the Sourcebook information.
			bool blnChecked = false;

			XmlDocument objXmlDocument = XmlManager.Instance.Load("books.xml");

			// Put the Sourcebooks into a List so they can first be sorted.
			XmlNodeList objXmlBookList = objXmlDocument.SelectNodes("/chummer/books/book[code != \"SR4\"]");
			treSourcebook.Nodes.Clear();
			foreach (XmlNode objXmlBook in objXmlBookList)
			{
				blnChecked = false;
				if (_objOptions.Books.Contains(objXmlBook["code"].InnerText))
					blnChecked = true;

				TreeNode objNode = new TreeNode();
				if (objXmlBook["translate"] != null)
					objNode.Text = objXmlBook["translate"].InnerText;
				else
					objNode.Text = objXmlBook["name"].InnerText;
				objNode.Tag = objXmlBook["code"].InnerText;
				objNode.Checked = blnChecked;
				treSourcebook.Nodes.Add(objNode);
			}
			treSourcebook.Sort();

			// General Options.
			bool blnConfirmDelete = true;
			try
			{
				blnConfirmDelete = _objOptions.ConfirmDelete;
			}
			catch
			{
			}
			chkConfirmDelete.Checked = blnConfirmDelete;

			bool blnConfirmKarmaExpense = true;
			try
			{
				blnConfirmKarmaExpense = _objOptions.ConfirmKarmaExpense;
			}
			catch
			{
			}
			chkConfirmKarmaExpense.Checked = blnConfirmKarmaExpense;

			bool blnPrintSkillsWithZeroRating = true;
			try
			{
				blnPrintSkillsWithZeroRating = _objOptions.PrintSkillsWithZeroRating;
			}
			catch
			{
			}
			chkPrintSkillsWithZeroRating.Checked = blnPrintSkillsWithZeroRating;

			bool blnMoreLethalGameplay = false;
			try
			{
				blnMoreLethalGameplay = _objOptions.MoreLethalGameplay;
			}
			catch
			{
			}
			chkMoreLethalGameplay.Checked = blnMoreLethalGameplay;

			bool blnSpiritForceBasedOnTotalMAG = false;
			try
			{
				blnSpiritForceBasedOnTotalMAG = _objOptions.SpiritForceBasedOnTotalMAG;
			}
			catch
			{
			}
			chkSpiritForceBasedOnTotalMAG.Checked = blnSpiritForceBasedOnTotalMAG;

			bool blnSkillDefaultingIncludesModifiers = false;
			try
			{
				blnSkillDefaultingIncludesModifiers = _objOptions.SkillDefaultingIncludesModifiers;
			}
			catch
			{
			}
			chkSkillDefaultingIncludesModifiers.Checked = blnSkillDefaultingIncludesModifiers;

			bool blnEnforceSkillMaximumModifiedRating = true;
			try
			{
				blnEnforceSkillMaximumModifiedRating = _objOptions.EnforceMaximumSkillRatingModifier;
			}
			catch
			{
			}
			chkEnforceSkillMaximumModifiedRating.Checked = blnEnforceSkillMaximumModifiedRating;

			bool blnCapSkillRating = false;
			try
			{
				blnCapSkillRating = _objOptions.CapSkillRating;
			}
			catch
			{
			}
			chkCapSkillRating.Checked = blnCapSkillRating;

			bool blnPrintExpenses = false;
			try
			{
				blnPrintExpenses = _objOptions.PrintExpenses;
			}
			catch
			{
			}
			chkPrintExpenses.Checked = blnPrintExpenses;

			bool blnFreeKarmaContacts = false;
			try
			{
				blnFreeKarmaContacts = _objOptions.FreeContacts;
			}
			catch
			{
			}
			chkFreeKarmaContacts.Checked = blnFreeKarmaContacts;

			int intFreeKarmaContactsMultiplier = 2;
			try
			{
				intFreeKarmaContactsMultiplier = _objOptions.FreeContactsMultiplier;
			}
			catch
			{
			}
			nudFreeKarmaContactsMultiplier.Value = intFreeKarmaContactsMultiplier;

			bool blnFreeContactsFlat = false;
			try
			{
				blnFreeContactsFlat = _objOptions.FreeContactsFlat;
			}
			catch
			{
			}
			chkFreeContactsFlat.Checked = blnFreeContactsFlat;

			int intFreeContactsFlatNumber = 0;
			try
			{
				intFreeContactsFlatNumber = _objOptions.FreeContactsFlatNumber;
			}
			catch
			{
			}
			nudFreeContactsFlatNumber.Value = intFreeContactsFlatNumber;

			bool blnFreeKarmaKnowledge = false;
			try
			{
				blnFreeKarmaKnowledge = _objOptions.FreeKarmaKnowledge;
			}
			catch
			{
			}
			chkFreeKarmaKnowledge.Checked = blnFreeKarmaKnowledge;

			int intNuyenPerBP = 5000;
			try
			{
				intNuyenPerBP = _objOptions.NuyenPerBP;
			}
			catch
			{
			}
			nudNuyenPerBP.Value = intNuyenPerBP;

			int intEssenceDecimals = 2;
			try
			{
				intEssenceDecimals = _objOptions.EssenceDecimals;
			}
			catch
			{
			}
			if (intEssenceDecimals == 0)
				intEssenceDecimals = 2;
			cboEssenceDecimals.SelectedValue = intEssenceDecimals.ToString();

			bool blnNoSingleArmorEncumbrance = false;
			try
			{
				blnNoSingleArmorEncumbrance = _objOptions.NoSingleArmorEncumbrance;
			}
			catch
			{
			}
			chkNoSingleArmorEncumbrance.Checked = blnNoSingleArmorEncumbrance;

			bool blnIgnoreArmorEncumbrance = false;
			try
			{
				blnIgnoreArmorEncumbrance = _objOptions.IgnoreArmorEncumbrance;
			}
			catch
			{
			}
			chkIgnoreArmorEncumbrance.Checked = blnIgnoreArmorEncumbrance;

			bool blnAlternateArmorEncumbrance = false;
			try
			{
				blnAlternateArmorEncumbrance = _objOptions.AlternateArmorEncumbrance;
			}
			catch
			{
			}
			chkAlternateArmorEncumbrance.Checked = blnAlternateArmorEncumbrance;

			bool blnAllowCyberwareESSDiscounts = false;
			try
			{
				blnAllowCyberwareESSDiscounts = _objOptions.AllowCyberwareESSDiscounts;
			}
			catch
			{
			}
			chkAllowCyberwareESSDiscounts.Checked = blnAllowCyberwareESSDiscounts;

			bool blnESSLossReducesMaximumOnly = false;
			try
			{
				blnESSLossReducesMaximumOnly = _objOptions.ESSLossReducesMaximumOnly;
			}
			catch
			{
			}
			chkESSLossReducesMaximumOnly.Checked = blnESSLossReducesMaximumOnly;

			bool blnAllowSkillRegrouping = false;
			try
			{
				blnAllowSkillRegrouping = _objOptions.AllowSkillRegrouping;
			}
			catch
			{
			}
			chkAllowSkillRegrouping.Checked = blnAllowSkillRegrouping;

			bool blnMetatypeCostsKarma = false;
			try
			{
				blnMetatypeCostsKarma = _objOptions.MetatypeCostsKarma;
			}
			catch
			{
			}
			chkMetatypeCostsKarma.Checked = blnMetatypeCostsKarma;

			int intMetatypeCostsKarmaMultiplier = 1;
			try
			{
				intMetatypeCostsKarmaMultiplier = _objOptions.MetatypeCostsKarmaMultiplier;
			}
			catch
			{
			}
			nudMetatypeCostsKarmaMultiplier.Value = intMetatypeCostsKarmaMultiplier;

			bool blnStrengthAffectsRecoil = false;
			try
			{
				blnStrengthAffectsRecoil = _objOptions.StrengthAffectsRecoil;
			}
			catch
			{
			}
			chkStrengthAffectsRecoil.Checked = blnStrengthAffectsRecoil;

			bool blnMaximumArmorModifications = false;
			try
			{
				blnMaximumArmorModifications = _objOptions.MaximumArmorModifications;
			}
			catch
			{
			}
			chkMaximumArmorModifications.Checked = blnMaximumArmorModifications;

			bool blnArmorSuitCapacity = false;
			try
			{
				blnArmorSuitCapacity = _objOptions.ArmorSuitCapacity;
			}
			catch
			{
			}
			chkArmorSuitCapacity.Checked = blnArmorSuitCapacity;

			bool blnArmorDegradation = false;
			try
			{
				blnArmorDegradation = _objOptions.ArmorDegradation;
			}
			catch
			{
			}
			chkArmorDegradation.Checked = blnArmorDegradation;

			bool blnAutomaticCopyProtection = true;
			try
			{
				blnAutomaticCopyProtection = _objOptions.AutomaticCopyProtection;
			}
			catch
			{
			}
			chkAutomaticCopyProtection.Checked = blnAutomaticCopyProtection;

			bool blnAutomaticRegistration = true;
			try
			{
				blnAutomaticRegistration = _objOptions.AutomaticRegistration;
			}
			catch
			{
			}
			chkAutomaticRegistration.Checked = blnAutomaticRegistration;

			bool blnExceedNegativeQualities = false;
			try
			{
				blnExceedNegativeQualities = _objOptions.ExceedNegativeQualities;
			}
			catch
			{
			}
			chkExceedNegativeQualities.Checked = blnExceedNegativeQualities;

			if (chkExceedPositiveQualities.Checked)
				chkExceedNegativeQualitiesLimit.Enabled = true;

			bool blnExceedNegativeQualitiesLimit = false;
			try
			{
				blnExceedNegativeQualitiesLimit = _objOptions.ExceedNegativeQualitiesLimit;
			}
			catch
			{
			}
			chkExceedNegativeQualitiesLimit.Checked = blnExceedNegativeQualitiesLimit;

			bool blnExceedPositiveQualities = false;
			try
			{
				blnExceedPositiveQualities = _objOptions.ExceedPositiveQualities;
			}
			catch
			{
			}
			chkExceedPositiveQualities.Checked = blnExceedPositiveQualities;

			bool blnUseCalculatedVehicleSensorRatings = false;
			try
			{
				blnUseCalculatedVehicleSensorRatings = _objOptions.UseCalculatedVehicleSensorRatings;
			}
			catch
			{
			}
			chkUseCalculatedVehicleSensorRatings.Checked = blnUseCalculatedVehicleSensorRatings;

			bool blnAlternateMatrixAttribute = false;
			try
			{
				blnAlternateMatrixAttribute = _objOptions.AlternateMatrixAttribute;
			}
			catch
			{
			}
			chkAlternateMatrixAttribute.Checked = blnAlternateMatrixAttribute;

			bool blnAlternateComplexFormCost = false;
			try
			{
				blnAlternateComplexFormCost = _objOptions.AlternateComplexFormCost;
			}
			catch
			{
			}
			chkAlternateComplexFormCost.Checked = blnAlternateComplexFormCost;

			bool blnAllowCustomTransgenics = false;
			try
			{
				blnAllowCustomTransgenics = _objOptions.AllowCustomTransgenics;
			}
			catch
			{
			}
			chkAllowCustomTransgenics.Checked = blnAllowCustomTransgenics;

			bool blnRestrictRecoil = true;
			try
			{
				blnRestrictRecoil = _objOptions.RestrictRecoil;
			}
			catch
			{
			}
			chkRestrictRecoil.Checked = blnRestrictRecoil;

			bool blnMultiplyRestrictedCost = false;
			try
			{
				blnMultiplyRestrictedCost = _objOptions.MultiplyRestrictedCost;
			}
			catch
			{
			}
			chkMultiplyRestrictedCost.Checked = blnMultiplyRestrictedCost;

			bool blnMultiplyForbiddenCost = false;
			try
			{
				blnMultiplyForbiddenCost = _objOptions.MultiplyForbiddenCost;
			}
			catch
			{
			}
			chkMultiplyForbiddenCost.Checked = blnMultiplyForbiddenCost;

			int intRestrictedCostMultiplier = 1;
			try
			{
				intRestrictedCostMultiplier = _objOptions.RestrictedCostMultiplier;
			}
			catch
			{
			}
			nudRestrictedCostMultiplier.Value = intRestrictedCostMultiplier;

			int intForbiddenCostMultiplier = 1;
			try
			{
				intForbiddenCostMultiplier = _objOptions.ForbiddenCostMultiplier;
			}
			catch
			{
			}
			nudForbiddenCostMultiplier.Value = intForbiddenCostMultiplier;

			bool blnEnforceCapacity = true;
			try
			{
				blnEnforceCapacity = _objOptions.EnforceCapacity;
			}
			catch
			{
			}
			chkEnforceCapacity.Checked = blnEnforceCapacity;

			bool blnAllowExceedAttributeBP = false;
			try
			{
				blnAllowExceedAttributeBP = _objOptions.AllowExceedAttributeBP;
			}
			catch
			{
			}
			chkAllowExceedAttributeBP.Checked = blnAllowExceedAttributeBP;

			bool blnUnrestrictedNueyn = false;
			try
			{
				blnUnrestrictedNueyn = _objOptions.UnrestrictedNuyen;
			}
			catch
			{
			}
			chkUnrestrictedNuyen.Checked = blnUnrestrictedNueyn;

			bool blnCalculateCommlinkResponse = true;
			try
			{
				blnCalculateCommlinkResponse = _objOptions.CalculateCommlinkResponse;
			}
			catch
			{
			}
			chkCalculateCommlinkResponse.Checked = blnCalculateCommlinkResponse;

			bool blnErgonomicProgramLimit = true;
			try
			{
				blnErgonomicProgramLimit = _objOptions.ErgonomicProgramLimit;
			}
			catch
			{
			}
			chkErgonomicProgramLimit.Checked = blnErgonomicProgramLimit;

			bool blnAllowSkillDiceRolling = false;
			try
			{
				blnAllowSkillDiceRolling = _objOptions.AllowSkillDiceRolling;
			}
			catch
			{
			}
			chkAllowSkillDiceRolling.Checked = blnAllowSkillDiceRolling;

			bool blnCreateBackupOnCareer = false;
			try
			{
				blnCreateBackupOnCareer = _objOptions.CreateBackupOnCareer;
			}
			catch
			{
			}
			chkCreateBackupOnCareer.Checked = blnCreateBackupOnCareer;

			bool blnPrintLeadershipAlternates = false;
			try
			{
				blnPrintLeadershipAlternates = _objOptions.PrintLeadershipAlternates;
			}
			catch
			{
			}
			chkPrintLeadershipAlternates.Checked = blnPrintLeadershipAlternates;

			bool blnPrintArcanaAlternates = false;
			try
			{
				blnPrintArcanaAlternates = _objOptions.PrintArcanaAlternates;
			}
			catch
			{
			}
			chkPrintArcanaAlternates.Checked = blnPrintArcanaAlternates;

			bool blnPrintNotes = false;
			try
			{
				blnPrintNotes = _objOptions.PrintNotes;
			}
			catch
			{
			}
			chkPrintNotes.Checked = blnPrintNotes;

			bool blnAllowHigherStackedFoci = false;
			try
			{
				blnAllowHigherStackedFoci = _objOptions.AllowHigherStackedFoci;
			}
			catch
			{
			}
			chkAllowHigherStackedFoci.Checked = blnAllowHigherStackedFoci;

			bool blnAllowEditPartOfBaseWeapon = false;
			try
			{
				blnAllowEditPartOfBaseWeapon = _objOptions.AllowEditPartOfBaseWeapon;
			}
			catch
			{
			}
			chkAllowEditPartOfBaseWeapon.Checked = blnAllowEditPartOfBaseWeapon;

			bool blnAlternateMetatypeAttributeKarma = false;
			try
			{
				blnAlternateMetatypeAttributeKarma = _objOptions.AlternateMetatypeAttributeKarma;
			}
			catch
			{
			}
			chkAlternateMetatypeAttributeKarma.Checked = blnAlternateMetatypeAttributeKarma;

			bool blnAllowObsolescentUpgrade = false;
			try
			{
				blnAllowObsolescentUpgrade = _objOptions.AllowObsolescentUpgrade;
			}
			catch
			{
			}
			chkAllowObsolescentUpgrade.Checked = blnAllowObsolescentUpgrade;

			bool blnAllowBiowareSuites = false;
			try
			{
				blnAllowBiowareSuites = _objOptions.AllowBiowareSuites;
			}
			catch
			{
			}
			chkAllowBiowareSuites.Checked = blnAllowBiowareSuites;

			bool blnFreeSpiritPowerPointsMAG = false;
			try
			{
				blnFreeSpiritPowerPointsMAG = _objOptions.FreeSpiritPowerPointsMAG;
			}
			catch
			{
			}
			chkFreeSpiritsPowerPointsMAG.Checked = blnFreeSpiritPowerPointsMAG;

			bool blnSpecialAttributeKarmaLimit = false;
			try
			{
				blnSpecialAttributeKarmaLimit = _objOptions.SpecialAttributeKarmaLimit;
			}
			catch
			{
			}
			chkSpecialAttributeKarmaLimit.Checked = blnSpecialAttributeKarmaLimit;

			bool blnTechnomancerAllowAutosoft = false;
			try
			{
				blnTechnomancerAllowAutosoft = _objOptions.TechnomancerAllowAutosoft;
			}
			catch
			{
			}
			chkTechnomancerAllowAutosoft.Checked = blnTechnomancerAllowAutosoft;

			if (_objOptions.LimbCount == 6)
				cboLimbCount.SelectedValue = "all";
			else
			{
				if (_objOptions.ExcludeLimbSlot == "skull")
					cboLimbCount.SelectedValue = "torso";
				else
					cboLimbCount.SelectedValue = "skull";
			}

			// Populate BP fields.
			nudBPAttribute.Value = _objOptions.BPAttribute;
			nudBPAttributeMax.Value = _objOptions.BPAttributeMax;
			nudBPContact.Value = _objOptions.BPContact;
			nudBPMartialArt.Value = _objOptions.BPMartialArt;
			nudBPMartialArtManeuver.Value = _objOptions.BPMartialArtManeuver;
			nudBPSkillGroup.Value = _objOptions.BPSkillGroup;
			nudBPActiveSkill.Value = _objOptions.BPActiveSkill;
			nudBPSkillSpecialization.Value = _objOptions.BPActiveSkillSpecialization;
			nudBPKnowledgeSkill.Value = _objOptions.BPKnowledgeSkill;
			nudBPSpell.Value = _objOptions.BPSpell;
			nudBPFocus.Value = _objOptions.BPFocus;
			nudBPSpirit.Value = _objOptions.BPSpirit;
			nudBPComplexForm.Value = _objOptions.BPComplexForm;
			nudBPComplexFormOption.Value = _objOptions.BPComplexFormOption;

			// Populate the Karma fields.
			nudKarmaAttribute.Value = _objOptions.KarmaAttribute;
			nudKarmaQuality.Value = _objOptions.KarmaQuality;
			nudKarmaSpecialization.Value = _objOptions.KarmaSpecialization;
			nudKarmaNewKnowledgeSkill.Value = _objOptions.KarmaNewKnowledgeSkill;
			nudKarmaNewActiveSkill.Value = _objOptions.KarmaNewActiveSkill;
			nudKarmaNewSkillGroup.Value = _objOptions.KarmaNewSkillGroup;
			nudKarmaImproveKnowledgeSkill.Value = _objOptions.KarmaImproveKnowledgeSkill;
			nudKarmaImproveActiveSkill.Value = _objOptions.KarmaImproveActiveSkill;
			nudKarmaImproveSkillGroup.Value = _objOptions.KarmaImproveSkillGroup;
			nudKarmaSpell.Value = _objOptions.KarmaSpell;
			nudKarmaNewComplexForm.Value = _objOptions.KarmaNewComplexForm;
			nudKarmaImproveComplexForm.Value = _objOptions.KarmaImproveComplexForm;
			nudKarmaComplexFormOption.Value = _objOptions.KarmaComplexFormOption;
			nudKarmaComplexFormSkillsoft.Value = _objOptions.KarmaComplexFormSkillsoft;
			nudKarmaNuyenPer.Value = _objOptions.KarmaNuyenPer;
			nudKarmaContact.Value = _objOptions.KarmaContact;
			nudKarmaCarryover.Value = _objOptions.KarmaCarryover;
			nudKarmaSpirit.Value = _objOptions.KarmaSpirit;
			nudKarmaManeuver.Value = _objOptions.KarmaManeuver;
			nudKarmaInitiation.Value = _objOptions.KarmaInitiation;
			nudKarmaMetamagic.Value = _objOptions.KarmaMetamagic;
			nudKarmaJoinGroup.Value = _objOptions.KarmaJoinGroup;
			nudKarmaLeaveGroup.Value = _objOptions.KarmaLeaveGroup;

			nudKarmaAnchoringFocus.Value = _objOptions.KarmaAnchoringFocus;
			nudKarmaBanishingFocus.Value = _objOptions.KarmaBanishingFocus;
			nudKarmaBindingFocus.Value = _objOptions.KarmaBindingFocus;
			nudKarmaCenteringFocus.Value = _objOptions.KarmaCenteringFocus;
			nudKarmaCounterspellingFocus.Value = _objOptions.KarmaCounterspellingFocus;
			nudKarmaDiviningFocus.Value = _objOptions.KarmaDiviningFocus;
			nudKarmaDowsingFocus.Value = _objOptions.KarmaDowsingFocus;
			nudKarmaInfusionFocus.Value = _objOptions.KarmaInfusionFocus;
			nudKarmaMaskingFocus.Value = _objOptions.KarmaMaskingFocus;
			nudKarmaPowerFocus.Value = _objOptions.KarmaPowerFocus;
			nudKarmaShieldingFocus.Value = _objOptions.KarmaShieldingFocus;
			nudKarmaSpellcastingFocus.Value = _objOptions.KarmaSpellcastingFocus;
			nudKarmaSummoningFocus.Value = _objOptions.KarmaSummoningFocus;
			nudKarmaSustainingFocus.Value = _objOptions.KarmaSustainingFocus;
			nudKarmaSymbolicLinkFocus.Value = _objOptions.KarmaSymbolicLinkFocus;
			nudKarmaWeaponFocus.Value = _objOptions.KarmaWeaponFocus;

			// Load default build method info.
			cboBuildMethod.SelectedValue = _objOptions.BuildMethod;
			nudBP.Value = _objOptions.BuildPoints;
			nudMaxAvail.Value = _objOptions.Availability;

			txtSettingName.Text = _objOptions.Name;
			if (cboSetting.SelectedValue.ToString() == "default.xml")
				txtSettingName.Enabled = false;
			else
				txtSettingName.Enabled = true;
		}
		#endregion
	}
}