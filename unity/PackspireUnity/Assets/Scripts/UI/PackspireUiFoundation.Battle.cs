using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire {
public sealed partial class PackspireUiFoundation {
 bool battleUiBuilt;
 VisualElement battleRoot,battleHandRoot,battlePlayerStatuses,battleEnemyStatuses,battleConsumablesRoot;
 Label battlePlayerName,battleEnemyName,battleEnemyIntent;
 Label battlePlayerHpLabel,battlePlayerBlockLabel,battleEnemyHpLabel,battleEnemyBlockLabel;
 VisualElement battlePlayerHpFill,battlePlayerBlockFill,battleEnemyHpFill,battleEnemyBlockFill;
 Image battlePlayerPortrait,battleEnemyPortrait;
 Label battleSkillMetaLabel;
 Button battleSkillButton,battleEndTurnButton;
 Texture2D battleEnvFarBg,battleEnvMidBg,battleCircleBase;
 Texture2D[] battleCardFrames=new Texture2D[3];

 void EnsureBattleAssets(){
  if(battleEnvFarBg!=null)return;
  battleEnvFarBg=Resources.Load<Texture2D>("Art/RouteKeyed/far-background-v1");
  battleEnvMidBg=Resources.Load<Texture2D>("Art/RouteKeyed/midground-v1");
  battleCircleBase=Resources.Load<Texture2D>("Art/Rite/rite-circle-base-v1");
  for(int i=0;i<3;i++)battleCardFrames[i]=Resources.Load<Texture2D>($"Art/UI/Cards/combat-card-{i:00}");
 }

 void BuildBattle(){
  EnsureBattleAssets();
  battleUiBuilt=true;
  battleRoot=Container("ps-battle-screen");
  screenRoot.Add(battleRoot);
  BuildBattleBackground(battleRoot);

  battleConsumablesRoot=Container("ps-battle-items");
  battleRoot.Add(battleConsumablesRoot);

  battleRoot.Add(BuildBattleActor(false));
  battleRoot.Add(BuildBattleHud(false));
  battleRoot.Add(BuildBattleActor(true));
  battleRoot.Add(BuildBattleHud(true));

  // Skill only — between player and hand
  var skillCol=Container("ps-battle-skill-col");
  battleSkillButton=PackspireUiFactory.Button("スキル",OnBattleSkillClicked);
  battleSkillButton.AddToClassList("ps-battle-btn");
  skillCol.Add(battleSkillButton);
  battleRoot.Add(skillCol);

  // End turn + EN/deck above hand, right-aligned
  var handMeta=Container("ps-battle-hand-meta");
  battleSkillMetaLabel=new Label(""){pickingMode=PickingMode.Ignore};
  battleSkillMetaLabel.AddToClassList("ps-battle-hand-meta-info");
  handMeta.Add(battleSkillMetaLabel);
  battleEndTurnButton=PackspireUiFactory.Button("ターンを終了",()=>game.UiEndBattleTurn());
  battleEndTurnButton.AddToClassList("ps-battle-btn ps-battle-btn-end");
  handMeta.Add(battleEndTurnButton);
  battleRoot.Add(handMeta);

  battleHandRoot=Container("ps-battle-hand");
  battleRoot.Add(battleHandRoot);

  RefreshBattleUi();
 }

 void BuildBattleBackground(VisualElement root){
  if(battleEnvFarBg!=null)
   root.Add(Image(battleEnvFarBg,new Rect(0,0,1,1),"ps-battle-bg ps-battle-bg-far",ScaleMode.ScaleAndCrop));
  if(battleEnvMidBg!=null)
   root.Add(Image(battleEnvMidBg,new Rect(0,0,1,1),"ps-battle-bg ps-battle-bg-mid",ScaleMode.ScaleAndCrop));
  root.Add(Container("ps-battle-bg-dim"));
 }

 VisualElement BuildBattleActor(bool player){
  var wrap=Container(player?"ps-battle-actor ps-battle-actor-player":"ps-battle-actor ps-battle-actor-enemy");
  if(battleCircleBase!=null){
   var pedestal=new Image{image=battleCircleBase,scaleMode=ScaleMode.ScaleToFit,pickingMode=PickingMode.Ignore};
   pedestal.AddToClassList("ps-battle-pedestal");
   if(!player)pedestal.tintColor=new Color(.75f,.72f,.68f,.45f);
   wrap.Add(pedestal);
  }
  if(player){
   battlePlayerPortrait=Atlas(game.UiCharacterArt,new Rect(0,0,1,1),"ps-battle-actor-image");
   wrap.Add(battlePlayerPortrait);
  } else {
   battleEnemyPortrait=Atlas(game.UiEnemyArt,new Rect(0,0,1,1),"ps-battle-actor-image");
   wrap.Add(battleEnemyPortrait);
  }
  return wrap;
 }

 VisualElement BuildBattleHud(bool player){
  if(player){
   var hud=Container("ps-battle-hud ps-battle-hud-player");
   var playerMeta=Container("ps-battle-player-meta");
   battlePlayerName=new Label(""){pickingMode=PickingMode.Ignore};
   battlePlayerName.AddToClassList("ps-battle-hud-name");
   playerMeta.Add(battlePlayerName);
   hud.Add(playerMeta);
   var playerBars=Container("ps-battle-vbars");
   playerBars.Add(BuildMeter(out battlePlayerHpFill,out battlePlayerHpLabel,"ps-battle-meter-hp",true));
   playerBars.Add(BuildMeter(out battlePlayerBlockFill,out battlePlayerBlockLabel,"ps-battle-meter-block",true));
   hud.Add(playerBars);
   battlePlayerStatuses=Container("ps-battle-status-row");
   hud.Add(battlePlayerStatuses);
   return hud;
  }
  var enemyHud=Container("ps-battle-hud ps-battle-hud-enemy");
  var enemyMeta=Container("ps-battle-enemy-meta");
  battleEnemyName=new Label(""){pickingMode=PickingMode.Ignore};
  battleEnemyName.AddToClassList("ps-battle-hud-name");
  enemyMeta.Add(battleEnemyName);
  battleEnemyIntent=new Label(""){pickingMode=PickingMode.Ignore};
  battleEnemyIntent.AddToClassList("ps-battle-hud-meta ps-battle-intent");
  enemyMeta.Add(battleEnemyIntent);
  enemyHud.Add(enemyMeta);
  var enemyBars=Container("ps-battle-vbars");
  enemyBars.Add(BuildMeter(out battleEnemyHpFill,out battleEnemyHpLabel,"ps-battle-meter-hp",true));
  enemyBars.Add(BuildMeter(out battleEnemyBlockFill,out battleEnemyBlockLabel,"ps-battle-meter-block",true));
  enemyHud.Add(enemyBars);
  battleEnemyStatuses=Container("ps-battle-status-row");
  enemyHud.Add(battleEnemyStatuses);
  return enemyHud;
 }

 VisualElement BuildMeter(out VisualElement fill,out Label label,string toneClass,bool vertical){
  var meter=Container(vertical?"ps-battle-meter ps-battle-meter-v "+toneClass:"ps-battle-meter "+toneClass);
  var track=Container("ps-battle-meter-track");
  fill=Container("ps-battle-meter-fill");
  track.Add(fill);
  meter.Add(track);
  label=new Label(""){pickingMode=PickingMode.Ignore};
  label.AddToClassList("ps-battle-meter-label");
  meter.Add(label);
  return meter;
 }

 void OnBattleSkillClicked(){
  if(game.UiUseActiveSkill())return;
  RefreshBattleUi();
 }

 public void RefreshBattleUi(){
  if(!battleUiBuilt||battleRoot==null)return;
  var run=game.UiRun;
  var battle=game.UiBattle;
  if(run==null||battle==null)return;
  var dungeon=GameCatalog.Dungeons.First(x=>x.id==run.dungeon);

  battlePlayerName.text=GameCatalog.Roles.ContainsKey(run.role)?GameCatalog.Roles[run.role].name:run.role;
  if(battlePlayerPortrait!=null){
   var meta=game.UiMeta;
   battlePlayerPortrait.uv=CharacterUv(meta.body,meta.hair);
   battlePlayerPortrait.style.display=DisplayStyle.Flex;
  }
  SetMeter(battlePlayerHpFill,battlePlayerHpLabel,run.hp,run.maxHp,$"{run.hp}/{run.maxHp}",true);
  SetMeter(battlePlayerBlockFill,battlePlayerBlockLabel,run.block,24,$"{run.block}",true);
  RefreshStatuses(battlePlayerStatuses,run.statuses);

  battleEnemyName.text=battle.enemy.name;
  if(battleEnemyPortrait!=null){
   battleEnemyPortrait.uv=EnemyUv(battle.enemy.id);
   battleEnemyPortrait.image=game.UiEnemyArt;
  }
  int intent=battle.enemy.damages[battle.move%battle.enemy.damages.Length]+dungeon.damage;
  battleEnemyIntent.text=$"予告 {intent}";
  SetMeter(battleEnemyHpFill,battleEnemyHpLabel,Mathf.Max(0,battle.enemyHp),battle.enemyMaxHp,$"{Mathf.Max(0,battle.enemyHp)}/{battle.enemyMaxHp}",true);
  SetMeter(battleEnemyBlockFill,battleEnemyBlockLabel,battle.enemyBlock,24,$"{battle.enemyBlock}",true);
  RefreshStatuses(battleEnemyStatuses,battle.enemyStatuses);

  RefreshBattleHand(run);
  RefreshBattleConsumables(run);
  battleSkillMetaLabel.text=$"EN {run.energy}/3　山札 {run.draw.Count}　捨て札 {run.discard.Count}";
  battleSkillButton.text=game.UiActiveSkillAvailable?game.UiActiveSkillLabel:"スキル済";
  battleSkillButton.tooltip=game.UiActiveSkillTooltip;
  battleSkillButton.SetEnabled(game.UiActiveSkillAvailable);
 }

 static void SetMeter(VisualElement fill,Label label,int value,int max,string text,bool vertical){
  if(fill==null||label==null)return;
  float ratio=max<=0?0f:Mathf.Clamp01(value/(float)max);
  if(vertical){
   fill.style.left=2;
   fill.style.right=2;
   fill.style.width=StyleKeyword.Auto;
   fill.style.top=StyleKeyword.Auto;
   fill.style.bottom=2;
   fill.style.height=Length.Percent(ratio*100f);
  } else {
   fill.style.left=2;
   fill.style.right=StyleKeyword.Auto;
   fill.style.top=2;
   fill.style.bottom=2;
   fill.style.height=StyleKeyword.Auto;
   fill.style.width=Length.Percent(ratio*100f);
  }
  label.text=text;
 }

 void RefreshStatuses(VisualElement row,List<StatusState> statuses){
  if(row==null)return;
  row.Clear();
  if(statuses==null||statuses.Count==0)return;
  foreach(var status in statuses.Take(7)){
   var def=ContentDatabase.Status(status.type);
   bool debuff=def!=null&&def.kind=="debuff";
   var chip=Container(debuff?"ps-battle-status-chip ps-battle-status-debuff":"ps-battle-status-chip ps-battle-status-buff");
   chip.tooltip=def!=null?def.name:status.type;
   chip.Add(new Label(def?.icon??"●"){pickingMode=PickingMode.Ignore});
   var count=new Label(status.amount.ToString()){pickingMode=PickingMode.Ignore};
   count.AddToClassList("ps-battle-status-count");
   chip.Add(count);
   row.Add(chip);
  }
 }

 void RefreshBattleHand(RunState run){
  if(battleHandRoot==null)return;
  battleHandRoot.Clear();
  if(run.hand.Count==0)return;
  int count=run.hand.Count;
  float center=(count-1)*0.5f;
  // 5枚はゆるい扇、6–7枚は弧をきつめに
  float spreadDeg=count<=5?6.2f:count==6?7.4f:count==7?7.0f:5.0f;
  float radius=count<=5?110f:count==6?205f:count==7?198f:155f;
  float horizontalStep=count<=5?92f:count==6?78f:count==7?66f:58f;
  var handSlots=new List<(Button button,float depth)>(count);
  for(int i=0;i<count;i++){
   int index=i;
   var card=run.hand[index];
   bool affordable=card.cost<=run.energy;
   var button=new Button(()=>{
    if(!affordable)return;
    game.UiPlayBattleCard(index);
   });
   button.AddToClassList("ps-battle-card");
   if(!affordable)button.AddToClassList("ps-battle-card-disabled");
   PopulateBattleCard(button,card,run,affordable);
   float spreadIndex=i-center;
   float angle=spreadIndex*spreadDeg;
   float rad=angle*Mathf.Deg2Rad;
   float arcLift=radius*(1f-Mathf.Cos(rad));
   float baseRight=(count-1-i)*horizontalStep;
   float arcShift=radius*Mathf.Sin(rad);
   button.style.position=Position.Absolute;
   button.style.right=baseRight-arcShift;
   button.style.bottom=arcLift;
   button.style.rotate=new Rotate(new Angle(angle,AngleUnit.Degree));
   button.style.transformOrigin=new TransformOrigin(new Length(50,LengthUnit.Percent),new Length(100,LengthUnit.Percent));
   button.RegisterCallback<PointerEnterEvent>(_=>button.BringToFront());
   handSlots.Add((button,Mathf.Abs(spreadIndex)));
  }
  foreach(var slot in handSlots.OrderByDescending(x=>x.depth))
   battleHandRoot.Add(slot.button);
 }

 void PopulateBattleCard(VisualElement slot,CardInstance card,RunState run,bool affordable){
  int frameIndex=card.type==CardType.Attack?0:card.type==CardType.Skill?1:2;
  Texture2D frame=frameIndex<battleCardFrames.Length?battleCardFrames[frameIndex]:null;
  if(frame!=null){
   slot.style.backgroundImage=new StyleBackground(frame);
   slot.style.unityBackgroundScaleMode=ScaleMode.StretchToFill;
  }
  var cost=new Label(card.cost.ToString()){pickingMode=PickingMode.Ignore};
  cost.AddToClassList("ps-battle-card-cost");
  slot.Add(cost);
  var illustration=Container("ps-battle-card-art");
  var sourceItem=run.inventory.FirstOrDefault(x=>x.uid==card.sourceItemUid);
  if(sourceItem!=null)
   illustration.Add(Atlas(game.UiEquipmentArt,ItemUv(sourceItem.templateId),"ps-battle-card-art-image"));
  else if(!string.IsNullOrEmpty(run.role))
   illustration.Add(Atlas(game.UiRoleArt,RoleUv(run.role),"ps-battle-card-art-image"));
  if(!affordable)illustration.AddToClassList("ps-battle-card-art-disabled");
  slot.Add(illustration);
  var name=new Label(card.name){pickingMode=PickingMode.Ignore};
  name.AddToClassList("ps-battle-card-name");
  slot.Add(name);
  var body=new Label(card.text){pickingMode=PickingMode.Ignore};
  body.AddToClassList("ps-battle-card-text");
  slot.Add(body);
  string sourceName=card.source;
  if(sourceItem!=null&&GameCatalog.Items.TryGetValue(sourceItem.templateId,out var itemDef))sourceName=itemDef.name;
  var foot=Container("ps-battle-card-foot");
  var source=new Label(sourceName){pickingMode=PickingMode.Ignore};
  source.AddToClassList("ps-battle-card-source");
  foot.Add(source);
  string durability=sourceItem!=null?$"耐久 {sourceItem.durability}/6":card.roleCard?"役職":"基本";
  var dur=new Label(durability){pickingMode=PickingMode.Ignore};
  dur.AddToClassList(sourceItem!=null&&sourceItem.durability<=1?"ps-battle-card-durability ps-battle-card-durability-low":"ps-battle-card-durability");
  foot.Add(dur);
  slot.Add(foot);
  if(!affordable){
   var lockLabel=new Label("EN不足"){pickingMode=PickingMode.Ignore};
   lockLabel.AddToClassList("ps-battle-card-lock");
   slot.Add(lockLabel);
  }
 }

 void RefreshBattleConsumables(RunState run){
  if(battleConsumablesRoot==null)return;
  battleConsumablesRoot.Clear();
  if(run.consumables.Count==0)return;
  for(int i=0;i<run.consumables.Count;i++){
   int index=i;
   string id=run.consumables[index];
   var button=new Button(()=>game.UiUseBattleConsumable(index));
   button.AddToClassList("ps-battle-cons");
   button.tooltip=ConsumableSystem.Name(id);
   string visual=id=="heal"?"herb":id=="guard"?"buckler":id=="fire"?"bomb":"flask";
   button.Add(Atlas(game.UiEquipmentArt,ItemUv(visual),"ps-battle-cons-art"));
   battleConsumablesRoot.Add(button);
  }
 }

 void SuspendBattleUi(){
  battleUiBuilt=false;
  battleRoot=null;
  battleHandRoot=null;
  battlePlayerStatuses=null;
  battleEnemyStatuses=null;
  battleConsumablesRoot=null;
  battlePlayerName=null;
  battleEnemyName=null;
  battleEnemyIntent=null;
  battlePlayerHpLabel=null;
  battlePlayerBlockLabel=null;
  battleEnemyHpLabel=null;
  battleEnemyBlockLabel=null;
  battlePlayerHpFill=null;
  battlePlayerBlockFill=null;
  battleEnemyHpFill=null;
  battleEnemyBlockFill=null;
  battleSkillMetaLabel=null;
  battleSkillButton=null;
  battleEndTurnButton=null;
  battlePlayerPortrait=null;
  battleEnemyPortrait=null;
 }
}
}
