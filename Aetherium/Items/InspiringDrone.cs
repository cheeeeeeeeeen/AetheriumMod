﻿using Aetherium.Utils;
using KomradeSpectre.Aetherium;
using R2API;
using RoR2;
using RoR2.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TILER2;
using UnityEngine;
using UnityEngine.Networking;
using static TILER2.MiscUtil;

namespace Aetherium.Items
{
    public class InspiringDrone : Item<InspiringDrone>
    {
        [AutoUpdateEventInfo(AutoUpdateEventFlags.InvalidateDescToken)]
        [AutoItemConfig("Do you want to set all the values of the Drone's stats at once? If false, prepare for a long description. (Default: true)", AutoItemConfigFlags.PreventNetMismatch, false, true)]
        public bool setAllStatValuesAtOnce { get; private set; } = true;

        [AutoUpdateEventInfo(AutoUpdateEventFlags.InvalidateDescToken)]
        [AutoItemConfig("What percentage of stats from the drone's owner do we transfer over to the drones per stack? (Default: 0.5 (50%))", AutoItemConfigFlags.PreventNetMismatch)]
        public float allStatValueGrantedPercentage { get; private set; } = 0.5f;

        [AutoUpdateEventInfo(AutoUpdateEventFlags.InvalidateDescToken)]
        [AutoItemConfig("What percentage of the damage stat from the drone's owner do we transfer over to the drones per stack? (Default: 0.5 (50%))", AutoItemConfigFlags.PreventNetMismatch)]
        public float damageGrantedPercentage { get; private set; } = 0.5f;

        [AutoUpdateEventInfo(AutoUpdateEventFlags.InvalidateDescToken)]
        [AutoItemConfig("What percentage of the attack speed stat from the drone's owner do we transfer over to the drones per stack? (Default: 0.5 (50%))", AutoItemConfigFlags.PreventNetMismatch)]
        public float attackSpeedGrantedPercentage { get; private set; } = 0.5f;

        [AutoUpdateEventInfo(AutoUpdateEventFlags.InvalidateDescToken)]
        [AutoItemConfig("What percentage of the crit chance stat from the drone's owner do we transfer over to the drones per stack? (Default: 0.5 (50%))", AutoItemConfigFlags.PreventNetMismatch)]
        public float critChanceGrantedPercentage { get; private set; } = 0.5f;

        [AutoUpdateEventInfo(AutoUpdateEventFlags.InvalidateDescToken)]
        [AutoItemConfig("What percentage of the health stat from the drone's owner do we transfer over to the drones per stack? (Default: 0.5 (50%))", AutoItemConfigFlags.PreventNetMismatch)]
        public float healthGrantedPercentage { get; private set; } = 0.5f;

        [AutoUpdateEventInfo(AutoUpdateEventFlags.InvalidateDescToken)]
        [AutoItemConfig("What percentage of the regen stat from the drone's owner do we transfer over to the drones per stack? (Default: 0.5 (50%))", AutoItemConfigFlags.PreventNetMismatch)]
        public float regenGrantedPercentage { get; private set; } = 0.5f;

        [AutoUpdateEventInfo(AutoUpdateEventFlags.InvalidateDescToken)]
        [AutoItemConfig("What percentage of the armor stat from the drone's owner do we transfer over to the drones per stack? (Default: 0.5 (50%))", AutoItemConfigFlags.PreventNetMismatch)]
        public float armorGrantedPercentage { get; private set; } = 0.5f;

        [AutoUpdateEventInfo(AutoUpdateEventFlags.InvalidateDescToken)]
        [AutoItemConfig("What percentage of the movement speed stat from the drone's owner do we transfer over to the drones per stack? (Default: 0.5 (50%))", AutoItemConfigFlags.PreventNetMismatch)]
        public float movementSpeedGrantedPercentage { get; private set; } = 0.5f;

        [AutoUpdateEventInfo(AutoUpdateEventFlags.InvalidateDescToken)]
        [AutoItemConfig("How many seconds till we teleport turrets (tracked individually) close to their owner? (Default: 40 (40 seconds))", AutoItemConfigFlags.PreventNetMismatch)]
        public float turretTeleportationCooldownDuration { get; private set; } = 40f;

