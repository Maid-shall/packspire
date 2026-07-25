using System;
using UnityEngine;

namespace Packspire {
[CreateAssetMenu(fileName="CardContent",menuName="PACKSPIRE/Content/Card Content")]
public sealed class PackspireCardContentSet : ScriptableObject {
 public CardContent[] cards=Array.Empty<CardContent>();
 public ExplorationCardContent[] explorationCards=Array.Empty<ExplorationCardContent>();
 public ConsumableContent[] consumables=Array.Empty<ConsumableContent>();
 public StatusContent[] statuses=Array.Empty<StatusContent>();
}
}
