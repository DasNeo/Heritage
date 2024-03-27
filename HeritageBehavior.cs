using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets.Menu.Overlay;

namespace Heritage
{
    internal class HeritageBehavior : CampaignBehaviorBase
    {
        private Hero newLeader = null;
        private Hero leader = null;

        private Hero characterWindowHero = null;
        private MobileParty characterWindowParty = null;
        // TODO: add back in
        //private List<string> characterWindowEvents = null;
        private Settlement characterWindowSettlement = null;


        public override void SyncData(IDataStore dataStore)
        {
        }
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        private void OnSessionLaunched(CampaignGameStarter campaignStarter)
        {
            campaignStarter.AddPlayerLine("zendzee_legacy_start_leader", "companion_role", "zendzee_legacy_leader_pretalk",
                "{=zeeD75C6D0E}I want you to lead our clan.",
                ConditionChangeClanLeader, null, 110);
            campaignStarter.AddDialogLine("zendzee_legacy_candidate_pretalk", "zendzee_legacy_leader_pretalk", "zendzee_legacy_leader_need_confirm",
                "{=zee4E3D805F}Are you sure? This is a serious change...",
                null, null);
            campaignStarter.AddPlayerLine("zendzee_legacy_pretalk_confirm", "zendzee_legacy_leader_need_confirm", "zendzee_legacy_leader_confirm",
                "{=zee1E20B79B}Yes. I am absolutely sure.",
                null, null);
            campaignStarter.AddPlayerLine("zendzee_legacy_pretalk_nevermind", "zendzee_legacy_leader_need_confirm", "companion_okay",
                "{=mdNRYlfS}Nevermind.",
                null, null);
            campaignStarter.AddDialogLine("zendzee_legacy_candidate_accept", "zendzee_legacy_leader_confirm", "hero_main_options",
                "{=5hhxQBTj}I will be honored.[rb:positive]",
                null, ConsequenceChangeClanLeader);

            campaignStarter.AddPlayerLine("zendzee_charcter_window_open", "companion_role", "zendzee_charcter_window_active",
                "{=zee9065692B}Let me check your progress.",
                ConditionOpenCharacterWindow, ConsequenceCharacterWindowOpen, 110);
            campaignStarter.AddPlayerLine("zendzee_inventory_window_open", "companion_role", "zendzee_charcter_window_active",
                "{=zeeA3B242F4}Do you need new equipment?",
                ConditionOpenCharacterWindow, ConsequenceInventoryWindowOpen, 110);
            campaignStarter.AddDialogLine("zendzee_charcter_window_reply1", "zendzee_charcter_window_active", "zendzee_charcter_window_close",
                "{=zee376CA300}Is everything alright?",
                null, null);
            campaignStarter.AddPlayerLine("zendzee_charcter_window_reply2", "zendzee_charcter_window_close", "companion_okay",
                "{=zee5F9A2B79}Yes.",
                null, ConsequenceCharacterWindowClose);
        }

        private static bool ConditionOpenCharacterWindow()
        {
            return Hero.OneToOneConversationHero != null
                && Clan.PlayerClan != null
                && Hero.OneToOneConversationHero.Clan == Clan.PlayerClan
                && Hero.MainHero != null
                && MobileParty.MainParty != null
                && MobileParty.MainParty != Hero.OneToOneConversationHero.PartyBelongedTo;
        }

        private void PrepareCharacterWindow_Internal()
        {
            characterWindowSettlement = characterWindowHero.StayingInSettlement;

            /*
            characterWindowHero.CanHaveQuestsOrIssues
            characterWindowEvents = new List<string>(characterWindowHero.GetHeroOccupiedEvents());
            string evt;
            while ((evt = characterWindowHero.GetHeroOccupiedEvents().FirstOrDefault()) != default)
            {
                characterWindowHero.RemoveEventFromOccupiedHero(evt);
            }
            */

            characterWindowParty = characterWindowHero.PartyBelongedTo;
            if (characterWindowParty != MobileParty.MainParty)
            {
                if (characterWindowParty != null)
                {
                    characterWindowParty.MemberRoster.AddToCounts(characterWindowHero.CharacterObject, -1);
                }
                MobileParty.MainParty.MemberRoster.AddToCounts(characterWindowHero.CharacterObject, 1);
            }
        }

