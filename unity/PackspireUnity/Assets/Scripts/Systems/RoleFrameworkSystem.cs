using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Packspire {
[Serializable]
public sealed class RoleBranchDef {
 public string name;
 public string text;
 public RoleBranchDef(string name,string text){this.name=name;this.text=text;}
}

[Serializable]
public sealed class RoleFrameworkDef {
 public string id;
 public string name;
 public string passiveName;
 public string passiveText;
 public string activeName;
 public string activeText;
 public string sealName;
 public string sealText;
 public string sealTarget;
 public RoleBranchDef[] branches;
}

/// <summary>
/// Production role surface: four stable jobs, one small branch choice and one
/// optional qualification seal. Legacy advanced roles remain discovery data.
/// </summary>
public static class RoleFrameworkSystem {
 public static readonly string[] CoreRoleIds={"warrior","guardian","scout","artificer"};

 static readonly Dictionary<string,RoleFrameworkDef> Definitions=new(){
  ["warrior"]=new RoleFrameworkDef{
   id="warrior",name="戦士",passiveName="強行運搬",passiveText="危険区間で発生する最初の追加日数を1日防ぐ。",
   activeName="突破",activeText="戦闘中、一度だけ攻撃札を強化する。",
   sealName="強行印",sealText="次区間の所要日数を1日減らす。",sealTarget="ROUTE",
   branches=new[]{new RoleBranchDef("破砕経路","高危険区間を通過した後、次の戦闘札を強化。"),new RoleBranchDef("護送刃","地点失敗による追加日数を一度だけ防ぐ。")}
  },
  ["guardian"]=new RoleFrameworkDef{
   id="guardian",name="守護者",passiveName="固定護送",passiveText="地点失敗による最初の追加日数を無効化する。",
   activeName="庇護",activeText="戦闘中、一度だけ大きな防御を得る。",
   sealName="庇護印",sealText="次地点で発生する追加日数を1日防ぐ。",sealTarget="DELAY",
   branches=new[]{new RoleBranchDef("固定護送","中継所へ向かう所要日数を初回だけ1日減らす。"),new RoleBranchDef("身代わり封","地点失敗による追加日数を一度だけ防ぐ。")}
  },
  ["scout"]=new RoleFrameworkDef{
   id="scout",name="斥候",passiveName="先読み",passiveText="各段の候補区間の危険度を公開する。",
   activeName="見切り",activeText="戦闘中、一度だけ回避し札を引く。",
   sealName="先読印",sealText="次区間の所要日数を1日減らす。",sealTarget="ROUTE",
   branches=new[]{new RoleBranchDef("先読経路","次段の隠された条件を公開する。"),new RoleBranchDef("影渡り","危険経路の地点情報を一段先まで公開する。")}
  },
  ["artificer"]=new RoleFrameworkDef{
   id="artificer",name="術師",passiveName="術式再編",passiveText="配達印のいずれか1枚の使用回数を1増やす。",
   activeName="短絡",activeText="戦闘中、一度だけ札の消費ENを軽減する。",
   sealName="再編印",sealText="使用済み配達印を1回だけ再使用可能にする。",sealTarget="SEAL",
   branches=new[]{new RoleBranchDef("再充填","色一致3マスごとに配達印の使用回数を追加。"),new RoleBranchDef("術式短絡","異なる色を3種揃えると緊急印を得る。")}
  }
 };

 public static IEnumerable<RoleFrameworkDef> All=>CoreRoleIds.Select(Get);
 public static RoleFrameworkDef Get(string id)=>Definitions.TryGetValue(CoreRoleId(id),out var value)?value:Definitions["warrior"];

 public static string CoreRoleId(string id){
  if(!string.IsNullOrEmpty(id)&&Definitions.ContainsKey(id))return id;
  if(!string.IsNullOrEmpty(id)&&GameCatalog.Roles.TryGetValue(id,out var role)&&Definitions.ContainsKey(role.family))return role.family;
  return "warrior";
 }

 public static void MigrateLegacyRole(MetaSave save){
  if(save==null)return;
  string previous=save.currentRole;
  string core=CoreRoleId(previous);
  if(!string.IsNullOrEmpty(previous)&&previous!=core&&string.IsNullOrEmpty(save.qualificationSealId))
   save.qualificationSealId=previous;
  save.currentRole=core;
  NormalizeBranches(save);
 }

 public static void Normalize(MetaSave save){
  if(save==null)return;
  save.currentRole=CoreRoleId(save.currentRole);
  save.unlockedRoles??=new List<string>();
  if(!string.IsNullOrEmpty(save.qualificationSealId)){
   string seal=save.qualificationSealId;
   bool valid=save.unlockedRoles.Contains(seal)&&GameCatalog.Roles.ContainsKey(seal)&&!CoreRoleIds.Contains(seal);
   if(!valid)save.qualificationSealId="";
  }
  NormalizeBranches(save);
 }

 static void NormalizeBranches(MetaSave save){
  save.roleBranches??=new List<IdInt>();
  var unique=save.roleBranches.Where(x=>x!=null&&CoreRoleIds.Contains(x.id))
   .GroupBy(x=>x.id).ToDictionary(x=>x.Key,x=>Mathf.Clamp(x.First().value,0,1));
  save.roleBranches=CoreRoleIds.Select(id=>new IdInt(id,unique.TryGetValue(id,out var value)?value:0)).ToList();
 }

 public static int Branch(MetaSave save,string roleId){
  string id=CoreRoleId(roleId);
  return Mathf.Clamp(save?.roleBranches?.FirstOrDefault(x=>x.id==id)?.value??0,0,1);
 }

 public static void SetBranch(MetaSave save,string roleId,int branch){
  if(save==null)return;
  NormalizeBranches(save);
  string id=CoreRoleId(roleId);
  save.roleBranches.First(x=>x.id==id).value=Mathf.Clamp(branch,0,1);
 }
}
}
