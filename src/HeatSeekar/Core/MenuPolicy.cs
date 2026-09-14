namespace HeatSeekar.Core;

// Device-independent rules shared by runtime navigation and regression checks.
internal sealed class MenuPolicy
{
    public bool PointerActive { get; private set; }
    public bool SlotFocus { get; private set; }
    private int lastX, lastY;
    private double repeatAt;

    public void Enter(bool pointerActive = false)
    {
        PointerActive = pointerActive;
        SlotFocus = false;
        lastX = lastY = 0;
        repeatAt = 0;
    }

    public void UpdatePointer(bool moved, bool navigationInput)
    {
        if (moved) PointerActive = true;
        if (navigationInput) PointerActive = false;
    }

    public void EnterSlots() => SlotFocus = true;
    public void LeaveSlots() => SlotFocus = false;

    public bool ConsumeSlotCancel()
    {
        if (!SlotFocus) return false;
        SlotFocus = false;
        return true;
    }

    public bool Repeat(int x, int y, double now)
    {
        if (x == 0 && y == 0) { lastX = lastY = 0; return false; }
        if (x != lastX || y != lastY)
        {
            lastX = x; lastY = y; repeatAt = now + 0.5;
            return true;
        }
        if (now < repeatAt) return false;
        repeatAt = now + 0.1;
        return true;
    }

    public static int Wrap(int index, int step, int count) => count <= 0 ? -1 : ((index + step) % count + count) % count;
}
