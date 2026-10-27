using TourVoice.API.Presence;
using Xunit;

namespace TourVoice.API.Tests;

public class PresenceTrackerTests
{
    [Fact]
    public void Heartbeat_AddsNewEntry_ReflectedInCounts()
    {
        var tracker = new PresenceTracker();

        tracker.Heartbeat("conn-1", "dinh-binh-tien");

        var counts = tracker.GetCounts();
        Assert.Equal(1, counts["dinh-binh-tien"]);
    }

    [Fact]
    public void Heartbeat_MultipleVisitorsSamePlace_CountsCorrectly()
    {
        var tracker = new PresenceTracker();

        tracker.Heartbeat("conn-1", "dinh-binh-tien");
        tracker.Heartbeat("conn-2", "dinh-binh-tien");
        tracker.Heartbeat("conn-3", "cho-binh-tay");

        var counts = tracker.GetCounts();
        Assert.Equal(2, counts["dinh-binh-tien"]);
        Assert.Equal(1, counts["cho-binh-tay"]);
    }

    [Fact]
    public void Heartbeat_SameConnectionTwice_DoesNotDoubleCount()
    {
        var tracker = new PresenceTracker();

        tracker.Heartbeat("conn-1", "dinh-binh-tien");
        tracker.Heartbeat("conn-1", "dinh-binh-tien");

        var counts = tracker.GetCounts();
        Assert.Equal(1, counts["dinh-binh-tien"]);
    }

    [Fact]
    public void Heartbeat_ConnectionSwitchesPlace_MovesCountToNewPlace()
    {
        var tracker = new PresenceTracker();

        tracker.Heartbeat("conn-1", "dinh-binh-tien");
        tracker.Heartbeat("conn-1", "cho-binh-tay");

        var counts = tracker.GetCounts();
        Assert.False(counts.ContainsKey("dinh-binh-tien"));
        Assert.Equal(1, counts["cho-binh-tay"]);
    }

    [Fact]
    public void Remove_RemovesConnection_NoLongerCounted()
    {
        var tracker = new PresenceTracker();
        tracker.Heartbeat("conn-1", "dinh-binh-tien");

        tracker.Remove("conn-1");

        var counts = tracker.GetCounts();
        Assert.False(counts.ContainsKey("dinh-binh-tien"));
    }

    [Fact]
    public void PurgeExpired_RemovesEntriesOlderThanTtl()
    {
        var tracker = new PresenceTracker();
        tracker.Heartbeat("conn-1", "dinh-binh-tien");

        var removedCount = tracker.PurgeExpired(TimeSpan.Zero);

        Assert.Equal(1, removedCount);
        Assert.Empty(tracker.GetCounts());
    }

    [Fact]
    public void PurgeExpired_KeepsFreshEntries()
    {
        var tracker = new PresenceTracker();
        tracker.Heartbeat("conn-1", "dinh-binh-tien");

        var removedCount = tracker.PurgeExpired(TimeSpan.FromSeconds(45));

        Assert.Equal(0, removedCount);
        Assert.Equal(1, tracker.GetCounts()["dinh-binh-tien"]);
    }
}