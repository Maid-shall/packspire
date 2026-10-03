using System;
using UnityEngine;

namespace Packspire {
[CreateAssetMenu(fileName="WorldContent",menuName="PACKSPIRE/Content/World Content")]
public sealed class PackspireWorldContentSet : ScriptableObject {
 public DungeonContent[] dungeons=Array.Empty<DungeonContent>();
 public FacilityContent[] facilities=Array.Empty<FacilityContent>();
 public EventContent[] events=Array.Empty<EventContent>();
 public MerchantContent[] merchants=Array.Empty<MerchantContent>();
 public RewardPoolContent[] rewardPools=Array.Empty<RewardPoolContent>();
 public GameBalanceContent balance=new();
}
}