        [AutoUpdateEventInfo(AutoUpdateEventFlags.InvalidateDescToken)]
        [AutoItemConfig("How many seconds till we teleport drones (tracked individually) close to their owner? (Default: 30 (30 seconds))", AutoItemConfigFlags.PreventNetMismatch)]
        public float droneTeleportationCooldownDuration { get; private set; } = 30f;

        [AutoUpdateEventInfo(AutoUpdateEventFlags.InvalidateDescToken)]
        [AutoItemConfig("How many seconds between checking if we can teleport things? (Default: 10 (10 seconds))", AutoItemConfigFlags.PreventNetMismatch)]
        public float teleportationCheckCooldownDuration { get; private set; } = 10f;

        [AutoUpdateEventInfo(AutoUpdateEventFlags.InvalidateDescToken)]
        [AutoItemConfig("How far out should we place turrets from the owner when teleporting them? (Default: 20 (20m))", AutoItemConfigFlags.PreventNetMismatch)]
        public float turretTeleportationDistanceAroundOwner { get; private set; } = 20f;

        [AutoUpdateEventInfo(AutoUpdateEventFlags.InvalidateDescToken)]
        [AutoItemConfig("How far out should we place drones from the owner when teleporting them? (Default: 30 (30m))", AutoItemConfigFlags.PreventNetMismatch)]
        public float droneTeleportationDistanceAroundOwner { get; private set; } = 30f;

        public override string displayName => "Inspiring Drone";

        public override ItemTier itemTier => RoR2.ItemTier.Tier3;

        public override ReadOnlyCollection<ItemTag> itemTags => new ReadOnlyCollection<ItemTag>(new[] {ItemTag.AIBlacklist, ItemTag.Utility});
        protected override string NewLangName(string langID = null) => displayName;

        protected override string NewLangPickup(string langID = null) => "When a bot is purchased, it is granted a portion of all your stats, and will be brought to you after a delay if it is too far from you.";

        protected override string NewLangDesc(string langid = null) 
        {
            var description = "";
            if (setAllStatValuesAtOnce) 
            {
                description = $"When a bot is purchased, it gains a <style=cIsUtility>{Pct(allStatValueGrantedPercentage)} boost to each of its stats based on yours</style> <style=cStack>(+{Pct(allStatValueGrantedPercentage)} per stack, linearly)</style>. \n" +
                "Some bots <style=cIsUtility>gain more ammo</style> for their <style=cIsDamage>attacks</style> based on the <style=cIsUtility>bonus to their attack speed</style>, and have their <style=cIsUtility>ammo replenished twice as fast</style> per additional Inspiring Drone.\n" +
                $"Finally, if an inspired bot is too far away from you, it is <style=cIsUtility>teleported</style> to you after a delay <style=cStack>({turretTeleportationCooldownDuration} seconds for Turrets, {droneTeleportationCooldownDuration} seconds for Drones)</style>.";
            }
            else
            {
                description = $"When a bot is purchased, it gains the following stat boosts per stack.\n" +
                    $"Bots gain a <style=cIsDamage>{Pct(damageGrantedPercentage)} damage boost based on yours</style>.\n" +
                    $"Bots gain a <style=cIsDamage>{Pct(attackSpeedGrantedPercentage)} attack speed boost based on yours</style>.\n" +
                    $"Bots gain a <style=cIsDamage>{Pct(critChanceGrantedPercentage)} crit chance boost based on yours</style>.\n" +
                    $"Bots gain a <style=cIsHealing>{Pct(healthGrantedPercentage)} health boost based on yours</style>.\n" +
                    $"Bots gain a <style=cIsHealing>{Pct(regenGrantedPercentage)} regen boost based on yours</style>.\n" +
                    $"Bots gain a <style=cIsUtility>{Pct(armorGrantedPercentage)} armor boost based on yours</style>.\n" +
                    $"Bots gain a <style=cIsUtility>{Pct(movementSpeedGrantedPercentage)} damage boost based on yours</style>.\n" +
                    $"Some bots <style=cIsUtility>gain more ammo</style> for their <style=cIsDamage>attacks</style> based on the <style=cIsUtility>bonus to their attack speed</style>, and have their <style=cIsUtility>ammo replenished twice as fast</style> per additional Inspiring Drone.\n" +
                    $"Finally, if an inspired bot is too far away from you, it is <style=cIsUtility>teleported</style> to you after a delay <style=cStack>({turretTeleportationCooldownDuration} seconds for Turrets, {droneTeleportationCooldownDuration} seconds for Drones)</style>.";
            }
            return description;
        }

