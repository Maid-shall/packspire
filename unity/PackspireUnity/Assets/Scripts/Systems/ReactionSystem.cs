using System;
using System.Collections.Generic;
using System.Linq;

namespace Packspire {
public sealed class ReactionSourceValue {
 public string reactionId,sourceId;
 public int value;
 public ReactionScope scope;
 public ReactionStackRule stackRule;
}

public sealed class ReactionSnapshot {
 readonly List<ReactionSourceValue> values;
 public IReadOnlyList<ReactionSourceValue> Values=>values;

 public ReactionSnapshot(IEnumerable<ReactionSourceValue> values){
  this.values=(values??Enumerable.Empty<ReactionSourceValue>())
   .Where(value=>value!=null&&!string.IsNullOrEmpty(value.reactionId)&&value.value!=0)
   .ToList();
 }

 public int Total(string reactionId,ReactionScopeMask scopes=ReactionScopeMask.All){
  var matching=values.Where(value=>value.reactionId==reactionId&&Includes(scopes,value.scope)).ToArray();
  int additive=matching.Where(value=>value.stackRule==ReactionStackRule.Add).Sum(value=>value.value);
  int highest=matching.Where(value=>value.stackRule==ReactionStackRule.Highest)
   .Select(value=>value.value).DefaultIfEmpty(0).Max();
  int unique=matching.Where(value=>value.stackRule==ReactionStackRule.UniqueSource)
   .GroupBy(value=>value.sourceId??"")
   .Sum(group=>group.Select(value=>value.value).DefaultIfEmpty(0).Max());
  return additive+highest+unique;
 }

 public int SourceCount(string reactionId,ReactionScopeMask scopes=ReactionScopeMask.All)=>
  values.Where(value=>value.reactionId==reactionId&&Includes(scopes,value.scope))
   .Select(value=>value.sourceId??"").Distinct().Count();

 public IReadOnlyList<ReactionSourceValue> Breakdown(string reactionId,ReactionScopeMask scopes=ReactionScopeMask.All)=>
  values.Where(value=>value.reactionId==reactionId&&Includes(scopes,value.scope)).ToArray();

 public static bool Includes(ReactionScopeMask mask,ReactionScope scope)=>
  (mask&(ReactionScopeMask)(1<<(int)scope))!=0;
}

/// <summary>
/// Builds the shared discovery/reaction language from role mastery, equipped items,
/// expedition state and short-lived context. Serialized content stays list-based;
/// runtime lookup and aggregation may use dictionaries safely.
/// </summary>
public static class ReactionSystem {
 public static ReactionSnapshot Build(MetaSave meta,RunState run=null,
  IEnumerable<ReactionValueState> momentValues=null){
  var values=new List<ReactionSourceValue>();
  if(meta!=null){
   foreach(var level in meta.jobLevels??new List<IdInt>()){
    if(level==null||level.value<=0)continue;
    var role=PackspireContent.Data.roles.FirstOrDefault(value=>value.id==level.id);
    if(role==null)continue;
    Add(values,role.reactionContributions,$"role:{role.id}",level.value);
   }
   Add(values,meta.memoryReactions,"memory");
  }
  if(run!=null){
   var current=PackspireContent.Data.roles.FirstOrDefault(value=>value.id==run.role);
   if(current!=null)Add(values,current.currentRoleContributions,$"current-role:{current.id}",1);

   var placed=(run.placements??new List<Placement>())
    .Where(value=>value!=null&&!string.IsNullOrEmpty(value.itemUid))
    .GroupBy(value=>value.itemUid).Select(group=>group.First());
   foreach(var placement in placed){
    var instance=(run.inventory??new List<ItemInstance>()).FirstOrDefault(value=>value.uid==placement.itemUid);
    if(instance==null)continue;
    var item=PackspireContent.Data.items.FirstOrDefault(value=>value.id==instance.templateId);
    if(item!=null)Add(values,item.reactionContributions,$"item:{instance.uid}",1);
   }
   Add(values,run.reactionValues,"run");
  }
  Add(values,momentValues,"moment");
  return new ReactionSnapshot(values);
 }

