using SimManager.Models;
using System;
using System.Threading.Tasks;

namespace SimManager.Interfaces
{
    public interface IGridManager
    {
        Task<Grid> GetGridSize();
        Task<Guid> AssignBlock(Guid iD, bool isZone = false);
        GridBlock GetBlockByLocation(Location location);
        bool IsGridBlockOccupied(Location location);
        Task<GridBlock> MoveInDirection(SimObject simObject, MoveDirection direction);
        Task<GridBlock> MoveToLocation(SimObject simObject, Location location);
        Task<Location> GetGridLocation(Guid id);
        event EventHandler<ZoneEventArgs> ZoneEntered;
        event EventHandler<ZoneEventArgs> ZoneExited;
    }
}
