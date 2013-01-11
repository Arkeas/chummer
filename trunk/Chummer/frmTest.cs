﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.XPath;

namespace Chummer
{
	public partial class frmTest : Form
	{
		public frmTest()
		{
			InitializeComponent();
		}

		private void cmdTest_Click(object sender, EventArgs e)
		{
			txtOutput.Text = "";
			switch (cboTest.Text)
			{
				case "armor.xml":
					TestArmor();
					break;
				case "bioware.xml":
				case "cyberware.xml":
					TestCyberware(cboTest.Text);
					break;
				case "critters.xml":
					TestMetatype("critters.xml");
					break;
				case "gear.xml":
					TestGear();
					break;
				case "metatypes.xml":
					TestMetatype("metatypes.xml");
					break;
				case "qualities.xml":
					TestQuality();
					break;
				case "vehicles.xml":
					TestVehicles();
					break;
				case "weapons.xml":
					TestWeapons();
					break;
			}
			txtOutput.Text += "Done validation";
		}

		private void TestVehicles()
		{
			Character objCharacter = new Character();
			XmlDocument objXmlDocument = XmlManager.Instance.Load("vehicles.xml");
			pgbProgress.Minimum = 0;
			pgbProgress.Value = 0;
			pgbProgress.Maximum = objXmlDocument.SelectNodes("/chummer/vehicles/vehicle").Count;
			pgbProgress.Maximum += objXmlDocument.SelectNodes("/chummer/mods/mod").Count;

			// Vehicles.
			foreach (XmlNode objXmlGear in objXmlDocument.SelectNodes("/chummer/vehicles/vehicle"))
			{
				pgbProgress.Value++;
				Application.DoEvents();
				try
				{
					TreeNode objTempNode = new TreeNode();
					Vehicle objTemp = new Vehicle(objCharacter);
					objTemp.Create(objXmlGear, objTempNode, null, null, null, null, null);
					try
					{
						int objValue = objTemp.TotalCost;
					}
					catch
					{
						txtOutput.Text += objXmlGear["name"].InnerText + " failed TotalCost\n";
					}
					try
					{
						string objValue = objTemp.TotalAccel;
					}
					catch
					{
						txtOutput.Text += objXmlGear["name"].InnerText + " failed TotalAccel\n";
					}
					try
					{
						int objValue = objTemp.TotalArmor;
					}
					catch
					{
						txtOutput.Text += objXmlGear["name"].InnerText + " failed TotalArmor\n";
					}
					try
					{
						int objValue = objTemp.TotalBody;
					}
					catch
					{
						txtOutput.Text += objXmlGear["name"].InnerText + " failed TotalBody\n";
					}
					try
					{
						int objValue = objTemp.TotalHandling;
					}
					catch
					{
						txtOutput.Text += objXmlGear["name"].InnerText + " failed TotalHandling\n";
					}
					try
					{
						int objValue = objTemp.TotalSpeed;
					}
					catch
					{
						txtOutput.Text += objXmlGear["name"].InnerText + " failed TotalSpeed\n";
					}
					try
					{
						string objValue = objTemp.CalculatedAvail;
					}
					catch
					{
						txtOutput.Text += objXmlGear["name"].InnerText + " failed CalculatedAvail\n";
					}
				}
				catch
				{
					txtOutput.Text += objXmlGear["name"].InnerText + " general failure\n";
				}
			}

			// Vehicle Mods.
			foreach (XmlNode objXmlGear in objXmlDocument.SelectNodes("/chummer/mods/mod"))
			{
				pgbProgress.Value++;
				Application.DoEvents();
				try
				{
					TreeNode objTempNode = new TreeNode();
					VehicleMod objTemp = new VehicleMod(objCharacter);
					objTemp.Create(objXmlGear, objTempNode, 1);
					try
					{
						int objValue = objTemp.TotalCost;
					}
					catch
					{
						txtOutput.Text += objXmlGear["name"].InnerText + " failed TotalCost\n";
					}
					try
					{
						string objValue = objTemp.TotalAvail;
					}
					catch
					{
						txtOutput.Text += objXmlGear["name"].InnerText + " failed TotalAvail\n";
					}
					try
					{
						int objValue = objTemp.CalculatedSlots;
					}
					catch
					{
						txtOutput.Text += objXmlGear["name"].InnerText + " failed CalculatedSlots\n";
					}
				}
				catch
				{
					txtOutput.Text += objXmlGear["name"].InnerText + " general failure\n";
				}
			}
		}

