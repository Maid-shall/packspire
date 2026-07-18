using System;
using System.Collections.Generic;
using UnityEngine;

namespace Packspire {
public enum ItemType { Weapon, Armor, Rune, Supply }
public enum Element { Fire, Water, Wind, Earth }
public enum CardType { Attack, Skill, Power }
public enum ScreenId { Character, Hub, Status, Vault, Faction, Expedition, Pack, Map, Battle, Reward, Shop, Event, Compendium, GameOver }

[Serializable] public struct CellDef { public int x,y; public Element element; public int value; public CellDef(int x,int y,Element e,int value=1){this.x=x;this.y=y;element=e;this.value=value;} }
[Serializable] public class ItemDef { public string id,name,description,cardId,linkRule; public string[] cardIds; public ItemType type; public CellDef[] cells; public int handle; public ItemDef(string id,string name,ItemType type,string card,string description,params CellDef[] cells){this.id=id;this.name=name;this.type=type;cardId=card;cardIds=(id=="sword"||id=="shield"||id=="dagger")?new[]{card,card}:new[]{card};this.description=description;this.cells=cells;handle=0;} }
[Serializable] public class CardDef { public string id,name,text; public CardType type; public int cost,damage,block,heal,buff,energy,selfDamage; public bool exhaust; public CardDef(string id,string name,CardType type,int cost,string text){this.id=id;this.name=name;this.type=type;this.cost=cost;this.text=text;} }
[Serializable] public class EffectSpec { public string type,target="enemy"; public int amount=1,duration; }
[Serializable] public class StatusState { public string type; public int amount,duration; }
[Serializable] public class CardInstance { public string id,name,text,source,sourceItemUid,slotKey; public CardType type; public int cost,damage,block,heal,buff,energy,selfDamage,draw; public bool exhaust,recycle,durabilityFree,roleCard; public List<EffectSpec> effects=new(); public CardInstance Clone()=>new(){id=id,name=name,text=text,source=source,sourceItemUid=sourceItemUid,slotKey=slotKey,type=type,cost=cost,damage=damage,block=block,heal=heal,buff=buff,energy=energy,selfDamage=selfDamage,draw=draw,exhaust=exhaust,recycle=recycle,durabilityFree=durabilityFree,roleCard=roleCard,effects=effects.ConvertAll(x=>new EffectSpec{type=x.type,target=x.target,amount=x.amount,duration=x.duration})}; }
[Serializable] public class ScarRecord { public string type,dungeon; public int floor; public long timestamp; }
[Serializable] public class HeirloomHistory { public int battles,bosses,defeats; public List<IdInt> dungeons=new(); }
[Serializable] public class ItemInstance { public string uid,templateId,origin="loot",traitId=""; public int durability=6,uses,temper; public List<Element> colors=new(); public List<ScarRecord> scars=new(); public HeirloomHistory history=new(); public bool insured,heirloomCertified,identified=true; public ItemInstance(string id,string origin="loot"){uid=Guid.NewGuid().ToString("N");templateId=id;this.origin=origin;} }
[Serializable] public class Placement { public string itemUid; public int anchor,rotation; public Placement(string uid,int anchor,int rotation=0){itemUid=uid;this.anchor=anchor;this.rotation=rotation;} }
[Serializable] public class RoleDef { public string id,name,kind,description; public int maxLevel; public RoleDef(string id,string name,string kind,string desc,int max=10){this.id=id;this.name=name;this.kind=kind;description=desc;maxLevel=max;} }
[Serializable] public class EnemyDef { public string id,name; public int tier,hp; public int[] damages; public EnemyDef(string id,string name,int tier,int hp,params int[] moves){this.id=id;this.name=name;this.tier=tier;this.hp=hp;damages=moves;} }
[Serializable] public class DungeonDef { public string id,name,description; public int battles; public float hpScale,goldScale; public int damage; public DungeonDef(string id,string name,string desc,int battles,float hpScale,int damage,float goldScale){this.id=id;this.name=name;description=desc;this.battles=battles;this.hpScale=hpScale;this.damage=damage;this.goldScale=goldScale;} }
[Serializable] public class FactionDef { public string id,name,description; public string[] ranks; public FactionDef(string id,string name,string desc,params string[] ranks){this.id=id;this.name=name;description=desc;this.ranks=ranks;} }
[Serializable] public class BackpackDef { public string id,name,description; public int[] safeCells; public BackpackDef(string id,string name,string desc,params int[] safe){this.id=id;this.name=name;description=desc;safeCells=safe;} }
[Serializable] public class LoadoutSave { public string id="loadout-1",name="編成 1",backpack="standard",coreId="",conduitId="",resonanceId="",stabilityId=""; public List<Placement> slots=new(); public List<string> deck=new(); public bool deckInitialized; }
[Serializable] public class MetaSave { public int version=16,baseGold=24,vaultLevel,runs,wins,dungeonsUnlocked=1,body,hair,consumableCapacity=5; public bool characterMade; public string currentRole="warrior",currentFaction="iron",selectedBackpack="standard",selectedHeirloomUid="",selectedLoadoutId="loadout-1"; public List<ItemInstance> stash=new(); public List<LoadoutSave> loadouts=new(); public List<string> consumables=new(){"heal","heal","guard","fire","energy"}; public List<string> dungeonDiscoveries=new(); public List<string> unlockedRoles=new(){"warrior","guardian","scout","artificer"}; public List<string> discoveredItems=new(){"sword","shield","ember","herb"}; public List<string> discoveredEnemies=new(); public List<string> unlockedSecrets=new(); public List<IdInt> jobLevels=new(){new("warrior",1)}; public List<IdFloat> factionRep=new(){new("iron",0),new("spore",0),new("guild",0),new("void",0)}; }
[Serializable] public class IdInt { public string id; public int value; public IdInt(string id,int value){this.id=id;this.value=value;} }
[Serializable] public class IdFloat { public string id; public float value; public IdFloat(string id,float value){this.id=id;this.value=value;} }
[Serializable] public class DungeonAxes { public int alert,collapse,corruption; public int Clamp(int value)=>Mathf.Clamp(value,-15,15); public void Change(int alertDelta=0,int collapseDelta=0,int corruptionDelta=0){alert=Clamp(alert+alertDelta);collapse=Clamp(collapse+collapseDelta);corruption=Clamp(corruption+corruptionDelta);} }
/// <summary>
/// Expedition run. Persistent: hp/gold/inventory/lootBag/axes/battlesWon/consumables/ids.
/// Battle-ephemeral (reset in BattleSystem.ResetBattleEphemeral): energy, block, attackBuff, statuses, hand/draw/discard.
/// </summary>
[Serializable] public class RunState { public int hp=42,maxHp=42,gold=24,energy=3,block,attackBuff,battlesWon,mapPosition; public string role,dungeon="old_spire",faction,backpack="standard",loadoutId="loadout-1",heirloomUid="",coreId="",conduitId="",resonanceId="",stabilityId=""; public List<ItemInstance> inventory=new(),lootBag=new(); public List<Placement> placements=new(); public List<string> selectedCardSlots=new(),consumables=new(); public List<StatusState> statuses=new(); public DungeonAxes axes=new(); public List<CardInstance> deck=new(),draw=new(),discard=new(),hand=new(); }
}
