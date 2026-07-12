using UnityEngine;
namespace Packspire { public static class RuntimeBootstrap { [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void Start(){if(Object.FindFirstObjectByType<PackspireGame>()==null)new GameObject("PackspireGame").AddComponent<PackspireGame>();} } }
