﻿// Copyright 2020 Jamie Taylor
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buildings;

namespace RangeHighlight {
    internal class Integrations {
        private TheMod theMod;
        public Integrations(TheMod theMod) {
            this.theMod = theMod;
            IntegratePrismaticTools();
            IntegrateRadioactiveTools();
            IntegrateBetterJunimos();
            IntegrateBetterBeehouses();
            IntegrateBetterSprinklers();
            IntegrateSimpleSprinklers();
            IntegrateLineSprinklers();
        }

        private void IntegratePrismaticTools() {
            IPrismaticToolsAPI? api = theMod.helper.ModRegistry.GetApi<IPrismaticToolsAPI>("stokastic.PrismaticTools");
            if (api == null) return;
            theMod.defaultShapes.prismaticSprinkler = theMod.api.GetSquareCircle((uint)api.SprinklerRange);
        }
        private void IntegrateRadioactiveTools() {
            RadioactiveToolsAPI? api = theMod.helper.ModRegistry.GetApi<RadioactiveToolsAPI>("kakashigr.RadioactiveTools");
            if (api == null) return;
            theMod.defaultShapes.radioactiveSprinkler = theMod.api.GetSquareCircle((uint)api.SprinklerRange);
        }
        private void IntegrateBetterJunimos() {
            IBetterJunimosAPI? api = theMod.helper.ModRegistry.GetApi<IBetterJunimosAPI>("hawkfalcon.BetterJunimos");
            if (api == null) return;
            // Lots of duplicated code here, but it's the best we can do without adding something to the
            // api just for the purpose of letting us fiddle with internal structures
            void setRange() {
                int r = api.GetJunimoHutMaxRadius();
                if (r > 1) {
                    theMod.defaultShapes.SetJunimoRange((uint)r);
                } else {
                    theMod.Monitor.LogOnce($"ignoring nonsense value {r} from Better Junimos for Junimo Hut radius", LogLevel.Info);
                }
            }
            theMod.api.RemoveBuildingRangeHighlighter("jltaylor-us.RangeHighlight/junimoHut");
            theMod.api.AddBuildingRangeHighlighter("jltaylor-us.RangeHighlight/better-junimoHut",
                () => theMod.config.ShowJunimoRange,
                () => theMod.config.ShowJunimoRangeKey,
                blueprint => {
                    if (blueprint.name == "Junimo Hut") {
                        setRange();
                        return new Tuple<Color, bool[,], int, int>(theMod.config.JunimoRangeTint, theMod.defaultShapes.junimoHut, 1, 1);
                    } else {
                        return null;
                    }
                },
                building => {
                    setRange();
                    if (building is JunimoHut) {
                        setRange();
                        return new Tuple<Color, bool[,], int, int>(theMod.config.JunimoRangeTint, theMod.defaultShapes.junimoHut, 1, 1);
                    } else {
                        return null;
                    }
                });
        }
        private void IntegrateBetterBeehouses() {
            IBetterBeehousesAPI? api = theMod.helper.ModRegistry.GetApi<IBetterBeehousesAPI>("tlitookilakin.BetterBeehouses");
            if (api == null) return;
            theMod.api.RemoveItemRangeHighlighter("jltaylor-us.RangeHighlight/beehouse");
            bool[,] beehouseShape = { };
            int lastVal = 0;
            theMod.api.AddItemRangeHighlighter("jltaylor-us.RangeHighlight/better-beehouses",
                () => theMod.config.ShowBeehouseRange,
                () => theMod.config.ShowBeehouseRangeKey,
                () => theMod.config.ShowOtherBeehousesWhenHoldingBeehouse,
                () => {
                    int r = api.GetSearchRadius();
                    if (r != lastVal) {
                        lastVal = r;
                        if (r > 1) {
                            beehouseShape = theMod.api.GetManhattanCircle((uint)r);
                        } else {
                            theMod.Monitor.Log($"ignoring nonsense value {r} from Better Beehouses for Flower search radius", LogLevel.Info);
                            beehouseShape = theMod.defaultShapes.beehouse;
                        }
                    }
                },
                (item, itemID, itemName) => {
                    if (itemName.Contains("bee house")) {
                        return new List<Tuple<Color, bool[,]>>(1) { new (theMod.config.BeehouseRangeTint, beehouseShape) };
                    } else {
                        return null;
                    }
                },
                () => { });

        }
        private void IntegrateSprinklerCommon(string highlighterName, Func<IDictionary<int,Vector2[]>> getCoverage, bool fallbackToDefault) {
            theMod.api.RemoveItemRangeHighlighter("jltaylor-us.RangeHighlight/sprinkler");
            IDictionary<int, bool[,]> coverageMask = new Dictionary<int, bool[,]>();
            theMod.api.AddItemRangeHighlighter(highlighterName,
                () => theMod.config.ShowSprinklerRange,
                () => theMod.config.ShowSprinklerRangeKey,
                () => theMod.config.ShowOtherSprinklersWhenHoldingSprinkler,
                () => {
                    foreach(var entry in getCoverage()) {
                        coverageMask[entry.Key] = PointsToMask(entry.Value);
                    }
                },
                (item, itemID, itemName) => {
                    if (coverageMask.TryGetValue(itemID, out bool[,]? tiles)) {
                        return new List<Tuple<Color, bool[,]>>(1) { new (theMod.config.SprinklerRangeTint, tiles) };
                    } else if (fallbackToDefault) {
                        var x = theMod.GetDefaultSprinklerHighlight(item, itemID, itemName);
                        if (x is null) return null;
                        return new List<Tuple<Color, bool[,]>>(1) { x };
                    } else {
                        return null;
                    }
                },
                () => {
                    coverageMask.Clear();
                });
        }
        private void IntegrateBetterSprinklers() {
            IBetterSprinklersApi? api = theMod.helper.ModRegistry.GetApi<IBetterSprinklersApi>("Speeder.BetterSprinklers");
            if (api == null) return;
            theMod.api.RemoveItemRangeHighlighter("jltaylor-us.RangeHighlight/sprinkler");
            IntegrateSprinklerCommon("jltaylor-us.RangeHighlight/better-sprinkler", api.GetSprinklerCoverage, false);
        }
        private void IntegrateSimpleSprinklers() {
            ISimplerSprinklerApi? api = theMod.helper.ModRegistry.GetApi<ISimplerSprinklerApi>("tZed.SimpleSprinkler");
            if (api == null) return;
            theMod.api.RemoveItemRangeHighlighter("jltaylor-us.RangeHighlight/sprinkler");
            IntegrateSprinklerCommon("jltaylor-us.RangeHighlight/simple-sprinkler", api.GetNewSprinklerCoverage, false);
        }
        private void IntegrateLineSprinklers() {
            ILineSprinklersApi? api = theMod.helper.ModRegistry.GetApi<ILineSprinklersApi>("hootless.LineSprinklers");
            if (api == null) return;
            theMod.api.RemoveItemRangeHighlighter("jltaylor-us.RangeHighlight/sprinkler");
            IntegrateSprinklerCommon("jltaylor-us.RangeHighlight/line-sprinkler", api.GetSprinklerCoverage, true);
        }
        private bool[,] PointsToMask(Vector2[] points) {
            int maxX = 0;
            int maxY = 0;
            foreach (var point in points) {
                maxX = Math.Max(maxX, Math.Abs((int)point.X));
                maxY = Math.Max(maxY, Math.Abs((int)point.Y));
            }
            bool[,] result = new bool[maxX * 2 + 1, maxY * 2 + 1];
            foreach (var point in points) {
                result[(int)point.X + maxX, (int)point.Y + maxY] = true;
            }
            return result;
        }
    }

    public interface IPrismaticToolsAPI {
        int SprinklerRange { get; }
        int SprinklerIndex { get; }
    }

    public interface IBetterJunimosAPI {
        int GetJunimoHutMaxRadius();
    }

    public interface IBetterSprinklersApi {
        int GetMaxGridSize();
        IDictionary<int, Vector2[]> GetSprinklerCoverage();
    }
    public interface ISimplerSprinklerApi {
        IDictionary<int, Vector2[]> GetNewSprinklerCoverage();
    }
    public interface ILineSprinklersApi {
        int GetMaxGridSize();
        IDictionary<int, Vector2[]> GetSprinklerCoverage();
    }
    public interface RadioactiveToolsAPI {
        int SprinklerRange { get; }
        int SprinklerIndex { get; }
    }
    public interface IBetterBeehousesAPI {
        public bool GetEnabledHere(GameLocation location, bool isWinter);
        public int GetSearchRadius();
    }
}
