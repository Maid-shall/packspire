using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire {
public sealed partial class PackspireUiFoundation {
 ScrollView mgmtListScroll;
 VisualElement mgmtArtHost;
 ScrollView mgmtDetailScroll;
 VisualElement mgmtListHeader;
 float mgmtListScrollY;
 int vaultFilter;

 VisualElement BuildManagementShell(string eyebrow,string title,ScreenId current,out ScrollView listScroll,out VisualElement artHost,out ScrollView detailScroll){
  var shell=Container("ps-mgmt-screen");
  var bg=HubBackgroundArt();
  if(bg==null)bg=CourtyardArt();
  if(bg!=null)shell.Add(Image(bg,new Rect(0,0,1,1),"ps-mgmt-bg",ScaleMode.ScaleAndCrop));
  var shade=Container("ps-mgmt-shade");
  shade.pickingMode=PickingMode.Ignore;
  shell.Add(shade);

  var header=Container("ps-mgmt-header");
  header.Add(ChromeBrand(eyebrow,title));
  var back=new Button(()=>game.UiNavigate(ScreenId.Hub)){text="拠点へ戻る"};
  back.AddToClassList("ps-mgmt-back");
  back.AddToClassList("ps-chrome-action");
  header.Add(back);
  shell.Add(header);

  var body=Container("ps-mgmt-body");
  var listCol=Container("ps-mgmt-col-list");
  mgmtListHeader=Container("ps-mgmt-list-header");
  listCol.Add(mgmtListHeader);
  listScroll=new ScrollView(ScrollViewMode.Vertical);
  listScroll.AddToClassList("ps-mgmt-list-scroll");
  listScroll.verticalScrollerVisibility=ScrollerVisibility.Auto;
  listCol.Add(listScroll);

  var artCol=Container("ps-mgmt-col-art");
  artHost=Container("ps-mgmt-art-host");
  artCol.Add(artHost);

  var detailCol=Container("ps-mgmt-col-detail");
  detailScroll=new ScrollView(ScrollViewMode.Vertical);
  detailScroll.AddToClassList("ps-mgmt-detail-scroll");
  detailScroll.verticalScrollerVisibility=ScrollerVisibility.Auto;
  detailCol.Add(detailScroll);

  body.Add(listCol);
  body.Add(artCol);
  body.Add(detailCol);
  shell.Add(body);

  mgmtListScroll=listScroll;
  mgmtArtHost=artHost;
  mgmtDetailScroll=detailScroll;
  return shell;
 }

 void SaveMgmtListScroll(){if(mgmtListScroll!=null)mgmtListScrollY=mgmtListScroll.scrollOffset.y;}
 void RestoreMgmtListScroll(){if(mgmtListScroll!=null)mgmtListScroll.scrollOffset=new Vector2(0,mgmtListScrollY);}

 void ClearMgmtArt(){mgmtArtHost?.Clear();}
 void ClearMgmtDetail(){mgmtDetailScroll?.Clear();}

 void SetMgmtArt(VisualElement art,string aspectClass,bool unknown=false){
  ClearMgmtArt();
  if(mgmtArtHost==null||art==null)return;
  var frame=Container("ps-mgmt-art-frame "+aspectClass);
  if(unknown)frame.AddToClassList("ps-mgmt-art-unknown");
  frame.pickingMode=PickingMode.Ignore;
  art.pickingMode=PickingMode.Ignore;
  art.style.flexGrow=0;
  art.style.flexShrink=0;
  art.style.alignSelf=Align.Center;
  frame.Add(art);
  mgmtArtHost.Add(frame);
 }

 void UpdateMgmtListSelection(string selectedId){
  if(mgmtListScroll==null)return;
  foreach(var child in mgmtListScroll.contentContainer.Children()){
   if(child is not Button button)continue;
   bool selected=button.userData is string id&&id==selectedId;
   button.EnableInClassList("ps-selected",selected);
  }
 }

 Button ManagementListRow(string id,VisualElement icon,string primary,string secondary,bool selected,System.Action onClick){
  var row=new Button(onClick){userData=id};
  row.AddToClassList("ps-mgmt-list-row");
  if(selected)row.AddToClassList("ps-selected");
  if(icon!=null){
   icon.AddToClassList("ps-mgmt-list-icon");
   icon.pickingMode=PickingMode.Ignore;
   row.Add(icon);
  }
  var copy=Container("ps-mgmt-list-copy");
  copy.pickingMode=PickingMode.Ignore;
  var name=new Label(primary);
  name.AddToClassList("ps-mgmt-list-name");
  copy.Add(name);
  if(!string.IsNullOrEmpty(secondary)){
   var sub=new Label(secondary);
   sub.AddToClassList("ps-mgmt-list-sub");
   copy.Add(sub);
  }
  row.Add(copy);
  return row;
 }

 VisualElement ManagementFilterBar(string[] labels,int selectedIndex,System.Action<int> onPick){
  var bar=Container("ps-mgmt-filter-bar");
  for(int i=0;i<labels.Length;i++){
   int index=i;
   var button=PackspireUiFactory.Button(labels[i],()=>onPick(index));
   button.AddToClassList("ps-mgmt-filter");
   if(i==selectedIndex)button.AddToClassList("ps-selected");
   bar.Add(button);
  }
  return bar;
 }

 VisualElement ManagementSection(string title,string body,bool dim=false){
  var section=Container(dim?"ps-mgmt-section ps-mgmt-section-dim":"ps-mgmt-section");
  if(!string.IsNullOrEmpty(title)){
   var heading=PackspireUiFactory.Title(title);
   heading.AddToClassList("ps-mgmt-section-title");
   section.Add(heading);
  }
  if(!string.IsNullOrEmpty(body))section.Add(PackspireUiFactory.Body(body));
  return section;
 }

 VisualElement ManagementCharacterHeader(CharacterDef character,MetaSave meta){
  var learned=meta.jobLevels.Where(x=>x.value>0&&GameCatalog.Roles.ContainsKey(x.id)).ToList();
  int totalLevel=learned.Sum(x=>x.value);
  var currentRole=GameCatalog.Roles.ContainsKey(meta.currentRole)?GameCatalog.Roles[meta.currentRole].name:meta.currentRole;
  var box=Container("ps-mgmt-char-header");
  box.Add(PackspireUiFactory.Title(character.name));
  box.Add(PackspireUiFactory.Body($"{character.title}　合計Lv.{totalLevel}"));
  box.Add(PackspireUiFactory.Body($"主役職　{currentRole}"));
  return box;
 }

 VisualElement SmallAtlasIcon(Texture2D texture,Rect uv,bool unknown=false){
  var icon=Atlas(texture,uv,"ps-mgmt-thumb");
  if(unknown)icon.AddToClassList("ps-mgmt-thumb-unknown");
  return icon;
 }

 VisualElement SmallPortraitIcon(VisualElement portrait,bool unknown=false){
  portrait.AddToClassList("ps-mgmt-thumb");
  if(unknown)portrait.AddToClassList("ps-mgmt-thumb-unknown");
  return portrait;
 }

 bool VaultItemInLoadout(MetaSave meta,string uid)=>meta.loadouts!=null&&meta.loadouts.Any(l=>l.slots!=null&&l.slots.Any(s=>s.itemUid==uid));
 string VaultLoadoutName(MetaSave meta,string uid){
  var loadout=meta.loadouts?.FirstOrDefault(l=>l.slots!=null&&l.slots.Any(s=>s.itemUid==uid));
  return loadout?.name??"";
 }
}
}
