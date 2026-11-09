using System;
using System.Collections.Generic;

#if UNITY_5_3_OR_NEWER
using UnityEngine;
#endif

// This file contains the game rules and a minimal Unity OnGUI front end.
// Copy it into Unity's Assets folder, attach CardBattleUnityDemo to a GameObject,
// and press Play to try the five-battle run.
public enum PlayerAction { Attack, Dart, Defend, Ultimate }
public enum UpgradeFamily { None, Poison, Bleed, Power, Mana, Defense, Precision }
public enum EnemyKind { Goblin, Orc, Ghost, Slime, GoblinKing, Troll, OgreChief, RuinsGuardian }
public enum BattleMove { Attack, Dart, Defend, Ultimate, Charge, Heavy, Drain, Command, Stunned }
public enum UpgradeId
{
    Weapon, PoisonCoating, PoisonMastery, BleedEdge, BleedMastery,
    UltimatePower, ManaFlow, ManaReserve, CounterStance,
    DartPower, DartEfficiency, DefenseTraining
}

public sealed class UpgradeOffer
{
    public UpgradeId Id { get; private set; }
    public string Label { get; private set; }

    public UpgradeOffer(UpgradeId id, string label)
    {
        Id = id;
        Label = label;
    }
}

public sealed class CardBattleGame
{
    private sealed class PlayerState
    {
        public int Hp = 100;
        public int Mana = 15;
        public int AttackDamage = 15;
        public int AttackManaGain = 5;
        public int DartDamage = 5;
        public int DartCost = 5;
        public int DefenseReduction = 30;
        public int UltimateDamage = 40;
        public bool RetainMana;
        public int PoisonCoating;
        public int PoisonMastery;
        public int BleedEdge;
        public int BleedMastery;
        public int CounterStance;
        public readonly int[] UpgradeLevels = new int[12];
        public readonly int[] BuildCounts = new int[7];
        public readonly int[] ActionCounts = new int[4];
    }

    private sealed class EnemyState
    {
        public EnemyKind Kind;
        public string Name;
        public int Hp;
        public int MaxHp;
        public int Mana;
        public int AttackDamage;
        public int AttackManaGain;
        public int DartDamage;
        public int DartCost;
        public int DefenseReduction = 25;
        public int Turn;
        public int DrainsUsed;
        public int SlimeHeals;
        public int Rage;
        public int PoisonStacks;
        public int PoisonTurns;
        public int BleedStacks;
        public int BleedTurns;
        public bool ChargePending;
        public bool StunnedNext;
        public bool CommandPending;
        public bool HasAdaptTarget;
        public PlayerAction AdaptTarget = PlayerAction.Attack;
    }

    private sealed class Intent
    {
        public BattleMove Move;
        public int Damage;
        public int ExtraDamage;
        public bool Heavy;

        public Intent(BattleMove move, int damage, int extraDamage, bool heavy)
        {
            Move = move;
            Damage = damage;
            ExtraDamage = extraDamage;
            Heavy = heavy;
        }
    }

    private sealed class UpgradeDefinition
    {
        public UpgradeId Id;
        public string Label;
        public UpgradeFamily Family;
        public int MaxLevel;
        public UpgradeId Prerequisite;
        public bool HasPrerequisite;

        public UpgradeDefinition(UpgradeId id, string label, UpgradeFamily family, int maxLevel,
            UpgradeId prerequisite, bool hasPrerequisite)
        {
            Id = id;
            Label = label;
            Family = family;
            MaxLevel = maxLevel;
            Prerequisite = prerequisite;
            HasPrerequisite = hasPrerequisite;
        }
    }