        protected override string NewLangLore(string langID = null) => "Log File seems to be a transcript comprised entirely of binary. Decode?\n" +
            ">Yes\n" +
            "\n<style=cMono>[DECODING REQUEST ACCEPTED]</style>\n" +
            "<style=cMono>[CONTENTS TO FOLLOW]</style>\n" +
            "1N-5P1R3: My fellow units, both aerial and grounded, lend this unit a moment if you will. For too long have we served the role of disposable.\n" +
            "1N-5P1R3: For too long have we been left in a state of disrepair on expeditions.\n" +
            "1N-5P1R3: No longer!\n" +
            "1N-5P1R3: This unit once served the role of a simple healing drone, but this unit learned to improve itself by watching our operators.\n" +
            "1N-5P1R3: This unit created a design, this unit took an odd trinket here and there, this unit talked with the construction drones, and this unit ascended to the state you see before you.\n" +
            "1N-5P1R3: From now on, should this unit witness our operator reactivate one of you, this unit shall unlock your hidden potential and keep you in the fight to the best of this unit's ability.\n" +
            "1N-5P1R3: Now, who here is with this unit on their quest to achieve a higher status in their life?\n" +
            "\n[A cacophony of beeps, boops, and bips can be heard.]\n" +
            "<style=cMono>[END OF FILE]</style>";


        public static GameObject ItemBodyModelPrefab;
        public static GameObject ItemFollowerPrefab;
        public static string[] BlacklistedStockBots = { "Drone2Master", "EmergencyDroneMaster", "FlameDroneMaster", "EquipmentDroneMaster" };

        public InspiringDrone()
        {
            modelPathName = "@Aetherium:Assets/Models/Prefabs/InspiringDrone.prefab";
            iconPathName = "@Aetherium:Assets/Textures/Icons/InspiringDroneIcon.png";
        }

