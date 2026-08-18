using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire
{
    public sealed partial class JourneyTravelGameplayPrototype
    {
        private bool CanUseJourneyConsumable(ConsumableContent item)
        {
            if (item == null || phase != Phase.Battle || battle == null ||
                battleInputLocked || pileOverlayOpen || !realtimeBattleActive) return false;
            return item.effect switch
            {
                ConsumableEffectType.Heal => run.hp < run.maxHp,
                ConsumableEffectType.Energy => realtimeBattle.Energy < realtimeBattle.MaximumEnergy,
                ConsumableEffectType.EnemyDelay => realtimeBattle.HasDelayableActions,
                ConsumableEffectType.DayReduction => false,
                _ => true
            };
        }

        private void UseJourneyConsumable(string id)
        {
            if (run?.consumables == null) return;
            int inventoryIndex = run.consumables.IndexOf(id);
            ConsumableContent item = ConsumableSystem.Definition(id);
            if (inventoryIndex < 0 || !CanUseJourneyConsumable(item)) return;

            bool applied = true;
            BattleActionFx fx = new BattleActionFx
            {
                ok = true,
                cardName = item.name,
                cardType = CardType.Skill
            };
            switch (item.effect)
            {
                case ConsumableEffectType.Heal:
                {
                    int before = run.hp;
                    run.hp = Mathf.Min(run.maxHp, run.hp + item.amount);
                    fx.healGained = run.hp - before;
                    break;
                }
                case ConsumableEffectType.Block:
                    run.block += item.amount;
                    fx.blockGained = item.amount;
                    break;
                case ConsumableEffectType.Damage:
                {
                    int damage = Mathf.Max(0, item.amount);
                    int dealt = Mathf.Max(0, damage - battle.enemyBlock);
                    battle.enemyBlock = Mathf.Max(0, battle.enemyBlock - damage);
                    battle.enemyHp -= dealt;
                    fx.damageToEnemy = dealt;
                    fx.rolledDamage = damage;
                    fx.cardType = CardType.Attack;
                    fx.enemyDefeated = battle.enemyHp <= 0;
                    break;
                }
                case ConsumableEffectType.Energy:
                {
                    int before = realtimeBattle.Energy;
                    realtimeBattle.AddEnergy(item.amount);
                    run.energy = realtimeBattle.Energy;
                    fx.energyGained = realtimeBattle.Energy - before;
                    break;
                }
                case ConsumableEffectType.AttackBoost:
                    realtimeBattle.ApplyAttackBoost(item.amount, item.durationSeconds);
                    break;
                case ConsumableEffectType.GuardBoost:
                    realtimeBattle.ApplyGuardBoost(item.amount, item.durationSeconds);
                    break;
                case ConsumableEffectType.EnemyDelay:
                    applied = realtimeBattle.DelayUpcomingActions(item.amount) > 0;
                    break;
                default:
                    applied = false;
                    break;
            }

            if (!applied) return;
            run.consumables.RemoveAt(inventoryIndex);
            BattleSystem.Record(battle, $"{item.name}を使用");
            battleLog.text = item.effect switch
            {
                ConsumableEffectType.AttackBoost =>
                    $"{item.name}：{item.durationSeconds:0}秒間、攻撃+{item.amount}",
                ConsumableEffectType.GuardBoost =>
                    $"{item.name}：{item.durationSeconds:0}秒間、防御+{item.amount}",
                ConsumableEffectType.EnemyDelay =>
                    $"{item.name}：未確定の敵行動を{item.amount}秒遅らせた。",
                _ => battle.log
            };

            if (fx.damageToEnemy > 0)
            {
                battlePresentationRoutine = StartCoroutine(PlayerAttackPresentationRoutine(
                    fx.damageToEnemy,
                    fx.enemyDefeated,
                    battleRevision));
                return;
            }
            if (fx.enemyDefeated)
            {
                SetMainEnemyVisible(false);
                CompleteJourneyBattleVictory();
                return;
            }
            RefreshBattleUi();
            realtimeReelPresenter?.Refresh(realtimeBattle);
        }

        private void AppendRealtimeConsumableStatuses()
        {
            if (playerStatusHost == null || !realtimeBattleActive) return;
            AppendRealtimeConsumableStatus(
                "攻",
                realtimeBattle.AttackBonus,
                realtimeBattle.AttackBonusRemaining,
                "攻勢香：攻撃カードのダメージ上昇");
            AppendRealtimeConsumableStatus(
                "防",
                realtimeBattle.GuardBonus,
                realtimeBattle.GuardBonusRemaining,
                "防護印：防御カードのブロック上昇");
        }

        private void AppendRealtimeConsumableStatus(
            string iconText,
            int amount,
            double remaining,
            string tooltip)
        {
            if (amount <= 0 || remaining <= 0d) return;
            var chip = new VisualElement
            {
                pickingMode = PickingMode.Position,
                tooltip = $"{tooltip}\n残り {remaining:0.0}秒"
            };
            chip.AddToClassList("ps-journey__status-chip");
            chip.AddToClassList("status--consumable");

            var icon = new Label(iconText) { pickingMode = PickingMode.Ignore };
            icon.AddToClassList("ps-journey__status-icon");
            chip.Add(icon);

            var count = new Label($"+{amount} / {Math.Ceiling(remaining):0}s")
            {
                pickingMode = PickingMode.Ignore
            };
            count.AddToClassList("ps-journey__status-count");
            chip.Add(count);
            playerStatusHost.Add(chip);
        }
    }
}
