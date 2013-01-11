using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Drawing;

namespace Chummer
{
	public class CommonFunctions
	{
		#region Constructor and Instance
		private Character _objCharacter;

		public CommonFunctions(Character objCharacter)
		{
			_objCharacter = objCharacter;
		}

		CommonFunctions()
		{
		}
		#endregion

		#region Find Functions
		/// <summary>
		/// Locate a piece of Gear.
		/// </summary>
		/// <param name="strGuid">InternalId of the Gear to find.</param>
		/// <param name="lstGear">List of Gear to search.</param>
		public Gear FindGear(string strGuid, List<Gear> lstGear)
		{
			Gear objReturn = new Gear(_objCharacter);
			foreach (Gear objGear in lstGear)
			{
				if (objGear.InternalId == strGuid)
					objReturn = objGear;
				else
				{
					if (objGear.Children.Count > 0)
						objReturn = FindGear(strGuid, objGear.Children);
				}

				if (objReturn != null)
				{
					if (objReturn.InternalId != Guid.Empty.ToString() && objReturn.Name != "")
						return objReturn;
				}
			}

			objReturn = null;
			return objReturn;
		}

		/// <summary>
		/// Locate a piece of Gear by matching on its Weapon ID.
		/// </summary>
		/// <param name="strGuid">InternalId of the Weapon to find.</param>
		/// <param name="lstGear">List of Gear to search.</param>
		public Gear FindGearByWeaponID(string strGuid, List<Gear> lstGear)
		{
			Gear objReturn = new Gear(_objCharacter);
			foreach (Gear objGear in lstGear)
			{
				if (objGear.WeaponID == strGuid)
					objReturn = objGear;
				else
				{
					if (objGear.Children.Count > 0)
						objReturn = FindGearByWeaponID(strGuid, objGear.Children);
				}

				if (objReturn != null)
				{
					if (objReturn.InternalId != Guid.Empty.ToString() && objReturn.Name != "")
						return objReturn;
				}
			}

			objReturn = null;
			return objReturn;
		}

		/// <summary>
		/// Locate a piece of Gear within the character's Vehicles.
		/// </summary>
		/// <param name="strGuid">InternalId of the Gear to find.</param>
		/// <param name="lstVehicles">List of Vehicles to search.</param>
		/// <param name="objFoundVehicle">Vehicle that the Gear was found in.</param>
		public Gear FindVehicleGear(string strGuid, List<Vehicle> lstVehicles, out Vehicle objFoundVehicle)
		{
			Gear objReturn = new Gear(_objCharacter);
			foreach (Vehicle objVehicle in lstVehicles)
			{
				objReturn = FindGear(strGuid, objVehicle.Gear);

				if (objReturn != null)
				{
					if (objReturn.InternalId != Guid.Empty.ToString() && objReturn.Name != "")
					{
						objFoundVehicle = objVehicle;
						return objReturn;
					}
				}

				// Look for any Gear that might be attached to this Vehicle through Weapon Accessories or Cyberware.
				foreach (VehicleMod objMod in objVehicle.Mods)
				{
					// Weapon Accessories.
					WeaponAccessory objAccessory = new WeaponAccessory(_objCharacter);
					objReturn = FindWeaponGear(strGuid, objMod.Weapons, out objAccessory);

					if (objReturn != null)
					{
						if (objReturn.InternalId != Guid.Empty.ToString() && objReturn.Name != "")
						{
							objFoundVehicle = objVehicle;
							return objReturn;
						}
					}

					// Cyberware.
					Cyberware objCyberware = new Cyberware(_objCharacter);
					objReturn = FindCyberwareGear(strGuid, objMod.Cyberware, out objCyberware);

					if (objReturn != null)
					{
						if (objReturn.InternalId != Guid.Empty.ToString() && objReturn.Name != "")
						{
							objFoundVehicle = objVehicle;
							return objReturn;
						}
					}
				}
			}

			objFoundVehicle = null;
			objReturn = null;
			return objReturn;
		}

		/// <summary>
		/// Locate a Vehicle within the character's Vehicles.
		/// </summary>
		/// <param name="strGuid">InternalId of the Vehicle to Find.</param>
		/// <param name="lstArmors">List of Vehicles to search.</param>
		public Vehicle FindVehicle(string strGuid, List<Vehicle> lstVehicles)
		{
			foreach (Vehicle objVehicle in lstVehicles)
			{
				if (objVehicle.InternalId == strGuid)
					return objVehicle;
			}

			return null;
		}

		/// <summary>
		/// Locate a VehicleMod within the character's Vehicles.
		/// </summary>
		/// <param name="strGuid">InternalId of the VehicleMod to find.</param>
		/// <param name="lstArmors">List of Vehicles to search.</param>
		/// <param name="objFoundArmor">Vehicle that the VehicleMod was found in.</param>
		public VehicleMod FindVehicleMod(string strGuid, List<Vehicle> lstVehicles, out Vehicle objFoundVehicle)
		{
			foreach (Vehicle objVehicle in lstVehicles)
			{
				foreach (VehicleMod objMod in objVehicle.Mods)
				{
					if (objMod.InternalId == strGuid)
					{
						objFoundVehicle = objVehicle;
						return objMod;
					}
				}
			}

			objFoundVehicle = null;
			return null;
		}

