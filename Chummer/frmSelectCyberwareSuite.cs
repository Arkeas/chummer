using System;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using System.Xml.XPath;

namespace Chummer
{
	public partial class frmSelectCyberwareSuite : Form
	{
		private string _strSelectedSuite = "";
		private double _dblCharacterESSModifier = 1.0;
		private Improvement.ImprovementSource _objSource = Improvement.ImprovementSource.Cyberware;
		private string _strType = "cyberware";

		private XmlDocument _objXmlDocument = new XmlDocument();

		#region Control events
		public frmSelectCyberwareSuite(Improvement.ImprovementSource objSource)
		{
			InitializeComponent();
			_objSource = objSource;
			LanguageManager.Instance.Load(GlobalOptions.Instance.Language, this);

			if (_objSource == Improvement.ImprovementSource.Cyberware)
				_strType = "cyberware";
			else
			{
				_strType = "bioware";
				this.Text = LanguageManager.Instance.GetString("Title_SelectBiowareSuite");
				lblCyberwareLabel.Text = LanguageManager.Instance.GetString("Label_SelectBiowareSuite_PartsInSuite");
			}
		}

		private void cmdOK_Click(object sender, EventArgs e)
		{
			if (lstCyberware.Text != "")
				AcceptForm();
		}

		private void cmdCancel_Click(object sender, EventArgs e)
		{
			this.DialogResult = DialogResult.Cancel;
		}

		private void lstCyberware_DoubleClick(object sender, EventArgs e)
		{
			if (lstCyberware.Text != "")
				AcceptForm();
		}

		private void frmSelectCyberwareSuite_Load(object sender, EventArgs e)
		{
			foreach (Label objLabel in this.Controls.OfType<Label>())
			{
				if (objLabel.Text.StartsWith("["))
					objLabel.Text = "";
			}

			_objXmlDocument = XmlManager.Instance.Load(_strType + ".xml");

			XmlNodeList objXmlSuiteList = _objXmlDocument.SelectNodes("/chummer/suites/suite");

			foreach (XmlNode objXmlSuite in objXmlSuiteList)
			{
				lstCyberware.Items.Add(objXmlSuite["name"].InnerText);
			}
		}

		private void lstCyberware_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (lstCyberware.Text == "")
				return;

			XmlNode objXmlSuite = _objXmlDocument.SelectSingleNode("/chummer/suites/suite[name = \"" + lstCyberware.Text + "\"]");
			lblGrade.Text = objXmlSuite["grade"].InnerText;

			double dblTotalESS = 0.0;
			double dblTotalCost = 0.0;

			// Retrieve the information for the selected Grade.
			double dblCostMultiplier = 1.0;
			double dblESSMultiplier = 1.0;
			XmlNode objXmlGrade = _objXmlDocument.SelectSingleNode("/chummer/grades/grade[name = \"" + CyberwareGradeName(objXmlSuite["grade"].InnerText) + "\"]");
			dblCostMultiplier = Convert.ToDouble(objXmlGrade["cost"].InnerText, GlobalOptions.Instance.CultureInfo);
			dblESSMultiplier = Convert.ToDouble(objXmlGrade["ess"].InnerText, GlobalOptions.Instance.CultureInfo) - 0.1;

			XPathNavigator nav = _objXmlDocument.CreateNavigator();
			lblCyberware.Text = "";

