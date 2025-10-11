using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SqlSugar;
using System.IO;
using BPFTP.Models;

namespace BPFTP.Services
{
    public class DatabaseService
    {
        private readonly SqlSugarScope _db;

        public DatabaseService()
        {
            var dbPath = Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
                "BP",
                "BP.db");

            var dbDir = Path.GetDirectoryName(dbPath);
            if (!Directory.Exists(dbDir))
            {
                Directory.CreateDirectory(dbDir!);
            }

            _db = new SqlSugarScope(new ConnectionConfig()
            {
                ConnectionString = $"Data Source={dbPath}",
                DbType = DbType.Sqlite,
                IsAutoCloseConnection = true
            });

            _db.DbMaintenance.CreateDatabase();
            _db.CodeFirst.InitTables<ConnectionProfile>();
            _db.CodeFirst.InitTables<ApplicationSetting>();
        }

        public Task<List<ConnectionProfile>> GetConnectionsAsync() => _db.Queryable<ConnectionProfile>().ToListAsync();

        public Task<int> SaveConnectionAsync(ConnectionProfile profile) => _db.Storageable(profile).ExecuteCommandAsync();

        public Task<int> DeleteConnectionAsync(int id) => _db.Deleteable<ConnectionProfile>().In(id).ExecuteCommandAsync();

        public async Task<string?> GetSettingAsync(SettingKey key)
        {
            return (await _db.Queryable<ApplicationSetting>().FirstAsync(it => it.Key == key))?.Value;
        }

        public async Task<int> SaveSettingAsync(SettingKey key, string value)
        {
            var setting = (await _db.Queryable<ApplicationSetting>().Where(X => X.Key == key).FirstAsync()) ?? new();
            setting.Value = value;
            return await _db.Storageable(setting).WhereColumns(it => it.Key).ExecuteCommandAsync();
        }

    }

}
