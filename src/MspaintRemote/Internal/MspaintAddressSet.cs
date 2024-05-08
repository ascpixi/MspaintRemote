namespace MspaintRemote.Internal;

internal struct MspaintAddressSet
{
    public nuint Zoom;
    public DependentAddress FontSize;
    
    public void Initialize(MspaintController owner)
    {
        Zoom = FollowTrail(owner, [0xE0EA8, 0xE0]) + 0x104;
        FontSize = FollowDependentTrail(owner, [0xDF980, 0x30, 0x158, 0x0, 0x138], 0x148);
    }
    
    static nuint FollowTrail(MspaintController owner, ReadOnlySpan<uint> offsets)
    {
        nuint addr = owner.BaseAddress;
        foreach (uint t in offsets) {
            addr = owner.ReadArbitrary<nuint>(addr + t);
        }

        return addr;
    }

    static DependentAddress FollowDependentTrail(MspaintController owner, uint[] offsets, uint final)
    {
        return new((out nuint addr) => {
            addr = owner.BaseAddress;
            try {
                foreach (uint t in offsets) {
                    addr = owner.ReadArbitrary<nuint>(addr + t);
                }
            }
            catch {
                return false;
            }

            addr += final;
            return true;
        });
    }
}