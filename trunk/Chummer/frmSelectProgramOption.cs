﻿using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Xml;

namespace Chummer
{
	public partial class frmSelectProgramOption : Form
	{
		private string _strSelectedOption = "";
		private string _strProgramName = "";
		private string _strProgramCategory = "";
		private List<string> _lstTags = new List<string>();

		private bool _blnAddAgain = false;

		private XmlDocument _objXmlDocument = new XmlDocument();
		private readonly Character _objCharacter;

		#region Control Events
		public frmSelectProgramOption(Character objCharacter)
		{
			InitializeComponent();
			LanguageManager.Instance.Load(GlobalOptions.Instance.Language, this);
			_objCharacter = objCharacter;
			MoveControls();
		}

		private void frmSelectProgramOption_Load(object sender, EventArgs e)
		{
			List<ListItem> lstOption = new List<ListItem>();

			// Load the Programs information.
			_objXmlDocument = XmlManager.Instance.Load("programs.xml");

			// Populate the Program list.
			XmlNodeList objXmlOptionList = _objXmlDocument.SelectNodes("/chummer/options/option[" + _objCharacter.Options.BookXPath() + "]");

			foreach (XmlNode objXmlOption in objXmlOptionList)
			{
				bool blnAdd = true;
				// If the Option has Category requirements, make sure they are met before adding the item to the list.
				if (objXmlOption["programtypes"] != null)
				{
					blnAdd = false;
					foreach (XmlNode objXmlCategory in objXmlOption.SelectNodes("programtypes/programtype"))
					{
						if (objXmlCategory.InnerText == _strProgramCategory)
							blnAdd = true;
					}
				}

				if (blnAdd)
				{
					ListItem objItem = new ListItem();
					objItem.Value = objXmlOption["name"].InnerText;
					if (objXmlOption["translate"] != null)
						objItem.Name = objXmlOption["translate"].InnerText;
					else
						objItem.Name = objXmlOption["name"].InnerText;
					lstOption.Add(objItem);
				}
			}
			SortListItem objSort = new SortListItem();
			lstOption.Sort(objSort.Compare);
			lstOptions.ValueMember = "Value";
			lstOptions.DisplayMember = "Name";
			lstOptions.DataSource = lstOption;
		}

		private void lstOptions_SelectedIndexChanged(object sender, EventArgs e)
		{
			// Display the Program information.
			XmlNode objXmlOption = _objXmlDocument.SelectSingleNode("/chummer/options/option[name = \"" + lstOptions.SelectedValue + "\"]");

			string strBook = _objCharacter.Options.LanguageBookShort(objXmlOption["source"].InnerText);
			string strPage = objXmlOption["page"].InnerText;
			if (objXmlOption["altpage"] != null)
				strPage = objXmlOption["altpage"].InnerText;
			lblSource.Text = strBook + " " + strPage;

			tipTooltip.SetToolTip(lblSource, _objCharacter.Options.LanguageBookLong(objXmlOption["source"].InnerText) + " " + LanguageManager.Instance.GetString("String_Page") + " " + strPage);
		}

		private void cmdOK_Click(object sender, EventArgs e)
		{
			if (lstOptions.Text != "")
				AcceptForm();
		}

		private void lstOptions_DoubleClick(object sender, EventArgs e)
		{
			if (lstOptions.Text != "")
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
		/// Name of the Program the Option will be added to.
		/// </summary>
		public string ProgramName
		{
			set
			{
				_strProgramName = value;
			}
		}

		/// <summary>
		/// Category of the Program the Option will be added to.
		/// </summary>
		public string ProgramCategory
		{
			set
			{
				_strProgramCategory = value;
			}
		}

		/// <summary>
		/// Tags associated with the Program.
		/// </summary>
		public List<string> ProgramTags
		{
			get
			{
				return _lstTags;
			}
			set
			{
				_lstTags = value;
			}
		}

		/// <summary>
		/// Program Option that was selected in the dialogue.
		/// </summary>
		public string SelectedOption
		{
			get
			{
				return _strSelectedOption;
			}
		}
		#endregion

		#region Methods
		/// <summary>
		/// Accept the selected item and close the form.
		/// </summary>
		private void AcceptForm()
		{
			_strSelectedOption = lstOptions.SelectedValue.ToString();
			this.DialogResult = DialogResult.OK;
		}

		private void MoveControls()
		{
			lblCommonSkill.Left = lblCommonSkillLabel.Left + lblCommonSkillLabel.Width + 6;
			lblSource.Left = lblSourceLabel.Left + lblSourceLabel.Width + 6;
		}
		#endregion
	}
}