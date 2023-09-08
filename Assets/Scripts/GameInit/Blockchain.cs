using Chromia;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.Linq;
#if ENABLE_IL2CPP
using Newtonsoft.Json.Utilities;
#endif

using Buffer = Chromia.Buffer;
using System.Runtime.Serialization;
using System;

public class Blockchain : MonoBehaviour
{
    public static Blockchain Instance { get; private set; }
    public static ChromiaClient Client { get; private set; }

    public bool Initialized { get; private set; } = false;

    private static Buffer AccountId;
    private static SignatureProvider Signer;
#if LOCAL_NODE
    private static readonly string NodeUri = "http://localhost:7741";
#else
    private static readonly string NodeUri = "https://coa-demo-postchain.chromia.dev/"; 
#endif


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

#if ENABLE_IL2CPP
            AotTypeEnforce();
#endif

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    async void Start()
    {
        ChromiaClient.SetTransport(new UnityTransport());

        Client = await ChromiaClient.Create(NodeUri, 0);

        var privKey = Buffer.From(PlayerPrefs.GetString("privkey"));
        if (privKey.IsEmpty)
            privKey = KeyPair.GeneratePrivKey();
        PlayerPrefs.SetString("privkey", privKey.Parse());

        Signer = SignatureProvider.Create(privKey);
        await RegisterPlayer();
        Initialized = true;

        var backpack = await GetBackpacksFromShop();
        foreach (var b in backpack)
            Debug.Log($"{b.Key} {b.Value}");
    }

    #region  Operations
    private Operation AuthOp()
    {
        return new Operation("ft4.ft_auth", AccountId, AccountId);
    }

    public async UniTask RegisterPlayer()
    {
        var authDesc = ToAuthDesc(Signer.PubKey);
        AccountId = ChromiaClient.Hash(authDesc);

        var exists = await Client.Query<bool>("IPlayer.does_exist", ("account_id", AccountId));
        if (!exists)
        {
            await Client.SendUniqueTransaction(Transaction.Build()
                .AddOperation(new Operation("IPlayer.register", authDesc))
                .AddSignatureProvider(Signer));
        }
    }

    public async UniTask<TransactionReceipt> StartExpedition(
        List<Hero> heroes,
        List<Consumable> items,
        DangerLevel dangerLevel,
        Rarity backpack
    )
    {
        return await Client.SendUniqueTransaction(Transaction.Build()
                .AddOperation(AuthOp())
                .AddOperation(new Operation(
                    "IExpedition.start",
                    heroes.Select(h => h.Id).ToArray(),
                    items.Select(c => c.ToString()).ToArray(),
                    dangerLevel.ToString(),
                    backpack.ToString()
                ))
                .AddSignatureProvider(Signer));
    }

    public async UniTask<TransactionReceipt> UseConsumable(Buffer expeditionId, Consumable item)
    {
        return await Client.SendUniqueTransaction(Transaction.Build()
                .AddOperation(AuthOp())
                .AddOperation(new Operation("IExpedition.use_consumable", expeditionId, item.ToString()))
                .AddSignatureProvider(Signer));
    }

    public async UniTask<TransactionReceipt> DropConsumable(Buffer expeditionId, Consumable item)
    {
        return await Client.SendUniqueTransaction(Transaction.Build()
                .AddOperation(AuthOp())
                .AddOperation(new Operation("IExpedition.drop_consumable", expeditionId, item.ToString()))
                .AddSignatureProvider(Signer));
    }

    public async UniTask<TransactionReceipt> DropLoot(Buffer expeditionId, string itemName)
    {
        return await Client.SendUniqueTransaction(Transaction.Build()
                .AddOperation(AuthOp())
                .AddOperation(new Operation("IExpedition.drop_loot", expeditionId, itemName))
                .AddSignatureProvider(Signer));
    }

    public async UniTask<TransactionReceipt> SelectExpeditionChallenge(Buffer challengeId)
    {
        return await Client.SendUniqueTransaction(Transaction.Build()
                .AddOperation(AuthOp())
                .AddOperation(new Operation("IExpedition.select_challenge", challengeId))
                .AddSignatureProvider(Signer));
    }

    public async UniTask<TransactionReceipt> AdvanceExpedition(Buffer challengeId, ChallengeAction action)
    {
        return await Client.SendUniqueTransaction(Transaction.Build()
                .AddOperation(AuthOp())
                .AddOperation(new Operation("IExpedition.advance", challengeId, action.ToString()))
                .AddSignatureProvider(Signer));
    }

    public async UniTask<TransactionReceipt> RetreatFromExpedition(Buffer expedition_id)
    {
        return await Client.SendUniqueTransaction(Transaction.Build()
                .AddOperation(AuthOp())
                .AddOperation(new Operation("IExpedition.retreat", expedition_id))
                .AddSignatureProvider(Signer));
    }

