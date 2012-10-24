﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Xml;

namespace Chummer
{
	public partial class frmSelectMetamagic : Form
	{
		private string _strSelectedMetamagic = "";

		private Mode _objMode = Mode.Metamagic;
		private string _strNode = "metamagic";
		private string _strRoot = "metamagics";
		private bool _blnAddAgain = false;

		private readonly Character _objCharacter;

		private XmlDocument _objXmlDocument = new XmlDocument();

		private readonly XmlDocument _objMetatypeDocument = new XmlDocument();
		private readonly XmlDocument _objCritterDocument = new XmlDocument();
		private readonly XmlDocument _objQualityDocument = new XmlDocument();

		public enum Mode
		{
			Metamagic = 0,
			Echo = 1,
		}

		#region Control Events
		public frmSelectMetamagic(Character objCharacter)
		{
			InitializeComponent();
			LanguageManager.Instance.Load(GlobalOptions.Instance.Language, this);
			_objCharacter = objCharacter;

			_objMetatypeDocument = XmlManager.Instance.Load("metatypes.xml");
			_objCritterDocument = XmlManager.Instance.Load("critters.xml");
			_objQualityDocument = XmlManager.Instance.Load("qualities.xml");
		}

		private void frmSelectMetamagic_Load(object sender, EventArgs e)
		{
			// Update the window title if needed.
			if (_strNode == "echo")
			{
				this.Text = LanguageManager.Instance.GetString("Title_SelectMetamagic_Echo");
				chkLimitList.Text = LanguageManager.Instance.GetString("Checkbox_SelectEcho_LimitList");
			}

			foreach (Label objLabel in this.Controls.OfType<Label>())
			{
				if (objLabel.Text.StartsWith("["))
					objLabel.Text = "";
			}

			BuildMetamagicList();
		}

		private void lstMetamagic_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (lstMetamagic.Text == "")
				return;

			// Retireve the information for the selected piece of Cyberware.
			XmlNode objXmlMetamagic = _objXmlDocument.SelectSingleNode("/chummer/" + _strRoot + "/" + _strNode + "[name = \"" + lstMetamagic.SelectedValue + "\"]");

			string strBook = _objCharacter.Options.LanguageBookShort(objXmlMetamagic["source"].InnerText);
			string strPage = objXmlMetamagic["page"].InnerText;
			if (objXmlMetamagic["altpage"] != null)
				strPage = objXmlMetamagic["altpage"].InnerText;
			lblSource.Text = strBook + " " + strPage;

			tipTooltip.SetToolTip(lblSource, _objCharacter.Options.LanguageBookLong(objXmlMetamagic["source"].InnerText) + " " + LanguageManager.Instance.GetString("String_Page") + " " + strPage);
		}

