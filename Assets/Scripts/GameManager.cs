﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using EazyEngine.Tools;
using ScriptableObjectArchitecture;
using Sirenix.OdinInspector;
using System.Numerics;
using EasyMobile;
using System;
using static EasyMobile.StoreReview;

namespace Pok
{
    public class InappPendingInfo
    {
        public string id;
        public System.Action<bool> result;
    }
    public class GameManager : PersistentSingleton<GameManager>, EzEventListener<TimeEvent>, EzEventListener<AddCreatureEvent>, EzEventListener<GameDatabaseInventoryEvent>, EzEventListener<RemoveTimeEvent>
    {

        public static Dictionary<string, int> stateReady = new Dictionary<string, int>();

        public TimeCounterInfoCollection timeCollection;
        public StringVariable currentMap;

        public void addFactorSuperIncome(float factor, double time)
        {
            var timing = GameManager.Instance.Database.timeRestore.Find(x => x.id == $"[SuperInCome]{factor}");
            if (timing != null)
            {
                timing.destinyIfHave += time;
                if(time == -1)
                {
                    timing.destinyIfHave = -1;
                }
            }
            else
            {
                TimeCounter.Instance.addTimer(new TimeCounterInfo() { id = $"[SuperInCome]{factor}", destinyIfHave = time });
            }
        }
        public void DiscountCreature(float percent, double time)
        {
            var timing = GameManager.Instance.Database.timeRestore.Find(x => x.id == $"[DiscountCreature]{percent}");
            if (timing != null)
            {
                timing.destinyIfHave += time;
            }
            else
            {
                TimeCounter.Instance.addTimer(new TimeCounterInfo() { id = $"[DiscountCreature]{percent}", destinyIfHave = time });
            }
        }
        public void ReduceTimeEgg(float percent, double time)
        {
            var timing = GameManager.Instance.Database.timeRestore.Find(x => x.id == $"[ReduceTimeEgg]{percent}");
            if (timing != null)
            {
                timing.destinyIfHave += time;
            }
            else
            {
                TimeCounter.Instance.addTimer(new TimeCounterInfo() { id = $"[ReduceTimeEgg]{percent}", destinyIfHave = time });
            }
        }
        public UnityEngine.Vector2 getPercentReduceTimeEgg()
        {

            var timings = GameManager.Instance.Database.timeRestore.FindAll(x => x.id.Contains("[ReduceTimeEgg]"));
            UnityEngine.Vector2 factor = new UnityEngine.Vector2(0, 0);
            foreach (var timing in timings)
            {
                float factorTime = float.Parse(timing.id.Remove(0, ("[ReduceTimeEgg]").Length));
                if (factorTime > factor.x)
                {
                    double timeLefxt = (timing.destinyIfHave - timing.CounterTime).Clamp(timing.destinyIfHave, 0);
                    factor = new UnityEngine.Vector2(factorTime, (float)timeLefxt);
                }
            }
            return factor;
        }
        public UnityEngine.Vector2 getPercentDiscount()
        {

            var timings = GameManager.Instance.Database.timeRestore.FindAll(x => x.id.Contains("[DiscountCreature]"));
            UnityEngine.Vector2 factor = new UnityEngine.Vector2(0, 0);
            foreach (var timing in timings)
            {
                float factorTime = float.Parse(timing.id.Remove(0, ("[DiscountCreature]").Length));
                if (factorTime > factor.x)
                {
                    double timeLefxt = (timing.destinyIfHave - timing.CounterTime).Clamp(timing.destinyIfHave, 0);
                    factor = new UnityEngine.Vector2(factorTime, (float)timeLefxt);
                }
            }
            return factor;
        }
        public UnityEngine.Vector2 getFactorIncome()
        {

            var timings = GameManager.Instance.Database.timeRestore.FindAll(x => x.id.Contains("[SuperInCome]"));
            UnityEngine.Vector2 factor = new UnityEngine.Vector2(1, 0);
            foreach (var timing in timings)
            {
                float factorTime = float.Parse(timing.id.Remove(0, ("[SuperInCome]").Length));
                if (factorTime > factor.x)
                {
                    double timeLefxt = (timing.destinyIfHave - timing.CounterTime).Clamp(timing.destinyIfHave, 0);
                    factor = new UnityEngine.Vector2(factorTime, (float)timeLefxt);
                }
            }
            return factor;
        }
        public static bool readyForThisState(string state)
        {
            if (stateReady.ContainsKey(state))
            {
                return stateReady[state] == 0;
            }
            return false;
        }
        public static void addDirtyState(string state)
        {
            if (!stateReady.ContainsKey(state))
            {
                stateReady.Add(state, 1);
            }
            stateReady[state]++;
        }
        public static void removeDirtyState(string state)
        {
            if (stateReady.ContainsKey(state))
            {
                stateReady[state]--;
            }
        }
        protected GameDatabaseInstanced _database;
        public UnityEngine.Vector2 resolution = new UnityEngine.Vector2(1080, 1920);
        public GameDatabaseInstanced _defaultDatabase;
        public BaseItemGameInstanced[] itemADDIfNotExist;
        public GameDatabaseInstanced Database
        {
            get
            {
                return _database == null ? _database = LoadDatabaseGame() : _database;
            }
        }
        protected string _zoneChoosed;
        public string ZoneChoosed
        {
            set
            {
                ES3.Save("ZoneChoosed", value);
                _zoneChoosed = value;
            }
            get
            {
                if (string.IsNullOrEmpty(_zoneChoosed))
                {
                    return ES3.Load<string>(key: "ZoneChoosed", defaultValue: GameDatabase.Instance.ZoneCollection[0].ItemID);
                }
                return _zoneChoosed;
            }
        }

