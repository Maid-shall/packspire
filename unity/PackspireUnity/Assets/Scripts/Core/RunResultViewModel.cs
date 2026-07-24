using System.Collections.Generic;

namespace Packspire {
public enum RunResultType { Defeat, Clear }

public sealed class RunResultStat {
 public string label;
 public string value;
 public RunResultStat(string label,string value){this.label=label;this.value=value;}
}

/// <summary>UI adapter model for GameOver / GameClear. Built from live run or DEV preview only.</summary>
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