		/// <summary>
		/// Locate a Weapon within the character's Vehicles.
		/// </summary>
		/// <param name="strGuid">InteralId of the Weapon to find.</param>
		/// <param name="lstVehicles">List of Vehicles to search.</param>
		/// <param name="objFoundVehicle">Vehicle that the Weapon was found in.</param>
		public Weapon FindVehicleWeapon(string strGuid, List<Vehicle> lstVehicles, out Vehicle objFoundVehicle)
		{
			Weapon objReturn = new Weapon(_objCharacter);
			foreach (Vehicle objVehicle in lstVehicles)
			{
				objReturn = FindWeapon(strGuid, objVehicle.Weapons);
				if (objReturn != null)
				{
					objFoundVehicle = objVehicle;
					return objReturn;
				}

				foreach (VehicleMod objMod in objVehicle.Mods)
				{
					objReturn = FindWeapon(strGuid, objMod.Weapons);
					if (objReturn != null)
					{
						objFoundVehicle = objVehicle;
						return objReturn;
					}
				}
			}

			objFoundVehicle = null;
			return null;
		}

		/// <summary>
		/// Locate a Weapon Accessory within the character's Vehicles.
		/// </summary>
		/// <param name="strGuid">InternalId of the Weapon Accessory to find.</param>
		/// <param name="lstVehicles">List of Vehicles to search.</param>
		public WeaponAccessory FindVehicleWeaponAccessory(string strGuid, List<Vehicle> lstVehicles)
		{
			WeaponAccessory objReturn = new WeaponAccessory(_objCharacter);
			foreach (Vehicle objVehicle in lstVehicles)
			{
				objReturn = FindWeaponAccessory(strGuid, objVehicle.Weapons);
				if (objReturn != null)
					return objReturn;

				foreach (VehicleMod objMod in objVehicle.Mods)
				{
					objReturn = FindWeaponAccessory(strGuid, objMod.Weapons);
					if (objReturn != null)
						return objReturn;
				}
			}

			return null;
		}

		/// <summary>
		/// Locate a Weapon Mod within the character's Vehicles.
		/// </summary>
		/// <param name="strGuid">InternalId of the Weapon Accessory to find.</param>
		/// <param name="lstVehicles">List of Vehicles to search.</param>
		public WeaponMod FindVehicleWeaponMod(string strGuid, List<Vehicle> lstVehicles)
		{
			WeaponMod objReturn = new WeaponMod(_objCharacter);
			foreach (Vehicle objVehicle in lstVehicles)
			{
				objReturn = FindWeaponMod(strGuid, objVehicle.Weapons);
				if (objReturn != null)
					return objReturn;

				foreach (VehicleMod objMod in objVehicle.Mods)
				{
					objReturn = FindWeaponMod(strGuid, objMod.Weapons);
					if (objReturn != null)
						return objReturn;
				}
			}

			return null;
		}

		/// <summary>
		/// Locate a piece of Cyberware within the character's Vehicles.
		/// </summary>
		/// <param name="strGuid">InternalId of the Cyberware to find.</param>
		/// <param name="lstVehicles">List of Vehicles to search.</param>
		public Cyberware FindVehicleCyberware(string strGuid, List<Vehicle> lstVehicles)
		{
			Cyberware objReturn = new Cyberware(_objCharacter);
			foreach (Vehicle objVehicle in lstVehicles)
			{
				foreach (VehicleMod objMod in objVehicle.Mods)
				{
					objReturn = FindCyberware(strGuid, objMod.Cyberware);
					if (objReturn != null)
						return objReturn;
				}
			}

			return null;
		}

		/// <summary>
		/// Locate a piece of Gear within the character's Armors.
		/// </summary>
		/// <param name="strGuid">InternalId of the Gear to find.</param>
		/// <param name="lstArmors">List of Armors to search.</param>
		/// <param name="objFoundArmor">Armor that the Gear was found in.</param>
		public Gear FindArmorGear(string strGuid, List<Armor> lstArmors, out Armor objFoundArmor)
		{
			Gear objReturn = new Gear(_objCharacter);
			foreach (Armor objArmor in lstArmors)
			{
				objReturn = FindGear(strGuid, objArmor.Gear);

				if (objReturn != null)
				{
					if (objReturn.InternalId != Guid.Empty.ToString() && objReturn.Name != "")
					{
						objFoundArmor = objArmor;
						return objReturn;
					}
				}
			}

			objFoundArmor = null;
			return objReturn;
		}

		/// <summary>
		/// Locate a piece of Armor within the character's Armors.
		/// </summary>
		/// <param name="strGuid">InternalId of the Armor to Find.</param>
		/// <param name="lstArmors">List of Armors to search.</param>
		public Armor FindArmor(string strGuid, List<Armor> lstArmors)
		{
			foreach (Armor objArmor in lstArmors)
			{
				if (objArmor.InternalId == strGuid)
					return objArmor;
			}

			return null;
		}

		/// <summary>
		/// Locate an Armor Mod within the character's Armors.
		/// </summary>
		/// <param name="strGuid">InternalId of the ArmorMod to Find.</param>
		/// <param name="lstArmors">List of Armors to search.</param>
		public ArmorMod FindArmorMod(string strGuid, List<Armor> lstArmors)
		{
			foreach (Armor objArmor in lstArmors)
			{
				foreach (ArmorMod objMod in objArmor.ArmorMods)
				{
					if (objMod.InternalId == strGuid)
						return objMod;
				}
			}

			return null;
		}

