using UnityEngine;
using Verse;
using RimWorld;

namespace SaveOurShip2
{
	class Designator_MakeAllShipblueprints : Designator
	{
		public override AcceptanceReport CanDesignateCell(IntVec3 loc)
		{
			return true;
		}

		public Designator_MakeAllShipblueprints()
		{
			defaultLabel = "Make all ship blueprints Ship";
			defaultDesc = "Generate a blueprint object for each ship in current language and save that to file. Click anywhere on the map to activate.";
			icon = ContentFinder<Texture2D>.Get("UI/Save_XML");
			soundDragSustain = SoundDefOf.Designate_DragStandard;
			soundDragChanged = SoundDefOf.Designate_DragStandard_Changed;
			useMouseIcon = true;
			soundSucceeded = SoundDefOf.Designate_Deconstruct;
		}

		//new save system from min x/z
		public override void DesignateSingleCell(IntVec3 loc)
		{
			BlueprintMakerCommand.MakeBlueprints();
		}
	}
}
