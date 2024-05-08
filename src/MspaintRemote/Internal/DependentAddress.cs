namespace MspaintRemote.Internal;

/// <summary>
/// Represents an address that is only valid after a certain action is performed by the user.
/// </summary>
internal struct DependentAddress(DependentAddressProvider provider)
{
    nuint value;
    bool initialized;

    public readonly DependentAddressProvider Provider = provider;
    
    public nuint? Value {
        get {
            if (initialized)
                return value;

            if (Provider(out value)) {
                initialized = true;
                return value;
            }
            
            return null;
        }
    }
}

internal delegate bool DependentAddressProvider(out nuint address);