		/// <summary>
		/// Locate a piece of Gear within the character's Cyberware.
		/// </summary>
		/// <param name="strGuid">InternalId of the Gear to find.</param>
		/// <param name="lstCyberware">List of Cyberware to search.</param>
		/// <param name="objFoundCyberware">Cyberware that the Gear was found in.</param>
		public Gear FindCyberwareGear(string strGuid, List<Cyberware> lstCyberware, out Cyberware objFoundCyberware)
		{
			Gear objReturn = new Gear(_objCharacter);
			foreach (Cyberware objCyberware in lstCyberware)
			{
				objReturn = FindGear(strGuid, objCyberware.Gear);

				if (objReturn != null)
				{
					if (objReturn.InternalId != Guid.Empty.ToString() && objReturn.Name != "")
					{
						objFoundCyberware = objCyberware;
						return objReturn;
					}
				}

				if (objCyberware.Children.Count > 0)
				{
					objReturn = FindCyberwareGear(strGuid, objCyberware.Children, out objFoundCyberware);
					if (objReturn != null)
					{
						if (objReturn.InternalId != Guid.Empty.ToString() && objReturn.Name != "")
						{
							objFoundCyberware = objCyberware;
							return objReturn;
						}
					}
				}
			}

			objFoundCyberware = null;
			return objReturn;
		}

		/// <summary>
		/// Locate a Weapon within the character's Weapons.
		/// </summary>
		/// <param name="strGuid">InternalId of the Weapon to find.</param>
		/// <param name="lstWeaopns">List of Weapons to search.</param>
		public Weapon FindWeapon(string strGuid, List<Weapon> lstWeaopns)
		{
			Weapon objReturn = new Weapon(_objCharacter);
			foreach (Weapon objWeapon in lstWeaopns)
			{
				if (objWeapon.InternalId == strGuid)
					return objWeapon;

				// Look within Underbarrel Weapons.
				objReturn = FindWeapon(strGuid, objWeapon.UnderbarrelWeapons);
				if (objReturn != null)
					return objReturn;
			}

			return null;
		}

		/// <summary>
		/// Locate a WeaponAccessory within the character's Weapons.
		/// </summary>
		/// <param name="strGuid">InternalId of the WeaponAccessory to find.</param>
		/// <param name="lstWeapons">List of Weapons to search.</param>
		public WeaponAccessory FindWeaponAccessory(string strGuid, List<Weapon> lstWeapons)
		{
			WeaponAccessory objReturn = new WeaponAccessory(_objCharacter);
			foreach (Weapon objWeapon in lstWeapons)
			{
				foreach (WeaponAccessory objAccessory in objWeapon.WeaponAccessories)
				{
					if (objAccessory.InternalId == strGuid)
						return objAccessory;
				}

				// Look within Underbarrel Weapons.
				objReturn = FindWeaponAccessory(strGuid, objWeapon.UnderbarrelWeapons);
				if (objReturn != null)
					return objReturn;
			}

			return null;
		}

		/// <summary>
		/// Locate a WeaponMod within the character's Weapons.
		/// </summary>
		/// <param name="strGuid">InternalId of the WeaponMod to find.</param>
		/// <param name="lstWeapons">List of Weapons to search.</param>
		public WeaponMod FindWeaponMod(string strGuid, List<Weapon> lstWeapons)
		{
			WeaponMod objReturn = new WeaponMod(_objCharacter);
			foreach (Weapon objWeapon in lstWeapons)
			{
				foreach (WeaponMod objMod in objWeapon.WeaponMods)
				{
					if (objMod.InternalId == strGuid)
						return objMod;
				}

				// Look within Underbarrel Weapons.
				objReturn = FindWeaponMod(strGuid, objWeapon.UnderbarrelWeapons);
				if (objReturn != null)
					return objReturn;
			}

			return null;
		}

		/// <summary>
		/// Locate a piece of Gear within the character's Weapons.
		/// </summary>
		/// <param name="strGuid">InternalId of the Gear to find.</param>
		/// <param name="lstWeapons">List of Weapons to search.</param>
		/// <param name="objFoundAccessory">WeaponAccessory that the Gear was found in.</param>
		public Gear FindWeaponGear(string strGuid, List<Weapon> lstWeapons, out WeaponAccessory objFoundAccessory)
		{
			Gear objReturn = new Gear(_objCharacter);
			foreach (Weapon objWeapon in lstWeapons)
			{
				foreach (WeaponAccessory objAccessory in objWeapon.WeaponAccessories)
				{
					objReturn = FindGear(strGuid, objAccessory.Gear);

					if (objReturn != null)
					{
						if (objReturn.InternalId != Guid.Empty.ToString() && objReturn.Name != "")
						{
							objFoundAccessory = objAccessory;
							return objReturn;
						}
					}
				}

				if (objWeapon.UnderbarrelWeapons.Count > 0)
				{
					objReturn = FindWeaponGear(strGuid, objWeapon.UnderbarrelWeapons, out objFoundAccessory);

					if (objReturn != null)
					{
						if (objReturn.InternalId != Guid.Empty.ToString() && objReturn.Name != "")
							return objReturn;
					}
				}
			}

			objFoundAccessory = null;
			return objReturn;
		}

		/// <summary>
		/// Locate a Lifestyle within the character's Lifestyles.
		/// </summary>
		/// <param name="strGuid">InternalId of the Lifestyle to find.</param>
		/// <param name="lstLifestyles">List of Lifestyles to search.</param>
		public Lifestyle FindLifestyle(string strGuid, List<Lifestyle> lstLifestyles)
		{
			foreach (Lifestyle objLifestyle in lstLifestyles)
			{
				if (objLifestyle.InternalId == strGuid)
					return objLifestyle;
			}

			return null;
		}

		/// <summary>
		/// Locate a piece of Cyberware within the character's Cyberware.
		/// </summary>
		/// <param name="strGuid">InternalId of the Cyberware to find.</param>
		/// <param name="lstCyberware">List of Cyberware to search.</param>
		public Cyberware FindCyberware(string strGuid, List<Cyberware> lstCyberware)
		{
			Cyberware objReturn = new Cyberware(_objCharacter);
			foreach (Cyberware objCyberware in lstCyberware)
			{
				if (objCyberware.InternalId == strGuid)
					return objCyberware;

				objReturn = FindCyberware(strGuid, objCyberware.Children);
				if (objReturn != null)
					return objReturn;
			}

			return null;
		}

