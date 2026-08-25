namespace Packspire
{
    /// <summary>
    /// World-space translation of the legacy battle composition presets.
    /// Ratios are normalized from the authored actor viewports in PackspireBattle.uss.
    /// </summary>
    public readonly struct JourneyBattleFormationPreset
    {
        public JourneyBattleFormationPreset(
            float playerScale,
            float enemyScale,
            float playerAnchorRatio,
            float enemyZoneStartRatio)
        {
            PlayerScale = playerScale;
            EnemyScale = enemyScale;
            PlayerAnchorRatio = playerAnchorRatio;
            EnemyZoneStartRatio = enemyZoneStartRatio;
        }

        public float PlayerScale { get; }
        public float EnemyScale { get; }
        public float PlayerAnchorRatio { get; }
        public float EnemyZoneStartRatio { get; }
    }

    public static class JourneyBattleFormationLayout
    {
        private const float LegacyNormalPlayerHeight = 350f;
        private const float LegacyNormalEnemyHeight = 390f;

        public static JourneyBattleFormationPreset Resolve(
            BattleFormationScale formation,
            int enemyCount)
        {
            if (enemyCount > 1)
            {
                return new JourneyBattleFormationPreset(
                    286f / LegacyNormalPlayerHeight,
                    238f / LegacyNormalEnemyHeight,
                    .16f,
                    .32f);
            }

            return formation switch
            {
                BattleFormationScale.Small => new JourneyBattleFormationPreset(
                    360f / LegacyNormalPlayerHeight,
                    250f / LegacyNormalEnemyHeight,
                    .23f,
                    .45f),
                BattleFormationScale.Large => new JourneyBattleFormationPreset(
                    315f / LegacyNormalPlayerHeight,
                    420f / LegacyNormalEnemyHeight,
                    .20f,
                    .39f),
                BattleFormationScale.Boss => new JourneyBattleFormationPreset(
                    280f / LegacyNormalPlayerHeight,
                    440f / LegacyNormalEnemyHeight,
                    .18f,
                    .35f),
                _ => new JourneyBattleFormationPreset(1f, 1f, .22f, .43f)
            };
        }
    }
}
