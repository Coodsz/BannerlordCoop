using GameInterface.Configuration;
using GameInterface.Services.Save.Patches;
using Xunit;

namespace GameInterface.Tests.Services.Save;

[Collection("ModConfigSerial")]
public class AutosaveCountPatchesTests
{
    [Fact]
    public void DefaultCount_RotatesThroughTheThreeVanillaSlots()
    {
        Assert.Equal(3, new ModOptions(new ModOptionsData()).AutosaveCount);
        Assert.Equal(new[] { 1, 2, 3, 1, 2 }, Rotate(start: 0, count: 3, steps: 5));
    }

    [Fact]
    public void ConfiguredCount_RotatesThroughThatManySlots()
    {
        Assert.Equal(5, new ModOptions(new ModOptionsData { AutosaveCount = 5 }).AutosaveCount);
        Assert.Equal(new[] { 1, 2, 3, 4, 5, 1 }, Rotate(start: 0, count: 5, steps: 6));
    }

    [Fact]
    public void LoweredCount_WrapsFromAHigherSlotWithoutReclaimingIt()
    {
        Assert.Equal(new[] { 1, 2, 1 }, Rotate(start: 5, count: 2, steps: 3));
        Assert.False(AutosaveCountPatches.TryGetAutosaveSlot("saveauto5", "saveauto", 2, out _));
    }

    [Theory]
    [InlineData(-4, 1)]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(3, 3)]
    [InlineData(20, 20)]
    [InlineData(21, 20)]
    public void Count_IsClampedBetweenOneAndTwenty(int configured, int expected)
    {
        Assert.Equal(expected, AutosaveCountPatches.ClampAutosaveCount(configured));
    }

    [Theory]
    [InlineData("saveauto1", 3, true, 1)]
    [InlineData("saveauto3", 3, true, 3)]
    [InlineData("saveauto4", 3, false, 0)]
    [InlineData("saveauto4", 5, true, 4)]
    [InlineData("saveauto0", 5, false, 0)]
    [InlineData("save12", 5, false, 0)]
    [InlineData("saveautox", 5, false, 0)]
    public void AutosaveSlot_IsOnlyAnAutosaveNameWithinTheCount(string name, int count, bool expected, int expectedSlot)
    {
        Assert.Equal(expected, AutosaveCountPatches.TryGetAutosaveSlot(name, "saveauto", count, out var slot));
        Assert.Equal(expectedSlot, expected ? slot : 0);
    }

    private static int[] Rotate(int start, int count, int steps)
    {
        var slots = new int[steps];
        int index = start;
        for (int i = 0; i < steps; i++)
            slots[i] = index = AutosaveCountPatches.NextAutosaveIndex(index, count);
        return slots;
    }
}