		/// <summary>
		/// Locate a Complex Form within the character's Complex Forms.
		/// </summary>
		/// <param name="strGuid">InternalId of the Complex Form to find.</param>
		/// <param name="lstPrograms">List of Complex Forms to search.</param>
		public TechProgram FindTechProgram(string strGuid, List<TechProgram> lstPrograms)
		{
			foreach (TechProgram objProgram in lstPrograms)
			{
				if (objProgram.InternalId == strGuid)
					return objProgram;
			}

			return null;
		}

		/// <summary>
		/// Locate a Complex Form Option within the character's Complex Forms.
		/// </summary>
		/// <param name="strGuid">InternalId of the Complex Form Option to Find.</param>
		/// <param name="lstPrograms">List of Complex Forms to search.</param>
		/// <param name="objFoundProgram">Complex Form that the Option was found in.</param>
		public TechProgramOption FindTechProgramOption(string strGuid, List<TechProgram> lstPrograms, out TechProgram objFoundProgram)
		{
			foreach (TechProgram objProgram in lstPrograms)
			{
				foreach (TechProgramOption objOption in objProgram.Options)
				{
					if (objOption.InternalId == strGuid)
					{
						objFoundProgram = objProgram;
						return objOption;
					}
				}
			}

			objFoundProgram = null;
			return null;
		}

		/// <summary>
		/// Locate a Spell within the character's Spells.
		/// </summary>
		/// <param name="strGuid">InternalId of the Spell to find.</param>
		/// <param name="lstSpells">List of Spells to search.</param>
		public Spell FindSpell(string strGuid, List<Spell> lstSpells)
		{
			foreach (Spell objSpell in lstSpells)
			{
				if (objSpell.InternalId == strGuid)
					return objSpell;
			}

			return null;
		}

		/// <summary>
		/// Locate a Critter Power within the character's Critter Powers.
		/// </summary>
		/// <param name="strGuid">InternalId of the Critter Power to find.</param>
		/// <param name="lstCritterPowers">List of Critter Powers to search.</param>
		public CritterPower FindCritterPower(string strGuid, List<CritterPower> lstCritterPowers)
		{
			foreach (CritterPower objPower in lstCritterPowers)
			{
				if (objPower.InternalId == strGuid)
					return objPower;
			}

			return null;
		}

		/// <summary>
		/// Locate a Quality within the character's Qualities.
		/// </summary>
		/// <param name="strGuid">InternalId of the Quality to find.</param>
		/// <param name="lstQualities">List of Qualities to search.</param>
		public Quality FindQuality(string strGuid, List<Quality> lstQualities)
		{
			foreach (Quality objQuality in lstQualities)
			{
				if (objQuality.InternalId == strGuid)
					return objQuality;
			}

			return null;
		}

		/// <summary>
		/// Locate a Metamagic within the character's Metamagics.
		/// </summary>
		/// <param name="strGuid">InternalId of the Metamagic to find.</param>
		/// <param name="lstMetamagics">List of Metamagics to search.</param>
		public Metamagic FindMetamagic(string strGuid, List<Metamagic> lstMetamagics)
		{
			foreach (Metamagic objMetamagic in lstMetamagics)
			{
				if (objMetamagic.InternalId == strGuid)
					return objMetamagic;
			}

			return null;
		}

		/// <summary>
		/// Locate a Martial Art within the character's Martial Arts.
		/// </summary>
		/// <param name="strName">Name of the Martial Art to find.</param>
		/// <param name="lstMartialArts">List of Martial Arts to search.</param>
		public MartialArt FindMartialArt(string strName, List<MartialArt> lstMartialArts)
		{
			foreach (MartialArt objArt in lstMartialArts)
			{
				if (objArt.Name == strName)
					return objArt;
			}

			return null;
		}

		/// <summary>
		/// Locate a Martial Art Advantage within the character's Martial Arts.
		/// </summary>
		/// <param name="strGuid">InternalId of the Martial Art Advantage to find.</param>
		/// <param name="lstMartialArts">List of Martial Arts to search.</param>
		/// <param name="objFoundMartialArt">MartialArt the Advantage was found in.</param>
		public MartialArtAdvantage FindMartialArtAdvantage(string strGuid, List<MartialArt> lstMartialArts, out MartialArt objFoundMartialArt)
		{
			foreach (MartialArt objArt in lstMartialArts)
			{
				foreach (MartialArtAdvantage objAdvantage in objArt.Advantages)
				{
					if (objAdvantage.InternalId == strGuid)
					{
						objFoundMartialArt = objArt;
						return objAdvantage;
					}
				}
			}

			objFoundMartialArt = null;
			return null;
		}

		/// <summary>
		/// Locate a Martial Art Maneuver within the character's Martial Art Maneuvers.
		/// </summary>
		/// <param name="strGuid">InternalId of the Martial Art Maneuver to find.</param>
		/// <param name="lstManeuvers">List of Martial Art Maneuvers to search.</param>
		public MartialArtManeuver FindMartialArtManeuver(string strGuid, List<MartialArtManeuver> lstManeuvers)
		{
			foreach (MartialArtManeuver objManeuver in lstManeuvers)
			{
				if (objManeuver.InternalId == strGuid)
					return objManeuver;
			}

			return null;
		}

