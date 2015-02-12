using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

using LeagueSharp;
using LeagueSharp.Common;

namespace Ezreal
{
    public static class Program
    {
        private const string ChampionName = "Ezreal";
        public static Orbwalking.Orbwalker Orbwalker;
        public static Spell Q, W, E, R;
        public static SpellSlot IgniteSlot;
        private static Obj_AI_Hero Player;

        public static Menu menu;

        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        static void Game_OnGameLoad(EventArgs args)
        {
            Player = ObjectManager.Player;
            if (Player.ChampionName != "Ezreal") return;

            Q = new Spell(SpellSlot.Q, 1150f);
            W = new Spell(SpellSlot.W, 1000f);
            E = new Spell(SpellSlot.E, 475f);
            R = new Spell(SpellSlot.R, 2500f);

            Q.SetSkillshot(0.25f, 60f, 2000f, true, SkillshotType.SkillshotLine);
            W.SetSkillshot(0.25f, 80f, 1600f, false, SkillshotType.SkillshotLine);
            R.SetSkillshot(1f, 160f, 2000f, false, SkillshotType.SkillshotLine);

            IgniteSlot = ObjectManager.Player.GetSpellSlot("SummonerDot");

            menu = new Menu("Ezreal!", ChampionName, true);

            var ts = new Menu("TargetSelector", "Target Selector");
            TargetSelector.AddToMenu(ts);
            menu.AddSubMenu(ts);

            var orbmenu = menu.AddSubMenu(new Menu("Obwalker", "Orwalker"));
            new Orbwalking.Orbwalker(orbmenu);

            menu.AddSubMenu(new Menu("Combo Settings", "combo"));
            menu.SubMenu("combo").AddItem(new MenuItem("UseQCombo", "Use Q")).SetValue(true);
            menu.SubMenu("combo").AddItem(new MenuItem("UseWCombo", "Use W")).SetValue(true);
            menu.SubMenu("Combo").AddItem(new MenuItem("useRCombo", "Use R", true).SetValue(true));
            menu.SubMenu("combo")
                .AddItem(new MenuItem("ActiveCombo", "Combo!").SetValue(new KeyBind(32, KeyBindType.Press)));

            menu.AddSubMenu(new Menu("Wave Clear", "Wave"));
            menu.SubMenu("Wave").AddItem(new MenuItem("UseQWave", "Use Q")).SetValue(true);
            menu.SubMenu("Wave")
                .AddItem(
                    new MenuItem("ActiveWave", "WaveClear Key").SetValue(new KeyBind("V".ToCharArray()[0],
                        KeyBindType.Press)));

            menu.AddSubMenu(new Menu("Misc Settings", "Misc"));
            menu.SubMenu("Misc").AddItem(new MenuItem("usePackets", "Use Packets to Cast Spells").SetValue(false));
            menu.SubMenu("Misc").AddItem(new MenuItem("killsteal", "Use Killsteal", true).SetValue(true));
            menu.SubMenu("Misc").AddItem(new MenuItem("jump", "Jump to mouse", true).SetValue(new KeyBind('G', KeyBindType.Press)));

            menu.AddSubMenu(new Menu("Drawings", "Drawings"));
            menu.SubMenu("Drawings").AddItem(new MenuItem("DrawQ", "Draw Q")).SetValue(true);
            menu.SubMenu("Drawings").AddItem(new MenuItem("DrawW", "Draw W")).SetValue(true);
            menu.SubMenu("Drawings").AddItem(new MenuItem("DrawE", "Draw E")).SetValue(true);
            menu.SubMenu("Drawings").AddItem(new MenuItem("CircleLag", "Lag Free Circles").SetValue(true));
            menu.SubMenu("Drawings")
                .AddItem(new MenuItem("CircleQuality", "Circles Quality").SetValue(new Slider(100, 100, 10)));
            menu.SubMenu("Drawings")
                .AddItem(new MenuItem("CircleThickness", "Circles Thickness").SetValue(new Slider(1, 10, 1)));

            menu.AddToMainMenu();

            Game.PrintChat("Script" + ChampionName + "Ijected");

            Game.OnGameUpdate += OnGameUpdate;
            Drawing.OnDraw += OnDraw;
        }

