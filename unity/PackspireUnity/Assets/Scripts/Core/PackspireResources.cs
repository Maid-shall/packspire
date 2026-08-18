using System;
using System.Collections.Generic;
using UnityEngine;

namespace Packspire {
/// <summary>
/// Centralized runtime access to Resources assets.
/// Keeps repeated UI lookups allocation-free and provides one migration point
/// if the project moves to Addressables later.
/// </summary>
public static class PackspireResources {
 static readonly Dictionary<string,UnityEngine.Object> Cache=new();
 static readonly Dictionary<string,UnityEngine.Object[]> ArrayCache=new();
 static readonly Dictionary<string,UsageCounter> Usage=new();

 sealed class UsageCounter {
  public int requests;
  public int cacheHits;
  public int misses;
 }

 public readonly struct UsageSnapshot {
  public readonly string key;
  public readonly int requests;
  public readonly int cacheHits;
  public readonly int misses;

  public UsageSnapshot(string key,int requests,int cacheHits,int misses){
   this.key=key;
   this.requests=requests;
   this.cacheHits=cacheHits;
   this.misses=misses;
  }
 }

 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
 static void ResetCache(){Cache.Clear();ArrayCache.Clear();Usage.Clear();}

 public static T Load<T>(string path) where T:UnityEngine.Object {
  if(string.IsNullOrWhiteSpace(path))return null;
  string key=typeof(T).FullName+"|"+path;
  UsageCounter usage=RecordRequest(key);
  if(Cache.TryGetValue(key,out var cached)){
   if(cached!=null){usage.cacheHits++;return cached as T;}
   Cache.Remove(key);
  }
  var asset=Resources.Load<T>(path);
  if(asset!=null)Cache[key]=asset;
  else usage.misses++;
  return asset;
 }

 public static T[] LoadAll<T>(string path) where T:UnityEngine.Object {
  if(string.IsNullOrWhiteSpace(path))return Array.Empty<T>();
  string key=typeof(T).FullName+"[]|"+path;
  UsageCounter usage=RecordRequest(key);
  if(ArrayCache.TryGetValue(key,out var cached)){
   usage.cacheHits++;
   return cached as T[]??Array.Empty<T>();
  }
  T[] assets=Resources.LoadAll<T>(path);
  if(assets==null||assets.Length==0){usage.misses++;return Array.Empty<T>();}
  ArrayCache[key]=assets;
  return assets;
 }

 public static T LoadFirst<T>(params string[] paths) where T:UnityEngine.Object {
  if(paths==null)return null;
  foreach(var path in paths){
   var asset=Load<T>(path);
   if(asset!=null)return asset;
  }
  return null;
 }

 public static void Invalidate<T>(string path) where T:UnityEngine.Object {
  if(string.IsNullOrWhiteSpace(path))return;
  Cache.Remove(typeof(T).FullName+"|"+path);
  ArrayCache.Remove(typeof(T).FullName+"[]|"+path);
 }

 public static IReadOnlyList<UsageSnapshot> GetUsageSnapshot(){
  var result=new List<UsageSnapshot>(Usage.Count);
  foreach(var pair in Usage)
   result.Add(new UsageSnapshot(pair.Key,pair.Value.requests,pair.Value.cacheHits,pair.Value.misses));
  result.Sort(static (left,right)=>string.CompareOrdinal(left.key,right.key));
  return result;
 }

 static UsageCounter RecordRequest(string key){
  if(!Usage.TryGetValue(key,out var counter)){
   counter=new UsageCounter();
   Usage[key]=counter;
  }
  counter.requests++;
  return counter;
 }

#if UNITY_EDITOR
 public static void ClearCacheForTests()=>ResetCache();
#endif
}
}