    public async UniTask<TransactionReceipt> FinishExpedition(Buffer expedition_id)
    {
        return await Client.SendUniqueTransaction(Transaction.Build()
                .AddOperation(AuthOp())
                .AddOperation(new Operation("IExpedition.finish", expedition_id))
                .AddSignatureProvider(Signer));
    }

    public async UniTask<TransactionReceipt> RefreshShop()
    {
        return await Client.SendUniqueTransaction(Transaction.Build()
                .AddOperation(AuthOp())
                .AddOperation(new Operation("IShop.refresh"))
                .AddSignatureProvider(Signer));
    }

    public async UniTask<TransactionReceipt> BuyHero(Buffer heroId, string name)
    {
        return await Client.SendUniqueTransaction(Transaction.Build()
                .AddOperation(AuthOp())
                .AddOperation(new Operation("IShop.buy_hero", heroId, name))
                .AddSignatureProvider(Signer));
    }

    public async UniTask<TransactionReceipt> BuyConsumable(Consumable item)
    {
        return await Client.SendUniqueTransaction(Transaction.Build()
                .AddOperation(AuthOp())
                .AddOperation(new Operation("IShop.buy_consumable", item.ToString()))
                .AddSignatureProvider(Signer));
    }

    public async UniTask<TransactionReceipt> BuyBackpack(string backpackName)
    {
        return await Client.SendUniqueTransaction(Transaction.Build()
                .AddOperation(AuthOp())
                .AddOperation(new Operation("IShop.buy_backpack", backpackName))
                .AddSignatureProvider(Signer));
    }
    #endregion

    #region Queries

    public async UniTask<List<Hero>> GetHeroes()
    {
        return await Client.Query<List<Hero>>("IPlayer.get_heroes", ("account_id", AccountId));
    }

    public async UniTask<Dictionary<string, int>> GetBackpacks()
    {
        return (await Client.Query<List<(string, int)>>("IPlayer.get_backpacks", ("account_id", AccountId)))
            .ToDictionary(s => s.Item1, s => s.Item2);
    }

    public async UniTask<Dictionary<Consumable, int>> GetConsumables()
    {
        return (await Client.Query<List<(Consumable, int)>>("IPlayer.get_consumables", ("account_id", AccountId)))
            .ToDictionary(s => s.Item1, s => s.Item2);
    }

    public async UniTask<long> GetShards()
    {
        return await Client.Query<long>("IPlayer.get_shards", ("account_id", AccountId));
    }

    public async UniTask<List<ExpeditionOverview>> GetActiveExpeditions()
    {
        return await Client.Query<List<ExpeditionOverview>>("IExpedition.get_active", ("account_id", AccountId));
    }

    public async UniTask<List<ExpeditionOverview>> GetAllExpeditions()
    {
        return await Client.Query<List<ExpeditionOverview>>("IExpedition.get_all", ("account_id", AccountId));
    }

    public async UniTask<List<Expedition>> GetExpeditionDetails(Buffer expeditionId)
    {
        return await Client.Query<List<Expedition>>("IExpedition.get_details", ("expedition_id", expeditionId));
    }

    public async UniTask<ChallengeResult> GetChallengeResult(Buffer challengeId)
    {
        return await Client.Query<ChallengeResult>("IExpedition.get_result", ("challenge_id", challengeId));
    }

    public async UniTask<List<HeroShopEntry>> GetHeroSelectionFromShop()
    {
        return await Client.Query<List<HeroShopEntry>>("IShop.get_hero_selection", ("account_id", AccountId));
    }

    public async UniTask<List<ConsumableShopEntry>> GetConsumablesFromShop()
    {
        return await Client.Query<List<ConsumableShopEntry>>("IShop.get_consumables");
    }

    public async UniTask<Dictionary<string, int>> GetBackpacksFromShop()
    {
        var x = await Client.Query<object>("IShop.get_backpacks");
        Debug.Log(JsonConvert.SerializeObject(x));
        return await Client.Query<Dictionary<string, int>>("IShop.get_backpacks");
    }
    #endregion

    #region Models

    public class Hero
    {
        [JsonProperty("id")]
        public Buffer Id;
        [JsonProperty("name")]
        public string Name;
        [JsonProperty("rarity")]
        public Rarity Rarity;
        [JsonProperty("classs")]
        public Class Class;
        [JsonProperty("species")]
        public Species Species;
        [JsonProperty("health")]
        public int Health;
    }

    public class HeroShopEntry
    {
        [JsonProperty("hero")]
        public Hero Hero;
        [JsonProperty("price")]
        public int Price;
    }

    public class ConsumableShopEntry
    {
        [JsonProperty("consumable")]
        public Consumable Consumable;
        [JsonProperty("price")]
        public int Price;
    }

    public class Expedition
    {
        [JsonProperty("party")]
        public List<PartyMember> Party;
        [JsonProperty("challenges")]
        public List<Challenge> Challenges;
        [JsonProperty("consumables")]
        public List<(Consumable, int)> Consumables;
        [JsonProperty("arrival_at")]
        public long ArrivalAt;
    }

