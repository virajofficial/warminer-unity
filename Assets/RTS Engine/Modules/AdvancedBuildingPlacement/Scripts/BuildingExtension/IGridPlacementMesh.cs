
using RTSEngine.Game;

namespace RTSEngine.BuildingExtension
{
    public interface IGridPlacementMesh : IPostRunGameService
    {
        void Toggle(bool enable);
    }
}
