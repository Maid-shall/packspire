using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Packspire {
public class ItemAnalysis {
 public bool active=true;
 public Dictionary<Element,int> matches=new();
 public List<Vector2Int> cells=new();
}

public class DeckBuildResult {
 public List<CardInstance> roleCards=new(),candidates=new(),deck=new();
 public Dictionary<Element,int> colors=new();
 public List<string> links=new();
 public ActiveStorageFormula formula;
 public StabilityEval stability;
}

/// <summary>Packing / deck build pipeline. Board rules come from the active storage formula.</summary>
public static class BackpackSystem {
 public const int Width=6,Height=4;

 public static ActiveStorageFormula Formula(RunState run)=>StorageFormulaSystem.Resolve(run);
 public static int GridWidth(RunState run)=>Formula(run).core.width;
 public static int GridHeight(RunState run)=>Formula(run).core.height;

 public static List<(Vector2Int pos,int original,Element element,int value)> Layout(ItemDef item,int rotation,ItemInstance instance=null){
  var raw=item.cells.Select((c,i)=>(
   p:new Vector2Int(c.x,c.y),
   original:i,
   element:instance!=null&&instance.colors!=null&&i<instance.colors.Count?instance.colors[i]:c.element,
   value:c.value
  )).ToList();
  for(int r=0;r<((rotation%4)+4)%4;r++)
   raw=raw.Select(x=>(p:new Vector2Int(-x.p.y,x.p.x),original:x.original,element:x.element,value:x.value)).ToList();
  int minX=raw.Min(x=>x.p.x),minY=raw.Min(x=>x.p.y);
  return raw.Select(x=>(x.p-new Vector2Int(minX,minY),x.original,x.element,x.value)).ToList();
 }

 public static bool CanPlace(RunState run,ItemInstance item,int anchor,int rotation,string ignoreUid=null){
  var formula=Formula(run);
  int width=formula.core.width,height=formula.core.height;
  rotation=((rotation%4)+4)%4;
  if(!StorageFormulaSystem.IsRotationAllowed(formula.core.rotation,rotation))return false;

  var occupied=new HashSet<int>();
  foreach(var p in run.placements.Where(x=>x.itemUid!=ignoreUid)){
   var inst=run.inventory.FirstOrDefault(x=>x.uid==p.itemUid);
   if(inst==null)continue;
   foreach(var c in Layout(GameCatalog.Items[inst.templateId],p.rotation,inst))
    occupied.Add(p.anchor+c.pos.y*width+c.pos.x);
  }

  int ax=anchor%width,ay=anchor/width;
  foreach(var c in Layout(GameCatalog.Items[item.templateId],rotation,item)){
   int x=ax+c.pos.x,y=ay+c.pos.y;
   if(x<0||x>=width||y<0||y>=height||occupied.Contains(y*width+x))return false;
  }
  return true;
 }

 public static ItemAnalysis Analyze(ItemInstance instance,Placement p,RunState run=null){
  var formula=run!=null?Formula(run):default;
  var core=run!=null?formula.core:StorageFormulaCatalog.Core(StorageFormulaCatalog.DefaultCoreId);
  int width=core.width;
  var def=GameCatalog.Items[instance.templateId];
  var a=new ItemAnalysis();
  int ax=p.anchor%width,ay=p.anchor/width;
  foreach(var c in Layout(def,p.rotation,instance)){
   int x=ax+c.pos.x,y=ay+c.pos.y,index=y*width+x;
   a.cells.Add(new Vector2Int(x,y));
   if(index>=0&&index<core.board.Length&&core.board[index]==c.element){
    a.matches.TryGetValue(c.element,out int current);
    a.matches[c.element]=current+1;
   }
  }
  return a;
 }

 public static bool Adjacent(RunState run,Placement a,Placement b){
  var ia=run.inventory.First(x=>x.uid==a.itemUid);
  var ib=run.inventory.First(x=>x.uid==b.itemUid);
  var ca=Analyze(ia,a,run).cells;
  var cb=Analyze(ib,b,run).cells;
  return ca.Any(x=>cb.Any(y=>Mathf.Abs(x.x-y.x)+Mathf.Abs(x.y-y.y)==1));
 }

 public static DeckBuildResult Build(RunState run){
  var formula=Formula(run);
  var result=new DeckBuildResult{roleCards=RoleCards(run.role),formula=formula,stability=StorageFormulaSystem.EvaluateStability(run,formula)};
  foreach(Element e in System.Enum.GetValues(typeof(Element)))result.colors[e]=0;

  var analyses=new Dictionary<string,ItemAnalysis>();
  foreach(var p in run.placements){
   var item=run.inventory.FirstOrDefault(x=>x.uid==p.itemUid);
   if(item==null||!GameCatalog.Items.ContainsKey(item.templateId))continue;
   StorageFormulaSystem.EnsureItemRolled(item);
   var analysis=Analyze(item,p,run);
   analyses[item.uid]=analysis;
   foreach(var pair in analysis.matches)result.colors[pair.Key]+=pair.Value;
  }

  foreach(var p in run.placements){
   var item=run.inventory.FirstOrDefault(x=>x.uid==p.itemUid);
   if(item==null)continue;
   var def=GameCatalog.Items[item.templateId];
   var neighbors=run.placements
    .Where(other=>other!=p&&Adjacent(run,p,other))
    .Select(other=>run.inventory.First(x=>x.uid==other.itemUid).templateId)
    .ToList();
   var ids=StorageFormulaSystem.ResolveCardIds(def,neighbors,formula.resonance);
   for(int copy=0;copy<ids.Length;copy++){
    if(!GameCatalog.Cards.ContainsKey(ids[copy]))continue;
    var card=FromDef(GameCatalog.Cards[ids[copy]],def.name,item.uid);
    card.slotKey=$"{item.uid}:{ids[copy]}:{copy}";
    result.candidates.Add(card);
   }
  }

  StorageFormulaSystem.ApplyResonanceLinks(run,formula.resonance,result.candidates,result.links);
  StorageFormulaSystem.ApplyConduitBonuses(formula.conduit,result.colors,result.candidates);
  StorageFormulaSystem.ApplyColorTraits(run,result.colors,result.candidates);

  foreach(var card in result.candidates){
   var item=run.inventory.FirstOrDefault(x=>x.uid==card.sourceItemUid);
   if(item==null)continue;
   card.damage+=item.temper;
   card.block+=item.temper;
   card.heal+=item.temper;
   if(item.durability<=0){
    card.damage=(card.damage+1)/2;
    card.block=(card.block+1)/2;
   }
   if(result.stability.runaway){
    card.damage=Mathf.FloorToInt(card.damage*result.stability.cardPenalty);
    card.block=Mathf.FloorToInt(card.block*result.stability.cardPenalty);
   }
  }

  var valid=result.candidates.Select(x=>x.slotKey).ToHashSet();
  run.selectedCardSlots=run.selectedCardSlots.Where(valid.Contains).ToList();
  if(run.selectedCardSlots.Count==0&&result.candidates.Count>0)
   run.selectedCardSlots=result.candidates.Select(x=>x.slotKey).ToList();
  var selected=run.selectedCardSlots.ToHashSet();
  result.deck.AddRange(result.roleCards);
  result.deck.AddRange(result.candidates.Where(x=>selected.Contains(x.slotKey)));
  return result;
 }

 public static List<CardInstance> BuildDeck(RunState run)=>Build(run).deck;

 public static CardInstance FromDef(CardDef d,string source,string uid){
  var card=new CardInstance{
   id=d.id,name=d.name,text=d.text,type=d.type,cost=d.cost,damage=d.damage,block=d.block,heal=d.heal,
   buff=d.buff,energy=d.energy,selfDamage=d.selfDamage,exhaust=d.exhaust,source=source,sourceItemUid=uid
  };
  card.effects=ContentDatabase.CardEffects(d.id);
  return card;
 }

 static List<CardInstance> RoleCards(string role){
  string archetype=new[]{"guardian","bulwark","anchor_knight","iron_vanguard","pack_saint"}.Contains(role)?"guardian"
   :new[]{"scout","hunter","quickblade","grid_dancer","spore_druid"}.Contains(role)?"scout"
   :new[]{"artificer","grand_artificer","rune_weaver","siege_channeler","guild_factor"}.Contains(role)?"artificer"
   :"warrior";
  string[] ids=archetype=="guardian"?new[]{"basicStrike","basicGuard","basicGuard","basicGuard"}
   :archetype=="scout"?new[]{"basicStrike","basicStrike","basicStrike","basicTactic"}
   :archetype=="artificer"?new[]{"basicStrike","basicGuard","basicTactic","basicTactic"}
   :new[]{"basicStrike","basicStrike","basicGuard","basicGuard"};
  return ids.Select((id,i)=>{
   string roleName=GameCatalog.Roles.TryGetValue(role,out var roleDef)?roleDef.name:role;
   var c=FromDef(GameCatalog.Cards[id],roleName+"の基本技","role-"+i);
   c.roleCard=true;
   return c;
  }).ToList();
 }
}
}
