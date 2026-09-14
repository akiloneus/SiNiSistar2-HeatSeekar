using SiNiSistar2;
using SiNiSistar2.Obj;

namespace HeatSeekar;

// Route the alternate chord through the native sword/arrow state machine. Its
// unlock checks, timing, costs and explosion behavior remain authoritative.
internal sealed class CombatInput
{
    private InputManager? owner;
    private bool attack, sword, swordHeld;

    internal void Apply(InputManager input, bool enabled, bool hasExplosion, bool attackHeld, MagicArrow.EState bowState, bool bowIsAction)
    {
        Restore();
        if (!enabled || !hasExplosion || !input.IsPressedMagic01 || !attackHeld) return;
        // A held Shoot Magic key survives jump/crouch cancellation. Draw can
        // survive it too, so both the phase and native action flag are required.
        // A fresh Shoot press still permits the simultaneous two-button chord.
        if (!(bowState == MagicArrow.EState.Draw && bowIsAction) && !input.WasPressedThisFrameMagic01) return;
        owner = input;
        attack = input.WasPressedThisFrameAttack;
        sword = input.WasPressedThisFrameSword;
        swordHeld = input.IsPressedMagic02;
        input.WasPressedThisFrameSword = sword || attack;
        input.IsPressedMagic02 = true;
        input.WasPressedThisFrameAttack = false;
    }

    internal void Restore()
    {
        if (owner == null) return;
        owner.WasPressedThisFrameAttack = attack;
        owner.WasPressedThisFrameSword = sword;
        owner.IsPressedMagic02 = swordHeld;
        owner = null;
    }
}