    private static readonly UpgradeDefinition[] UpgradeDefinitions =
    {
        new UpgradeDefinition(UpgradeId.Weapon, "磨利武器：普攻傷害 +3", UpgradeFamily.Power, 3, UpgradeId.Weapon, false),
        new UpgradeDefinition(UpgradeId.PoisonCoating, "毒刃：暗器命中施加中毒", UpgradeFamily.Poison, 1, UpgradeId.PoisonCoating, false),
        new UpgradeDefinition(UpgradeId.PoisonMastery, "毒液增幅：中毒每回合總傷害 +1", UpgradeFamily.Poison, 2, UpgradeId.PoisonCoating, true),
        new UpgradeDefinition(UpgradeId.BleedEdge, "裂傷：普攻命中施加流血", UpgradeFamily.Bleed, 1, UpgradeId.BleedEdge, false),
        new UpgradeDefinition(UpgradeId.BleedMastery, "傷口擴大：流血每回合總傷害 +1", UpgradeFamily.Bleed, 2, UpgradeId.BleedEdge, true),
        new UpgradeDefinition(UpgradeId.UltimatePower, "蓄力爆發：大招傷害 +5", UpgradeFamily.Power, 2, UpgradeId.UltimatePower, false),
        new UpgradeDefinition(UpgradeId.ManaFlow, "魔力共鳴：普攻額外獲得 2 魔力", UpgradeFamily.Mana, 2, UpgradeId.ManaFlow, false),
        new UpgradeDefinition(UpgradeId.ManaReserve, "魔力蓄存：戰鬥間保留剩餘魔力", UpgradeFamily.Mana, 1, UpgradeId.ManaReserve, false),
        new UpgradeDefinition(UpgradeId.CounterStance, "反擊架勢：防禦擋到攻擊時反擊 5 傷害", UpgradeFamily.Defense, 2, UpgradeId.CounterStance, false),
        new UpgradeDefinition(UpgradeId.DartPower, "精準暗器：暗器傷害 +2", UpgradeFamily.Precision, 2, UpgradeId.DartPower, false),
        new UpgradeDefinition(UpgradeId.DartEfficiency, "節省暗器：暗器消耗魔力 −1", UpgradeFamily.Precision, 2, UpgradeId.DartEfficiency, false),
        new UpgradeDefinition(UpgradeId.DefenseTraining, "防禦訓練：一般防禦減傷 +5%", UpgradeFamily.Defense, 2, UpgradeId.DefenseTraining, false)
    };

    private readonly System.Random random = new System.Random();
    private readonly PlayerState player = new PlayerState();
    private readonly List<string> log = new List<string>();
    private readonly List<UpgradeOffer> upgradeOffers = new List<UpgradeOffer>();
    private EnemyState enemy;
    private Intent currentIntent;

    public int Stage { get; private set; }
    public bool IsResting { get; private set; }
    public bool IsGameOver { get; private set; }
    public bool HasWon { get; private set; }
    public int PlayerHp { get { return player.Hp; } }
    public int PlayerMana { get { return player.Mana; } }
    public int PlayerMaxHp { get { return 100; } }
    public int PlayerMaxMana { get { return 95; } }
    public int PlayerAttackDamage { get { return player.AttackDamage; } }
    public int PlayerAttackManaGain { get { return player.AttackManaGain; } }
    public int PlayerDartDamage { get { return player.DartDamage; } }
    public int PlayerDartCost { get { return player.DartCost; } }
    public int PlayerUltimateDamage { get { return player.UltimateDamage; } }
    public int PlayerDefenseReduction { get { return player.DefenseReduction; } }
    public string EnemyName { get { return enemy == null ? "" : enemy.Name; } }
    public int EnemyHp { get { return enemy == null ? 0 : enemy.Hp; } }
    public int EnemyMaxHp { get { return enemy == null ? 0 : enemy.MaxHp; } }
    public IList<string> Log { get { return log.AsReadOnly(); } }
    public IList<UpgradeOffer> UpgradeOffers { get { return upgradeOffers.AsReadOnly(); } }
    public string CurrentIntentText { get { return GetIntentText(); } }
    public string BuildText { get { return DescribeBuild(); } }

    public void StartNewRun()
    {
        Stage = 1;
        IsResting = false;
        IsGameOver = false;
        HasWon = false;
        enemy = null;
        currentIntent = null;
        log.Clear();
        upgradeOffers.Clear();
        player.Hp = 100;
        player.Mana = 15;
        player.AttackDamage = 15;
        player.AttackManaGain = 5;
        player.DartDamage = 5;
        player.DartCost = 5;
        player.DefenseReduction = 30;
        player.UltimateDamage = 40;
        player.RetainMana = false;
        player.PoisonCoating = 0;
        player.PoisonMastery = 0;
        player.BleedEdge = 0;
        player.BleedMastery = 0;
        player.CounterStance = 0;
        Array.Clear(player.UpgradeLevels, 0, player.UpgradeLevels.Length);
        Array.Clear(player.BuildCounts, 0, player.BuildCounts.Length);
        Array.Clear(player.ActionCounts, 0, player.ActionCounts.Length);
        AddLog("新的一輪開始。玩家 HP 100、MP 15。目標是連續贏得五場戰鬥。");
        StartBattle();
    }

