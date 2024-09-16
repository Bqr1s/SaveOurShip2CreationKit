using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using RimWorld.Planet;
using HarmonyLib;
using Vehicles;
using System;
using System.IO;

namespace SaveOurShip2
{	
	[HarmonyPatch(typeof(VehicleMapping), "GenerateRegionsAsync")]
	public static class VehicleRegion3
	{
		public static bool Prefix(VehicleMapping __instance)
		{;
			return false;
		}
	}
	[HarmonyPatch(typeof(VehicleMapping), "GenerateRegions")]
	public static class VehicleRegion2
	{
		public static bool Prefix(VehicleMapping __instance)
		{
			return false;
		}
	}

	[HarmonyPatch(typeof(VehicleRegionDirtyer), "SetRegionDirty")]
	public static class VehicleRegion
	{
		public static bool Prefix()
		{
			return false;
		}
	}
	[HarmonyPatch(typeof(LongEventHandler), "DrawLongEventWindow")]
	public static class HideWindowForScreenshots
	{
		public static bool Prefix()
		{
			ScreenshotHelperWorldComp worldComp = (ScreenshotHelperWorldComp)Find.World.components.First(x => x is ScreenshotHelperWorldComp);
			if (worldComp == null)
			{
				return true;
			}
			return !worldComp.screenshotMode;
		}
	}
	public class ScreenshotHelperWorldComp : WorldComponent
	{
		public bool screenshotMode = false;
		public List<ShipDef> screenshotShips;
		public int lastShipSpawnTick = -10000;
		public Map shipMap;
		public Map shipMap2;
		ShipDef currentShip;
		public ShipMapComp shipMapComp;
		public ShipMapComp oldMapComp;
		public string csvText;
		public List<string> excludeDefNames = new List<string>()
			{
				"MechanoidMoonBase",
				"StarshipBowDungeon",
				"StationArchotechGarden",
				"TribalVillageIsNotAShip",
				"0",
			};
		/*public string defsString = "ShipTurret_Laser,ShipTurret_Laser_Large,ShipTurret_Plasma,ShipTurret_Plasma_Large,ShipTurret_ACI,ShipTurret_ACII,ShipTurret_ACIII,ShipTorpedoOne,ShipTorpedoTwo," +
			"ShipTorpedoSix,ShipTurret_Kinetic,ShipTurret_Kinetic_Large,ShipSpinalBarrelKinetic,ShipSpinalBarrelLaser,ShipSpinalBarrelPlasma,ShipSpinalBarrelPhysic,ShipSpinalBarrelMechanite," +
			"ShipSpinalAmplifier,ShipSpinalEmitter,ShipCombatShieldGeneratorMini,ShipCombatShieldGenerator,ArchotechShieldGenerator,ShipHeatsink,ShipHeatsinkLarge,ShipHeatBankLarge," +
			"ShipHeatsinkAntiEntropy,ShipPurgePort,ShipPurgePortLarge,ShipInside_SolarGenerator,ShipInside_SolarGeneratorMech,ShipInside_SolarGeneratorArchotech,ShipReactor_Small,ShipReactor," +
			"ArchotechAntimatterReactor,ShipCapacitorSmall,ShipCapacitor,ShipSencorCluster,ShipSencorClusterAdv,ShipCloakingDevice,ShipSalvageBay,ShipSalvageBayNano,Ship_Thruster,Ship_Engine_Small," +
			"Ship_Engine,Ship_Engine_Large,Ship_Engine_Archotech,Ship_Engine_Interplanetary,Ship_Engine_Interplanetary_Large,Ship_DroneCore";*/

