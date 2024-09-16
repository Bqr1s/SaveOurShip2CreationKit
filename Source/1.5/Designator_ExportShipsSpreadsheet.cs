using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Verse;
using RimWorld;

namespace SaveOurShip2
{
	class Designator_ExportShipsSpreadsheet : Designator
	{
		public override AcceptanceReport CanDesignateCell(IntVec3 loc)
		{
			return true;
		}

		public Designator_ExportShipsSpreadsheet()
		{
			defaultLabel = "Export Ships Spreadsheet";
			defaultDesc = "Save ship defs spreadsheet to a file. Click anywhere on the map to activate.";
			soundDragSustain = SoundDefOf.Designate_DragStandard;
			soundDragChanged = SoundDefOf.Designate_DragStandard_Changed;
			useMouseIcon = true;
			soundSucceeded = SoundDefOf.Designate_Deconstruct;
		}

		//new save system from min x/z
		public override void DesignateSingleCell(IntVec3 loc)
		{
			string path = Path.Combine(GenFilePaths.SaveDataFolderPath, "ExportedShips");
			DirectoryInfo dir = new DirectoryInfo(path);
			if (!dir.Exists)
				dir.Create();

			string fileName = Path.Combine(path, "ShipList.csv");
			string text = "";

			List<ShipDef> shipDefs = DefDatabase<ShipDef>.AllDefs.ToList();

			text += "Def name,Label,CombatPoints,SizeX,SizeZ,TradeShip,CargoValue,NeverAttacks,NeverWreck\n";
			foreach (ShipDef ship in shipDefs)
			{
				text += ship.defName;
				AddField(ref text, ship.label);
				AddField(ref text, ship.combatPoints);
				AddField(ref text, ship.sizeX);
				AddField(ref text, ship.sizeZ);
				AddField(ref text, ship.tradeShip);
				AddField(ref text, ship.cargoValue);
				AddField(ref text, ship.neverAttacks);
				AddField(ref text, ship.neverWreck);

				text += "\n";
			}
			File.WriteAllText(fileName, text);
		}

		private void AddField(ref string text, string field)
		{
			text += "," + field;
		}

		private void AddField(ref string text, int field)
		{
			text += "," + field;
		}

		private void AddField(ref string text, bool field)
		{
			text += "," + field;
		}
	}
}