		private void TestWeapons()
		{
			Character objCharacter = new Character();
			XmlDocument objXmlDocument = XmlManager.Instance.Load("weapons.xml");
			pgbProgress.Minimum = 0;
			pgbProgress.Value = 0;
			pgbProgress.Maximum = objXmlDocument.SelectNodes("/chummer/weapons/weapon").Count;
			pgbProgress.Maximum += objXmlDocument.SelectNodes("/chummer/accessories/accessory").Count;
			pgbProgress.Maximum += objXmlDocument.SelectNodes("/chummer/mods/mod").Count;

			// Weapons.
			foreach (XmlNode objXmlGear in objXmlDocument.SelectNodes("/chummer/weapons/weapon"))
			{
				pgbProgress.Value++;
				Application.DoEvents();
				try
				{
					TreeNode objTempNode = new TreeNode();
					Weapon objTemp = new Weapon(objCharacter);
					objTemp.Create(objXmlGear, objCharacter, objTempNode, null, null, null);
					try
					{
						int objValue = objTemp.TotalCost;
					}
					catch
					{
						txtOutput.Text += objXmlGear["name"].InnerText + " failed TotalCost\n";
					}
					try
					{
						string objValue = objTemp.TotalAP;
					}
					catch
					{
						txtOutput.Text += objXmlGear["name"].InnerText + " failed TotalAP\n";
					}
					try
					{
						string objValue = objTemp.TotalAvail;
					}
					catch
					{
						txtOutput.Text += objXmlGear["name"].InnerText + " failed TotalAvail\n";
					}
					try
					{
						string objValue = objTemp.TotalRC;
					}
					catch
					{
						txtOutput.Text += objXmlGear["name"].InnerText + " failed TotalRC\n";
					}
					try
					{
						int objValue = objTemp.TotalReach;
					}
					catch
					{
						txtOutput.Text += objXmlGear["name"].InnerText + " failed TotalReach\n";
					}
					try
					{
						string objValue = objTemp.CalculatedAmmo();
					}
					catch
					{
						txtOutput.Text += objXmlGear["name"].InnerText + " failed CalculatedAmmo\n";
					}
					try
					{
						string objValue = objTemp.CalculatedConcealability();
					}
					catch
					{
						txtOutput.Text += objXmlGear["name"].InnerText + " failed CalculatedConcealability\n";
					}
					try
					{
						string objValue = objTemp.CalculatedDamage();
					}
					catch
					{
						txtOutput.Text += objXmlGear["name"].InnerText + " failed CalculatedDamage\n";
					}
				}
				catch
				{
					txtOutput.Text += objXmlGear["name"].InnerText + " general failure\n";
				}
			}

			// Weapon Accessories.
			foreach (XmlNode objXmlGear in objXmlDocument.SelectNodes("/chummer/accessories/accessory"))
			{
				pgbProgress.Value++;
				Application.DoEvents();
				try
				{
					TreeNode objTempNode = new TreeNode();
					WeaponAccessory objTemp = new WeaponAccessory(objCharacter);
					objTemp.Create(objXmlGear, objTempNode, "");
					try
					{
						int objValue = objTemp.TotalCost;
					}
					catch
					{
						txtOutput.Text += objXmlGear["name"].InnerText + " failed CalculatedCost\n";
					}
					try
					{
						string objValue = objTemp.TotalAvail;
					}
					catch
					{
						txtOutput.Text += objXmlGear["name"].InnerText + " failed TotalAvail\n";
					}
				}
				catch
				{
					txtOutput.Text += objXmlGear["name"].InnerText + " general failure\n";
				}
			}

			// Weapon Mods.
			foreach (XmlNode objXmlGear in objXmlDocument.SelectNodes("/chummer/mods/mod"))
			{
				pgbProgress.Value++;
				Application.DoEvents();
				try
				{
					TreeNode objTempNode = new TreeNode();
					WeaponMod objTemp = new WeaponMod(objCharacter);
					objTemp.Create(objXmlGear, objTempNode);
					try
					{
						int objValue = objTemp.TotalCost;
					}
					catch
					{
						txtOutput.Text += objXmlGear["name"].InnerText + " failed CalculatedCost\n";
					}
					try
					{
						string objValue = objTemp.TotalAvail;
					}
					catch
					{
						txtOutput.Text += objXmlGear["name"].InnerText + " failed TotalAvail\n";
					}
				}
				catch
				{
					txtOutput.Text += objXmlGear["name"].InnerText + " general failure\n";
				}
			}
		}

		private void TestArmor()
		{
			Character objCharacter = new Character();
			XmlDocument objXmlDocument = XmlManager.Instance.Load("armor.xml");
			pgbProgress.Minimum = 0;
			pgbProgress.Value = 0;
			pgbProgress.Maximum = objXmlDocument.SelectNodes("/chummer/armors/armor").Count;
			pgbProgress.Maximum += objXmlDocument.SelectNodes("/chummer/mods/mod").Count;

			// Armor.
			foreach (XmlNode objXmlGear in objXmlDocument.SelectNodes("/chummer/armors/armor"))
			{
				pgbProgress.Value++;
				Application.DoEvents();
				try
				{
					TreeNode objTempNode = new TreeNode();
					Armor objTemp = new Armor(objCharacter);
					objTemp.Create(objXmlGear, objTempNode, null);
					try
					{
						int objValue = objTemp.TotalCost;
					}
					catch
					{
						txtOutput.Text += objXmlGear["name"].InnerText + " failed TotalCost\n";
					}
					try
					{
						int objValue = objTemp.TotalBallistic;
					}
					catch
					{
						txtOutput.Text += objXmlGear["name"].InnerText + " failed TotalBallistic\n";
					}
					try
					{
						int objValue = objTemp.TotalImpact;
					}
					catch
					{
						txtOutput.Text += objXmlGear["name"].InnerText + " failed TotalImpact\n";
					}
					try
					{
						string objValue = objTemp.TotalAvail;
					}
					catch
					{
						txtOutput.Text += objXmlGear["name"].InnerText + " failed TotalAvail\n";
					}
					try
					{
						string objValue = objTemp.CalculatedCapacity;
					}
					catch
					{
						txtOutput.Text += objXmlGear["name"].InnerText + " failed CalculatedCapacity\n";
					}
				}
				catch
				{
					txtOutput.Text += objXmlGear["name"].InnerText + " general failure\n";
				}
			}

			// Armor Mods.
			foreach (XmlNode objXmlGear in objXmlDocument.SelectNodes("/chummer/mods/mod"))
			{
				pgbProgress.Value++;
				Application.DoEvents();
				try
				{
					TreeNode objTempNode = new TreeNode();
					ArmorMod objTemp = new ArmorMod(objCharacter);
					List<Weapon> lstWeaopns = new List<Weapon>();
					List<TreeNode> lstNodes = new List<TreeNode>();
					objTemp.Create(objXmlGear, objTempNode, 1, lstWeaopns, lstNodes);
					try
					{
						string objValue = objTemp.TotalAvail;
					}
					catch
					{
						txtOutput.Text += objXmlGear["name"].InnerText + " failed TotalAvail\n";
					}
					try
					{
						string objValue = objTemp.CalculatedCapacity;
					}
					catch
					{
						txtOutput.Text += objXmlGear["name"].InnerText + " failed CalculatedCapacity\n";
					}
				}
				catch
				{
					txtOutput.Text += objXmlGear["name"].InnerText + " general failure\n";
				}
			}
		}

