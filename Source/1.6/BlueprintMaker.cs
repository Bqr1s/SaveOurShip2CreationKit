using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Verse;
using RimWorld;
using LudeonTK;

namespace SaveOurShip2
{
	// This command will create an XML file containing blueprint object for each ship def present in current game
    // with short descriotion and in launguage that is active, so can automatically generate not translations, but mod replacements
    // in another languages
    public static class BlueprintMakerCommand
	{
        [DebugAction("SOS2", null, false, false, false, false, false, 0, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        // Using mod name to prevent conflict with this command expected to be added to base game
        public static void MakeBlueprints()
        {
            // Read pattern file
            string path = Path.Combine(GenFilePaths.SaveDataFolderPath, "Sos2Allblueprints", "Template.xml");
            //string template = File.ReadAllText(path);
            string beginning = @"<?xml version=""1.0"" encoding=""utf-8"" ?>
<Defs>
";
            string ending = @"
</Defs>";
            string template = @"<ThingDef ParentName=""ShipBlueprintBase"">
	<defName>{0}</defName>
	<label>{1}</label>
	<description>{2}</description>
	<statBases>
		<MarketValue>250</MarketValue>
	</statBases>
	<comps>
		<li Class=""SaveOurShip2.CompProps_ShipBlueprint"">
			<shipDef>{3}</shipDef>
		</li>
	</comps>
</ThingDef>
";

            string result = beginning;
            int count = 1;
            foreach(ShipDef ship in DefDatabase<ShipDef>.AllDefs)
            {
                if (Char.IsNumber(ship.defName.Last()))
                {
                    continue;
                }
                string bpDefName = "AllBP_" + ship.defName;
                string label = TranslatorFormattedStringExtensions.Translate("SoS.CK.BlueprintLabel", ship.label);
                string description = TranslatorFormattedStringExtensions.Translate("SoS.CK.BlueprintDesc", ship.combatPoints, ship.sizeX, ship.sizeZ);
                result += String.Format(template, bpDefName, label, description, ship.defName);
                count++;
                if (count > 10)
                {
                    // break;
                }
            }
            result += ending;
            string resultPath = Path.Combine(GenFilePaths.SaveDataFolderPath, "Sos2Allblueprints", "AllBlueprints.xml");
            File.WriteAllText(resultPath, result);
            Messages.Message("File created: " + resultPath, MessageTypeDefOf.NeutralEvent);
        }
    }
	public class BlueprintMaker
	{

	}
}