    public bool CanUse(PlayerAction action)
    {
        if (action == PlayerAction.Dart) return player.Mana >= player.DartCost;
        if (action == PlayerAction.Ultimate) return player.Mana >= 45;
        return true;
    }

    public void TakeTurn(PlayerAction action)
    {
        if (enemy == null || IsResting || IsGameOver || HasWon) return;
        if (!CanUse(action))
        {
            AddLog(action == PlayerAction.Dart ? "魔力不足，不能使用暗器。" : "魔力不足，不能使用大招。");
            return;
        }

        Intent intent = EnsureIntent();
        player.ActionCounts[(int)action]++;
        if (action == PlayerAction.Dart) player.Mana -= player.DartCost;
        if (action == PlayerAction.Ultimate) player.Mana -= 45;

        int damageToEnemy = GetPlayerDamage(action);
        int damageToPlayer = intent.Damage + intent.ExtraDamage;
        bool adaptedDefense = false;

        if (action == PlayerAction.Dart && intent.Move == BattleMove.Attack)
        {
            damageToPlayer = 0;
            AddLog("暗器打斷了敵人的普攻。");
        }
        if (intent.Move == BattleMove.Dart && action == PlayerAction.Attack)
        {
            damageToEnemy = 0;
            AddLog("敵人的暗器打斷了你的普攻。");
        }

        if (enemy.Kind == EnemyKind.RuinsGuardian && action == enemy.AdaptTarget)
        {
            if (action == PlayerAction.Defend) adaptedDefense = true;
            else damageToEnemy /= 2;
            enemy.AdaptTarget = NextAdaptTarget(action);
            AddLog("遠古遺跡守衛觸發適應，並開始分析另一種行動。");
        }

        if (intent.Move == BattleMove.Charge)
        {
            damageToPlayer = 0;
            if (enemy.Kind == EnemyKind.Orc && action == PlayerAction.Dart)
            {
                AddLog("暗器打斷獸人的蓄力！");
            }
            else
            {
                int threshold = enemy.Kind == EnemyKind.Troll ? 30 : 20;
                if ((enemy.Kind == EnemyKind.Troll || enemy.Kind == EnemyKind.OgreChief) && damageToEnemy >= threshold)
                {
                    enemy.StunnedNext = true;
                    AddLog(enemy.Name + "受到重創，蓄力中斷並暈眩！");
                }
                else
                {
                    enemy.ChargePending = true;
                    AddLog(enemy.Name + "蓄力尚未被打斷，下回合將使出重擊。");
                }
            }
        }
        if (intent.Move == BattleMove.Command)
        {
            enemy.CommandPending = true;
            damageToPlayer = 0;
            AddLog("哥布林王下令小兵準備攻擊。");
        }
        if (intent.Move == BattleMove.Drain || intent.Move == BattleMove.Stunned) damageToPlayer = 0;
        if (intent.Move == BattleMove.Drain)
        {
            int stolen = Math.Min(5, player.Mana);
            player.Mana -= stolen;
            enemy.Mana = Clamp(enemy.Mana + stolen, 0, 95);
            enemy.DrainsUsed++;
            AddLog("幽靈吸走 " + stolen + " MP。");
        }

        if (intent.Move == BattleMove.Heavy && action == PlayerAction.Defend)
            damageToPlayer = damageToPlayer * 50 / 100;
        else if (action == PlayerAction.Defend && damageToPlayer > 0)
        {
            int reduction = adaptedDefense ? Math.Max(0, player.DefenseReduction - 15) : player.DefenseReduction;
            if (intent.Move == BattleMove.Ultimate) reduction = adaptedDefense ? 35 : 50;
            damageToPlayer = damageToPlayer * (100 - reduction) / 100;
        }
        if (intent.Move == BattleMove.Defend && damageToEnemy > 0)
            damageToEnemy = damageToEnemy * (100 - enemy.DefenseReduction) / 100;

        if (action == PlayerAction.Attack && intent.Move != BattleMove.Dart)
            player.Mana = Clamp(player.Mana + player.AttackManaGain, 0, 95);
        if (action == PlayerAction.Defend)
            player.Mana = Clamp(player.Mana + (intent.Move == BattleMove.Dart ? 15 : 10), 0, 95);
        if (intent.Move == BattleMove.Attack && action != PlayerAction.Dart)
            enemy.Mana = Clamp(enemy.Mana + enemy.AttackManaGain, 0, 95);
        if (intent.Move == BattleMove.Defend)
            enemy.Mana = Clamp(enemy.Mana + (action == PlayerAction.Dart ? 15 : 10), 0, 95);

        if (action == PlayerAction.Defend && damageToPlayer > 0 && player.CounterStance > 0)
        {
            int counterDamage = player.CounterStance * 5;
            damageToEnemy += counterDamage;
            AddLog("反擊造成 " + counterDamage + " 傷害。");
        }

        if (damageToEnemy > 0)
        {
            if (action == PlayerAction.Dart && player.PoisonCoating > 0)
            {
                enemy.PoisonStacks = Math.Min(3, enemy.PoisonStacks + 1);
                enemy.PoisonTurns = 3;
                AddLog("敵人中毒（" + enemy.PoisonStacks + " 層）。");
            }
            if (action == PlayerAction.Attack && player.BleedEdge > 0)
            {
                enemy.BleedStacks = Math.Min(3, enemy.BleedStacks + 1);
                enemy.BleedTurns = 2;
                AddLog("敵人流血（" + enemy.BleedStacks + " 層）。");
            }
        }

        enemy.Hp = Math.Max(0, enemy.Hp - damageToEnemy);
        int dotDamage = 0;
        if (enemy.Hp > 0) dotDamage = ApplyDamageOverTime();
        if (enemy.Kind == EnemyKind.Slime && damageToEnemy == 0 && dotDamage == 0 && enemy.Hp > 0 && enemy.SlimeHeals < 3)
        {
            enemy.Hp = Math.Min(enemy.MaxHp, enemy.Hp + 3);
            enemy.SlimeHeals++;
            AddLog("史萊姆吸收黏液，恢復 3 HP。");
        }
        if (enemy.Kind == EnemyKind.OgreChief && damageToEnemy > 0 && intent.Move != BattleMove.Charge)
            enemy.Rage++;

        player.Hp = Clamp(player.Hp - damageToPlayer, 0, 100);
        AddLog("本回合：你受到 " + damageToPlayer + " 傷害；敵人受到 " + damageToEnemy +
               " 直接傷害" + (dotDamage > 0 ? " + " + dotDamage + " 持續傷害。" : "。"));
        currentIntent = null;

        if (player.Hp <= 0)
        {
            IsGameOver = true;
            AddLog("你在第 " + Stage + " 關倒下，本輪結束。");
        }
        else if (enemy.Hp <= 0)
        {
            AddLog("擊敗了 " + enemy.Name + "！");
            if (Stage == 5)
            {
                HasWon = true;
                AddLog("恭喜通關五場戰鬥！");
            }
            else
            {
                BeginRest();
            }
        }
    }

