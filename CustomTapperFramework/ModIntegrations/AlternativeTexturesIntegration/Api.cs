using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Object = StardewValley.Object;

namespace Selph.StardewMods.MachineTerrainFramework;

public interface IATApi {

  public Texture2D GetTextureForObject(Object obj, out Rectangle sourceRect);
}