		public string defsString = @"
			ShipTurret_Laser, ShipTurret_Laser_Large,ShipTurret_Plasma,ShipTurret_Plasma_Large,ShipTurret_ACI,ShipTurret_ACII,ShipTurret_ACIII,ShipTorpedoOne,ShipTorpedoTwo,
			ShipTorpedoSix,ShipTurret_Kinetic,ShipTurret_Kinetic_Large,ShipSpinalBarrelKinetic,ShipSpinalBarrelLaser,ShipSpinalBarrelPlasma,ShipSpinalBarrelPhysic,ShipSpinalBarrelMechanite,
			ShipSpinalAmplifier,ShipSpinalEmitter,ShipCombatShieldGeneratorMini,ShipCombatShieldGenerator,ArchotechShieldGenerator,ShipHeatsink,ShipHeatsinkLarge,ShipHeatManifold,ShipHeatBankLarge,
			ShipHeatsinkAntiEntropy,ShipPurgePort,ShipPurgePortLarge,ShipInside_SolarGenerator,ShipInside_SolarGeneratorMech,ShipInside_SolarGeneratorArchotech,Ship_Reactor_Small,Ship_Reactor,
			ArchotechAntimatterReactor,ShipCapacitorSmall,ShipCapacitor,Ship_SencorCluster,Ship_SencorClusterAdv,Ship_LifeSupport,Ship_LifeSupport_Small,ShipCloakingDevice,ShipSalvageBay,
			ShipSalvageBayNano,Ship_Thruster,Ship_Engine_Small,Ship_Engine,Ship_Engine_Large,Ship_Engine_Archotech,Ship_Engine_Interplanetary,Ship_Engine_Interplanetary_Large,Ship_DroneCore,
			Turret_Autocannon,Turret_Sniper,Turret_MiniTurret,SoS2_Shuttle_Personal,SoS2_Shuttle,SoS2_Shuttle_Heavy,SoS2_Shuttle_Superheavy
			";
		public List<string> defsLit;

		public ScreenshotHelperWorldComp(World world) : base(world)
		{

		}
		public void StartScreenshotsSequence()
		{
			// Make CSV header
			csvText = "Def name,Label,CombatPoints,SizeX,SizeZ,TradeShip,CargoValue,NeverAttacks,NeverWreck," +
			"Mass,ThrustToWeight,HeatCap,PowerCap,PD Weapons,Small Weapons, Large Weapons,Capital Weapons,Amplifiers,Torpedo Tubes";
			defsLit = defsString.Split(',').Select(p => p.Trim()).ToList();
			foreach (string defString in defsLit)
			{
				csvText += "," + defString;
			}
			csvText += ";\n";
			screenshotMode = true;
		}

