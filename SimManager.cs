using Microsoft.Azure.Devices.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SimManager.Interfaces;
using SimManager.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SimManager
{
    public class SimManager : ISimManager
    {
        private readonly ILogger _logger;
        private readonly IOptions<ApplicationSettings> _appSettings;
        private readonly IDeviceManager _deviceManager;
        private readonly IGridManager _gridManager;
        List<Person> _people;
        List<Zone> _zones;
        List<Vehicle> _vehicles;


        public SimManager(ILogger<SimManager> logger, IOptions<ApplicationSettings> appSettings,
            IDeviceManager deviceManager, IGridManager gridManager)
        {
            _logger = logger;
            _appSettings = appSettings;
            _deviceManager = deviceManager;
            _gridManager = gridManager;

            _people = new List<Person>();
            _zones = new List<Zone>();
            _vehicles = new List<Vehicle>();

            _gridManager.ZoneEntered += GridZoneEntered;
            _gridManager.ZoneExited += GridZoneExited;
        }

        private async void GridZoneEntered(object sender, ZoneEventArgs args)
        {
            Zone zone = _zones.Find(z => z.Id == args.GridBlock.ZoneId);

            var telemetryDataPoint = new ZoneDataPoint
            {
                DeviceId = zone.IoTDeviceInfo.DeviceId,
                Name = args.SimObject.Name,
                Type = zone.Type,
                Grid_x = args.GridBlock.Location.Rowx,
                Grid_y = args.GridBlock.Location.Columny,
                Operation = "zone_entered",
                InZone = args.GridBlock.IsZone,
                ZoneId = args.GridBlock.ZoneId,
                ZoneGrid_x = args.GridBlock.Location.Rowx,
                ZoneGrid_y = args.GridBlock.Location.Columny,
                ZoneName = _zones.Find(z => z.Id == args.GridBlock.ZoneId).Name
            };

            await SendIoTHubMessage(zone, telemetryDataPoint).ConfigureAwait(false);

            if (_appSettings.Value.SimulateButtonPushes)
            {
                var rand = new Random();
                int c = rand.Next(1, 10);

                if (c % 2 == 0)
                {
                    telemetryDataPoint.Operation = "button_pushed";

                    _logger.LogInformation("Sending button push message to hub");

                    await SendIoTHubMessage(zone, telemetryDataPoint).ConfigureAwait(false);
                }
            }
        }

        private async void GridZoneExited(object sender, ZoneEventArgs args)
        {
            Zone zone = _zones.Find(z => z.Id == args.GridBlock.ZoneId);

            var telemetryDataPoint = new ZoneDataPoint
            {
                DeviceId = zone.IoTDeviceInfo.DeviceId,
                Name = args.SimObject.Name,
                Type = "zone",
                Grid_x = args.GridBlock.Location.Rowx,
                Grid_y = args.GridBlock.Location.Columny,
                Operation = "zone_exited",
                InZone = args.GridBlock.IsZone,
                ZoneId = args.GridBlock.ZoneId,
                ZoneGrid_x = args.GridBlock.Location.Rowx,
                ZoneGrid_y = args.GridBlock.Location.Columny,
                ZoneName = _zones.Find(z => z.Id == args.GridBlock.ZoneId).Name
            };

            await SendIoTHubMessage(zone, telemetryDataPoint).ConfigureAwait(false);
        }

        public async Task StartSimulation()
        {
            _logger.LogInformation("Starting simulation");

            
            //Sends only a button push if configured
            if (_appSettings.Value.SimulateOnlyButtonPushes)
            {
                _logger.LogInformation("Simulating button push only");

                await SimulateButtonPush();

                _logger.LogInformation("Button simulation completed");

                return;
            }


            await GeneratePeople().ConfigureAwait(false); ;
            _logger.LogInformation("Sims created and assigned to grid blocks");


            await GenerateZones().ConfigureAwait(false);
            _logger.LogInformation("Zones created and assigned to grid blocks");

            
            await GenerateVehicles().ConfigureAwait(false);
            _logger.LogInformation("Vehicles created and assigned to grid blocks");


            //This is mostly for testing, but is good to initiate the simulation
            await SendStartupMessages().ConfigureAwait(false);


            //This will begin moving the sims based on the number of movements requested
            for (int i = 0; i < _appSettings.Value.MovementsToSimulate; i++)
            {
                await MoveSims(_people).ConfigureAwait(false);
                await MoveSims(_vehicles).ConfigureAwait(false);
            }

            _logger.LogInformation("Completing simulation");
        }


        private async Task SimulateButtonPush()
        {
            Guid iD = Guid.NewGuid();

            _logger.LogInformation("Generating grid zone {0} for button push", iD);

            Zone zone = new Zone
            {
                Id = iD,
                Name = "Zone_Button_Sim",
                IoTDeviceInfo = await _deviceManager.AddDeviceAsync(iD.ToString()).ConfigureAwait(false),
                GridLocation = await _gridManager.AssignBlock(iD, true).ConfigureAwait(false)
            };

            Location location = await _gridManager.GetGridLocation(zone.GridLocation).ConfigureAwait(false);

            _logger.LogInformation("Creating telemetry data point for device {0}", zone.Name);

            var telemetryDataPoint = new ZoneDataPoint
            {
                DeviceId = zone.IoTDeviceInfo.DeviceId,
                Name = "Sim1",
                Type = zone.Type,
                Grid_x = location.Rowx,
                Grid_y = location.Columny,
                Operation = "button_pushed",
                InZone = true,
                ZoneId = zone.Id,
                ZoneGrid_x = location.Rowx,
                ZoneGrid_y = location.Columny,
                ZoneName = zone.Name
            };

            _logger.LogInformation("Sending button push message to hub");

            await SendIoTHubMessage(zone, telemetryDataPoint).ConfigureAwait(false);


            _logger.LogInformation("Removing device {0} from IoT Hub", zone.Id);

            await _deviceManager.RemoveDeviceAsync(zone.Id.ToString()).ConfigureAwait(false);
        }



        public async Task GenerateVehicles()
        {
            //Generate the sims and assign them a grid spot
            for (int i = 0; i < _appSettings.Value.Vehicles; i++)
            {
                Guid iD = Guid.NewGuid();

                _logger.LogInformation("Generating sim vehicle {0}", iD);

                _vehicles.Add(new Vehicle
                {
                    Id = iD,
                    Name = "Vehicle-" + i,
                    IoTDeviceInfo = await _deviceManager.AddDeviceAsync(iD.ToString()).ConfigureAwait(false),
                    GridLocation = await _gridManager.AssignBlock(iD).ConfigureAwait(false)
                });
            }
        }

        public async Task GeneratePeople()
        {
            //Generate the sims and assign them a grid spot
            for (int i = 0; i < _appSettings.Value.Sims; i++)
            {
                Guid iD = Guid.NewGuid();

                _logger.LogInformation("Generating sim person {0}", iD);

                _people.Add(new Person
                {
                    Id = iD,
                    Name = "Sim-" + i,
                    IoTDeviceInfo = await _deviceManager.AddDeviceAsync(iD.ToString()).ConfigureAwait(false),
                    GridLocation = await _gridManager.AssignBlock(iD).ConfigureAwait(false)
                });
            }
        }

        public async Task GenerateZones()
        {
            //Generate the sims and assign them a grid spot
            foreach (ZoneSetting zone in _appSettings.Value.Zones)
            {
                Guid iD = Guid.NewGuid();

                _logger.LogInformation("Generating grid zone {0}", iD);

                _zones.Add(new Zone
                {
                    Id = iD,
                    Name = zone.Name,
                    IoTDeviceInfo = await _deviceManager.AddDeviceAsync(iD.ToString()).ConfigureAwait(false),
                    GridLocation = await _gridManager.AssignBlock(iD, true).ConfigureAwait(false)
                });
            }
        }

        public async Task MoveSims(List<Vehicle> simObjects)
        {
            List<Task> tasks = new List<Task>();

            foreach (Vehicle v in simObjects)
            {
                Task t = Task.Run(async () =>
                {
                    await MoveSim(v);
                });

                tasks.Add(t);
                tasks.Add(Task.Delay(_appSettings.Value.Delay));

            }
            
            try
            {
                Task.WaitAll(tasks.ToArray());
            }
            catch (AggregateException e)
            {
                _logger.LogInformation("The following exceptions were encountered during processing");

                for (int j = 0; j < e.InnerExceptions.Count; j++)
                {
                    _logger.LogError(e.InnerExceptions[j].ToString());
                }
            }
        }


        public async Task MoveSims(List<Person> simObjects)
        {
            List<Task> tasks = new List<Task>();

            foreach (Person p in simObjects)
            {
                Task t = Task.Run(async () =>
                {
                    await MoveSim(p);
                });

                tasks.Add(t);
                tasks.Add(Task.Delay(_appSettings.Value.Delay));
            }

            try
            {
                Task.WaitAll(tasks.ToArray());
            }
            catch (AggregateException e)
            {
                _logger.LogInformation("The following exceptions were encountered during processing");

                for (int j = 0; j < e.InnerExceptions.Count; j++)
                {
                    _logger.LogError(e.InnerExceptions[j].ToString());
                }
            }

        }

        public async Task MoveSim(SimObject simObject)
        {
            GridBlock gridBlock;

            try
            {
                //Gets a random direction and attempts to move the sim
                gridBlock = await _gridManager.MoveInDirection(simObject, await GetRandomDirection().ConfigureAwait(false)).ConfigureAwait(false);

                if (gridBlock != null)
                {
                    var telemetryDataPoint = new TelemetryDataPoint
                    {
                        DeviceId = simObject.IoTDeviceInfo.DeviceId,
                        Name = simObject.Name,
                        Type = simObject.Type,
                        Grid_x = gridBlock.Location.Rowx,
                        Grid_y = gridBlock.Location.Columny,
                        InZone = gridBlock.IsZone,
                        Operation = "move_completed"
                    };

                    await SendIoTHubMessage(simObject, telemetryDataPoint).ConfigureAwait(false);
                }
            }
            catch (LocationOccupiedException lex)
            {
                _logger.LogError(lex.Message);
            }
            catch (OffTheGridException oex)
            {
                _logger.LogError(oex.Message);
            }

        }

        #region Old MoveSims
        /*
                public async Task MoveSims(List<SimObject> simObjects)
                {
                    List<Task> tasks = new List<Task>();
                    GridBlock gridBlock;

                    foreach (SimObject p in simObjects)
                    {
                        Task t = Task.Run(async () =>
                        {
                            try
                            {
                                //Gets a random direction and attempts to move the sim
                                gridBlock = await _gridManager.MoveInDirection(p, await GetRandomDirection().ConfigureAwait(false)).ConfigureAwait(false);

                                if (gridBlock != null)
                                {
                                    var telemetryDataPoint = new TelemetryDataPoint
                                    {
                                        DeviceId = p.IoTDeviceInfo.DeviceId,
                                        Name = p.Name,
                                        Type = p.GetType().ToString().ToLower(),
                                        Grid_x = gridBlock.Location.Rowx,
                                        Grid_y = gridBlock.Location.Columny,
                                        InZone = gridBlock.IsZone,
                                        Operation = "move_completed"
                                    };

                                    await SendIoTHubMessage(p, telemetryDataPoint).ConfigureAwait(false);

                                }
                            }
                            catch (LocationOccupiedException lex)
                            {
                                _logger.LogError(lex.Message);
                            }
                            catch (OffTheGridException oex)
                            {
                                _logger.LogError(oex.Message);
                            }
                        });

                        tasks.Add(t);
                        tasks.Add(Task.Delay(_appSettings.Value.Delay));
                    }

                    try
                    {
                        Task.WaitAll(tasks.ToArray());
                    }
                    catch (AggregateException e)
                    {
                        _logger.LogInformation("The following exceptions were encountered during processing");

                        for (int j = 0; j < e.InnerExceptions.Count; j++)
                        {
                            _logger.LogError(e.InnerExceptions[j].ToString());
                        }
                    }
                }

        */
        #endregion


        public async Task<MoveDirection> GetRandomDirection()
        {
            var rnd = new Random();
            return (MoveDirection)rnd.Next(Enum.GetNames(typeof(MoveDirection)).Length);
        }


        public async Task SendStartupMessages()
        {
            _logger.LogInformation("Sending startup messages");

            List<Task> tasks = new List<Task>();

            foreach (Person p in _people)
            {
                Task t = Task.Run(async () =>
                {
                    Location curLocation = await _gridManager.GetGridLocation(p.GridLocation).ConfigureAwait(false);

                    var telemetryDataPoint = new TelemetryDataPoint
                    {
                        DeviceId = p.IoTDeviceInfo.DeviceId,
                        Name = p.Name,
                        Type = p.Type,
                        Grid_x = curLocation.Rowx,
                        Grid_y = curLocation.Columny,
                        Operation = "startup_completed"
                    };

                    await SendIoTHubMessage(p, telemetryDataPoint).ConfigureAwait(false);
                });

                tasks.Add(t);
            }

            foreach (Zone z in _zones)
            {
                Task t = Task.Run(async () =>
                {
                    Location curLocation = await _gridManager.GetGridLocation(z.GridLocation).ConfigureAwait(false);

                    var telemetryDataPoint = new TelemetryDataPoint
                    {
                        DeviceId = z.IoTDeviceInfo.DeviceId,
                        Name = z.Name,
                        Type = z.Type,
                        Grid_x = curLocation.Rowx,
                        Grid_y = curLocation.Columny,
                        Operation = "startup_completed"
                    };

                    await SendIoTHubMessage(z, telemetryDataPoint).ConfigureAwait(false);
                });

                tasks.Add(t);
            }

            foreach (Vehicle v in _vehicles)
            {
                Task t = Task.Run(async () =>
                {
                    Location curLocation = await _gridManager.GetGridLocation(v.GridLocation).ConfigureAwait(false);

                    var telemetryDataPoint = new TelemetryDataPoint
                    {
                        DeviceId = v.IoTDeviceInfo.DeviceId,
                        Name = v.Name,
                        Type = v.Type,
                        Grid_x = curLocation.Rowx,
                        Grid_y = curLocation.Columny,
                        Operation = "startup_completed"
                    };

                    await SendIoTHubMessage(v, telemetryDataPoint).ConfigureAwait(false);
                });

                tasks.Add(t);
            }

            try
            {
                Task.WaitAll(tasks.ToArray());
            }
            catch (AggregateException e)
            {
                _logger.LogInformation("The following exceptions were encountered during processing");

                for (int j = 0; j < e.InnerExceptions.Count; j++)
                {
                    _logger.LogError(e.InnerExceptions[j].ToString());
                }
            }
        }

        private async Task SendIoTHubMessage(SimObject simObject, object message)
        {
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(simObject.IoTDeviceInfo.IoTHubConnectionString, TransportType.Mqtt);

            string messageString = JsonConvert.SerializeObject(message);

            using (var eventMessage = new Message(Encoding.UTF8.GetBytes(messageString)))
            {
                await deviceClient.SendEventAsync(eventMessage).ConfigureAwait(false);
            }

            await deviceClient.CloseAsync().ConfigureAwait(false);
        }


        public async Task StopSimulation()
        {
            _logger.LogInformation("Stopping simulation");


            List<Task> tasks = new List<Task>();

            //Cleanup the IoT Device info
            foreach (Person p in _people)
            {

                Task t = Task.Run(async () =>
                {
                    await _deviceManager.RemoveDeviceAsync(p.Id.ToString()).ConfigureAwait(false);
                    _logger.LogInformation("{0} removed from IoT Hub", p.Name);
                });

                tasks.Add(t);
            }

            foreach (Zone z in _zones)
            {
                Task t = Task.Run(async () =>
                {
                    await _deviceManager.RemoveDeviceAsync(z.Id.ToString()).ConfigureAwait(false);
                    _logger.LogInformation("{0} removed from IoT Hub", z.Name);
                });

                tasks.Add(t);
            }

            foreach (Vehicle v in _vehicles)
            {
                Task t = Task.Run(async () =>
                {
                    await _deviceManager.RemoveDeviceAsync(v.Id.ToString()).ConfigureAwait(false);
                    _logger.LogInformation("{0} removed from IoT Hub", v.Name);
                });

                tasks.Add(t);
            }

            try
            {
                Task.WaitAll(tasks.ToArray());
            }
            catch (AggregateException e)
            {
                _logger.LogInformation("The following exceptions were encountered during processing");

                for (int j = 0; j < e.InnerExceptions.Count; j++)
                {
                    _logger.LogError(e.InnerExceptions[j].ToString());
                }
            }

            _logger.LogInformation("Simulation stopped");
        }
    }
}