        [SerializeField]
        [HideInInspector]
        private int generateID = -1;
        public int GenerateID
        {
            set
            {
                ES3.Save("GenerateID", value);
                generateID = value;
            }
            get
            {
                if (generateID == -1)
                {
                    return ES3.Load<int>(key: "GenerateID", defaultValue: 0);
                }
                return generateID;
            }
        }

        public static UnityEngine.Vector3 ScaleFactor
        {
            get
            {
                return new UnityEngine.Vector3(1, 1, 1);
            }
        }

        protected override void Awake()
        {
            base.Awake();
            if (!GameDatabase.Instance.isInit)
            {
                GameDatabase.Instance.onInit();
            }

            _database = LoadDatabaseGame();
            for(int i =0; i < itemADDIfNotExist.Length; ++i)
            {
                var item = _database.inventory.Find(x => x.itemID == itemADDIfNotExist[i].itemID);
                if (item == null)
                {
                    _database.inventory.Add(ES3.Deserialize<BaseItemGameInstanced>(ES3.Serialize<BaseItemGameInstanced>(itemADDIfNotExist[i])));
                }
            }
       
            _database.InitDatabase();
            _zoneChoosed = ZoneChoosed;
            if (!InAppPurchasing.IsInitialized())
            {
                InAppPurchasing.InitializePurchasing();
                InAppPurchasing.PurchaseCompleted += PurchaseComplete;
            }
        }
        public List<InappPendingInfo> inappPending = new List<InappPendingInfo>();
        public void PurchaseComplete(IAPProduct product)
        {
            var inapps = inappPending.FindAll(x => x.id == product.Id);
            foreach (var inapp in inapps)
            {
                inapp.result?.Invoke(true);
                inappPending.Remove(inapp);
            }
        }
        public CreatureItem[] items;
        [ContextMenu("hack")]
        public void hack()
        {
            for(int i = 0; i <items.Length -2; ++i)
            {
                var list = new List<CreatureItem>();
                items[i].getChild(list, 6);
                foreach(var element in list)
                {
                    GameManager.Instance.Database.creatureInfos.Find(x => x.id == element.ItemID).isUnLock = true;
                }
            }
            GameManager.Instance.Database.creatureInfos.Find(x => x.id == items[items.Length-2].ItemID).isUnLock = true;
            GameManager.Instance.Database.creatureInfos.Find(x => x.id == items[items.Length - 1].ItemID).isUnLock = true;
            // GameManager.Instance.Database.zoneInfos[0].leaderSelected.Add
        }
        public string getTotalGoldGrowthCurrentZone()
        {
            var creatures = GameManager.Instance.Database.getAllCreatureInstanceInZone(GameManager.Instance.ZoneChoosed);
            var total = System.Numerics.BigInteger.Parse("0");
            for (int i = creatures.Count - 1; i >= 0; --i)
            {
                var creature = creatures[i];
                var original = GameDatabase.Instance.CreatureCollection.Find(x => x.ItemID == creature.id);
                if (original == null)
                {
                    creatures.RemoveAt(i);
                    continue;
                }
                total += System.Numerics.BigInteger.Parse(original.getGoldAFK(GameManager.Instance.ZoneChoosed));
            }
            return total.toString();
        }
        public GameDatabaseInstanced LoadDatabaseGame()
        {
            var pDataBase = ES3.Load<GameDatabaseInstanced>("Database", ES3.Deserialize<GameDatabaseInstanced>(ES3.Serialize(_defaultDatabase)));
            return pDataBase;
        }