		public override void WorldComponentTick()
		{
			base.WorldComponentTick();
			if (!screenshotMode)
			{
				return;
			}
			// Small wait here was needed to avoid glitches directly after load, loner wait if for solatr panels to extend.
			if (Find.TickManager.TicksGame == lastShipSpawnTick + 180 && shipMap != null)
			{
				AddShipText();
				Messages.Clear();
				Find.ScreenshotModeHandler.active = true;
				TakeShipScreenshot();
			}
			if (Find.TickManager.TicksGame > lastShipSpawnTick + 360)
			{
				if (shipMap != null)
				{
					DetroyMapWithParent(shipMap);
					shipMap = null;
					DetroyMapWithParent(shipMap2);
					shipMap2 = null;
				}
			}
			if (Find.TickManager.TicksGame > lastShipSpawnTick + 420)
			{
				lastShipSpawnTick = Find.TickManager.TicksGame;
				if (screenshotShips.Empty())
				{
					screenshotMode = false;
					Find.ScreenshotModeHandler.active = false;

					string path = Path.Combine(GenFilePaths.SaveDataFolderPath, "ExportedShips");
					DirectoryInfo dir = new DirectoryInfo(path);
					if (!dir.Exists)
						dir.Create();
					string fileName = Path.Combine(path, "ShipList.csv");

					File.WriteAllText(fileName, csvText);
					return;
				}
				currentShip = screenshotShips.First();
				screenshotShips.Remove(currentShip);

				shipMap = CreateMap();

				shipMap2 = SoSBuilder.GenerateShip(null, currentShip);
				shipMapComp = shipMap2.GetComponent<ShipMapComp>();
				Find.Selector.ClearSelection();
				Event.current.Use();
				ZoomToShip();
				Messages.Clear();
				
			}
		}
		private void AddShipText()
		{
			csvText += currentShip.defName;
			AddField(currentShip.label);
			AddField(currentShip.combatPoints);
			AddField(currentShip.sizeX);
			AddField(currentShip.sizeZ);
			AddField(currentShip.tradeShip);
			AddField(currentShip.cargoValue);
			AddField(currentShip.neverAttacks);
			AddField(currentShip.neverWreck);

			SpaceShipCache ship = shipMapComp.ShipsOnMap.Values.First();
			AddField((int)ship.MassActual);
			AddField(ship.ThrustRatio.ToString("F3"));

			Building_ShipBridge bridge = ship.Bridges.First();
			if (bridge != null)
			{ 
				AddField((int)bridge.heatCap);
				AddField(bridge.powerCap);
			}
			else
			{
				AddField("n/a");
				AddField("n/a");
			}

			int pdCount = ship.Buildings.Where(b => b.def.defName.Equals("ShipTurret_ACI") || b.def.defName.Equals("ShipTurret_Laser")).Count();
			AddField(pdCount);

			List<String> smallWeaponsDefs = new List<String>()
			{
				"ShipPartTurretSmall",  // random
				"ShipTurret_ACI",
				"ShipTurret_Laser",
				"ShipTurret_Plasma",
				"ShipTurret_Kinetic"
			};
			int smallCount = ship.Buildings.Where(b => smallWeaponsDefs.Contains(b.def.defName)).Count();
			AddField(smallCount);

			List<String> largeWeaponsDefs = new List<String>()
			{
				"ShipPartTurretLarge",  // random
				"ShipTurret_ACII",
				"ShipTurret_Laser_Large",
				"ShipTurret_Plasma_Large",
				"ShipTurret_Kinetic_Large"
			};
			int largeCount = ship.Buildings.Where(b => largeWeaponsDefs.Contains(b.def.defName)).Count();
			AddField(largeCount);

			List<String> capitalWeaponsDefs = new List<String>()
			{
				"ShipPartTurretSpinal",  // random
				"ShipTurret_ACIII",
				"ShipSpinalBarrelLaser",
				"ShipSpinalBarrelPlasma",
				"ShipSpinalBarrelKinetic",
				"ShipSpinalBarrelPsychic",
				"ShipSpinalBarrelMechanite"
			};
			int capitalCount = ship.Buildings.Where(b => capitalWeaponsDefs.Contains(b.def.defName)).Count();
			AddField(capitalCount);

			int amplifierCount = ship.Buildings.Where(b => b.def.defName.Equals("ShipSpinalAmplifier")).Count();
			AddField(amplifierCount);

			int singleTubeCount = ship.Buildings.Where(b => b.def.defName.Equals("ShipTorpedoOne")).Count();
			int doubleTubeCount = ship.Buildings.Where(b => b.def.defName.Equals("ShipTorpedoTwo")).Count();
			int sixTubeCount = ship.Buildings.Where(b => b.def.defName.Equals("ShipTorpedoSix")).Count();

			AddField(singleTubeCount + 2* doubleTubeCount + 6* sixTubeCount);

			Dictionary<string, int>  defsCount = new Dictionary<string, int>();
			foreach (Building b in ship.Buildings)
			{
				if (!defsCount.ContainsKey(b.def.defName))
				{
					defsCount[b.def.defName] = 1;
				}
				else
				{
					defsCount[b.def.defName] += 1;
				}
			}
			foreach (VehiclePawn veh in shipMap2.mapPawns.AllPawnsSpawned.Where(pawn => pawn is VehiclePawn veh && veh.CompVehicleLauncher != null && veh.CompVehicleLauncher.SpaceFlight))
			{
				if (!defsCount.ContainsKey(veh.def.defName))
				{
					defsCount[veh.def.defName] = 1;
				}
				else
				{
					defsCount[veh.def.defName] += 1;
				}
			}

			foreach (string defString in defsLit)
			{
				int value = 0;
				defsCount.TryGetValue(defString, out value);
				csvText += "," + value;
			}

			csvText += ";\n";
		}
		private void ZoomToShip()
		{
			float minZoomSize = Find.CameraDriver.config.sizeRange.min;
			float maxZoomSize = Find.CameraDriver.config.sizeRange.max;

			float viewRectRatio = (float)Find.CameraDriver.CurrentViewRect.Width / Find.CameraDriver.CurrentViewRect.Height;
			float shipRatio = (float)currentShip.sizeX / currentShip.sizeZ;
			float scale = 0;
			int spacing = 10;
			if (currentShip.sizeX > 100 || currentShip.sizeZ > 100)
				spacing = 24;
			if (shipRatio > viewRectRatio) // long ship
			{
				scale = (float)(currentShip.sizeX + spacing) / Find.CameraDriver.CurrentViewRect.Width;
			}
			else
			{
				scale = (float)(currentShip.sizeZ + spacing) / Find.CameraDriver.CurrentViewRect.Height;
			}
			Find.CameraDriver.desiredSize = Find.CameraDriver.desiredSize * scale;
			Find.CameraDriver.Update();
			Find.CameraDriver.ApplyPositionToGameObject();
			CameraJumper.TryJump(new IntVec3(currentShip.offsetX + currentShip.sizeX / 2, 0, currentShip.offsetZ + currentShip.sizeZ / 2), shipMap2);
		}
		private void TakeShipScreenshot()
		{
			DirectoryInfo directoryInfo = new DirectoryInfo(GenFilePaths.ScreenshotFolderPath);
			if (!directoryInfo.Exists)
			{
				directoryInfo.Create();
			}

			string fileName = GenFilePaths.ScreenshotFolderPath + Path.DirectorySeparatorChar.ToString() + currentShip.defName + ".png";
			if (File.Exists(fileName))
			{
				Log.Warning("Screensot taker: File exists: " + fileName);
				return;
			}
			ScreenCapture.CaptureScreenshot(fileName);
		}
		private Map CreateMap()
		{
			int newTile = -1;
			for (int i = 0; i < 420; i++)
			{
				if (!Find.World.worldObjects.AnyMapParentAt(i))
				{
					newTile = i;
					break;
				}
			}
			Map map = GetOrGenerateMapUtility.GetOrGenerateMap(newTile, ResourceBank.WorldObjectDefOf.WreckSpace);
			map.GetComponent<ShipMapComp>().ShipMapState = ShipMapState.isGraveyard;
			CameraJumper.TryJump(map.Center, map);
			return map;
		}