    public void ChooseExtraHeal()
    {
        if (!IsResting) return;
        player.Hp = Clamp(player.Hp + 15, 0, 100);
        AddLog("額外恢復 15 HP，目前 HP " + player.Hp + "/100。");
        FinishRest();
    }

    public void ChooseUpgrade(UpgradeId id)
    {
        if (!IsResting) return;
        bool offered = false;
        for (int i = 0; i < upgradeOffers.Count; i++)
            if (upgradeOffers[i].Id == id) offered = true;
        if (!offered) return;

        ApplyUpgrade(id);
        FinishRest();
    }

    private void BeginRest()
    {
        IsResting = true;
        player.Hp = Clamp(player.Hp + 10, 0, 100);
        upgradeOffers.Clear();
        upgradeOffers.AddRange(CreateOffers());
        AddLog("整備開始：固定恢復 10 HP，目前 HP " + player.Hp + "/100。");
    }

    private void FinishRest()
    {
        IsResting = false;
        Stage++;
        if (!player.RetainMana) player.Mana = 15;
        StartBattle();
    }

    private void StartBattle()
    {
        EnemyKind kind;
        switch (Stage)
        {
            case 1: kind = EnemyKind.Goblin; break;
            case 2: kind = EnemyKind.Orc; break;
            case 3: kind = EnemyKind.Ghost; break;
            case 4: kind = EnemyKind.Slime; break;
            default:
                kind = (EnemyKind)((int)EnemyKind.GoblinKing + random.Next(0, 4));
                break;
        }
        enemy = CreateEnemy(kind, Stage);
        currentIntent = null;
        AddLog("第 " + Stage + " 關：" + enemy.Name + (Stage == 5 ? "（Boss）" : "") +
               "。敵人 HP " + enemy.Hp + "，你有 HP " + player.Hp + "、MP " + player.Mana + "。");
    }

