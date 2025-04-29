namespace Grb.Building.Tests
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Design;
    using Microsoft.EntityFrameworkCore.Diagnostics;
    using Microsoft.EntityFrameworkCore.Infrastructure;
    using Microsoft.EntityFrameworkCore.Storage;

    public class FakeBuildingGrbContext : BuildingGrbContext
    {
        private readonly bool _canBeDisposed;
        public FakeDatabaseFacade FakeDatabase;

        public FakeBuildingGrbContext(DbContextOptions<BuildingGrbContext> options, bool canBeDisposed) : base(options)
        {
            _canBeDisposed = canBeDisposed;
            FakeDatabase = new FakeDatabaseFacade(this);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning));

            base.OnConfiguring(optionsBuilder);
        }

        public override DatabaseFacade Database => FakeDatabase;

        public override void Dispose()
        {
            if (_canBeDisposed)
            {
                base.Dispose();
            }
        }

        public override ValueTask DisposeAsync()
        {
            if(_canBeDisposed)
                return base.DisposeAsync();

            return ValueTask.CompletedTask;
        }
    }

    public class FakeDatabaseFacade : DatabaseFacade
    {
        private IDbContextTransaction? _currentTransaction;

        public FakeDatabaseFacade(DbContext context) : base(context)
        { }

        public override Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = new())
        {
            IDbContextTransaction transaction = new FakeDbContextTransaction();
            _currentTransaction = transaction;

            return Task.FromResult(transaction);
        }

        public override IDbContextTransaction? CurrentTransaction => _currentTransaction;
    }

    public class FakeDbContextTransaction : IDbContextTransaction
    {
        public FakeDbContextTransaction()
        {
            TransactionId = Guid.NewGuid();
        }

        public TransactionStatus Status { get; private set; } = TransactionStatus.Started;

        public enum TransactionStatus
        {
            Started,
            Committed,
            Rolledback
        }

        public void Dispose() { }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public void Commit()
        {
            Status = TransactionStatus.Committed;
        }

        public Task CommitAsync(CancellationToken cancellationToken = new())
        {
            Status = TransactionStatus.Committed;
            return Task.CompletedTask;
        }

        public void Rollback()
        {
            Status = TransactionStatus.Rolledback;
        }

        public Task RollbackAsync(CancellationToken cancellationToken = new())
        {
            Status = TransactionStatus.Rolledback;
            return Task.CompletedTask;
        }

        public Guid TransactionId { get; }
    }

    public class FakeBuildingGrbContextFactory : IDesignTimeDbContextFactory<FakeBuildingGrbContext>
    {
        private readonly bool _canBeDisposed;
        private readonly string _databaseName;

        public FakeBuildingGrbContextFactory(
            string? databaseName = null,
            bool canBeDisposed = true)
        {
            _canBeDisposed = canBeDisposed;
            _databaseName = !string.IsNullOrWhiteSpace(databaseName)
                ? databaseName : Guid.NewGuid().ToString();
        }

        public FakeBuildingGrbContext CreateDbContext(params string[] args)
        {
            var options = new DbContextOptionsBuilder<BuildingGrbContext>()
                .UseInMemoryDatabase(_databaseName)
                .Options;

            return new FakeBuildingGrbContext(options, _canBeDisposed);
        }
    }
}
