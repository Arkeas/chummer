﻿using System;
using System.Drawing;
using System.Windows.Forms;

// PowerRatingChanged Event Handler.
public delegate void PowerRatingChangedHandler(Object sender);
// DeletePower Event Handler;
public delegate void DeletePowerHandler(Object sender);

namespace Chummer
{
    public partial class PowerControl : UserControl
    {
		private Power _objPower;

        // Events.
        public event PowerRatingChangedHandler PowerRatingChanged;
        public event DeletePowerHandler DeletePower;


		#region Control Events
		public PowerControl()
        {
            InitializeComponent();
			LanguageManager.Instance.Load(GlobalOptions.Instance.Language, this);
			MoveControls();
        }

		private void PowerControl_Load(object sender, EventArgs e)
		{
			this.Width = cmdDelete.Left + cmdDelete.Width;
		}

		private void nudRating_ValueChanged(object sender, EventArgs e)
        {
            // Raise the PowerRatingChanged Event when the NumericUpDown's Value changes.
            // The entire PowerControl is passed as an argument so the handling event can evaluate its contents.
			_objPower.Rating = nudRating.Value;
            PowerRatingChanged(this);
			UpdatePointsPerLevel();
        }

        private void cmdDelete_Click(object sender, EventArgs e)
        {
            // Raise the DeletePower Event when the user has confirmed their desire to delete the Power.
            // The entire PowerControl is passed as an argument so the handling event can evaluate its contents.
			DeletePower(this);
        }

		private void chkDiscounted_CheckedChanged(object sender, EventArgs e)
		{
			// Raise the PowerRatingChanged Event when the user has changed the discounted status.
			// The entire PowerControl is passed as an argument so the handling event can evaluate its contents.
			_objPower.DiscountedAdeptWay = chkDiscountedAdeptWay.Checked;
			PowerRatingChanged(this);
			UpdatePointsPerLevel();
		}

		private void chkDiscountedGeas_CheckedChanged(object sender, EventArgs e)
		{
			// Raise the PowerRatingChanged Event when the user has changed the Geas discounted status.
			// The entire PowerControl is passed as an argument so the handling event can evaluate its contents.
			_objPower.DiscountedGeas = chkDiscountedGeas.Checked;
			PowerRatingChanged(this);
			UpdatePointsPerLevel();
		}

		private void imgNotes_Click(object sender, EventArgs e)
		{
			frmNotes frmPowerNotes = new frmNotes();
			frmPowerNotes.Notes = _objPower.Notes;
			frmPowerNotes.ShowDialog(this);

			if (frmPowerNotes.DialogResult == DialogResult.OK)
				_objPower.Notes = frmPowerNotes.Notes;

			string strTooltip = LanguageManager.Instance.GetString("Tip_Power_EditNotes");
			if (_objPower.Notes != string.Empty)
				strTooltip += "\n\n" + _objPower.Notes;
			tipTooltip.SetToolTip(imgNotes, strTooltip);
		}
		#endregion

		#region Properties
		/// <summary>
		/// The Power object this control is linked to.
		/// </summary>
		public Power PowerObject
		{
			get
			{
				return _objPower;
			}
			set
			{
				_objPower = value;

				string strTooltip = LanguageManager.Instance.GetString("Tip_Power_EditNotes");
				if (_objPower.Notes != string.Empty)
					strTooltip += "\n\n" + _objPower.Notes;
				tipTooltip.SetToolTip(imgNotes, strTooltip);
			}
		}

        /// <summary>
        /// Power name.
        /// </summary>
        public string PowerName
        {
            get
            {
				return _objPower.Name;
            }
            set
            {
				_objPower.Name = value;
				lblPowerName.Text = _objPower.FullName;

				RefreshTooltip();
            }
        }

        /// <summary>
        /// Extra Power information (selected Attribute or Skill name).
        /// </summary>
        public string Extra
        {
            get
            {
				return _objPower.Extra;
            }
            set
            {
				_objPower.Extra = value;
				lblPowerName.Text = _objPower.FullName;
            }
        }

        /// <summary>
        /// Power level.
        /// </summary>
        public int PowerLevel
        {
            get
            {
                return Convert.ToInt32(nudRating.Value);
            }
            set
            {
				_objPower.Rating = value;
                nudRating.Value = value;
                UpdatePointsPerLevel();
            }
        }

        /// <summary>
        /// Is the Power level enabled?
        /// </summary>
        public bool LevelEnabled
        {
            get
            {
                return nudRating.Enabled;
            }
            set
            {
				_objPower.LevelsEnabled = value;
                nudRating.Enabled = value;
                UpdatePointsPerLevel();
            }
        }

        /// <summary>
        /// The number of Power Points this Power costs per level.
        /// </summary>
        public decimal PointsPerLevel
        {
            get
            {
				return _objPower.CalculatedPointsPerLevel;
            }
            set
            {
				_objPower.PointsPerLevel = value;
                UpdatePointsPerLevel();
            }
        }

        /// <summary>
        /// The Power's total cost in Power Points.
        /// </summary>
        public decimal PowerPoints
        {
            get
            {
				return _objPower.PowerPoints;
            }
        }