		private void TestGear()
		{
			Character objCharacter = new Character();
			XmlDocument objXmlDocument = XmlManager.Instance.Load("gear.xml");
			pgbProgress.Minimum = 0;
			pgbProgress.Value = 0;
			pgbProgress.Maximum = objXmlDocument.SelectNodes("/chummer/gears/gear").Count;

			// Gear.
			foreach (XmlNode objXmlGear in objXmlDocument.SelectNodes("/chummer/gears/gear"))
			{
				pgbProgress.Value++;
				Application.DoEvents();
				try
				{
					TreeNode objTempNode = new TreeNode();
					Gear objTemp = new Gear(objCharacter);
					List<Weapon> lstWeapons = new List<Weapon>();
					List<TreeNode> lstNodes = new List<TreeNode>();
					objTemp.Create(objXmlGear, objCharacter, objTempNode, 1, lstWeapons, lstNodes, "Blades");
					try
					{
						int objValue = objTemp.TotalCost;
					}
					catch
					{
						if (objXmlGear["category"].InnerText != "Mook")
							txtOutput.Text += objXmlGear["name"].InnerText + " failed TotalCost\n";
					}
					try
					{
						string objValue = objTemp.TotalAvail();
					}
					catch
					{
						txtOutput.Text += objXmlGear["name"].InnerText + " failed TotalAvail\n";
					}
					try
					{
						string objValue = objTemp.CalculatedArmorCapacity;
					}
					catch
					{
						txtOutput.Text += objXmlGear["name"].InnerText + " failed CalculatedArmorCapacity\n";
					}
					try
					{
						string objValue = objTemp.CalculatedCapacity;
					}
					catch
					{
						txtOutput.Text += objXmlGear["name"].InnerText + " failed CalculatedCapacity\n";
					}
					try
					{
						int objValue = objTemp.CalculatedCost;
					}
					catch
					{
						if (objXmlGear["category"].InnerText != "Mook")
							txtOutput.Text += objXmlGear["name"].InnerText + " failed CalculatedCost\n";
					}
				}
				catch
				{
					txtOutput.Text += objXmlGear["name"].InnerText + " general failure\n";
				}
			}
		}

		private void TestCyberware(string strFile)
		{
			string strPrefix = "";
			Improvement.ImprovementSource objSource = new Improvement.ImprovementSource();
			if (strFile == "bioware.xml")
			{
				strPrefix = "bioware";
				objSource = Improvement.ImprovementSource.Bioware;
			}
			else
			{
				strPrefix = "cyberware";
				objSource = Improvement.ImprovementSource.Cyberware;
			}

			Character objCharacter = new Character();
			XmlDocument objXmlDocument = XmlManager.Instance.Load(strFile);
			pgbProgress.Minimum = 0;
			pgbProgress.Value = 0;
			pgbProgress.Maximum = objXmlDocument.SelectNodes("/chummer/" + strPrefix + "s/" + strPrefix).Count;

			// Gear.
			foreach (XmlNode objXmlGear in objXmlDocument.SelectNodes("/chummer/" + strPrefix + "s/" + strPrefix))
			{
				pgbProgress.Value++;
				Application.DoEvents();
				try
				{
					TreeNode objTempNode = new TreeNode();
					Cyberware objTemp = new Cyberware(objCharacter);
					List<Weapon> lstWeapons = new List<Weapon>();
					List<TreeNode> lstNodes = new List<TreeNode>();
					objTemp.Create(objXmlGear, objCharacter, GlobalOptions.CyberwareGrades.GetGrade("Standard"), objSource, 1, objTempNode, lstWeapons, lstNodes);
					try
					{
						int objValue = objTemp.TotalCost;
					}
					catch
					{
						txtOutput.Text += objXmlGear["name"].InnerText + " failed TotalCost\n";
					}
					try
					{
						string objValue = objTemp.TotalAvail;
					}
					catch
					{
						txtOutput.Text += objXmlGear["name"].InnerText + " failed TotalAvail\n";
					}
					try
					{
						int objValue = objTemp.TotalAgility;
					}
					catch
					{
						txtOutput.Text += objXmlGear["name"].InnerText + " failed TotalAgility\n";
					}
					try
					{
						int objValue = objTemp.TotalBody;
					}
					catch
					{
						txtOutput.Text += objXmlGear["name"].InnerText + " failed TotalBody\n";
					}
					try
					{
						int objValue = objTemp.TotalStrength;
					}
					catch
					{
						txtOutput.Text += objXmlGear["name"].InnerText + " failed TotalStrength\n";
					}
					try
					{
						string objValue = objTemp.CalculatedCapacity;
					}
					catch
					{
						txtOutput.Text += objXmlGear["name"].InnerText + " failed CalculatedCapacity\n";
					}
					try
					{
						decimal objValue = objTemp.CalculatedESS;
					}
					catch
					{
						txtOutput.Text += objXmlGear["name"].InnerText + " failed CalculatedESS\n";
					}
				}
				catch
				{
					txtOutput.Text += objXmlGear["name"].InnerText + " general failure\n";
				}
			}
		}