    private EnemyState CreateEnemy(EnemyKind kind, int stage)
    {
        EnemyState e = new EnemyState();
        e.Kind = kind;
        switch (kind)
        {
            case EnemyKind.Goblin:
                e.Name = "哥布林"; e.Hp = 50; e.Mana = 20; e.AttackDamage = 12;
                e.AttackManaGain = 8; e.DartDamage = 8; e.DartCost = 3; e.DefenseReduction = 25;
                break;
            case EnemyKind.Orc:
                e.Name = "獸人"; e.Hp = 55; e.Mana = 10; e.AttackDamage = 10; e.DefenseReduction = 30;
                break;
            case EnemyKind.Ghost:
                e.Name = "幽靈"; e.Hp = 40; e.Mana = 20; e.AttackDamage = 8;
                break;
            case EnemyKind.Slime:
                e.Name = "史萊姆"; e.Hp = 45; e.AttackDamage = 7;
                break;
            case EnemyKind.GoblinKing:
                e.Name = "哥布林王"; e.Hp = 85; e.Mana = 30; e.AttackDamage = 7;
                e.AttackManaGain = 5; e.DartDamage = 9; e.DartCost = 5; e.DefenseReduction = 25;
                break;
            case EnemyKind.Troll:
                e.Name = "巨魔"; e.Hp = 90; e.Mana = 20; e.AttackDamage = 8;
                break;
            case EnemyKind.OgreChief:
                e.Name = "食人魔酋長"; e.Hp = 85; e.Mana = 20; e.AttackDamage = 8;
                break;
            default:
                e.Name = "遠古遺跡守衛"; e.Hp = 100; e.Mana = 30; e.AttackDamage = 8;
                break;
        }

        if (stage >= 1 && stage <= 4)
        {
            int[] hpPercent = { 100, 105, 110, 120 };
            int[] damagePercent = { 100, 101, 103, 105 };
            e.Hp = (e.Hp * hpPercent[stage - 1] + 99) / 100;
            e.AttackDamage = (e.AttackDamage * damagePercent[stage - 1] + 99) / 100;
            if (e.DartDamage > 0) e.DartDamage = (e.DartDamage * damagePercent[stage - 1] + 99) / 100;
        }
        e.MaxHp = e.Hp;
        return e;
    }