		/// <summary>
		/// Find a TreeNode in a TreeView based on its Tag.
		/// </summary>
		/// <param name="strGuid">InternalId of the Node to find.</param>
		/// <param name="treTree">TreeView to search.</param>
		public TreeNode FindNode(string strGuid, TreeView treTree)
		{
			foreach (TreeNode objNode in treTree.Nodes)
			{
				if (objNode.Tag.ToString() == strGuid)
					return objNode;
				else
					return FindNode(strGuid, objNode);
			}
			return null;
		}

		/// <summary>
		/// Find a TreeNode in a TreeNode based on its Tag.
		/// </summary>
		/// <param name="strGuid">InternalId of the Node to find.</param>
		/// <param name="objNode">TreeNode to search.</param>
		public TreeNode FindNode(string strGuid, TreeNode objNode)
		{
			TreeNode objFound = new TreeNode();
			foreach (TreeNode objChild in objNode.Nodes)
			{
				if (objChild.Tag.ToString() == strGuid)
					return objChild;
				else
				{
					objFound = FindNode(strGuid, objChild);
					if (objFound != null)
						return objFound;
				}
			}
			return null;
		}

		/// <summary>
		/// Find all of the Commlinks carried by the character.
		/// </summary>
		/// <param name="lstGear">List of Gear to search within for Commlinks.</param>
		public List<Commlink> FindCharacterCommlinks(List<Gear> lstGear)
		{
			List<Commlink> lstReturn = new List<Commlink>();
			foreach (Gear objGear in lstGear)
			{
				if (objGear.GetType() == typeof(Commlink))
					lstReturn.Add((Commlink)objGear);

				if (objGear.Children.Count > 0)
				{
					// Retrieve the list of Commlinks in child items.
					List<Commlink> lstAppend = FindCharacterCommlinks(objGear.Children);
					if (lstAppend.Count > 0)
					{
						// Append the entries to the current list.
						foreach (Commlink objCommlink in lstAppend)
							lstReturn.Add(objCommlink);
					}
				}
			}

			return lstReturn;
		}
		#endregion

		#region Delete Functions
		/// <summary>
		/// Recursive method to delete a piece of Gear and its Improvements from the character.
		/// </summary>
		/// <param name="objGear">Gear to delete.</param>
		/// <param name="treWeapons">TreeView that holds the list of Weapons.</param>
		/// <param name="objImprovementManager">Improvement Manager the character is using.</param>
		public void DeleteGear(Gear objGear, TreeView treWeapons, ImprovementManager objImprovementManager)
		{
			// Remove any children the Gear may have.
			foreach (Gear objChild in objGear.Children)
				DeleteGear(objChild, treWeapons, objImprovementManager);

			// Remove the Gear Weapon created by the Gear if applicable.
			if (objGear.WeaponID != Guid.Empty.ToString())
			{
				// Remove the Weapon from the TreeView.
				TreeNode objRemoveNode = new TreeNode();
				foreach (TreeNode objWeaponNode in treWeapons.Nodes[0].Nodes)
				{
					if (objWeaponNode.Tag.ToString() == objGear.WeaponID)
						objRemoveNode = objWeaponNode;
				}
				treWeapons.Nodes.Remove(objRemoveNode);

				// Remove the Weapon from the Character.
				Weapon objRemoveWeapon = new Weapon(_objCharacter);
				foreach (Weapon objWeapon in _objCharacter.Weapons)
				{
					if (objWeapon.InternalId == objGear.WeaponID)
						objRemoveWeapon = objWeapon;
				}
				_objCharacter.Weapons.Remove(objRemoveWeapon);
			}

			objImprovementManager.RemoveImprovements(Improvement.ImprovementSource.Gear, objGear.InternalId);

			// If a Focus is being removed, make sure the actual Focus is being removed from the character as well.
			if (objGear.Category == "Foci" || objGear.Category == "Metamagic Foci")
			{
				List<Focus> lstRemoveFoci = new List<Focus>();
				foreach (Focus objFocus in _objCharacter.Foci)
				{
					if (objFocus.GearId == objGear.InternalId)
						lstRemoveFoci.Add(objFocus);
				}
				foreach (Focus objFocus in lstRemoveFoci)
					_objCharacter.Foci.Remove(objFocus);
			}

			// If a Stacked Focus is being removed, make sure the Stacked Foci and its bonuses are being removed.
			if (objGear.Category == "Stacked Focus")
			{
				foreach (StackedFocus objStack in _objCharacter.StackedFoci)
				{
					if (objStack.GearId == objGear.InternalId)
					{
						objImprovementManager.RemoveImprovements(Improvement.ImprovementSource.StackedFocus, objStack.InternalId);
						_objCharacter.StackedFoci.Remove(objStack);
						break;
					}
				}
			}
		}

		/// <summary>
		/// Recursive method to delete a piece of Gear and from a Vehicle.
		/// </summary>
		/// <param name="objGear">Gear to delete.</param>
		/// <param name="treVehicles">TreeView that holds the list of Vehicles.</param>
		/// <param name="objVehicle">Vehicle to remove items from.</param>
		public void DeleteVehicleGear(Gear objGear, TreeView treVehicles, Vehicle objVehicle)
		{
			// Remove any children the Gear may have.
			foreach (Gear objChild in objGear.Children)
				DeleteVehicleGear(objChild, treVehicles, objVehicle);

			// Remove the Gear Weapon created by the Gear if applicable.
			if (objGear.WeaponID != Guid.Empty.ToString())
			{
				// Remove the Weapon from the TreeView.
				TreeNode objRemoveNode = new TreeNode();
				foreach (TreeNode objVehicleNode in treVehicles.Nodes[0].Nodes)
				{
					foreach (TreeNode objWeaponNode in objVehicleNode.Nodes)
					{
						if (objWeaponNode.Tag.ToString() == objGear.WeaponID)
							objRemoveNode = objWeaponNode;
					}
					objVehicleNode.Nodes.Remove(objRemoveNode);
				}
				
				// Remove the Weapon from the Vehicle.
				Weapon objRemoveWeapon = new Weapon(_objCharacter);
				foreach (Weapon objWeapon in objVehicle.Weapons)
				{
					if (objWeapon.InternalId == objGear.WeaponID)
						objRemoveWeapon = objWeapon;
				}
				objVehicle.Weapons.Remove(objRemoveWeapon);
			}
		}
		#endregion

