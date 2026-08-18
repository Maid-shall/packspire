#if PACKSPIRE_LEGACY_BATTLE_UI
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire {
public sealed partial class PackspireUiFoundation {
 static readonly Rect BattleShowcasePlayerUv=new Rect(0.075f,0.078f,0.85f,0.854f);
 static readonly Rect BattleShowcaseEnemyUv=new Rect(0.08f,0.135f,0.84f,0.825f);
 bool battleUiBuilt,battleInputLocked;
 VisualElement battleRoot,battleStage,battlePlayerLane,battleEnemyLane,battlePlayerCombatant,battleEnemyCombatant,battleEnemyLine;
 VisualElement battleHandRoot,battlePlayerStatuses,battleEnemyStatuses;
 VisualElement battleFxLayer,battlePlayerActor,battleEnemyActor,battleIntentList,battlePlayerHud,battleEnemyHud;
 VisualElement battleHoverDetail,battleDeckOverlay,battleDeckGrid;
 Label battleRoundLabel;
 Label battlePlayerHpLabel,battlePlayerBlockLabel,battlePlayerEnergyLabel,battleEnemyHpLabel,battleEnemyBlockLabel;
 Label battleDrawCountLabel,battleDiscardCountLabel,battleSkillLabel;
 Label battleHoverEyebrow,battleHoverTitle,battleHoverBody,battleDeckTitle,battleDeckSummary;
 VisualElement battlePlayerHpFill,battleEnemyHpFill;
 Image battlePlayerPortrait,battleEnemyPortrait;
 Button battleSkillButton,battleDrawButton,battleDiscardButton,battleEndTurnButton;
 Texture2D battleIconDamage,battleIconBlock,battleIconHeal,battleIconEnergy,battleIconClaw;
 int battleFloaterSerial;
 bool battleStartBannerShown;
 readonly List<Button> battleHandButtons=new();
 readonly List<BattleCardView> battleHandViews=new();
 readonly List<(Button button,float depth)> battleHandOrder=new();

 void EnsureBattleAssets(){
  if(battleIconDamage!=null)return;
  battleIconDamage=PackspireResources.Load<Texture2D>("Art/Battle/Icons/icon-damage");
  battleIconBlock=PackspireResources.Load<Texture2D>("Art/Battle/Icons/icon-block");
  battleIconHeal=PackspireResources.Load<Texture2D>("Art/Battle/Icons/icon-heal");
  battleIconEnergy=PackspireResources.Load<Texture2D>("Art/Battle/Icons/icon-energy");
  battleIconClaw=PackspireResources.Load<Texture2D>("Art/Battle/Icons/icon-claw");
 }

 void BuildBattle(){
  EnsureBattleAssets();
  battleUiBuilt=true;
  battleRoot=CloneView("UI/PackspireBattleView","ps-battle-screen ps-battle");
  if(battleRoot==null){
   Debug.LogError("Battle view could not be created.");
   return;
  }
  screenRoot.Add(battleRoot);
  RequireViewElement<VisualElement>(battleRoot,"battle-background");
  battleRoundLabel=RequireViewElement<Label>(battleRoot,"battle-round");
  battleStage=RequireViewElement<VisualElement>(battleRoot,"battle-stage");
  battlePlayerLane=RequireViewElement<VisualElement>(battleRoot,"battle-player-lane");
  battleEnemyLane=RequireViewElement<VisualElement>(battleRoot,"battle-enemy-lane");
  battlePlayerCombatant=RequireViewElement<VisualElement>(battleRoot,"battle-player-combatant");
  battleEnemyCombatant=RequireViewElement<VisualElement>(battleRoot,"battle-enemy-combatant");
  battleEnemyLine=RequireViewElement<VisualElement>(battleRoot,"battle-enemy-line");
  battlePlayerActor=RequireViewElement<VisualElement>(battleRoot,"battle-player-actor");
  battleEnemyActor=RequireViewElement<VisualElement>(battleRoot,"battle-enemy-actor");
  battlePlayerPortrait=RequireViewElement<Image>(battleRoot,"battle-player-portrait");
  battleEnemyPortrait=RequireViewElement<Image>(battleRoot,"battle-enemy-portrait");
  battlePlayerPortrait.scaleMode=ScaleMode.ScaleToFit;
  battleEnemyPortrait.scaleMode=ScaleMode.ScaleToFit;
  battlePlayerHud=RequireViewElement<VisualElement>(battleRoot,"battle-player-hud");
  battleEnemyHud=RequireViewElement<VisualElement>(battleRoot,"battle-enemy-hud");
  battlePlayerHpLabel=RequireViewElement<Label>(battleRoot,"battle-player-hp-label");
  battlePlayerBlockLabel=RequireViewElement<Label>(battleRoot,"battle-player-block-label");
  battlePlayerEnergyLabel=RequireViewElement<Label>(battleRoot,"battle-player-energy-label");
  battleDrawCountLabel=RequireViewElement<Label>(battleRoot,"battle-draw-count");
  battleDiscardCountLabel=RequireViewElement<Label>(battleRoot,"battle-discard-count");
  battleEnemyHpLabel=RequireViewElement<Label>(battleRoot,"battle-enemy-hp-label");
  battleEnemyBlockLabel=RequireViewElement<Label>(battleRoot,"battle-enemy-block-label");
  battlePlayerHpFill=RequireViewElement<VisualElement>(battleRoot,"battle-player-hp-fill");
  battleEnemyHpFill=RequireViewElement<VisualElement>(battleRoot,"battle-enemy-hp-fill");
  battlePlayerStatuses=RequireViewElement<VisualElement>(battleRoot,"battle-player-statuses");
  battleEnemyStatuses=RequireViewElement<VisualElement>(battleRoot,"battle-enemy-statuses");
  battleIntentList=RequireViewElement<VisualElement>(battleRoot,"battle-intent-list");
  battleFxLayer=RequireViewElement<VisualElement>(battleRoot,"battle-fx");
  battleHandRoot=RequireViewElement<VisualElement>(battleRoot,"battle-hand");
  battleFxLayer.pickingMode=PickingMode.Ignore;
  battleHandRoot.pickingMode=PickingMode.Ignore;

  battleSkillButton=RequireViewElement<Button>(battleRoot,"battle-skill-button");
  battleSkillLabel=RequireViewElement<Label>(battleRoot,"battle-skill-label");
  battleDrawButton=RequireViewElement<Button>(battleRoot,"battle-draw-button");
  battleDiscardButton=RequireViewElement<Button>(battleRoot,"battle-discard-button");
  battleEndTurnButton=RequireViewElement<Button>(battleRoot,"battle-end-turn");
  battleSkillButton.clicked+=OnBattleSkillClicked;
  battleDrawButton.clicked+=()=>OpenBattlePile("山札",game.UiRun?.draw);
  battleDiscardButton.clicked+=()=>OpenBattlePile("捨て札",game.UiRun?.discard);
  battleEndTurnButton.clicked+=()=>{
   if(battleInputLocked)return;
   game.UiEndBattleTurn();
  };
  battleSkillButton.RegisterCallback<PointerEnterEvent>(_=>
   ShowBattleHoverDetail("ACTIVE SKILL","発動スキル",game.UiActiveSkillTooltip));
  battleSkillButton.RegisterCallback<PointerLeaveEvent>(_=>HideBattleHoverDetail());

  battleHoverDetail=RequireViewElement<VisualElement>(battleRoot,"battle-hover-detail");
  battleHoverEyebrow=RequireViewElement<Label>(battleRoot,"battle-hover-eyebrow");
  battleHoverTitle=RequireViewElement<Label>(battleRoot,"battle-hover-title");
  battleHoverBody=RequireViewElement<Label>(battleRoot,"battle-hover-body");
  battleDeckOverlay=RequireViewElement<VisualElement>(battleRoot,"battle-deck-overlay");
  battleDeckGrid=RequireViewElement<VisualElement>(battleRoot,"battle-deck-grid");
  battleDeckTitle=RequireViewElement<Label>(battleRoot,"battle-deck-title");
  battleDeckSummary=RequireViewElement<Label>(battleRoot,"battle-deck-summary");
  RequireViewElement<Button>(battleRoot,"battle-deck-backdrop").clicked+=CloseBattlePile;
  RequireViewElement<Button>(battleRoot,"battle-deck-close").clicked+=CloseBattlePile;

  RefreshBattleUi();
  ShowBattleStartBanner();
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

 void OnBattleSkillClicked(){
  if(battleInputLocked||battleSkillButton==null||!battleSkillButton.enabledSelf)return;
  if(!game.UiUseActiveSkill())RefreshBattleUi();
 }

 public void RefreshBattleUi(){
  using var performanceScope=PackspirePerformance.ProductBattleRefresh.Auto();
  if(gridBoardBuilt&&game.UiScreen==ScreenId.GridBoard){
   RefreshGridBoard();
   return;
  }
  if(!battleUiBuilt||battleRoot==null)return;
  var run=game.UiRun;
  var battle=game.UiBattle;
  if(run==null||battle==null)return;
  var dungeon=game.UiCurrentDungeon;

  battleRoot.RemoveFromClassList("is-dungeon-old-spire");
  battleRoot.RemoveFromClassList("is-dungeon-ash-forge");
  battleRoot.RemoveFromClassList("is-dungeon-hollow-archive");
  string dungeonClass=dungeon?.id=="ash_forge"?"is-dungeon-ash-forge":
   dungeon?.id=="hollow_archive"?"is-dungeon-hollow-archive":"is-dungeon-old-spire";
  battleRoot.AddToClassList(dungeonClass);
  battleRoundLabel.text=$"{battle.move+1:00}";

  var character=CharacterCatalog.Get(PackspireGame.LockBattleShowcaseArt?"mio":run.characterId);
  if(PackspireGame.LockBattleShowcaseArt&&game.UiShowcaseHeroSprite!=null){
   battlePlayerPortrait.sprite=game.UiShowcaseHeroSprite;
   battlePlayerPortrait.uv=BattleShowcasePlayerUv;
  } else if(PackspireGame.LockBattleShowcaseArt&&game.UiShowcaseHeroArt!=null){
   battlePlayerPortrait.image=game.UiShowcaseHeroArt;
   battlePlayerPortrait.uv=BattleShowcasePlayerUv;
  } else ApplyCharacterPortraitImage(battlePlayerPortrait,character);
  battlePlayerPortrait.style.display=DisplayStyle.Flex;

  SetMeter(battlePlayerHpFill,battlePlayerHpLabel,run.hp,run.maxHp,$"{run.hp}/{run.maxHp}",false);
  RefreshBattleBlock(battlePlayerHud,battlePlayerBlockLabel,run.block);
  int baseEnergy=Mathf.Max(1,PackspireContent.Data.balance.baseEnergy);
  battlePlayerEnergyLabel.text=$"{run.energy} / {baseEnergy}";
  RefreshStatuses(battlePlayerStatuses,run.statuses);

  if(PackspireGame.LockBattleShowcaseArt&&game.UiShowcaseDragonArt!=null){
   battleEnemyPortrait.image=game.UiShowcaseDragonArt;
   battleEnemyPortrait.uv=BattleShowcaseEnemyUv;
  } else if(battle.enemy.HasPortraitAsset){
   battleEnemyPortrait.image=game.ResolveEnemyPortrait(battle.enemy);
   battleEnemyPortrait.uv=new Rect(0,0,1,1);
  } else {
   battleEnemyPortrait.image=game.UiEnemyArt;
   battleEnemyPortrait.uv=EnemyUv(battle.enemy.id);
  }

  int moveIndex=BattleSystem.NextEnemyMoveIndex(battle);
  int baseDamage=battle.enemy.damages[moveIndex];
  int rawIntent=baseDamage>0
   ?BattleSystem.Damage(baseDamage+(dungeon?.damage??0),battle.enemyStatuses,run.statuses)
   :0;
  var authoredMove=ContentDatabase.EnemyMove(battle.enemy.id,moveIndex);
  var moveEffects=ContentDatabase.EnemyEffects(battle.enemy.id,moveIndex);
  RefreshBattleIntent(rawIntent,baseDamage==0,moveEffects,authoredMove);

  int enemyHp=Mathf.Max(0,battle.enemyHp);
  SetMeter(battleEnemyHpFill,battleEnemyHpLabel,enemyHp,battle.enemyMaxHp,$"{enemyHp}/{battle.enemyMaxHp}",false);
  RefreshBattleBlock(battleEnemyHud,battleEnemyBlockLabel,battle.enemyBlock);
  RefreshStatuses(battleEnemyStatuses,battle.enemyStatuses);
  RefreshBattleFormationPresentation(rawIntent,baseDamage==0,moveEffects,authoredMove);
  RefreshBattleHand(run);
  battleDrawCountLabel.text=run.draw.Count.ToString();
  battleDiscardCountLabel.text=run.discard.Count.ToString();
  battleSkillLabel.text=game.UiActiveSkillAvailable?game.UiActiveSkillLabel:"使用済み";
  battleSkillButton.tooltip=game.UiActiveSkillTooltip;
  battleSkillButton.SetEnabled(game.UiActiveSkillAvailable);
 }

 void RefreshBattleIntent(int rawDamage,bool specialMove,List<EffectSpec> effects,EnemyMoveContent move=null){
  if(battleIntentList==null)return;
  battleIntentList.Clear();
  if(rawDamage>0){
   AddBattleIntent(battleIconClaw!=null?battleIconClaw:battleIconDamage,
    rawDamage.ToString(),"is-attack");
  } else if(specialMove){
   AddBattleIntent(battleIconEnergy,"◆","is-special");
  }
  if((move?.block??0)>0)
    AddBattleIntent(battleIconBlock,move.block.ToString(),"is-guard");
  if((move?.heal??0)>0)
    AddBattleIntent(battleIconHeal,move.heal.ToString(),"is-heal");
  if(effects!=null)foreach(var effect in effects){
   AddBattleIntent(battleIconEnergy,effect.amount.ToString(),"is-status");
  }
  if(battleIntentList.childCount==0)
    AddBattleIntent(battleIconEnergy,"?","is-special");
 }

 void AddBattleIntent(Texture2D icon,string value,string toneClass){
  var entry=Container("ps-battle-intent-entry "+toneClass);
  if(icon!=null){
   var image=new Image{image=icon,scaleMode=ScaleMode.ScaleToFit,pickingMode=PickingMode.Ignore};
   image.AddToClassList("ps-battle-intent-entry__icon");
   entry.Add(image);
  }
  var valueLabel=new Label(value){pickingMode=PickingMode.Ignore};
  valueLabel.AddToClassList("ps-battle-intent-entry__value");
  entry.Add(valueLabel);
  battleIntentList.Add(entry);
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
   fill.style.width=Length.Percent(ratio*100f);
  }
  if(label!=null)label.text=text;
 }

 static void RefreshBattleBlock(VisualElement combatantHud,Label label,int value){
  bool active=value>0;
  combatantHud?.EnableInClassList("has-block",active);
  if(label!=null)label.text=active?value.ToString():string.Empty;
 }

 void RefreshStatuses(VisualElement row,List<StatusState> statuses){
  if(row==null)return;
  row.Clear();
  int visibleSlots=5;
  if(statuses!=null)foreach(var status in statuses.Take(visibleSlots)){
   var def=ContentDatabase.Status(status.type);
   bool debuff=def!=null&&def.kind=="debuff";
   var chip=Container(debuff
    ?"ps-battle-status-chip ps-battle-status-debuff"
    :"ps-battle-status-chip ps-battle-status-buff");
   chip.tooltip=def!=null?$"{def.name}\n{def.description}":status.type;
   chip.Add(new Label(def?.icon??"◆"){pickingMode=PickingMode.Ignore});
   var count=new Label(status.amount.ToString()){pickingMode=PickingMode.Ignore};
   count.AddToClassList("ps-battle-status-count");
   chip.Add(count);
   row.Add(chip);
  }
 }

 void RefreshBattleHand(RunState run){
  using var performanceScope=PackspirePerformance.ProductBattleHandRefresh.Auto();
  if(battleHandRoot==null)return;
  battleHandRoot.Clear();
  if(run.hand.Count==0)return;
  int count=run.hand.Count;
  battleHandOrder.Clear();
  for(int i=0;i<count;i++){
   int index=i;
   var card=run.hand[index];
   bool affordable=!card.unplayable&&card.cost<=run.energy;
   EnsureBattleHandCapacity(index+1);
   Button button=battleHandButtons[index];
   BattleCardView view=battleHandViews[index];
   button.userData=index;
   button.SetEnabled(affordable&&!battleInputLocked);
   view.Bind(BattleCardModel(card,run,affordable));
   float depth=BattleHandFanLayout.Apply(button,i,count);
   battleHandOrder.Add((button,depth));
  }
  battleHandOrder.Sort(static (left,right)=>right.depth.CompareTo(left.depth));
  foreach(var slot in battleHandOrder)battleHandRoot.Add(slot.button);
 }

 void EnsureBattleHandCapacity(int count){
  while(battleHandButtons.Count<count){
   var button=new Button();
   button.AddToClassList("ps-battle-card");
   button.clicked+=()=>OnBattleHandCardClicked(button);
   button.RegisterCallback<PointerEnterEvent>(_=>button.BringToFront());
   battleHandButtons.Add(button);
   battleHandViews.Add(new BattleCardView(button));
  }
 }

 void OnBattleHandCardClicked(Button button){
  if(battleInputLocked||button?.userData is not int index)return;
  var run=game.UiRun;
  if(run==null||index<0||index>=run.hand.Count)return;
  var card=run.hand[index];
  if(card.unplayable||card.cost>run.energy)return;
  PlayBattleCardMotion(button,card,()=>game.UiPlayBattleCard(index));
 }

 BattleCardViewModel BattleCardModel(CardInstance card,RunState run,bool affordable){
  return new BattleCardViewModel(
   card.name,
   card.cost.ToString(),
   BattleCardDisplayTextWithKeywords(card),
   BattleCardPresentationKind(card,run),
   affordable?string.Empty:"LOW EN / 保留",
   BattleCardArtwork(card.id),
   affordable);
 }

 void PopulateBattleCard(VisualElement slot,CardInstance card,RunState run,bool affordable){
  ApplyBattleCardPresentation(slot,card,run);
  var sourceItem=run.inventory.FirstOrDefault(x=>x.uid==card.sourceItemUid);
  string sourceName=card.source;
  if(sourceItem!=null&&GameCatalog.Items.TryGetValue(sourceItem.templateId,out var itemDef))sourceName=itemDef.name;
  int maximumDurability=sourceItem!=null&&GameCatalog.Items.TryGetValue(sourceItem.templateId,out var durabilityItem)
   ?durabilityItem.baseDurability:6;
  string durability=sourceItem!=null?$"DUR {sourceItem.durability}/{maximumDurability}":card.roleCard?"ROLE":"BASIC";
  PopulateDocketCard(slot,card,BattleCardDisplayTextWithKeywords(card),sourceName,durability,affordable,false);
 }

 static string BattleCardDisplayTextWithKeywords(CardInstance card){
  if(card==null)return "";
  string text=BattleCardDisplayText(card);
  var keywords=new List<string>();
  if(card.innate)keywords.Add("開始手札");
  if(card.retain)keywords.Add("保持");
  if(card.ethereal)keywords.Add("揮発");
  if(card.unplayable)keywords.Add("使用不可");
  if(card.afterUse==BattleCardAfterUse.ExhaustBattle)keywords.Add("戦闘除外");
  if(card.afterUse==BattleCardAfterUse.RemoveExpedition)keywords.Add("遠征除外");
  return keywords.Count==0?text:$"{text}\n〈{string.Join("・",keywords)}〉";
 }

 static string BattleCardDisplayText(CardInstance card){
  if(card==null||card.damage<=0)return card?.text??"";
  int modifier=card.damage-7;
  string formula=$"2D6 {(modifier>=0?"+ ":"− ")}{Mathf.Abs(modifier)} ダメージ";
  return card.text.Replace($"{card.damage}ダメージ",formula);
 }

 void ShowBattleHoverDetail(string eyebrow,string title,string body){
  if(battleHoverDetail==null)return;
  battleHoverEyebrow.text=eyebrow;
  battleHoverTitle.text=title;
  battleHoverBody.text=string.IsNullOrEmpty(body)?"詳細情報はありません。":body;
  battleHoverDetail.AddToClassList("is-visible");
 }

 void HideBattleHoverDetail(){
  battleHoverDetail?.RemoveFromClassList("is-visible");
 }

 void OpenBattlePile(string title,IReadOnlyList<CardInstance> cards){
  if(battleDeckOverlay==null||battleDeckGrid==null)return;
  battleDeckTitle.text=title;
  battleDeckGrid.Clear();
  int count=cards?.Count??0;
  battleDeckSummary.text=$"{count} 枚の戦闘カード";
  if(cards!=null){
   var run=game.UiRun;
   foreach(var card in cards){
    var cardView=new VisualElement{pickingMode=PickingMode.Ignore};
    cardView.AddToClassList("ps-battle-card");
    cardView.AddToClassList("ps-battle-deck-card");
    PopulateBattleCard(cardView,card,run,true);
    battleDeckGrid.Add(cardView);
   }
  }
  battleDeckOverlay.AddToClassList("is-open");
 }

 void CloseBattlePile(){
  battleDeckOverlay?.RemoveFromClassList("is-open");
 }

 void SuspendBattleUi(){
  ResetBattleFormationPresentation();
  battleUiBuilt=false;
  battleInputLocked=false;
  battleStartBannerShown=false;
  battleRoot=null;
  battleStage=null;
  battlePlayerLane=null;
  battleEnemyLane=null;
  battlePlayerCombatant=null;
  battleEnemyCombatant=null;
  battleEnemyLine=null;
  battleHandRoot=null;
  battleFxLayer=null;
  battlePlayerActor=null;
  battleEnemyActor=null;
  battlePlayerStatuses=null;
  battleEnemyStatuses=null;
  battleIntentList=null;
  battlePlayerHud=null;
  battleEnemyHud=null;
  battlePlayerHpLabel=null;
  battlePlayerBlockLabel=null;
  battlePlayerEnergyLabel=null;
  battleDrawCountLabel=null;
  battleDiscardCountLabel=null;
  battleEnemyHpLabel=null;
  battleEnemyBlockLabel=null;
  battlePlayerHpFill=null;
  battleEnemyHpFill=null;
  battleSkillButton=null;
  battleSkillLabel=null;
  battleDrawButton=null;
  battleDiscardButton=null;
  battleEndTurnButton=null;
  battlePlayerPortrait=null;
  battleEnemyPortrait=null;
  battleRoundLabel=null;
  battleHoverDetail=null;
  battleHoverEyebrow=null;
  battleHoverTitle=null;
  battleHoverBody=null;
  battleDeckOverlay=null;
  battleDeckGrid=null;
  battleDeckTitle=null;
  battleDeckSummary=null;
 }

 public void PlayBattleActionFx(BattleActionFx fx){
  if(!fx.ok)return;
  if(gridBoardBuilt&&gridBoardCombatMode&&gridBoardCombatStage!=null){
   PlayGridBattleActionFx(fx);
   return;
  }
  if(!battleUiBuilt||battleRoot==null)return;
  int stagger=0;
  if(fx.damageToEnemy>0){
   SpawnBattleFloater(true,battleIconDamage,fx.damageToEnemy.ToString(),"ps-battle-floater-damage",stagger);
   PulseBattleActor(false,"ps-battle-actor-hit");
   stagger+=70;
  }
  if(fx.damageToPlayer>0){
   var damageIcon=battleIconClaw!=null?battleIconClaw:battleIconDamage;
   SpawnBattleFloater(false,damageIcon,fx.damageToPlayer.ToString(),"ps-battle-floater-damage",stagger);
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
  var ghost=Container(towardEnemy
   ?"ps-battle-card-flight ps-battle-card-flight-attack"
   :"ps-battle-card-flight ps-battle-card-flight-support");
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
  floater.schedule.Execute(()=>{
   if(floater.parent!=null)floater.AddToClassList("ps-battle-floater-out");
  }).StartingIn(Mathf.Max(16,delayMs)+420);
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
   actor.schedule.Execute(()=>{
    if(actor!=null)actor.RemoveFromClassList(pulseClass);
   }).StartingIn(220);
  }).StartingIn(16);
}
}
}
#endif