		/// <summary>
		/// Maximum number of levels allowed.
		/// </summary>
		public int MaxLevels
		{
			get
			{
				return Convert.ToInt32(nudRating.Maximum);
			}
			set
			{
				_objPower.MaxLevels = value;
				nudRating.Maximum = value;
			}
		}

		/// <summary>
		/// Whether or not the Power Cost is discounted by 25% for Adept Way.
		/// </summary>
		public bool DiscountedByAdeptWay
		{
			get
			{
				return _objPower.DiscountedAdeptWay;
			}
			set
			{
				_objPower.DiscountedAdeptWay = value;
				chkDiscountedAdeptWay.Checked = value;
			}
		}

		/// <summary>
		/// Whether or not the Power Cost is discounted by 25% for Geas.
		/// </summary>
		public bool DiscountedByGeas
		{
			get
			{
				return _objPower.DiscountedGeas;
			}
			set
			{
				_objPower.DiscountedGeas = value;
				chkDiscountedGeas.Checked = value;
			}
		}

		/// <summary>
		/// Whether or not the Discounted by Adept Way CheckBox is enabled.
		/// </summary>
		public bool DiscountAdeptWayEnabled
		{
			get
			{
				return chkDiscountedAdeptWay.Enabled;
			}
			set
			{
				chkDiscountedAdeptWay.Enabled = value;
			}
		}

		/// <summary>
		/// Whether or not the Discounted by Geas CheckBox is enabled.
		/// </summary>
		public bool DiscountGeasEnabled
		{
			get
			{
				return chkDiscountedGeas.Enabled;
			}
			set
			{
				chkDiscountedGeas.Enabled = value;
			}
		}
		#endregion

		#region Methods
		/// <summary>
        /// Update the Power Points display for the Control.
        /// </summary>
        public void UpdatePointsPerLevel()
        {
			string strCalculated = _objPower.CalculatedPointsPerLevel.ToString();
			string strPoints = _objPower.PowerPoints.ToString();

			while (strCalculated.EndsWith("0") && strCalculated.Length > 4)
				strCalculated = strCalculated.Substring(0, strCalculated.Length - 1);

			while (strPoints.EndsWith("0") && strPoints.Length > 4)
				strPoints = strPoints.Substring(0, strPoints.Length - 1);

			lblPowerPoints.Text = String.Format("{0} / {1} = {2}", strCalculated, LanguageManager.Instance.GetString("Label_Power_Level"), strPoints);
        }

		/// <summary>
		/// Refresh the maximum level for the Power based on the character's MAG Attribute.
		/// </summary>
		/// <param name="intMAG">MAG value.</param>
		public void RefreshMaximum(int intMAG)
		{
			if (_objPower.MaxLevels > 0)
				return;

			nudRating.Maximum = intMAG;
		}

		/// <summary>
		/// Refresh the tooltip for the control.
		/// </summary>
		public void RefreshTooltip()
		{
			string strTooltip = _objPower.CharacterObject.Options.LanguageBookLong(_objPower.Source) + " " + LanguageManager.Instance.GetString("String_Page") + " " + _objPower.Page;
			if (_objPower.DoubledPoints > 0)
				strTooltip += "\n" + LanguageManager.Instance.GetString("Tip_Power_DoublePoints").Replace("{0}", _objPower.DoubledPoints.ToString());
			tipTooltip.SetToolTip(lblPowerName, strTooltip);
		}

		private void MoveControls()
		{
			if (lblPowerName.Font.Size < 8.25f)
			{
				for (float i = 8.25f; i >= lblPowerName.Font.Size; i -= 0.25f)
					lblRating.Left -= 1;
			}

			nudRating.Left = lblRating.Left + lblRating.Width + 6;
			lblX.Left = nudRating.Left + nudRating.Width + 6;
			lblPowerPoints.Left = lblX.Left + lblX.Width + 6;
			lblDiscountLabel.Left = lblPowerPoints.Left + lblPowerPoints.Width + 26;
			chkDiscountedAdeptWay.Left = lblDiscountLabel.Left + lblDiscountLabel.Width + 6;
			chkDiscountedGeas.Left = chkDiscountedAdeptWay.Left + chkDiscountedAdeptWay.Width + 6;
			imgNotes.Left = chkDiscountedGeas.Left + chkDiscountedGeas.Width + 6;
			cmdDelete.Left = imgNotes.Left + imgNotes.Width + 6;

			if (cmdDelete.Left + cmdDelete.Width > this.Width)
			{
				lblPowerName.Font = new Font(lblPowerName.Font.FontFamily.Name, lblPowerName.Font.Size - 0.25f);
				lblRating.Font = lblPowerName.Font;
				nudRating.Font = lblPowerName.Font;
				lblX.Font = lblPowerName.Font;
				lblPowerPoints.Font = lblPowerName.Font;
				lblDiscountLabel.Font = lblPowerName.Font;
				chkDiscountedAdeptWay.Font = lblPowerName.Font;
				chkDiscountedGeas.Font = lblPowerName.Font;
				cmdDelete.Font = lblPowerName.Font;
				MoveControls();
			}
		}
		#endregion
    }
}