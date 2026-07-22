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
 VisualElement factionDetailHeader;
 ScrollView factionDetailScroll;
 bool factionGraphEdgesDirty;
 bool factionEdgeLayoutReady;
 Vector2 factionGraphLastSize;

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
  AddSurfaceOuterCorners(factionGraphHost);
  factionGraphEdges=Container("ps-faction-graph-edges");
  factionGraphEdges.pickingMode=PickingMode.Ignore;
  factionGraphNodes=Container("ps-faction-graph-nodes");
  factionGraphHost.Add(factionGraphEdges);
  factionGraphHost.Add(factionGraphNodes);
  factionGraphHost.RegisterCallback<GeometryChangedEvent>(OnFactionGraphGeometryChanged);
  graphCol.Add(factionGraphHost);
  factionEdgeLayoutReady=false;
  factionGraphLastSize=Vector2.zero;
  body.Add(graphCol);

  var detailCol=Container("ps-faction-col-detail");
  var detailSurface=Container("ps-faction-detail-surface");
  factionDetailHeader=Container("ps-faction-detail-header");
  detailSurface.Add(factionDetailHeader);
  factionDetailScroll=new ScrollView(ScrollViewMode.Vertical);
  factionDetailScroll.AddToClassList("ps-faction-detail-scroll");
  factionDetailScroll.verticalScrollerVisibility=ScrollerVisibility.Auto;
  StretchMgmtScrollContent(factionDetailScroll,false);
  detailSurface.Add(factionDetailScroll);
  detailCol.Add(detailSurface);
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
  factionEdgeLayoutReady=false;
  factionGraphLastSize=Vector2.zero;
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

 void OnFactionGraphGeometryChanged(GeometryChangedEvent evt){
  if(factionGraphHost==null)return;
  var size=factionGraphHost.contentRect.size;
  if(factionEdgeLayoutReady
   &&Mathf.Abs(size.x-factionGraphLastSize.x)<0.5f
   &&Mathf.Abs(size.y-factionGraphLastSize.y)<0.5f)
   return;
  ScheduleFactionEdgeRefresh();
 }

 void ScheduleFactionEdgeRefresh(){
  if(factionGraphEdgesDirty)return;
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
  var hostRect=factionGraphHost.contentRect;
  if(hostRect.width<=1f||hostRect.height<=1f){
   factionEdgeLayoutReady=false;
   factionGraphEdgesDirty=true;
   factionGraphHost.schedule.Execute(RefreshFactionEdges).ExecuteLater(16);
   return;
  }
  // Centers must resolve; if any node is still unlaid out, retry once next frame.
  foreach(var child in factionGraphNodes.Children()){
   if(child is not Button)continue;
   if(child.worldBound.width<=0.5f||child.worldBound.height<=0.5f){
    factionEdgeLayoutReady=false;
    factionGraphEdgesDirty=true;
    factionGraphHost.schedule.Execute(RefreshFactionEdges).ExecuteLater(16);
    return;
   }
  }
  factionGraphEdges.Clear();
  factionGraphLastSize=hostRect.size;
  factionEdgeLayoutReady=true;
  var visible=VisibleFactions(game.UiMeta).Select(x=>x.id).ToHashSet();
  foreach(var edge in FactionGraphEdges){
   if(!visible.Contains(edge.a)||!visible.Contains(edge.b))continue;
   var nodeA=FindFactionNode(edge.a);
   var nodeB=FindFactionNode(edge.b);
   if(nodeA==null||nodeB==null)continue;
   var localA=FactionNodeCenterInGraph(nodeA);
   var localB=FactionNodeCenterInGraph(nodeB);
   if(localA==Vector2.zero||localB==Vector2.zero)continue;
   // Connect visual centers only — no radius offset.
   factionGraphEdges.Add(BuildFactionEdgeLocal(localA,localB,edge.kind,edge.a,edge.b));
  }
 }

 // Always convert via worldBound → graphHost local. Never mix layout+translate math.
 Vector2 FactionNodeCenterInGraph(VisualElement node){
  if(node==null||factionGraphHost==null)return Vector2.zero;
  var bounds=node.worldBound;
  if(bounds.width<=0.5f||bounds.height<=0.5f)return Vector2.zero;
  return factionGraphHost.WorldToLocal(bounds.center);
 }

 VisualElement BuildFactionEdgeLocal(Vector2 a,Vector2 b,string kind,string idA,string idB){
  var line=Container("ps-faction-edge ps-faction-edge-"+kind);
  line.pickingMode=PickingMode.Ignore;
  line.style.position=Position.Absolute;
  line.userData=$"{idA}|{idB}|{kind}";
  if(idA==selectedFactionId||idB==selectedFactionId)
   line.AddToClassList("ps-faction-edge-active");
  float dx=b.x-a.x;
  float dy=b.y-a.y;
  float len=Mathf.Sqrt(dx*dx+dy*dy);
  if(len<1f)return line;
  float angle=Mathf.Atan2(dy,dx)*Mathf.Rad2Deg;
  float thickness=kind=="hostile"?3f:kind=="neutral"?1f:2f;
  line.style.left=Length.Pixels(a.x);
  line.style.top=Length.Pixels(a.y-thickness*0.5f);
  line.style.width=Length.Pixels(len);
  line.style.height=thickness;
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
  UpdateFactionEdgeEmphasis();
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

 void UpdateFactionEdgeEmphasis(){
  if(factionGraphEdges==null)return;
  foreach(var child in factionGraphEdges.Children()){
   bool active=false;
   if(child.userData is string key){
    var parts=key.Split('|');
    if(parts.Length>=2)
     active=parts[0]==selectedFactionId||parts[1]==selectedFactionId;
   }
   child.EnableInClassList("ps-faction-edge-active",active);
  }
 }

 void RefreshFactionDetail(MetaSave meta){
  if(factionDetailScroll==null||factionDetailHeader==null)return;
  factionDetailHeader.Clear();
  factionDetailScroll.Clear();
  factionDetailScroll.scrollOffset=Vector2.zero;
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

  var art=Atlas(game.UiFactionArt,FactionUv(selected.id),"ps-faction-detail-art");
  art.pickingMode=PickingMode.Ignore;
  factionDetailHeader.Add(art);
  var identity=Container("ps-faction-detail-identity");
  var name=PackspireUiFactory.Title(selected.name);
  name.AddToClassList("ps-faction-detail-name");
  identity.Add(name);
  identity.Add(PackspireUiFactory.Body($"階級　{selected.ranks[rank]}"));
  if(selected.id==meta.currentFaction){
   var stamp=Container("ps-seal-mark");
   stamp.pickingMode=PickingMode.Ignore;
   stamp.Add(new Label("所属中"){pickingMode=PickingMode.Ignore});
   identity.Add(stamp);
  }else if(previewUndiscovered)
   identity.Add(PackspireUiFactory.Body("未発見（プレビュー）"));
  factionDetailHeader.Add(identity);

  var body=Container("ps-faction-detail-body");
  if(previewUndiscovered)
   body.Add(ManagementSection("プレビュー","レイアウト確認用・セーブ未変更"));
  body.Add(ManagementSection("概要",selected.description));
  body.Add(ManagementSection("貢献度",$"{value:0} / {nextThreshold:0}"));
  var progress=Container("ps-progress");
  var fill=Container("ps-progress-fill");
  fill.style.width=Length.Percent(Mathf.Clamp01(value/nextThreshold)*100f);
  progress.Add(fill);
  body.Add(progress);
  if(rank<selected.ranks.Length-1)
   body.Add(ManagementSection("次の階級",selected.ranks[nextRank]));
  body.Add(ManagementSection("勢力効果",FactionEffectSummary(selected.id,rank)));

  var relations=Container("ps-faction-relations");
  relations.Add(ManagementSection("各勢力との関係",""));
  foreach(var edge in FactionGraphEdges){
   if(edge.a!=selected.id&&edge.b!=selected.id)continue;
   var otherId=edge.a==selected.id?edge.b:edge.a;
   if(!IsFactionVisibleInGraph(meta,otherId))continue;
   var otherName=GameCatalog.Factions.First(x=>x.id==otherId).name;
   var label=edge.kind switch{"friendly"=>"友好","hostile"=>"敵対",_=>"中立"};
   var row=Container("ps-faction-relation-row");
   var other=new Label(otherName){pickingMode=PickingMode.Ignore};
   other.AddToClassList("ps-typo-item");
   var kind=new Label(label){pickingMode=PickingMode.Ignore};
   kind.AddToClassList("ps-typo-secondary");
   kind.AddToClassList("ps-faction-relation-kind-"+edge.kind);
   row.Add(other);
   row.Add(kind);
   relations.Add(row);
  }
  if(relations.childCount<=1)
   relations.Add(PackspireUiFactory.Body("記録なし"));
  body.Add(relations);

  if(selected.id!=meta.currentFaction&&discovered){
   var change=PackspireUiFactory.Button("20Gで所属を変更",()=>{
    if(game.UiChangeFaction(selected.id)){
     ShowToast(selected.name+"へ所属を変更しました");
     UpdateFactionGraphSelection();
     UpdateFactionEdgeEmphasis();
     RefreshFactionDetail(game.UiMeta);
    }
   });
   change.AddToClassList("ps-action-secondary");
   change.AddToClassList("ps-mgmt-action");
   if(meta.baseGold<20)change.SetEnabled(false);
   body.Add(change);
  }

  var tail=Container("ps-space-scroll-tail");
  tail.pickingMode=PickingMode.Ignore;
  body.Add(tail);
  factionDetailScroll.Add(body);
  factionDetailScroll.scrollOffset=Vector2.zero;
  factionDetailScroll.schedule.Execute(()=>{if(factionDetailScroll!=null)factionDetailScroll.scrollOffset=Vector2.zero;}).ExecuteLater(0);
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
