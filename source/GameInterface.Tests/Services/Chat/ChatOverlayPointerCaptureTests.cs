using GameInterface.Services.Chat;
using Xunit;

namespace GameInterface.Tests.Services.Chat;

public class ChatOverlayPointerCaptureTests
{
    // #4038: clicking into the input to type must not turn later map clicks into a chat hold
    [Fact]
    public void MapClicksAfterTyping_AreNeverHeldByTheOpenChat()
    {
        bool held = false;

        held = Frame(held, buttonDown: true, buttonPressed: true, pointerOverChat: true);
        Assert.True(held);
        held = Frame(held, buttonDown: false, buttonPressed: false, pointerOverChat: true);
        Assert.False(held);

        // Typing frames, then the pointer leaves the panel
        held = Frame(held, buttonDown: false, buttonPressed: false, pointerOverChat: true);
        held = Frame(held, buttonDown: false, buttonPressed: false, pointerOverChat: false);

        for (int click = 0; click < 2; click++)
        {
            held = Frame(held, buttonDown: true, buttonPressed: true, pointerOverChat: false);
            Assert.False(held);
            held = Frame(held, buttonDown: true, buttonPressed: false, pointerOverChat: false);
            Assert.False(held);
            held = Frame(held, buttonDown: false, buttonPressed: false, pointerOverChat: false);
            Assert.False(held);
        }
    }

    [Fact]
    public void DragStartedOnTheOpenChat_StaysHeldOffThePanelUntilRelease()
    {
        bool held = Frame(false, buttonDown: true, buttonPressed: true, pointerOverChat: true);

        held = Frame(held, buttonDown: true, buttonPressed: false, pointerOverChat: false);
        Assert.True(held);

        held = Frame(held, buttonDown: false, buttonPressed: false, pointerOverChat: false);
        Assert.False(held);
    }

    private static bool Frame(bool held, bool buttonDown, bool buttonPressed, bool pointerOverChat)
        => ChatOverlay.ShouldKeepChatPointerCapture(held, buttonDown, buttonPressed, pointerOverChat);
}
