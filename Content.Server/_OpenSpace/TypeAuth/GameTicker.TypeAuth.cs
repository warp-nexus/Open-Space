using Content.Server._OpenSpace.TypeAuth;

namespace Content.Server.GameTicking;

public sealed partial class GameTicker
{
    [Dependency] private TypeAuthManager _typeAuth = default!; // OpenSpace
}
