using UnityEngine;

namespace Packspire
{
    public sealed partial class JourneyTravelGameplayPrototype
    {
        private const float EnemyPoseRecoveryDuration = .24f;
        private const float EnemyPoseRecoverySwitchPoint = .82f;
        private const float EnemyPoseRecoveryDamping = 6f;

        private float enemyPoseRecoveryRemaining;
        private Vector3 enemyPoseRecoveryStartPosition;
        private Quaternion enemyPoseRecoveryStartRotation = Quaternion.identity;
        private Vector3 enemyPoseRecoveryStartScale = Vector3.one;
        private EnemyBattlePose enemyPoseRecoveryTargetPose;
        private bool enemyPoseRecoveryPoseSwitched;

        private void BeginEnemyPoseRecovery(EnemyBattlePose targetPose)
        {
            if (enemyRenderer == null)
            {
                enemyPoseRecoveryRemaining = 0f;
                return;
            }

            CancelEnemyPoseRecovery();

            if (enemyRenderer.sprite == null)
            {
                SetEnemyBattlePose(targetPose);
                enemyRenderer.transform.position = enemyBattleBasePosition;
                enemyRenderer.transform.rotation = Quaternion.identity;
                enemyRenderer.color = Color.white;
                return;
            }

            enemyPoseRecoveryStartPosition = enemyRenderer.transform.position;
            enemyPoseRecoveryStartRotation = enemyRenderer.transform.rotation;
            enemyPoseRecoveryStartScale = enemyRenderer.transform.localScale;
            enemyPoseRecoveryTargetPose = targetPose;
            enemyPoseRecoveryPoseSwitched = false;
            enemyRenderer.color = Color.white;
            enemyPoseRecoveryRemaining = EnemyPoseRecoveryDuration;
        }

        private void UpdateEnemyPoseRecovery(float delta)
        {
            if (enemyPoseRecoveryRemaining <= 0f || enemyRenderer == null)
                return;

            Vector3 targetPosition = enemyRenderer.transform.position;
            Quaternion targetRotation = enemyRenderer.transform.rotation;
            Vector3 targetScale = Vector3.one * enemyBattleBaseScale;

            enemyPoseRecoveryRemaining = Mathf.Max(
                0f,
                enemyPoseRecoveryRemaining - Mathf.Max(0f, delta));
            float progress = 1f - enemyPoseRecoveryRemaining /
                EnemyPoseRecoveryDuration;

            float dampingAtEnd = 1f -
                (1f + EnemyPoseRecoveryDamping) *
                Mathf.Exp(-EnemyPoseRecoveryDamping);
            float damped = 1f -
                (1f + EnemyPoseRecoveryDamping * progress) *
                Mathf.Exp(-EnemyPoseRecoveryDamping * progress);
            float eased = dampingAtEnd <= 0f
                ? progress
                : Mathf.Clamp01(damped / dampingAtEnd);

            Vector3 settledPosition = Vector3.Lerp(
                enemyPoseRecoveryStartPosition,
                targetPosition,
                eased);
            Quaternion settledRotation = Quaternion.Slerp(
                enemyPoseRecoveryStartRotation,
                targetRotation,
                eased);
            Vector3 settledScale = Vector3.Lerp(
                enemyPoseRecoveryStartScale,
                targetScale,
                eased);
            float settlePulse = Mathf.Sin(progress * Mathf.PI);
            settledScale = new Vector3(
                settledScale.x * (1f + settlePulse * .010f),
                settledScale.y * (1f - settlePulse * .018f),
                settledScale.z);

            if (!enemyPoseRecoveryPoseSwitched &&
                progress >= EnemyPoseRecoverySwitchPoint)
            {
                SetEnemyBattlePose(enemyPoseRecoveryTargetPose);
                enemyPoseRecoveryPoseSwitched = true;
            }

            enemyRenderer.transform.position = settledPosition;
            enemyRenderer.transform.rotation = settledRotation;
            enemyRenderer.transform.localScale = settledScale;
            enemyRenderer.color = Color.white;
            SyncMainEnemyShadow();
            UpdateEnemyImpactContactCue(delta);

            if (enemyPoseRecoveryRemaining > 0f) return;
            if (!enemyPoseRecoveryPoseSwitched)
                SetEnemyBattlePose(enemyPoseRecoveryTargetPose);
            enemyRenderer.transform.position = targetPosition;
            enemyRenderer.transform.rotation = targetRotation;
            enemyRenderer.transform.localScale =
                Vector3.one * enemyBattleBaseScale;
            enemyRenderer.color = Color.white;
            enemyPoseRecoveryPoseSwitched = false;
            ResetEnemyImpactContactCue();
            SyncMainEnemyShadow();
        }

        private void CancelEnemyPoseRecovery()
        {
            enemyPoseRecoveryRemaining = 0f;
            enemyPoseRecoveryPoseSwitched = false;
            if (enemyRenderer != null)
                enemyRenderer.color = Color.white;
        }
    }
}
