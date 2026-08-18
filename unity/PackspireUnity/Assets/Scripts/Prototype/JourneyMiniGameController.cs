using UnityEngine;

namespace Packspire
{
    internal enum MiniGameKind
    {
        StampTiming,
        CargoBalance,
        AddressLabel,
        WaxMatch,
        RoadDodge,
        RainCover
    }

    internal enum JourneyMiniGameInput
    {
        Left,
        Action,
        Right
    }

    internal readonly struct JourneyMiniGameOutcome
    {
        public readonly bool Success;
        public readonly bool Perfect;
        public readonly float TravelAdvance;
        public readonly bool HealCourier;
        public readonly string Message;

        public JourneyMiniGameOutcome(bool success, string message, bool perfect = false,
            float travelAdvance = 0f, bool healCourier = false)
        {
            Success = success;
            Perfect = perfect;
            TravelAdvance = travelAdvance;
            HealCourier = healCourier;
            Message = message ?? string.Empty;
        }
    }

    /// <summary>
    /// UI-free model for roadside tasks. Presentation and rewards remain owned by the
    /// journey screen, while timing, input, and success rules can be tested alone.
    /// </summary>
    internal sealed class JourneyMiniGameController
    {
        private JourneyMiniGameOutcome pendingOutcome;
        private bool hasPendingOutcome;
        private float balanceVelocity;

        public MiniGameKind Kind { get; private set; }
        public float Clock { get; private set; }
        public float Value { get; private set; }
        public float BalanceHold { get; private set; }
        public int Stage { get; private set; }
        public int Target { get; private set; }
        public int Lane { get; private set; }
        public int Revision { get; private set; }
        public bool IsResolved { get; private set; }
        public bool LastInputRejected { get; private set; }
        public float SecondsRemaining => Mathf.Max(0f, 7f - Clock);

        public void Start(MiniGameKind kind)
        {
            Kind = kind;
            Clock = 0f;
            BalanceHold = 0f;
            balanceVelocity = 0f;
            Stage = 0;
            Target = Random.Range(0, 3);
            Lane = 1;
            Value = kind == MiniGameKind.CargoBalance ? Random.Range(22f, 78f) : 50f;
            IsResolved = false;
            LastInputRejected = false;
            hasPendingOutcome = false;
            Revision++;
        }

        public void Tick(float delta)
        {
            if (IsResolved || delta <= 0f) return;
            Clock += delta;
            switch (Kind)
            {
                case MiniGameKind.StampTiming:
                    Value = (Mathf.Sin(Clock * 3.7f) * .5f + .5f) * 100f;
                    break;
                case MiniGameKind.CargoBalance:
                    balanceVelocity += Mathf.Sin(Clock * 1.9f) * 9f * delta;
                    balanceVelocity = Mathf.Clamp(balanceVelocity, -17f, 17f);
                    Value = Mathf.Clamp(Value + balanceVelocity * delta, 0f, 100f);
                    if (Mathf.Abs(Value - 50f) < 10f) BalanceHold += delta;
                    else BalanceHold = Mathf.Max(0f, BalanceHold - delta * .65f);
                    if (BalanceHold >= 2f)
                        Complete(new JourneyMiniGameOutcome(true,
                            "荷姿が安定した。封印片を1つ回収。", healCourier: true));
                    break;
                case MiniGameKind.RoadDodge:
                    Value = Lane * 50f;
                    break;
            }

            if (!IsResolved && Clock >= 7f)
                Complete(new JourneyMiniGameOutcome(false, "見送り。旅程へのペナルティはありません。"));
        }

        public void Input(JourneyMiniGameInput input)
        {
            if (IsResolved) return;
            int side = input == JourneyMiniGameInput.Left ? 0 :
                input == JourneyMiniGameInput.Right ? 2 : 1;

            switch (Kind)
            {
                case MiniGameKind.StampTiming when input == JourneyMiniGameInput.Action:
                    float distance = Mathf.Abs(Value - 50f);
                    bool success = distance <= 18f;
                    bool perfect = distance <= 7f;
                    Complete(new JourneyMiniGameOutcome(success,
                        perfect
                            ? "PERFECT　未整理荷物 +1、次の判断地点まで少し短縮。"
                            : success ? "GOOD　未整理荷物 +1。" : "受取印がずれ、回収を見送った。",
                        perfect, perfect ? .75f : 0f));
                    break;
                case MiniGameKind.CargoBalance:
                    if (input == JourneyMiniGameInput.Action)
                    {
                        balanceVelocity *= .35f;
                        Value = Mathf.Lerp(Value, 50f, .28f);
                    }
                    else balanceVelocity += input == JourneyMiniGameInput.Left ? -10f : 10f;
                    break;
                case MiniGameKind.AddressLabel:
                case MiniGameKind.WaxMatch:
                    ResolveChoice(side);
                    break;
                case MiniGameKind.RoadDodge:
                    if (input == JourneyMiniGameInput.Left || input == JourneyMiniGameInput.Right)
                    {
                        Lane = side;
                        Value = Lane * 50f;
                        Revision++;
                    }
                    else ResolveRoadHazard();
                    break;
                case MiniGameKind.RainCover:
                    ResolveRainCover(side);
                    break;
            }
        }

        public bool TryConsumeOutcome(out JourneyMiniGameOutcome outcome)
        {
            outcome = pendingOutcome;
            if (!hasPendingOutcome) return false;
            hasPendingOutcome = false;
            return true;
        }

        private void ResolveChoice(int choice)
        {
            if (choice != Target)
            {
                Complete(new JourneyMiniGameOutcome(false, "照合が一致しない。今回は荷物を見送った。"));
                return;
            }
            Stage++;
            int goal = Kind == MiniGameKind.WaxMatch ? 2 : 1;
            Revision++;
            if (Stage < goal)
            {
                Target = Random.Range(0, 3);
                Revision++;
                return;
            }
            Complete(new JourneyMiniGameOutcome(true,
                Kind == MiniGameKind.WaxMatch
                    ? "封蝋照合完了。正しい小包を確保した。"
                    : "宛先照合完了。飛散荷札を回収した。"));
        }

        private void ResolveRoadHazard()
        {
            if (Lane == Target)
            {
                Complete(new JourneyMiniGameOutcome(false, "轍に接触。荷を守るため回収を断念した。"));
                return;
            }
            Stage++;
            Revision++;
            if (Stage >= 3)
            {
                Complete(new JourneyMiniGameOutcome(true,
                    "障害物をすべて回避。進行を少し短縮した。", travelAdvance: .65f));
                return;
            }
            Target = Random.Range(0, 3);
            Revision++;
        }

        private void ResolveRainCover(int input)
        {
            if (input != Stage)
            {
                Stage = 0;
                LastInputRejected = true;
                Revision++;
                return;
            }
            LastInputRejected = false;
            Stage++;
            Revision++;
            if (Stage >= 3)
                Complete(new JourneyMiniGameOutcome(true, "雨除け布を固定。荷濡れを防いだ。"));
        }

        private void Complete(in JourneyMiniGameOutcome outcome)
        {
            if (IsResolved) return;
            IsResolved = true;
            pendingOutcome = outcome;
            hasPendingOutcome = true;
            Revision++;
        }
    }
}
