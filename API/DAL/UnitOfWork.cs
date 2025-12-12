using Dapper;
using Microsoft.Extensions.Options;
using Npgsql;
using Microsoft.Extensions.Logging;
using API.DAL;

namespace API.DAL
{
    public interface IUnitOfWork : IAsyncDisposable, IDisposable
    {
        Task<NpgsqlConnection> GetConnection(CancellationToken token);
        Task<NpgsqlTransaction> BeginTransactionAsync(CancellationToken token);
    }

    public class UnitOfWork : IUnitOfWork
    {
        private readonly string _connectionString;
        private readonly ILogger<UnitOfWork> _logger;
        private NpgsqlConnection _connection;
        private bool _disposed;
        private static NpgsqlDataSource _dataSource;
        private static readonly object _dataSourceLock = new();
        private readonly SemaphoreSlim _connectionLock = new(1, 1);

        public UnitOfWork(
            IOptions<DbSettings> dbSettings,
            ILogger<UnitOfWork> logger)
        {
            _connectionString = dbSettings?.Value?.ConnectionString 
                ?? throw new ArgumentNullException(nameof(dbSettings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            InitializeDataSource();
        }

        private void InitializeDataSource()
        {
            if (_dataSource != null) 
                return;

            lock (_dataSourceLock)
            {
                if (_dataSource != null) 
                    return;

                _logger.LogInformation("Initializing PostgreSQL DataSource");
                
                var builder = new NpgsqlDataSourceBuilder(_connectionString);
                
                builder.EnableDynamicJson();
                
                
                _dataSource = builder.Build();
            }
        }

        public async Task<NpgsqlConnection> GetConnection(CancellationToken token)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UnitOfWork));

            await _connectionLock.WaitAsync(token);
            
            try
            {
                if (_connection != null && _connection.State == System.Data.ConnectionState.Open)
                {
                    return _connection;
                }

                if (_connection != null)
                {
                    try
                    {
                        await _connection.CloseAsync();
                        await _connection.DisposeAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error closing old connection");
                    }
                    finally
                    {
                        _connection = null;
                    }
                }

                _connection = await _dataSource.OpenConnectionAsync(token);
                return _connection;
            }
            finally
            {
                _connectionLock.Release();
            }
        }

        public async Task<NpgsqlTransaction> BeginTransactionAsync(CancellationToken token)
        {
            var connection = await GetConnection(token);
            return await connection.BeginTransactionAsync(token);
        }

        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore().ConfigureAwait(false);
            GC.SuppressFinalize(this);
        }

        protected virtual async ValueTask DisposeAsyncCore()
        {
            if (_disposed)
                return;

            _disposed = true;

            if (_connection != null)
            {
                try
                {
                    await _connection.CloseAsync();
                    await _connection.DisposeAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error disposing connection");
                }
                finally
                {
                    _connection = null;
                }
            }

            _connectionLock?.Dispose();
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                // Синхронное освобождение ресурсов
                try
                {
                    // Пытаемся асинхронно освободить, но синхронно
                    DisposeAsync().AsTask().GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error during synchronous dispose");
                }
            }

            _disposed = true;
        }

        ~UnitOfWork()
        {
            Dispose(disposing: false);
        }
    }
}