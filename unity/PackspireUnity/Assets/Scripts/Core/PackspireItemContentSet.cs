using System;
using UnityEngine;

namespace Packspire {
[CreateAssetMenu(fileName="ItemContent",menuName="PACKSPIRE/Content/Item Content")]
public sealed class PackspireItemContentSet : ScriptableObject {
 public Element[] board=Array.Empty<Element>();
 public ItemContent[] items=Array.Empty<ItemContent>();
 public BackpackContent[] backpacks=Array.Empty<BackpackContent>();
 public StorageCoreContent[] storageCores=Array.Empty<StorageCoreContent>();
 public ConduitContent[] conduits=Array.Empty<ConduitContent>();
 public ResonanceContent[] resonances=Array.Empty<ResonanceContent>();
 public StabilityContent[] stabilities=Array.Empty<StabilityContent>();
 public ColorTraitContent[] colorTraits=Array.Empty<ColorTraitContent>();
}
}
