﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Xml;

namespace Chummer
{
	public partial class frmSelectMentorSpirit : Form
	{
		private string _strSelectedMentor = "";

		private XmlNode _nodBonus;
		private XmlNode _nodChoice1Bonus;
		private XmlNode _nodChoice2Bonus;
		private static string _strSelectCategory = "";
		private string _strXmlFile = "mentors.xml";

		private XmlDocument _objXmlDocument = new XmlDocument();
		private readonly Character _objCharacter;

		private List<ListItem> _lstCategory = new List<ListItem>();

		#region Control Events
		public frmSelectMentorSpirit(Character objCharacter)
		{
			InitializeComponent();
			LanguageManager.Instance.Load(GlobalOptions.Instance.Language, this);
			_objCharacter = objCharacter;
		}

		private void frmSelectMentorSpirit_Load(object sender, EventArgs e)
		{
			if (_strXmlFile == "paragons.xml")
				this.Text = LanguageManager.Instance.GetString("Title_SelectMentorSpirit_Paragon");

			foreach (Label objLabel in this.Controls.OfType<Label>())
			{
				if (objLabel.Text.StartsWith("["))
					objLabel.Text = "";
			}

			// Load the Mentor information.
			_objXmlDocument = XmlManager.Instance.Load(_strXmlFile);

			// Populate the Mentor Spirit Category list.
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

		private void cmdOK_Click(object sender, EventArgs e)
		{
			AcceptForm();
		}

		private void lstMentor_DoubleClick(object sender, EventArgs e)
		{
			AcceptForm();
		}

		private void lstMentor_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (lstMentor.Text == "")
				return;

			// Get the information for the selected Mentor.
			XmlNode objXmlMentor = _objXmlDocument.SelectSingleNode("/chummer/mentors/mentor[name = \"" + lstMentor.SelectedValue + "\"]");

			if (objXmlMentor["altadvantage"] != null)
				lblAdvantage.Text = objXmlMentor["altadvantage"].InnerText;
			else
				lblAdvantage.Text = objXmlMentor["advantage"].InnerText;
			if (objXmlMentor["altdisadvantage"] != null)
				lblDisadvantage.Text = objXmlMentor["altdisadvantage"].InnerText;
			else
				lblDisadvantage.Text = objXmlMentor["disadvantage"].InnerText;

			cboChoice1.DataSource = null;
			cboChoice2.DataSource = null;

			// If the Mentor offers a choice of bonuses, build the list and let the user select one.
			if (objXmlMentor["choices"] != null)
			{
				List<ListItem> lstChoice1 = new List<ListItem>();
				List<ListItem> lstChoice2 = new List<ListItem>();

				foreach (XmlNode objChoice in objXmlMentor["choices"].SelectNodes("choice"))
				{					
					ListItem objItem = new ListItem();
					objItem.Value = objChoice["name"].InnerText;
					if (objChoice["translate"] != null)
						objItem.Name = objChoice["translate"].InnerText;
					else
						objItem.Name = objChoice["name"].InnerText;

					if (objChoice.Attributes["set"] != null)
					{
						if (objChoice.Attributes["set"].InnerText == "2")
							lstChoice2.Add(objItem);
						else
							lstChoice1.Add(objItem);
					}
					else
						lstChoice1.Add(objItem);
				}

				lblChoice1.Visible = true;
				cboChoice1.Visible = true;
				cboChoice1.ValueMember = "Value";
				cboChoice1.DisplayMember = "Name";
				cboChoice1.DataSource = lstChoice1;

				if (lstChoice2.Count > 0)
				{
					lblChoice2.Visible = true;
					cboChoice2.Visible = true;
					cboChoice2.ValueMember = "Value";
					cboChoice2.DisplayMember = "Name";
					cboChoice2.DataSource = lstChoice2;
				}

				cboChoice1.Top = lblAdvantage.Top + lblAdvantage.Height + 6;
				lblChoice1.Top = cboChoice1.Top + 3;
				cboChoice2.Top = cboChoice1.Top + cboChoice1.Height + 6;
				lblChoice2.Top = cboChoice2.Top + 3;
			}
			else
			{
				lblChoice1.Visible = false;
				cboChoice1.Visible = false;
				lblChoice2.Visible = false;
				cboChoice2.Visible = false;
			}

			string strBook = _objCharacter.Options.LanguageBookShort(objXmlMentor["source"].InnerText);
			string strPage = objXmlMentor["page"].InnerText;
			if (objXmlMentor["altpage"] != null)
				strPage = objXmlMentor["altpage"].InnerText;
			lblSource.Text = strBook + " " + strPage;

			tipTooltip.SetToolTip(lblSource, _objCharacter.Options.LanguageBookLong(objXmlMentor["source"].InnerText) + " " + LanguageManager.Instance.GetString("String_Page") + " " + strPage);
		}

