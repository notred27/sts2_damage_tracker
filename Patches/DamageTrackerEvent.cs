using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Players;

namespace DamageTracker.Patches
{

    public struct DamageEvent
    {
        public ulong DealerNetId;
        public int Amount;
        public int Overkill;
        public bool Kill;
        public Player Dealer;
    }


    public static class DamageTrackerEvent
    {
        public static event Action<DamageEvent> OnDamageDealt;

        public static void EmitDamage(int amount, int overkill, Creature dealer, bool kill)
        {
            if (dealer.IsPet)
            {
                GD.Print($"[DamageTrackerEvent] Damage dealt by pet {dealer.Name} (owner: {dealer.PetOwner?.NetId}): {amount}");
                OnDamageDealt?.Invoke(new DamageEvent { Amount = amount, Overkill = overkill, DealerNetId = (ulong)(dealer.PetOwner?.NetId), Kill = kill, Dealer = dealer.PetOwner });
                return;
            }

            GD.Print($"[DamageTrackerEvent] Damage dealt by {dealer.Player?.NetId}: {amount}");
            OnDamageDealt?.Invoke(new DamageEvent { Amount = amount, Overkill = overkill, DealerNetId = (ulong)(dealer.Player?.NetId), Kill = kill, Dealer = (Player)dealer.Player });
        }
    }
}
