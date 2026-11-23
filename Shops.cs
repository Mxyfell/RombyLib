using System;
using UnityEngine;

namespace RombyLib
{
    /// <summary>
    /// Controller for shops, merchants, item purchasing, and ammo restocking.
    /// </summary>
    public class ShopsController
    {
        private ShopManager _activeShop;
        private MerchantShopManager _activeMerchantShop;

        internal ShopsController() { }

        private static T GetCached<T>(ref T cached) where T : UnityEngine.Object
        {
            if (!Romby.IsValid(cached))
            {
                try
                {
                    var found = UnityEngine.Object.FindObjectOfType<T>();
                    cached = Romby.IsValid(found) ? found : null;
                }
                catch
                {
                    cached = null;
                }
            }
            return Romby.IsValid(cached) ? cached : null;
        }

        /// <summary>Gets the currently active standard shop manager in the scene.</summary>
        public ShopManager ActiveShop => GetCached(ref _activeShop);

        /// <summary>Gets the currently active traveling merchant shop manager in the scene.</summary>
        public MerchantShopManager ActiveMerchantShop => GetCached(ref _activeMerchantShop);

        /// <summary>Checks whether any shop or merchant panel is currently open.</summary>
        public bool IsAnyShopOpen
        {
            get
            {
                try
                {
                    if (MerchantShopManager.IsAnyPanelOpen) return true;
                    var shop = ActiveShop;
                    return Romby.IsValid(shop) && Romby.IsValid(shop.ammoPanelParent) && shop.ammoPanelParent.activeInHierarchy;
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <summary>Checks whether the active standard shop belongs to the scammer vendor.</summary>
        public bool IsScammerVendor => Romby.IsValid(ActiveShop) && ActiveShop.isScammer;

        /// <summary>Gets available catalog items in the active standard shop.</summary>
        public ShopItem[] AvailableItems => Romby.IsValid(ActiveShop) ? ActiveShop.availableItems : null;

        /// <summary>Gets available ammo items from the active traveling merchant.</summary>
        public MerchantItem[] MerchantAmmoItems => Romby.IsValid(ActiveMerchantShop) ? ActiveMerchantShop.ammoItems : null;

        /// <summary>Gets available totem items from the active traveling merchant.</summary>
        public MerchantItem[] MerchantTotemItems => Romby.IsValid(ActiveMerchantShop) ? ActiveMerchantShop.totemItems : null;

        /// <summary>Gets available special/other items from the active traveling merchant.</summary>
        public MerchantItem[] MerchantOtherItems => Romby.IsValid(ActiveMerchantShop) ? ActiveMerchantShop.otherItems : null;

        /// <summary>Purchases all missing ammunition across weapons in the active shop.</summary>
        public void BuyAllAmmo()
        {
            if (Romby.IsValid(ActiveShop))
            {
                try
                {
                    ActiveShop.BuyAllAmmo();
                    Debug.Log("[RombyLib.Shops] Purchased all ammo from active shop.");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[RombyLib.Shops] Failed to purchase ammo: {ex.Message}");
                }
            }
        }

        /// <summary>Purchases a specific shop item from the active standard shop.</summary>
        public bool PurchaseItem(ShopItem item)
        {
            if (item == null) return false;
            if (Romby.IsValid(ActiveShop))
            {
                try
                {
                    ActiveShop.PurchaseItem(item);
                    Debug.Log($"[RombyLib.Shops] Requested purchase for shop item ID {item.itemId}.");
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[RombyLib.Shops] Failed to purchase item {item.itemId}: {ex.Message}");
                }
            }
            return false;
        }

        /// <summary>Purchases an item from the active traveling merchant.</summary>
        public bool PurchaseMerchantItem(MerchantItem item)
        {
            if (item == null) return false;
            if (Romby.IsValid(ActiveMerchantShop))
            {
                try
                {
                    ActiveMerchantShop.PurchaseMerchantItem(item, null);
                    Debug.Log($"[RombyLib.Shops] Requested purchase for merchant item type {item.itemType}.");
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[RombyLib.Shops] Failed to purchase merchant item: {ex.Message}");
                }
            }
            return false;
        }

        /// <summary>Closes any active shop or merchant panels.</summary>
        public void CloseActiveShops()
        {
            if (Romby.IsValid(ActiveShop))
            {
                try { ActiveShop.CloseShop(); }
                catch { }
            }

            if (Romby.IsValid(ActiveMerchantShop))
            {
                try { ActiveMerchantShop.ClosePanel(); }
                catch { }
            }

            Debug.Log("[RombyLib.Shops] Closed active shop interfaces.");
        }

        /// <summary>Refreshes the active standard shop UI and currency displays.</summary>
        public void RefreshShopUI()
        {
            if (Romby.IsValid(ActiveShop))
            {
                try
                {
                    ActiveShop.FullRefreshShopUI();
                    ActiveShop.RefreshMoneyDisplay();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[RombyLib.Shops] Failed to refresh shop UI: {ex.Message}");
                }
            }
        }
    }
}