			// Run through all of the items in the Suite list.
			foreach (XmlNode objXmlItem in objXmlSuite.SelectNodes(_strType + "s/" + _strType))
			{
				int intRating = 0;
				lblCyberware.Text += objXmlItem["name"].InnerText;
				if (objXmlItem["rating"] != null)
				{
					intRating = Convert.ToInt32(objXmlItem["rating"].InnerText);
					lblCyberware.Text += " " + LanguageManager.Instance.GetString("String_Rating") + " " + intRating.ToString();
				}
				lblCyberware.Text += "\n";

				// Retrieve the information for the current piece of Cyberware and add it to the ESS and Cost totals.
				XmlNode objXmlCyberware = _objXmlDocument.SelectSingleNode("/chummer/" + _strType + "s/" + _strType + "[name = \"" + objXmlItem["name"].InnerText + "\"]");
				// Essence.
				if (objXmlCyberware["ess"].InnerText.StartsWith("FixedValues"))
				{
					string[] strValues = objXmlCyberware["ess"].InnerText.Replace("FixedValues(", string.Empty).Replace(")", string.Empty).Split(',');
					decimal decESS = Convert.ToDecimal(strValues[Convert.ToInt32(intRating) - 1], GlobalOptions.Instance.CultureInfo);
					dblTotalESS += Math.Round(((Convert.ToDouble(decESS, GlobalOptions.Instance.CultureInfo) * dblESSMultiplier) * _dblCharacterESSModifier), 2, MidpointRounding.AwayFromZero);
				}
				else
				{
					XPathExpression xprEssence = nav.Compile(objXmlCyberware["ess"].InnerText.Replace("Rating", intRating.ToString()));
					dblTotalESS += Math.Round(((Convert.ToDouble(nav.Evaluate(xprEssence), GlobalOptions.Instance.CultureInfo) * dblESSMultiplier) * _dblCharacterESSModifier), 2, MidpointRounding.AwayFromZero);
				}
				
				// Cost
				try
				{
					XPathExpression xprCost = nav.Compile(objXmlCyberware["cost"].InnerText.Replace("Rating", intRating.ToString()));
					dblTotalCost += (Convert.ToDouble(nav.Evaluate(xprCost), GlobalOptions.Instance.CultureInfo) * dblCostMultiplier);
				}
				catch
				{
					if (objXmlCyberware["cost"].InnerText.StartsWith("FixedValues"))
					{
						string[] strValues = objXmlCyberware["cost"].InnerText.Replace("FixedValues(", string.Empty).Replace(")", string.Empty).Split(',');
						dblTotalCost += (Convert.ToDouble(strValues[Convert.ToInt32(intRating) - 1], GlobalOptions.Instance.CultureInfo) * dblCostMultiplier);
					}
					else
					{
						if (objXmlCyberware["cost"].InnerText.StartsWith("Variable"))
						{
							int intMin = 0;
							int intMax = 0;
							string strCost = objXmlCyberware["cost"].InnerText.Replace("Variable(", string.Empty).Replace(")", string.Empty);
							if (strCost.Contains("-"))
							{
								string[] strValues = strCost.Split('-');
								intMin = Convert.ToInt32(strValues[0]);
								intMax = Convert.ToInt32(strValues[1]);
							}
							else
								intMin = Convert.ToInt32(strCost.Replace("+", string.Empty));

							dblTotalCost += (Convert.ToDouble(intMin, GlobalOptions.Instance.CultureInfo) * dblCostMultiplier);
						}
						else
							dblTotalCost += (Convert.ToDouble(objXmlCyberware["cost"].InnerText, GlobalOptions.Instance.CultureInfo) * dblCostMultiplier);
					}
				}

				foreach (XmlNode objXmlChild in objXmlItem.SelectNodes(_strType + "s/" + _strType))
				{
					// Retrieve the information for the current piece of Cyberware and add it to the ESS and Cost totals.
					XmlNode objXmlChildCyberware = _objXmlDocument.SelectSingleNode("/chummer/" + _strType + "s/" + _strType + "[name = \"" + objXmlChild["name"].InnerText + "\"]");

					lblCyberware.Text += "     " + objXmlChildCyberware["name"].InnerText;
					int intChildRating = 0;
					if (objXmlChild["rating"] != null)
					{
						intChildRating = Convert.ToInt32(objXmlChild["rating"].InnerText);
						lblCyberware.Text += " " + LanguageManager.Instance.GetString("String_Rating") + " " + intChildRating.ToString();
					}
					lblCyberware.Text += "\n";

					// Cost
					try
					{
						XPathExpression xprCost = nav.Compile(objXmlChildCyberware["cost"].InnerText.Replace("Rating", intChildRating.ToString()));
						dblTotalCost += (Convert.ToDouble(nav.Evaluate(xprCost), GlobalOptions.Instance.CultureInfo) * dblCostMultiplier);
					}
					catch
					{
						if (objXmlChildCyberware["cost"].InnerText.StartsWith("FixedValues"))
						{
							string[] strValues = objXmlChildCyberware["cost"].InnerText.Replace("FixedValues(", string.Empty).Replace(")", string.Empty).Split(',');
							dblTotalCost += (Convert.ToDouble(strValues[Convert.ToInt32(intChildRating) - 1], GlobalOptions.Instance.CultureInfo) * dblCostMultiplier);
						}
						else
						{
							if (objXmlChildCyberware["cost"].InnerText.StartsWith("Variable"))
							{
								int intMin = 0;
								int intMax = 0;
								string strCost = objXmlChildCyberware["cost"].InnerText.Replace("Variable(", string.Empty).Replace(")", string.Empty);
								if (strCost.Contains("-"))
								{
									string[] strValues = strCost.Split('-');
									intMin = Convert.ToInt32(strValues[0]);
									intMax = Convert.ToInt32(strValues[1]);
								}
								else
									intMin = Convert.ToInt32(strCost.Replace("+", string.Empty));

								dblTotalCost += (Convert.ToDouble(intMin, GlobalOptions.Instance.CultureInfo) * dblCostMultiplier);
							}
							else
								dblTotalCost += (Convert.ToDouble(objXmlChildCyberware["cost"].InnerText, GlobalOptions.Instance.CultureInfo) * dblCostMultiplier);
						}
					}
				}
			}

