using Microsoft.EntityFrameworkCore.Storage;
using Moq;
namespace Fulfilment.Test.FulfilmentTestInfrastructure
{

    public static class TxMock
    {
        public static Mock<IDbContextTransaction> Create()
        {
            var tx = new Mock<IDbContextTransaction>();

            tx.Setup(t => t.CommitAsync(It.IsAny<CancellationToken>()))
              .Returns(Task.CompletedTask);

            tx.Setup(t => t.RollbackAsync(It.IsAny<CancellationToken>()))
              .Returns(Task.CompletedTask);

            tx.Setup(t => t.DisposeAsync())
              .Returns(ValueTask.CompletedTask);

            return tx;
        }
    }

}