        private static void OnGameUpdate(EventArgs args)
        {
            var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
            var comboKey = menu.Item("ActiveCombo").GetValue<KeyBind>().Active;
            var farmKey = menu.Item("ActiveWave").GetValue<KeyBind>().Active;

            if (comboKey && target != null)
                Combo(target);
            else
            {
                if (farmKey)
                    WaveClear();
            }
            if (menu.Item("jump", true).GetValue<KeyBind>().Active)
            {
                if (E.IsReady())
                {
                    Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
                    E.Cast(Game.CursorPos);
                }
            }
        }
        private static void Killsteal()
        {
            if (!menu.Item("killsteal", true).GetValue<Boolean>())
                return;

            foreach (Obj_AI_Hero target in ObjectManager.Get<Obj_AI_Hero>().Where(x => x.IsValidTarget(R.Range) && x.IsEnemy && !x.HasBuffOfType(BuffType.Invulnerability) && !x.HasBuffOfType(BuffType.SpellShield)))
            {
                if (target != null)
                {
                    if (R.CanCast(target) && (target.Health + target.HPRegenRate * 2) <= R.GetDamage(target))
                        R.Cast(target);
                }
            }
        }
        private static void Combo(Obj_AI_Base target)
        {
            if (!target.IsValidTarget()) return;

            if (Q.IsReady())
                castSkillshot(Q, Q.Range, TargetSelector.DamageType.Magical, HitChance.High);
            if (W.IsReady())
                castSkillshot(W, W.Range, TargetSelector.DamageType.Magical, HitChance.High);
            if (menu.Item("useRCombo", true).GetValue<Boolean>())
            {
                var Rtarget = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Magical, true);

                if (R.CanCast(Rtarget) && R.GetPrediction(Rtarget).Hitchance >= HitChance.VeryHigh)
                    R.CastIfWillHit(Rtarget, 2);
            }
        }

        private static void WaveClear()
        {
            var useQ = menu.Item("UseQWave").GetValue<bool>();

            List<Obj_AI_Base> rangedMinions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition,
                Q.Range + Q.Width + 30, MinionTypes.Ranged);
            List<Obj_AI_Base> allMinions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition,
                Q.Range + Q.Width + 30, MinionTypes.All);

            if (useQ && Q.IsReady())
            {
                foreach (Obj_AI_Base minion in allMinions)
                {
                    if (minion.Health <= ObjectManager.Player.GetSpellDamage(minion, SpellSlot.Q) &&
                        minion.Health > ObjectManager.Player.GetAutoAttackDamage(minion))
                    {
                        Q.Cast(minion);
                    }
                }
            }
        }

        private static void OnDraw(EventArgs args)
        {
            if (menu.Item("CircleLag").GetValue<bool>())
            {
                if (menu.Item("DrawQ").GetValue<bool>())
                {
                    Utility.DrawCircle(ObjectManager.Player.Position, Q.Range, Color.White,
                        menu.Item("CircleThickness").GetValue<Slider>().Value,
                        menu.Item("CircleQuality").GetValue<Slider>().Value);
                }
                if (menu.Item("DrawW").GetValue<bool>())
                {
                    Utility.DrawCircle(ObjectManager.Player.Position, W.Range, Color.White,
                        menu.Item("CircleThickness").GetValue<Slider>().Value,
                        menu.Item("CircleQuality").GetValue<Slider>().Value);
                }
                if (menu.Item("DrawR").GetValue<bool>())
                {
                    Utility.DrawCircle(ObjectManager.Player.Position, R.Range, Color.White,
                        menu.Item("CircleThickness").GetValue<Slider>().Value,
                        menu.Item("CircleQuality").GetValue<Slider>().Value);
                }
            }
            else
            {
                if (menu.Item("DrawQ").GetValue<bool>())
                {
                    Drawing.DrawCircle(ObjectManager.Player.Position, Q.Range, Color.White);
                }
                if (menu.Item("DrawW").GetValue<bool>())
                {
                    Drawing.DrawCircle(ObjectManager.Player.Position, W.Range, Color.Green);
                }
                if (menu.Item("DrawR").GetValue<bool>())
                {
                    Drawing.DrawCircle(ObjectManager.Player.Position, R.Range, Color.Purple);
                }
            }
        }
        public static void castSkillshot(Spell spell, float range, LeagueSharp.Common.TargetSelector.DamageType type, HitChance hitChance)
        {
            var target = LeagueSharp.Common.TargetSelector.GetTarget(range, type);
            if (target == null || !spell.IsReady())
                return;
            spell.UpdateSourcePosition();
            if (spell.GetPrediction(target).Hitchance >= hitChance)
                spell.Cast(target, packets());
        }

        public static bool packets()
        {
            return menu.Item("usePackets").GetValue<bool>();
        }

    }
}