using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;

namespace awp_only
{
    public class awp_only : BasePlugin
    {
        public override string ModuleAuthor => "GSM-RO (modified)";
        public override string ModuleName => "Awp_only";
        public override string ModuleVersion => "1.0.1";
        public override string ModuleDescription => "AWP Only (весь матч)";

        private static HashSet<string> AllowedWeapons = new();

        public override void Load(bool hotReload)
        {
            LoadConfig();
            RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
            RegisterListener<Listeners.OnTick>(OnTick);
        }

        private void LoadConfig()
        {
            var path = Path.Combine(ModuleDirectory, "config.cfg");
            if (!File.Exists(path))
            {
                File.WriteAllText(path,
                    "allowed_weapons = weapon_awp, weapon_knife\n");
            }

            var lines = File.ReadAllLines(path);
            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();

                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                    continue;

                var parts = line.Split('=', 2);
                if (parts.Length < 2)
                    continue;

                var key = parts[0].Trim();
                var value = parts[1].Trim();

                if (key == "allowed_weapons")
                {
                    AllowedWeapons = value
                        .Split(',')
                        .Select(w => w.Trim())
                        .Where(w => !string.IsNullOrEmpty(w))
                        .ToHashSet();
                }
            }

            Logger.LogInformation(
                $"[Awp_only] Allowed weapons: {string.Join(", ", AllowedWeapons)}"
            );
        }

        private static void OnTick()
        {
            foreach (var player in Utilities.GetPlayers())
            {
                var pawn = player.PlayerPawn?.Value;
                if (pawn == null || (LifeState_t)pawn.LifeState != LifeState_t.LIFE_ALIVE)
                    continue;

                var weaponServices = player.PlayerPawn?.Value?.WeaponServices;
                if (weaponServices?.MyWeapons == null)
                    continue;

                foreach (var weapon in weaponServices.MyWeapons)
                {
                    if (weapon?.IsValid != true || weapon.Value == null)
                        continue;

                    var name = weapon.Value.DesignerName;
                    if (!AllowedWeapons.Contains(name))
                    {
                        weapon.Value.AddEntityIOEvent(
                            "Kill",
                            weapon.Value,
                            null,
                            "",
                            0.0f
                        );
                    }
                }
            }
        }

        private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
        {
            var player = @event.Userid;
            if (player == null || !player.IsValid)
                return HookResult.Continue;

            Server.NextFrame(() =>
            {
                var pawn = player.PlayerPawn?.Value;
                if (pawn == null)
                    return;

                if (!pawn.LifeState.Equals(LifeState_t.LIFE_ALIVE))
                    return;

                RemoveAllWeapons(player);

                foreach (var weapon in AllowedWeapons)
                {
                    player.GiveNamedItem(weapon);
                }
            });

            return HookResult.Continue;
        }

        private static void RemoveAllWeapons(CCSPlayerController player)
        {
            var weaponServices = player.PlayerPawn?.Value?.WeaponServices;
            if (weaponServices?.MyWeapons == null)
                return;

            foreach (var weapon in weaponServices.MyWeapons)
            {
                if (weapon?.IsValid != true || weapon.Value == null)
                    continue;

                var name = weapon.Value.DesignerName;
                if (AllowedWeapons.Contains(name))
                    continue;

                weapon.Value.AddEntityIOEvent(
                    "Kill",
                    weapon.Value,
                    null,
                    "",
                    0.1f
                );
            }
        }
    }
}
