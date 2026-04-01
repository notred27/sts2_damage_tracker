using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Platform;
namespace DamageTracker.Patches;



[HarmonyPatch(typeof(NGame), nameof(NGame._Ready))]
public class DamageTrackerPatch
{

    public static Godot.VBoxContainer container;
    public static Dictionary<ulong, Godot.Label> DamageLabels = new Dictionary<ulong, Label>();
    public static Dictionary<ulong, PlayerTrackerInfo>  playerTrackers = new Dictionary<ulong, PlayerTrackerInfo>();


    static void Postfix(NGame __instance)
    {
        SetUpTable(__instance);


        Action<DamageEvent> handler = null;
        handler = (damageEvent) =>
        {
            ulong id = damageEvent.DealerNetId;


            // Create label if first time seeing player
            if (!playerTrackers.ContainsKey(id))
            {
                playerTrackers[id] = new PlayerTrackerInfo(id, PlatformUtil.GetPlayerName(PlatformType.Steam, id), damageEvent.Dealer.Character.IconTexture);
                container.AddChild(playerTrackers[id].Display);
                GD.Print($"[Tracker] Created label + icon for {id}");
            }

            playerTrackers[id].UpdateDamage(damageEvent);
        };

        DamageTrackerEvent.OnDamageDealt += handler;

        // Cleanup when game node exits
        container.TreeExiting += () =>
        {
            DamageTrackerEvent.OnDamageDealt -= handler;
        };

        GD.Print("[DamageTrackerPatch] Multiplayer tracker initialized (inline)");
    }


    /// <summary>
    /// Set up the headers for the damage tracker table. We will add player rows dynamically as we see damage events from new players.
    /// </summary>
    /// <param name="__instance"></param>
    private static void SetUpTable(NGame __instance)
    {
        // Base container for the table
        container = new VBoxContainer();
        container.OffsetLeft = 20;
        container.OffsetTop = 700;
        container.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        container.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        container.Visible = false;

        __instance.AddChild(container);

        // Header row
        var headerRow = new HBoxContainer();
        headerRow.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        headerRow.AddThemeConstantOverride("separation", 10);

        // Player column (left-aligned)
        var playerNameHeader = new Label();
        playerNameHeader.Text = "Player";
        playerNameHeader.HorizontalAlignment = HorizontalAlignment.Left;
        playerNameHeader.SizeFlagsHorizontal = Control.SizeFlags.Fill;
        playerNameHeader.CustomMinimumSize = new Vector2(200, 0);
        headerRow.AddChild(playerNameHeader);

        // Damage column (centered)
        var damageDealtHeader = new Label();
        damageDealtHeader.Text = "Damage";
        damageDealtHeader.HorizontalAlignment = HorizontalAlignment.Center;
        damageDealtHeader.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        headerRow.AddChild(damageDealtHeader);

        // Kills column (centered)
        var killsHeader = new Label();
        killsHeader.Text = "Kills";
        killsHeader.HorizontalAlignment = HorizontalAlignment.Center;
        killsHeader.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        headerRow.AddChild(killsHeader);

        container.AddChild(headerRow);
    }




    public class PlayerTrackerInfo
    {
        public HBoxContainer Display;

        private ulong PlayerId;

        private string PlayerName;
        private Texture IconTexture;

        private DamageTrackerDisplayEntry DamageEntry;

        private int Kills;
        private Godot.Label KillLabel;


        public PlayerTrackerInfo(ulong playerId, string playerName, Texture iconTexture)
        {
            PlayerId = playerId;
            PlayerName = playerName;
            IconTexture = iconTexture;
            DamageEntry = new DamageTrackerDisplayEntry();
            Kills = 0;

            Initialize();
        }

        private void Initialize()
        {
            Display = new HBoxContainer();
            Display.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            Display.AddThemeConstantOverride("separation", 10);

            // Left col (Icon + Name)
            var playerNameRow = new HBoxContainer();
            playerNameRow.SizeFlagsHorizontal = Control.SizeFlags.Fill;
            playerNameRow.CustomMinimumSize = new Vector2(200, 0);
            playerNameRow.AddThemeConstantOverride("separation", 10);

            // Player icon
            var iconRect = new TextureRect();
            iconRect.Texture = (Texture2D)IconTexture;
            iconRect.StretchMode = TextureRect.StretchModeEnum.Scale;
            iconRect.CustomMinimumSize = new Vector2(10, 10);
            iconRect.SizeFlagsHorizontal = Control.SizeFlags.Fill;
            iconRect.SizeFlagsVertical = Control.SizeFlags.Fill;
            playerNameRow.AddChild(iconRect);

            // Player name label
            var nameLabel = new Label();
            nameLabel.Text = PlayerName;
            nameLabel.HorizontalAlignment = HorizontalAlignment.Left;
            playerNameRow.AddChild(nameLabel);

            // Kills column
            KillLabel = new Label();
            KillLabel.Text = "0";
            KillLabel.HorizontalAlignment = HorizontalAlignment.Center;
            KillLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;


            Display.AddChild(playerNameRow);
            Display.AddChild(DamageEntry.Label);
            Display.AddChild(KillLabel);
        }