        private void ConsequenceCharacterWindowOpen()
        {
            characterWindowHero = Hero.OneToOneConversationHero;
            if (Hero.MainHero != null && MobileParty.MainParty != null && characterWindowHero != null)
            {
                PrepareCharacterWindow_Internal();

                Game.Current.GameStateManager.PushState(Game.Current.GameStateManager.CreateState<CharacterDeveloperState>(), 0);
            }
        }

        private void ConsequenceInventoryWindowOpen()
        {
            characterWindowHero = Hero.OneToOneConversationHero;
            if (Hero.MainHero != null && MobileParty.MainParty != null && characterWindowHero != null)
            {
                PrepareCharacterWindow_Internal();

                InventoryManager.OpenScreenAsInventory(null);
            }
        }

        private void ConsequenceCharacterWindowClose()
        {
            if (Hero.MainHero != null && MobileParty.MainParty != null && characterWindowHero != null)
            {
                if (characterWindowParty != MobileParty.MainParty)
                {
                    MobileParty.MainParty.MemberRoster.AddToCounts(characterWindowHero.CharacterObject, -1);
                    if (characterWindowParty != null)
                    {
                        characterWindowParty.MemberRoster.AddToCounts(characterWindowHero.CharacterObject, 1);
                        characterWindowParty.ChangePartyLeader(characterWindowHero.CharacterObject.HeroObject);
                    }
                }

                /*
                foreach (string e in characterWindowEvents)
                {
                    characterWindowHero.AddEventForOccupiedHero(e);
                }
                */

                characterWindowHero.StayingInSettlement = characterWindowSettlement;
            }

            characterWindowHero = null;
            characterWindowParty = null;
            //characterWindowEvents = null;
        }

        private void ConsequenceChangeClanLeader()
        {
            newLeader = Hero.OneToOneConversationHero;
            leader = Hero.MainHero;

            Utils.Print($"[ConsequenceChangeClanLeader] current leader {leader.Name} new leader {newLeader.Name}");
            if (newLeader.PartyBelongedTo != leader.PartyBelongedTo)
            {
                if (newLeader.PartyBelongedTo == null || newLeader != newLeader.PartyBelongedTo.LeaderHero)
                {
                    MobilePartyHelper.CreateNewClanMobileParty(newLeader, leader.Clan, out _);
                }

                if (leader.PartyBelongedTo?.CurrentSettlement != null)
                {
                    EnterSettlementAction.ApplyForParty(newLeader.PartyBelongedTo,
                        leader.PartyBelongedTo.CurrentSettlement);
                    LeaveSettlementAction.ApplyForParty(leader.PartyBelongedTo);
                }
            }

            CampaignEvents.TickEvent.AddNonSerializedListener(this, OnChangeClanLeader);
        }

