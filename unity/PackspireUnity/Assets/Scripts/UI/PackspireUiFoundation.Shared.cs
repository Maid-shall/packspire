using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire {
public sealed partial class PackspireUiFoundation {
 VisualElement TabletopDesk(string extraClass){var desk=Container("ps-tabletop-desk "+extraClass);if(tabletopDesk!=null)desk.Add(Image(tabletopDesk,new Rect(0,0,1,1),"ps-tabletop-desk-art",ScaleMode.ScaleAndCrop));return desk;}
 Button TabletopBack(){var button=new Button(()=>game.UiNavigate(ScreenId.Hub)){text="机の中央へ戻る"};button.AddToClassList("ps-tabletop-back");return button;}

 void ShowToast(string message){if(toast==null)return;toast.Clear();toast.Add(new Label(message));toast.style.display=DisplayStyle.Flex;toast.style.opacity=0f;toast.style.translate=new Translate(0,-10,0);toast.schedule.Execute(()=>{if(toast==null)return;toast.style.opacity=1f;toast.style.translate=new Translate(0,0,0);}).StartingIn(16);toast.schedule.Execute(()=>{if(toast==null)return;toast.style.opacity=0f;toast.style.translate=new Translate(0,-8,0);}).StartingIn(1700);toast.schedule.Execute(()=>{if(toast!=null)toast.style.display=DisplayStyle.None;}).StartingIn(1950);}
 VisualElement Milestone(int required,int current,string title,string description){var item=Container(current>=required?"ps-milestone ps-unlocked":"ps-milestone ps-locked");item.Add(PackspireUiFactory.Title($"Lv.{required}　{title}"));item.Add(PackspireUiFactory.Body(description));return item;}

 VisualElement BookShell(string title,VisualElement singleContent){nextPageIsLeft=true;var shell=Container("ps-book-screen");if(game.UiBookArt!=null)shell.Add(Image(game.UiBookArt,new Rect(0,0,1,1),"ps-book-background",ScaleMode.StretchToFill));var heading=new Label(title);heading.AddToClassList("ps-book-heading");shell.Add(heading);var pages=Container("ps-book-pages");pages.name="book-pages";shell.Add(pages);if(singleContent!=null)pages.Add(singleContent);return shell;}
 VisualElement Page(string title){var page=new ScrollView();page.AddToClassList("ps-book-page");page.AddToClassList(nextPageIsLeft?"ps-page-left":"ps-page-right");nextPageIsLeft=!nextPageIsLeft;var heading=Container("ps-page-heading");var label=PackspireUiFactory.Title(title);label.AddToClassList("ps-page-heading-title");heading.Add(label);heading.Add(InkRule());page.Add(heading);return page;}
 void AddBookTabs(VisualElement shell,ScreenId current){var tabs=Container("ps-book-tabs");(string,ScreenId)[] values={("拠点",ScreenId.Hub),("遠征",ScreenId.Expedition),("荷造り",ScreenId.Pack),("保管",ScreenId.Vault),("役職",ScreenId.Status),("勢力",ScreenId.Faction),("図鑑",ScreenId.Compendium)};foreach(var entry in values){var target=entry.Item2;var button=new Button(()=>game.UiNavigate(target)){tooltip=entry.Item1};button.AddToClassList("ps-book-tab");var label=new Label(entry.Item1);label.AddToClassList("ps-book-tab-label");button.Add(label);if(target==current){button.AddToClassList("ps-selected");button.SetEnabled(false);}tabs.Add(button);}shell.Add(tabs);}

 VisualElement RecordButton(string title,string subtitle,VisualElement art,bool selected,System.Action clicked){var button=new Button(clicked){tooltip=title+"\n"+subtitle};button.AddToClassList("ps-record-button");if(selected)button.AddToClassList("ps-selected");button.Add(art);var copy=Container("ps-record-copy");copy.Add(PackspireUiFactory.Title(title));copy.Add(PackspireUiFactory.Body(subtitle));button.Add(copy);if(selected)button.Add(SelectionBadge());button.Add(InkRule());return button;}
 VisualElement AtlasButton(Texture2D texture,Rect uv,string label,bool selected,System.Action clicked){var button=new Button(clicked){tooltip=label};button.AddToClassList("ps-atlas-button");if(selected)button.AddToClassList("ps-selected");button.Add(Atlas(texture,uv,"ps-atlas-image"));var name=new Label(label);name.AddToClassList("ps-atlas-label");button.Add(name);if(selected)button.Add(SelectionBadge());return button;}
 VisualElement SelectionBadge(){var badge=new Label("選択");badge.AddToClassList("ps-selection-badge");return badge;}
 VisualElement InkRule(){var rule=Container("ps-ink-rule");rule.pickingMode=PickingMode.Ignore;return rule;}
 VisualElement Container(string classes){var element=new VisualElement();foreach(var value in classes.Split(' '))element.AddToClassList(value);return element;}
 Image Image(Texture2D texture,Rect uv,string className,ScaleMode mode){var image=new Image{image=texture,uv=uv,scaleMode=mode,pickingMode=PickingMode.Ignore};image.AddToClassList(className);return image;}
 Image Atlas(Texture2D texture,Rect uv,string className)=>Image(texture,uv,className,ScaleMode.ScaleToFit);

 string FactionName(string id)=>GameCatalog.Factions.FirstOrDefault(x=>x.id==id)?.name??id;
 string BackpackName(string id)=>GameCatalog.Backpacks.FirstOrDefault(x=>x.id==id)?.name??id;
 string ItemTypeLabel(ItemType type)=>type==ItemType.Weapon?"武器":type==ItemType.Armor?"防具":type==ItemType.Rune?"ルーン":"消耗品";
 string ElementLabel(Element value)=>value==Element.Fire?"火":value==Element.Water?"水":value==Element.Wind?"風":"土";
 Rect ItemUv(string id){int index=id=="sword"?0:id=="shield"?1:id=="ember"?2:id=="herb"?3:id=="dagger"?4:id=="plate"?5:id=="crystal"?6:id=="bomb"?7:id=="spear"?8:id=="buckler"?9:id=="flask"?10:id=="charm"?11:0;return new Rect(index%4*.25f,(2-index/4)/3f,.25f,1f/3f);}
 Rect RoleUv(string id){int index=id=="warrior"?0:id=="guardian"?1:id=="scout"?2:id=="artificer"?3:id=="blade_master"?4:id=="bulwark"?5:id=="hunter"?6:id=="grand_artificer"?7:id=="arsenal_lord"?8:id=="pack_saint"?9:id=="rune_weaver"?10:id=="grid_dancer"?11:id.Contains("knight")?5:id.Contains("druid")?6:id.Contains("artificer")||id.Contains("channeler")?7:0;return new Rect(index%4*.25f,(2-index/4)/3f,.25f,1f/3f);}
 Rect EnemyUv(string id)=>id=="sentinel"?new Rect(0,.5f,.174f,.5f):id=="rats"?new Rect(.174f,.5f,.172f,.5f):id=="porter"?new Rect(.346f,.5f,.172f,.5f):id=="mage"?new Rect(.518f,.5f,.172f,.5f):id=="beast"?new Rect(0,0,.344f,.5f):id=="knight"?new Rect(.344f,0,.347f,.5f):new Rect(.69f,0,.31f,1);
 Rect CharacterUv(int body,int hair)=>new Rect(Mathf.Clamp(body,0,3)*.25f,(2-Mathf.Clamp(hair,0,2))/3f,.25f,1f/3f);
 Rect FactionUv(string id)=>id=="iron"?new Rect(0,.5f,.5f,.5f):id=="spore"?new Rect(.5f,.5f,.5f,.5f):id=="guild"?new Rect(0,0,.5f,.5f):new Rect(.5f,0,.5f,.5f);
 Rect DungeonUv(string id)=>id=="hollow_archive"?new Rect(.5f,.5f,.5f,.5f):id=="ash_forge"?new Rect(.5f,0,.5f,.5f):new Rect(0,.5f,.5f,.5f);
}
}
