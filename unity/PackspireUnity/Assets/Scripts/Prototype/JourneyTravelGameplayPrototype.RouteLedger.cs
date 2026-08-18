using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire
{
    public sealed partial class JourneyTravelGameplayPrototype
    {
        private void ToggleLedger()
        {
            if (ledgerOverlay == null) return;
            ledgerOpen = !ledgerOpen;
            ledgerOverlay.EnableInClassList("ledger--open", ledgerOpen);
            screen.EnableInClassList("ledger--active", ledgerOpen);
            if (ledgerOpen) PopulateLedger();
            ApplyWorldMotion();
        }

        private void PopulateLedger()
        {
            PopulateLedgerMap();
            PopulateSealBox();
            CourierRouteState route = run.courierRoute;
            CourierRouteNodeDef current = CourierRouteSystem.Node(route.currentNodeId);
            int remaining = Mathf.Max(0, route.deadlineDays - route.daysElapsed);
            ledgerSummary.text = $"現在地：{current?.title ?? "出発局"}　経過 {route.daysElapsed}日　期限まで {remaining}日　回収 {route.recoveredCargoCount}件";
            ShowLedgerNode(current);
        }

        private void PopulateLedgerMap()
        {
            ledgerPhaseHost.Clear();
            CourierRouteState route = run.courierRoute;
            ILookup<int, CourierRouteNodeDef> byPhase = CourierRouteSystem.Nodes.ToLookup(node => node.phase);
            for (int phaseIndex = 0; phaseIndex <= CourierRouteSystem.TotalSegments; phaseIndex++)
            {
                VisualElement column = new VisualElement();
                column.AddToClassList("ps-journey__ledger-phase");
                Label phaseLabel = new Label(phaseIndex == 0 ? "START" : phaseIndex == CourierRouteSystem.TotalSegments ? "GOAL" : phaseIndex.ToString("00"));
                phaseLabel.AddToClassList("ps-journey__ledger-phase-number");
                column.Add(phaseLabel);

                VisualElement nodes = new VisualElement();
                nodes.AddToClassList("ps-journey__ledger-phase-nodes");
                foreach (CourierRouteNodeDef node in byPhase[phaseIndex])
                {
                    CourierRouteNodeDef captured = node;
                    Button button = new Button(() => ShowLedgerNode(captured));
                    button.AddToClassList("ps-journey__ledger-node");
                    bool revealed = CourierRouteSystem.IsRevealed(route, node);
                    button.text = revealed ? node.title : "未確認";
                    button.EnableInClassList("node--unknown", !revealed);
                    button.EnableInClassList("node--resolved", route.resolvedNodeIds.Contains(node.id));
                    button.EnableInClassList("node--current", route.currentNodeId == node.id);
                    button.EnableInClassList("node--available", choices.Any(choice => choice.id == node.id));
                    button.EnableInClassList("node--destination", node.resolution == CourierResolutionKind.Delivery);
                    nodes.Add(button);
                }
                column.Add(nodes);
                ledgerPhaseHost.Add(column);

                if (phaseIndex < CourierRouteSystem.TotalSegments)
                {
                    Label arrow = new Label("›");
                    arrow.AddToClassList("ps-journey__ledger-arrow");
                    ledgerPhaseHost.Add(arrow);
                }
            }
        }

        private void ShowLedgerNode(CourierRouteNodeDef node)
        {
            if (node == null || !CourierRouteSystem.IsRevealed(run.courierRoute, node))
            {
                ledgerNodeTitle.text = "未確認地点";
                ledgerNodeMeta.text = node == null ? "ROUTE DATA UNAVAILABLE" : $"PHASE {node.phase:00}";
                ledgerNodeBody.text = "この先へ進むと遭遇傾向と所要日数が明らかになります。";
                return;
            }

            ledgerNodeTitle.text = node.title;
            ledgerNodeMeta.text = $"PHASE {node.phase:00} / {ResolutionLabel(node.resolution)} / {node.dayCost}日 / {RiskLabel(node.risk)} / {JourneyPresentationConfig.GetRoadWidthLabel(node)}";
            ledgerNodeBody.text = string.IsNullOrWhiteSpace(node.condition)
                ? node.resolutionText
                : $"{node.condition}\n{node.resolutionText}";
        }

        private void PopulateSealBox()
        {
            sealHost.Clear();
            DeliverySealState[] seals = (run.courierRoute.seals ?? new List<DeliverySealState>())
                .Where(seal => seal != null && seal.available).ToArray();
            bool canUse = CanUseSealNow();
            sealTiming.text = phase == Phase.Choice
                ? "大分岐の確定前です。区間用の印を使用できます。"
                : "台帳の確認は可能です。印は主要経路の確定前に使用します。";

            foreach (DeliverySealState seal in seals)
            {
                DeliverySealState captured = seal;
                Button entry = new Button(() => SelectSeal(captured.key));
                entry.userData = seal.key;
                entry.AddToClassList("ps-journey__seal-entry");
                entry.EnableInClassList("seal--selected", selectedSealKey == seal.key);
                entry.EnableInClassList("seal--spent", seal.charges <= 0);
                entry.SetEnabled(seal.charges > 0);

                VisualElement mark = new VisualElement { pickingMode = PickingMode.Ignore };
                mark.AddToClassList("ps-journey__seal-mark");
                Label name = new Label(seal.name) { pickingMode = PickingMode.Ignore };
                name.AddToClassList("ps-journey__seal-entry-name");
                Label charge = new Label($"残 {seal.charges} / {seal.maxCharges}　{SealTargetLabel(seal.target)}") { pickingMode = PickingMode.Ignore };
                charge.AddToClassList("ps-journey__seal-entry-charge");
                entry.Add(mark);
                entry.Add(name);
                entry.Add(charge);
                sealHost.Add(entry);
            }

            DeliverySealState selected = seals.FirstOrDefault(seal => seal.key == selectedSealKey);
            if (selected == null)
            {
                selectedSealKey = "";
                sealDetailName.text = seals.Length == 0 ? "配達印なし" : "印を選択";
                sealDetailEffect.text = seals.Length == 0 ? "荷造りの色一致から配達印を生成できます。" : "印を選ぶと効果と使用可能なタイミングを確認できます。";
                sealApply.SetEnabled(false);
            }
            else ShowSealDetail(selected, canUse);
        }

        private void SelectSeal(string key)
        {
            selectedSealKey = key;
            PopulateSealBox();
        }

        private void ShowSealDetail(DeliverySealState seal, bool canUse)
        {
            sealDetailName.text = $"{seal.name}　残 {seal.charges}/{seal.maxCharges}";
            sealDetailEffect.text = $"{SealTargetLabel(seal.target)}：{seal.text}";
            sealApply.SetEnabled(canUse && seal.charges > 0);
        }

        private void ApplySelectedSeal()
        {
            if (!CanUseSealNow() || string.IsNullOrEmpty(selectedSealKey)) return;
            if (!CourierRouteSystem.UseSeal(run.courierRoute, selectedSealKey, out string message)) return;
            RefreshPersistentUi();
            PopulateSealBox();
            RefreshChoiceAfterSeal();
            ShowToast(message);
        }

        private bool CanUseSealNow()
        {
            CourierRouteState route = run.courierRoute;
            return phase == Phase.Choice && !choiceLocked && !route.complete && !route.failed &&
                   !route.travelPending && !route.awaitingResolution;
        }

        private void RefreshChoiceAfterSeal()
        {
            if (phase != Phase.Choice) return;
            if (choices.Length > 0) BindChoice(0, choices[0]);
            if (choices.Length > 1) BindChoice(1, choices[1]);
        }

        private static string SealTargetLabel(string target)
        {
            return target switch
            {
                DeliverySealSystem.DelayTarget => "地点事故",
                DeliverySealSystem.SealTarget => "印の再装填",
                _ => "次の区間"
            };
        }

        private static void ApplyRouteArt(VisualElement art, CourierRouteNodeDef node)
        {
            string[] classes = { "route-art--ash", "route-art--danger", "route-art--drowned", "route-art--blackbell" };
            foreach (string className in classes) art.RemoveFromClassList(className);
            string nextClass = node.phase >= 8
                ? "route-art--blackbell"
                : node.phase >= 5
                    ? "route-art--drowned"
                    : node.risk >= 2 ? "route-art--danger" : "route-art--ash";
            art.AddToClassList(nextClass);
        }

        private static string RouteFlavor(CourierRouteNodeDef node)
        {
            return node.resolution switch
            {
                CourierResolutionKind.Relay => "検札灯の残る街道を辿る。遠回りだが、荷と身体を整えられる。",
                CourierResolutionKind.Battle => "追跡者の巡回路を横切る。短いが、戦闘を避けては通れない。",
                CourierResolutionKind.Cargo => "配達記録が途切れた区画へ入る。未回収の荷が残されている。",
                CourierResolutionKind.Event => "古い道標に従う不確かな経路。何が待つかは現地で判明する。",
                CourierResolutionKind.Delivery => "封鐘塔へ続く最後の道。荷を守り、受取印まで届ける。",
                _ => "灰市を抜け、次の中継地点へ向かう主要経路。"
            };
        }

        private static string RouteRewardLabel(string resolution)
        {
            return resolution switch
            {
                CourierResolutionKind.Relay => "整備・回復",
                CourierResolutionKind.Cargo => "回収荷物",
                CourierResolutionKind.Battle => "戦利品",
                CourierResolutionKind.Event => "特殊報酬",
                CourierResolutionKind.Delivery => "配達完了",
                _ => "旅程進行"
            };
        }

        private static string RouteSealLabel(CourierRouteNodeDef node)
        {
            if (node.risk >= 2) return "遅延防止印";
            if (node.dayCost >= 2) return "短縮印";
            if (node.resolution == CourierResolutionKind.Relay) return "補綴印";
            return "任意";
        }


    }
}
