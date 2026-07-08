using SureType.Models;

namespace SureType.Services;

public interface IInputStateReader
{
    InputState ReadCurrentState();
}