        public int getLevelItem(string item)
        {
            var originalItem = GameDatabase.Instance.getItemInventory(item);
            if (originalItem.categoryItem == CategoryItem.COMMON)
            {
                var pInfo = Database.getItem(originalItem.ItemID);
                return pInfo.CurrentLevel;
            }
            else if (originalItem.categoryItem == CategoryItem.CREATURE)
            {
                return Database.creatureInfos.Find(x => x.id == item).level;
            }
            return 0;
        }
        public int getNumberBoughtItem(string item)
        {
            var originalItem = GameDatabase.Instance.getItemInventory(item);
            if (originalItem.categoryItem == CategoryItem.COMMON)
            {
                var pInfo = Database.getItem(originalItem.ItemID);
                return pInfo.boughtNumber;
            }
            else if (originalItem.categoryItem == CategoryItem.CREATURE)
            {
                return Database.creatureInfos.Find(x => x.id == item).boughtNumber;
            }
            return 0;
        }
        private void OnEnable()
        {
            EzEventManager.AddListener<TimeEvent>(this);
            EzEventManager.AddListener<AddCreatureEvent>(this);
            EzEventManager.AddListener<GameDatabaseInventoryEvent>(this);
            EzEventManager.AddListener<RemoveTimeEvent>(this);
        }

        private void OnDisable()
        {
            EzEventManager.RemoveListener<TimeEvent>(this);
            EzEventManager.RemoveListener<AddCreatureEvent>(this);
            EzEventManager.RemoveListener<GameDatabaseInventoryEvent>(this);
            EzEventManager.RemoveListener<RemoveTimeEvent>(this);
        }
        private void OnDestroy()
        {
            ES3.Save("Database", Database);
        }
        public void SaveGame()
        {

        }

        public int TimeDelayBonusEvolution
        {
            get
            {
                return UnityEngine.Random.Range(300, 1500);
            }
        }
        public int TimeDelayBoxRewardADS { 
            get
            {
                return UnityEngine.Random.Range(5, 10);
            }
        }

