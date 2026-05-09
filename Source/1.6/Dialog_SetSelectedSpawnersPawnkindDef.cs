using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;


namespace SaveOurShip2
{
	public class Dialog_SetSelectedSpawnersPawnkindDef: Dialog_RenameShip
	{
		protected override void SetName(string name)
		{
			if (string.IsNullOrEmpty(name))
				return;

			// Will rename all selected spawners, just easy implementation for one gizmo working for 
			// renaming multiple objects using dialog.
			List<ThingWithComps> things = Find.Selector.SelectedObjects.OfType<ThingWithComps>().ToList();
			foreach(ThingWithComps thing in things)
			{
				CompNameMe comp = thing.TryGetComp<CompNameMe>();
				if (comp != null)
				{
					comp.pawnKindDef = name;
				}
			}
		}
	}
}