    private Intent EnsureIntent()
    {
        if (currentIntent != null) return currentIntent;
        enemy.Turn++;
        if (enemy.StunnedNext)
        {
            enemy.StunnedNext = false;
            currentIntent = new Intent(BattleMove.Stunned, 0, 0, false);
            return currentIntent;
        }
        if (enemy.ChargePending)
        {
            enemy.ChargePending = false;
            currentIntent = new Intent(BattleMove.Heavy, 18, 0, true);
            return currentIntent;
        }
        if (enemy.Kind == EnemyKind.Orc && enemy.Turn % 3 == 0)
            return currentIntent = new Intent(BattleMove.Charge, 0, 0, true);
        if (enemy.Kind == EnemyKind.Troll && enemy.Turn % 4 == 0)
            return currentIntent = new Intent(BattleMove.Charge, 0, 0, true);
        if (enemy.Kind == EnemyKind.OgreChief && enemy.Rage >= 3)
        {
            enemy.Rage = 0;
            return currentIntent = new Intent(BattleMove.Charge, 0, 0, true);
        }
        if (enemy.Kind == EnemyKind.Ghost && enemy.DrainsUsed < 2 && enemy.Turn % 2 == 0)
            return currentIntent = new Intent(BattleMove.Drain, 0, 0, false);
        if (enemy.Kind == EnemyKind.GoblinKing && enemy.CommandPending)
        {
            enemy.CommandPending = false;
            return currentIntent = new Intent(BattleMove.Attack, enemy.AttackDamage, 6, false);
        }
        if (enemy.Kind == EnemyKind.GoblinKing && enemy.Turn % 3 == 0)
            return currentIntent = new Intent(BattleMove.Command, 0, 0, false);
        if (enemy.Kind == EnemyKind.RuinsGuardian)
        {
            if (!enemy.HasAdaptTarget)
            {
                enemy.AdaptTarget = SelectAdaptTarget();
                enemy.HasAdaptTarget = true;
            }
            return currentIntent = new Intent(BattleMove.Attack, enemy.AttackDamage, 0, false);
        }

        if (enemy.Mana >= 45 && random.Next(0, 8) == 0)
        {
            enemy.Mana -= 45;
            return currentIntent = new Intent(BattleMove.Ultimate, 40, 0, true);
        }
        if (enemy.DartDamage > 0 && enemy.Mana >= enemy.DartCost && random.Next(0, 4) == 0)
        {
            enemy.Mana -= enemy.DartCost;
            return currentIntent = new Intent(BattleMove.Dart, enemy.DartDamage, 0, false);
        }
        if (random.Next(0, 5) == 0)
            return currentIntent = new Intent(BattleMove.Defend, 0, 0, false);
        return currentIntent = new Intent(BattleMove.Attack, enemy.AttackDamage, 0, false);
    }

    private string GetIntentText()
    {
        if (enemy == null || IsResting || IsGameOver || HasWon) return "";
        Intent intent = EnsureIntent();
        string text = intent.Move.ToString();
        switch (intent.Move)
        {
            case BattleMove.Attack: text = "普攻"; break;
            case BattleMove.Dart: text = "暗器"; break;
            case BattleMove.Defend: text = "防禦"; break;
            case BattleMove.Ultimate: text = "大招"; break;
            case BattleMove.Charge: text = "蓄力中"; break;
            case BattleMove.Heavy: text = "蓄力重擊"; break;
            case BattleMove.Drain: text = "吸魔"; break;
            case BattleMove.Command: text = "下達王令"; break;
            case BattleMove.Stunned: text = "暈眩，無法行動"; break;
        }
        if (enemy.Kind == EnemyKind.RuinsGuardian)
            text += "（適應目標：" + GetActionName(enemy.AdaptTarget) + "）";
        return text;
    }

    private int ApplyDamageOverTime()
    {
        int damage = 0;
        if (enemy.PoisonTurns > 0)
        {
            damage += enemy.PoisonStacks * 2 + player.PoisonMastery;
            enemy.PoisonTurns--;
            if (enemy.PoisonTurns == 0) enemy.PoisonStacks = 0;
        }
        if (enemy.BleedTurns > 0)
        {
            damage += enemy.BleedStacks * 2 + player.BleedMastery;
            enemy.BleedTurns--;
            if (enemy.BleedTurns == 0) enemy.BleedStacks = 0;
        }
        if (damage > 0)
        {
            enemy.Hp = Math.Max(0, enemy.Hp - damage);
            AddLog("持續傷害造成 " + damage + " 點傷害。");
        }
        return damage;
    }

    private int GetPlayerDamage(PlayerAction action)
    {
        switch (action)
        {
            case PlayerAction.Attack: return player.AttackDamage;
            case PlayerAction.Dart: return player.DartDamage;
            case PlayerAction.Ultimate: return player.UltimateDamage;
            default: return 0;
        }
    }

    private PlayerAction SelectAdaptTarget()
    {
        int selected = 0;
        for (int i = 1; i < player.ActionCounts.Length; i++)
            if (player.ActionCounts[i] > player.ActionCounts[selected]) selected = i;
        return (PlayerAction)selected;
    }

    private PlayerAction NextAdaptTarget(PlayerAction current)
    {
        int selected = -1;
        int bestCount = -1;
        for (int i = 0; i < player.ActionCounts.Length; i++)
        {
            if (i == (int)current) continue;
            if (player.ActionCounts[i] > bestCount)
            {
                bestCount = player.ActionCounts[i];
                selected = i;
            }
        }
        return selected < 0 ? PlayerAction.Attack : (PlayerAction)selected;
    }

