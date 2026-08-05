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

 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
 static void ResetCache()=>Cache.Clear();

 public static T Load<T>(string path) where T:UnityEngine.Object {
  if(string.IsNullOrWhiteSpace(path))return null;
  string key=typeof(T).FullName+"|"+path;
  if(Cache.TryGetValue(key,out var cached)){
   if(cached!=null)return cached as T;
   Cache.Remove(key);
  }
  var asset=Resources.Load<T>(path);
  if(asset!=null)Cache[key]=asset;
  return asset;
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
 }

#if UNITY_EDITOR
 public static void ClearCacheForTests()=>Cache.Clear();
#endif
}
}
