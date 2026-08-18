using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire
{
    public readonly struct BattleCardViewModel
    {
        public readonly string Name;
        public readonly string Cost;
        public readonly string Body;
        public readonly string Kind;
        public readonly string LockText;
        public readonly Sprite Artwork;
        public readonly bool Affordable;

        public BattleCardViewModel(
            string name,
            string cost,
            string body,
            string kind,
            string lockText,
            Sprite artwork,
            bool affordable)
        {
            Name = name ?? string.Empty;
            Cost = cost ?? string.Empty;
            Body = string.IsNullOrWhiteSpace(body) ? "効果なし" : body;
            Kind = string.IsNullOrWhiteSpace(kind) ? "technique" : kind;
            LockText = lockText ?? string.Empty;
            Artwork = artwork;
            Affordable = affordable;
        }
    }

    /// <summary>
    /// Retained-mode battle card view. The UXML tree is cloned once; later refreshes
    /// only change content and semantic state classes.
    /// </summary>
    public sealed class BattleCardView
    {
        private const string TemplatePath = "UI/PackspireDocketCard";
        private static VisualTreeAsset template;
        private static readonly string[] KindClasses =
        {
            "ps-docket-attack", "ps-docket-defense", "ps-docket-technique",
            "ps-docket-consumable", "ps-docket-immediate", "ps-docket-installation",
            "ps-docket-curse", "ps-docket-support"
        };

        private readonly VisualElement host;
        private readonly Label nameLabel;
        private readonly Label costLabel;
        private readonly Label lockLabel;
        private readonly VisualElement effectHost;
        private readonly VisualElement artwork;
        private readonly Label effectLabel;
        private BattleCardViewModel previous;
        private bool hasPrevious;

        public VisualElement Root => host;

        public BattleCardView(VisualElement host)
        {
            this.host = host ?? throw new System.ArgumentNullException(nameof(host));
            template ??= PackspireResources.Load<VisualTreeAsset>(TemplatePath);
            if (template == null)
                throw new System.InvalidOperationException($"Missing battle card template: {TemplatePath}");

            host.Clear();
            template.CloneTree(host);
            nameLabel = Require<Label>("docket-main-name");
            costLabel = Require<Label>("docket-receipt-cost");
            lockLabel = Require<Label>("docket-lock");
            effectHost = Require<VisualElement>("docket-main-text");
            artwork = Require<VisualElement>("docket-art");
            effectLabel = new Label { pickingMode = PickingMode.Ignore };
            effectLabel.AddToClassList("ps-docket__effect-copy");
            effectHost.Add(effectLabel);
        }

        public void Bind(in BattleCardViewModel model)
        {
            if (!hasPrevious || previous.Name != model.Name) nameLabel.text = model.Name;
            if (!hasPrevious || previous.Cost != model.Cost) costLabel.text = model.Cost;
            if (!hasPrevious || previous.Body != model.Body) effectLabel.text = model.Body;
            if (!hasPrevious || previous.LockText != model.LockText) lockLabel.text = model.LockText;
            if (!hasPrevious || previous.Artwork != model.Artwork)
                artwork.style.backgroundImage = model.Artwork == null
                    ? StyleKeyword.None
                    : new StyleBackground(model.Artwork);

            if (!hasPrevious || previous.Kind != model.Kind)
            {
                foreach (string className in KindClasses) host.RemoveFromClassList(className);
                host.AddToClassList("ps-docket-" + model.Kind);
            }

            host.EnableInClassList("ps-docket-authorized", model.Affordable);
            host.EnableInClassList("ps-docket-held", !model.Affordable);
            host.EnableInClassList("ps-battle-card-disabled", !model.Affordable);
            previous = model;
            hasPrevious = true;
        }

        private T Require<T>(string name) where T : VisualElement
        {
            T element = host.Q<T>(name);
            if (element != null) return element;
            throw new System.InvalidOperationException(
                $"Battle card template is missing required {typeof(T).Name} '{name}'.");
        }
    }
}
