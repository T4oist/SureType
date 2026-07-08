using SureType.Models;

namespace SureType.Services;

public sealed class InputStateChangedEventArgs : EventArgs
{
    public InputStateChangedEventArgs(InputState state)
    {
        State = state;
    }

    public InputState State { get; }
}
