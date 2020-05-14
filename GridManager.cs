using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SimManager.Interfaces;
using SimManager.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimManager
{
    public struct Grid
    {
        public int x;
        public int y;

        public Grid(int xx, int yy)
        {
            x = xx;
            y = yy;
        }
    }

    public enum MoveDirection
    {
        Up,
        Down,
        Left,
        Right
    }


    public class GridManager: IGridManager
    {
        int _columns, _rows = 0;
        int _col_high, _col_low = 0;
        int _row_high, _row_low = 0;
        
        private readonly ILogger _logger;
        private readonly IOptions<ApplicationSettings> _appSettings;
        private readonly object _blockLock = new object();

        public event EventHandler<ZoneEventArgs> ZoneEntered;
        public event EventHandler<ZoneEventArgs> ZoneExited;

        List<GridBlock> _gridBlocks;


        public GridManager(ILogger<GridManager> logger, IOptions<ApplicationSettings> appSettings)
        {
            _logger = logger;
            _appSettings = appSettings;

            _logger.LogInformation("Configuring grid manager");

            _columns = _appSettings.Value.GridColumns;
            _rows = _appSettings.Value.GridRows;

            if (_columns <= 0 || _rows <= 0)
                throw new ArgumentOutOfRangeException("Column and row values must be larger than 0");


            _logger.LogInformation("Total columns " + _columns);
            _logger.LogInformation("Total rows " + _rows);

            //Gets the low and high end of the columns for the cortesion layout
            _col_low = (int)Math.Ceiling((double)_columns / 2) - _columns;
            _col_high = _col_low + _columns;

            //Gets the low and high end of the rows for the cortesion layout
            _row_low = (int)Math.Ceiling((double)_rows / 2) - _rows;
            _row_high = _row_low + _rows;

            _gridBlocks = new List<GridBlock>();

            _logger.LogInformation("Building gridblocks");

            for (int c = _col_low; c < _col_high ; c++)
            {
                 for (int r = _row_low; r < _row_high; r++)
                {
                    //Generates an object to manage each block in the grid
                    _gridBlocks.Add(new GridBlock
                    {
                        Id = Guid.NewGuid(),
                        Location = new Location { Columny = c, Rowx = r },
                        OccupierId = null,
                        ZoneId = null
                    });
                    
                }
            }

            _logger.LogInformation("Num of gridblocks created: " + _gridBlocks.Count);
        }


        public async Task<Guid> AssignBlock(Guid iD, bool isZone = false)
        {
            GridBlock gridBlock;
            var rnd = new Random();
            int ranX, ranY = 0;
            bool occupied = true;
            Location location = new Location();

            //Find an empty block & make sure this is done threadsafe
            lock (_blockLock)
            {
                while (occupied)
                {
                    ranX = rnd.Next(_row_low, _row_high);
                    ranY = rnd.Next(_col_low, _col_high);

                    location.Columny = ranY;
                    location.Rowx = ranX;

                    occupied = IsGridBlockOccupied(location);
                }

                gridBlock = GetBlockByLocation(location);

                //Not the best way to do this, but ok for now
                if (isZone)
                    gridBlock.ZoneId = iD;
                else
                    gridBlock.OccupierId = iD;

            }

            _logger.LogInformation("Grid object id {0} assigned to x: {1} and y: {2} - Zone: {3}", iD, gridBlock.Location.Rowx, gridBlock.Location.Columny, isZone);

            return gridBlock.Id;
        }


        public GridBlock GetBlockByLocation(Location location)
        {
            GridBlock gridBlock;

            gridBlock = _gridBlocks.Find(g => g.Location.Columny == location.Columny && g.Location.Rowx == location.Rowx);

            return gridBlock;
        }

        public bool IsGridBlockOccupied(Location location)
        {
            GridBlock gridBlock = null;

            try
            {
                gridBlock = _gridBlocks.Find(g => g.Location.Columny == location.Columny && g.Location.Rowx == location.Rowx);
            }
            catch(NullReferenceException nullEx)
            {
                throw new OffTheGridException(string.Format("Estimated location x:{0} and y:{1} will exceed the grid coordinates", location.Rowx, location.Columny));
            }

            return gridBlock == null ? false : gridBlock.IsOccupied;
        }


        public async Task<GridBlock> MoveInDirection(SimObject simObject, MoveDirection direction)
        {
            GridBlock currentGridBlock, newGridBlock = null;
            int x, y = 0;

            lock (_blockLock)
            {
                currentGridBlock = _gridBlocks.Find(g => g.OccupierId == simObject.Id);
                
                y = currentGridBlock.Location.Columny;
                x = currentGridBlock.Location.Rowx;

                if (currentGridBlock.IsZone)
                {
                    _logger.LogInformation("Zone {0} exited by {1}", currentGridBlock.Id, simObject.Id);

                    ZoneEventArgs args = new ZoneEventArgs();
                    args.GridBlock = currentGridBlock;
                    args.SimObject = simObject;
                    OnZoneExited(args);
                }

                switch (direction)
                {
                    case MoveDirection.Up:
                        y = y + 1;

                        if (y > _col_high)
                            throw new OffTheGridException(string.Format("Moving {0} UP to estimated location {1} will exceed the grid coordinates", simObject.Id, y));

                        break;

                    case MoveDirection.Down:
                        y = y - 1;

                        if (y < _col_low)
                            throw new OffTheGridException(string.Format("Moving {0} DOWN to estimated location {1} will exceed the grid coordinates", simObject.Id, y));

                        break;

                    case MoveDirection.Left:
                        x = x - 1;

                        if (x < _row_low)
                            throw new OffTheGridException(string.Format("Moving {0} LEFT to estimated location {1} will exceed the grid coordinates", simObject.Id, x));

                        break;

                    case MoveDirection.Right:
                        x = x + 1;

                        if (x > _row_high)
                            throw new OffTheGridException(string.Format("Moving {0} RIGHT to estimated location {1} will exceed the grid coordinates", simObject.Id, x));

                        break;

                    default:
                        break;
                }


                if (IsGridBlockOccupied(new Location { Columny = y, Rowx = x }))
                    throw new LocationOccupiedException(string.Format("Grid block already occupied at location x:{0}, y:{1} by {2}", x, y, currentGridBlock.OccupierId));


                //Get the new grid block
                newGridBlock = _gridBlocks.Find(g => g.Location.Columny == y && g.Location.Rowx == x);

                if (newGridBlock != null)
                {
                    newGridBlock.OccupierId = simObject.Id;
                    currentGridBlock.OccupierId = null;

                    if (newGridBlock.IsZone)
                    {
                        _logger.LogInformation("Zone {0} entered by {1}", newGridBlock.ZoneId, simObject.Id);

                        ZoneEventArgs args = new ZoneEventArgs();
                        args.GridBlock = newGridBlock;
                        args.SimObject = simObject;
                        OnZoneEntered(args);
                    }
                }

                _logger.LogInformation("Moved sim {0} in direction: {1} to x: {2} and y {3}", simObject.Id, direction, x, y);
            }


            return newGridBlock != null ? newGridBlock : null;
        }

        protected virtual void OnZoneEntered(ZoneEventArgs e)
        {
            ZoneEntered?.Invoke(this, e);

        }

        protected virtual void OnZoneExited(ZoneEventArgs e)
        {
            ZoneExited?.Invoke(this, e);

        }

        public async Task<Location> GetGridLocation(Guid id)
        {
            return _gridBlocks.Find(g => g.Id == id).Location;
        }

        public async Task<GridBlock> MoveToLocation(SimObject simObject, Location location)
        {

            //Not implemented yet....

            GridBlock currentGridBlock;

            currentGridBlock = _gridBlocks.Find(g => g.OccupierId == simObject.Id);

            return currentGridBlock;
        }

        public int Columns
        {
            get
            {
                return _columns;
            }
        }

        public int Rows
        {
            get
            {
                return _rows;
            }
        }

        public async Task<Grid> GetGridSize()
        {
            return new Grid(_columns, _rows);
        }
    }

    public class OffTheGridException : Exception
    {
        public OffTheGridException()
        {
        }

        public OffTheGridException(string message)
            : base(message)
        {
        }

        public OffTheGridException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

    public class LocationOccupiedException : Exception
    {
        public LocationOccupiedException()
        {
        }

        public LocationOccupiedException(string message)
            : base(message)
        {
        }

        public LocationOccupiedException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}

