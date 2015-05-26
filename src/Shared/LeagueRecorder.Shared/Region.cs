using System;
using System.Linq;

namespace LeagueRecorder.Shared
{
    public abstract class Region
    {
        #region Values
        public static readonly Region NorthAmerica = new NorthAmericaRegion();
        public static readonly Region EuropeWest = new EuropeWestRegion();
        public static readonly Region EuropeNordicAndEast = new EuropeNordicAndEastRegion();
        public static readonly Region Korea = new KoreaRegion();
        public static readonly Region Oceanic = new OceanicRegion();
        public static readonly Region Brasil = new BrasilRegion();
        public static readonly Region LatinAmericaNorth = new LatinAmericaNorthRegion();
        public static readonly Region LatinAmericaSouth = new LatinAmericaSouthRegion();
        public static readonly Region Russia = new RussiaRegion();
        public static readonly Region Turkey = new TurkeyRegion();

        public static readonly Region[] All = new Region[]
        {
            NorthAmerica,
            EuropeWest,
            EuropeNordicAndEast,
            Korea,
            Oceanic,
            Brasil,
            LatinAmericaNorth,
            LatinAmericaSouth,
            Russia,
            Turkey
        };
        #endregion

        #region Constructors
        /// <summary>
        /// Prevents a outside user of this class to create instances of it.
        /// Only nested classes are valid.
        /// </summary>
        private Region()
        {
            
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets the spectator URL.
        /// </summary>
        public abstract string SpectatorUrl { get; }
        /// <summary>
        /// Gets the spectator port.
        /// </summary>
        public abstract int SpectatorPort { get; }
        /// <summary>
        /// Gets the spectator platform identifier.
        /// </summary>
        public abstract string SpectatorPlatformId { get; }
        /// <summary>
        /// Gets the riot API platform identifier.
        /// </summary>
        public abstract string RiotApiPlatformId { get; }
        #endregion

        #region Methods
        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        public override string ToString()
        {
            return this.RiotApiPlatformId;
        }
        #endregion

        #region Static Methods
        /// <summary>
        /// Creates a region using the specified <paramref name="platformId"/> for the Riot Api.
        /// </summary>
        /// <param name="platformId">The platform identifier.</param>
        public static Region FromString(string platformId)
        {
            return Region.All.FirstOrDefault(f => 
                string.Equals(f.RiotApiPlatformId, platformId, StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals(f.SpectatorPlatformId, platformId, StringComparison.InvariantCultureIgnoreCase));
        }
        #endregion

        #region Internal
        private class NorthAmericaRegion : Region
        {
            public override string SpectatorUrl
            {
                get { return "spectator.na.lol.riotgames.com"; }
            }

            public override int SpectatorPort
            {
                get { return 80; }
            }

            public override string SpectatorPlatformId
            {
                get { return "NA1"; }
            }

            public override string RiotApiPlatformId
            {
                get { return "na"; }
            }
        }
        private class EuropeWestRegion : Region
        {
            public override string SpectatorUrl
            {
                get { return "spectator.euw1.lol.riotgames.com"; }
            }

            public override int SpectatorPort
            {
                get { return 80; }
            }

            public override string SpectatorPlatformId
            {
                get { return "EUW1"; }
            }

            public override string RiotApiPlatformId
            {
                get { return "euw"; }
            }
        }
        private class EuropeNordicAndEastRegion : Region
        {
            public override string SpectatorUrl
            {
                get { return "spectator.eu.lol.riotgames.com"; }
            }

            public override int SpectatorPort
            {
                get { return 8088; }
            }

            public override string SpectatorPlatformId
            {
                get { return "EUN1"; }
            }

            public override string RiotApiPlatformId
            {
                get { return "eune"; }
            }
        }
        private class KoreaRegion : Region
        {
            public override string SpectatorUrl
            {
                get { return "spectator.kr.lol.riotgames.com"; }
            }

            public override int SpectatorPort
            {
                get { return 80; }
            }

            public override string SpectatorPlatformId
            {
                get { return "KR"; }
            }

            public override string RiotApiPlatformId
            {
                get { return "kr"; }
            }
        }
        private class OceanicRegion : Region
        {
            public override string SpectatorUrl
            {
                get { return "spectator.oc1.lol.riotgames.com"; }
            }

            public override int SpectatorPort
            {
                get { return 80; }
            }

            public override string SpectatorPlatformId
            {
                get { return "OC1"; }
            }

            public override string RiotApiPlatformId
            {
                get { return "oce"; }
            }
        }
        private class BrasilRegion : Region
        {
            public override string SpectatorUrl
            {
                get { return "spectator.br.lol.riotgames.com"; }
            }

            public override int SpectatorPort
            {
                get { return 80; }
            }

            public override string SpectatorPlatformId
            {
                get { return "BR1"; }
            }

            public override string RiotApiPlatformId
            {
                get { return "br"; }
            }
        }
        private class LatinAmericaNorthRegion : Region
        {
            public override string SpectatorUrl
            {
                get { return "spectator.la1.lol.riotgames.com"; }
            }

            public override int SpectatorPort
            {
                get { return 80; }
            }

            public override string SpectatorPlatformId
            {
                get { return "LA1"; }
            }

            public override string RiotApiPlatformId
            {
                get { return "lan"; }
            }
        }
        private class LatinAmericaSouthRegion : Region
        {
            public override string SpectatorUrl
            {
                get { return "spectator.la2.lol.riotgames.com"; }
            }

            public override int SpectatorPort
            {
                get { return 80; }
            }

            public override string SpectatorPlatformId
            {
                get { return "LA2"; }
            }

            public override string RiotApiPlatformId
            {
                get { return "las"; }
            }
        }
        private class RussiaRegion : Region
        {
            public override string SpectatorUrl
            {
                get { return "spectator.ru.lol.riotgames.com"; }
            }

            public override int SpectatorPort
            {
                get { return 80; }
            }

            public override string SpectatorPlatformId
            {
                get { return "RU"; }
            }

            public override string RiotApiPlatformId
            {
                get { return "ru"; }
            }
        }
        private class TurkeyRegion : Region
        {
            public override string SpectatorUrl
            {
                get { return "spectator.tr.lol.riotgames.com"; }
            }

            public override int SpectatorPort
            {
                get { return 80; }
            }

            public override string SpectatorPlatformId
            {
                get { return "TR1"; }
            }

            public override string RiotApiPlatformId
            {
                get { return "tr"; }
            }
        }
        #endregion
    }
}