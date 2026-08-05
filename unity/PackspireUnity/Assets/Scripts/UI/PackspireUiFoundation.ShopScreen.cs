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
 Label shopHeaderGoldLabel;
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

 string[] ShopStockIds()=>GameCatalog.Items.Keys.Take(6).ToArray();

 IEnumerable<string> FilteredShopStock(){
  var stock=ShopStockIds();
  if(shopCategoryFilter<=0)return stock;
  return stock.Where(id=>{
   if(!GameCatalog.Items.TryGetValue(id,out var item))return false;
   return shopCategoryFilter switch{
   1=>item.type==ItemType.Weapon,
   2=>item.type==ItemType.Armor,
    3=>item.type==ItemType.Rune||item.type==ItemType.Supply,
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

  shopShell=CloneView("UI/PackspireShopView","ps-shop-screen ps-shop-v3 ps-dark-surface");
  if(shopShell==null){
   Debug.LogError("Shop view could not be created.");
   return;
  }
  RequireViewElement<VisualElement>(shopShell,"shop-background");

  RequireViewElement<VisualElement>(shopShell,"shop-header");
  shopHeaderGoldLabel=RequireViewElement<Label>(shopShell,"shop-header-gold");
  shopDevHintLabel=RequireViewElement<Label>(shopShell,"shop-dev-hint");
  shopDevHintLabel.style.display=shopPreviewMode?DisplayStyle.Flex:DisplayStyle.None;

  var merchantCol=RequireViewElement<VisualElement>(shopShell,"shop-merchant-column");
  BuildShopMerchantSceneShell(merchantCol);

  RequireViewElement<VisualElement>(shopShell,"shop-list-column");
  shopFilterHost=RequireViewElement<VisualElement>(shopShell,"shop-filter-host");
  shopProductScroll=RequireViewElement<ScrollView>(shopShell,"shop-product-scroll");
  shopProductScroll.verticalScrollerVisibility=ScrollerVisibility.Auto;
  shopProductScroll.horizontalScrollerVisibility=ScrollerVisibility.Hidden;
  shopProductScroll.scrollOffset=new Vector2(0,shopProductScrollY);
  shopProductGrid=RequireViewElement<VisualElement>(shopShell,"shop-product-grid");

  var detailCol=RequireViewElement<VisualElement>(shopShell,"shop-detail-column");
  shopDetailHost=RequireViewElement<VisualElement>(shopShell,"shop-detail-host");
  shopDetailScroll=RequireViewElement<ScrollView>(shopShell,"shop-detail-scroll");
  shopDetailScroll.verticalScrollerVisibility=ScrollerVisibility.Auto;
  var transactionCol=RequireViewElement<VisualElement>(shopShell,"shop-transaction-column");
  shopMerchantTransactionLayer.RemoveFromHierarchy();
  transactionCol.Add(shopMerchantTransactionLayer);
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
  shopMerchantDialogueLayer.Add(PackspireUiFactory.ManagementV6Art(
   PackspireUiFactory.ManagementV6Piece.ShopSpeech,
   "ps-management-v6-bg ps-shop-dialogue-plaque"
  ));
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
  var bg=PackspireResources.Load<Texture2D>(bgPath);
  if(bg==null)bg=HubBackgroundArt();
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
    var counterTex=PackspireResources.Load<Texture2D>(shopMerchant.counterResource);
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
   PackspireUiFactory.SetActionLabel(shopLeaveButton,shopPreviewMode?"プレビューを閉じる":"地図へ戻る");
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
   var named=PackspireResources.Load<Texture2D>(shopMerchant.characterResource);
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
  string[] labels={"おすすめ","武器","防具","道具"};
  var icons=new[]{
   PackspireUiFactory.ManagementChrome.AllItems,
   PackspireUiFactory.ManagementChrome.WeaponCategory,
   PackspireUiFactory.ManagementChrome.ArmorCategory,
   PackspireUiFactory.ManagementChrome.SupplyCategory
  };
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
   button.Add(PackspireUiFactory.ManagementV6Art(
    PackspireUiFactory.ManagementV6Piece.ShopCategory,
    "ps-management-v6-bg ps-shop-filter-plate ps-shop-filter-plate-normal"
   ));
   button.Add(PackspireUiFactory.ManagementV6Art(
    PackspireUiFactory.ManagementV6Piece.ShopCategorySelected,
    "ps-management-v6-bg ps-shop-filter-plate ps-shop-filter-plate-selected"
   ));
   button.Add(PackspireUiFactory.ManagementArt(icons[i],"ps-shop-filter-icon ps-management-filter-medallion"));
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
   card.Add(PackspireUiFactory.ManagementV6Art(
    PackspireUiFactory.ManagementV6Piece.ShopProduct,
    "ps-management-v6-bg ps-shop-product-plate ps-shop-product-plate-normal"
   ));
   card.Add(PackspireUiFactory.ManagementV6Art(
    PackspireUiFactory.ManagementV6Piece.ShopProductSelected,
    "ps-management-v6-bg ps-shop-product-plate ps-shop-product-plate-selected"
   ));
   var art=Container("ps-shop-product-art");
   art.pickingMode=PickingMode.Ignore;
   art.Add(Atlas(game.UiEquipmentArt,ItemUv(productId),"ps-shop-product-image"));
   card.Add(art);
   var copy=Container("ps-shop-product-copy");
   copy.pickingMode=PickingMode.Ignore;
   var nameLabel=new Label(item.name){pickingMode=PickingMode.Ignore};
   nameLabel.AddToClassList("ps-shop-product-name");
   copy.Add(nameLabel);
   var rankLabel=new Label(ItemTypeLabel(item.type)){pickingMode=PickingMode.Ignore};
   rankLabel.AddToClassList("ps-shop-product-rank");
   copy.Add(rankLabel);
   var priceLabel=new Label($"{price}G"){pickingMode=PickingMode.Ignore};
   priceLabel.AddToClassList("ps-shop-product-price");
   copy.Add(priceLabel);
   card.Add(copy);
   card.Add(PackspireUiFactory.ManagementArt(
    PackspireUiFactory.ManagementChrome.SelectionCorners,
    "ps-management-selection-corners"
   ));
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
  shopDetailScroll.Add(PackspireUiFactory.Title(item.name));
  shopDetailScroll.Add(ShopDetailBlock("ランク / 価格",$"STANDARD　{ShopItemPrice(item):N0}G"));
  var artFrame=Container("ps-shop-detail-art");
  artFrame.pickingMode=PickingMode.Ignore;
  artFrame.Add(VaultItemDisplayArt(selectedShopId,"ps-shop-detail-image"));
  shopDetailScroll.Add(artFrame);
  shopDetailScroll.Add(ShopDetailBlock("種類",ItemTypeLabel(item.type)));
  if(item.cells!=null&&item.cells.Length>0){
   shopDetailScroll.Add(ShopDetailBlock("形状",$"{item.cells.Length}マス"));
   shopDetailScroll.Add(ShopDetailBlock("属性",string.Join("・",item.cells.Select(x=>ElementLabel(x.element)).Distinct())));
  }
  if(!string.IsNullOrEmpty(item.description))
   shopDetailScroll.Add(ShopDetailBlock("性能",item.description));
  if(!string.IsNullOrEmpty(item.linkRule))
   shopDetailScroll.Add(ShopDetailBlock("LINK効果",item.linkRule));
  var cardPair=BuildEquipmentCardPairPreview(new ItemInstance(item.id),item,game.UiRun);
  if(cardPair!=null)shopDetailScroll.Add(cardPair);
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
  if(shopHeaderGoldLabel!=null)shopHeaderGoldLabel.text=$"{gold:N0} G";
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
   PackspireUiFactory.SetActionLabel(shopBuyButton,"購入する");
   return;
  }
  if(gold<price){
   shopPurchaseReason.text="所持金が足りません";
   shopBuyButton.SetEnabled(false);
   PackspireUiFactory.SetActionLabel(shopBuyButton,"所持金が足りません");
  } else {
   shopPurchaseReason.text="";
   shopBuyButton.SetEnabled(true);
   PackspireUiFactory.SetActionLabel(shopBuyButton,$"{price}Gで購入する");
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
