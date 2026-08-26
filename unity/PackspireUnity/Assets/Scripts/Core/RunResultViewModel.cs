using System.Collections.Generic;

namespace Packspire {
public enum RunResultType { Defeat, Return, Clear }

public static class RunResultPresentationSystem {
 public static RunResultType ResolveType(
  bool clearFallback,ExpeditionFinalizationSummary finalization){
  if(finalization==null)return clearFallback?RunResultType.Clear:RunResultType.Defeat;
  return finalization.reason switch {
   ExpeditionEndReason.Return=>RunResultType.Return,
   ExpeditionEndReason.Clear=>RunResultType.Clear,
   _=>RunResultType.Defeat,
  };
 }
}

public sealed class RunResultStat {
 public string label;
 public string value;
 public RunResultStat(string label,string value){this.label=label;this.value=value;}
}

/// <summary>UI adapter model for defeat, voluntary return, or full clear.</summary>
public sealed class RunResultViewModel {
 public RunResultType resultType;
 public string title;
 public string subtitle;
 public string dungeonName;
 public string locationName;
 public string causeText;
 public string messageText;
 public string backgroundHintDungeonId;
 public readonly List<RunResultStat> primaryStats=new();
 public readonly List<RunResultStat> records=new();
 public readonly List<RunResultStat> unlocks=new();
 public readonly List<RunResultStat> heirloomChanges=new();
 public bool preview;
}
}
