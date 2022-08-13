﻿using BepInEx;
using Eremite;
using Eremite.Buildings;
using Eremite.Controller;
using Eremite.Model;
using Eremite.Model.Effects;
using Eremite.Model.Meta;
using Eremite.Model.Orders;
using Eremite.Services;
using Eremite.WorldMap;
using HarmonyLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BubbleStormTweaks
{
    public class RewardData
    {
        [JsonProperty]
        public string name;

        [JsonProperty]
        public string description;

        [JsonProperty]
        public string img;

        public RewardData(MetaRewardModel reward)
        {
            name = reward.Name;
            description = reward.Description;
            img = reward.Icon.Small();
        }
    }

    public class UpgradeData
    {
        [JsonProperty]
        public string name;
        [JsonProperty]
        public string description;

        [JsonProperty]
        public string []cost;
        
        [JsonProperty]
        public RewardData []rewards;
    }

    public class LevelRewards
    {
        [JsonProperty]
        public string name = "";

        [JsonProperty]
        public string description = "";

        [JsonProperty]
        public RewardData[] rewards;

        [JsonProperty]
        public string[] costs = Array.Empty<string>();
    }

    public class AssetLoader
    {
        public static Sprite LoadInternal(string file, Vector2Int size)
        {
            return Image2Sprite.Create(Path.Combine(Plugin.Dir, file + ".png"), size);
        }
        // Loosely based on https://forum.unity.com/threads/generating-sprites-dynamically-from-png-or-jpeg-files-in-c.343735/
        public static class Image2Sprite
        {
            public static string icons_folder = "";
            public static Sprite Create(string filePath, Vector2Int size)
            {
                var bytes = File.ReadAllBytes(icons_folder + filePath);
                var texture = new Texture2D(size.x, size.y, TextureFormat.DXT5, false);
                _ = texture.LoadImage(bytes);
                return Sprite.Create(texture, new Rect(0, 0, size.x, size.y), new Vector2(0, 0));
            }
        }
    }
    public static class Ext
    {
        private static Regex goodWithCategory = new(@"\[(.*?)\] (.*)");
        public static string Div(this string contents) => contents.Surround("div");

        public static string Cost(this GoodRef good, string costType)
        {
            return @$"<span class=""cost-{costType}"" data-base-cost=""{good.amount}"">{good.amount}</span>× {good.Icon.Small()} <a href=""{good.good.Link()}""> {good.good.displayName.Text}</a>";
        }

        public static string AsSummary(this string str) => str.Surround("summary");

        public static string Surround(this string str, string with)
        {
            return $"<{with}>{str}</{with}>";
        }
        public static string GatherIn(this IEnumerable<string> contents, string with)
        {
            return $"<{with}>{string.Join("\n", contents)}</{with}>";
        }

        public static string GatherIn(this IEnumerable<string> contents, string with, string attribs)
        {
            return $"<{with} {attribs}>{string.Join("\n", contents)}</{with}>";
        }

        public static string Surround(this string str, string with, string attribs)
        {
            return $"<{with} {attribs}>{str}</{with}>";
        }
        public static string DescriptionOrLink(this EffectModel effect)
        {
            if (effect is GoodsEffectModel goods)
            {
                return $@"{goods.good.amount}× {effect.GetIcon().Small()} <a href=""{goods.good.good.Link()}"">{goods.good.good.Name.StripCategory()}</a>";
            }
            else
            {
                return effect.Description;
            }

        }

        public static string DataAttrib(this object obj, string suffix)
        {
            return $@"data-{suffix}='{JSON.ToJson(obj).Replace("'", "`")}'";
        }
        public static string Minutes(this float seconds)
        {
            int mins = (int)(seconds / 60);
            int secs = (int)(seconds % 60);

            return mins.ToString().PadLeft(2, '0') + ':' + secs.ToString().PadLeft(2, '0');
        }
        public static string StripCategory(this string s)
        {
            var match = goodWithCategory.Match(s);
            if (match.Success)
                return goodWithCategory.Match(s).Groups[2].Value;
            else
                return s;
        }
        public static string Link(this string s, string directory)
        {
            return $"../{directory}/{s.Sane()}.html";
        }
        public static string Sane(this string s)
        {
            return s.Replace(' ', '_');
        }

        public static string Link(this SO so)
        {
            if (so is BuildingModel)
                return so.Name.Link("buildings");
            else if (so is GoodModel)
                return so.Name.Link("goods");
            else if (so is RecipeModel recipe)
                return recipe.GetProducedGood().Link("goods");
            else
                throw new NotSupportedException();
        }
        public static string ImageScaled(this Sprite icon, int target, string attribs = "")
        {
            Plugin.Write(icon.texture);
            float scale = (icon.textureRect.width / target);
            int w = target;
            int h = target;
            return $@"<img {attribs} src=""../img/1x1.png"" width={w} height={h} style=""background: url(../img/{icon.texture.name.Replace(" ", "%20").Replace("'", "\\'")}.png); background-position: -{icon.textureRect.x / scale}px -{(icon.texture.height - icon.textureRect.y - icon.textureRect.height) / scale}px; background-size: {icon.texture.width / scale}px {icon.texture.height / scale}px;""/>";
        }
        public static string Normal(this Sprite sprite)
        {
            return sprite.ImageScaled(128);
        }
        public static string Small(this Sprite sprite)
        {
            return sprite.ImageScaled(32);
        }
    }

    public class SimpleString
    {
        public string Key;
        public string Value;

        public SimpleString(string key, string value)
        {
            Key = key;
            Value = value;
        }
    }

    public static class Strings
    {
        public static SimpleString catDisplayName = new("bubble.cats.displayName", "Catfolk");
        public static SimpleString catPluralName = new("bubble.cats.pluralName", "Catfolk");
        public static SimpleString catDescription = new("bubble.cats.description", "Catfolk are a race of natural explorers who rarely tire of trailblazing, but such trailblazing is not limited merely to the search for new horizons in distant lands. Many catfolk see personal growth and development as equally valid avenues of exploration.");
        public static SimpleString catResilienceLabel = new("bubble.cats.resilience", "low");
        public static SimpleString catDemandingLabel = new("bubble.cats.demanding", "low");
        public static SimpleString catDecadentLabel = new("bubble.cats.decadent", "low");

        public static LocaText Loc(this SimpleString s) { return new() { key = s.Key }; }
    }

    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static string Dir => Path.Combine(Directory.GetCurrentDirectory(), "BepInEx/plugins/bubblestorm");
        public Harmony harmony;
        public static Plugin Instance;
        public static void LogInfo(object data) => Instance.Logger.LogInfo(data);

        public static string Div(string contents) => contents.Surround("div");
        public static void LogError(object data) => Instance.Logger.LogError(data);

        private void Awake()
        {
            Instance = this;
            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded");

            harmony = new Harmony("bubblestorm");

            harmony.PatchAll(typeof(Plugin));
        }

        public static Settings GameSettings => MainController.Instance.Settings;

        [HarmonyPatch(typeof(MainController), nameof(MainController.InitSettings))]
        [HarmonyPostfix]
        static void InitSettings()
        {
            //var lizard = GameSettings.GetRace("Lizard");

            //var cat = SO.CreateInstance<RaceModel>();
            //cat.name = "Cat";
            //cat.icon = AssetLoader.LoadInternal("cat_temp", new(255, 255));
            //cat.roundIcon = AssetLoader.LoadInternal("cat_circle", new(255, 255));
            //cat.widePortrait = AssetLoader.LoadInternal("cat_temp", new(255, 255));
            //cat.isActive = true;
            //cat.isEssential = true;
            //cat.displayName = Strings.catDisplayName.Loc();
            //cat.pluralName = Strings.catPluralName.Loc();
            //cat.description = Strings.catDescription.Loc();
            //cat.order = 2;
            //cat.assignAction = new();
            //cat.tag = SO.CreateInstance<ModelTag>();
            //cat.tag.name = "cat.tag";
            //cat.malePrefab = lizard.malePrefab;
            //cat.femalePrefab = lizard.femalePrefab;
            //cat.avatarClickSound = lizard.avatarClickSound;
            //cat.ambient = lizard.ambient;
            //cat.maleNames = new string[]
            //{
            //    "Oliver", "Leo", "Milo", "Charlie", "Simba", "Max", "Jack", "Loki", "Tiger", "Jasper", "Ollie", "Oscar", "George", "Buddy", "Toby", "Smokey", "Finn", "Felix", "Simon", "Shadow",
            //};
            //cat.femaleNames = new string[]
            //{
            //    "Luna", "Bella", "Lily", "Lucy", "Nala", "Kitty", "Chloe", "Stella", "Zoe", "Lola"
            //};
            //cat.initialResolve = 5;
            //cat.minResolve = 0;
            //cat.maxResolve = 50;
            //cat.resolvePositveChangePerSec = 0.15f;
            //cat.resolveNegativeChangePerSec = 0.05f;
            //cat.resolveNegativeChangeDiffFactor = 0.02f;
            //cat.reputationPerSec = 0.0003f;
            //cat.minPopulationToGainReputation = 1;
            //cat.resolveForReputationTreshold = new(15, 50);
            //cat.maxReputationFromResolvePerSec = 0.025f;
            //cat.reputationTresholdIncreasePerReputation = 7;
            //cat.resolveToReputationRatio = 0.1f;
            //cat.populationToReputationRatio = 0.7f;

            //cat.resilienceLabel = Strings.catResilienceLabel.Loc();
            //cat.demandingLabel = Strings.catDemandingLabel.Loc();
            //cat.decadentLabel = Strings.catDecadentLabel.Loc();
            //var anyHousing = GameSettings.Needs.First(n => n.name == "Any Housing");
            //cat.needs = new NeedModel[]
            //{
            //    anyHousing,
            //};

            //cat.needsInterval = 100;
            //cat.hungerEffect = lizard.hungerEffect;
            //cat.homelessPenalty = null;
            //cat.initialEffects = Array.Empty<ResolveEffectModel>();
            //cat.characteristics = new RaceCharacteristicModel[]
            //{

            //};

            //cat.needsCategoriesLookup = new();

            //GameSettings.Races = GameSettings.Races.AddToArray(cat);

        }

        [HarmonyPatch(typeof(AppServices), nameof(AppServices.CreateServices))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        static void InitServices(AppServices __instance)
        {
            __instance.TextsService.OnTextsChanged.Subscribe(new Action(() =>
            {
                var s = MB.TextsService as TextsService;
                s.texts["MenuUI_KeyBindings_Action_select_race_1"] = "Pick Next Race";

                foreach (var f in typeof(Strings).GetFields(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public))
                {
                    SimpleString str = (SimpleString)f.GetValue(null);
                    s.texts[str.Key] = str.Value;
                }

            }));
        }

        public static InputAction dumpACtion;

        [HarmonyPatch(typeof(InputConfig), MethodType.Constructor)]
        [HarmonyPostfix]
        static void InputConfig_ctor()
        {
            dumpACtion = new("select_race_1", InputActionType.Button, expectedControlType: "Button");
            dumpACtion.AddBinding("<Keyboard>/tab", groups: "Keyboard");
            dumpACtion.performed += DoDump;
            dumpACtion.Enable();
        }

        public class RecipeRaw
        {
            public RecipeRaw() { }
            public RecipeRaw(BuildingModel source, NeedModel need, int outputCount, float prodTime, string tier)
            {
                this.tier = tier;
                count = outputCount;
                outputName = need.Name;
                building = source.Name;
                this.need = need;
                buildingModel = source;
                processingTime = prodTime;
            }

            public RecipeRaw(BuildingModel source, GoodRef good, float prodTime, string tier)
            {
                this.tier = tier;
                output = good;
                outputName = good.Name.StripCategory();
                building = source.Name;
                buildingModel = source;
                processingTime = prodTime;
            }
            public string tier;
            public GoodsSet[] ingredients = Array.Empty<GoodsSet>();
            public GoodRef output;
            public int count;
            public float processingTime;
            public string building;
            public NeedModel need;
            public (string, float) timeA = (null, 0);
            public (string, float) timeB = (null, 0);

            public string Id => building + "::" + outputName;

            public BuildingModel buildingModel;
            public string outputName;

            public bool CustomTime => processingTime == 0;
        }

        public class BuildingRaw
        {
            public List<RecipeRaw> recipes = new();
            public BuildingModel model;
            public string Name => model.Name;
        }

        private static readonly Dictionary<string, BuildingModel> buildingsByName = new();
        private static readonly Dictionary<string, RecipeRaw> recipeById = new();
        private static readonly Dictionary<string, GoodInfo> goodInfo = new();

        private static IEnumerable<IGrouping<GoodModel, RecipeRaw>> FindConsumersOf(string good)
        {
            return goodInfo[good].ingredientIn.Select(s => recipeById[s]).GroupBy(r => r.output?.good);
        }

        private static IEnumerable<RecipeRaw> FindProducersOf(string good)
        {
            return goodInfo[good].producedBy.OrderByDescending(x => x.tier);
        }

        private static GoodInfo InfoFor(string good)
        {
            if (goodInfo.TryGetValue(good, out var info))
                return info;

            info = new GoodInfo();
            goodInfo[good] = info;
            return info;
        }
        private static RecipeRaw Add(BuildingModel source, NeedModel need, int outputCount, float prodTime, string tier)
        {
            RecipeRaw raw = new(source, need, outputCount, prodTime, tier);
            recipeById[raw.Id] = raw;
            InfoFor(need.Name).producedBy.Add(raw);
            return raw;
        }

        private static RecipeRaw Add(RecipeModel recipe, BuildingModel source, GoodRef good, float prodTime, string tier)
        {
            RecipeRaw raw = new(source, good, prodTime, tier);
            recipeById[raw.Id] = raw;
            InfoFor(recipe.GetProducedGood()).producedBy.Add(raw);
            return raw;
        }

        public class EffectSource
        {
            public EffectModel model;
            public HashSet<string> biomes = new();
        }
        private static readonly Dictionary<string, EffectSource> effectSources = new();
        public static EffectSource BiomeEffect(EffectModel effect, string biome) {
            if (!effectSources.TryGetValue(effect.Name, out EffectSource source))
            {
                source = new() { model = effect };
                effectSources[effect.Name] = source;
            }

            source.biomes.Add(biome);

            return source;
        }

        private static void DumpCornerstones(StringBuilder index)
        {
            index.AppendLine($@"<html>{HTML_HEAD}<body>
            <header> {NAV} </header><main>
            <div class=""cornerstone-container"">");

            HashSet<string> seen = new();

            foreach (var biome in GameSettings.biomes)
            {
                foreach (var effect in biome.seasons.SeasonRewards.SelectMany(season => season.effectsTable.effects).Select(tableEntry => tableEntry.effect))
                {
                    BiomeEffect(effect, biome.Name);
                }
            }

            foreach (var source in effectSources.Values)
            {
                var effect = source.model;
                index.AppendLine($@"<div class=""cornerstone"">");
                index.AppendLine($@"<a class=""section-anchor"" href=""#{effect.Name.Sane()}"" id=""{effect.Name.Sane()}""><div>{effect.GetIcon().Small()}<h3>{effect.Name.StripCategory()}</h3></div></a>");
                index.AppendLine($@"<p>{effect.Description}</p>");
                index.AppendLine($@"<h4>Seasonal reward in:</h4><ul>");
                foreach (var biome in source.biomes)
                    index.AppendLine($@"<li>{biome}</li>");
                index.AppendLine($@"</ul>");
                index.AppendLine($@"</div>");
            }
            //{
            //    index.AppendLine(@"<div class=""relic"">");
            //    index.AppendLine($@"<h3>{relic.Name}</h3>");
            //    index.AppendLine($@"<h5>{relic.Description}</h5>");

            //    foreach (var tier in relic.effectsTiers.Where(tier => tier.effect.Length > 0))
            //    {
            //        index.AppendLine($@"<h4>Active after {tier.timeToStart} seconds:</h4>");
            //        index.AppendLine($@"<ul>");
            //        foreach (var effect in tier.effect)
            //        {
            //            index.AppendLine($@"<li>{effect.Description}</li>");
            //        }
            //        index.AppendLine($@"</ul>");
            //    }

            //    foreach (var diff in relic.difficulties)
            //    {
            //        index.AppendLine($@"<h4>Difficulty {diff.difficulty}: {diff.workingTimeRatio * relic.workTime} seconds</h4>");
            //        index.AppendLine($@"<ul>");
            //        foreach (var set in diff.requriedGoods.sets)
            //        {
            //            index.AppendLine($@"<li>{string.Join(" | ", set.goods.Select(good => good.amount + $@"× {good.good.icon.Small()}<a href=""{good.good.Link()}"">" + good.Name.StripCategory() + "</a>"))}</li>");
            //        }
            //        index.AppendLine($@"</ul>");
            //    }

            //    foreach (var reward in relic.rewardsTiers)
            //    {
            //        string amount = "";
            //        if (reward.rewardsTable != null && reward.rewardsTable.effects.Length > 0)
            //        {
            //            var amounts = reward.rewardsTable.amounts;
            //            amount = " " + ((amounts.x == amounts.y) ? amounts.x.ToString() : $"{amounts.x} - {amounts.y}");
            //        }

            //        index.AppendLine($@"<h3>After {reward.timeToStart} seconds get{amount}:</h3>");
            //        index.AppendLine($@"<ul>");
            //        if (reward.rewards != null && reward.rewards.Length > 0)
            //            foreach (var thing in reward.rewards)
            //                index.AppendLine($@"<li>{thing.DescriptionOrLink()}</li>");
            //        if (reward.rewardsTable != null && reward.rewardsTable.effects.Length > 0)
            //        {
            //            foreach (var thing in reward.rewardsTable.effects)
            //                index.AppendLine($@"<li>{thing.effect.DescriptionOrLink()} chance: {thing.chance}</li>");
            //        }
            //        index.AppendLine($@"</ul>");
            //    }

            //    index.AppendLine($@"<h3>Choose one of these after resolving:</h3>");
            //    foreach (var rewardSet in relic.rewardsSets)
            //    {
            //        var amounts = rewardSet.effects.amounts;
            //        string amount = (amounts.x == amounts.y) ? amounts.x.ToString() : $"{amounts.x} - {amounts.y}";
            //        index.AppendLine($@"<h4>{rewardSet.label},    {amount} of:</h4>");
            //        index.AppendLine($@"<ul>");
            //        foreach (var thing in rewardSet.effects.effects)
            //            index.AppendLine($@"<li>{thing.effect.DescriptionOrLink()} {thing.chance}%</li>");
            //        index.AppendLine($@"</ul>");

            //    }

            //    index.AppendLine(@"</div>");
            //}
            index.AppendLine(@"</div></main></body>");
            index.AppendLine(@"
            <style>
            img { vertical-align: middle; }
            h3 { display:inline-block; padding-left:4px; }
            </style>
            <script>
            </script>");

            index.AppendLine("</html>");
            Write(index, "cornerstones", "index");

        }


        private static void DumpRelics(StringBuilder index)
        {
            index.AppendLine($@"<html>{HTML_HEAD}<body>
            <header> {NAV} </header><main>
            <div>");

            Dictionary<int, string> difficultyByIndex = GameSettings.difficulties.Where(d => d.index >= 0).ToDictionary(d => d.index, d => "difficulty-" + d.Name.Sane());

            foreach (var danger in GameSettings.Relics.GroupBy(r => r.dangerLevel).OrderByDescending(g => g.Key))
            {
                index.AppendLine($@"<h3>{danger.Key}</h3>");

                index.AppendLine($@"<table><tr><th>Name</th><th>Time Needed</th><th>Materials Needed (1 per column)</th><th>Bad Stuff</th></tr>");


                foreach (var relic in danger)
                {
                    List<string>[] diffclasses = new List<string>[relic.difficulties.Length];
                    for (int i = 0; i < relic.difficulties.Length; i++)
                    {
                        RelicDifficulty diff = relic.difficulties[i];

                        List<string> diffclass = new() { "filter-difficulty", difficultyByIndex[diff.difficulty] };

                        if (i == relic.difficulties.Length - 1)
                        {
                            int diffIndex = diff.difficulty + 1;
                            while (difficultyByIndex.TryGetValue(diffIndex, out var next))
                            {
                                diffclass.Add(next);
                                diffIndex += 1;
                            }
                        }

                        if (i == 0)
                        {
                            int diffIndex = diff.difficulty - 1;
                            while (difficultyByIndex.TryGetValue(diffIndex, out var next))
                            {
                                diffclass.Add(next);
                                diffIndex -= 1;
                            }
                        }

                        diffclasses[i] = diffclass;
                    }


                    index.AppendLine($@"<tr><td>{relic.displayName.Text}</td>");

                    index.AppendLine("<td>");
                    for (int i = 0; i < relic.difficulties.Length; i++)
                    {
                        RelicDifficulty diff = relic.difficulties[i];
                        List<string> diffclass = diffclasses[i];
                        index.AppendLine($@"<div class=""{string.Join(" ", diffclass)}"">");
                        float baseSeconds = diff.workingTimeRatio * relic.workTime; 
                        index.AppendLine(baseSeconds.Minutes().Surround("span", $@"class=""relic-time"" data-base-time=""{baseSeconds}"""));
                        index.AppendLine($@"</div>");
                    }
                    index.AppendLine("</td>");

                    index.AppendLine("<td>");
                    for (int i = 0; i < relic.difficulties.Length; i++)
                    {
                        RelicDifficulty diff = relic.difficulties[i];
                        List<string> diffclass = diffclasses[i];
                        index.AppendLine($@"<div class=""{string.Join(" ", diffclass)}"">");
                        index.AppendLine($@"<div class=""to-solve-sets"">");
                        foreach (var set in diff.requriedGoods.sets)
                        {
                            index.AppendLine($@"<div class=""to-solve-set"">");
                            foreach (var g in set.goods)
                                index.AppendLine(g.Cost("construction").Surround("Div"));
                            index.AppendLine($@"</div>");
                        }
                        index.AppendLine($@"</div>");
                        index.AppendLine($@"</div>");
                    }
                    index.AppendLine("</td>");

                    index.AppendLine("<td><div>");
                    foreach (var tier in relic.effectsTiers.Where(tier => tier.effect.Length > 0))
                    {
                        index.AppendLine($@"<div>Every {tier.timeToStart.Minutes()}:</div>");
                        foreach (var effect in tier.effect)
                        {
                            index.AppendLine($@"<div>{effect.Description}</div>");
                        }
                    }
                    index.AppendLine("</div></td></tr>");

                }
                index.AppendLine("</table>");

            }


            //foreach (var relic in GameSettings.Relics.OrderBy(r => r.dangerLevel))
            //{
            //    index.AppendLine($@"<div class=""relic"" class=""relic-danger-{relic.dangerLevel}"">");
            //    index.AppendLine($@"<a class=""section-anchor"" href=""#{relic.Name.Sane()}"" id=""{relic.Name.Sane()}""><h3>{relic.Name}</h3></a>");
            //    index.AppendLine($@"<h5>Glade danger level: {relic.dangerLevel}</h5>");
            //    index.AppendLine($@"<h5>{relic.Description}</h5>");

            //    foreach (var tier in relic.effectsTiers.Where(tier => tier.effect.Length > 0))
            //    {
            //        index.AppendLine($@"<h4>Active after {tier.timeToStart.Minutes()}:</h4>");
            //        index.AppendLine($@"<ul>");
            //        foreach (var effect in tier.effect)
            //        {
            //            index.AppendLine($@"<li>{effect.Description}</li>");
            //        }
            //        index.AppendLine($@"</ul>");
            //    }

            //    for (int i = 0; i < relic.difficulties.Length; i++)
            //    {
            //        RelicDifficulty diff = relic.difficulties[i];
            //        index.AppendLine($@"<h4>{GameSettings.difficulties.First(d => d.index == diff.difficulty).GetDisplayName()}{(i == relic.difficulties.Length - 1 ? "+" : "")}: ⏱ {(diff.workingTimeRatio * relic.workTime).Minutes()}️</h4>");
            //        index.AppendLine($@"<ul>");
            //        foreach (var set in diff.requriedGoods.sets)
            //        {
            //            index.AppendLine($@"<li>{string.Join(" | ", set.goods.Select(good => good.Cost("construction")))}</li>");
            //        }
            //        index.AppendLine($@"</ul>");
            //    }

            //    foreach (var reward in relic.rewardsTiers)
            //    {
            //        string amount = "";
            //        if (reward.rewardsTable != null && reward.rewardsTable.effects.Length > 0)
            //        {
            //            var amounts = reward.rewardsTable.amounts;
            //            amount = " " + ((amounts.x == amounts.y) ? amounts.x.ToString() : $"{amounts.x} - {amounts.y}");
            //        }

            //        index.AppendLine($@"<h3>After {reward.timeToStart} seconds get{amount}:</h3>");
            //        index.AppendLine($@"<ul>");
            //        if (reward.rewards != null && reward.rewards.Length > 0)
            //            foreach (var thing in reward.rewards)
            //                index.AppendLine($@"<li>{thing.DescriptionOrLink()}</li>");
            //        if (reward.rewardsTable != null && reward.rewardsTable.effects.Length > 0)
            //        {
            //            foreach (var thing in reward.rewardsTable.effects)
            //                index.AppendLine($@"<li>{thing.effect.DescriptionOrLink()} chance: {thing.chance}</li>");
            //        }
            //        index.AppendLine($@"</ul>");
            //    }

            //    index.AppendLine($@"<h3>Choose one of these after resolving:</h3>");
            //    foreach (var rewardSet in relic.rewardsSets)
            //    {
            //        var amounts = rewardSet.effects.amounts;
            //        string amount = (amounts.x == amounts.y) ? amounts.x.ToString() : $"{amounts.x} - {amounts.y}";
            //        index.AppendLine($@"<h4>{rewardSet.label},    {amount} of:</h4>");
            //        index.AppendLine($@"<ul>");
            //        foreach (var thing in rewardSet.effects.effects)
            //            index.AppendLine($@"<li>{thing.effect.DescriptionOrLink()} {thing.chance}%</li>");
            //        index.AppendLine($@"</ul>");

            //    }

            //    index.AppendLine(@"</div>");
            //}
            index.AppendLine(@"</div></main></body>");
            index.AppendLine(@"
            <style>
.relic {
    padding: 4px;
    border: 1px solid black;
}

ul { margin: 4px; padding-left: 12px }
img { vertical-align: middle;}
a {padding-left: 4px;}

            </style>
            <script>
            </script>
");

            index.AppendLine("</html>");
            Write(index, "relics", "index");

        }

        public class OrdersGiven
        {
            public int easy = 0;
            public int medium = 0;
            public int hard = 0;

            public override bool Equals(object obj)
            {
                return obj is OrdersGiven given &&
                       easy == given.easy &&
                       medium == given.medium &&
                       hard == given.hard;
            }

            public override int GetHashCode()
            {
                int hashCode = -412224243;
                hashCode = hashCode * -1521134295 + easy.GetHashCode();
                hashCode = hashCode * -1521134295 + medium.GetHashCode();
                hashCode = hashCode * -1521134295 + hard.GetHashCode();
                return hashCode;
            }
        }

        public static void DumpBiome(BiomeModel biome)
        {
            StringBuilder txt = new();

            txt.AppendLine($@"<html>{HTML_HEAD}<body>
                                    <header>{NAV}");


            txt.AppendLine(@"</header> <main><div>");

            txt.AppendLine($@"{biome.icon.Normal()} <h2>{biome.displayName.Text}</h2>");
            txt.AppendLine($@"<p>{biome.description}</p>");

            txt.AppendLine($@"<details>");
            txt.AppendLine("Newcomers".AsSummary());
            txt.AppendLine($@"<p>{biome.newcomers.minNewcomers} - {biome.newcomers.maxNewcomers} (+ any bonus) will arrive every {biome.newcomers.newComersInterval.Minutes()}, possible races:</p>");

            txt.AppendLine($@"<div class=""weighted-table"">");
            {
                float total = biome.newcomers.races.Sum(w => w.weight);
                foreach (var nc in biome.newcomers.races)
                    AddGridRow(txt, $"{nc.race.roundIcon.Small()} {nc.race.Name}", "", $"{(int)(100 * (nc.weight / (float)total))}%");
            }
            txt.AppendLine($@"</div>");

            txt.AppendLine($@"</details>");

            txt.AppendLine($@"<details>");
            txt.AppendLine("Traders".AsSummary());

            txt.AppendLine($@"<div class=""weighted-table"">");
            foreach (var nc in biome.trade.ranges)
            {
                txt.AppendLine($@"<div style=""grid-column: 1 / 4;""><b>When city score >= {nc.minCityScore}</b></div>");
                float total = nc.weights.Sum(w => w.weight);
                foreach (var trader in nc.weights)
                    AddGridRow(txt, $"{trader.trader.icon.Small()} {trader.trader.Name}", "", $"{(int)(100 * (trader.weight / (float)total))}%");
                txt.AppendLine($@"<div style=""grid-column: 1 / 4; height: 32px;""></div>");
            }
            txt.AppendLine($@"</div>");

            txt.AppendLine($@"</details>");


            txt.AppendLine($@"<details>");
            txt.AppendLine("Orders".AsSummary());
            txt.AppendLine(@"<div>");
            txt.AppendLine($@"<table>");

            Dictionary<OrdersGiven, List<string>> ordersByDifficulty = new();
            foreach (var diff in biome.difficulty.difficultiesData)
            {
                OrdersGiven given = new();
                foreach (var slot in diff.orders.ordersSlots)
                {
                    if (slot.difficulties == OrderDifficulty.Easy)
                        given.easy++;
                    if (slot.difficulties == OrderDifficulty.Medium)
                        given.medium++;
                    if (slot.difficulties == OrderDifficulty.Hard)
                        given.hard++;
                }

                if (ordersByDifficulty.TryGetValue(given, out var list))
                    list.Add(diff.difficulty.GetDisplayName() );
                else
                {
                    list = new() { diff.difficulty.GetDisplayName() };
                    ordersByDifficulty[given] = list;
                }
            }




            txt.AppendLine($@"<tr><th>Difficulty</th><th>Easy Orders</th><th>Medium Orders</th><th>Hard Orders</th></tr>");
            foreach (var kv in ordersByDifficulty)
            {
                if (kv.Value.Count == 1)
                    txt.AppendLine($@"<tr><td><div>{kv.Value[0]}</div></td><td>{kv.Key.easy}</td><td>{kv.Key.medium}</td><td>{kv.Key.hard}</td></tr>");
                else
                    txt.AppendLine($@"<tr><td><div>{kv.Value.First()}</div><div>to</div><div>{kv.Value.Last()}</div></td><td>{kv.Key.easy}</td><td>{kv.Key.medium}</td><td>{kv.Key.hard}</td></tr>");

            }
            txt.AppendLine($@"</table>");

            txt.AppendLine("<h5>Excluded orders:</h5><ul>");
            txt.AppendLine(string.Join("\n", ExclusionsForBiome(biome.Name).Select(order => order.Surround("li"))));

            txt.AppendLine("</ul>");
            txt.AppendLine("</div>");

            txt.AppendLine($@"</details>");


            txt.AppendLine("</div></main></body></html>");
            Write(txt, "biomes", biome.Name.Sane());

        }

        private static void AddGridRow(StringBuilder txt, string v1, string v2, string v3)
        {
            txt.AppendLine($@"<div style=""grid-column: 1;"">{v1}</div> <div style=""grid-column: 2;"">{v2}</div> <div style=""grid-column: 3;"">{v3}</div>");
        }

        private static Dictionary<string, List<string>> buildingTagToRace = new();

        public static void DoDump(InputAction.CallbackContext context)
        {
            LogInfo(" === DUMP STARTING ===");
            GetOrderExclusions();

            Dictionary<string, string> currentEfffect = new();


            foreach (var race in GameSettings.Races)
            {
                foreach (var ch in race.characteristics)
                {
                    if (buildingTagToRace.TryGetValue(ch.tag.Name, out var list))
                        list.Add(race.Name);
                    else
                        buildingTagToRace[ch.tag.Name] = new() { race.Name };
                }
            }


            StringBuilder index = new();

            index.AppendLine($@"<html>{HTML_HEAD}<body>
                                <header>{NAV}</header><main>
                                <h1>AgainstTheStormpedia</h1>
                                <p> Click the links above to go to fun pages full of even more fun data.  </p>
                                <footer>
                                    <p style=""color: skyblue"">Generated by bubbles, website is CC0, all assets are copyright Eremite</p>
                                </footer>
                            </main></body></html>");

            Write(index, null, "index");
            index.Clear();

            DumpEffects(index);
            index.Clear();

            DumpOrders(index);
            index.Clear();

            DumpRelics(index);
            index.Clear();

            DumpCornerstones(index);
            index.Clear();

            foreach (var source in GameSettings.Buildings)
            {
                if (source is AltarModel altar) { }
                else if (source is BlightPostModel blightPost)
                {
                    foreach (var recipe in blightPost.recipes)
                    {
                        RecipeRaw raw = Add(recipe, source, recipe.producedGood, recipe.productionTime, GetTier(recipe.Name));

                        if (!recipe.RequiredGoodsNotValid())
                        {
                            raw.ingredients = recipe.requiredGoods;
                            foreach (var i in raw.ingredients)
                            {
                                foreach (var a in i.goods)
                                {
                                    InfoFor(a.Name).ingredientIn.Add(raw.Id);
                                }
                            }
                        }
                    }

                }
                else if (source is CampModel camp)
                {
                    foreach (var recipe in camp.recipes)
                        Add(recipe, source, recipe.refGood, recipe.productionTime, GetTier(recipe.Name));
                }
                else if (source is DecorationModel decoration) { }
                else if (source is FarmfieldModel field) { }
                else if (source is HearthModel hearth) { }
                else if (source is HouseModel house) { }
                else if (source is HydrantModel hydrant) { }
                else if (source is InstitutionModel institution)
                {
                    foreach (var recipe in institution.recipes)
                    {
                        var raw = Add(source, recipe.servedNeed, 1, 1, "-");
                        raw.ingredients = new GoodsSet[] { recipe.requiredGoods };
                        foreach (var i in raw.ingredients)
                        {
                            foreach (var a in i.goods)
                            {
                                InfoFor(a.Name).ingredientIn.Add(raw.Id);
                            }
                        }
                    }
                }
                else if (source is RelicModel relic) { }
                else if (source is RoadModel road) { }
                else if (source is StorageModel storage) { }
                else if (source is TradingPostModel trader) { }
                else if (source is CollectorModel collector)
                {
                    foreach (var recipe in collector.recipes)
                        Add(recipe, source, recipe.producedGood, recipe.productionTime, GetTier(recipe.Name));
                }
                else if (source is FarmModel farm)
                {
                    foreach (var recipe in farm.recipes)
                    {
                        RecipeRaw raw = Add(recipe, source, recipe.producedGood, 0, GetTier(recipe.Name));
                        raw.timeA = ("plant", recipe.plantingTime);
                        raw.timeB = ("harvest", recipe.harvestTime);
                    }
                }
                else if (source is GathererHutModel gatherer)
                {
                    foreach (var recipe in gatherer.recipes)
                        Add(recipe, source, recipe.refGood, recipe.productionTime, GetTier(recipe.Name));
                }
                else if (source is MineModel mine)
                {
                    foreach (var recipe in mine.recipes)
                        Add(recipe, source, recipe.refGood, recipe.productionTime, GetTier(recipe.Name));
                }
                else if (source is WorkshopModel workshop)
                {
                    foreach (var recipe in workshop.recipes)
                    {
                        RecipeRaw raw = Add(recipe, source, recipe.producedGood, recipe.productionTime, GetTier(recipe.Name));

                        if (!recipe.RequiredGoodsNotValid())
                        {
                            raw.ingredients = recipe.requiredGoods;
                            foreach (var i in raw.ingredients)
                            {
                                foreach (var a in i.goods)
                                {
                                    InfoFor(a.Name).ingredientIn.Add(raw.Id);
                                }
                            }
                        }
                    }
                }

            }

            try
            {
                index.Clear();
                index.AppendLine($@"<html>{HTML_HEAD}<body>
                                    <header>{NAV}</header><main>
                                    <div>Click on an upgrade or level marker to see its rewards and cost to unlock.</div>
                                    <div class=""capital-upgrade-tree"">");

                int maxLevel = GameSettings.metaConfig.levels.Length;
                for (int i = 0; i < GameSettings.capitalStructures.Length; i++)
                {
                    var structure = GameSettings.capitalStructures[i];
                    int fromRow = 100, toRow = 0;
                    foreach (var upgrade in structure.upgrades)
                    {

                        var data = new UpgradeData()
                        {
                            name = upgrade.Name,
                            cost = upgrade.price.Select(curr => $"{curr.amount}x {curr.Name}").ToArray(),
                            rewards = upgrade.rewards.Select(reward => new RewardData(reward)).ToArray(),
                        };

                        int row = 2 * (1 + (maxLevel - upgrade.requiredLevel));
                        string cell = $"grid-column: {1 + i}; grid-row: {row}";
                        index.AppendLine($@"<div class=""hex-img required-level-{upgrade.requiredLevel}"" style=""z-index: 2; {cell}"">{upgrade.icon.ImageScaled(64)}</div>");
                        index.AppendLine($@"<div class=""hex-frame reward-provider"" style=""z-index: 3; {cell}"" {data.DataAttrib("rewards")}'></div>");

                        if (row < fromRow) fromRow = row;
                        if (row > toRow) toRow = row;

                    }

                    index.AppendLine($@"<div class=""upgrade-track"" style=""z-index: 1; grid-column: {1 + i}; grid-row: {fromRow} / {toRow + 1}""></div>");

                }

                for (int i = 1; i < (maxLevel); i++)
                {

                    LevelRewards levelup = new() {
                        name = $"Level {i + 1}",
                        rewards = GameSettings.metaConfig.levels[i].rewards.Select(reward => new RewardData(reward)).ToArray()
                    };

                    int row = 2 * (1 + (maxLevel - i)) - 1;
                    index.AppendLine($@"<div class=""upgrade-level reward-provider"" {levelup.DataAttrib("rewards")} style=""z-index: 1; grid-column: 1 / 8; grid-row: {row}"">Level {1 + i}</div>");
                }

                index.AppendLine($@"
            <div class=""upgrade-preview"" style=""grid-column: 8; grid-row: 5 / 34"">
                <div id=""upgrade-panel"">
                    <h4 id=""upgrade-name"">Name</h4>
                    <div id=""upgrade-cost"">
                        <span>3 blah</span>
                        <span>2 foo</span>
                        <span>8 bar</span>
                    </div>
                    <div id=""upgrade-rewards""></div>
                </div>
            </div>");

                index.AppendLine("</div></main></body></html>");
                Write(index, "upgrades", "index");
                index.Clear();

            }
            catch (Exception ex)
            {
                Instance.Logger.LogError(ex);
                throw;
            }

            try
            {
                index.Clear();
                index.AppendLine($@"<html>{HTML_HEAD}<body>
                                    <header>{NAV}</header><main>
                                    <div>");
                foreach (var biome in GameSettings.biomes)
                {
                    DumpBiome(biome);
                    index.AppendLine($@"<div>{biome.icon.Small()}<a href=""{biome.Name.Sane()}.html"">{(biome.displayName.HasText ? biome.displayName.Text : biome.Name.StripCategory())}</a></div>");
                }

                index.AppendLine("</div></main></body></html>");
                Write(index, "biomes", "index");
                index.Clear();

            }
            catch (Exception ex)
            {
                Instance.Logger.LogError(ex);
                throw;
            }


            index.AppendLine($@"<html>{HTML_HEAD}<body>
            <header>{NAV}</header><main>
            <div class=""building-categories"">");

            foreach (var cat in MainController.Instance.Settings.Goods.GroupBy(g => g.category.Name))
            {
                index.AppendLine($@"<div class=""building-category"">");
                index.AppendLine($@"<h2>{cat.Key}</h2>");
                foreach (var g in cat)
                {
                    DumpGood(g);
                    index.AppendLine($@"<div>{g.icon.Small()}<a href=""{g.Name.Sane()}.html"">{g.Name.StripCategory()}</a></div>");
                }
                index.AppendLine($@"</div>");
            }
            index.AppendLine(@"</div></main></body>
            <style>
            body {
                background-color: #444751;
                color: lightgray;
            }
            a, header>nav a { color: lightgray; }
            img { vertical-align: middle; }
            a { padding-left:4px; }
            :root { 
                --accent: gray;
            }
            </style></html>");
            Write(index, "goods", "index");

            index.Clear();

            index.AppendLine($@"<html>{HTML_HEAD}<body>
            <header>{NAV}</header><main>
            <table class=""building-categories"">");

            foreach (var cat in GameSettings.Buildings
                .GroupBy(b => Regex.Replace(b.category.Name, @"\d", ""))
                .OrderByDescending(group => group.Sum(bm => recipeById.Values.Where(r => r.buildingModel == bm).Count())))
            {
                index.AppendLine($@"
                        <tr><th class=""category-name sticky-first"" colspan=""5"">{cat.Key}</th></tr>
                        <tr>
                            <th class=""sticky-second"">Name</th>
                            <th class=""sticky-second"">Produces</th>
                            <th class=""sticky-second"">Building cost</th>
                            <th class=""sticky-second"">Movable</th>
                            <th class=""sticky-second"">Worker bonus</th>
                        </tr>");


                foreach (var b in cat)
                {
                    DumpBuilding(b);

                    string move = "👎";
                    if (b.movable)
                    {
                        if (b.HasMovingCost())
                        {
                            move = b.movingCost.Cost("construction");
                        }
                        else
                        {
                            move = "👍 - free";
                        }
                    }
                    string buildCost = b.requiredGoods.Select(g => Div(g.Cost("construction"))).GatherIn("div");

                    static string MakeRecipeLine(RecipeRaw r)
                    {
                        if (r.output != null)
                            return $@"{r.output.good.icon.Small()} {r.tier} <a href=""{r.output.good.Link()}""> {r.output.good.displayName.Text} </a>".Surround("div");
                        else if (r.need != null)
                            return $@"{r.need.GetIcon().Small()} {r.tier} {r.outputName.StripCategory()}".Surround("div");
                        else
                            return "-";
                    }

                    string produces = string.Join("\n", recipeById.Values.Where(r => r.buildingModel == b).Select(MakeRecipeLine)).Surround("div");

                    string bonusRaces = "-";
                    HashSet<(string, string)> racesWithBonus = new();
                    foreach (var tag in b.tags)
                        if (buildingTagToRace.TryGetValue(tag.Name, out var list))
                            foreach (var r in list)
                                racesWithBonus.Add((r, tag.Name));
                    
                    if (racesWithBonus.Count > 0)
                    {
                        bonusRaces = string.Join("\n", racesWithBonus.Select(rr => $"{GameSettings.GetRace(rr.Item1).icon.Small()} {rr.Item1} ({rr.Item2.Replace("Hearth_", "")})".Div()));
                    }

                    index.AppendLine($@"<tr>
                                            <td>{b.icon.Small()}<a href=""{b.Name.Sane()}.html"">{b.Name.StripCategory()}</a></td>
                                            <td>{produces}</td>
                                            <td>{buildCost}</td>
                                            <td>{move}</td>
                                            <td>{bonusRaces}</td>
                                        </tr>");
                }
            }
            index.AppendLine(@"</table></main></body>
            <style>
            body {
                background-color: #444751;
                color: lightgray;
            }
            :root { 
                --accent: gray;
                --text: lightgray;
            }
            a, header>nav a { color: lightgray; }
            img { vertical-align: middle; }
            a { padding-left:4px; }
            </style></html>");
            Write(index, "buildings", "index");

            LogInfo(" === DUMP COMPLETE ===");
        }

        private static Dictionary<string, HashSet<string>> ordersExcludedByBiome = new();
        private static HashSet<string> ExclusionsForBiome(string biome)
        {
            if (!ordersExcludedByBiome.TryGetValue(biome, out var set)) {
                set = new();
                ordersExcludedByBiome[biome] = set;
            }
            return set;
        }
        private static void GetOrderExclusions()
        {
            foreach (var order in GameSettings.orders)
            {
                if (order.excludeOnBiomes?.Length > 0)
                {
                    foreach (var exclude in order.excludeOnBiomes)
                        ExclusionsForBiome(exclude.Name).Add(order.Name);
                }
            }

        }

        private static void DumpEffects(StringBuilder index)
        {
//            index.AppendLine($@"<html>{HTML_HEAD}<body>
//            <header>{NAV}</header><main>
//<div class=""orders-container"">");
//            foreach (var effect in GameSettings.effects)
//            {
//                LogInfo(diff.Name);

//                foreach (var mod in diff.modifiers)
//                {
//                    if (!currentEfffect.ContainsKey(mod.effect.Name))
//                    {
//                        currentEfffect.Add(mod.effect.Name, mod.effect.Description);
//                        LogInfo("    " + mod.effect.DisplayName);
//                        LogInfo("        " + mod.effect.Description);
//                    }
//                }
//                LogInfo("");


//            }
//            index.AppendLine(@"</div></main></body>");
//            index.AppendLine(@"
//<style>
//</style>
//            <script>
//            </script>
//");

//            index.AppendLine("</html>");
//            Write(index, "effects", "index");

        }

        private static void DumpOrders(StringBuilder index)
        {
            index.AppendLine($@"<html>{HTML_HEAD}<body>
            <header>{NAV}</header><main>
<div class=""orders-container"">");
            foreach (var order in GameSettings.orders)
            {
                index.AppendLine(@"<div class=""order"">");
                index.AppendLine($@"<h3>{order.Name}</h3>");
                foreach (OrderLogicsSet orderLogicSet in order.logicsSets)
                {
                    index.AppendLine($@"<div class=""order-logic for-difficulty-{orderLogicSet.difficulty}"">");
                    index.AppendLine($@"<h4>Requirements:</h4>");
                    foreach (var logic in orderLogicSet.logics)
                    {
                        if (logic.Timed)
                            index.AppendLine($@"<div><h5>{logic.Description}</div>");
                        else
                            index.AppendLine($@"<div><h5>{logic.DisplayName} {logic.GetAmountText()}</div>");
                    }
                    if (orderLogicSet.rewards != null) //overrideRewards???
                    {
                        index.AppendLine($@"<h4>Rewards:</h4>");
                        foreach (var reward in orderLogicSet.rewards)
                            index.AppendLine($@"<div><h5>{reward?.Name ?? "<???>"}</h5></div>");
                    }
                    else
                    {
                        index.AppendLine($@"<h4>Rewards:</h4>");
                        foreach (var reward in order.rewards)
                            index.AppendLine($@"<div><h5>{reward.DisplayName} {reward.GetAmountText()}</h5></div>");

                    }
                    index.AppendLine($@"<div><h5>{order.reputationReward.DisplayName} {order.reputationReward.GetAmountText()}</h5></div>");
                    index.AppendLine($@"</div>");
                }

                if (order.excludeOnBiomes?.Length > 0)
                {
                    index.AppendLine($@"<h5>Excluded on:</h5><ul>");
                    foreach (var exclude in order.excludeOnBiomes)
                        index.AppendLine($@"<li>{exclude.Name}</li>");
                    index.AppendLine($@"</ul>");
                }

                index.AppendLine(@"</div>");
            }
            index.AppendLine(@"</div></main></body>");
            index.AppendLine(@"
<style>
    .orders-content {
        justify-content: space-between;
    }

    .rad-selected {
        border-style: inset;
        ;
        border: 3px solid red;
    }

    /* CSS */
    .button-28 {
        float: left;
        appearance: none;
        border: 1px solid #1A1A1A;
        box-sizing: border-box;
        /* color: #3B3B3B; */
        background-color: lightgray;
        cursor: pointer;
        display: inline;
        font-size: 16px;
        font-weight: 600;
        will-change: transform;
    }

    .button-selected {
        background-color: darkgreen;
        color: white;
    }

    .button-28:disabled {
        pointer-events: none;
    }

    .button-28:hover {
        color: #fff;
        background-color: #1A1A1A;
        box-shadow: rgba(0, 0, 0, 0.25) 0 8px 15px;
    }

    .button-28:active {
        box-shadow: none;
        transform: translateY(0);
    }

    .button-selected:hover {
        background-color: #003000;
    }

    .filter-parent {
        position: relative;
        width: 100%;
    }

    .filter {
        top: 0;
        left: 0;
        position: fixed;
        background-color: #444444;
        color: lightgray;
        padding: 4px;
        padding-left: 100px;
        width: 100%;
    }

    .orders-container {
        padding-top: 50px;
        display: flex;
        flex-wrap: wrap;
        gap: 20px;
        width: 100%;
    }

    .content {
        width: 100%;
    }

    .order {
        box-sizing: border-box;
        padding: 4px;
        ;
        border: 1px solid black;
        width: 320px;
        /* height: 400px; */
    }

    .order-hidden {
        display: none;
    }

    h3 {
        margin: 0px;
    }

    h4 {
        margin: 8px;
    }

    h5 {
        margin: 0px;
        padding-left: 32px;
        line-height: 1;
    }

    ul {
        margin: 4px;
        padding-left: 32px
    }
</style>
            <script>
                var difficulty = `Easy`;
                function doFilter() {
                    for (let o of document.getElementsByClassName(`order-logic`)) {
                        if (o.classList.contains(`for-difficulty-${difficulty}`)) {
                            o.classList.remove(`order-hidden`);
                        } else {
                            o.classList.add(`order-hidden`);
                        }
                    }

                    for (let o of document.getElementsByClassName(`order`)) {
                        let hide = true;
                        for (let logic of o.getElementsByClassName(`order-logic`)) {
                            if (!logic.classList.contains(`order-hidden`)) {
                                hide = false;
                                break;
                            }
                        }

                        if (hide)
                            o.classList.add(`order-hidden`);
                        else
                            o.classList.remove(`order-hidden`);
                    }
                }
                function filterDifficulty(btn) {
                    for (let b of document.getElementsByClassName(`button-28`))
                        if (b != btn)
                            b.classList.remove(`button-selected`);
                    btn.classList.toggle(`button-selected`);

                    if (btn.innerText != difficulty) {
                        difficulty = btn.innerText
                        doFilter();
                    }
                }

                doFilter();
            </script>
");

            index.AppendLine("</html>");
            Write(index, "orders", "index");
        }

        private static void DumpBuilding(BuildingModel building)
        {
            var txt = Begin(building.Name, building.Description, building.icon);

            foreach (var recipe in recipeById.Values.Where(r => r.buildingModel == building))
            {
                DumpRecipe(txt, recipe.outputName, recipe, false, LinkType.Good);
            }
         
            End(txt);
            Write(txt, "buildings", building.Name);
        }

        private static void DumpGood(GoodModel good)
        {
            try
            {

                var txt = Begin(good.Name, good.Description, good.icon);

                if (goodInfo.ContainsKey(good.Name))
                {
                    txt.AppendLine($@"<h3>Produced By</h3>");
                    foreach (var recipe in FindProducersOf(good.Name))
                        DumpRecipe(txt, recipe.building, recipe, false, LinkType.Building);

                    txt.AppendLine($@"<h3>Ingredient Of</h3>");
                    foreach (var creates in FindConsumersOf(good.Name))
                    {
                        if (creates.Key != null)
                            txt.AppendLine($@"<h4>{creates.Key.icon.Small()}<a href=""{creates.Key.Link()}"">{creates.Key.Name}</a></h4>");
                        else
                            txt.AppendLine($@"<h4>Needs</h4>");
                        txt.AppendLine($@"<div class=""recipe-block"">");
                        foreach (var creating in creates.OrderByDescending(c => c.tier))
                            DumpRecipe(txt, creating.building, creating, true, LinkType.Building);
                        txt.AppendLine($@"</div>");
                    }
                }

                End(txt);
                Write(txt, "goods", good.Name);
            }
            catch (Exception ex)
            {
                LogInfo("ERROR: good: " + good.Name);
                Instance.Logger.LogError(ex);
            }
        }

        public enum LinkType
        {
            Good,
            Recipe,
            Building,
            Need,
        }

        private static void DumpRecipe(StringBuilder txt, string title, RecipeRaw recipe, bool hidden, LinkType linkType)
        {
            string link = linkType switch
            {
                LinkType.Good => recipe.output?.good?.Link(),
                LinkType.Recipe => recipe.output.good.Link() + $"#{recipe.outputName.Sane()}",
                LinkType.Building => recipe.buildingModel.Link(),
                LinkType.Need => null,
                _ => null,
            };
            Sprite icon = linkType switch
            {
                LinkType.Good => recipe.output?.good?.icon,
                LinkType.Recipe => recipe.output?.good?.icon,
                LinkType.Building => recipe.buildingModel.icon,
                LinkType.Need => null,
                _ => null,
            };
            BeginRecipeEmit(txt, title, recipe.tier, link, hidden, icon, recipe);
            bool first = true;
            foreach (var input in recipe.ingredients)
            {
                if (!first)
                    txt.AppendLine($@"<div class=""recipe-add"">+</div>");
                txt.AppendLine($@"<div class=""ingredient"">");
                foreach (var g in input.goods)
                    txt.AppendLine(g.Cost("recipe-ingredient").Div());

                txt.AppendLine($@"</div>");
                first = false;
            }
            if (recipe.output != null)
            {
                txt.AppendLine($@"<div class=""recipe-result"">");
                txt.AppendLine("🢂 " + recipe.output.Cost("recipe-ingredient").Surround("span"));
                txt.AppendLine($@"</div>");
            }
            EndRecipeEmit(txt);
        }

        public static void Write(StringBuilder txt, string directory, string name)
        {
            File.WriteAllText($@"C:\Users\worce\source\repos\data-wiki-root\data-wiki\{(directory != null ? directory + "\\" : "")}{name.Sane()}.html", txt.ToString());
        }
        public static void Write(Texture2D texture)
        {
            //var file = $@"C:\Users\worce\source\repos\data-wiki\img\{texture.name}";
            //if (!File.Exists(file + ".png"))
            //    File.Create(file + ".WANT").Close();
            //else if (File.Exists(file + ".WANT"))
            //    File.Delete(file + ".WANT");
        }

        private static void EmitResult(StringBuilder txt, string result)
        {
            txt.AppendLine($@"<div class=""result"">{result}</div>");
        }

        private static void BeginRecipeEmit(StringBuilder txt, string title, string tier, string link, bool hidden, Sprite icon, RecipeRaw recipe)
        {
            txt.AppendLine($@"<div class=""recipe-container"">");
            txt.AppendLine($@"<div class=""recipe-header"">");
            if (icon != null)
                txt.AppendLine(icon.Small());
            txt.Append($@"<h4 class=""recipe-title"">");
            if (link != null)
                txt.Append($@"<a href=""{link}"">");
            txt.Append($@"{title.Replace(" T0", "").Replace(" T1", "").Replace(" T2", "").Replace(" T3", "")} [{tier}]");
            txt.Append($@"</h4>");
            if (link != null)
                txt.Append($@"</a>");
            txt.AppendLine();
            txt.AppendLine(@"<span width=100 style=""display:inline-block; width: 100px;""/></span>");

            txt.AppendLine(@"<span class=""recipe-controls"">");
            if (recipe.CustomTime)
            {
                txt.AppendLine($@"<h4 class=""recipe-title recipe-time"">{recipe.timeA.Item2.Minutes()} + {recipe.timeB.Item2.Minutes()}</h4>");
            }
            else
                txt.AppendLine($@"<h4 class=""recipe-title recipe-time"">{recipe.processingTime.Minutes()}</h4>");
            txt.AppendLine($@"<span class=""toggle-recipe"" onclick=""toggleRecipe(this);"">{(hidden ? "show" : "hide")}</button>");
            txt.AppendLine(@"</span>");
            txt.AppendLine($@"</div>");
            txt.AppendLine($@"<div class=""recipe {(hidden ? "recipe-hidden" : "")}"">");
        }

        private static void EndRecipeEmit(StringBuilder txt)
        {
            txt.AppendLine($@"</div>");
            txt.AppendLine($@"</div>");
        }

        private static string HTML_HEAD_STR = null;
        public static string HTML_HEAD => HTML_HEAD_STR ??= $@"<head> 
                <meta charset=""UTF-8"">
                <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
                <title>AgainstTheStormpedia</title>
                <link rel=""stylesheet"" href=""/data-wiki/simple.min.css"">
                <link rel=""stylesheet"" href=""/data-wiki/common.css"">
                <script>
                    var constructionRates = new Map([{
                        string.Join(",\n",
                            GameSettings.difficulties.Where(d => d.modifiers.Any(m => m.effect is ConstructionCostEffectModel)).Select(
                                d => @$"[""{d.Name.Sane()}"", {d.modifiers.Where(m => m.effect is ConstructionCostEffectModel).Sum(m => (m.effect as ConstructionCostEffectModel).amount)}]"
                            )
                        )
                    }]);
                    var relicWorkRates = new Map([{
                        string.Join(",\n",
                            GameSettings.difficulties.Where(d => d.modifiers.Any(m => m.effect is RelicsWorkingTimeRateEffectModel)).Select(
                                d => @$"[""{d.Name.Sane()}"", {d.modifiers.Where(m => m.effect is RelicsWorkingTimeRateEffectModel).Sum(m => (m.effect as RelicsWorkingTimeRateEffectModel).amount)}]"
                            )
                        )
                    }]);
                </script>
                <script src=""/data-wiki/common.js""></script>
            </head>";
        private static string NAV_STR = null;
        public static string NAV => NAV_STR ??= GenerateNav();
        private static string GenerateNav()
        {
            return $@"
                <nav>
                    <a href=""/data-wiki/buildings"">Buildings</a>
                    <a href=""/data-wiki/goods"">Goods</a>
                    <a href=""/data-wiki/orders"">Orders</a>
                    <a href=""/data-wiki/relics"">Glade Events</a>
                    <a href=""/data-wiki/cornerstones"">Cornerstones</a>
                    <a href=""/data-wiki/biomes"">Biomes</a>
                    <a href=""/data-wiki/upgrades"">Upgrades</a>
                </nav>
                 <nav>
                    <span>Game Version: {MainController.Instance.Build.version}</span>
                    <span>Scale costs for difficulty:</span>
                    <select id=""difficulty-select"">
                        {
                            string.Join("\n", 
                                    GameSettings.difficulties
                                    .Where(d => d.index >= 0)
                                    .Select(d => d.GetDisplayName().Surround("option", $@"value=""{d.Name.Sane()}"""))
                                    )
                        }
                    </select>
                </nav>
                ";
        }

        private static StringBuilder Begin(string name, string description, Sprite icon)
        {
            StringBuilder txt = new();
            Write(icon.texture);
            txt.AppendLine($@"<html> {HTML_HEAD} <body>");

            txt.AppendLine($@"<header>{NAV}</header><main>");
            txt.AppendLine($@"<h2>{icon.Normal()}{name}</h2>");
            txt.AppendLine($@"<h4>{description}</h4>");
            return txt;
        }

        private static void End(StringBuilder txt)
        {
                txt.AppendLine(@"</body>");
                txt.Append(@"


        <style>
            h4 {
                margin: 0;
            }
        
            .recipe {
                display: flex;
                padding-left: 40;
                align-items: center;
                gap: 16px;
            }
        
            .recipe-title {
                display: inline-block;
                margin: 0;
                margin-top: 8px;
            }
        
            .recipe-hidden {
                display: none;
            }
        
            .recipe>div {
                display: inline-block;
                font-size: 24;
            }
        
            .recipe-block {
                margin-left: 40px;
            }
        
            .ingredient,
            .result {
                background-color: transparent;
                padding: 4px 10px 4px 10px;
                border-radius: 14px;
                border: 2px solid darkslategrey;
                box-shadow: 0px 0px 3px darkslategray;
            }

            .recipe-container {
                margin-top: 16px;
                margin-bottom: 16px;
                width: 900px;
            }
        
            .recipe-header {
                /* background: url(../recipe_bg_header.png); */
                height: 50px;
                /* background-size: 100% 100%; */
                border-image-source: url(../recipe_bg_header.png);
                border-image-slice: 20 26 9 12 fill;
                border-image-width: 20px 20px 20px 20px;
                border-image-outset: 0px 0px 0px 0px;
                border-image-repeat: stretch repeat;
                padding: 0px 0px 0px 22px;
            }
        
          .recipe {
                background: url(../recipe_bg_main.png);
                padding: 10px 0px 30px 30px;
                background-size: 100%;
                background-repeat: repeat-y;
            }
        
        
            h2 {
                margin: 0px;
            }
        
            img {
                vertical-align: middle;
            }
        
            a {
                padding-left: 4px;
            }
        
            .recipe-title>a, .recipe-title {
                color: lightgray;
            }

            .recipe-controls {
                position: relative;
                float: right;
                margin-right: 30px;
            }
        
            .toggle-recipe {
                height: 0px;
                width: 64px;
                display: inline-flex;
                align-items: center;
                border-image-source: url(../button_bg.png);
                border-image-slice: 10 9 13 9 fill;
                border-image-width: 13px 13px 13px 13px;
                border-image-outset: 6px 13px 6px 13px;
                border-image-repeat: stretch stretch;
                cursor: pointer;
                user-select: none;
                font-size: smaller;
                padding: 10px;
                margin-left: 40px;;
            }

            .recipe-time {
                margin-right: 100px;
            }
        
            .toggle-recipe:hover {
                filter: brightness(150%);
            }

        
            .recipe-container,
            .recipe a {
                color: lightgray;
            }
        </style>
        <script>
            function toggleRecipe(showButton) {
                if (showButton.parentElement.parentElement.parentElement.getElementsByClassName(`recipe`)[0].classList.toggle(`recipe-hidden`))
                    showButton.innerText = `show`;
                else
                    showButton.innerText = `hide`;
            }
        </script>
        ");

                txt.AppendLine(@"</html>");

        }

        private static string GetTier(string name)
        {
            if (name.EndsWith("T3"))
                return "★★★";
            else if (name.EndsWith("T2"))
                return "★★☆";
            else if (name.EndsWith("T1"))
                return "★☆☆";
            else
                return "☆☆☆";
        }

        //[HarmonyPatch(typeof(BuildingBlight), nameof(BuildingBlight.AddBlight))]
        //[HarmonyPrefix]
        //static bool BuildingBlight_AddBlight() { return false; }
    }

    public class GoodInfo
    {
        public List<Plugin.RecipeRaw> producedBy = new();
        public List<string> ingredientIn = new();

        public GoodInfo()
        {
        }

        public override bool Equals(object obj)
        {
            return obj is GoodInfo other &&
                   EqualityComparer<List<Plugin.RecipeRaw>>.Default.Equals(producedBy, other.producedBy) &&
                   EqualityComparer<List<string>>.Default.Equals(ingredientIn, other.ingredientIn);
        }

        public override int GetHashCode()
        {
            int hashCode = 2144880161;
            hashCode = hashCode * -1521134295 + EqualityComparer<List<Plugin.RecipeRaw>>.Default.GetHashCode(producedBy);
            hashCode = hashCode * -1521134295 + EqualityComparer<List<string>>.Default.GetHashCode(ingredientIn);
            return hashCode;
        }
    }
}