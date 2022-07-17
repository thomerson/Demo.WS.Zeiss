using Demo.WS.Zeiss.Model;

namespace Demo.WS.Core.contract
{
    public interface IMachineService
    {
        Task UpdateStatus(wsMachineInfo info);
    }
}
