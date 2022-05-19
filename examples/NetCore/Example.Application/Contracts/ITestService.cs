using System.Threading.Tasks;
using Example.Application.Dtos;

namespace Example.Application.Contracts
{
    public interface ITestService
    {
        Task<bool> AddAsync(TestDto checkInLogDto);
        Task<TestDto> GetByIdAsync(long id);
    }
}