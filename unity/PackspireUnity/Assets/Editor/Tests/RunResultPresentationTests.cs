using NUnit.Framework;

namespace Packspire.Tests {
public sealed class RunResultPresentationTests {
 [TestCase(ExpeditionEndReason.Defeat,RunResultType.Defeat)]
 [TestCase(ExpeditionEndReason.Return,RunResultType.Return)]
 [TestCase(ExpeditionEndReason.Clear,RunResultType.Clear)]
 public void ResolveType_UsesFinalizedExpeditionReason(
  ExpeditionEndReason reason,RunResultType expected){
  var summary=new ExpeditionFinalizationSummary{reason=reason};

  Assert.That(RunResultPresentationSystem.ResolveType(true,summary),Is.EqualTo(expected));
  Assert.That(RunResultPresentationSystem.ResolveType(false,summary),Is.EqualTo(expected));
 }

 [TestCase(false,RunResultType.Defeat)]
 [TestCase(true,RunResultType.Clear)]
 public void ResolveType_UsesScreenFallbackWithoutFinalization(
  bool clearFallback,RunResultType expected){
  Assert.That(
   RunResultPresentationSystem.ResolveType(clearFallback,null),
   Is.EqualTo(expected));
 }
}
}
