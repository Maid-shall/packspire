using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire {
public sealed partial class PackspireUiFoundation {
 VisualElement factionShell;
 VisualElement factionGraphHost;
 VisualElement factionGraphEdges;
 VisualElement factionGraphNodes;
 ScrollView factionDetailScroll;
 bool factionGraphEdgesDirty;

#if UNITY_EDITOR
 const bool showAllFactionsForLayoutPreview=true;
#else
 const bool showAllFactionsForLayoutPreview=false;
#endif

 static readonly Dictionary<string,Vector2> FactionGraphLayout=new(){
  ["iron"]=new Vector2(0.24f,0.30f),
  ["spore"]=new Vector2(0.76f,0.24f),
  ["guild"]=new Vector2(0.34f,0.74f),
  ["void"]=new Vector2(0.72f,0.68f),
 };

 static readonly (string a,string b,string kind)[] FactionGraphEdges={
  ("iron","spore","hostile"),
  ("iron","guild","friendly"),
  ("spore","guild","neutral"),
  ("void","iron","hostile"),
  ("void","spore","hostile"),
  ("void","guild","neutral"),
 };

 void BuildFaction(){
  var meta=game.UiMeta;
  if(string.IsNullOrEmpty(selectedFactionId)||!IsFactionVisibleInGraph(meta,selectedFactionId))
   selectedFactionId=VisibleFactions(meta).FirstOrDefault()?.id??meta.currentFaction;

  factionShell=Container("ps-faction-screen ps-dark-surface");
  if(showAllFactionsForLayoutPreview)factionShell.AddToClassList("ps-faction-layout-preview");
  var backgroundHost=Container("ps-layer-background");
  var bg=HubBackgroundArt();
  if(bg==null)bg=CourtyardArt();
  if(bg!=null)backgroundHost.Add(Image(bg,new Rect(0,0,1,1),"ps-mgmt-bg",ScaleMode.ScaleAndCrop));
  var shade=Container("ps-mgmt-shade");
  shade.pickingMode=PickingMode.Ignore;
  backgroundHost.Add(shade);
  factionShell.Add(backgroundHost);

  var contentHost=Container("ps-layer-content");
  var header=Container("ps-mgmt-header");
  header.Add(ChromeBrand("FACTION  /  LEDGER","勢力"));
  contentHost.Add(header);

  var body=Container("ps-faction-body");
  var graphCol=Container("ps-faction-col-graph");
  factionGraphHost=Container("ps-faction-graph-host");
  factionGraphEdges=Container("ps-faction-graph-edges");
  factionGraphEdges.pickingMode=PickingMode.Ignore;
  factionGraphNodes=Container("ps-faction-graph-nodes");
  factionGraphHost.Add(factionGraphEdges);
  factionGraphHost.Add(factionGraphNodes);
  factionGraphHost.RegisterCallback<GeometryChangedEvent>(_=>ScheduleFactionEdgeRefresh());
  graphCol.Add(factionGraphHost);
  body.Add(graphCol);

  var detailCol=Container("ps-faction-col-detail");
  factionDetailScroll=new ScrollView(ScrollViewMode.Vertical);
  factionDetailScroll.AddToClassList("ps-faction-detail-scroll");
  factionDetailScroll.verticalScrollerVisibility=ScrollerVisibility.Auto;
  detailCol.Add(factionDetailScroll);
  body.Add(detailCol);

  contentHost.Add(body);
  factionShell.Add(contentHost);
  screenRoot.Add(factionShell);

  PopulateFactionGraph(meta);
  RefreshFactionDetail(meta);
  ScheduleFactionEdgeRefresh();
 }

 bool FactionLayoutPreviewEnabled(){
#if UNITY_EDITOR
  return showAllFactionsForLayoutPreview;
#else
  return showAllFactionsForLayoutPreview||Debug.isDebugBuild;
#endif
 }

 bool IsFactionDiscovered(MetaSave meta,string factionId){
  if(factionId==meta.currentFaction)return true;
  return (meta.factionRep.FirstOrDefault(x=>x.id==factionId)?.value??0f)>0f;
 }

 bool IsFactionVisibleInGraph(MetaSave meta,string factionId){
  if(FactionLayoutPreviewEnabled())return true;
  return IsFactionDiscovered(meta,factionId);
 }

 IEnumerable<FactionDef> VisibleFactions(MetaSave meta){
  foreach(var faction in GameCatalog.Factions)
   if(IsFactionVisibleInGraph(meta,faction.id))yield return faction;
 }

 void PopulateFactionGraph(MetaSave meta){
  if(factionGraphNodes==null||factionGraphEdges==null)return;
  factionGraphNodes.Clear();
  factionGraphEdges.Clear();
  var visible=VisibleFactions(meta).ToList();
  if(visible.Count==0)return;

  float maxRep=Mathf.Max(1f,visible.Max(f=>meta.factionRep.FirstOrDefault(x=>x.id==f.id)?.value??0f));
  foreach(var faction in visible){
   float rep=meta.factionRep.FirstOrDefault(x=>x.id==faction.id)?.value??0f;
   if(!FactionGraphLayout.TryGetValue(faction.id,out var pos))continue;
   var node=BuildFactionGraphNode(meta,faction,rep,maxRep,pos);
   factionGraphNodes.Add(node);
  }
  ScheduleFactionEdgeRefresh();
 }

 void ScheduleFactionEdgeRefresh(){
  factionGraphEdgesDirty=true;
  factionGraphHost?.schedule.Execute(RefreshFactionEdges).ExecuteLater(0);
 }

 Button FindFactionNode(string factionId){
  if(factionGraphNodes==null)return null;
  foreach(var child in factionGraphNodes.Children()){
   if(child is Button node&&node.userData is string id&&id==factionId)return node;
  }
  return null;
 }

 void RefreshFactionEdges(){
  if(!factionGraphEdgesDirty||factionGraphEdges==null||factionGraphHost==null||factionGraphNodes==null)return;
  factionGraphEdgesDirty=false;
  factionGraphEdges.Clear();
  var hostRect=factionGraphHost.contentRect;
  if(hostRect.width<=1f||hostRect.height<=1f){
   factionGraphEdgesDirty=true;
   factionGraphHost.schedule.Execute(RefreshFactionEdges).ExecuteLater(16);
   return;
  }
  var visible=VisibleFactions(game.UiMeta).Select(x=>x.id).ToHashSet();
  foreach(var edge in FactionGraphEdges){
   if(!visible.Contains(edge.a)||!visible.Contains(edge.b))continue;
   var nodeA=FindFactionNode(edge.a);
   var nodeB=FindFactionNode(edge.b);
   if(nodeA==null||nodeB==null)continue;
   var localA=FactionNodeCenterLocal(nodeA);
   var localB=FactionNodeCenterLocal(nodeB);
   if(localA==Vector2.zero&&localB==Vector2.zero)continue;
   factionGraphEdges.Add(BuildFactionEdgeLocal(localA,localB,edge.kind));
  }
 }

 Vector2 FactionNodeCenterLocal(VisualElement node){
  if(node==null||factionGraphHost==null)return Vector2.zero;
  var layout=node.layout;
  if(layout.width<=0f||layout.height<=0f){
   var bounds=node.worldBound;
   if(bounds.width<=0f)return Vector2.zero;
   return factionGraphHost.WorldToLocal(bounds.center);
  }
  var nodesRect=factionGraphNodes.layout;
  return new Vector2(nodesRect.x+layout.x+layout.width*0.5f,nodesRect.y+layout.y+layout.height*0.5f);
 }

 VisualElement BuildFactionEdgeLocal(Vector2 a,Vector2 b,string kind){
  var line=Container("ps-faction-edge ps-faction-edge-"+kind);
  line.pickingMode=PickingMode.Ignore;
  line.style.position=Position.Absolute;
  float dx=b.x-a.x;
  float dy=b.y-a.y;
  float len=Mathf.Sqrt(dx*dx+dy*dy);
  if(len<1f)return line;
  float angle=Mathf.Atan2(dy,dx)*Mathf.Rad2Deg;
  line.style.left=Length.Pixels(a.x);
  line.style.top=Length.Pixels(a.y);
  line.style.width=Length.Pixels(len);
  line.style.height=kind=="hostile"?3:2;
  line.style.rotate=new Rotate(new Angle(angle,AngleUnit.Degree));
  return line;
 }

 VisualElement BuildFactionGraphNode(MetaSave meta,FactionDef faction,float rep,float maxRep,Vector2 pos){
  float influence=Mathf.Lerp(0.35f,1f,rep/maxRep);
  bool discovered=IsFactionDiscovered(meta,faction.id);
  var node=new Button(()=>SelectFactionNode(faction.id)){tooltip=faction.name,userData=faction.id};
  node.AddToClassList("ps-faction-graph-node");
  node.style.position=Position.Absolute;
  node.style.left=Length.Percent(pos.x*100f);
  node.style.top=Length.Percent(pos.y*100f);
  node.style.translate=new Translate(new Length(-50,LengthUnit.Percent),new Length(-50,LengthUnit.Percent));
  node.style.width=Mathf.RoundToInt(88f+56f*influence);
  node.style.height=Mathf.RoundToInt(88f+56f*influence);
  node.RegisterCallback<GeometryChangedEvent>(_=>ScheduleFactionEdgeRefresh());
  if(faction.id==meta.currentFaction)node.AddToClassList("ps-faction-affiliated");
  if(faction.id==selectedFactionId)node.AddToClassList("ps-selected");
  if(discovered)node.AddToClassList("ps-faction-discovered");
  else if(FactionLayoutPreviewEnabled())node.AddToClassList("ps-faction-preview-undiscovered");
  node.Add(Atlas(game.UiFactionArt,FactionUv(faction.id),"ps-faction-graph-emblem"));
  var name=new Label(faction.name){pickingMode=PickingMode.Ignore};
  name.AddToClassList("ps-faction-graph-name");
  node.Add(name);
  return node;
 }

 void SelectFactionNode(string factionId){
  if(selectedFactionId==factionId){
   RefreshFactionDetail(game.UiMeta);
   return;
  }
  selectedFactionId=factionId;
  UpdateFactionGraphSelection();
  RefreshFactionDetail(game.UiMeta);
 }

 void UpdateFactionGraphSelection(){
  if(factionGraphNodes==null)return;
  var meta=game.UiMeta;
  foreach(var child in factionGraphNodes.Children()){
   if(child is not Button node||node.userData is not string id)continue;
   node.EnableInClassList("ps-selected",id==selectedFactionId);
   node.EnableInClassList("ps-faction-affiliated",id==meta.currentFaction);
  }
 }

 void RefreshFactionDetail(MetaSave meta){
  if(factionDetailScroll==null)return;
  factionDetailScroll.Clear();
  var selected=GameCatalog.Factions.FirstOrDefault(x=>x.id==selectedFactionId);
  if(selected==null||!IsFactionVisibleInGraph(meta,selected.id)){
   factionDetailScroll.Add(PackspireUiFactory.EmptyState("勢力を選択","関係図の紋章を選ぶと詳細が表示されます。"));
   return;
  }
  bool discovered=IsFactionDiscovered(meta,selected.id);
  bool previewUndiscovered=FactionLayoutPreviewEnabled()&&!discovered;
  float value=meta.factionRep.FirstOrDefault(x=>x.id==selected.id)?.value??0f;
  int rank=Mathf.Clamp(Mathf.FloorToInt(value/25f),0,selected.ranks.Length-1);
  int nextRank=Mathf.Min(rank+1,selected.ranks.Length-1);
  float nextThreshold=(rank+1)*25f;

  factionDetailScroll.Add(ChromeBrand("SEAL  /  RANK",selected.name));
  if(previewUndiscovered)
   factionDetailScroll.Add(ManagementSection("プレビュー","未発見（レイアウト確認用・セーブ未変更）"));
  factionDetailScroll.Add(Atlas(game.UiFactionArt,FactionUv(selected.id),"ps-faction-detail-art"));
  factionDetailScroll.Add(PackspireUiFactory.Body(selected.description));
  factionDetailScroll.Add(ManagementSection("現在階級",selected.ranks[rank]));
  factionDetailScroll.Add(ManagementSection("貢献度",$"{value:0} / {nextThreshold:0}"));
  var progress=Container("ps-progress");
  var fill=Container("ps-progress-fill");
  fill.style.width=Length.Percent(Mathf.Clamp01(value/nextThreshold)*100f);
  progress.Add(fill);
  factionDetailScroll.Add(progress);
  if(rank<selected.ranks.Length-1)
   factionDetailScroll.Add(ManagementSection("次の階級",selected.ranks[nextRank]));
  factionDetailScroll.Add(ManagementSection("勢力効果",FactionEffectSummary(selected.id,rank)));
  factionDetailScroll.Add(ManagementSection("関係",FactionRelationSummary(selected.id,meta)));
  if(selected.id==meta.currentFaction)
   factionDetailScroll.Add(ManagementSection("所属","● 現在の所属勢力"));
  else if(discovered){
   var change=PackspireUiFactory.Button("20Gで所属を変更",()=>{
    if(game.UiChangeFaction(selected.id)){
     ShowToast(selected.name+"へ所属を変更しました");
     UpdateFactionGraphSelection();
     RefreshFactionDetail(game.UiMeta);
    }
   });
   change.AddToClassList("ps-chrome-action");
   change.AddToClassList("ps-mgmt-action");
   if(meta.baseGold<20)change.SetEnabled(false);
   factionDetailScroll.Add(change);
  }else if(previewUndiscovered)
   factionDetailScroll.Add(ManagementSection("所属","未発見のため変更不可（プレビュー）"));
 }

 string FactionEffectSummary(string factionId,int rank){
  return factionId switch{
   "iron"=>rank>=2?"防具カードの防御が強化":"防具カードを強化",
   "spore"=>rank>=2?"勝利後の回復が増加":"勝利後の回復を強化",
   "guild"=>rank>=2?"商店価格の割引が拡大":"商店価格を割引",
   "void"=>rank>=2?"高リスク高報酬が増幅":"獲得ゴールドと呪い装備が増加",
   _=>"—"
  };
 }

 string FactionRelationSummary(string factionId,MetaSave meta){
  var lines=new List<string>();
  foreach(var edge in FactionGraphEdges){
   if(edge.a!=factionId&&edge.b!=factionId)continue;
   if(!IsFactionVisibleInGraph(meta,edge.a)||!IsFactionVisibleInGraph(meta,edge.b==factionId?edge.a:edge.b))continue;
   var other=edge.a==factionId?edge.b:edge.a;
   var otherName=GameCatalog.Factions.First(x=>x.id==other).name;
   var label=edge.kind switch{"friendly"=>"友好","hostile"=>"敵対",_=>"中立"};
   lines.Add($"{otherName}　{label}");
  }
  return lines.Count>0?string.Join("\n",lines):"記録なし";
 }
}
}