    public class ExpeditionOverview
    {
        [JsonProperty("id")]
        public Buffer Id;
        [JsonProperty("danger_level")]
        public DangerLevel DangerLevel;
        [JsonProperty("created_at")]
        public long CreatedAt;
        [JsonProperty("party")]
        public List<PartyMember> Party;
        [JsonProperty("active_challenge")]
        public ActiveChallengeOverview ActiveChallenge;
        [JsonProperty("active_challenge")]
        public List<ChallengeOverview> NextChallenges;
    }

    public class ActiveChallengeOverview
    {
        [JsonProperty("challenge")]
        public ChallengeOverview Challenge;
        [JsonProperty("arrival_at")]
        public long ArrivalAt;
    }

    public class ChallengeOverview
    {
        [JsonProperty("id")]
        public Buffer Id;
        [JsonProperty("level")]
        public int Level;
        [JsonProperty("type")]
        public ChallengeType Type;
        [JsonProperty("difficulty")]
        public ChallengeDifficulty Difficulty;
        [JsonProperty("terrain")]
        public Terrain Terrain;
        [JsonProperty("class_advantage")]
        public Class ClassAdvantage;
    }

    public class Challenge : ChallengeOverview
    {
        [JsonProperty("states")]
        public List<ChallengeState> States;
        [JsonProperty("outcome")]
        public List<ChallengeOutcome> Outcome;
        [JsonProperty("loot")]
        public List<ChallengeLoot> Loot;
        [JsonProperty("effects")]
        public List<Consumable> Effects;
    }

    public class ChallengeResult
    {
        [JsonProperty("current")]
        public Challenge Current;
        [JsonProperty("next")]
        public List<ChallengeOverview> Next;
    }

    public class ChallengeState
    {
        [JsonProperty("state")]
        public State State;
        [JsonProperty("created_at")]
        public long CreatedAt;
    }

    public class ChallengeOutcome
    {
        [JsonProperty("hero_id")]
        public Buffer HeroId;
        [JsonProperty("damage")]
        public int Damage;
        [JsonProperty("needed")]
        public int NeededRoll;
        [JsonProperty("roll")]
        public int ActualRoll;
        [JsonProperty("success")]
        public bool Success;
    }

    public class ChallengeLoot
    {
        [JsonProperty("type")]
        public Loot Type;
        [JsonProperty("amount")]
        public int Amount;
        [JsonProperty("state")]
        public LootState State;
    }

    public class PartyMember : Hero
    {
        [JsonProperty("max_health")]
        public int MaxHealth;
    }

    #endregion

    #region Enums
    public enum Consumable
    {
        [EnumMember(Value = "heal_single_small")]
        HealthPotionSingleSmall,
        [EnumMember(Value = "heal_single_full")]
        HealthPotionSingleFull,
        [EnumMember(Value = "heal_party_s")]
        HealthPotionPartySmall,
        [EnumMember(Value = "heal_party_m")]
        HealthPotionPartyMedium,
        [EnumMember(Value = "heal_party_f")]
        HealthPotionPartyFull,
        [EnumMember(Value = "protect_s")]
        ProtectSmall,
        [EnumMember(Value = "protect_l")]
        ProtectLarge,
        [EnumMember(Value = "evade_encounter")]
        EvadeCounter,
        [EnumMember(Value = "changeling")]
        Changeling,
        [EnumMember(Value = "hustle")]
        Hustle,

    }

    public enum DangerLevel
    {
        Neglible,
        Harmless,
        Unhealthy,
        Interesting,
        Seriously
    }

    public enum Loot
    {
        Artifact,
        Consumable,
        Shards
    }

    public enum LootState
    {
        Open,
        Claimed,
        Lost,
        Dropped
    }

    public enum State
    {
        Selectable,
        Ignored,
        Traveling,
        Skipped,
        Resolved,
        Retreated,
        Perished
    }

    public enum ChallengeType
    {
        Fight,
        Check
    }

    public enum ChallengeDifficulty
    {
        Normal,
        Hard,
        Boss
    }

    public enum ChallengeAction
    {
        Resolve,
        Skip
    }

    public enum Terrain
    {
        Savannah,
        Forrest,
        Lava,
        Village
    }

    public enum Rarity
    {
        Common,
        Uncommon,
        Rare,
        Magical,
        Legendary
    }

    public enum Class
    {
        Bard,
        Fighter,
        Mage,
        Paladin,
        Ranger,
    }

    public enum Species
    {
        Human,
        Leo,
        Elve,
        Draco,
    }
    #endregion

    private static object ToAuthDesc(Buffer pubKey)
    {
        return new object[]
        {
            0,
            new object[] { new object[] {"A", "T"}, pubKey },
            null
        };
    }

#if ENABLE_IL2CPP
        private void AotTypeEnforce()
        {
            AotHelper.EnsureType<BufferConverter>();
        }
#endif
}