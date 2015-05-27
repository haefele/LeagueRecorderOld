using FluentNHibernate.Mapping;

namespace LeagueRecorder.Server.Infrastructure.Extensions
{
    public static class PropertyPartExtensions
    {
        public static PropertyPart MaxLength(this PropertyPart map)
        {
            return map.Length(10000);
        }
    }
}