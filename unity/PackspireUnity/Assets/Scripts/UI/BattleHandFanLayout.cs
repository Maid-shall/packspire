using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire
{
    /// <summary>
    /// Shared card-hand composition used by the regular battle and the seamless
    /// journey battle. Keeping the curve here prevents both screens from slowly
    /// drifting into different card sizes and fan rhythms.
    /// </summary>
    internal static class BattleHandFanLayout
    {
        private static readonly string[] DensityClasses =
        {
            "ps-battle-card-large",
            "ps-battle-card-standard",
            "ps-battle-card-compact",
            "ps-battle-card-dense"
        };

        internal static float Apply(VisualElement card, int index, int count)
        {
            count = Mathf.Max(1, count);
            float center = (count - 1) * .5f;
            float edgeAngle = count <= 2 ? 1.5f : count == 3 ? 3f : count == 4 ? 4.5f :
                count == 5 ? 6f : count == 6 ? 7.5f : count == 7 ? 9f : count == 8 ? 10f : 11f;
            float centerLift = count <= 2 ? 1f : count == 3 ? 4f : count == 4 ? 7f :
                count == 5 ? 10f : count == 6 ? 13f : count == 7 ? 16f : 20f;
            float horizontalStep = count <= 3 ? 126f : count <= 5 ? 110f : count == 6 ? 96f :
                count == 7 ? 86f : count == 8 ? 78f : count == 9 ? 70f : 62f;
            string densityClass = count <= 3 ? DensityClasses[0] : count <= 6 ? DensityClasses[1] :
                count <= 8 ? DensityClasses[2] : DensityClasses[3];

            foreach (string candidate in DensityClasses)
            {
                card.EnableInClassList(candidate, candidate == densityClass);
            }

            float spreadIndex = index - center;
            float normalized = center > 0f ? spreadIndex / center : 0f;
            card.style.position = Position.Absolute;
            card.style.left = new Length(50f, LengthUnit.Percent);
            card.style.marginLeft = -78f + spreadIndex * horizontalStep;
            card.style.bottom = centerLift * (1f - normalized * normalized);
            card.style.rotate = new Rotate(new Angle(normalized * edgeAngle, AngleUnit.Degree));
            card.style.transformOrigin = new TransformOrigin(
                new Length(50f, LengthUnit.Percent),
                new Length(100f, LengthUnit.Percent));
            return Mathf.Abs(spreadIndex);
        }
    }
}
