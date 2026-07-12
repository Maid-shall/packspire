using System.IO;
using UnityEngine;
namespace Packspire {
public static class SaveSystem {
 const string Key="packspire_unity_save_v1";
 public static MetaSave Load(){try{var json=PlayerPrefs.GetString(Key,"");return string.IsNullOrEmpty(json)?new MetaSave():JsonUtility.FromJson<MetaSave>(json)??new MetaSave();}catch{return new MetaSave();}}
 public static void Save(MetaSave data){PlayerPrefs.SetString(Key,JsonUtility.ToJson(data));PlayerPrefs.Save();}
 public static void Reset(){PlayerPrefs.DeleteKey(Key);PlayerPrefs.Save();}
 public static string Export(MetaSave data)=>JsonUtility.ToJson(data,true);
}
}
