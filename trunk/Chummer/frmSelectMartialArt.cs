﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Xml;

namespace Chummer
{
	public partial class frmSelectMartialArt : Form
	{
		private string _strSelectedMartialArt = "";

		private bool _blnAddAgain = false;
		private string _strForcedValue = "";

		private XmlDocument _objXmlDocument = new XmlDocument();
		private readonly Character _objCharacter;

		#region Control Events
		public frmSelectMartialArt(Character objCharacter)
		{
			InitializeComponent();
			LanguageManager.Instance.Load(GlobalOptions.Instance.Language, this);
			_objCharacter = objCharacter;
		}

		private void frmSelectMartialArt_Load(object sender, EventArgs e)
		{
			foreach (Label objLabel in this.Controls.OfType<Label>())
			{
				if (objLabel.Text.StartsWith("["))
					objLabel.Text = "";
			}

			XmlNodeList objArtList;
			List<ListItem> lstMartialArt = new List<ListItem>();

			// Load the Martial Arts information.
			_objXmlDocument = XmlManager.Instance.Load("martialarts.xml");

			// Populate the Martial Arts list.
			if (_strForcedValue == "")
				objArtList = _objXmlDocument.SelectNodes("/chummer/martialarts/martialart[" + _objCharacter.Options.BookXPath() + "]");
			else
				objArtList = _objXmlDocument.SelectNodes("/chummer/martialarts/martialart[name = \"" + _strForcedValue + "\"]");
			foreach (XmlNode objXmlArt in objArtList)
			{
				ListItem objItem = new ListItem();
				objItem.Value = objXmlArt["name"].InnerText;
				if (objXmlArt["translate"] != null)
					objItem.Name = objXmlArt["translate"].InnerText;
				else
					objItem.Name = objXmlArt["name"].InnerText;
				lstMartialArt.Add(objItem);
			}
			SortListItem objSort = new SortListItem();
			lstMartialArt.Sort(objSort.Compare);
			lstMartialArts.DataSource = null;
			lstMartialArts.ValueMember = "Value";
			lstMartialArts.DisplayMember = "Name";
			lstMartialArts.DataSource = lstMartialArt;

			if (lstMartialArts.Items.Count == 1)
			{
				lstMartialArts.SelectedIndex = 0;
				AcceptForm();
			}
		}

		private void cmdOK_Click(object sender, EventArgs e)
		{
			if (lstMartialArts.Text != "")
				AcceptForm();
		}

		private void cmdCancel_Click(object sender, EventArgs e)
		{
			this.DialogResult = DialogResult.Cancel;
		}

		private void lstMartialArts_DoubleClick(object sender, EventArgs e)
		{
			if (lstMartialArts.Text != "")
				AcceptForm();
		}

		private void lstMartialArts_SelectedIndexChanged(object sender, EventArgs e)
		{
			// Populate the Martial Arts list.
			XmlNode objXmlArt = _objXmlDocument.SelectSingleNode("/chummer/martialarts/martialart[name = \"" + lstMartialArts.SelectedValue + "\"]");

			string strBook = _objCharacter.Options.LanguageBookShort(objXmlArt["source"].InnerText);
			string strPage = objXmlArt["page"].InnerText;
			if (objXmlArt["altpage"] != null)
				strPage = objXmlArt["altpage"].InnerText;
			lblSource.Text = strBook + " " + strPage;

			tipTooltip.SetToolTip(lblSource, _objCharacter.Options.LanguageBookLong(objXmlArt["source"].InnerText) + " " + LanguageManager.Instance.GetString("String_Page") + " " + strPage);
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
		/// Martial Art that was selected in the dialogue.
		/// </summary>
		public string SelectedMartialArt
		{
			get
			{
				return _strSelectedMartialArt;
			}
		}

		/// <summary>
		/// Force a Martial Art to be selected.
		/// </summary>
		public string ForcedValue
		{
			set
			{
				_strForcedValue = value;
			}
		}
		#endregion

		#region Methods
		/// <summary>
		/// Accept the selected item and close the form.
		/// </summary>
		private void AcceptForm()
		{
			_strSelectedMartialArt = lstMartialArts.SelectedValue.ToString();
			this.DialogResult = DialogResult.OK;
		}
		#endregion
	}
}