		private void cmdOK_Click(object sender, EventArgs e)
		{
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

		private void lstMetamagic_DoubleClick(object sender, EventArgs e)
		{
			if (lstMetamagic.Text != "")
				AcceptForm();
		}

		private void chkLimitList_CheckedChanged(object sender, EventArgs e)
		{
			BuildMetamagicList();
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
		/// Set the window's Mode to Cyberware or Bioware.
		/// </summary>
		public Mode WindowMode
		{
			get
			{
				return _objMode;
			}
			set
			{
				_objMode = value;
				switch (_objMode)
				{
					case Mode.Metamagic:
						_strNode = "metamagic";
						_strRoot = "metamagics";
						break;
					case Mode.Echo:
						_strNode = "echo";
						_strRoot = "echoes";
						break;
				}
			}
		}

		/// <summary>
		/// Name of Metamagic that was selected in the dialogue.
		/// </summary>
		public string SelectedMetamagic
		{
			get
			{
				return _strSelectedMetamagic;
			}
		}
		#endregion

		#region Methods
		/// <summary>
		/// Build the list of Metamagics.
		/// </summary>
		private void BuildMetamagicList()
		{
			XmlNodeList objXmlMetamagicList;
			List<ListItem> lstMetamagics = new List<ListItem>();

			// Load the Metamagic information.
			switch (_objMode)
			{
				case Mode.Metamagic:
					_objXmlDocument = XmlManager.Instance.Load("metamagic.xml");
					break;
				case Mode.Echo:
					_objXmlDocument = XmlManager.Instance.Load("echoes.xml");
					break;
			}

			// If the character has MAG enabled, filter the list based on Adept/Magician availability.
			if (_objCharacter.MAGEnabled)
			{
				if (_objCharacter.MagicianEnabled && !_objCharacter.AdeptEnabled)
					objXmlMetamagicList = _objXmlDocument.SelectNodes("/chummer/" + _strRoot + "/" + _strNode + "[magician = 'yes' and (" + _objCharacter.Options.BookXPath() + ")]");
				else if (!_objCharacter.MagicianEnabled && _objCharacter.AdeptEnabled)
					objXmlMetamagicList = _objXmlDocument.SelectNodes("/chummer/" + _strRoot + "/" + _strNode + "[adept = 'yes' and (" + _objCharacter.Options.BookXPath() + ")]");
				else
					objXmlMetamagicList = _objXmlDocument.SelectNodes("/chummer/" + _strRoot + "/" + _strNode + "[" + _objCharacter.Options.BookXPath() + "]");
			}
			else
				objXmlMetamagicList = _objXmlDocument.SelectNodes("/chummer/" + _strRoot + "/" + _strNode + "[" + _objCharacter.Options.BookXPath() + "]");

			foreach (XmlNode objXmlMetamagic in objXmlMetamagicList)
			{
				if (!chkLimitList.Checked || (chkLimitList.Checked && RequirementMet(objXmlMetamagic, false)))
				{
					ListItem objItem = new ListItem();
					objItem.Value = objXmlMetamagic["name"].InnerText;
					if (objXmlMetamagic["translate"] != null)
						objItem.Name = objXmlMetamagic["translate"].InnerText;
					else
						objItem.Name = objXmlMetamagic["name"].InnerText;
					lstMetamagics.Add(objItem);
				}
			}
			SortListItem objSort = new SortListItem();
			lstMetamagics.Sort(objSort.Compare);
			lstMetamagic.DataSource = null;
			lstMetamagic.ValueMember = "Value";
			lstMetamagic.DisplayMember = "Name";
			lstMetamagic.DataSource = lstMetamagics;
		}

		/// <summary>
		/// Accept the selected item and close the form.
		/// </summary>
		private void AcceptForm()
		{
			if (lstMetamagic.Text == "")
				return;

			_strSelectedMetamagic = lstMetamagic.SelectedValue.ToString();

			// Make sure the selected Metamagic or Echo meets its requirements.
			XmlNode objXmlMetamagic;
			if (_objMode == Mode.Metamagic)
				objXmlMetamagic = _objXmlDocument.SelectSingleNode("/chummer/metamagics/metamagic[name = \"" + lstMetamagic.SelectedValue + "\"]");
			else
				objXmlMetamagic = _objXmlDocument.SelectSingleNode("/chummer/echoes/echo[name = \"" + lstMetamagic.SelectedValue + "\"]");

			if (!RequirementMet(objXmlMetamagic, true))
				return;

			this.DialogResult = DialogResult.OK;
		}

		/// <summary>
		/// Check if the Metamagic's requirements/restrictions are being met.
		/// </summary>
		/// <param name="objXmlCheckMetamagic">XmlNode of the Metamagic.</param>
		/// <param name="blnShowMessage">Whether or not a message should be shown if the requirements are not met.</param>
		private bool RequirementMet(XmlNode objXmlCheckMetamagic, bool blnShowMessage)
		{
			// Ignore the rules.
			if (_objCharacter.IgnoreRules)
				return true;

			string strParent = "";
			string strChild = "";
			if (_objMode == Mode.Metamagic)
			{
				strParent = "metamagics";
				strChild = "metamagic";
			}
			else
			{
				strParent = "echoes";
				strChild = "echo";
			}

			if (objXmlCheckMetamagic.InnerXml.Contains("<required>"))
			{
				string strRequirement = "\n" + LanguageManager.Instance.GetString("Message_SelectQuality_AllOf");
				bool blnRequirementMet = true;

				// Metamagic requirements.
				foreach (XmlNode objXmlMetamagic in objXmlCheckMetamagic.SelectNodes("required/allof/metamagic"))
				{
					bool blnFound = false;
					foreach (Metamagic objMetamagic in _objCharacter.Metamagics)
					{
						if (objMetamagic.Name == objXmlMetamagic.InnerText)
						{
							blnFound = true;
							break;
						}
					}

					if (!blnFound)
					{
						blnRequirementMet = false;
						XmlNode objNode = _objXmlDocument.SelectSingleNode("/chummer/" + strParent + "/" + strChild + "[name = \"" + objXmlMetamagic.InnerText + "\"]");
						if (objNode["translate"] != null)
							strRequirement += "\n\t" + objNode["translate"].InnerText;
						else
							strRequirement += "\n\t" + objXmlMetamagic.InnerText;
					}
				}

				// Echo requirements.
				foreach (XmlNode objXmlEcho in objXmlCheckMetamagic.SelectNodes("required/allof/echo"))
				{
					bool blnFound = false;
					foreach (Metamagic objEcho in _objCharacter.Metamagics)
					{
						if (objEcho.Name == objXmlEcho.InnerText)
						{
							blnFound = true;
							break;
						}
					}

					if (!blnFound)
					{
						blnRequirementMet = false;
						XmlNode objNode = _objXmlDocument.SelectSingleNode("/chummer/" + strParent + "/" + strChild + "[name = \"" + objXmlEcho.InnerText + "\"]");
						if (objNode["translate"] != null)
							strRequirement += "\n\t" + objNode["translate"].InnerText;
						else
							strRequirement += "\n\t" + objXmlEcho.InnerText;
					}
				}

				// Metatype requirements.
				bool blnMetatypeFound = false;
				string strMetatypeRequirement = "";
				if (objXmlCheckMetamagic.SelectNodes("required/allof/metatype").Count == 0)
					blnMetatypeFound = true;
				else
				{
					foreach (XmlNode objXmlMetatype in objXmlCheckMetamagic.SelectNodes("required/allof/metatype"))
					{
						if (_objCharacter.Metatype == objXmlMetatype.InnerText)
						{
							blnMetatypeFound = true;
							break;
						}

						XmlNode objNode =
							_objMetatypeDocument.SelectSingleNode("/chummer/metatypes/metatype[name = \"" + objXmlMetatype.InnerText + "\"]");
						if (objNode == null)
							objNode =
								_objCritterDocument.SelectSingleNode("/chummer/metatypes/metatype[name = \"" + objXmlMetatype.InnerText + "\"]");
						if (objNode["translate"] != null)
							strMetatypeRequirement += "\n\t" + objNode["translate"].InnerText;
						else
							strMetatypeRequirement += "\n\t" + objXmlMetatype.InnerText;
					}
					if (!blnMetatypeFound)
					{
						blnRequirementMet = false;
						strRequirement += strMetatypeRequirement;
					}
				}

				// Quality requirements.
				bool blnQualityFound = false;
				string strQualityRequirement = "";
				if (objXmlCheckMetamagic.SelectNodes("required/allof/quality").Count == 0)
					blnQualityFound = true;
				else
				{
					foreach (XmlNode objXmlQuality in objXmlCheckMetamagic.SelectNodes("required/allof/quality"))
					{
						foreach (Quality objQuality in _objCharacter.Qualities)
						{
							if (objQuality.Name == objXmlQuality.InnerText)
							{
								blnQualityFound = true;
								break;
							}

							XmlNode objNode =
								_objQualityDocument.SelectSingleNode("/chummer/qualities/quality[name = \"" + objXmlQuality.InnerText + "\"]");
							if (objNode["translate"] != null)
								strQualityRequirement += "\n\t" + objNode["translate"].InnerText;
							else
								strQualityRequirement += "\n\t" + objXmlQuality.InnerText;
						}
					}
					if (!blnQualityFound)
					{
						blnRequirementMet = false;
						strRequirement += strQualityRequirement;
					}
				}

				if (!blnRequirementMet)
				{
					string strMessage = "";
					string strTitle = "";

					if (_objMode == Mode.Metamagic)
					{
						strMessage = LanguageManager.Instance.GetString("Message_SelectMetamagic_MetamagicRequirement");
						strTitle = LanguageManager.Instance.GetString("MessageTitle_SelectMetamagic_MetamagicRequirement");
					}
					else
					{
						strMessage = LanguageManager.Instance.GetString("Message_SelectMetamagic_EchoRequirement");
						strTitle = LanguageManager.Instance.GetString("MessageTitle_SelectMetamagic_EchoRequirement");
					}
					strMessage += strRequirement;

					if (blnShowMessage)
						MessageBox.Show(strMessage, strTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
					return false;
				}
			}

			return true;
		}
		#endregion
	}
}