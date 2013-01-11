using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Chummer
{
    public partial class frmSelectBP : Form
    {
		private readonly Character _objCharacter;
		private readonly CharacterOptions _objOptions;
		private bool _blnUseCurrentValues = false;

		#region Control Events
		public frmSelectBP(Character objCharacter, bool blnUseCurrentValues = false)
        {
			_objCharacter = objCharacter;
			_objOptions = _objCharacter.Options;
			_blnUseCurrentValues = blnUseCurrentValues;
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

			cboBuildMethod.SelectedValue = _objOptions.BuildMethod;
			nudBP.Value = _objOptions.BuildPoints;
			nudMaxAvail.Value = _objOptions.Availability;

			toolTip1.SetToolTip(chkIgnoreRules, LanguageManager.Instance.GetString("Tip_SelectBP_IgnoreRules"));

			if (blnUseCurrentValues)
			{
				if (objCharacter.BuildMethod == CharacterBuildMethod.Karma)
				{
					cboBuildMethod.SelectedValue = "Karma";
					nudBP.Value = objCharacter.BuildKarma;
				}
				else
				{
					cboBuildMethod.SelectedValue = "BP";
					nudBP.Value = objCharacter.BuildPoints;
				}

				nudMaxAvail.Value = objCharacter.MaximumAvailability;

				cboBuildMethod.Enabled = false;
			}
        }

        private void cmdOK_Click(object sender, EventArgs e)
        {
			if (cboBuildMethod.SelectedValue.ToString() == "BP")
			{
				_objCharacter.BuildPoints = Convert.ToInt32(nudBP.Value);
				_objCharacter.NuyenMaximumBP = 50;
				_objCharacter.BuildMethod = CharacterBuildMethod.BP;
			}
			else if (cboBuildMethod.SelectedValue.ToString() == "Karma")
			{
				_objCharacter.BuildPoints = 0;
				_objCharacter.BuildKarma = Convert.ToInt32(nudBP.Value);
				_objCharacter.NuyenMaximumBP = 100;
				_objCharacter.BuildMethod = CharacterBuildMethod.Karma;
			}
			_objCharacter.IgnoreRules = chkIgnoreRules.Checked;
			_objCharacter.MaximumAvailability = Convert.ToInt32(nudMaxAvail.Value);
            this.DialogResult = DialogResult.OK;
        }

		private void cmdCancel_Click(object sender, EventArgs e)
		{
			this.DialogResult = DialogResult.Cancel;
			this.Close();
		}

		private void cboBuildMethod_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (cboBuildMethod.SelectedValue.ToString() == "BP")
			{
				if (_objOptions.BuildMethod == "BP")
				{
					lblDescription.Text = LanguageManager.Instance.GetString("String_SelectBP_BPSummary").Replace("{0}", _objOptions.BuildPoints.ToString());
					if (!_blnUseCurrentValues)
						nudBP.Value = _objOptions.BuildPoints;
				}
				else
				{
					lblDescription.Text = LanguageManager.Instance.GetString("String_SelectBP_BPSummary").Replace("{0}", "400");
					if (!_blnUseCurrentValues)
						nudBP.Value = 400;
				}
			}
			else if (cboBuildMethod.SelectedValue.ToString() == "Karma")
			{
				if (_objOptions.BuildMethod == "Karma")
				{
					lblDescription.Text = LanguageManager.Instance.GetString("String_SelectBP_KarmaSummary").Replace("{0}", _objOptions.BuildPoints.ToString());
					if (!_blnUseCurrentValues)
						nudBP.Value = _objOptions.BuildPoints;
				}
				else
				{
					lblDescription.Text = LanguageManager.Instance.GetString("String_SelectBP_KarmaSummary").Replace("{0}", "750");
					if (!_blnUseCurrentValues)
						nudBP.Value = 750;
				}
			}
		}

		private void frmSelectBP_Load(object sender, EventArgs e)
		{
			this.Height = cmdOK.Bottom + 40;
			cboBuildMethod_SelectedIndexChanged(this, e);
		}
		#endregion
	}
}