			// Once we have the totals, multiply these by 0.9 which is the Essence and Cost modifier for Cyberware Suites.
			dblTotalCost *= 0.9;

			lblEssence.Text = Math.Round(dblTotalESS, 2).ToString();
			lblCost.Text = String.Format("{0:###,###,##0¥}", dblTotalCost);
		}
		#endregion

		#region Properties
		/// <summary>
		/// Essence cost multiplier from the character.
		/// </summary>
		public double CharacterESSMultiplier
		{
			set
			{
				_dblCharacterESSModifier = value;
			}
		}

		/// <summary>
		/// Name of Suite that was selected in the dialogue.
		/// </summary>
		public string SelectedSuite
		{
			get
			{
				return _strSelectedSuite;
			}
		}
		#endregion

		#region Methods
		/// <summary>
		/// Accept the selected item and close the form.
		/// </summary>
		private void AcceptForm()
		{
			_strSelectedSuite = lstCyberware.Text;
			this.DialogResult = DialogResult.OK;
		}

		/// <summary>
		/// Convert the grade string found in the file to the name of the Grade found in the Cyberware.xml file.
		/// </summary>
		/// <param name="strValue">Grade from the Cyberware Suite.</param>
		private string CyberwareGradeName(string strValue)
		{
			switch (strValue)
			{
				case "Alphaware":
					return "Alphaware";
				case "Betaware":
					return "Betaware";
				case "Deltaware":
					return "Deltaware";
				case "Standard (Second-Hand)":
				case "StandardSecondHand":
					return "Standard (Second-Hand)";
				case "Alphaware (Second-Hand)":
				case "AlphawareSecondHand":
					return "Alphaware (Second-Hand)";
				case "StandardAdapsin":
					return "Standard (Adapsin)";
				case "AlphawareAdapsin":
					return "Alphaware (Adapsin)";
				case "BetawareAdapsin":
					return "Betaware (Adapsin)";
				case "DeltawareAdapsin":
					return "Deltaware (Adapsin)";
				case "Standard (Second-Hand) (Adapsin)":
				case "StandardSecondHandAdapsin":
					return "Standard (Second-Hand) (Adapsin)";
				case "Alphaware (Second-Hand) (Adapsin)":
				case "AlphawareSecondHandAdapsin":
					return "Alphaware (Second-Hand) (Adapsin)";
				default:
					return "Standard";
			}
		}
		#endregion
	}
}