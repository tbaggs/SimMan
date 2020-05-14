using System.Threading.Tasks;

namespace SimManager.Interfaces
{
    public interface ISimManager
    {
        Task StartSimulation();
        Task StopSimulation();
        Task GeneratePeople();
        Task GenerateZones();
    }
}
