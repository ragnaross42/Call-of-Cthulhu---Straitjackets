﻿// ----------------------------------------------------------------------
// These are basic usings. Always let them be here.
// ----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

// ----------------------------------------------------------------------
// These are RimWorld-specific usings. Activate/Deactivate what you need:
// ----------------------------------------------------------------------
using UnityEngine;         // Always needed
//using VerseBase;         // Material/Graphics handling functions are found here
using Verse;               // RimWorld universal objects are here (like 'Building')
using Verse.AI;          // Needed when you do something with the AI
using Verse.AI.Group;
using Verse.Sound;       // Needed when you do something with Sound
using Verse.Noise;       // Needed when you do something with Noises
using RimWorld;            // RimWorld specific functions are found here (like 'Building_Battery')
using RimWorld.Planet;   // RimWorld specific functions for world creation
//using RimWorld.SquadAI;  // RimWorld specific functions for squad brains 

namespace StraitJacket
{
    class MapComponent_StraitJacket : MapComponent
    {
        private Map mapRecord = null;
        private int lazyTick = 750;

        public MapComponent_StraitJacket(Map map) : base(map)
        {
            this.map = map;
            this.mapRecord = map;
        }

        public static MapComponent_StraitJacket GetComponent(Map map)
        {
            MapComponent_StraitJacket result = map.components.OfType<MapComponent_StraitJacket>().FirstOrDefault<MapComponent_StraitJacket>();
            if (result == null)
            {
                result = new MapComponent_StraitJacket(map);
                map.components.Add(result);
            }
            return result;
        }

        public override void MapComponentTick()
        {
            lazyTick--;
            if (lazyTick < 0) {
                lazyTick = 750;
                PerformStraitJacketCheck();
            }
            base.MapComponentTick();
        }

        // Verse.MapPawns
        public IEnumerable<Pawn> Prisoners(Map map)
        {
                return from x in map.mapPawns.AllPawns
                       where x.IsPrisoner
                       select x;
        }

        private void PerformStraitJacketCheck()
        {
            if (map.mapPawns != null)
            {
                if (map.mapPawns.FreeColonists != null)
                {

                    HashSet<Pawn> colonists = new HashSet<Pawn>(map.mapPawns.FreeColonists);
                    HashSet<Pawn> prisoners = new HashSet<Pawn>(Prisoners(map));
                    HashSet<Pawn> others = new HashSet<Pawn>(map.mapPawns.AllPawns.Where(x => (x?.RaceProps?.Humanlike ?? false) && x?.Faction != Faction.OfPlayer));
                    bool giveThoughtToAll = false;
                    Pawn straightjackedPawn = null;
                    Hediff pawnJacketHediff = null;
                    
                    //Check our prisoners first
                    foreach (Pawn p in prisoners.Concat(others))
                    {
                        
                        if (p.apparel != null)
                        {
                            bool jacketOn = false;
                            foreach (Apparel apparel in p.apparel.WornApparel)
                            {
                                if (apparel.def == StraitjacketDefOf.ROM_Straitjacket)
                                {
                                    jacketOn = true;
                                    //Log.Message("Straitjacket Prisoner Check");

                                    straightjackedPawn = p;
                                    p.needs.mood.thoughts.memories.TryGainMemory(StraitjacketDefOf.ROM_WoreStraitjacket);

                                    pawnJacketHediff = p.health.hediffSet.GetFirstHediffOfDef(StraitjacketDefOf.ROM_RestainedByStraitjacket);
                                    if (pawnJacketHediff == null)
                                    {
                                        pawnJacketHediff = HediffMaker.MakeHediff(StraitjacketDefOf.ROM_RestainedByStraitjacket, p);
                                        p.health.AddHediff(pawnJacketHediff);
                                    }
                                }
                            }

                            if (!jacketOn)
                            {
                                pawnJacketHediff = p.health.hediffSet.GetFirstHediffOfDef(StraitjacketDefOf.ROM_RestainedByStraitjacket);
                                if (pawnJacketHediff != null)
                                {
                                    p.health.RemoveHediff(pawnJacketHediff);
                                }
                            }
                        }
                    }

                    //Check our colonists
                    foreach (Pawn p in colonists)
                    {
                        if (p.apparel != null)
                        {
                            bool jacketOn = false;

                            foreach (Apparel apparel in p.apparel.WornApparel)
                            {
                                if (apparel.def == StraitjacketDefOf.ROM_Straitjacket)
                                {
                                    straightjackedPawn = p;
                                    p.needs.mood.thoughts.memories.TryGainMemory(StraitjacketDefOf.ROM_WoreStraitjacket);
                                    jacketOn = true;

                                    if (pawnJacketHediff == null)
                                    {
                                        pawnJacketHediff = HediffMaker.MakeHediff(StraitjacketDefOf.ROM_RestainedByStraitjacket, p);
                                        p.health.AddHediff(pawnJacketHediff);
                                    }

                                    giveThoughtToAll = true; //Different than prisoners
                                }
                            }
                            if (!jacketOn)
                            {
                                pawnJacketHediff = p.health.hediffSet.GetFirstHediffOfDef(StraitjacketDefOf.ROM_RestainedByStraitjacket);
                                if (pawnJacketHediff != null)
                                {
                                    p.health.RemoveHediff(pawnJacketHediff);
                                }
                            }
                        }
                    }
                    if (giveThoughtToAll)
                    {
                        foreach (Pawn p in colonists)
                        {
                            if (p != straightjackedPawn)
                            {
                                p.needs.mood.thoughts.memories.TryGainMemory(StraitjacketDefOf.ROM_ColonistWoreStraitjacket);
                            }
                        }
                    }

                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref this.lazyTick, "lazyTick", 750, false);
        }
    }
}
