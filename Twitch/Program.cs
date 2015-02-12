
#region
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp.Common;
using SharpDX;
using LeagueSharp;
using SharpDX.Direct3D9;
using SharpDX.Windows;
using Color = System.Drawing.Color;

#endregion
namespace Twitch
{
    public static class Program
    {
        private static String ChampionName = "Twitch";
        public static Obj_AI_Hero Player;
        private static Menu menu;
        private static Orbwalking.Orbwalker orbwalker;
        private static Spell Q, W, E, Recall;

        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        static void Game_OnGameLoad(EventArgs args)
        {
            Player = ObjectManager.Player;
            if (Player.ChampionName != "Twitch") return;

            Q = new Spell(SpellSlot.Q);
            W = new Spell(SpellSlot.W, 950f);
            E = new Spell(SpellSlot.E, 1200f);
            Recall = new Spell(SpellSlot.Recall);

            W.SetSkillshot(0.25f, 120f, 1400f, false, SkillshotType.SkillshotCircle);

            menu = new Menu("Twitch", ChampionName, true);

            var ts = new Menu("TargetSelector", "Target Selector");
            TargetSelector.AddToMenu(ts);
            menu.AddSubMenu(ts);

            menu.AddSubMenu(new Menu("Combo Settings", "Combo"));
            menu.SubMenu("Combo").AddItem(new MenuItem("UseQCombo", "Use Q", true).SetValue(true));
            menu.SubMenu("Combo").AddItem(new MenuItem("UseECombo", "Use E", true).SetValue(true));
            menu.SubMenu("Combo")
                .AddItem(new MenuItem("ComboEStacks", "Use E if Stack ==", true).SetValue(new Slider(6, 1, 6)));

            menu.AddSubMenu(new Menu("Harass", "Harass"));
            menu.SubMenu("Harass").AddItem(new MenuItem("UseWharass", "Use W", true).SetValue(true));
            menu.SubMenu("Harass").AddItem(new MenuItem("UseEharass", "Use E", true).SetValue(true));
            menu.SubMenu("Harass")
                .AddItem(new MenuItem("harassUseEStack", "Use E if Stack ==", true).SetValue(new Slider(4, 1, 6)));
            menu.SubMenu("Harass")
                .AddItem(new MenuItem("harassMana", "Mana % >", true).SetValue(new Slider(50, 0, 100)));

            menu.AddSubMenu(new Menu("WaveClear", "WaveClear"));
            menu.SubMenu("WaveClear").AddItem(new MenuItem("waveclearUseW", "Use W", true).SetValue(true));
            menu.SubMenu("WaveClear").AddItem(new MenuItem("waveclearUseE", "Use E", true).SetValue(true));
            menu.SubMenu("WaveClear")
                .AddItem(new MenuItem("waveclearMana", "if Mana % >", true).SetValue(new Slider(60, 0, 100)));

            menu.AddSubMenu(new Menu("JungleClear", "JungleClear"));
            menu.SubMenu("JungleClear").AddItem(new MenuItem("jungleclearUseW", "Use W", true).SetValue(true));
            menu.SubMenu("JungleClear").AddItem(new MenuItem("jungleclearUseE", "Use E", true).SetValue(true));
            menu.SubMenu("JungleClear")
                .AddItem(new MenuItem("jungleclearMana", "if Mana % >", true).SetValue(new Slider(30, 0, 100)));

            menu.AddSubMenu(new Menu("Misc", "Misc"));
            menu.SubMenu("Misc").AddItem(new MenuItem("Killsteal", "Use Killsteal", true).SetValue(true));
            menu.SubMenu("Misc")
                .AddItem(
                    new MenuItem("stealthrecall", "Stealth Recall", true).SetValue(new KeyBind('T', KeyBindType.Press)));

            menu.AddSubMenu(new Menu("Drawings", "Drawings"));
            menu.SubMenu("Drawings")
                .AddItem(
                    new MenuItem("drawingAA", "AA Range", true).SetValue(new Circle(true, System.Drawing.Color.Blue)));
            menu.SubMenu("Drawings")
                .AddItem(new MenuItem("drawingW", "W Range", true).SetValue(new Circle(true, System.Drawing.Color.Aqua)));
            menu.SubMenu("Drawings")
                .AddItem(new MenuItem("drawingE", "E Range", true).SetValue(new Circle(false, System.Drawing.Color.Blue)));
            menu.SubMenu("Drawings").AddItem(new MenuItem("drawingQTimer", "Stealth Timer", true).SetValue(true));
            menu.SubMenu("Drawings").AddItem(new MenuItem("drawingRTimer", "R Timer", true).SetValue(true));
            menu.SubMenu("Drawings").AddItem(new MenuItem("drawingRLine", "R Pierce Line", true).SetValue(true));

            menu.AddToMainMenu();

            Game.OnGameUpdate += OnGameUpdate;
            Drawing.OnDraw += OnDraw;
        }