        public void tryShowBoxRewardAds()
        {
            if (TimeCounter.InstanceRaw.IsDestroyed()) return;
            if (TimeCounter.CounterValue < 30) return;
            var time = TimeCounter.Instance.timeCollection.Value.Find(x => x.id.Contains("RewardADS"));
            if (time != null)
            {
                return;
            }
            TimeCounter.Instance.addTimer(new TimeCounterInfo() { id = $"[Block]RewardADS", autoRemoveIfToDestiny = true, destinyIfHave = GameManager.Instance.TimeDelayBoxRewardADS });

            HUDManager.Instance.showBoxRewardADS();
        }
        public void showBoxRate(System.Action<UserAction> callback)
        {
            StoreReview.RequestRating(null, callback);
        }
        public void OnEzEvent(TimeEvent eventType)
        {
            var eventString = eventType.timeInfo.id;
            if (eventString.Contains("[Restore]"))
            {
                eventString = eventString.Remove(0, 9);
                var timeID = eventString.Split('/');
                var itemExist = timeID.Length == 1 ? GameManager.Instance.Database.getItem(eventString) : GameManager.Instance.Database.getCreatureItem(timeID[1], timeID[0]);
                if (itemExist != null && eventType.timeInfo.destinyIfHave != -1 && eventType.timeInfo.counterTime >= eventType.timeInfo.destinyIfHave)
                {
                    Database.checkTimeItem(timeID.Length == 2 ? timeID[1] : eventString);
                }
            }
            if (eventType.timeInfo.id.Contains("SuperInCome"))
            {
                var factor = GameManager.Instance.getFactorIncome();
                if (HUDManager.InstanceRaw)
                HUDManager.Instance.timeXInCome.transform.parent.gameObject.SetActive(factor.y > 0);
                if (factor.y > 0)
                {

                    var timeSpan = TimeSpan.FromSeconds(factor.y);
                    if (HUDManager.InstanceRaw)
                        HUDManager.Instance.timeXInCome.text = string.Format("{0}H {1}M {2}S", timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);
                }
                else if(eventType.timeInfo.destinyIfHave != -1)
                {
                    GameManager.Instance.Database.removeTime(eventType.timeInfo);
                }
            }
            if (HUDManager.InstanceRaw)
                HUDManager.Instance.factorGoldToBuy.text = (getFactorIncome().x < 2) ? "x2" : "x4";
            if (HUDManager.InstanceRaw)
                HUDManager.Instance.factorGoldToBuy.transform.parent.parent.gameObject.SetActive(getFactorIncome().x < 4);
            if (eventType.timeInfo.id.Contains("DiscountCreature"))
            {
                var factor = GameManager.Instance.getPercentDiscount();
                if (HUDManager.InstanceRaw)
                    HUDManager.Instance.timeDisCountCreature.transform.parent.gameObject.SetActive(factor.y > 0);
                if (factor.y > 0)
                {

                    var timeSpan = TimeSpan.FromSeconds(factor.y);
                    HUDManager.Instance.timeDisCountCreature.text = string.Format("{0}H {1}M {2}S", timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);
                }
                else
                {
                    GameManager.Instance.Database.removeTime(eventType.timeInfo);
                }
            }
            if (eventType.timeInfo.id.Contains("ReduceTimeEgg"))
            {
                var factor = GameManager.Instance.getPercentReduceTimeEgg();
                if (HUDManager.InstanceRaw)
                    HUDManager.Instance.timeEggReduce.transform.parent.gameObject.SetActive(factor.y > 0);
                if (factor.y > 0)
                {

                    var timeSpan = TimeSpan.FromSeconds(factor.y);
                    if (HUDManager.InstanceRaw)
                        HUDManager.Instance.timeEggReduce.text = string.Format("{0}H {1}M {2}S", timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);
                }
                else
                {
                    GameManager.Instance.Database.removeTime(eventType.timeInfo);
                }
               
            }
            if (eventType.timeInfo.counterTime >= eventType.timeInfo.destinyIfHave && eventType.timeInfo.autoRemoveIfToDestiny)
            {
                GameManager.Instance.Database.removeTime(eventType.timeInfo);
            }
        }