        public void UpdateDamage(DamageEvent damageEvent)
        {
            DamageEntry.UpdateDamage(damageEvent);

            if (damageEvent.Kill)
                Kills++;
                KillLabel.Text = Kills.ToString();
        }
    }



    public class DamageTrackerDisplayEntry
    {
        public Godot.Label Label;
        private int TotalDamage;
        private int OverkillDamage;


        public DamageTrackerDisplayEntry()
        {
            Label = new Godot.Label();
            TotalDamage = 0;
            OverkillDamage = 0;
            UpdateLabel();
        }

        public void UpdateDamage(DamageEvent damageEvent)
        {
            TotalDamage += damageEvent.Amount;
            OverkillDamage += damageEvent.Overkill;
            UpdateLabel();
        }

        private void UpdateLabel()
        {
            Label.Text = $"{TotalDamage} (+{OverkillDamage})";
        }
    }
}



[HarmonyPatch(typeof(NGame), nameof(NGame._Input))]
public class TabInputPatch
{
    static void Postfix(Godot.InputEvent inputEvent)
    {
        if (inputEvent is Godot.InputEventKey key &&
            key.Pressed &&
            !key.Echo &&
            key.Keycode == Godot.Key.Tab)
        {
            GD.Print("TAB pressed!");

            ToggleTracker();
        }
    }

    static void ToggleTracker()
    {
        if (DamageTrackerPatch.container != null)
        {
            DamageTrackerPatch.container.Visible = !DamageTrackerPatch.container.Visible;
        }
    }
}


[HarmonyPatch(typeof(CreatureAttackedEntry))]
[HarmonyPatch(MethodType.Constructor)]
[HarmonyPatch(new Type[] {
    typeof(Creature),
    typeof(IReadOnlyList<DamageResult>),
    typeof(int),
    typeof(CombatSide),
    typeof(CombatHistory),
})]
public static class CreatureAttackedEntryCtorPatch
{
    public static void Postfix(
        CreatureAttackedEntry __instance,
        Creature attacker,
        IReadOnlyList<DamageResult> damageResults,
        int roundNumber,
        CombatSide currentSide,
        CombatHistory history)
    {
        GD.Print($"     === Attacker: {attacker}");

        foreach (var damageResult in damageResults)
        {
            GD.Print($"     === Target was killed: {damageResult.WasTargetKilled} (overkill = {damageResult.OverkillDamage})");
            // Prob don't need overkill here, but will test to be sure
            if ((damageResult.TotalDamage + damageResult.OverkillDamage) > 0 && (attacker.IsPlayer || attacker.IsPet))
            {
                DamageTrackerEvent.EmitDamage(damageResult.TotalDamage, damageResult.OverkillDamage, attacker, damageResult.WasTargetKilled);
            }
        }

    }
}


// Doesn't account for orbs :(


//public void DamageReceived(CombatState combatState, Creature receiver, Creature? dealer, DamageResult result, CardModel? cardSource)
//    {
//        Add(new DamageReceivedEntry(result, receiver, dealer, cardSource, combatState.RoundNumber, combatState.CurrentSide, this));
//    }
//[HarmonyPatch(typeof(CombatHistory), nameof(CombatHistory.DamageReceived))]

//public static class CombatHistoryDamageReceivedPatch
//{
//    public static void Postfix(
//        CombatHistory __instance,
//        CombatState combatState,
//        Creature receiver,
//        Creature? dealer,
//        DamageResult result,
//        CardModel? cardSource)
//    {
//        GD.Print($"=====[CombatHistoryDamageReceivedPatch] DamageReceived called. Result: {result.TotalDamage} damage, overkill: {result.OverkillDamage}, target killed: {result.WasTargetKilled}");
//        int damageDealt = result.TotalDamage + result.OverkillDamage;
//        if (damageDealt > 0 && dealer != null && (dealer.IsPlayer || dealer.IsPet))
//        {
//            GD.Print($"=====[CombatHistoryDamageReceivedPatch] Emitting damage event for dealer {dealer.Name} (NetId: {(dealer.IsPet ? dealer.PetOwner?.NetId : dealer.Player?.NetId)}), damage: {damageDealt}, kill: {result.WasTargetKilled}");
//            //DamageTrackerEvent.EmitDamage(damageDealt, dealer, result.WasTargetKilled);
//        }
//    }
//}



//EndCombatInternal
//[HarmonyPatch(typeof(CombatManager), nameof(CombatManager.EndCombatInternal))]
//public static class CombatManagerEndingPatch
// {
//    public static bool Prefix(CombatManager __instance)
//    {
//        GD.Print("[CombatManager.EndCombatInternal] Combat ended, resetting damage tracker.");
//        // Reset or clear any necessary data here if needed


//        //foreach (var entry in __instance.History.Entries)
//        //{
//        //    // do something with entry
//        //    GD.Print(entry.HumanReadableString);
//        //    GD.Print(entry);

//        //}
//        return true; 
//    }
//}