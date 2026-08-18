using System;
using UnityEngine;

namespace Packspire {
[CreateAssetMenu(fileName="ActorContent",menuName="PACKSPIRE/Content/Actor Content")]
public sealed class PackspireActorContentSet : ScriptableObject {
 public ReactionValueContent[] reactionValues=Array.Empty<ReactionValueContent>();
 public RoleContent[] roles=Array.Empty<RoleContent>();
 public EnemyContent[] enemies=Array.Empty<EnemyContent>();
 public FactionContent[] factions=Array.Empty<FactionContent>();
 public CharacterContent[] characters=Array.Empty<CharacterContent>();
}
}
