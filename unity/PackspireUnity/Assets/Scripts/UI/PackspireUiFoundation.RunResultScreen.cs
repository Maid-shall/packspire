using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire {
public sealed partial class PackspireUiFoundation {
 VisualElement resultShell;
 VisualElement resultVisualHost;
 VisualElement resultTitleOverlay;
 Label resultTitleLabel;
 Label resultSubtitleLabel;
 Label resultCauseLabel;
 VisualElement resultPrimaryStatsHost;
 ScrollView resultRecordScroll;
 VisualElement resultUnlockHost;
 VisualElement resultHeirloomHost;
 Button resultReturnButton;
 bool resultReturnArmed=true;
 bool resultPreviewMode;
 RunResultViewModel activeResultViewModel;
#if UNITY_EDITOR
 int resultBuildCount;
#endif

 void BuildGameOver()=>BuildRunResult(false);
 void BuildGameClear()=>BuildRunResult(true);

 void BuildRunResult(bool clearPreferred){
#if UNITY_EDITOR
  resultBuildCount++;
  Debug.Log($"[PackspireQA] BuildRunResult count={resultBuildCount} clearPreferred={clearPreferred} screen={game.UiScreen}");
#endif
  if(!resultPreviewMode&&game.UiRun!=null)resultPreviewMode=false;
  bool clear=clearPreferred||game.UiScreen==ScreenId.GameClear;
  activeResultViewModel=resultPreviewMode
   ?BuildPreviewResultViewModel(clear)
   :BuildLiveResultViewModel(clear);
  resultReturnArmed=true;
  BuildResultShell();
  ApplyResultViewModel(activeResultViewModel);
 }

 void BuildResultShell(){
  resultShell=CloneView("UI/PackspireResultView","ps-result-screen ps-dark-surface");
  if(resultShell==null){
   Debug.LogError("Result view could not be created.");
   return;
  }
  resultShell.EnableInClassList("ps-result-clear",false);
  resultShell.EnableInClassList("ps-result-defeat",false);

  RequireViewElement<VisualElement>(resultShell,"result-background");

  resultVisualHost=RequireViewElement<VisualElement>(resultShell,"result-visual-host");
  resultTitleOverlay=RequireViewElement<VisualElement>(resultShell,"result-title-overlay");
  resultTitleLabel=RequireViewElement<Label>(resultShell,"result-title");
  resultSubtitleLabel=RequireViewElement<Label>(resultShell,"result-subtitle");
  RequireViewElement<VisualElement>(resultShell,"result-summary-column");
  resultCauseLabel=RequireViewElement<Label>(resultShell,"result-cause");
  resultPrimaryStatsHost=RequireViewElement<VisualElement>(resultShell,"result-primary-stats");
  resultRecordScroll=RequireViewElement<ScrollView>(resultShell,"result-record-scroll");
  resultRecordScroll.verticalScrollerVisibility=ScrollerVisibility.Auto;
  resultUnlockHost=RequireViewElement<VisualElement>(resultShell,"result-unlock-host");
  resultHeirloomHost=RequireViewElement<VisualElement>(resultShell,"result-heirloom-host");
  var footer=RequireViewElement<VisualElement>(resultShell,"result-footer");
  resultReturnButton=PackspireUiFactory.Button("拠点へ帰還",ReturnFromResultScreen);
  resultReturnButton.AddToClassList("ps-primary-action");
  resultReturnButton.AddToClassList("ps-chrome-action");
  resultReturnButton.AddToClassList("ps-result-return-btn");
  PackspireUiFactory.DecorateActionButton(resultReturnButton,true);
  footer.Add(resultReturnButton);
  screenRoot.Add(resultShell);
 }

 void ApplyResultViewModel(RunResultViewModel model){
  if(resultShell==null||model==null)return;
  bool clear=model.resultType==RunResultType.Clear;
  resultShell.EnableInClassList("ps-result-clear",clear);
  resultShell.EnableInClassList("ps-result-defeat",!clear);

  if(resultTitleLabel!=null)resultTitleLabel.text=model.title??"";
  if(resultSubtitleLabel!=null)resultSubtitleLabel.text=model.subtitle??"";
  if(resultCauseLabel!=null){
   resultCauseLabel.text=model.causeText??"";
   resultCauseLabel.style.display=string.IsNullOrEmpty(model.causeText)?DisplayStyle.None:DisplayStyle.Flex;
  }

  RefreshResultVisual(model);
  RefreshResultPrimaryStats(model);
  RefreshResultRecords(model);
  RefreshResultUnlocks(model);
  RefreshResultHeirloom(model);
  SetReturnEnabled(true);
 }

 void RefreshResultVisual(RunResultViewModel model){
  if(resultVisualHost==null)return;
  resultVisualHost.Clear();
  string dungeonId=string.IsNullOrEmpty(model.backgroundHintDungeonId)?"old_spire":model.backgroundHintDungeonId;
  if(game.UiDungeonArt!=null)
   resultVisualHost.Add(Atlas(game.UiDungeonArt,DungeonUv(dungeonId),"ps-result-visual-art"));
  else {
   var fallback=Container("ps-result-visual-fallback");
   fallback.pickingMode=PickingMode.Ignore;
   resultVisualHost.Add(fallback);
  }
  if(!string.IsNullOrEmpty(model.dungeonName)){
   var place=new Label(model.dungeonName){pickingMode=PickingMode.Ignore};
   place.AddToClassList("ps-result-visual-place");
   resultVisualHost.Add(place);
  }
 }

 void RefreshResultPrimaryStats(RunResultViewModel model){
  if(resultPrimaryStatsHost==null)return;
  resultPrimaryStatsHost.Clear();
  foreach(var stat in model.primaryStats){
   if(stat==null||string.IsNullOrEmpty(stat.value))continue;
   resultPrimaryStatsHost.Add(ResultStatRow(stat.label,stat.value));
  }
 }

 void RefreshResultRecords(RunResultViewModel model){
  if(resultRecordScroll==null)return;
  resultRecordScroll.Clear();
  resultRecordScroll.Add(ChromeSection("LEDGER","遠征の記録"));
  if(!string.IsNullOrEmpty(model.messageText))
   resultRecordScroll.Add(ResultBodyLine(model.messageText));
  if(model.records.Count==0){
   resultRecordScroll.Add(ResultBodyLine("追加の記録はない。"));
   return;
  }
  foreach(var record in model.records){
   if(record==null||string.IsNullOrEmpty(record.value))continue;
   resultRecordScroll.Add(ResultStatRow(record.label,record.value));
  }
 }

 void RefreshResultUnlocks(RunResultViewModel model){
  if(resultUnlockHost==null)return;
  resultUnlockHost.Clear();
  if(model.unlocks==null||model.unlocks.Count==0){
   resultUnlockHost.style.display=DisplayStyle.None;
   return;
  }
  resultUnlockHost.style.display=DisplayStyle.Flex;
  resultUnlockHost.Add(ChromeSection("NEW RECORD","新たな記録"));
  foreach(var unlock in model.unlocks){
   if(unlock==null||string.IsNullOrEmpty(unlock.value))continue;
   resultUnlockHost.Add(ResultStatRow(unlock.label,unlock.value));
  }
 }

 void RefreshResultHeirloom(RunResultViewModel model){
  if(resultHeirloomHost==null)return;
  resultHeirloomHost.Clear();
  if(model.heirloomChanges==null||model.heirloomChanges.Count==0){
   resultHeirloomHost.style.display=DisplayStyle.None;
   return;
  }
  resultHeirloomHost.style.display=DisplayStyle.Flex;
  resultHeirloomHost.Add(ChromeSection("HEIRLOOM","家宝の記録"));
  foreach(var change in model.heirloomChanges){
   if(change==null||string.IsNullOrEmpty(change.value))continue;
   resultHeirloomHost.Add(ResultStatRow(change.label,change.value));
  }
 }

 void SetReturnEnabled(bool enabled){
  if(resultReturnButton==null)return;
  resultReturnButton.SetEnabled(enabled&&resultReturnArmed);
 }

 void ReturnFromResultScreen(){
  if(!resultReturnArmed)return;
  resultReturnArmed=false;
  SetReturnEnabled(false);
  if(resultPreviewMode){
   resultPreviewMode=false;
   DevNavigate(ScreenId.Hub);
   return;
  }
  game.UiReturnToHub();
 }

 VisualElement ResultStatRow(string label,string value){
  var row=Container("ps-result-stat-row");
  var lab=new Label(label){pickingMode=PickingMode.Ignore};
  lab.AddToClassList("ps-result-stat-label");
  row.Add(lab);
  var val=new Label(value){pickingMode=PickingMode.Ignore};
  val.AddToClassList("ps-result-stat-value");
  row.Add(val);
  return row;
 }

 VisualElement ResultBodyLine(string text){
  var line=new Label(text){pickingMode=PickingMode.Ignore};
  line.AddToClassList("ps-result-body-line");
  return line;
 }

 RunResultViewModel BuildLiveResultViewModel(bool clear){
  var run=game.UiRun;
  var model=new RunResultViewModel{
   resultType=clear?RunResultType.Clear:RunResultType.Defeat,
   preview=false,
   title=clear?"旅の完遂":"旅の断章",
   subtitle=clear?"遠征の記録を閉じる":"今回の旅の記録",
   causeText=clear?"":(string.IsNullOrEmpty(game.UiMessage)?"":game.UiMessage),
   messageText=game.UiMessage??"",
  };
  if(run!=null){
   var dungeon=GameCatalog.Dungeons.FirstOrDefault(x=>x.id==run.dungeon);
   model.dungeonName=dungeon?.name??run.dungeon;
   model.backgroundHintDungeonId=run.dungeon;
   model.locationName="";
   model.primaryStats.Add(new RunResultStat("遠征先",model.dungeonName));
   if(run.battlesWon>0)model.primaryStats.Add(new RunResultStat("戦闘勝利",$"{run.battlesWon}"));
   if(clear&&run.gold>0)model.primaryStats.Add(new RunResultStat("持ち帰るゴールド",$"{run.gold}G"));
   if(clear&&run.lootBag!=null&&run.lootBag.Count>0)
    model.primaryStats.Add(new RunResultStat("持ち帰る戦利品",$"{run.lootBag.Count}個"));
   if(!clear&&run.lootBag!=null&&run.lootBag.Count>0)
    model.records.Add(new RunResultStat("持ち帰れなかった戦利品",$"{run.lootBag.Count}個"));
   if(!clear&&run.gold>0)
    model.records.Add(new RunResultStat("持ち帰れなかったゴールド",$"{run.gold}G"));

   if(!string.IsNullOrEmpty(run.heirloomUid)){
    var heir=run.inventory?.FirstOrDefault(x=>x.uid==run.heirloomUid)
     ??game.UiMeta.stash.FirstOrDefault(x=>x.uid==run.heirloomUid);
    if(heir!=null&&GameCatalog.Items.TryGetValue(heir.templateId,out var heirDef)){
     if(heir.uses>0)model.heirloomChanges.Add(new RunResultStat("家宝の使用",$"{heirDef.name}　{heir.uses}回"));
     if(!clear&&heir.scars!=null&&heir.scars.Count>0){
      var latest=heir.scars[^1];
      model.heirloomChanges.Add(new RunResultStat("新しい傷跡",$"{latest.type}　{latest.dungeon} L{latest.floor}"));
     }
    }
   }
  } else {
   model.dungeonName="";
   model.backgroundHintDungeonId="old_spire";
  }
  return model;
 }

 RunResultViewModel BuildPreviewResultViewModel(bool clear){
  var model=new RunResultViewModel{
   resultType=clear?RunResultType.Clear:RunResultType.Defeat,
   preview=true,
   title=clear?"旅の完遂（DEV）":"旅の断章（DEV）",
   subtitle="プレビュー表示",
   dungeonName="古塔の外郭",
   backgroundHintDungeonId="old_spire",
   causeText=clear?"":"探索は途中で閉じられた。",
   messageText=clear?"遠征成功。戦利品をすべて保管しました":"探索終了。戦利品と獲得ゴールドは持ち帰れません",
  };
  model.primaryStats.Add(new RunResultStat("遠征先","古塔の外郭"));
  model.primaryStats.Add(new RunResultStat("戦闘勝利","2"));
  if(clear)model.records.Add(new RunResultStat("持ち帰る戦利品","1個"));
  else model.records.Add(new RunResultStat("持ち帰れなかった戦利品","1個"));
  return model;
 }

 void OpenGameOverPreviewFromDev(){
  DevNavigate(ScreenId.GameOver,()=>resultPreviewMode=true);
 }

 void OpenGameClearPreviewFromDev(){
  DevNavigate(ScreenId.GameClear,()=>resultPreviewMode=true);
 }
}
}