 public static bool Meets(RoleUnlockRecipeContent recipe,ReactionSnapshot snapshot){
  if(recipe==null||snapshot==null||recipe.requirements==null||recipe.requirements.Length==0)return false;
  return recipe.requirements.All(requirement=>
   requirement!=null&&
   snapshot.Total(requirement.reactionId,requirement.acceptedScopes)>=requirement.minimum&&
   (requirement.minimumSources<=0||
    snapshot.SourceCount(requirement.reactionId,requirement.acceptedScopes)>=requirement.minimumSources));
 }

 public static RoleUnlockRecipeContent MatchingRecipe(RoleContent role,ReactionSnapshot snapshot)=>
  (role?.unlockRecipes??Array.Empty<RoleUnlockRecipeContent>()).FirstOrDefault(recipe=>Meets(recipe,snapshot));

 public static IReadOnlyList<string> DiscoverRoles(MetaSave meta,RunState run=null,
  IEnumerable<ReactionValueState> momentValues=null){
  if(meta==null)return Array.Empty<string>();
  meta.unlockedRoles??=new List<string>();
  var snapshot=Build(meta,run,momentValues);
  var discovered=new List<string>();
  foreach(var role in PackspireContent.Data.roles){
   if(meta.unlockedRoles.Contains(role.id))continue;
   if(MatchingRecipe(role,snapshot)==null)continue;
   meta.unlockedRoles.Add(role.id);
   discovered.Add(role.id);
  }
  return discovered;
 }

 public static void Grant(MetaSave meta,RunState run,ReactionContributionContent contribution,string sourceId){
  if(contribution==null||contribution.amount==0||string.IsNullOrEmpty(contribution.reactionId))return;
  var state=new ReactionValueState(contribution.reactionId,contribution.amount,contribution.scope,sourceId);
  if(contribution.scope==ReactionScope.Memory){
   if(meta==null)return;
   meta.memoryReactions??=new List<ReactionValueState>();
   Merge(meta.memoryReactions,state);
  } else {
   if(run==null)return;
   run.reactionValues??=new List<ReactionValueState>();
   Merge(run.reactionValues,state);
  }
 }

 public static void ClearScope(RunState run,ReactionScope scope){
  if(run?.reactionValues==null)return;
  run.reactionValues.RemoveAll(value=>value==null||value.scope==scope);
 }

 static void Merge(List<ReactionValueState> target,ReactionValueState incoming){
  var existing=target.FirstOrDefault(value=>value!=null&&value.id==incoming.id&&
   value.scope==incoming.scope&&value.sourceId==incoming.sourceId);
  if(existing==null)target.Add(incoming);
  else existing.value+=incoming.value;
 }

 static void Add(List<ReactionSourceValue> target,
  IEnumerable<ReactionContributionContent> contributions,string sourceId,int multiplier){
  foreach(var contribution in contributions??Enumerable.Empty<ReactionContributionContent>()){
   if(contribution==null)continue;
   int value=contribution.amount*(contribution.perLevel?Math.Max(0,multiplier):1);
   target.Add(new ReactionSourceValue{
    reactionId=contribution.reactionId,value=value,scope=contribution.scope,
    sourceId=sourceId,stackRule=contribution.stackRule
   });
  }
 }

 static void Add(List<ReactionSourceValue> target,IEnumerable<ReactionValueState> states,string fallbackSource){
  foreach(var state in states??Enumerable.Empty<ReactionValueState>()){
   if(state==null)continue;
   target.Add(new ReactionSourceValue{
    reactionId=state.id,value=state.value,scope=state.scope,
    sourceId=string.IsNullOrEmpty(state.sourceId)?fallbackSource:state.sourceId,
    stackRule=ReactionStackRule.Add
   });
  }
 }
}
}
