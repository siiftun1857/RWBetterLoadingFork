using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace BetterLoading
{
    public class LoadingScreen : MonoBehaviour
    {
        public static LoadingScreen Instance { get; private set; }
        
        public bool shouldShow = true;

        public EnumLoadingStage currentStage = EnumLoadingStage.CreateClasses;

        public int numModClasses;
        public int currentModClassBeingInstantiated;
        public string modBeingInstantiatedName;

        public ModContentPack currentlyLoadingDefsFrom;
        public int totalLoadedContentPacks;
        public int numContentPacksLoaded;

        public int numPatchesToLoad;
        public int numPatchesLoaded;
        public ModContentPack currentlyPatching;

        public int numDefsToPreProcess;
        public int numDefsPreProcessed;

        public int numDefsToProcess;
        public int numDefsProcessed;

        public int numDefDatabases;
        public int numDatabasesReloaded;
        public Type currentDatabaseResolving;

        public int numStaticConstructorsToCall;
        public int numStaticConstructorsCalled;
        public Type currentStaticConstructor;

        //------------File Loading--------------
        public int numWorldGeneratorsToRun;
        public int numWorldGeneratorsRun;
        public WorldGenStep currentWorldGenStep;

        public List<Map> maps = new List<Map>();

        public int mapIndexSpawningItems = -1;
        public int numObjectsToSpawnCurrentMap;
        public int numObjectsSpawnedCurrentMap;

        public LoadingScreen()
        {
            Instance = this;
        }

        private void DrawInitialGameLoad()
        {
            //In terms of stages, we have:
            //
            //Initialize mods - this just verifies files exist etc and is pre-instantiation so cannot be shown
            //
            //Load mod content - this loads assemblies and schedules the load of audio clips, textures, and strings. Again, pre-instantiation.
            //
            //Create mod classes (that's where this gets setup, so it's unlikely that we'll be able to display this fully/at all)
            //
            //Loading of xml files in defs folder (LoadedModManager#LoadModXML) - can be displayed as a progress bar
            //
            //XML Unification (LoadedModManager#CombineIntoUnifiedXML) - may be doable as a progress bar, may just be easier to show it's being done
            //
            //Patch application. Loaded per mod in ModContentPack#LoadPatches and then applied in blocks (a mod at a time) by PatchOperation#Apply (but this is overridden, so... does harmony work?)
            //    This runs as Load Mod Patches -> Apply one at a time -> load next mod -> apply -> etc
            //
            //Parse + Process XML. Two time consuming stages:
            //    - Register all inheritence nodes (XMLInheritence#TryRegister for each xmlDoc.DocumentElement.ChildNodes in param for LoadedModManager#ParseAndProcessXML)
            //    - Addition of Defs - DirectXmlLoader#DefFromNode for each def followed OPTIONALLY by DefPackage#AddDef if it loads (which not all of even the vanilla ones do).
            //
            //Freeing of memory (probably don't need to show) via LoadedModManager#ClearCachedPatches and XmlInheritance#Clear

            //Draw window
            var rect = new Rect(100, 100, UI.screenWidth - 200, UI.screenHeight - 200);
            rect = rect.Rounded();
            UIMenuBackgroundManager.background.BackgroundOnGUI();
            Widgets.DrawShadowAround(rect);
            Widgets.DrawWindowBackground(rect);

            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.UpperCenter;

            rect.y += 20; //Nudge down for title

            Widgets.Label(rect, "BetterLoading :: Game Loading, Please Wait...");

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;

            rect.x += 20; //Indent
            rect.width -= 20;

            //------------------------Mod Construction------------------------
            rect.y += 50; //Move down a bit
            Widgets.Label(rect, currentStage == EnumLoadingStage.CreateClasses ? $"Constructing Mods ({currentModClassBeingInstantiated}/{numModClasses}): {modBeingInstantiatedName}" : "Mods Constructed");

            //Draw a bar
            var barRect = new Rect(rect.x, rect.y + 25, rect.width - 24, 20);
            Widgets.FillableBar(barRect, currentStage == EnumLoadingStage.CreateClasses ? numModClasses == 0 ? 0 : (float) currentModClassBeingInstantiated / numModClasses : 1);

            //------------------------Def XML Reading------------------------
            rect.y += 50;
            Widgets.Label(rect,
                $"Reading Def XML ({numContentPacksLoaded}/{(totalLoadedContentPacks == 0 ? "<waiting>" : "" + totalLoadedContentPacks)}): {(currentlyLoadingDefsFrom != null && currentlyLoadingDefsFrom.Name.Length > 0 ? currentlyLoadingDefsFrom.Name : "Waiting...")}");

            //Draw a bar
            barRect = new Rect(rect.x, rect.y + 25, rect.width - 24, 20);
            Widgets.FillableBar(barRect,
                totalLoadedContentPacks == 0 ? 0 : (float) numContentPacksLoaded / totalLoadedContentPacks);

            //------------------------XML Unification------------------------
            rect.y += 50;
            Widgets.Label(rect,
                currentStage < EnumLoadingStage.UnifyXML ? "Waiting for XML Load To Complete..." :
                currentStage == EnumLoadingStage.UnifyXML ? "Building Def Tree" : "Finished Building Def Tree");

            //Draw a bar
            barRect = new Rect(rect.x, rect.y + 25, rect.width - 24, 20);
            Widgets.FillableBar(barRect, currentStage <= EnumLoadingStage.UnifyXML ? 0 : 1);

            //------------------------Patch Application------------------------
            rect.y += 50;
            Widgets.Label(rect,
                currentStage < EnumLoadingStage.ApplyPatches
                    ? "Waiting for XML Tree..."
                    : currentStage == EnumLoadingStage.ApplyPatches
                        ? $"Applying mod patches ({numPatchesLoaded}/{numPatchesToLoad}): {(currentlyPatching == null ? "<waiting>" : currentlyPatching.Name)}"
                        : "Patches Applied");

            //Draw a bar
            barRect = new Rect(rect.x, rect.y + 25, rect.width - 24, 20);
            Widgets.FillableBar(barRect,
                currentStage < EnumLoadingStage.ApplyPatches ? 0 : numPatchesLoaded / (float) numPatchesToLoad);

            //------------------------XML Parse/Process Stage 1------------------------
            rect.y += 50;
            Widgets.Label(rect, currentStage < EnumLoadingStage.ParseProcessXMLStage1
                ? "Waiting for patches to be applied..."
                : currentStage == EnumLoadingStage.ParseProcessXMLStage1
                    ? $"Registering Defs from Patched XML: {numDefsPreProcessed}/{numDefsToPreProcess} ({((float) numDefsPreProcessed / numDefsToPreProcess).ToStringPercent()})"
                    : "Defs Registered");

            barRect = new Rect(rect.x, rect.y + 25, rect.width - 24, 20);
            Widgets.FillableBar(barRect,
                numDefsToPreProcess == 0 ? 0 : (float) numDefsPreProcessed / numDefsToPreProcess);

            //------------------------XML Parse/Process Stage 2------------------------
            rect.y += 50;
            Widgets.Label(rect, currentStage < EnumLoadingStage.ParseProcessXMLStage2
                ? "Waiting for defs to be registered..."
                : currentStage == EnumLoadingStage.ParseProcessXMLStage2
                    ? $"Creating Defs from Patched XML: {numDefsProcessed}/{numDefsToProcess} ({((float) numDefsProcessed / numDefsToProcess).ToStringPercent()})"
                    : "Defs Created");

            barRect = new Rect(rect.x, rect.y + 25, rect.width - 24, 20);
            Widgets.FillableBar(barRect,
                numDefsToProcess == 0 ? 0 : (float) numDefsProcessed / numDefsToProcess);

            //------------------------Reference Resolving------------------------
            rect.y += 50;
            Widgets.Label(rect, currentStage < EnumLoadingStage.ResolveReferences
                ? "Waiting for defs to be created..."
                : currentStage == EnumLoadingStage.ResolveReferences
                    ? $"Reloading DefDatabase: {currentDatabaseResolving.Name} ({numDatabasesReloaded}/{numDefDatabases})"
                    : "Databases Reloaded");

            barRect = new Rect(rect.x, rect.y + 25, rect.width - 24, 20);
            Widgets.FillableBar(barRect,
                numDefDatabases == 0 ? 0 : (float) numDatabasesReloaded / numDefDatabases);

            //------------------------Finishing Up------------------------
            rect.y += 50;
            Widgets.Label(rect,
                currentStage < EnumLoadingStage.FinishUp
                    ? "Waiting for databases to finish reload..."
                    : currentStage == EnumLoadingStage.FinishUp
                        ? $"Running Startup Static CCtors: {currentStaticConstructor?.FullName} ({numStaticConstructorsCalled}/{numStaticConstructorsToCall})"
                        : "Finished");

            barRect = new Rect(rect.x, rect.y + 25, rect.width - 24, 20);
            Widgets.FillableBar(barRect,
                numStaticConstructorsToCall == 0
                    ? 0
                    : (float) numStaticConstructorsCalled / numStaticConstructorsToCall);

            Text.Anchor = TextAnchor.UpperLeft; //Reset this
        }

        private void DrawSaveFileLoad()
        {
            //Draw window
            var rect = new Rect(100, 100, UI.screenWidth - 200, UI.screenHeight - 200);
            rect = rect.Rounded();
            UIMenuBackgroundManager.background.BackgroundOnGUI();
            Widgets.DrawShadowAround(rect);
            Widgets.DrawWindowBackground(rect);

            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.UpperCenter;

            rect.y += 20; //Nudge down for title

            Widgets.Label(rect, "BetterLoading :: Loading Save File...");

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;

            rect.x += 20; //Indent
            rect.width -= 20;

            //Stages for this, in terms of actual time consuming events:
            //Scene Load is triggered for "Play" scene, which triggers Verse.Root_Play#Start
            //This then calls QueueLongEvent (Action, string, bool, Action) with the action being call SavedGameLoaderNow.LoadGameFromSaveFileNow - which is probably where the ACTUAL save file logic is.
            //    The event name is "LoadingLongEvent" and it's async
            //If something goes wrong it calls GameAndMapInitExceptionHandlers.ErrorWhileLoadingGame - cleanup this?
            //The key clue that the game load has begun is ScribeLoader#InitLoading being called which buffers the save file into an XmlDocument, and saves it into ScribeLoader.curXmlParent
            //    Scribe.mode is set to LoadingVars once the file is buffered in - but this doesn't have a setter to hook.
            //Once the file is read then Game#LoadGame is called. This is the time consuming bit.
            //
            //Stage one could be "Load Small Components" (Game#ExposeSmallComponents) as this picks up a few small details such as research, the tutor, the scenario, etc.
            //
            //Second stage start coincides with vanilla's "Loading World" and hook could go in World's constructor or World#ExposeData   
            //    This is split up into loading of the world info (i.e. seed, coverage, etc) which is quick
            //    And the grid, which may not be.
            //    Then WorldGenerator.GenerateFromScribe (or MAYBE GenerateWithoutWorldData) will be called
            //        -Progress bar this? We can do a hook in WorldGenStep#GenerateFromScribe/GenerateWithoutWorldData to increment, and the total is equal to WorldGenerator.GenStepsInOrder.Length
            //    Then World#FinalizeInit is called, which recalcs paths and has a callback for all WorldComponents (FinalizeInit method).
            //
            //Next Stage is vanilla's "Loading Map" 
            //    This is deceptively simple as it just calls Map#ExposeData once per map.
            //        First part of this instantiates 80-odd classes and a bunch of MapComponents, but SHOULD be quite quick? (Call to Map#ConstructComponents)
            //        Second part populates all 80 classes and map components with saved data (call to Map#ExposeComponents) 
            //        Third part loads compressed stuff as a byte array (Call to MapFileCompressor#ExposeData)
            //        Fourth part loads uncompressed stuff, and it's a direct call to Scribe_Collections#Look
            //    It then sets the current map index
            //
            //Next is vanilla's "Initializing Game" which actually just loads the camera pos (CameraDriver#Expose)
            //
            //Next is "Finalize Loading" which resolves cross references, sets the current load mode to Inactive, and calls post-load callbacks
            //
            //Then "Spawning All Things"
            //    This first off calls Map#FinalizeLoading for each map, which:
            //        Merges Map#loadedFullThings and MapCompressor#ThingsToSpawnAfterLoad()'s return value (hook said after load method?)
            //        Actually spawns stuff
            //            GenSpawn.Spawn for every non-building item in the new list (get total based on size of the two?)
            //            GenSpawn#SpawnBuildingAsPossible for every building in same list.
            //            Then calls GenPlace#TryPlaceThing for a few things if loading an older version
            //            Finally calls Map#FinalizeInit which recalculates and rebuilds a bunch of stuff and calls PostMapInit for each
            //
            //Finally Game#FinalizeInit is called, which flushes the log file, applies research mods, and calls GameComponent#FinalizeInit for each component in Current.Game.components

            //----------------Load Small Components------------
            rect.y += 50;
            Widgets.Label(rect,
                currentStage == EnumLoadingStage.LoadSmallComponents
                    ? "Loading Misc Game Data..."
                    : "Basic Game Data Loaded");

            //bar
            var barRect = new Rect(rect.x, rect.y + 25, rect.width - 24, 20);
            Widgets.FillableBar(barRect, currentStage == EnumLoadingStage.LoadSmallComponents ? 0 : 1);

            //----------------Load World Map------------
            rect.y += 50;
            Widgets.Label(rect,
                currentStage < EnumLoadingStage.LoadWorldMap ? "Waiting for game data load..." :
                currentStage == EnumLoadingStage.LoadWorldMap ? "Loading World Map..." : "World Map Loaded");

            //bar
            barRect = new Rect(rect.x, rect.y + 25, rect.width - 24, 20);
            Widgets.FillableBar(barRect, currentStage <= EnumLoadingStage.LoadWorldMap ? 0 : 1);

            //----------------Generate World Features------------
            rect.y += 50;
            Widgets.Label(rect, currentStage < EnumLoadingStage.GenerateWorldData
                ? "Waiting for world map..."
                : currentStage == EnumLoadingStage.GenerateWorldData
                    ? $"Generating World Feature: {currentWorldGenStep} ({numWorldGeneratorsRun}/{numWorldGeneratorsToRun})"
                    : "World Features Generated");

            //bar
            barRect = new Rect(rect.x, rect.y + 25, rect.width - 24, 20);
            Widgets.FillableBar(barRect,
                numWorldGeneratorsToRun == 0 ? 0 : (float) numWorldGeneratorsRun / numWorldGeneratorsToRun);

            //----------------Finalizing World------------
            rect.y += 50;
            Widgets.Label(rect, currentStage < EnumLoadingStage.FinalizeWorld
                ? "Waiting for feature generation..."
                : currentStage == EnumLoadingStage.FinalizeWorld
                    ? "Applying finishing touches to world..."
                    : "World Finalized.");

            //bar
            barRect = new Rect(rect.x, rect.y + 25, rect.width - 24, 20);
            Widgets.FillableBar(barRect, currentStage <= EnumLoadingStage.FinalizeWorld ? 0 : 1);

            //----------------Map Loading------------
            rect.y += 50;

            if (currentStage >= EnumLoadingStage.LoadMaps_ConstructComponents)
            {
                if (currentStage <= EnumLoadingStage.LoadMaps_LoadData)
                {
                    Widgets.Label(rect, "Loading Maps...");
                    barRect = new Rect(rect.x, rect.y + 25, rect.width - 24, 20);
                    Widgets.FillableBar(barRect, 0.2f);
                }
                else
                {
                    Widgets.Label(rect, "Maps Loaded");
                    barRect = new Rect(rect.x, rect.y + 25, rect.width - 24, 20);
                    Widgets.FillableBar(barRect, 1);
                }

                rect.y += 50;
                rect.x += 25; //Indent
                rect.width -= 25;

                var num = 0;
                foreach (var unused in maps)
                {
                    if (num < maps.Count - 1 || currentStage > EnumLoadingStage.LoadMaps_LoadData)
                    {
                        //This map is loaded fully
                        Widgets.Label(rect, "Map " + (num + 1) + ": Loaded");

                        //bar
                        barRect = new Rect(rect.x, rect.y + 25, rect.width - 24, 20);
                        Widgets.FillableBar(barRect, 1);
                    }
                    else
                    {
                        //This map is partially loaded
                        Widgets.Label(rect,
                            "Map " + (num + 1) + ": " + (currentStage == EnumLoadingStage.LoadMaps_ConstructComponents
                                ? "Constructing Components..."
                                : currentStage == EnumLoadingStage.LoadMaps_LoadComponents
                                    ? "Loading Misc Map Details..."
                                    : "Reading Object Data..."));

                        barRect = new Rect(rect.x, rect.y + 25, rect.width - 24, 20);
                        Widgets.FillableBar(barRect,
                            (float) (currentStage + 1 - EnumLoadingStage.LoadMaps_ConstructComponents) / 5);
                    }

                    num++;
                    rect.y += 50;
                }

                rect.x -= 25; //Unindent
                rect.width += 25;
            }
            else if (currentStage < EnumLoadingStage.LoadMaps_LoadComponents)
            {
                Widgets.Label(rect, "Waiting for map data...");
                barRect = new Rect(rect.x, rect.y + 25, rect.width - 24, 20);
                Widgets.FillableBar(barRect, 0);
            }

            //----------------Init Camera------------
            Widgets.Label(rect, currentStage < EnumLoadingStage.InitCamera
                ? "Waiting for maps to finish loading..."
                : currentStage == EnumLoadingStage.InitCamera
                    ? "Setting up camera..."
                    : "Camera Setup Complete");

            //bar
            barRect = new Rect(rect.x, rect.y + 25, rect.width - 24, 20);
            Widgets.FillableBar(barRect, currentStage <= EnumLoadingStage.InitCamera ? 0 : 1);

            //----------------Resolve Cross-References------------
            rect.y += 50;
            Widgets.Label(rect, currentStage < EnumLoadingStage.ResolveSaveFileCrossReferences
                ? "Waiting for camera setup..."
                : currentStage == EnumLoadingStage.ResolveSaveFileCrossReferences
                    ? "Resolving Def Cross-References..."
                    : "Defs Successfully Cross-Referenced");

            //bar
            barRect = new Rect(rect.x, rect.y + 25, rect.width - 24, 20);
            Widgets.FillableBar(barRect, currentStage <= EnumLoadingStage.ResolveSaveFileCrossReferences ? 0 : 1);

            //----------------Spawning All Things------------
            rect.y += 50;


            if (currentStage > EnumLoadingStage.SpawnThings_RebuildRecalc)
            {
                Widgets.Label(rect, "Things Spawned");
                barRect = new Rect(rect.x, rect.y + 25, rect.width - 24, 20);
                Widgets.FillableBar(barRect, 1);
            }
            else if (currentStage >= EnumLoadingStage.SpawnThings_NonBuildings)
            {
                Widgets.Label(rect, "Spawning all things...");
                barRect = new Rect(rect.x, rect.y + 25, rect.width - 24, 20);
                Widgets.FillableBar(barRect,  (mapIndexSpawningItems + 1f) / (maps.Count + 1f));
            }
            else
            {
                Widgets.Label(rect, "Waiting for defs to be cross-referenced...");
                barRect = new Rect(rect.x, rect.y + 25, rect.width - 24, 20);
                Widgets.FillableBar(barRect, 0);
            }

            rect.y += 50;
            rect.x += 25; //Indent
            rect.width -= 25;

            var num2 = 0;
            foreach (var unused in maps)
            {
                if (num2 < mapIndexSpawningItems || currentStage > EnumLoadingStage.SpawnThings_RebuildRecalc)
                {
                    //This map is loaded fully
                    Widgets.Label(rect, "Map " + (num2 + 1) + ": Everything Spawned");

                    //bar
                    barRect = new Rect(rect.x, rect.y + 25, rect.width - 24, 20);
                    Widgets.FillableBar(barRect, 1);
                }
                else if (num2 == mapIndexSpawningItems)
                {
                    //This map is partially loaded
                    Widgets.Label(rect,
                        "Map " + (num2 + 1) + ": " + (currentStage == EnumLoadingStage.SpawnThings_NonBuildings
                            ? "Spawning Items..."
                            : currentStage == EnumLoadingStage.SpawnThings_Buildings
                                ? "Spawning Buildings..."
                                : currentStage == EnumLoadingStage.SpawnThings_BackCompat
                                    ? "Upgrading Level Format..."
                                    : "Rebuilding & Recalculating Pathfinding Map etc..."));

                    barRect = new Rect(rect.x, rect.y + 25, rect.width - 24, 20);
                    Widgets.FillableBar(barRect, (float) (currentStage + 1 - EnumLoadingStage.SpawnThings_NonBuildings) / 5);
                }
                else
                {
                    //This map is not yet loaded
                    Widgets.Label(rect, "Map " + (num2 + 1) + ": Waiting...");

                    //bar
                    barRect = new Rect(rect.x, rect.y + 25, rect.width - 24, 20);
                    Widgets.FillableBar(barRect, 0);
                }

                num2++;
                rect.y += 50;
            }

            rect.x -= 25; //Unindent
            rect.width += 25;

            //----------------Finalize Load------------
            Widgets.Label(rect, currentStage < EnumLoadingStage.FinalizeLoad
                ? "Waiting for things to finish spawning..."
                : currentStage == EnumLoadingStage.FinalizeLoad
                    ? "Finalizing Game State..."
                    : "Load Complete.");

            //bar
            barRect = new Rect(rect.x, rect.y + 25, rect.width - 24, 20);
            Widgets.FillableBar(barRect, currentStage <= EnumLoadingStage.FinalizeLoad ? 0 : 1);
        }

        public void OnGUI()
        {
            if (!LongEventHandler.AnyEventNowOrWaiting)
            {
                shouldShow = false;
            }

            if (!shouldShow) return;

            if (currentStage <= EnumLoadingStage.FinishUp) DrawInitialGameLoad();
            else DrawSaveFileLoad();
        }
    }
}