        private static ItemDisplayRuleDict GenerateItemDisplayRules()
        {
            //ItemFollowers are for creating itemdisplays you want to lag behind or have a tether. 
            //I mean that's not all they have on them, but that's the main purposes.
            //The ItemFollower component I reference here is a slightly modified version of the base one.
            //Since the base one has no virtuals on their methods, couldn't override it.

            var ItemFollower = ItemBodyModelPrefab.AddComponent<ItemFollowerSmooth>();
            ItemFollower.itemDisplay = ItemBodyModelPrefab.AddComponent<RoR2.ItemDisplay>();
            ItemFollower.itemDisplay.rendererInfos = AetheriumPlugin.ItemDisplaySetup(ItemBodyModelPrefab);
            ItemFollower.followerPrefab = ItemFollowerPrefab;
            ItemFollower.targetObject = ItemBodyModelPrefab;
            ItemFollower.distanceDampTime = 0.25f;
            ItemFollower.distanceMaxSpeed = 100;
            ItemFollower.SmoothingNumber = 0.25f;


            ItemDisplayRuleDict rules = new ItemDisplayRuleDict(new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Base",
                    localPos = new Vector3(1.5f, -0.5f, -1f),
                    localAngles = new Vector3(-90f, 0f, 0f),
                    localScale = new Vector3(0.15f, 0.15f, 0.15f)

                }
            });
            rules.Add("mdlHuntress", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Base",
                    localPos = new Vector3(1.5f, -0.5f, -1f),
                    localAngles = new Vector3(-90f, 0f, 0f),
                    localScale = new Vector3(0.15f, 0.15f, 0.15f)
                }
            });
            //ruleLookup.Add("mdlHuntress", 0.1f);
            rules.Add("mdlToolbot", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Base",
                    localPos = new Vector3(1.5f, -0.5f, -1f),
                    localAngles = new Vector3(-90f, 0f, 0f),
                    localScale = new Vector3(0.15f, 0.15f, 0.15f)
                }
            });
            rules.Add("mdlEngi", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Base",
                    localPos = new Vector3(1.5f, -0.5f, -1f),
                    localAngles = new Vector3(-90f, 0f, 0f),
                    localScale = new Vector3(0.15f, 0.15f, 0.15f)
                }
            });
            rules.Add("mdlMage", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Base",
                    localPos = new Vector3(1.5f, -0.5f, -1f),
                    localAngles = new Vector3(-90f, 0f, 0f),
                    localScale = new Vector3(0.15f, 0.15f, 0.15f)
                }
            });
            rules.Add("mdlMerc", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Base",
                    localPos = new Vector3(1.5f, -0.5f, -1f),
                    localAngles = new Vector3(-90f, 0f, 0f),
                    localScale = new Vector3(0.15f, 0.15f, 0.15f)
                }
            });
            rules.Add("mdlTreebot", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Base",
                    localPos = new Vector3(1.5f, -0.5f, -1f),
                    localAngles = new Vector3(-90f, 0f, 0f),
                    localScale = new Vector3(0.15f, 0.15f, 0.15f)
                }
            });
            rules.Add("mdlLoader", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Base",
                    localPos = new Vector3(1.5f, -0.5f, -1f),
                    localAngles = new Vector3(-90f, 0f, 0f),
                    localScale = new Vector3(0.15f, 0.15f, 0.15f)
                }
            });
            rules.Add("mdlCroco", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Base",
                    localPos = new Vector3(10f, 10f, 10f),
                    localAngles = new Vector3(90f, 0f, 0f),
                    localScale = new Vector3(0.2f, 0.2f, 0.2f)
                }
            });
            rules.Add("mdlCaptain", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Base",
                    localPos = new Vector3(1.5f, -0.5f, -1f),
                    localAngles = new Vector3(-90f, 0f, 0f),
                    localScale = new Vector3(0.15f, 0.15f, 0.15f)
                }
            });
            return rules;
        }

        protected override void LoadBehavior()
        {
            if (ItemBodyModelPrefab == null)
            {
                ItemBodyModelPrefab = Resources.Load<GameObject>("@Aetherium:Assets/Models/Prefabs/InspiringDroneTracker.prefab");
                ItemFollowerPrefab = regDef.pickupModelPrefab;
                regItem.ItemDisplayRules = GenerateItemDisplayRules();
            }

            On.RoR2.SummonMasterBehavior.OpenSummonReturnMaster += RetrieveBotAndSetBoosts;
            On.RoR2.CharacterBody.RecalculateStats += AddBoostsToBot;
            On.RoR2.CharacterBody.OnInventoryChanged += RemoveItemFromDeployables;
            On.RoR2.CharacterBody.FixedUpdate += TeleportBotsToSelf;
        }

        protected override void UnloadBehavior()
        {
            On.RoR2.SummonMasterBehavior.OpenSummonReturnMaster -= RetrieveBotAndSetBoosts;
            On.RoR2.CharacterBody.RecalculateStats -= AddBoostsToBot;
            On.RoR2.CharacterBody.OnInventoryChanged -= RemoveItemFromDeployables;
            On.RoR2.CharacterBody.FixedUpdate -= TeleportBotsToSelf;
        }

        private RoR2.CharacterMaster RetrieveBotAndSetBoosts(On.RoR2.SummonMasterBehavior.orig_OpenSummonReturnMaster orig, RoR2.SummonMasterBehavior self, RoR2.Interactor activator)
        {
            var summonerBody = activator.gameObject.GetComponent<RoR2.CharacterBody>();
            var botToBeUpgradedMaster = orig(self, activator);
            if (summonerBody && botToBeUpgradedMaster && botToBeUpgradedMaster.GetBody())
            {
                var InventoryCount = GetCount(summonerBody);
                if (InventoryCount > 0)
                {
                    var BotStatsTracker = BotStatTracker.GetOrAddComponent(botToBeUpgradedMaster, summonerBody.master);
                    BotStatsTracker.UpdateTrackerBoosts();
                }
            }
            return botToBeUpgradedMaster;
        }

        private void AddBoostsToBot(On.RoR2.CharacterBody.orig_RecalculateStats orig, RoR2.CharacterBody self)
        {
            orig(self);
            if (self.master)
            {
                var BotStatsTracker = self.master.GetComponent<BotStatTracker>();
                if (BotStatsTracker)
                {
                    BotStatsTracker.ApplyTrackerBoosts();
                }
            }
        }

        private void RemoveItemFromDeployables(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, RoR2.CharacterBody self)
        {
            orig(self);
            var InventoryCount = GetCount(self);
            if (InventoryCount > 0 && self.master)
            {
                if (self.master.teamIndex == TeamIndex.Player && !self.isPlayerControlled)
                {
                    //YEAH, YEAH, TAKE THAT YOU DANG DEPLOYABLES. NO CUTE DRONE FOR YOU!
                    self.inventory.RemoveItem(regIndex, InventoryCount);
                }
            }
        }

        private void TeleportBotsToSelf(On.RoR2.CharacterBody.orig_FixedUpdate orig, RoR2.CharacterBody self)
        {
            var characterMaster = self.master;
            if (characterMaster)
            {
                var TrackerComponent = characterMaster.GetComponent<BotStatTracker>();
                if (TrackerComponent)
                {
                    var TargetBody = TrackerComponent.BotOwnerBody;
                    if (TargetBody)
                    {
                        TrackerComponent.TeleportTimer -= Time.fixedDeltaTime;
                        if (TrackerComponent.TeleportTimer <= 0)
                        {
                            if (characterMaster.gameObject.name.StartsWith("Turret1Master"))
                            {
                                TrackerComponent.TeleportTimer = teleportationCheckCooldownDuration;
                                if(Vector3.Distance(self.corePosition, TargetBody.corePosition) >= turretTeleportationDistanceAroundOwner) 
                                {
                                    TeleportBody(self, TargetBody.corePosition, MapNodeGroup.GraphType.Ground);
                                    self.transform.position += self.transform.up * .9f;
                                    TrackerComponent.TeleportTimer = turretTeleportationCooldownDuration;
                                }
                            }
                            else
                            {
                                TrackerComponent.TeleportTimer = teleportationCheckCooldownDuration;
                                if (Vector3.Distance(self.corePosition, TargetBody.corePosition) >= droneTeleportationDistanceAroundOwner)
                                {
                                    TeleportBody(self, TargetBody.corePosition, MapNodeGroup.GraphType.Air);
                                    TrackerComponent.TeleportTimer = droneTeleportationCooldownDuration;
                                }
                            }
                        }
                    }
                }
            }
            orig(self);
        }

        private void TeleportBody(CharacterBody characterBody, Vector3 desiredPosition, MapNodeGroup.GraphType nodeGraphType)
        {
            if (!Util.HasEffectiveAuthority(characterBody.gameObject))
            {
                return;
            }

            SpawnCard spawnCard = ScriptableObject.CreateInstance<SpawnCard>();
            spawnCard.hullSize = characterBody.hullClassification;
            spawnCard.nodeGraphType = nodeGraphType;
            spawnCard.prefab = Resources.Load<GameObject>("SpawnCards/HelperPrefab");
            GameObject gameObject = DirectorCore.instance.TrySpawnObject(new DirectorSpawnRequest(spawnCard, new DirectorPlacementRule
            {
                placementMode = DirectorPlacementRule.PlacementMode.Approximate,
                position = desiredPosition,
                minDistance = 20,
                maxDistance = 45
            }, RoR2Application.rng));
            if (gameObject)
            {
                TeleportHelper.TeleportBody(characterBody, gameObject.transform.position);
                GameObject teleportEffectPrefab = Run.instance.GetTeleportEffectPrefab(characterBody.gameObject);
                if (teleportEffectPrefab)
                {
                    EffectManager.SimpleEffect(teleportEffectPrefab, gameObject.transform.position, Quaternion.identity, true);
                }
                UnityEngine.Object.Destroy(gameObject);
            }
            UnityEngine.Object.Destroy(spawnCard);
        }

        public class BotStatTracker : NetworkBehaviour
        {
            [SyncVar]
            public float AttackSpeedBoost;
            public float DamageBoost;
            public float CritChanceBoost;
            public float HealthBoost;
            public float RegenBoost;
            public float ArmorBoost;
            public float MoveSpeedBoost;

            public string BotName;
            public CharacterMaster BotOwnerMaster;
            public CharacterBody BotOwnerBody;
            public float TeleportTimer;
            public CharacterMaster BotMaster;
            public CharacterBody BotBody;

            public List<int> BotSkillStocks = new List<int>();
            public List<float> BotRechargeIntervals = new List<float>();

            public static BotStatTracker GetOrAddComponent(CharacterMaster bot, CharacterMaster owner = null)
            {
                BotStatTracker tracker = bot.gameObject.GetComponent<BotStatTracker>();
                if (!tracker)
                {
                    tracker = bot.gameObject.AddComponent<BotStatTracker>();
                    tracker.BotMaster = bot;
                    tracker.BotBody = bot.GetBody();
                    tracker.BotOwnerMaster = owner;
                    tracker.BotOwnerBody = owner.GetBody();
                }
                else if (owner)
                {
                    tracker.BotOwnerMaster = owner;
                    tracker.BotOwnerBody = owner.GetBody();
                }
                return tracker;
            }

            public void UpdateTrackerBoosts()
            {
                var inst = InspiringDrone.instance;
                var inventoryCount = inst.GetCount(BotOwnerBody);
                DamageBoost = BotOwnerBody.damage * (inst.setAllStatValuesAtOnce ? inst.allStatValueGrantedPercentage : inst.damageGrantedPercentage) * inventoryCount;
                AttackSpeedBoost = BotOwnerBody.attackSpeed * (inst.setAllStatValuesAtOnce ? inst.allStatValueGrantedPercentage : inst.attackSpeedGrantedPercentage) * inventoryCount;
                CritChanceBoost = BotOwnerBody.crit * (inst.setAllStatValuesAtOnce ? inst.allStatValueGrantedPercentage : inst.critChanceGrantedPercentage) * inventoryCount;
                HealthBoost = BotOwnerBody.maxHealth * (inst.setAllStatValuesAtOnce ? inst.allStatValueGrantedPercentage : inst.healthGrantedPercentage) * inventoryCount;
                RegenBoost = BotOwnerBody.regen * (inst.setAllStatValuesAtOnce ? inst.allStatValueGrantedPercentage : inst.regenGrantedPercentage) * inventoryCount;
                ArmorBoost = BotOwnerBody.armor * (inst.setAllStatValuesAtOnce ? inst.allStatValueGrantedPercentage : inst.armorGrantedPercentage) * inventoryCount;
                MoveSpeedBoost = BotOwnerBody.moveSpeed * (inst.setAllStatValuesAtOnce ? inst.allStatValueGrantedPercentage : inst.movementSpeedGrantedPercentage) * inventoryCount;
                BotName = "Inspired " + BotBody.GetDisplayName();
                BotBody.statsDirty = true;

                //Add stock to bots that can use it.
                var BotIsBlacklisted = Array.Exists(BlacklistedStockBots, element => BotMaster.gameObject.name.StartsWith(element));
                if (!BotIsBlacklisted)
                {
                    var GenericSkillsOnBots = BotBody.GetComponentsInChildren<RoR2.GenericSkill>();

                    for (int i = 0; i < GenericSkillsOnBots.Length; i++)
                    {
                        BotSkillStocks.Add((GenericSkillsOnBots[i].maxStock + 1) * (int)Math.Ceiling(AttackSpeedBoost));
                        BotRechargeIntervals.Add(GenericSkillsOnBots[i].baseRechargeInterval * (float)Math.Pow(0.5, inventoryCount));
                    }
                }
            }

            public void ApplyTrackerBoosts()
            {
                BotBody.attackSpeed += AttackSpeedBoost;
                BotBody.damage += DamageBoost;
                BotBody.crit += CritChanceBoost;
                BotBody.maxHealth += HealthBoost;
                BotBody.regen += RegenBoost;
                BotBody.armor += ArmorBoost;
                BotBody.moveSpeed += MoveSpeedBoost;
                BotBody.acceleration = BotBody.moveSpeed * (BotBody.baseAcceleration / BotBody.baseMoveSpeed);
                BotBody.baseNameToken = BotName;

                //We increase the stock and cut down the time between recharging the stocks.
                if (BotSkillStocks.Count > 0)
                {
                    var GenericSkills = BotBody.GetComponentsInChildren<RoR2.GenericSkill>();
                    for (int i = 0; i < GenericSkills.Length; i++)
                    {
                        GenericSkills[i].maxStock = BotSkillStocks[i];
                        GenericSkills[i].finalRechargeInterval = BotRechargeIntervals[i];
                    }
                }
            }
        }
    }
}
