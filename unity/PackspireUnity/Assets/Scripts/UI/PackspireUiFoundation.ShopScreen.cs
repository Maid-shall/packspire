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
 Image shopMerchantCharacterImage;
 Label shopMerchantDialogue;
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

  shopShell=CloneView("UI/PackspireShopView","ps-shop-screen");
  if(shopShell==null){
   Debug.LogError("Shop view could not be created.");
   return;
  }
  RequireViewElement<VisualElement>(shopShell,"shop-background");

  RequireViewElement<VisualElement>(shopShell,"shop-header");
  shopHeaderGoldLabel=RequireViewElement<Label>(shopShell,"shop-header-gold");
  shopDevHintLabel=RequireViewElement<Label>(shopShell,"shop-dev-hint");
  shopShell.EnableInClassList("ps-shop-live",!shopPreviewMode);

  BindShopMerchantView();

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
  RequireViewElement<VisualElement>(shopShell,"shop-transaction-column");
  screenRoot.Add(shopShell);

  ApplyShopMerchantPresentation();
  RefreshShopFilters();
  RefreshShopProductGrid(true);
  RefreshShopDetail();
  RefreshShopPurchaseFooter();
  SetShopDialogue(shopMerchant.idleLine);
 }

 void BindShopMerchantView(){
  shopMerchantScene=RequireViewElement<VisualElement>(shopShell,"shop-merchant-scene");
  shopMerchantBackdropLayer=RequireViewElement<VisualElement>(shopShell,"shop-merchant-backdrop");
  shopMerchantCharacterImage=RequireViewElement<Image>(shopShell,"shop-merchant-character");
  shopMerchantDialogue=RequireViewElement<Label>(shopShell,"shop-merchant-dialogue");
  shopSelectedNameLabel=RequireViewElement<Label>(shopShell,"shop-selected-name");
  shopGoldLabel=RequireViewElement<Label>(shopShell,"shop-gold");
  shopTotalLabel=RequireViewElement<Label>(shopShell,"shop-total");
  shopPurchaseReason=RequireViewElement<Label>(shopShell,"shop-purchase-reason");
  shopBuyButton=RequireViewElement<Button>(shopShell,"shop-buy");
  shopBuyButton.clicked+=TryShopPurchase;
  shopLeaveButton=RequireViewElement<Button>(shopShell,"shop-leave");
  shopLeaveButton.clicked+=()=>{
   if(shopPreviewMode)CloseShopPreview();
   else game.UiReturnToMap();
  };
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

  shopMerchantCharacterTex=ResolveShopMerchantCharacter();
  if(shopMerchantCharacterImage!=null){
   shopMerchantCharacterImage.image=shopMerchantCharacterTex;
   shopMerchantCharacterImage.sprite=null;
   shopMerchantCharacterImage.scaleMode=ScaleMode.ScaleToFit;
   shopMerchantCharacterImage.EnableInClassList("ps-missing-art",shopMerchantCharacterTex==null);
  }
  if(shopLeaveButton!=null)
   PackspireUiFactory.SetActionLabel(shopLeaveButton,shopPreviewMode?"プレビューを閉じる":"地図へ戻る");
  if(shopDevHintLabel!=null)
   shopShell.EnableInClassList("ps-shop-live",!shopPreviewMode);
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
  for(int i=0;i<labels.Length;i++){
   int index=i;
   var button=new Button(()=>{
    if(shopCategoryFilter==index)return;
    shopCategoryFilter=index;
    SaveShopProductScroll();
    EnsureShopSelection();
    RefreshShopFilters();
    RefreshShopProductGrid(true);
    RefreshShopDetail();
    RefreshShopPurchaseFooter();
   }){text=labels[i]};
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
  var title=new Label(item.name){pickingMode=PickingMode.Ignore};
  title.AddToClassList("ps-shop-detail-title");
  shopDetailScroll.Add(title);
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
  foreach(var cards in StorageFormulaSystem.ResolveCardIds(item,null,null)
   .Where(id=>!string.IsNullOrEmpty(id)).GroupBy(id=>id)){
   if(!GameCatalog.Cards.TryGetValue(cards.Key,out var card))continue;
   shopDetailScroll.Add(ShopDetailBlock(
    $"戦闘カード・基本値 / {cards.Count()}枚",
    $"{card.name}　{card.cost}エナジー\n{card.text}"
   ));
  }
  shopDetailScroll.Add(ShopDetailBlock(
   $"遠征印 / {DeliverySealSystem.Name(item.sealAttribute)}",
   DeliverySealSystem.Effect(item.sealAttribute)
  ));
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