        private void OnChangeClanLeader(float dt)
        {
            MobileParty leaderParty = leader.PartyBelongedTo;
            MobileParty newLeaderParty = newLeader.PartyBelongedTo;

            Utils.Print($"[OnChangeClanLeader] current leader {leader.Name} new leader {newLeader.Name}");
            
            if (leader == null || newLeader == null || leaderParty == null || newLeaderParty == null)
            {
                return;
            }

            if (newLeader.GovernorOf != null)
            {
                ChangeGovernorAction.RemoveGovernorOf(newLeader);
            }
            if (leader.GovernorOf != null)
            {
                ChangeGovernorAction.RemoveGovernorOf(leader);
            }

            /*
            string evt;
            while ((evt = newLeader.GetHeroOccupiedEvents().FirstOrDefault()) != default)
            {
                newLeader.RemoveEventFromOccupiedHero(evt);
            }
            while ((evt = leader.GetHeroOccupiedEvents().FirstOrDefault()) != default)
            {
                leader.RemoveEventFromOccupiedHero(evt);
            }
            */

            GiveGoldAction.ApplyBetweenCharacters(leader, newLeader, leader.Gold, true);

            leader.Clan.SetLeader(newLeader);

            Settlement currentSettlement = newLeaderParty.CurrentSettlement;
            
            ChangePlayerCharacterAction.Apply(newLeader);

            if (newLeaderParty != leaderParty)
            {
                if (newLeaderParty.CurrentSettlement == null)
                {
                    EnterSettlementAction.ApplyForParty(newLeaderParty, currentSettlement);
                }

                foreach (var troop in leaderParty.MemberRoster.GetTroopRoster())
                {
                    if (troop.Character == leader.CharacterObject)
                    {
                        continue;
                    }

                    leaderParty.MemberRoster.AddToCounts(troop.Character, -troop.Number);
                    newLeaderParty.MemberRoster.AddToCounts(troop.Character, troop.Number);
                }

                foreach (var troop in leaderParty.PrisonRoster.GetTroopRoster())
                {
                    if (troop.Character == leader.CharacterObject)
                    {
                        continue;
                    }

                    leaderParty.PrisonRoster.AddToCounts(troop.Character, -troop.Number);
                    newLeaderParty.PrisonRoster.AddToCounts(troop.Character, troop.Number);
                }

                ItemRosterElement i;
                while (leaderParty.ItemRoster.Count() > 0)
                {
                    i = leaderParty.ItemRoster.First();
                    leaderParty.ItemRoster.AddToCounts(i.EquipmentElement, -i.Amount);
                    newLeaderParty.ItemRoster.AddToCounts(i.EquipmentElement, i.Amount);
                }
                
                leaderParty.RemoveParty();
                AddHeroToPartyAction.Apply(leader, newLeaderParty);
            }
            else
            {
                leaderParty.Party.SetCustomOwner(newLeader);
                var partyList = leaderParty.Party.MemberRoster;
                partyList.RemoveTroop(newLeader.CharacterObject);
                partyList.AddToCounts(newLeader.CharacterObject, 1, true);
                var newPartyName = new TextObject("{=shL0WElC}{TROOP.NAME}'s Party");
                newPartyName.SetCharacterProperties("TROOP", newLeader.CharacterObject);
                leaderParty.SetCustomName(newPartyName);
            }

            leader.SetHasMet();

            newLeader.Clan.Influence = Math.Max(0, newLeader.Clan.Influence - 100);

            HeroFixHelper.FixHeroStats(newLeader);
            HeroFixHelper.FixEquipment(newLeader);

            newLeaderParty.Party.SetVisualAsDirty();//.Visuals.SetMapIconAsDirty();
            CampaignEventDispatcher.Instance.OnArmyOverlaySetDirty();

            newLeader = null;
            leader = null;
            CampaignEventDispatcher.Instance.RemoveListeners(this);
            Utils.Print($"[OnChangeClanLeader] changed over");
        }

        private bool ConditionChangeClanLeader()
        {
            return Hero.OneToOneConversationHero != null
                && Clan.PlayerClan != null
                && newLeader == null
                && Hero.OneToOneConversationHero.Clan == Clan.PlayerClan
                && Hero.MainHero == Clan.PlayerClan.Leader
                && Hero.MainHero.PartyBelongedTo != null
                && Hero.MainHero.PartyBelongedTo.Army == null
                && Hero.OneToOneConversationHero.PartyBelongedTo?.Army == null
                && Hero.OneToOneConversationHero.PartyBelongedTo == Hero.MainHero.PartyBelongedTo
                && (IsFamilyMember(Hero.MainHero, Hero.OneToOneConversationHero) || IsMarried(Hero.MainHero, Hero.OneToOneConversationHero) || IsBloodRelated(Hero.MainHero, Hero.OneToOneConversationHero));
        }

        private static bool IsFamilyMember(Hero familyHero, Hero hero)
        {
            if (familyHero.Clan.Lords == null
                || familyHero.Clan.Lords.IsEmpty())
            {
                return false;
            }

            return familyHero.Clan.Lords.Contains(hero);
        }

        private static bool IsBloodRelated(Hero hero1, Hero hero2)
        {
            return hero1.Siblings.Contains(hero2) || IsChildOf(hero1, hero2) || IsChildOf(hero2, hero1) || HaveSameParent(hero1, hero2);
        }

        private static bool IsChildOf(Hero hero1, Hero hero2)
        {
            foreach (Hero child in hero1.Children)
            {
                if (child == hero2) return true;

                if (IsChildOf(child, hero2)) return true;
            }
            return false;
        }

        private static bool IsMarried(Hero hero1, Hero hero2)
        {
            return (hero1.Spouse == hero2) || (hero2.Spouse == hero1);
        }

        private static bool HaveSameParent(Hero hero1, Hero hero2)
        {
            return (hero1.Father != null && (hero1.Father == hero2.Father)) || (hero1.Mother != null && (hero1.Mother == hero2.Mother));
        }
    }
}