		private void TestQuality()
		{
			Character objCharacter = new Character();
			XmlDocument objXmlDocument = XmlManager.Instance.Load("qualities.xml");
			pgbProgress.Minimum = 0;
			pgbProgress.Value = 0;
			pgbProgress.Maximum = objXmlDocument.SelectNodes("/chummer/qualities/quality").Count;

			// Qualities.
			foreach (XmlNode objXmlGear in objXmlDocument.SelectNodes("/chummer/qualities/quality"))
			{
				pgbProgress.Value++;
				Application.DoEvents();
				try
				{
					TreeNode objTempNode = new TreeNode();
					List<Weapon> objWeapons = new List<Weapon>();
					List<TreeNode> objWeaponNodes = new List<TreeNode>();
					Quality objTemp = new Quality(objCharacter);
					objTemp.Create(objXmlGear, objCharacter, QualitySource.Selected, objTempNode, objWeapons, objWeaponNodes);
				}
				catch
				{
					txtOutput.Text += objXmlGear["name"].InnerText + " general failure\n";
				}
			}
		}

		void TestMetatype(string strFile)
		{
			XmlDocument objXmlDocument = XmlManager.Instance.Load(strFile);

			pgbProgress.Minimum = 0;
			pgbProgress.Value = 0;
			pgbProgress.Maximum = objXmlDocument.SelectNodes("/chummer/metatypes/metatype").Count;

			foreach (XmlNode objXmlMetatype in objXmlDocument.SelectNodes("/chummer/metatypes/metatype"))
			{
				pgbProgress.Value++;
				Application.DoEvents();

				objXmlDocument = XmlManager.Instance.Load(strFile);
				Character _objCharacter = new Character();
				ImprovementManager objImprovementManager = new ImprovementManager(_objCharacter);

				try
				{
					int intForce = 0;
					if (objXmlMetatype["forcecreature"] != null)
						intForce = 1;

					// Set Metatype information.
					if (strFile != "critters.xml" || objXmlMetatype["name"].InnerText == "Ally Spirit")
					{
						_objCharacter.BOD.AssignLimits(ExpressionToString(objXmlMetatype["bodmin"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["bodmax"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["bodaug"].InnerText, intForce, 0));
						_objCharacter.AGI.AssignLimits(ExpressionToString(objXmlMetatype["agimin"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["agimax"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["agiaug"].InnerText, intForce, 0));
						_objCharacter.REA.AssignLimits(ExpressionToString(objXmlMetatype["reamin"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["reamax"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["reaaug"].InnerText, intForce, 0));
						_objCharacter.STR.AssignLimits(ExpressionToString(objXmlMetatype["strmin"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["strmax"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["straug"].InnerText, intForce, 0));
						_objCharacter.CHA.AssignLimits(ExpressionToString(objXmlMetatype["chamin"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["chamax"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["chaaug"].InnerText, intForce, 0));
						_objCharacter.INT.AssignLimits(ExpressionToString(objXmlMetatype["intmin"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["intmax"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["intaug"].InnerText, intForce, 0));
						_objCharacter.LOG.AssignLimits(ExpressionToString(objXmlMetatype["logmin"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["logmax"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["logaug"].InnerText, intForce, 0));
						_objCharacter.WIL.AssignLimits(ExpressionToString(objXmlMetatype["wilmin"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["wilmax"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["wilaug"].InnerText, intForce, 0));
						_objCharacter.INI.AssignLimits(ExpressionToString(objXmlMetatype["inimin"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["inimax"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["iniaug"].InnerText, intForce, 0));
						_objCharacter.MAG.AssignLimits(ExpressionToString(objXmlMetatype["magmin"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["magmax"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["magaug"].InnerText, intForce, 0));
						_objCharacter.RES.AssignLimits(ExpressionToString(objXmlMetatype["resmin"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["resmax"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["resaug"].InnerText, intForce, 0));
						_objCharacter.EDG.AssignLimits(ExpressionToString(objXmlMetatype["edgmin"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["edgmax"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["edgaug"].InnerText, intForce, 0));
						_objCharacter.ESS.AssignLimits(ExpressionToString(objXmlMetatype["essmin"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["essmax"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["essaug"].InnerText, intForce, 0));
					}
					else
					{
						int intMinModifier = -3;
						if (objXmlMetatype["category"].InnerText == "Mutant Critters")
							intMinModifier = 0;
						_objCharacter.BOD.AssignLimits(ExpressionToString(objXmlMetatype["bodmin"].InnerText, intForce, intMinModifier), ExpressionToString(objXmlMetatype["bodmin"].InnerText, intForce, 3), ExpressionToString(objXmlMetatype["bodmin"].InnerText, intForce, 3));
						_objCharacter.AGI.AssignLimits(ExpressionToString(objXmlMetatype["agimin"].InnerText, intForce, intMinModifier), ExpressionToString(objXmlMetatype["agimin"].InnerText, intForce, 3), ExpressionToString(objXmlMetatype["agimin"].InnerText, intForce, 3));
						_objCharacter.REA.AssignLimits(ExpressionToString(objXmlMetatype["reamin"].InnerText, intForce, intMinModifier), ExpressionToString(objXmlMetatype["reamin"].InnerText, intForce, 3), ExpressionToString(objXmlMetatype["reamin"].InnerText, intForce, 3));
						_objCharacter.STR.AssignLimits(ExpressionToString(objXmlMetatype["strmin"].InnerText, intForce, intMinModifier), ExpressionToString(objXmlMetatype["strmin"].InnerText, intForce, 3), ExpressionToString(objXmlMetatype["strmin"].InnerText, intForce, 3));
						_objCharacter.CHA.AssignLimits(ExpressionToString(objXmlMetatype["chamin"].InnerText, intForce, intMinModifier), ExpressionToString(objXmlMetatype["chamin"].InnerText, intForce, 3), ExpressionToString(objXmlMetatype["chamin"].InnerText, intForce, 3));
						_objCharacter.INT.AssignLimits(ExpressionToString(objXmlMetatype["intmin"].InnerText, intForce, intMinModifier), ExpressionToString(objXmlMetatype["intmin"].InnerText, intForce, 3), ExpressionToString(objXmlMetatype["intmin"].InnerText, intForce, 3));
						_objCharacter.LOG.AssignLimits(ExpressionToString(objXmlMetatype["logmin"].InnerText, intForce, intMinModifier), ExpressionToString(objXmlMetatype["logmin"].InnerText, intForce, 3), ExpressionToString(objXmlMetatype["logmin"].InnerText, intForce, 3));
						_objCharacter.WIL.AssignLimits(ExpressionToString(objXmlMetatype["wilmin"].InnerText, intForce, intMinModifier), ExpressionToString(objXmlMetatype["wilmin"].InnerText, intForce, 3), ExpressionToString(objXmlMetatype["wilmin"].InnerText, intForce, 3));
						_objCharacter.INI.AssignLimits(ExpressionToString(objXmlMetatype["inimin"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["inimax"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["iniaug"].InnerText, intForce, 0));
						_objCharacter.MAG.AssignLimits(ExpressionToString(objXmlMetatype["magmin"].InnerText, intForce, intMinModifier), ExpressionToString(objXmlMetatype["magmin"].InnerText, intForce, 3), ExpressionToString(objXmlMetatype["magmin"].InnerText, intForce, 3));
						_objCharacter.RES.AssignLimits(ExpressionToString(objXmlMetatype["resmin"].InnerText, intForce, intMinModifier), ExpressionToString(objXmlMetatype["resmin"].InnerText, intForce, 3), ExpressionToString(objXmlMetatype["resmin"].InnerText, intForce, 3));
						_objCharacter.EDG.AssignLimits(ExpressionToString(objXmlMetatype["edgmin"].InnerText, intForce, intMinModifier), ExpressionToString(objXmlMetatype["edgmin"].InnerText, intForce, 3), ExpressionToString(objXmlMetatype["edgmin"].InnerText, intForce, 3));
						_objCharacter.ESS.AssignLimits(ExpressionToString(objXmlMetatype["essmin"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["essmax"].InnerText, intForce, 0), ExpressionToString(objXmlMetatype["essaug"].InnerText, intForce, 0));
					}

					// If we're working with a Critter, set the Attributes to their default values.
					if (strFile == "critters.xml")
					{
						_objCharacter.BOD.Value = Convert.ToInt32(ExpressionToString(objXmlMetatype["bodmin"].InnerText, intForce, 0));
						_objCharacter.AGI.Value = Convert.ToInt32(ExpressionToString(objXmlMetatype["agimin"].InnerText, intForce, 0));
						_objCharacter.REA.Value = Convert.ToInt32(ExpressionToString(objXmlMetatype["reamin"].InnerText, intForce, 0));
						_objCharacter.STR.Value = Convert.ToInt32(ExpressionToString(objXmlMetatype["strmin"].InnerText, intForce, 0));
						_objCharacter.CHA.Value = Convert.ToInt32(ExpressionToString(objXmlMetatype["chamin"].InnerText, intForce, 0));
						_objCharacter.INT.Value = Convert.ToInt32(ExpressionToString(objXmlMetatype["intmin"].InnerText, intForce, 0));
						_objCharacter.LOG.Value = Convert.ToInt32(ExpressionToString(objXmlMetatype["logmin"].InnerText, intForce, 0));
						_objCharacter.WIL.Value = Convert.ToInt32(ExpressionToString(objXmlMetatype["wilmin"].InnerText, intForce, 0));
						_objCharacter.MAG.Value = Convert.ToInt32(ExpressionToString(objXmlMetatype["magmin"].InnerText, intForce, 0));
						_objCharacter.RES.Value = Convert.ToInt32(ExpressionToString(objXmlMetatype["resmin"].InnerText, intForce, 0));
						_objCharacter.EDG.Value = Convert.ToInt32(ExpressionToString(objXmlMetatype["edgmin"].InnerText, intForce, 0));
						_objCharacter.ESS.Value = Convert.ToInt32(ExpressionToString(objXmlMetatype["essmax"].InnerText, intForce, 0));
					}

					// Sprites can never have Physical Attributes or WIL.
					if (objXmlMetatype["name"].InnerText.EndsWith("Sprite"))
					{
						_objCharacter.BOD.AssignLimits("0", "0", "0");
						_objCharacter.AGI.AssignLimits("0", "0", "0");
						_objCharacter.REA.AssignLimits("0", "0", "0");
						_objCharacter.STR.AssignLimits("0", "0", "0");
						_objCharacter.WIL.AssignLimits("0", "0", "0");
						_objCharacter.INI.MetatypeMinimum = Convert.ToInt32(ExpressionToString(objXmlMetatype["inimax"].InnerText, intForce, 0));
						_objCharacter.INI.MetatypeMaximum = Convert.ToInt32(ExpressionToString(objXmlMetatype["inimax"].InnerText, intForce, 0));
					}

					_objCharacter.Metatype = objXmlMetatype["name"].InnerText;
					_objCharacter.MetatypeCategory = objXmlMetatype["category"].InnerText;
					_objCharacter.Metavariant = "";
					_objCharacter.MetatypeBP = 400;

					if (objXmlMetatype["movement"] != null)
						_objCharacter.Movement = objXmlMetatype["movement"].InnerText;
					// Load the Qualities file.
					XmlDocument objXmlQualityDocument = XmlManager.Instance.Load("qualities.xml");

					// Determine if the Metatype has any bonuses.
					if (objXmlMetatype.InnerXml.Contains("bonus"))
						objImprovementManager.CreateImprovements(Improvement.ImprovementSource.Metatype, objXmlMetatype["name"].InnerText, objXmlMetatype.SelectSingleNode("bonus"), false, 1, objXmlMetatype["name"].InnerText);

					// Create the Qualities that come with the Metatype.
					foreach (XmlNode objXmlQualityItem in objXmlMetatype.SelectNodes("qualities/positive/quality"))
					{
						XmlNode objXmlQuality = objXmlQualityDocument.SelectSingleNode("/chummer/qualities/quality[name = \"" + objXmlQualityItem.InnerText + "\"]");
						TreeNode objNode = new TreeNode();
						List<Weapon> objWeapons = new List<Weapon>();
						List<TreeNode> objWeaponNodes = new List<TreeNode>();
						Quality objQuality = new Quality(_objCharacter);
						string strForceValue = "";
						if (objXmlQualityItem.Attributes["select"] != null)
							strForceValue = objXmlQualityItem.Attributes["select"].InnerText;
						QualitySource objSource = new QualitySource();
						objSource = QualitySource.Metatype;
						if (objXmlQualityItem.Attributes["removable"] != null)
							objSource = QualitySource.MetatypeRemovable;
						objQuality.Create(objXmlQuality, _objCharacter, objSource, objNode, objWeapons, objWeaponNodes, strForceValue);
						_objCharacter.Qualities.Add(objQuality);
					}
					foreach (XmlNode objXmlQualityItem in objXmlMetatype.SelectNodes("qualities/negative/quality"))
					{
						XmlNode objXmlQuality = objXmlQualityDocument.SelectSingleNode("/chummer/qualities/quality[name = \"" + objXmlQualityItem.InnerText + "\"]");
						TreeNode objNode = new TreeNode();
						List<Weapon> objWeapons = new List<Weapon>();
						List<TreeNode> objWeaponNodes = new List<TreeNode>();
						Quality objQuality = new Quality(_objCharacter);
						string strForceValue = "";
						if (objXmlQualityItem.Attributes["select"] != null)
							strForceValue = objXmlQualityItem.Attributes["select"].InnerText;
						QualitySource objSource = new QualitySource();
						objSource = QualitySource.Metatype;
						if (objXmlQualityItem.Attributes["removable"] != null)
							objSource = QualitySource.MetatypeRemovable;
						objQuality.Create(objXmlQuality, _objCharacter, objSource, objNode, objWeapons, objWeaponNodes, strForceValue);
						_objCharacter.Qualities.Add(objQuality);
					}

					// Run through the character's Attributes one more time and make sure their value matches their minimum value.
					if (strFile == "metatypes.xml")
					{
						_objCharacter.BOD.Value = _objCharacter.BOD.TotalMinimum;
						_objCharacter.AGI.Value = _objCharacter.AGI.TotalMinimum;
						_objCharacter.REA.Value = _objCharacter.REA.TotalMinimum;
						_objCharacter.STR.Value = _objCharacter.STR.TotalMinimum;
						_objCharacter.CHA.Value = _objCharacter.CHA.TotalMinimum;
						_objCharacter.INT.Value = _objCharacter.INT.TotalMinimum;
						_objCharacter.LOG.Value = _objCharacter.LOG.TotalMinimum;
						_objCharacter.WIL.Value = _objCharacter.WIL.TotalMinimum;
					}

					// Add any Critter Powers the Metatype/Critter should have.
					XmlNode objXmlCritter = objXmlDocument.SelectSingleNode("/chummer/metatypes/metatype[name = \"" + _objCharacter.Metatype + "\"]");

					objXmlDocument = XmlManager.Instance.Load("critterpowers.xml");
					foreach (XmlNode objXmlPower in objXmlCritter.SelectNodes("powers/power"))
					{
						XmlNode objXmlCritterPower = objXmlDocument.SelectSingleNode("/chummer/powers/power[name = \"" + objXmlPower.InnerText + "\"]");
						TreeNode objNode = new TreeNode();
						CritterPower objPower = new CritterPower(_objCharacter);
						string strForcedValue = "";
						int intRating = 0;

						if (objXmlPower.Attributes["rating"] != null)
							intRating = Convert.ToInt32(objXmlPower.Attributes["rating"].InnerText);
						if (objXmlPower.Attributes["select"] != null)
							strForcedValue = objXmlPower.Attributes["select"].InnerText;

						objPower.Create(objXmlCritterPower, _objCharacter, objNode, intRating, strForcedValue);
						_objCharacter.CritterPowers.Add(objPower);
					}

					// Set the Skill Ratings for the Critter.
					foreach (XmlNode objXmlSkill in objXmlCritter.SelectNodes("skills/skill"))
					{
						if (objXmlSkill.InnerText.Contains("Exotic"))
						{
							Skill objExotic = new Skill(_objCharacter);
							objExotic.ExoticSkill = true;
							objExotic.Attribute = "AGI";
							if (objXmlSkill.Attributes["spec"] != null)
								objExotic.Specialization = objXmlSkill.Attributes["spec"].InnerText;
							if (Convert.ToInt32(ExpressionToString(objXmlSkill.Attributes["rating"].InnerText, Convert.ToInt32(intForce), 0)) > 6)
								objExotic.RatingMaximum = Convert.ToInt32(ExpressionToString(objXmlSkill.Attributes["rating"].InnerText, Convert.ToInt32(intForce), 0));
							objExotic.Rating = Convert.ToInt32(ExpressionToString(objXmlSkill.Attributes["rating"].InnerText, Convert.ToInt32(intForce), 0));
							objExotic.Name = objXmlSkill.InnerText;
							_objCharacter.Skills.Add(objExotic);
						}
						else
						{
							foreach (Skill objSkill in _objCharacter.Skills)
							{
								if (objSkill.Name == objXmlSkill.InnerText)
								{
									if (objXmlSkill.Attributes["spec"] != null)
										objSkill.Specialization = objXmlSkill.Attributes["spec"].InnerText;
									if (Convert.ToInt32(ExpressionToString(objXmlSkill.Attributes["rating"].InnerText, Convert.ToInt32(intForce), 0)) > 6)
										objSkill.RatingMaximum = Convert.ToInt32(ExpressionToString(objXmlSkill.Attributes["rating"].InnerText, Convert.ToInt32(intForce), 0));
									objSkill.Rating = Convert.ToInt32(ExpressionToString(objXmlSkill.Attributes["rating"].InnerText, Convert.ToInt32(intForce), 0));
									break;
								}
							}
						}
					}

					// Set the Skill Group Ratings for the Critter.
					foreach (XmlNode objXmlSkill in objXmlCritter.SelectNodes("skills/group"))
					{
						foreach (SkillGroup objSkill in _objCharacter.SkillGroups)
						{
							if (objSkill.Name == objXmlSkill.InnerText)
							{
								objSkill.RatingMaximum = Convert.ToInt32(ExpressionToString(objXmlSkill.Attributes["rating"].InnerText, Convert.ToInt32(intForce), 0));
								objSkill.Rating = Convert.ToInt32(ExpressionToString(objXmlSkill.Attributes["rating"].InnerText, Convert.ToInt32(intForce), 0));
								break;
							}
						}
					}

					// Set the Knowledge Skill Ratings for the Critter.
					foreach (XmlNode objXmlSkill in objXmlCritter.SelectNodes("skills/knowledge"))
					{
						Skill objKnowledge = new Skill(_objCharacter);
						objKnowledge.Name = objXmlSkill.InnerText;
						objKnowledge.KnowledgeSkill = true;
						if (objXmlSkill.Attributes["spec"] != null)
							objKnowledge.Specialization = objXmlSkill.Attributes["spec"].InnerText;
						objKnowledge.SkillCategory = objXmlSkill.Attributes["category"].InnerText;
						if (Convert.ToInt32(objXmlSkill.Attributes["rating"].InnerText) > 6)
							objKnowledge.RatingMaximum = Convert.ToInt32(objXmlSkill.Attributes["rating"].InnerText);
						objKnowledge.Rating = Convert.ToInt32(objXmlSkill.Attributes["rating"].InnerText);
						_objCharacter.Skills.Add(objKnowledge);
					}

					// If this is a Critter with a Force (which dictates their Skill Rating/Maximum Skill Rating), set their Skill Rating Maximums.
					if (intForce > 0)
					{
						int intMaxRating = intForce;
						// Determine the highest Skill Rating the Critter has.
						foreach (Skill objSkill in _objCharacter.Skills)
						{
							if (objSkill.RatingMaximum > intMaxRating)
								intMaxRating = objSkill.RatingMaximum;
						}

						// Now that we know the upper limit, set all of the Skill Rating Maximums to match.
						foreach (Skill objSkill in _objCharacter.Skills)
							objSkill.RatingMaximum = intMaxRating;
						foreach (SkillGroup objGroup in _objCharacter.SkillGroups)
							objGroup.RatingMaximum = intMaxRating;

						// Set the MaxSkillRating for the character so it can be used later when they add new Knowledge Skills or Exotic Skills.
						_objCharacter.MaxSkillRating = intMaxRating;
					}

					// Add any Complex Forms the Critter comes with (typically Sprites)
					XmlDocument objXmlProgramDocument = XmlManager.Instance.Load("programs.xml");
					foreach (XmlNode objXmlComplexForm in objXmlCritter.SelectNodes("complexforms/complexform"))
					{
						int intRating = 0;
						if (objXmlComplexForm.Attributes["rating"] != null)
							intRating = Convert.ToInt32(ExpressionToString(objXmlComplexForm.Attributes["rating"].InnerText, Convert.ToInt32(intForce), 0));
						string strForceValue = "";
						if (objXmlComplexForm.Attributes["select"] != null)
							strForceValue = objXmlComplexForm.Attributes["select"].InnerText;
						XmlNode objXmlProgram = objXmlProgramDocument.SelectSingleNode("/chummer/programs/program[name = \"" + objXmlComplexForm.InnerText + "\"]");
						TreeNode objNode = new TreeNode();
						TechProgram objProgram = new TechProgram(_objCharacter);
						objProgram.Create(objXmlProgram, _objCharacter, objNode, strForceValue);
						objProgram.Rating = intRating;
						_objCharacter.TechPrograms.Add(objProgram);

						// Add the Program Option if applicable.
						if (objXmlComplexForm.Attributes["option"] != null)
						{
							int intOptRating = 0;
							if (objXmlComplexForm.Attributes["optionrating"] != null)
								intOptRating = Convert.ToInt32(ExpressionToString(objXmlComplexForm.Attributes["optionrating"].InnerText, Convert.ToInt32(intForce), 0));
							string strOptForceValue = "";
							if (objXmlComplexForm.Attributes["optionselect"] != null)
								strOptForceValue = objXmlComplexForm.Attributes["optionselect"].InnerText;
							XmlNode objXmlOption = objXmlProgramDocument.SelectSingleNode("/chummer/options/option[name = \"" + objXmlComplexForm.Attributes["option"].InnerText + "\"]");
							TreeNode objNodeOpt = new TreeNode();
							TechProgramOption objOption = new TechProgramOption(_objCharacter);
							objOption.Create(objXmlOption, _objCharacter, objNodeOpt, strOptForceValue);
							objOption.Rating = intOptRating;
							objProgram.Options.Add(objOption);
						}
					}

					// Add any Gear the Critter comes with (typically Programs for A.I.s)
					XmlDocument objXmlGearDocument = XmlManager.Instance.Load("gear.xml");
					foreach (XmlNode objXmlGear in objXmlCritter.SelectNodes("gears/gear"))
					{
						int intRating = 0;
						if (objXmlGear.Attributes["rating"] != null)
							intRating = Convert.ToInt32(ExpressionToString(objXmlGear.Attributes["rating"].InnerText, Convert.ToInt32(intForce), 0));
						string strForceValue = "";
						if (objXmlGear.Attributes["select"] != null)
							strForceValue = objXmlGear.Attributes["select"].InnerText;
						XmlNode objXmlGearItem = objXmlGearDocument.SelectSingleNode("/chummer/gears/gear[name = \"" + objXmlGear.InnerText + "\"]");
						TreeNode objNode = new TreeNode();
						Gear objGear = new Gear(_objCharacter);
						List<Weapon> lstWeapons = new List<Weapon>();
						List<TreeNode> lstWeaponNodes = new List<TreeNode>();
						objGear.Create(objXmlGearItem, _objCharacter, objNode, intRating, lstWeapons, lstWeaponNodes, strForceValue);
						objGear.Cost = "0";
						objGear.Cost3 = "0";
						objGear.Cost6 = "0";
						objGear.Cost10 = "0";
						_objCharacter.Gear.Add(objGear);
					}

					// If this is a Mutant Critter, count up the number of Skill points they start with.
					if (_objCharacter.MetatypeCategory == "Mutant Critters")
					{
						foreach (Skill objSkill in _objCharacter.Skills)
							_objCharacter.MutantCritterBaseSkills += objSkill.Rating;
					}
				}
				catch
				{
					txtOutput.Text += _objCharacter.Metatype + " general failure\n";
				}
			}
		}

		/// <summary>
		/// Convert Force, 1D6, or 2D6 into a usable value.
		/// </summary>
		/// <param name="strIn">Expression to convert.</param>
		/// <param name="intForce">Force value to use.</param>
		/// <param name="intOffset">Dice offset.</param>
		/// <returns></returns>
		public string ExpressionToString(string strIn, int intForce, int intOffset)
		{
			int intValue = 0;
			XmlDocument objXmlDocument = new XmlDocument();
			XPathNavigator nav = objXmlDocument.CreateNavigator();
			XPathExpression xprAttribute = nav.Compile(strIn.Replace("/", " div ").Replace("F", intForce.ToString()).Replace("1D6", intForce.ToString()).Replace("2D6", intForce.ToString()));
			// This statement is wrapped in a try/catch since trying 1 div 2 results in an error with XSLT.
			try
			{
				intValue = Convert.ToInt32(nav.Evaluate(xprAttribute).ToString());
			}
			catch
			{
				intValue = 1;
			}
			intValue += intOffset;
			if (intForce > 0)
			{
				if (intValue < 1)
					intValue = 1;
			}
			else
			{
				if (intValue < 0)
					intValue = 0;
			}
			return intValue.ToString();
		}
	}
}