		private void cboCategory_SelectedIndexChanged(object sender, EventArgs e)
		{
			List<ListItem> lstMentors = new List<ListItem>();

			// Populate the Mentor list.
			XmlNodeList objXmlMentorList = _objXmlDocument.SelectNodes("/chummer/mentors/mentor[category = \"" + cboCategory.SelectedValue + "\" and (" + _objCharacter.Options.BookXPath() + ")]");
			foreach (XmlNode objXmlMentor in objXmlMentorList)
			{
				ListItem objItem = new ListItem();
				objItem.Value = objXmlMentor["name"].InnerText;
				if (objXmlMentor["translate"] != null)
					objItem.Name = objXmlMentor["translate"].InnerText;
				else
					objItem.Name = objXmlMentor["name"].InnerText;
				lstMentors.Add(objItem);
			}
			SortListItem objSort = new SortListItem();
			lstMentors.Sort(objSort.Compare);
			lstMentor.DataSource = null;
			lstMentor.ValueMember = "Value";
			lstMentor.DisplayMember = "Name";
			lstMentor.DataSource = lstMentors;
		}
		#endregion

		#region Properties
		/// <summary>
		/// XML file to read from. Default mentors.xml.
		/// </summary>
		public string XmlFile
		{
			set
			{
				_strXmlFile = value;
			}
		}

		/// <summary>
		/// Mentor that was selected in the dialogue.
		/// </summary>
		public string SelectedMentor
		{
			get
			{
				return _strSelectedMentor;
			}
		}

		/// <summary>
		/// First choice that was selected in the dialogue.
		/// </summary>
		public string Choice1
		{
			get
			{
				try
				{
					return cboChoice1.SelectedValue.ToString();
				}
				catch
				{
					return "";
				}
			}
		}

		/// <summary>
		/// Second choice that was selected in the dialogue.
		/// </summary>
		public string Choice2
		{
			get
			{
				try
				{
					return cboChoice2.SelectedValue.ToString();
				}
				catch
				{
					return "";
				}
			}
		}

		/// <summary>
		/// Bonus Node for the Mentor that was selected.
		/// </summary>
		public XmlNode BonusNode
		{
			get
			{
				return _nodBonus;
			}
		}

		/// <summary>
		/// Bonus Node for the first choice that was selected.
		/// </summary>
		public XmlNode Choice1BonusNode
		{
			get
			{
				return _nodChoice1Bonus;
			}
		}

		/// <summary>
		/// Bonus Node for the second choice that was selected.
		/// </summary>
		public XmlNode Choice2BonusNode
		{
			get
			{
				return _nodChoice2Bonus;
			}
		}
		#endregion

		#region Methods
		/// <summary>
		/// Accept the selected item and close the form.
		/// </summary>
		private void AcceptForm()
		{
			if (lstMentor.Text != "")
			{
				_strSelectedMentor = lstMentor.SelectedValue.ToString();

				XmlNode objXmlMentor = _objXmlDocument.SelectSingleNode("/chummer/mentors/mentor[name = \"" + lstMentor.SelectedValue + "\"]");
				if (objXmlMentor.InnerXml.Contains("<bonus>"))
					_nodBonus = objXmlMentor.SelectSingleNode("bonus");

				if (cboChoice1.SelectedValue != null)
				{
					XmlNode objChoice = objXmlMentor.SelectSingleNode("choices/choice[name = \"" + cboChoice1.SelectedValue + "\"]");
					if (objChoice.InnerXml.Contains("<bonus>"))
						_nodChoice1Bonus = objChoice.SelectSingleNode("bonus");
				}

				if (cboChoice2.SelectedValue != null)
				{
					XmlNode objChoice = objXmlMentor.SelectSingleNode("choices/choice[name = \"" + cboChoice2.SelectedValue + "\"]");
					if (objChoice.InnerXml.Contains("<bonus>"))
						_nodChoice2Bonus = objChoice.SelectSingleNode("bonus");
				}

				this.DialogResult = DialogResult.OK;
			}
		}
		#endregion
	}
}