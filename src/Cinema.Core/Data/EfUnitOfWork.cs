using System;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;
using Cinema.Core.Abstractions;

namespace Cinema.Core.Data
{
    /// <summary>
    /// Commits the request-scoped context and maps database conflicts to domain exceptions:
    /// a stale <c>Screening.SeatVersion</c> becomes <see cref="ConcurrencyConflictException"/>, and a
    /// violation of the live-seat unique index becomes <see cref="SeatUnavailableException"/>.
    /// </summary>
    public class EfUnitOfWork : IUnitOfWork
    {
        private const int SqlUniqueIndexViolation = 2601;
        private const int SqlUniqueConstraintViolation = 2627;

        private readonly CinemaDbContext _db;

        public EfUnitOfWork(CinemaDbContext db)
        {
            _db = db;
        }

        public void SaveChanges()
        {
            try
            {
                _db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConcurrencyConflictException();
            }
            catch (DbUpdateException ex)
            {
                if (IsLiveSeatViolation(ex))
                {
                    throw new SeatUnavailableException(Enumerable.Empty<int>());
                }

                throw;
            }
        }

        private static bool IsLiveSeatViolation(Exception ex)
        {
            for (var inner = ex; inner != null; inner = inner.InnerException)
            {
                var sql = inner as SqlException;
                if (sql != null
                    && (sql.Number == SqlUniqueIndexViolation || sql.Number == SqlUniqueConstraintViolation)
                    && sql.Message.IndexOf("UX_BookedSeat_Live", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