        private static void OnGameUpdate(EventArgs args)
        {
            if (Player.IsDead)
                return;

            if (orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
                Combo();

            if (orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
                Harass();

            if (orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear)
            {
                WaveClear();
                JungleClear();
            }
            Killsteal();
        }

        private static void OnDraw(EventArgs args)
        {
            if (Player.IsDead)
                return;
            var drawingAA = menu.Item("drawingAA", true).GetValue<Circle>();
            var drawingW = menu.Item("drawingW", true).GetValue<Circle>();
            var drawingE = menu.Item("drawingE", true).GetValue<Circle>();

            if (drawingAA.Active)
                Render.Circle.DrawCircle(Player.Position, Orbwalking.GetRealAutoAttackRange(Player), drawingAA.Color);

            if (W.IsReady() && drawingW.Active)
                Render.Circle.DrawCircle(Player.Position, W.Range, drawingW.Color);

            if (E.IsReady() && drawingE.Active)
                Render.Circle.DrawCircle(Player.Position, E.Range, drawingE.Color);

            if (menu.Item("drawingQTimer", true).GetValue<Boolean>())
            {
                foreach (var buff in Player.Buffs)
                {
                    if (buff.Name == "TwitchHideInShadows")
                    {
                        var targetpos = Drawing.WorldToScreen(Player.Position);
                        Drawing.DrawText(targetpos[0] - 10, targetpos[1], Color.Gold,
                            "" + (buff.EndTime - Game.ClockTime));
                    }
                }
            }
            if (menu.Item("stealthrecall", true).GetValue<KeyBind>().Active)
            {
                var targetpos = Drawing.WorldToScreen(Player.Position);

                if (Q.IsReady() && Recall.IsReady())
                {
                    Drawing.DrawText(targetpos[0] - 60, targetpos[1] - 50, System.Drawing.Color.Gold,
                        "Try Stealth recall");
                }
                else if (Player.HasBuff("TwitchHideInShadows") && Player.HasBuff("Recall"))
                    Drawing.DrawText(targetpos[0] - 60, targetpos[1] - 50, System.Drawing.Color.Gold,
                        "Stealth Recall Activated");
                else if (!Player.HasBuff("recall"))
                    Drawing.DrawText(targetpos[0] - 60, targetpos[1] - 50, System.Drawing.Color.Gold, "Q is not ready");
            }
            if (menu.Item("drawingRLine", true).GetValue<Boolean>())
            {
                if (Player.HasBuff("TwitchFullAutomatic", true))
                {
                    var aatarget = TargetSelector.GetTarget(Orbwalking.GetRealAutoAttackRange(Player),
                        TargetSelector.DamageType.Physical);

                    if (aatarget != null)
                    {
                        var from = Drawing.WorldToScreen(Player.Position);

                        var dis = (Orbwalking.GetRealAutoAttackRange(Player) + 300) - Player.Distance(aatarget, false);

                        var to =
                            Drawing.WorldToScreen(dis > 0
                                ? aatarget.ServerPosition.Extend(Player.Position, -dis)
                                : aatarget.ServerPosition);
                        Drawing.DrawLine(from[0], from[1], to[0], to[1], 10, System.Drawing.Color.Blue);
                    }
                }
            }
            if (menu.Item("drawingRTimer", true).GetValue<Boolean>())
            {
                foreach (var buff in Player.Buffs)
                {
                    if (buff.Name == "TwitchFullAutomatic")
                    {
                        var targetpos = Drawing.WorldToScreen(Player.Position);
                        Drawing.DrawText(targetpos[0] - 10, targetpos[1], System.Drawing.Color.Gold,
                            "" + (buff.EndTime - Game.ClockTime));
                        break;
                    }
                }
            }
        }

        private static void Killsteal()
        {
            if (!menu.Item("Killsteal", true).GetValue<Boolean>())
                return;

            foreach (
                Obj_AI_Hero target in
                    ObjectManager.Get<Obj_AI_Hero>()
                        .Where(
                            x =>
                                x.IsValidTarget(E.Range) && x.IsEnemy && !x.HasBuffOfType(BuffType.Invulnerability) &&
                                !x.HasBuffOfType(BuffType.SpellShield)))
            {
                if (target != null)
                {
                    if (E.CanCast(target) && (target.Health + target.HPRegenRate) <= E.GetDamage(target))
                    {
                        E.Cast();
                        break;
                    }
                }
            }
        }

        private static void Combo()
        {
            if (!Orbwalking.CanMove(1))
                return;

            if (menu.Item("UseWCombo", true).GetValue<Boolean>())
            {
                var Wtarget = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.True, false);

                if (W.CanCast(Wtarget) && W.GetPrediction(Wtarget).Hitchance >= HitChance.VeryHigh)
                    W.Cast(Wtarget);
            }
            if (menu.Item("UseECombo", true).GetValue<Boolean>())
            {
                var Etarget = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical, true);

                if (E.CanCast(Etarget))
                {
                    foreach (var buff in Etarget.Buffs)
                    {
                        if (buff.Name == "twitchdeadlyvenom")
                        {
                            if (buff.Count >= menu.Item("ComboUseEStack", true).GetValue<Slider>().Value)
                            {
                                E.Cast();
                                break;
                            }
                        }
                    }
                }
            }
        }

