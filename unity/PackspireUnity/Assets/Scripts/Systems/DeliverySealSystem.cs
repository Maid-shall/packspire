using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Packspire {
[Serializable]
public sealed class DeliverySealState {
 public string key,cardId,name,source,target,text;
 public int charges=1,maxCharges=1;
 public bool roleSignature,available=true;
}

/// <summary>
/// Derives delivery seals from the active role and storage-formula color matches.
/// This is the single source used by both packing presentation and route runtime.
/// </summary>
public static class DeliverySealSystem {
 public const string RouteTarget="ROUTE";
 public const string DelayTarget="DELAY";
 public const string SealTarget="SEAL";

 public static List<DeliverySealState> Build(RunState run,MetaSave meta,IDictionary<Element,int> colors){
  var result=new List<DeliverySealState>();
  var role=RoleFrameworkSystem.Get(run?.role);
  int branch=RoleFrameworkSystem.Branch(meta,role.id);
  int signatureCharges=1+(role.id=="artificer"&&branch==0?1:0)+
   (string.IsNullOrEmpty(meta?.qualificationSealId)?0:1);
  result.Add(new DeliverySealState{
   key="role-signature",cardId="role_"+role.id,name=role.sealName,source=role.name,
   target=role.sealTarget,text=role.sealText,charges=signatureCharges,
   maxCharges=signatureCharges,roleSignature=true
  });
  AddColorSeal(result,colors,Element.Fire);
  AddColorSeal(result,colors,Element.Water);
  AddColorSeal(result,colors,Element.Wind);
  AddColorSeal(result,colors,Element.Earth);
  return result;
 }

 public static List<DeliverySealState> Refresh(
  IEnumerable<DeliverySealState> current,RunState run,MetaSave meta,IDictionary<Element,int> colors){
  var currentList=(current??Enumerable.Empty<DeliverySealState>()).Where(seal=>seal!=null).ToList();
  var spentByKey=currentList
   .GroupBy(seal=>seal.key)
   .ToDictionary(group=>group.Key,group=>group.Max(seal=>Mathf.Max(0,seal.maxCharges-seal.charges)));
  var refreshed=Build(run,meta,colors);
  foreach(var seal in refreshed)
   if(spentByKey.TryGetValue(seal.key,out int spent))
    seal.charges=Mathf.Max(0,seal.maxCharges-spent);
  var activeKeys=refreshed.Select(seal=>seal.key).ToHashSet();
  foreach(var retired in currentList.Where(seal=>!activeKeys.Contains(seal.key)))
   refreshed.Add(Copy(retired,false));
  return refreshed;
 }

 public static int Charges(Element element,IDictionary<Element,int> colors){
  if(colors==null||!colors.TryGetValue(element,out int matches)||matches<=0)return 0;
  return Mathf.Clamp((matches+1)/2,1,3);
 }

 public static string Name(Element element)=>element switch{
  Element.Fire=>"焼却印",
  Element.Water=>"冷却印",
  Element.Wind=>"消音印",
  Element.Earth=>"補綴印",
  _=>"配達印"
 };

 public static string Target(Element element)=>element switch{
  Element.Fire=>RouteTarget,
  Element.Wind=>RouteTarget,
  _=>DelayTarget
 };

 public static string Effect(Element element)=>element switch{
  Element.Fire=>"次区間の期限消費を1減らす。",
  Element.Water=>"次地点で発生する追加日数を1日防ぐ。",
  Element.Wind=>"次区間の所要日数を1日減らす。",
  Element.Earth=>"次地点で発生する追加日数を1日防ぐ。",
  _=>"経路判定へ使用する。"
 };

 static void AddColorSeal(List<DeliverySealState> result,IDictionary<Element,int> colors,Element element){
  int charges=Charges(element,colors);
  if(charges<=0)return;
  result.Add(new DeliverySealState{
   key="color-"+element.ToString().ToLowerInvariant(),
   cardId="seal_"+element.ToString().ToLowerInvariant(),
   name=Name(element),source=ElementSource(element),target=Target(element),text=Effect(element),
   charges=charges,maxCharges=charges
  });
 }

 static string ElementSource(Element element)=>element switch{
  Element.Fire=>"火色一致",
  Element.Water=>"水色一致",
  Element.Wind=>"風色一致",
  Element.Earth=>"土色一致",
  _=>"色一致"
 };

 static DeliverySealState Copy(DeliverySealState source,bool available)=>new(){
  key=source.key,cardId=source.cardId,name=source.name,source=source.source,
  target=source.target,text=source.text,charges=source.charges,maxCharges=source.maxCharges,
  roleSignature=source.roleSignature,available=available
 };
}
}
