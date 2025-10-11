using SqlSugar;

namespace BPFTP.Models
{
    [SugarTable("ApplicationSettings")]
    public class ApplicationSetting
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }

        [SugarColumn]
        public SettingKey Key { get; set; }
        [SugarColumn]
        public string? Value { get; set; }
    }
}