        private static void Harass()
        {
            if (!Orbwalking.CanMove(1) ||
                !(Player.ManaPercentage() > menu.Item("harassMana", true).GetValue<Slider>().Value))
                return;

            if (menu.Item("UseWHarass", true).GetValue<Boolean>())
            {
                var Wtarget = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.True, false);

                if (W.CanCast(Wtarget) && W.GetPrediction(Wtarget).Hitchance >= HitChance.VeryHigh)
                    W.Cast(Wtarget);
            }

            if (menu.Item("UseEHarass", true).GetValue<Boolean>())
            {
                var Etarget = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical, true);

                if (E.CanCast(Etarget))
                {
                    foreach (var buff in Etarget.Buffs)
                    {
                        if (buff.Name == "twitchdeadlyvenom")
                        {
                            if (buff.Count >= menu.Item("harassUseEStack", true).GetValue<Slider>().Value)
                            {
                                E.Cast();
                                break;
                            }
                        }
                    }
                }
            }
        }

        private static void WaveClear()
        {
            if (!Orbwalking.CanMove(1) ||
                !(Player.ManaPercentage() > menu.Item("waveclearMana", true).GetValue<Slider>().Value))
                return;

            var Minions = MinionManager.GetMinions(Player.ServerPosition, E.Range, MinionTypes.All, MinionTeam.Enemy);

            if (Minions.Count <= 0)
                return;

            if (W.IsReady() && menu.Item("waveclearUseW", true).GetValue<Boolean>())
            {
                var Farmloc = W.GetCircularFarmLocation(Minions);

                if (Farmloc.MinionsHit >= 3)
                    W.Cast(Farmloc.Position);
            }

            if (E.IsReady() && menu.Item("laneclearUseE", true).GetValue<Boolean>())
            {
                var killcount = 0;
                foreach (var Minion in Minions)
                {
                    foreach (var buff in Minion.Buffs)
                    {
                        if (buff.Name == "twitchdeadlyvenom")
                        {
                            if (buff.Count >= 6)
                            {
                                E.Cast();
                                break;
                            }
                        }
                    }

                    if (Minion.Health <= E.GetDamage(Minion))
                        killcount++;
                }

                if (killcount >= 2)
                    E.Cast();
            }
        }

        private static void JungleClear()
        {
            if (!Orbwalking.CanMove(1) ||
                !(Player.ManaPercentage() > menu.Item("jungleclearMana", true).GetValue<Slider>().Value))
                return;

            var Mobs = MinionManager.GetMinions(Player.ServerPosition, Orbwalking.GetRealAutoAttackRange(Player) + 100,
                MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);

            if (Mobs.Count < 1)
                return;

            if (W.CanCast(Mobs[0]) && menu.Item("jungleclearUseW", true).GetValue<Boolean>())
                W.Cast(Mobs[0].Position);

            if (E.CanCast(Mobs[0]) && menu.Item("jungleclearUseE", true).GetValue<Boolean>())
            {
                foreach (var buff in Mobs[0].Buffs)
                {
                    if (buff.Name == "twitchdeadlyvenom")
                    {
                        if (buff.Count >= 6)
                        {
                            E.Cast();
                            break;
                        }
                    }
                }

                if ((Mobs[0].Health + Mobs[0].HPRegenRate) <= E.GetDamage(Mobs[0]))
                    E.Cast();
            }
        }
    }
}