        public ItemWithQuantity[] claimItem(BaseItemGame item, string quantity = "1",float bonus = 0)
        {
            if (typeof(IUsageItem).IsAssignableFrom(item.GetType()))
            {
                if (((IUsageItem)item).useWhenClaim())
                {
                    if (typeof(IExtractItem).IsAssignableFrom(item.GetType()))
                    {
                        var items = ((IExtractItem)item).ExtractHere();
                        for (int i = 0; i < items.Length; ++i)
                        {
                            var itemExist = GameManager.Instance.Database.getItem(items[i].item.ItemID);
                            items[i].quantity = (BigInteger.Parse(items[i].quantity.clearDot()) * ((int)(1 + bonus) * 100) / 100).ToString();
                            itemExist.addQuantity(items[i].quantity);
                        }
                        return items;
                    }
                    if (typeof(ItemBoosterObject).IsAssignableFrom(item.GetType()))
                    {
                        ((ItemBoosterObject)item).executeBooster();
                    }
                }
            }
            else
            {
                var itemExist = GameManager.Instance.Database.getItem(item.ItemID);
                itemExist.addQuantity(quantity);
            }
            return new ItemWithQuantity[] { new ItemWithQuantity() {item = item,quantity = quantity } };
        }
        public void RequestInappForItem(string id, System.Action<bool> result)
        {
#if UNITY_EDITOR
            result.Invoke(true);
#endif
            if (!inappPending.Exists(x => x.id == id))
            {
                inappPending.Add(new InappPendingInfo() { id = id, result = result });
                InAppPurchasing.PurchaseWithId(id);
            }

        }
        public void OnEzEvent(AddCreatureEvent eventType)
        {
            if (eventType.change < 0)
            {
                Database.checkTimeItem($"Egg{eventType.zoneid}");
            }
        }
        public IEnumerator delayAction(float sec ,System.Action action)
        {
            yield return new WaitForSeconds(sec);
            action?.Invoke();
        }
        public void WatchRewardADS(string id, System.Action<bool> result = null)
        {
            LogEvent("WATCH_ADS:" + id);
#if UNITY_EDITOR
            result.Invoke(true);

#endif
        }
        public void LoadRewardADS(string id, System.Action<bool> result = null)
        {
            LogEvent("WATCH_ADS:" + id);
#if UNITY_EDITOR
            StartCoroutine(delayAction(1, () => {
                result.Invoke(UnityEngine.Random.Range(0,2) == 0);
            }));
#endif
        }
        public bool isRewardADSReady(string id)
        {
#if UNITY_EDITOR
            return UnityEngine.Random.Range(0, 2) == 0;
#endif
            if (string.IsNullOrEmpty(id))
            {
                return Advertising.IsRewardedAdReady();
            }
            else
            {
                return Advertising.IsRewardedAdReady();
            }

        }

        public void LogEvent(string eventString)
        {

        }
        public IEnumerator actionOnEndFrame(System.Action action)
        {
            yield return new WaitForEndOfFrame();
            action?.Invoke();
        }
        public void OnEzEvent(GameDatabaseInventoryEvent eventType)
        {
            if (BigInteger.Parse(eventType.item.changeQuantity) < 0)
            {
                Database.checkTimeItem(eventType.item.itemID);
            }
            if (eventType.item.item.ItemID.Contains("CoinBank"))
            {
                var exist = GameManager.Instance.Database.getItem(eventType.item.item.ItemID);
                var quantity = exist.quantity;
                var moneyAdd = quantity.toInt() * getTotalGoldGrowthCurrentZone().toBigInt() * (int)getFactorIncome().x;
                if (eventType.item.changeQuantity.toDouble() > 0 && eventType.item.changeQuantity.toDouble() < GameDatabase.Instance.timeMinToShowBoxBank)
                {
                    StartCoroutine(actionOnEndFrame(() =>
                    {
                        exist.addQuantity((-quantity.toInt()).ToString());
                        var coin = GameManager.Instance.Database.getItem("Coin");
                        coin.addQuantity(moneyAdd.toString());
                    }));
                   
                }else if(eventType.item.changeQuantity.toDouble() > 0)
                {
                    if (HUDManager.InstanceRaw)
                    {
                        HUDManager.Instance.boxBank.show(moneyAdd.toString());
                    }
                    StartCoroutine(actionOnEndFrame(() =>
                    {
                        exist.addQuantity((-quantity.toInt()).ToString());
                    }));

                }
            }
        }

        public void OnEzEvent(RemoveTimeEvent eventType)
        {
            if (HUDManager.InstanceRaw)
            {
                HUDManager.Instance.factorGoldToBuy.text = (getFactorIncome().x < 2) ? "x2" : "x4";
                HUDManager.Instance.factorGoldToBuy.transform.parent.parent.gameObject.SetActive(getFactorIncome().x < 4);
            }
        }
    }
}