		private void DetroyMapWithParent(Map map)
		{
			MapParent mapParent = map.Parent;
			KillAllVehicles(map);
			Current.Game.DeinitAndRemoveMap(map, notifyPlayer: false);
			mapParent.Destroy();
		}

		private void KillAllVehicles(Map map)
		{
			List<VehiclePawn> vehiclesToKill = new List<VehiclePawn>();
			foreach (Thing t in map.spawnedThings)
			{
				if (t is VehiclePawn v)
				{
					vehiclesToKill.Add(v);
				}
			}
			foreach (VehiclePawn v in vehiclesToKill)
			{
				// v.Kill(new DamageInfo(DamageDefOf.Bomb, 99999));
				v.Destroy();
			}
		}
		private void AddField(string field)
		{
			csvText += "," + field;
		}

		private void AddField(int field)
		{
			csvText += "," + field;
		}

		private void AddField(bool field)
		{
			csvText += "," + field;
		}

	}
	class Designator_ExportScreenshots : Designator
	{
			public override AcceptanceReport CanDesignateCell(IntVec3 loc)
		{
			return true;
		}

		public Designator_ExportScreenshots()
		{
			defaultLabel = "Export Screenshots";
			defaultDesc = "Export ship catalog screenshots into screenshots folder. Takes time, game should not be interfered with during taking screenshots." +
				" When finished, whill show UI back (F11 command). Click anywhere on the map to activate";
			soundDragSustain = SoundDefOf.Designate_DragStandard;
			soundDragChanged = SoundDefOf.Designate_DragStandard_Changed;
			useMouseIcon = true;
			soundSucceeded = SoundDefOf.Designate_Deconstruct;
		}

		//new save system from min x/z
		public override void DesignateSingleCell(IntVec3 loc)
		{
			Find.Selector.ClearSelection();
			Find.MainTabsRoot.SetCurrentTab(null);
			ScreenshotHelperWorldComp worldComp = (ScreenshotHelperWorldComp)Find.World.components.First(x => x is ScreenshotHelperWorldComp);
			Find.CameraDriver.config.sizeRange.max = 80;
			worldComp.screenshotShips = new List<ShipDef>()
			{
				/*DefDatabase<ShipDef>.GetNamed("CorvetteT1"),
				DefDatabase<ShipDef>.GetNamed("CorvetteT2"),
				DefDatabase<ShipDef>.GetNamed("DestroyerT1"),
				DefDatabase<ShipDef>.GetNamed("Dreadnought")*/
				DefDatabase<ShipDef>.GetNamed("BattlecruiserCrescent"),
				DefDatabase<ShipDef>.GetNamed("CarrierHeavy"),
				DefDatabase<ShipDef>.GetNamed("FighterMicro"),
				DefDatabase<ShipDef>.GetNamed("StartShipH"),
				DefDatabase<ShipDef>.GetNamed("GardenShip")
			};
			worldComp.StartScreenshotsSequence();

		}
	}
}
