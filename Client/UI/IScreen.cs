using Microsoft.Xna.Framework;

namespace RealmOfReality.Client.UI;

/// <summary>
/// Interface for game screens (login, character select, gameplay)
/// </summary>
public interface IScreen
{
    void Enter();
    void Exit();
    void Update(GameTime gameTime);
    void Draw(GameTime gameTime);
}