		#region Tree Functions
		/// <summary>
		/// Clear the background colour for all TreeNodes except the one currently being hovered over during a drag-and-drop operation.
		/// </summary>
		/// <param name="treTree">TreeView to check.</param>
		/// <param name="objHighlighted">TreeNode that is currently being hovered over.</param>
		public void ClearNodeBackground(TreeView treTree, TreeNode objHighlighted)
		{
			foreach (TreeNode objNode in treTree.Nodes)
			{
				if (objNode != objHighlighted)
					objNode.BackColor = SystemColors.Window;
				ClearNodeBackground(objNode, objHighlighted);
			}
		}

		/// <summary>
		/// Recursive method to clear the background colour for all TreeNodes except the one currently being hovered over during a drag-and-drop operation.
		/// </summary>
		/// <param name="objNode">Parent TreeNode to check.</param>
		/// <param name="objHighlighted">TreeNode that is currently being hovered over.</param>
		private void ClearNodeBackground(TreeNode objNode, TreeNode objHighlighted)
		{
			foreach (TreeNode objChild in objNode.Nodes)
			{
				if (objChild != objHighlighted)
					objChild.BackColor = SystemColors.Window;
				if (objChild.Nodes.Count > 0)
					ClearNodeBackground(objChild, objHighlighted);
			}
		}

		/// <summary>
		/// Build up the Tree for the current piece of Gear and all of its children.
		/// </summary>
		/// <param name="objGear">Gear to iterate through.</param>
		/// <param name="objNode">TreeNode to append to.</param>
		/// <param name="objMenu">ContextMenuStrip that the new TreeNodes should use.</param>
		public void BuildGearTree(Gear objGear, TreeNode objNode, ContextMenuStrip objMenu)
		{
			foreach (Gear objChild in objGear.Children)
			{
				TreeNode objChildNode = new TreeNode();
				objChildNode.Text = objChild.DisplayName;
				objChildNode.Tag = objChild.InternalId;
				objChildNode.ContextMenuStrip = objMenu;
				if (objChild.Notes != string.Empty)
					objChildNode.ForeColor = Color.SaddleBrown;
				objChildNode.ToolTipText = objChild.Notes;

				objNode.Nodes.Add(objChildNode);
				objNode.Expand();

				// Set the Gear's Parent.
				objChild.Parent = objGear;

				BuildGearTree(objChild, objChildNode, objMenu);
			}
		}

		/// <summary>
		/// Build up the Tree for the current piece of Cyberware and all of its children.
		/// </summary>
		/// <param name="objCyberware">Cyberware to iterate through.</param>
		/// <param name="objParentNode">TreeNode to append to.</param>
		/// <param name="objMenu">ContextMenuStrip that the new Cyberware TreeNodes should use.</param>
		/// <param name="objGearMenu">ContextMenuStrip that the new Gear TreeNodes should use.</param>
		public void BuildCyberwareTree(Cyberware objCyberware, TreeNode objParentNode, ContextMenuStrip objMenu, ContextMenuStrip objGearMenu)
		{
				TreeNode objNode = new TreeNode();
				objNode.Text = objCyberware.DisplayName;
				objNode.Tag = objCyberware.InternalId;
				if (objCyberware.Notes != string.Empty)
					objNode.ForeColor = Color.SaddleBrown;
				objNode.ToolTipText = objCyberware.Notes;
				objNode.ContextMenuStrip = objMenu;

				objParentNode.Nodes.Add(objNode);
				objParentNode.Expand();

				foreach (Cyberware objChild in objCyberware.Children)
					BuildCyberwareTree(objChild, objNode, objMenu, objGearMenu);

				foreach (Gear objGear in objCyberware.Gear)
				{
					TreeNode objGearNode = new TreeNode();
					objGearNode.Text = objGear.DisplayName;
					objGearNode.Tag = objGear.InternalId;
					if (objGear.Notes != string.Empty)
						objGearNode.ForeColor = Color.SaddleBrown;
					objGearNode.ToolTipText = objGear.Notes;
					objGearNode.ContextMenuStrip = objGearMenu;

					BuildGearTree(objGear, objGearNode, objGearMenu);

					objNode.Nodes.Add(objGearNode);
					objNode.Expand();
				}

		}
		#endregion

