using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire {
public sealed partial class PackspireUiFoundation {
 bool battleUiBuilt,battleInputLocked;
 VisualElement battleRoot,battleHandRoot,battlePlayerStatuses,battleEnemyStatuses,battleConsumablesRoot;
 VisualElement battleFxLayer,battlePlayerActor,battleEnemyActor,battleIntentBadge;
 Label battlePlayerName,battleEnemyName;
 Label battlePlayerHpLabel,battlePlayerBlockLabel,battleEnemyHpLabel,battleEnemyBlockLabel;
 VisualElement battlePlayerHpFill,battlePlayerBlockFill,battleEnemyHpFill,battleEnemyBlockFill;
 Image battlePlayerPortrait,battleEnemyPortrait,battleIntentIcon;
 Label battleIntentKind,battleIntentValue,battleIntentHint;
 Label battleSkillMetaLabel;
 Button battleSkillButton,battleEndTurnButton;
 Texture2D battleEnvFarBg,battleEnvMidBg,battleSceneBg;
 Texture2D[] battleCardFrames=new Texture2D[3];
 Texture2D battleIconDamage,battleIconBlock,battleIconHeal,battleIconEnergy,battleIconClaw;
 Texture2D battlePlateWide,battlePlateHex,battleMeterFrame;
 int battleFloaterSerial;
 bool battleStartBannerShown;

 void EnsureBattleAssets(){
  if(battleSceneBg!=null&&battleIconDamage!=null&&battlePlateHex!=null)return;
  battleSceneBg=Resources.Load<Texture2D>("Art/Battle/battle-bg-forest-ground-v1");
  battleEnvFarBg=Resources.Load<Texture2D>("Art/RouteKeyed/far-background-v1");
  battleEnvMidBg=Resources.Load<Texture2D>("Art/RouteKeyed/midground-v1");
  battleIconDamage=Resources.Load<Texture2D>("Art/Battle/Icons/icon-damage");
  battleIconBlock=Resources.Load<Texture2D>("Art/Battle/Icons/icon-block");
  battleIconHeal=Resources.Load<Texture2D>("Art/Battle/Icons/icon-heal");
  battleIconEnergy=Resources.Load<Texture2D>("Art/Battle/Icons/icon-energy");
  battleIconClaw=Resources.Load<Texture2D>("Art/Battle/Icons/icon-claw");
  battlePlateWide=Resources.Load<Texture2D>("Art/UI/PopDark/btn-wide-v1")
   ??Resources.Load<Texture2D>("Art/UI/ChromeDD/btn-plate-wide")
   ??Resources.Load<Texture2D>("Art/Battle/Chrome/btn-plate-wide");
  battlePlateHex=Resources.Load<Texture2D>("Art/Battle/Chrome/btn-plate-hex");
  battleMeterFrame=Resources.Load<Texture2D>("Art/Battle/Chrome/meter-frame-v");
  for(int i=0;i<3;i++)battleCardFrames[i]=Resources.Load<Texture2D>($"Art/UI/Cards/combat-card-{i:00}");
 }

 void BuildBattle(){
  EnsureBattleAssets();
  battleUiBuilt=true;
  battleRoot=Container("ps-battle-screen ps-battle");
  screenRoot.Add(battleRoot);
  BuildBattleBackground(battleRoot);

  battleConsumablesRoot=Container("ps-battle-items");
  battleRoot.Add(battleConsumablesRoot);

  battleRoot.Add(BuildBattleActor(false));
  battleRoot.Add(BuildBattleHud(false));
  battleRoot.Add(BuildBattleActor(true));
  battleRoot.Add(BuildBattleHud(true));
  battleRoot.Add(BuildBattleIntentBadge());

  battleFxLayer=Container("ps-battle-fx-layer");
  battleFxLayer.pickingMode=PickingMode.Ignore;
  battleRoot.Add(battleFxLayer);

  // Hand first (above enemy HUD when overlapping), then chrome under the cards
  battleHandRoot=Container("ps-battle-hand");
  battleHandRoot.pickingMode=PickingMode.Ignore;
  battleRoot.Add(battleHandRoot);

  // EN / END TURN sit under the card fan
  var handMeta=Container("ps-battle-hand-meta");
  handMeta.style.backgroundColor=new Color(0.05f,0.035f,0.02f,0.94f);
  handMeta.style.borderTopColor=new Color(0.46f,0.32f,0.17f,1f);
  handMeta.style.borderBottomColor=new Color(0.12f,0.08f,0.05f,1f);
  handMeta.style.borderLeftColor=new Color(0.32f,0.21f,0.13f,1f);
  handMeta.style.borderRightColor=new Color(0.18f,0.12f,0.07f,1f);
  handMeta.style.borderTopWidth=2;
  handMeta.style.borderBottomWidth=2;
  handMeta.style.borderLeftWidth=2;
  handMeta.style.borderRightWidth=2;
  handMeta.style.paddingTop=6;
  handMeta.style.paddingBottom=6;
  handMeta.style.paddingLeft=10;
  handMeta.style.paddingRight=10;
  battleSkillMetaLabel=new Label(""){pickingMode=PickingMode.Ignore};
  battleSkillMetaLabel.AddToClassList("ps-battle-hand-meta-info");
  handMeta.Add(battleSkillMetaLabel);
  battleEndTurnButton=PackspireUiFactory.Button("END TURN",()=>{
   if(battleInputLocked)return;
   game.UiEndBattleTurn();
  });
  battleEndTurnButton.AddToClassList("ps-battle-btn");
  battleEndTurnButton.AddToClassList("ps-battle-btn-end");
  ApplyBattlePlate(battleEndTurnButton,battlePlateWide,ScaleMode.StretchToFill);
  battleEndTurnButton.style.width=200;
  battleEndTurnButton.style.height=56;
  battleEndTurnButton.style.minWidth=200;
  battleEndTurnButton.style.minHeight=56;
  battleEndTurnButton.style.fontSize=18;
  battleEndTurnButton.style.color=new Color(0.77f,0.66f,0.38f,1f);
  battleEndTurnButton.style.unityTextOutlineColor=new Color(0.03f,0.015f,0.01f,0.95f);
  battleEndTurnButton.style.unityTextOutlineWidth=1;
  handMeta.Add(battleEndTurnButton);
  battleRoot.Add(handMeta);

  // Skill last so it stays above hand/actors for hit-testing
  var skillCol=Container("ps-battle-skill-col");
  skillCol.pickingMode=PickingMode.Ignore;
  battleSkillButton=PackspireUiFactory.Button("SKILL",OnBattleSkillClicked);
  battleSkillButton.AddToClassList("ps-battle-btn");
  battleSkillButton.AddToClassList("ps-battle-btn-skill");
  battleSkillButton.pickingMode=PickingMode.Position;
  ApplyBattlePlate(battleSkillButton,battlePlateHex,ScaleMode.StretchToFill);
  // Force size in code — USS alone was missed when classes were space-joined.
  battleSkillButton.style.width=150;
  battleSkillButton.style.height=150;
  battleSkillButton.style.minWidth=150;
  battleSkillButton.style.minHeight=150;
  battleSkillButton.style.fontSize=22;
  battleSkillButton.style.color=new Color(0.77f,0.66f,0.38f,1f);
  battleSkillButton.style.unityTextOutlineColor=new Color(0.03f,0.015f,0.01f,0.95f);
  battleSkillButton.style.unityTextOutlineWidth=1;
  skillCol.Add(battleSkillButton);
  battleRoot.Add(skillCol);

  RefreshBattleUi();
  ShowBattleStartBanner();
 }

 void BuildBattleBackground(VisualElement root){
  if(battleSceneBg!=null)
   root.Add(Image(battleSceneBg,new Rect(0,0,1,1),"ps-battle-bg ps-battle-bg-scene",ScaleMode.ScaleAndCrop));
  else {
   if(battleEnvFarBg!=null)
    root.Add(Image(battleEnvFarBg,new Rect(0,0,1,1),"ps-battle-bg ps-battle-bg-far",ScaleMode.ScaleAndCrop));
   if(battleEnvMidBg!=null)
    root.Add(Image(battleEnvMidBg,new Rect(0,0,1,1),"ps-battle-bg ps-battle-bg-mid",ScaleMode.ScaleAndCrop));
  }
  var dim=Container("ps-battle-bg-dim");
  dim.pickingMode=PickingMode.Ignore;
  root.Add(dim);
  var veil=Container("ps-battle-bg-ground-veil");
  veil.pickingMode=PickingMode.Ignore;
  root.Add(veil);
 }

 void ShowBattleStartBanner(){
  if(!battleUiBuilt||battleRoot==null||battleStartBannerShown)return;
  battleStartBannerShown=true;
  var banner=Container("ps-battle-start-banner");
  banner.pickingMode=PickingMode.Ignore;
  var title=new Label("BATTLE START"){pickingMode=PickingMode.Ignore};
  title.AddToClassList("ps-battle-start-title");
  banner.Add(title);
  var enemy=game.UiBattle?.enemy?.name;
  if(!string.IsNullOrEmpty(enemy)){
   var sub=new Label($"VS  {enemy}"){pickingMode=PickingMode.Ignore};
   sub.AddToClassList("ps-battle-start-sub");
   banner.Add(sub);
  }
  battleRoot.Add(banner);
  banner.schedule.Execute(()=>banner.AddToClassList("ps-battle-start-banner-in")).StartingIn(30);
  banner.schedule.Execute(()=>banner.AddToClassList("ps-battle-start-banner-out")).StartingIn(1200);
  banner.schedule.Execute(()=>banner.RemoveFromHierarchy()).StartingIn(1750);
 }

 VisualElement BuildBattleActor(bool player){
  var wrap=Container(player?"ps-battle-actor ps-battle-actor-player":"ps-battle-actor ps-battle-actor-enemy");
  wrap.pickingMode=PickingMode.Ignore;
  if(player)battlePlayerActor=wrap;else battleEnemyActor=wrap;
  if(player){
   if(PackspireGame.LockBattleShowcaseArt&&game.UiShowcaseHeroSprite!=null)
    battlePlayerPortrait=SpriteImage(game.UiShowcaseHeroSprite,new Rect(0,0,1,1),"ps-battle-actor-image",ScaleMode.ScaleToFit);
   else if(PackspireGame.LockBattleShowcaseArt&&game.UiShowcaseHeroArt!=null)
    battlePlayerPortrait=Atlas(game.UiShowcaseHeroArt,new Rect(0,0,1,1),"ps-battle-actor-image");
   else
    battlePlayerPortrait=Atlas(game.UiCharacterArt,new Rect(0,0,1,1),"ps-battle-actor-image");
   wrap.Add(battlePlayerPortrait);
  } else {
   if(PackspireGame.LockBattleShowcaseArt&&game.UiShowcaseDragonArt!=null)
    battleEnemyPortrait=Atlas(game.UiShowcaseDragonArt,new Rect(0,0,1,1),"ps-battle-actor-image");
   else
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
   playerBars.Add(BuildMeter(out battlePlayerHpFill,out battlePlayerHpLabel,"ps-battle-meter-hp",true,"HP"));
   playerBars.Add(BuildMeter(out battlePlayerBlockFill,out battlePlayerBlockLabel,"ps-battle-meter-block",true,"BLOCK"));
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
  enemyHud.Add(enemyMeta);
  var enemyBars=Container("ps-battle-vbars");
  enemyBars.Add(BuildMeter(out battleEnemyHpFill,out battleEnemyHpLabel,"ps-battle-meter-hp",true,"HP"));
  enemyBars.Add(BuildMeter(out battleEnemyBlockFill,out battleEnemyBlockLabel,"ps-battle-meter-block",true,"BLOCK"));
  enemyHud.Add(enemyBars);
  battleEnemyStatuses=Container("ps-battle-status-row");
  enemyHud.Add(battleEnemyStatuses);
  return enemyHud;
 }

 VisualElement BuildBattleIntentBadge(){
  battleIntentBadge=Container("ps-battle-intent-badge");
  battleIntentBadge.pickingMode=PickingMode.Ignore;
  var next=new Label("NEXT"){pickingMode=PickingMode.Ignore};
  next.AddToClassList("ps-battle-intent-next");
  battleIntentBadge.Add(next);
  var row=Container("ps-battle-intent-row");
  row.pickingMode=PickingMode.Ignore;
  battleIntentIcon=new Image{scaleMode=ScaleMode.ScaleToFit,pickingMode=PickingMode.Ignore};
  battleIntentIcon.AddToClassList("ps-battle-intent-icon");
  row.Add(battleIntentIcon);
  battleIntentValue=new Label("0"){pickingMode=PickingMode.Ignore};
  battleIntentValue.AddToClassList("ps-battle-intent-value");
  row.Add(battleIntentValue);
  battleIntentBadge.Add(row);
  battleIntentKind=new Label("ATTACK"){pickingMode=PickingMode.Ignore};
  battleIntentKind.AddToClassList("ps-battle-intent-kind");
  battleIntentBadge.Add(battleIntentKind);
  battleIntentHint=new Label(""){pickingMode=PickingMode.Ignore};
  battleIntentHint.AddToClassList("ps-battle-intent-hint");
  battleIntentBadge.Add(battleIntentHint);
  return battleIntentBadge;
 }

 VisualElement BuildMeter(out VisualElement fill,out Label label,string toneClass,bool vertical,string caption=""){
  var meter=Container(vertical?"ps-battle-meter ps-battle-meter-v "+toneClass:"ps-battle-meter "+toneClass);
  bool framed=vertical&&battleMeterFrame!=null;
  if(framed)meter.AddToClassList("ps-battle-meter-framed");
  var track=Container("ps-battle-meter-track");
  fill=Container("ps-battle-meter-fill");
  // Force fill color in code — USS alone was hidden behind opaque frame art.
  if(toneClass.Contains("hp"))fill.style.backgroundColor=new Color(0.91f,0.21f,0.19f,1f);
  else if(toneClass.Contains("block"))fill.style.backgroundColor=new Color(0.22f,0.69f,0.91f,1f);
  track.Add(fill);
  meter.Add(track);
  if(framed){
   var frame=new Image{image=battleMeterFrame,scaleMode=ScaleMode.StretchToFill,pickingMode=PickingMode.Ignore};
   frame.AddToClassList("ps-battle-meter-frame");
   meter.Add(frame);
  }
  // Numbers stay off the gauges; captions (HP / BLOCK) remain below.
  label=null;
  if(!string.IsNullOrEmpty(caption)){
   var cap=new Label(caption){pickingMode=PickingMode.Ignore};
   cap.AddToClassList("ps-battle-meter-caption");
   meter.Add(cap);
  }
  return meter;
 }

 static void ApplyBattlePlate(VisualElement button,Texture2D plate,ScaleMode mode){
  if(button==null||plate==null)return;
  button.style.backgroundImage=new StyleBackground(plate);
  button.style.unityBackgroundScaleMode=mode;
  button.style.backgroundColor=Color.clear;
  button.style.borderTopWidth=0;
  button.style.borderRightWidth=0;
  button.style.borderBottomWidth=0;
  button.style.borderLeftWidth=0;
  button.style.paddingTop=0;
  button.style.paddingRight=0;
  button.style.paddingBottom=0;
  button.style.paddingLeft=0;
  button.style.unityBackgroundImageTintColor=Color.white;
 }

 void OnBattleSkillClicked(){
  if(battleInputLocked||battleSkillButton==null||!battleSkillButton.enabledSelf)return;
  if(!game.UiUseActiveSkill())RefreshBattleUi();
 }

 public void RefreshBattleUi(){
  if(!battleUiBuilt||battleRoot==null)return;
  var run=game.UiRun;
  var battle=game.UiBattle;
  if(run==null||battle==null)return;
  var dungeon=GameCatalog.Dungeons.First(x=>x.id==run.dungeon);

  if(PackspireGame.LockBattleShowcaseArt)
   battlePlayerName.text=CharacterCatalog.Get("sena").name;
  else
   battlePlayerName.text=CharacterCatalog.Get(run.characterId).name;
  if(battlePlayerPortrait!=null){
   if(PackspireGame.LockBattleShowcaseArt&&game.UiShowcaseHeroSprite!=null){
    battlePlayerPortrait.sprite=game.UiShowcaseHeroSprite;
    battlePlayerPortrait.uv=new Rect(0,0,1,1);
   } else if(PackspireGame.LockBattleShowcaseArt&&game.UiShowcaseHeroArt!=null){
    battlePlayerPortrait.image=game.UiShowcaseHeroArt;
    battlePlayerPortrait.uv=new Rect(0,0,1,1);
   } else {
    var character=CharacterCatalog.Get(run.characterId);
    ApplyCharacterPortraitImage(battlePlayerPortrait,character);
   }
   battlePlayerPortrait.style.display=DisplayStyle.Flex;
  }
  SetMeter(battlePlayerHpFill,battlePlayerHpLabel,run.hp,run.maxHp,$"{run.hp}/{run.maxHp}",true);
  SetMeter(battlePlayerBlockFill,battlePlayerBlockLabel,run.block,24,$"{run.block}",true);
  RefreshStatuses(battlePlayerStatuses,run.statuses);

  battleEnemyName.text=battle.enemy.name;
  if(battleEnemyPortrait!=null){
   if(PackspireGame.LockBattleShowcaseArt&&game.UiShowcaseDragonArt!=null){
    battleEnemyPortrait.image=game.UiShowcaseDragonArt;
    battleEnemyPortrait.uv=new Rect(0,0,1,1);
   } else if(battle.enemy.HasPortraitAsset){
    battleEnemyPortrait.image=game.ResolveEnemyPortrait(battle.enemy);
    battleEnemyPortrait.uv=new Rect(0,0,1,1);
   } else {
    battleEnemyPortrait.image=game.UiEnemyArt;
    battleEnemyPortrait.uv=EnemyUv(battle.enemy.id);
   }
  }
  int moveIndex=battle.move%battle.enemy.damages.Length;
  int baseDamage=battle.enemy.damages[moveIndex];
  int rawIntent=BattleSystem.Damage(baseDamage+dungeon.damage,battle.enemyStatuses,run.statuses);
  var moveEffects=ContentDatabase.EnemyEffects(battle.enemy.name,moveIndex);
  RefreshBattleIntent(rawIntent,baseDamage==0&&dungeon.damage==0,run.block,moveEffects);

  SetMeter(battleEnemyHpFill,battleEnemyHpLabel,Mathf.Max(0,battle.enemyHp),battle.enemyMaxHp,$"{Mathf.Max(0,battle.enemyHp)}/{battle.enemyMaxHp}",true);
  SetMeter(battleEnemyBlockFill,battleEnemyBlockLabel,battle.enemyBlock,24,$"{battle.enemyBlock}",true);
  RefreshStatuses(battleEnemyStatuses,battle.enemyStatuses);

  RefreshBattleHand(run);
  RefreshBattleConsumables(run);
  battleSkillMetaLabel.text=$"EN {run.energy}/3   DECK {run.draw.Count}   DISCARD {run.discard.Count}";
  battleSkillButton.text=game.UiActiveSkillAvailable?"SKILL":"USED";
  battleSkillButton.tooltip=game.UiActiveSkillTooltip;
  battleSkillButton.SetEnabled(game.UiActiveSkillAvailable);
 }

 void RefreshBattleIntent(int rawDamage,bool specialMove,int playerBlock,List<EffectSpec> effects){
  if(battleIntentBadge==null)return;
  bool attack=rawDamage>0;
  if(battleIntentIcon!=null){
   battleIntentIcon.image=specialMove&&!attack
    ?(battleIconEnergy??battleIconBlock)
    :(battleIconClaw??battleIconDamage);
   battleIntentIcon.style.display=battleIntentIcon.image!=null?DisplayStyle.Flex:DisplayStyle.None;
  }
  if(battleIntentValue!=null){
   battleIntentValue.text=attack?rawDamage.ToString():"!";
   battleIntentValue.EnableInClassList("ps-battle-intent-value-special",!attack);
  }
  if(battleIntentKind!=null)
   battleIntentKind.text=attack?"ATTACK":"SPECIAL";
  if(battleIntentHint!=null){
   var bits=new List<string>();
   if(attack){
    int afterBlock=Mathf.Max(0,rawDamage-Mathf.Max(0,playerBlock));
    bits.Add(playerBlock>0?$"HIT YOU  {afterBlock}":"DIRECT HIT");
   }
   if(effects!=null){
    foreach(var effect in effects.Take(2)){
     var def=ContentDatabase.Status(effect.type);
     string name=def!=null?def.name:effect.type;
     bits.Add($"+{name}{effect.amount}");
    }
   }
   if(!attack&&(effects==null||effects.Count==0))bits.Add("UNKNOWN MOVE");
   battleIntentHint.text=string.Join("   ·   ",bits);
  }
  battleIntentBadge.tooltip=attack
   ?$"NEXT ATTACK {rawDamage}"+(playerBlock>0?$"\nAfter BLOCK → {Mathf.Max(0,rawDamage-playerBlock)}":"")
   :"NEXT SPECIAL MOVE";
 }

 static void SetMeter(VisualElement fill,Label label,int value,int max,string text,bool vertical){
  if(fill==null)return;
  float ratio=max<=0?0f:Mathf.Clamp01(value/(float)max);
  if(vertical){
   fill.style.left=1;
   fill.style.right=1;
   fill.style.width=StyleKeyword.Auto;
   fill.style.top=StyleKeyword.Auto;
   fill.style.bottom=1;
   fill.style.height=Length.Percent(ratio*100f);
  } else {
   fill.style.left=2;
   fill.style.right=StyleKeyword.Auto;
   fill.style.top=2;
   fill.style.bottom=2;
   fill.style.height=StyleKeyword.Auto;
   fill.style.width=Length.Percent(ratio*100f);
  }
  if(label!=null)label.text=text;
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
   Button button=null;
   button=new Button(()=>{
    if(!affordable||battleInputLocked||button==null)return;
    PlayBattleCardMotion(button,card,()=>game.UiPlayBattleCard(index));
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
  string durability=sourceItem!=null?$"DUR {sourceItem.durability}/6":card.roleCard?"ROLE":"BASIC";
  var dur=new Label(durability){pickingMode=PickingMode.Ignore};
  dur.AddToClassList("ps-battle-card-durability");
  if(sourceItem!=null&&sourceItem.durability<=1)dur.AddToClassList("ps-battle-card-durability-low");
  foot.Add(dur);
  slot.Add(foot);
  if(!affordable){
   var lockLabel=new Label("LOW EN"){pickingMode=PickingMode.Ignore};
   lockLabel.AddToClassList("ps-battle-card-lock");
   slot.Add(lockLabel);
  }
 }

 void RefreshBattleConsumables(RunState run){
  if(battleConsumablesRoot==null)return;
  battleConsumablesRoot.Clear();
  if(run.consumables.Count==0){
   battleConsumablesRoot.style.display=DisplayStyle.None;
   return;
  }
  battleConsumablesRoot.style.display=DisplayStyle.Flex;
  for(int i=0;i<run.consumables.Count;i++){
   int index=i;
   string id=run.consumables[index];
   var button=new Button(()=>{
    if(battleInputLocked)return;
    game.UiUseBattleConsumable(index);
   });
   button.AddToClassList("ps-battle-cons");
   button.tooltip=ConsumableSystem.Name(id);
   string visual=id=="heal"?"herb":id=="guard"?"buckler":id=="fire"?"bomb":"flask";
   button.Add(Atlas(game.UiEquipmentArt,ItemUv(visual),"ps-battle-cons-art"));
   battleConsumablesRoot.Add(button);
  }
 }

 void SuspendBattleUi(){
  battleUiBuilt=false;
  battleInputLocked=false;
  battleStartBannerShown=false;
  battleRoot=null;
  battleHandRoot=null;
  battleFxLayer=null;
  battlePlayerActor=null;
  battleEnemyActor=null;
  battlePlayerStatuses=null;
  battleEnemyStatuses=null;
  battleConsumablesRoot=null;
  battlePlayerName=null;
  battleEnemyName=null;
  battleIntentBadge=null;
  battleIntentIcon=null;
  battleIntentKind=null;
  battleIntentValue=null;
  battleIntentHint=null;
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

 public void PlayBattleActionFx(BattleActionFx fx){
  if(!battleUiBuilt||battleRoot==null||!fx.ok)return;
  int stagger=0;
  if(fx.damageToEnemy>0){
   SpawnBattleFloater(true,battleIconDamage,fx.damageToEnemy.ToString(),"ps-battle-floater-damage",stagger);
   PulseBattleActor(false,"ps-battle-actor-hit");
   stagger+=70;
  }
  if(fx.damageToPlayer>0){
   SpawnBattleFloater(false,battleIconClaw??battleIconDamage,fx.damageToPlayer.ToString(),"ps-battle-floater-damage",stagger);
   PulseBattleActor(true,"ps-battle-actor-hit");
   stagger+=70;
  }
  if(fx.blockGained>0){
   SpawnBattleFloater(false,battleIconBlock,"+"+fx.blockGained,"ps-battle-floater-block",stagger);
   PulseBattleActor(true,"ps-battle-actor-guard");
   stagger+=70;
  }
  if(fx.healGained>0){
   SpawnBattleFloater(false,battleIconHeal,"+"+fx.healGained,"ps-battle-floater-heal",stagger);
   PulseBattleActor(true,"ps-battle-actor-heal");
   stagger+=70;
  }
  if(fx.energyGained>0){
   SpawnBattleFloater(false,battleIconEnergy,"+"+fx.energyGained,"ps-battle-floater-energy",stagger);
   stagger+=70;
  }
  if(fx.selfDamage>0)
   SpawnBattleFloater(false,battleIconDamage,fx.selfDamage.ToString(),"ps-battle-floater-self",stagger);
  if(fx.damageToEnemy<=0&&fx.damageToPlayer<=0&&fx.blockGained<=0&&fx.healGained<=0&&fx.energyGained<=0&&fx.selfDamage<=0){
   if(fx.cardType==CardType.Power)
    SpawnBattleFloater(false,battleIconEnergy,"強化","ps-battle-floater-power",0);
   else if(fx.cardType==CardType.Skill)
    SpawnBattleFloater(false,battleIconBlock,"発動","ps-battle-floater-skill",0);
  }
 }

 void PlayBattleCardMotion(Button source,CardInstance card,System.Action onDone){
  if(source==null||battleFxLayer==null||battleRoot==null){onDone?.Invoke();return;}
  battleInputLocked=true;
  source.SetEnabled(false);
  bool towardEnemy=card.type==CardType.Attack||card.damage>0;
  var target=towardEnemy?battleEnemyActor:battlePlayerActor;
  var ghost=Container(towardEnemy?"ps-battle-card-flight ps-battle-card-flight-attack":"ps-battle-card-flight ps-battle-card-flight-support");
  ghost.pickingMode=PickingMode.Ignore;
  var title=new Label(card.name){pickingMode=PickingMode.Ignore};
  title.AddToClassList("ps-battle-card-flight-name");
  ghost.Add(title);
  battleFxLayer.Add(ghost);

  var rootBound=battleRoot.worldBound;
  var srcBound=source.worldBound;
  var dstBound=target!=null?target.worldBound:srcBound;
  float startX=srcBound.x-rootBound.x;
  float startY=srcBound.y-rootBound.y;
  float endX=dstBound.x-rootBound.x+dstBound.width*0.35f;
  float endY=dstBound.y-rootBound.y+dstBound.height*(towardEnemy?0.28f:0.45f);
  ghost.style.left=startX;
  ghost.style.top=startY;
  ghost.style.opacity=1f;
  source.style.visibility=Visibility.Hidden;

  ghost.schedule.Execute(()=>{
   if(ghost.parent==null)return;
   ghost.AddToClassList("ps-battle-card-flight-go");
   ghost.style.left=endX;
   ghost.style.top=endY;
   ghost.style.opacity=0f;
   ghost.style.scale=new Scale(new Vector3(0.55f,0.55f,1f));
  }).StartingIn(20);

  ghost.schedule.Execute(()=>{
   ghost.RemoveFromHierarchy();
   onDone?.Invoke();
   battleInputLocked=false;
  }).StartingIn(280);
 }

 void SpawnBattleFloater(bool onEnemy,Texture2D icon,string value,string toneClass,int delayMs){
  if(battleFxLayer==null||battleRoot==null)return;
  var host=onEnemy?battleEnemyActor:battlePlayerActor;
  if(host==null)host=battleRoot;
  var floater=Container("ps-battle-floater "+toneClass);
  floater.pickingMode=PickingMode.Ignore;
  if(icon!=null){
   var iconImg=new Image{image=icon,scaleMode=ScaleMode.ScaleToFit,pickingMode=PickingMode.Ignore};
   iconImg.AddToClassList("ps-battle-floater-icon-art");
   floater.Add(iconImg);
  }
  var valueLabel=new Label(value){pickingMode=PickingMode.Ignore};
  valueLabel.AddToClassList("ps-battle-floater-value");
  floater.Add(valueLabel);
  battleFxLayer.Add(floater);

  int serial=++battleFloaterSerial;
  float jitterX=(serial%5-2)*18f;
  float jitterY=(serial%3)*10f;
  void Place(){
   if(battleRoot==null||floater.parent==null)return;
   var rootBound=battleRoot.worldBound;
   var hostBound=host.worldBound;
   floater.style.left=hostBound.x-rootBound.x+hostBound.width*0.38f+jitterX;
   floater.style.top=hostBound.y-rootBound.y+hostBound.height*0.22f+jitterY;
  }
  Place();
  floater.schedule.Execute(()=>{
   Place();
   floater.AddToClassList("ps-battle-floater-pop");
  }).StartingIn(Mathf.Max(16,delayMs));
  floater.schedule.Execute(()=>{if(floater.parent!=null)floater.AddToClassList("ps-battle-floater-out");}).StartingIn(Mathf.Max(16,delayMs)+420);
  floater.schedule.Execute(()=>floater.RemoveFromHierarchy()).StartingIn(Mathf.Max(16,delayMs)+980);
 }

 void PulseBattleActor(bool player,string pulseClass){
  var actor=player?battlePlayerActor:battleEnemyActor;
  if(actor==null)return;
  actor.RemoveFromClassList("ps-battle-actor-hit");
  actor.RemoveFromClassList("ps-battle-actor-guard");
  actor.RemoveFromClassList("ps-battle-actor-heal");
  actor.schedule.Execute(()=>{
   if(actor==null)return;
   actor.AddToClassList(pulseClass);
   actor.schedule.Execute(()=>{if(actor!=null)actor.RemoveFromClassList(pulseClass);}).StartingIn(220);
  }).StartingIn(16);
 }
}
}