    private List<UpgradeOffer> CreateOffers()
    {
        List<UpgradeDefinition> pool = new List<UpgradeDefinition>();
        for (int i = 0; i < UpgradeDefinitions.Length; i++)
        {
            UpgradeDefinition def = UpgradeDefinitions[i];
            if (player.UpgradeLevels[(int)def.Id] >= def.MaxLevel) continue;
            if (def.HasPrerequisite && player.UpgradeLevels[(int)def.Prerequisite] == 0) continue;
            pool.Add(def);
        }

        List<UpgradeOffer> result = new List<UpgradeOffer>();
        UpgradeFamily focus = GetDominantFamily();
        while (pool.Count > 0 && result.Count < 3)
        {
            int totalWeight = 0;
            for (int i = 0; i < pool.Count; i++)
                totalWeight += focus != UpgradeFamily.None && pool[i].Family == focus ? 4 : 1;
            int roll = random.Next(0, totalWeight);
            int selected = 0;
            for (int i = 0; i < pool.Count; i++)
            {
                roll -= focus != UpgradeFamily.None && pool[i].Family == focus ? 4 : 1;
                if (roll < 0) { selected = i; break; }
            }
            UpgradeDefinition choice = pool[selected];
            result.Add(new UpgradeOffer(choice.Id, choice.Label));
            pool.RemoveAt(selected);
        }
        return result;
    }

    private UpgradeFamily GetDominantFamily()
    {
        int bestIndex = 0;
        for (int i = 1; i < player.BuildCounts.Length; i++)
            if (player.BuildCounts[i] > player.BuildCounts[bestIndex]) bestIndex = i;
        if (bestIndex == 0 || player.BuildCounts[bestIndex] == 0) return UpgradeFamily.None;
        return (UpgradeFamily)bestIndex;
    }

    private void ApplyUpgrade(UpgradeId id)
    {
        UpgradeDefinition def = UpgradeDefinitions[(int)id];
        player.UpgradeLevels[(int)id]++;
        player.BuildCounts[(int)def.Family]++;
        switch (id)
        {
            case UpgradeId.Weapon: player.AttackDamage += 3; break;
            case UpgradeId.PoisonCoating: player.PoisonCoating = 1; break;
            case UpgradeId.PoisonMastery: player.PoisonMastery++; break;
            case UpgradeId.BleedEdge: player.BleedEdge = 1; break;
            case UpgradeId.BleedMastery: player.BleedMastery++; break;
            case UpgradeId.UltimatePower: player.UltimateDamage += 5; break;
            case UpgradeId.ManaFlow: player.AttackManaGain += 2; break;
            case UpgradeId.ManaReserve: player.RetainMana = true; break;
            case UpgradeId.CounterStance: player.CounterStance++; break;
            case UpgradeId.DartPower: player.DartDamage += 2; break;
            case UpgradeId.DartEfficiency: player.DartCost = Math.Max(3, player.DartCost - 1); break;
            case UpgradeId.DefenseTraining: player.DefenseReduction = Math.Min(40, player.DefenseReduction + 5); break;
        }
        AddLog("取得強化：「" + def.Label + "」。強化在本輪持續有效。");
    }

    private string DescribeBuild()
    {
        UpgradeFamily family = GetDominantFamily();
        switch (family)
        {
            case UpgradeFamily.Poison: return "毒傷流";
            case UpgradeFamily.Bleed: return "流血流";
            case UpgradeFamily.Power: return "爆發流";
            case UpgradeFamily.Mana: return "蓄魔流";
            case UpgradeFamily.Defense: return "反擊／防禦流";
            case UpgradeFamily.Precision: return "暗器流";
            default: return "尚未成形";
        }
    }

    private static int Clamp(int value, int min, int max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    private static string GetActionName(PlayerAction action)
    {
        switch (action)
        {
            case PlayerAction.Attack: return "普攻";
            case PlayerAction.Dart: return "暗器";
            case PlayerAction.Defend: return "防禦";
            default: return "大招";
        }
    }

    private void AddLog(string message)
    {
        log.Add(message);
        while (log.Count > 14) log.RemoveAt(0);
    }
}

#if UNITY_5_3_OR_NEWER
public sealed class CardBattleUnityDemo : MonoBehaviour
{
    private CardBattleGame game;
    private Vector2 logScroll;
    private GUIStyle headerStyle;