		#region TreeNode Creation Methods
		/// <summary>
		/// Add a piece of Armor to the Armor TreeView.
		/// </summary>
		/// <param name="objArmor">Armor to add.</param>
		/// <param name="treArmor">Armor TreeView.</param>
		/// <param name="cmsArmor">ContextMenuStrip for the Armor Node.</param>
		/// <param name="cmsArmorMod">ContextMenuStrip for Armor Mod Nodes.</param>
		/// <param name="cmsArmorGear">ContextMenuStrip for Armor Gear Nodes.</param>
		public void CreateArmorTreeNode(Armor objArmor, TreeView treArmor, ContextMenuStrip cmsArmor, ContextMenuStrip cmsArmorMod, ContextMenuStrip cmsArmorGear)
		{
			TreeNode objNode = new TreeNode();
			objNode.Text = objArmor.DisplayName;
			objNode.Tag = objArmor.InternalId;
			if (objArmor.Notes != string.Empty)
				objNode.ForeColor = Color.SaddleBrown;
			objNode.ToolTipText = objArmor.Notes;

			foreach (ArmorMod objMod in objArmor.ArmorMods)
			{
				TreeNode objChild = new TreeNode();
				objChild.Text = objMod.DisplayName;
				objChild.Tag = objMod.InternalId;
				objChild.ContextMenuStrip = cmsArmorMod;
				if (objMod.Notes != string.Empty)
					objChild.ForeColor = Color.SaddleBrown;
				objChild.ToolTipText = objMod.Notes;
				objNode.Nodes.Add(objChild);
				objNode.Expand();
			}

			foreach (Gear objGear in objArmor.Gear)
			{
				TreeNode objChild = new TreeNode();
				objChild.Text = objGear.DisplayName;
				objChild.Tag = objGear.InternalId;
				if (objGear.Notes != string.Empty)
					objChild.ForeColor = Color.SaddleBrown;
				objChild.ToolTipText = objGear.Notes;

				BuildGearTree(objGear, objChild, cmsArmorGear);

				objChild.ContextMenuStrip = cmsArmorGear;
				objNode.Nodes.Add(objChild);
				objNode.Expand();
			}

			TreeNode objParent = new TreeNode();
			if (objArmor.Location == "")
				objParent = treArmor.Nodes[0];
			else
			{
				foreach (TreeNode objFind in treArmor.Nodes)
				{
					if (objFind.Text == objArmor.Location)
					{
						objParent = objFind;
						break;
					}
				}
			}

			objNode.ContextMenuStrip = cmsArmor;
			objParent.Nodes.Add(objNode);
			objParent.Expand();
		}

		/// <summary>
		/// Add a Vehicle to the TreeView.
		/// </summary>
		/// <param name="objVehicle">Vehicle to add.</param>
		/// <param name="treVehicles">Vehicle TreeView.</param>
		/// <param name="cmsVehicle">ContextMenuStrip for the Vehicle Node.</param>
		/// <param name="cmsVehicleLocation">ContextMenuStrip for Vehicle Location Nodes.</param>
		/// <param name="cmsVehicleWeapon">ContextMenuStrip for Vehicle Weapon Nodes.</param>
		/// <param name="cmsWeaponMod">ContextMenuStrip for Vehicle Weapon Mod Nodes.</param>
		/// <param name="cmsWeaponAccessory">ContextMenuStrip for Vehicle Weapon Accessory Nodes.</param>
		/// <param name="cmsVehicleGear">ContextMenuStrip for Vehicle Gear Nodes.</param>
		public void CreateVehicleTreeNode(Vehicle objVehicle, TreeView treVehicles, ContextMenuStrip cmsVehicle, ContextMenuStrip cmsVehicleLocation, ContextMenuStrip cmsVehicleWeapon, ContextMenuStrip cmsWeaponMod, ContextMenuStrip cmsWeaponAccessory, ContextMenuStrip cmsWeaponAccessoryGear, ContextMenuStrip cmsVehicleGear)
		{
			TreeNode objNode = new TreeNode();
			objNode.Text = objVehicle.DisplayName;
			objNode.Tag = objVehicle.InternalId;
			if (objVehicle.Notes != string.Empty)
				objNode.ForeColor = Color.SaddleBrown;
			objNode.ToolTipText = objVehicle.Notes;

			// Populate the list of Vehicle Locations.
			foreach (string strLocation in objVehicle.Locations)
			{
				TreeNode objLocation = new TreeNode();
				objLocation.Tag = strLocation;
				objLocation.Text = strLocation;
				objLocation.ContextMenuStrip = cmsVehicleLocation;
				objNode.Nodes.Add(objLocation);
			}

			// VehicleMods.
			foreach (VehicleMod objMod in objVehicle.Mods)
			{
				TreeNode objChildNode = new TreeNode();
				objChildNode.Text = objMod.DisplayName;
				objChildNode.Tag = objMod.InternalId;
				if (objMod.IncludedInVehicle)
					objChildNode.ForeColor = SystemColors.GrayText;
				if (objMod.Notes != string.Empty)
					objChildNode.ForeColor = Color.SaddleBrown;
				objChildNode.ToolTipText = objMod.Notes;

				// Cyberware.
				foreach (Cyberware objCyberware in objMod.Cyberware)
				{
					TreeNode objCyberwareNode = new TreeNode();
					objCyberwareNode.Text = objCyberware.DisplayName;
					objCyberwareNode.Tag = objCyberware.InternalId;
					if (objCyberware.Notes != string.Empty)
						objCyberwareNode.ForeColor = Color.SaddleBrown;
					objCyberwareNode.ToolTipText = objCyberware.Notes;
					objChildNode.Nodes.Add(objCyberwareNode);
					objChildNode.Expand();
				}

				// VehicleWeapons.
				foreach (Weapon objWeapon in objMod.Weapons)
					CreateWeaponTreeNode(objWeapon, objChildNode, cmsVehicleWeapon, cmsWeaponMod, cmsWeaponAccessory, cmsWeaponAccessoryGear);

				// Attach the ContextMenuStrip.
				objChildNode.ContextMenuStrip = cmsVehicle;

				objNode.Nodes.Add(objChildNode);
				objNode.Expand();
			}

			// Vehicle Weapons (not attached to a mount).
			foreach (Weapon objWeapon in objVehicle.Weapons)
				CreateWeaponTreeNode(objWeapon, objNode, cmsVehicleWeapon, cmsWeaponMod, cmsWeaponAccessory, cmsWeaponAccessoryGear);

			// Vehicle Gear.
			foreach (Gear objGear in objVehicle.Gear)
			{
				TreeNode objGearNode = new TreeNode();
				objGearNode.Text = objGear.DisplayName;
				objGearNode.Tag = objGear.InternalId;
				if (objGear.Notes != string.Empty)
					objGearNode.ForeColor = Color.SaddleBrown;
				objGearNode.ToolTipText = objGear.Notes;

				BuildGearTree(objGear, objGearNode, cmsVehicleGear);

				objGearNode.ContextMenuStrip = cmsVehicleGear;

				TreeNode objParent = new TreeNode();
				if (objGear.Location == "")
					objParent = objNode;
				else
				{
					foreach (TreeNode objFind in objNode.Nodes)
					{
						if (objFind.Text == objGear.Location)
						{
							objParent = objFind;
							break;
						}
					}
				}

				objParent.Nodes.Add(objGearNode);
				objParent.Expand();
			}

			objNode.ContextMenuStrip = cmsVehicle;
			treVehicles.Nodes[0].Nodes.Add(objNode);
			treVehicles.Nodes[0].Expand();
		}

