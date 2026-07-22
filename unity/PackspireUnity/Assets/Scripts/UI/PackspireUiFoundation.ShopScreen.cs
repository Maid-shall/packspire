using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire {
public sealed partial class PackspireUiFoundation {
 VisualElement shopShell;
 VisualElement shopProductGrid;
 ScrollView shopProductScroll;
 VisualElement shopDetailHost;
 ScrollView shopDetailScroll;
 VisualElement shopMerchantScene;
 VisualElement shopMerchantBackdropLayer;
 VisualElement shopMerchantCharacterViewport;
 Image shopMerchantCharacterImage;
 VisualElement shopMerchantDialogueLayer;
 Label shopMerchantDialogue;
 VisualElement shopMerchantCounterLayer;
 VisualElement shopMerchantTransactionLayer;
 VisualElement shopFutureMerchantActionLayer;
 Label shopGoldLabel;
 Label shopTotalLabel;
 Label shopSelectedNameLabel;
 Label shopPurchaseReason;
 Button shopBuyButton;
 Button shopLeaveButton;
 VisualElement shopFilterHost;
 Label shopDevHintLabel;
 int shopCategoryFilter;
 float shopProductScrollY;
 bool shopPreviewMode;
 MerchantPresentation shopMerchant;
 Texture2D shopMerchantCharacterTex;

#if UNITY_EDITOR
 int shopBuildCount;
 int shopDetailRefreshCount;
#endif

 static int ShopItemPrice(ItemDef item)=>14+item.cells.Length*4;

 string[] ShopStockIds()=>GameCatalog.Items.Keys.Take(5).ToArray();

 IEnumerable<string> FilteredShopStock(){
  var stock=ShopStockIds();
  if(shopCategoryFilter<=0)return stock;
  return stock.Where(id=>{
   if(!GameCatalog.Items.TryGetValue(id,out var item))return false;
   return shopCategoryFilter switch{
    1=>item.type==ItemType.Weapon,
    2=>item.type==ItemType.Armor,
    3=>item.type==ItemType.Rune,
    4=>item.type==ItemType.Supply,
    _=>true
   };
  });
 }

 void BuildShop(){
#if UNITY_EDITOR
  shopBuildCount++;
  Debug.Log($"[PackspireQA] BuildShop count={shopBuildCount}");
#endif
  if(game.UiRun!=null)shopPreviewMode=false;
  shopPreviewMode=shopPreviewMode||game.UiRun==null;
  shopMerchant=MerchantCatalog.Default;
  EnsureShopSelection();

  shopShell=Container("ps-shop-screen ps-dark-surface");
  var backgroundHost=Container("ps-layer-background");
  var bg=HubBackgroundArt()??CourtyardArt();
  if(bg!=null)backgroundHost.Add(Image(bg,new Rect(0,0,1,1),"ps-mgmt-bg",ScaleMode.ScaleAndCrop));
  var shade=Container("ps-mgmt-shade");
  shade.pickingMode=PickingMode.Ignore;
  backgroundHost.Add(shade);
  shopShell.Add(backgroundHost);

  var contentHost=Container("ps-layer-content");
  var header=Container("ps-shop-header");
  header.Add(ChromeBrand("MERCHANT  /  COUNTER",shopMerchant.displayName));
  shopDevHintLabel=new Label("DEVプレビュー"){pickingMode=PickingMode.Ignore};
  shopDevHintLabel.AddToClassList("ps-shop-dev-hint");
  shopDevHintLabel.style.display=shopPreviewMode?DisplayStyle.Flex:DisplayStyle.None;
  header.Add(shopDevHintLabel);
  contentHost.Add(header);

  var body=Container("ps-shop-body");

  var listCol=Container("ps-shop-col-list");
  shopFilterHost=Container("ps-shop-filter-host");
  listCol.Add(shopFilterHost);
  shopProductScroll=new ScrollView(ScrollViewMode.Vertical);
  shopProductScroll.AddToClassList("ps-shop-product-scroll");
  shopProductScroll.verticalScrollerVisibility=ScrollerVisibility.Auto;
  shopProductScroll.horizontalScrollerVisibility=ScrollerVisibility.Hidden;
  shopProductScroll.scrollOffset=new Vector2(0,shopProductScrollY);
  shopProductGrid=Container("ps-shop-product-grid");
  shopProductScroll.Add(shopProductGrid);
  listCol.Add(shopProductScroll);
  body.Add(listCol);

  var detailCol=Container("ps-shop-col-detail");
  shopDetailHost=Container("ps-shop-detail-host");
  shopDetailScroll=new ScrollView(ScrollViewMode.Vertical);
  shopDetailScroll.AddToClassList("ps-shop-detail-scroll");
  shopDetailScroll.verticalScrollerVisibility=ScrollerVisibility.Auto;
  shopDetailHost.Add(shopDetailScroll);
  detailCol.Add(shopDetailHost);
  body.Add(detailCol);

  var merchantCol=Container("ps-shop-col-merchant");
  BuildShopMerchantSceneShell(merchantCol);
  body.Add(merchantCol);

  contentHost.Add(body);
  shopShell.Add(contentHost);
  screenRoot.Add(shopShell);

  ApplyShopMerchantPresentation();
  RefreshShopFilters();
  RefreshShopProductGrid(true);
  RefreshShopDetail();
  RefreshShopPurchaseFooter();
  SetShopDialogue(shopMerchant.idleLine);
 }

 void BuildShopMerchantSceneShell(VisualElement merchantCol){
  shopMerchantScene=Container("ps-shop-merchant-scene");
  merchantCol.Add(shopMerchantScene);

  shopMerchantBackdropLayer=Container("ps-shop-merchant-backdrop-layer");
  shopMerchantBackdropLayer.pickingMode=PickingMode.Ignore;
  shopMerchantScene.Add(shopMerchantBackdropLayer);

  shopMerchantCharacterViewport=Container("ps-shop-merchant-character-viewport");
  shopMerchantCharacterViewport.pickingMode=PickingMode.Ignore;
  shopMerchantCharacterImage=new Image{
   scaleMode=ScaleMode.ScaleToFit,
   pickingMode=PickingMode.Ignore
  };
  shopMerchantCharacterImage.AddToClassList("ps-shop-merchant-character-image");
  shopMerchantCharacterViewport.Add(shopMerchantCharacterImage);
  shopMerchantScene.Add(shopMerchantCharacterViewport);

  shopMerchantDialogueLayer=Container("ps-shop-merchant-dialogue-layer");
  shopMerchantDialogueLayer.pickingMode=PickingMode.Ignore;
  shopMerchantDialogue=new Label(){pickingMode=PickingMode.Ignore};
  shopMerchantDialogue.AddToClassList("ps-shop-merchant-dialogue");
  shopMerchantDialogueLayer.Add(shopMerchantDialogue);
  shopMerchantScene.Add(shopMerchantDialogueLayer);

  shopMerchantCounterLayer=Container("ps-shop-merchant-counter-layer");
  shopMerchantCounterLayer.pickingMode=PickingMode.Ignore;
  var counterFace=Container("ps-shop-merchant-counter-face");
  counterFace.pickingMode=PickingMode.Ignore;
  var counterTop=Container("ps-shop-merchant-counter-top");
  counterTop.pickingMode=PickingMode.Ignore;
  counterFace.Add(counterTop);
  var counterBody=Container("ps-shop-merchant-counter-body");
  counterBody.pickingMode=PickingMode.Ignore;
  counterFace.Add(counterBody);
  shopMerchantCounterLayer.Add(counterFace);
  shopMerchantScene.Add(shopMerchantCounterLayer);

  shopMerchantTransactionLayer=Container("ps-shop-merchant-transaction-layer");
  shopSelectedNameLabel=new Label(){pickingMode=PickingMode.Ignore};
  shopSelectedNameLabel.AddToClassList("ps-shop-tx-name");
  shopMerchantTransactionLayer.Add(shopSelectedNameLabel);
  shopGoldLabel=new Label(){pickingMode=PickingMode.Ignore};
  shopGoldLabel.AddToClassList("ps-shop-tx-gold");
  shopMerchantTransactionLayer.Add(shopGoldLabel);
  shopTotalLabel=new Label(){pickingMode=PickingMode.Ignore};
  shopTotalLabel.AddToClassList("ps-shop-tx-total");
  shopMerchantTransactionLayer.Add(shopTotalLabel);
  shopPurchaseReason=new Label(){pickingMode=PickingMode.Ignore};
  shopPurchaseReason.AddToClassList("ps-shop-tx-reason");
  shopMerchantTransactionLayer.Add(shopPurchaseReason);
  var actions=Container("ps-shop-tx-actions");
  shopBuyButton=PackspireUiFactory.Button("購入する",TryShopPurchase);
  shopBuyButton.AddToClassList("ps-primary-action");
  shopBuyButton.AddToClassList("ps-chrome-action");
  shopBuyButton.AddToClassList("ps-shop-buy-btn");
  actions.Add(shopBuyButton);
  shopLeaveButton=PackspireUiFactory.Button("地図へ戻る",()=>{
   if(shopPreviewMode)CloseShopPreview();
   else game.UiReturnToMap();
  });
  shopLeaveButton.AddToClassList("ps-chrome-action");
  shopLeaveButton.AddToClassList("ps-shop-leave-btn");
  actions.Add(shopLeaveButton);
  shopMerchantTransactionLayer.Add(actions);
  shopFutureMerchantActionLayer=Container("ps-shop-future-merchant-actions");
  shopFutureMerchantActionLayer.pickingMode=PickingMode.Ignore;
  shopMerchantTransactionLayer.Add(shopFutureMerchantActionLayer);
  shopMerchantScene.Add(shopMerchantTransactionLayer);
 }

 void ApplyShopMerchantPresentation(){
  if(shopMerchant==null||shopMerchantBackdropLayer==null)return;
  shopMerchantBackdropLayer.Clear();
  var bgPath=string.IsNullOrEmpty(shopMerchant.backdropResource)?"Art/UI/PopDark/hub-bg-v1":shopMerchant.backdropResource;
  var bg=Resources.Load<Texture2D>(bgPath)??HubBackgroundArt();
  if(bg!=null)
   shopMerchantBackdropLayer.Add(Image(bg,new Rect(0,0,1,1),"ps-shop-merchant-bg",ScaleMode.ScaleAndCrop));
  else {
   var fallback=Container("ps-shop-merchant-bg-fallback");
   fallback.pickingMode=PickingMode.Ignore;
   shopMerchantBackdropLayer.Add(fallback);
  }

  if(shopMerchantCounterLayer!=null){
   var artHost=shopMerchantCounterLayer.Q(className:"ps-shop-merchant-counter-art");
   artHost?.RemoveFromHierarchy();
   if(!string.IsNullOrEmpty(shopMerchant.counterResource)){
    var counterTex=Resources.Load<Texture2D>(shopMerchant.counterResource);
    if(counterTex!=null){
     var art=Image(counterTex,new Rect(0,0,1,1),"ps-shop-merchant-counter-art",ScaleMode.ScaleAndCrop);
     shopMerchantCounterLayer.Insert(0,art);
    }
   }
  }

  shopMerchantCharacterTex=ResolveShopMerchantCharacter();
  if(shopMerchantCharacterImage!=null){
   shopMerchantCharacterImage.image=shopMerchantCharacterTex;
   shopMerchantCharacterImage.sprite=null;
   shopMerchantCharacterImage.scaleMode=ScaleMode.ScaleToFit;
   shopMerchantCharacterImage.style.display=shopMerchantCharacterTex!=null?DisplayStyle.Flex:DisplayStyle.None;
  }
  LayoutShopMerchantCharacter();
  if(shopLeaveButton!=null)
   shopLeaveButton.text=shopPreviewMode?"プレビューを閉じる":"地図へ戻る";
  if(shopDevHintLabel!=null)
   shopDevHintLabel.style.display=shopPreviewMode?DisplayStyle.Flex:DisplayStyle.None;
 }

 void LayoutShopMerchantCharacter(){
  if(shopMerchantCharacterViewport==null||shopMerchant==null)return;
  float vh=Mathf.Clamp(shopMerchant.characterViewportHeight,0.55f,0.78f);
  shopMerchantCharacterViewport.style.top=Length.Percent(2f);
  shopMerchantCharacterViewport.style.height=Length.Percent(vh*100f);
  if(shopMerchantCharacterImage==null)return;
  float scale=Mathf.Max(1f,shopMerchant.characterScale);
  shopMerchantCharacterImage.style.width=Length.Percent(100f*scale);
  shopMerchantCharacterImage.style.height=Length.Percent(100f*scale);
  shopMerchantCharacterImage.style.left=Length.Percent(50f+shopMerchant.characterOffsetX-50f*scale);
  shopMerchantCharacterImage.style.top=Length.Percent(shopMerchant.characterOffsetY-(scale-1f)*35f);
 }

 Texture2D ResolveShopMerchantCharacter(){
  if(shopMerchant!=null&&!string.IsNullOrEmpty(shopMerchant.characterResource)){
   var named=Resources.Load<Texture2D>(shopMerchant.characterResource);
   if(named!=null)return named;
  }
  var pop=PopDarkPortraitArt(game.UiSelectedCharacter);
  if(pop!=null&&pop!=game.UiCharacterArt)return pop;
  return HubShowcasePortraitArt();
 }

 void EnsureShopSelection(){
  var filtered=FilteredShopStock().ToArray();
  if(filtered.Length==0){
   selectedShopId="";
   return;
  }
  if(string.IsNullOrEmpty(selectedShopId)||!filtered.Contains(selectedShopId))
   selectedShopId=filtered[0];
 }

 void RefreshShopFilters(){
  if(shopFilterHost==null)return;
  shopFilterHost.Clear();
  string[] labels={"すべて","武器","防具","ルーン","消耗"};
  for(int i=0;i<labels.Length;i++){
   int index=i;
   var button=PackspireUiFactory.Button(labels[i],()=>{
    if(shopCategoryFilter==index)return;
    shopCategoryFilter=index;
    SaveShopProductScroll();
    EnsureShopSelection();
    RefreshShopFilters();
    RefreshShopProductGrid(true);
    RefreshShopDetail();
    RefreshShopPurchaseFooter();
   });
   button.AddToClassList("ps-shop-filter");
   if(i==shopCategoryFilter)button.AddToClassList("ps-selected");
   shopFilterHost.Add(button);
  }
 }

 void SaveShopProductScroll(){
  if(shopProductScroll!=null)shopProductScrollY=shopProductScroll.scrollOffset.y;
 }

 void RestoreShopProductScroll(){
  if(shopProductScroll!=null)shopProductScroll.scrollOffset=new Vector2(0,shopProductScrollY);
 }

 void RefreshShopProductGrid(bool restoreScroll){
  if(shopProductGrid==null)return;
  if(restoreScroll)SaveShopProductScroll();
  shopProductGrid.Clear();
  var stock=FilteredShopStock().ToArray();
  if(stock.Length==0){
   shopProductGrid.Add(PackspireUiFactory.EmptyState("品なし","この分類の商品はありません。"));
   if(restoreScroll)RestoreShopProductScroll();
   return;
  }
  foreach(var id in stock){
   if(!GameCatalog.Items.TryGetValue(id,out var item))continue;
   var productId=id;
   int price=ShopItemPrice(item);
   var card=new Button(()=>SelectShopProduct(productId)){userData=productId,tooltip=item.name};
   card.AddToClassList("ps-shop-product-card");
   if(productId==selectedShopId)card.AddToClassList("ps-selected");
   var art=Container("ps-shop-product-art");
   art.pickingMode=PickingMode.Ignore;
   art.Add(Atlas(game.UiEquipmentArt,ItemUv(productId),"ps-shop-product-image"));
   card.Add(art);
   var priceLabel=new Label($"{price}G"){pickingMode=PickingMode.Ignore};
   priceLabel.AddToClassList("ps-shop-product-price");
   card.Add(priceLabel);
   shopProductGrid.Add(card);
  }
  if(restoreScroll)RestoreShopProductScroll();
 }

 void SelectShopProduct(string productId){
  if(selectedShopId==productId)return;
  selectedShopId=productId;
  UpdateShopProductSelection();
  RefreshShopDetail();
  RefreshShopPurchaseFooter();
  SetShopDialogue(shopMerchant.idleLine);
 }

 void UpdateShopProductSelection(){
  if(shopProductGrid==null)return;
  foreach(var child in shopProductGrid.Children()){
   if(child is not Button card||card.userData is not string id)continue;
   card.EnableInClassList("ps-selected",id==selectedShopId);
  }
 }

 void RefreshShopDetail(){
  if(shopDetailScroll==null)return;
#if UNITY_EDITOR
  shopDetailRefreshCount++;
#endif
  shopDetailScroll.Clear();
  if(string.IsNullOrEmpty(selectedShopId)||!GameCatalog.Items.TryGetValue(selectedShopId,out var item)){
   shopDetailScroll.Add(PackspireUiFactory.EmptyState("商品を選択","左の一覧から品物を選んでください。"));
   return;
  }
  var artFrame=Container("ps-shop-detail-art");
  artFrame.pickingMode=PickingMode.Ignore;
  artFrame.Add(Atlas(game.UiEquipmentArt,ItemUv(selectedShopId),"ps-shop-detail-image"));
  shopDetailScroll.Add(artFrame);
  shopDetailScroll.Add(PackspireUiFactory.Title(item.name));
  shopDetailScroll.Add(ShopDetailBlock("価格",$"{ShopItemPrice(item)}G"));
  shopDetailScroll.Add(ShopDetailBlock("種類",ItemTypeLabel(item.type)));
  if(item.cells!=null&&item.cells.Length>0){
   shopDetailScroll.Add(ShopDetailBlock("形状",$"{item.cells.Length}マス"));
   shopDetailScroll.Add(ShopDetailBlock("属性",string.Join("・",item.cells.Select(x=>ElementLabel(x.element)).Distinct())));
  }
  if(!string.IsNullOrEmpty(item.description))
   shopDetailScroll.Add(ShopDetailBlock("性能",item.description));
  if(!string.IsNullOrEmpty(item.linkRule))
   shopDetailScroll.Add(ShopDetailBlock("LINK効果",item.linkRule));
 }

 VisualElement ShopDetailBlock(string title,string body){
  var block=Container("ps-shop-detail-block");
  var head=new Label(title){pickingMode=PickingMode.Ignore};
  head.AddToClassList("ps-shop-detail-label");
  block.Add(head);
  var text=new Label(body){pickingMode=PickingMode.Ignore};
  text.AddToClassList("ps-shop-detail-body");
  block.Add(text);
  return block;
 }

 int CurrentShopGold()=>shopPreviewMode?48:(game.UiRun?.gold??0);

 void RefreshShopPurchaseFooter(){
  if(shopGoldLabel==null)return;
  int gold=CurrentShopGold();
  shopGoldLabel.text=$"所持金　{gold}G";
  if(string.IsNullOrEmpty(selectedShopId)||!GameCatalog.Items.TryGetValue(selectedShopId,out var item)){
   if(shopSelectedNameLabel!=null)shopSelectedNameLabel.text="選択商品　—";
   shopTotalLabel.text="購入合計　—";
   shopPurchaseReason.text="商品を選択してください";
   shopBuyButton?.SetEnabled(false);
   return;
  }
  int price=ShopItemPrice(item);
  if(shopSelectedNameLabel!=null)shopSelectedNameLabel.text=$"選択　{item.name}";
  shopTotalLabel.text=$"購入合計　{price}G";
  if(shopPreviewMode){
   shopPurchaseReason.text="DEVプレビューでは購入できません";
   shopBuyButton.SetEnabled(false);
   shopBuyButton.text="購入する";
   return;
  }
  if(gold<price){
   shopPurchaseReason.text="所持金が足りません";
   shopBuyButton.SetEnabled(false);
   shopBuyButton.text="所持金が足りません";
  } else {
   shopPurchaseReason.text="";
   shopBuyButton.SetEnabled(true);
   shopBuyButton.text=$"{price}Gで購入する";
  }
 }

 void TryShopPurchase(){
  if(shopPreviewMode||string.IsNullOrEmpty(selectedShopId))return;
  if(!GameCatalog.Items.TryGetValue(selectedShopId,out var item))return;
  SaveShopProductScroll();
  if(!game.UiBuy(selectedShopId)){
   RefreshShopPurchaseFooter();
   SetShopDialogue(shopMerchant.soldOutLine);
   return;
  }
  ShowToast(item.name+"を購入しました");
  SetShopDialogue(shopMerchant.purchaseLine);
  RefreshShopProductGrid(true);
  RefreshShopDetail();
  RefreshShopPurchaseFooter();
 }

 void SetShopDialogue(string line){
  if(shopMerchantDialogue==null)return;
  shopMerchantDialogue.text=string.IsNullOrEmpty(line)?shopMerchant.idleLine:line;
 }

 void CloseShopPreview(){
  shopPreviewMode=false;
  DevNavigate(ScreenId.Hub);
 }

 void OpenShopPreviewFromDev(){
  DevNavigate(ScreenId.Shop,()=>shopPreviewMode=true);
 }
}
}