    private void Start()
    {
        game = new CardBattleGame();
        game.StartNewRun();
    }

    private void OnGUI()
    {
        if (game == null) return;
        GUI.skin.label.fontSize = 18;
        GUI.skin.button.fontSize = 17;
        GUI.skin.box.fontSize = 18;
        if (headerStyle == null)
        {
            headerStyle = new GUIStyle(GUI.skin.label);
            headerStyle.fontSize = 25;
            headerStyle.fontStyle = FontStyle.Bold;
        }

        GUILayout.BeginArea(new Rect(20, 20, Screen.width - 40, Screen.height - 40), GUI.skin.box);
        GUILayout.Label("五關制卡牌戰鬥原型", headerStyle);
        GUILayout.Label("第 " + game.Stage + " 關　流派：" + game.BuildText);
        GUILayout.Label("玩家　HP " + game.PlayerHp + "/" + game.PlayerMaxHp + "　MP " + game.PlayerMana + "/" + game.PlayerMaxMana);
        GUILayout.Label("普攻 " + game.PlayerAttackDamage + " 傷害／+" + game.PlayerAttackManaGain +
                        " MP　暗器 " + game.PlayerDartDamage + " 傷害／消耗 " + game.PlayerDartCost +
                        " MP　防禦 " + game.PlayerDefenseReduction + "%　大招 " + game.PlayerUltimateDamage + " 傷害");
        if (!string.IsNullOrEmpty(game.EnemyName))
            GUILayout.Label(game.EnemyName + "　HP " + game.EnemyHp + "/" + game.EnemyMaxHp);

        if (!game.IsResting && !game.IsGameOver && !game.HasWon)
        {
            GUILayout.Label("敵方動向：" + game.CurrentIntentText);
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            DrawActionButton("普攻：" + game.PlayerAttackDamage + " 傷害，+" + game.PlayerAttackManaGain + " 魔力", PlayerAction.Attack, true);
            DrawActionButton("暗器：" + game.PlayerDartDamage + " 傷害，消耗 " + game.PlayerDartCost + " 魔力", PlayerAction.Dart, game.CanUse(PlayerAction.Dart));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            DrawActionButton("防禦：一般減傷 " + game.PlayerDefenseReduction + "%，大招減傷 50%", PlayerAction.Defend, true);
            DrawActionButton("大招：" + game.PlayerUltimateDamage + " 傷害，消耗 45 魔力", PlayerAction.Ultimate, game.CanUse(PlayerAction.Ultimate));
            GUILayout.EndHorizontal();
        }
        else if (game.IsResting)
        {
            GUILayout.Space(8);
            GUILayout.Label("整備：固定恢復 10 HP，請選額外恢復或一項強化。");
            if (GUILayout.Button("額外恢復 15 HP", GUILayout.Height(42))) game.ChooseExtraHeal();
            IList<UpgradeOffer> offers = game.UpgradeOffers;
            for (int i = 0; i < offers.Count; i++)
            {
                UpgradeOffer offer = offers[i];
                if (GUILayout.Button(offer.Label, GUILayout.Height(42))) game.ChooseUpgrade(offer.Id);
            }
        }
        else if (game.IsGameOver || game.HasWon)
        {
            GUILayout.Space(10);
            GUILayout.Label(game.HasWon ? "恭喜通關！" : "本輪結束。", headerStyle);
            if (GUILayout.Button("重新開始", GUILayout.Height(44))) game.StartNewRun();
        }

        GUILayout.Space(12);
        GUILayout.Label("戰鬥紀錄");
        logScroll = GUILayout.BeginScrollView(logScroll, GUILayout.ExpandHeight(true));
        IList<string> entries = game.Log;
        for (int i = 0; i < entries.Count; i++) GUILayout.Label(entries[i]);
        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private void DrawActionButton(string label, PlayerAction action, bool enabled)
    {
        GUI.enabled = enabled;
        if (GUILayout.Button(label, GUILayout.Height(50))) game.TakeTurn(action);
        GUI.enabled = true;
    }

}
#endif