		/// <summary>
		/// Add a Weapon to the TreeView.
		/// </summary>
		/// <param name="objWeapon">Weapon to add.</param>
		/// <param name="objWeaponsNode">Node to append the Weapon Node to.</param>
		/// <param name="cmsWeapon">ContextMenuStrip for the Weapon Node.</param>
		/// <param name="cmsWeaponMod">ContextMenuStrip for Weapon Mod Nodes.</param>
		/// <param name="cmsWeaponAccessory">ContextMenuStrip for Vehicle Accessory Nodes.</param>
		/// <param name="cmsWeaponAccessoryGear">ContextMenuStrip for Vehicle Weapon Accessory Gear Nodes.</param>
		public void CreateWeaponTreeNode(Weapon objWeapon, TreeNode objWeaponsNode, ContextMenuStrip cmsWeapon, ContextMenuStrip cmsWeaponMod, ContextMenuStrip cmsWeaponAccessory, ContextMenuStrip cmsWeaponAccessoryGear)
		{
			TreeNode objNode = new TreeNode();
			objNode.Text = objWeapon.DisplayName;
			objNode.Tag = objWeapon.InternalId;
			if (objWeapon.Category.StartsWith("Cyberware") || objWeapon.Category == "Gear" || objWeapon.Category.StartsWith("Quality"))
				objNode.ForeColor = SystemColors.GrayText;
			if (objWeapon.Notes != string.Empty)
				objNode.ForeColor = Color.SaddleBrown;
			objNode.ToolTipText = objWeapon.Notes;

			// Add attached Weapon Accessories.
			foreach (WeaponAccessory objAccessory in objWeapon.WeaponAccessories)
			{
				TreeNode objChild = new TreeNode();
				objChild.Text = objAccessory.DisplayName;
				objChild.Tag = objAccessory.InternalId;
				objChild.ContextMenuStrip = cmsWeaponAccessory;
				if (objAccessory.Notes != string.Empty)
					objChild.ForeColor = Color.SaddleBrown;
				objChild.ToolTipText = objAccessory.Notes;

				// Add any Gear attached to the Weapon Accessory.
				foreach (Gear objGear in objAccessory.Gear)
				{
					TreeNode objGearChild = new TreeNode();
					objGearChild.Text = objGear.DisplayName;
					objGearChild.Tag = objGear.InternalId;
					if (objGear.Notes != string.Empty)
						objGearChild.ForeColor = Color.SaddleBrown;
					objGearChild.ToolTipText = objGear.Notes;

					BuildGearTree(objGear, objGearChild, cmsWeaponAccessoryGear);

					objGearChild.ContextMenuStrip = cmsWeaponAccessoryGear;
					objChild.Nodes.Add(objGearChild);
					objChild.Expand();
				}

				objNode.Nodes.Add(objChild);
				objNode.Expand();
			}

			// Add Attached Weapon Modifications.
			foreach (WeaponMod objMod in objWeapon.WeaponMods)
			{
				TreeNode objChild = new TreeNode();
				objChild.Text = objMod.DisplayName;
				objChild.Tag = objMod.InternalId;
				objChild.ContextMenuStrip = cmsWeaponMod;
				if (objMod.Notes != string.Empty)
					objChild.ForeColor = Color.SaddleBrown;
				objChild.ToolTipText = objMod.Notes;
				objNode.Nodes.Add(objChild);
				objNode.Expand();
			}

			// Add Underbarrel Weapons.
			if (objWeapon.UnderbarrelWeapons.Count > 0)
			{
				foreach (Weapon objUnderbarrelWeapon in objWeapon.UnderbarrelWeapons)
					CreateWeaponTreeNode(objUnderbarrelWeapon, objNode, cmsWeapon, cmsWeaponMod, cmsWeaponAccessory, cmsWeaponAccessoryGear);
			}

			// If this is not an Underbarrel Weapon and it has a Location, find the Location Node that this should be attached to instead.
			if (!objWeapon.IsUnderbarrelWeapon && objWeapon.Location != string.Empty)
			{
				foreach (TreeNode objLocationNode in objWeaponsNode.TreeView.Nodes)
				{
					if (objLocationNode.Text == objWeapon.Location)
					{
						objWeaponsNode = objLocationNode;
						break;
					}
				}
			}

			objNode.ContextMenuStrip = cmsWeapon;
			objWeaponsNode.Nodes.Add(objNode);
			objWeaponsNode.Expand();
		}
		#endregion
	}
}