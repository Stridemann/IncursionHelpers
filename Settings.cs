using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;

namespace IncursionHelpers
{
    public class Settings : ISettings
    {
        public ToggleNode Enable { get; set; } = new ToggleNode(true);
    }
}