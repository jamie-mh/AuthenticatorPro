using ProAuth.Data;
using SQLite;

namespace ProAuth
{
    class ImplementationSource
    {
        private readonly SQLiteConnection _connection;

        public ImplementationSource(SQLiteConnection connection)
        {
            _connection = connection;
        }

        public Implementation Get(int id)
        {
            return _connection.Get<Implementation>(id);
        }
    }
}