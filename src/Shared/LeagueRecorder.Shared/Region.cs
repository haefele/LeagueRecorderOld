using System;
using System.Linq;

namespace LeagueRecorder.Shared.League
{
    public abstract class Region
    {
        #region Values
        public static readonly Region NorthAmerica = new NorthAmericaRegion();
        public static readonly Region EuropeWest = new EuropeWestRegion();
        public static readonly Region EuropeNordicAndEast = new EuropeNordicAndEastRegion();
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
            var allRegions = new[]
            {
                NorthAmerica,
                EuropeWest,
                EuropeNordicAndEast
            };

            return allRegions.FirstOrDefault(f => 
                string.Equals(f.RiotApiPlatformId, platformId, StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals(f.SpectatorPlatformId, platformId, StringComparison.InvariantCultureIgnoreCase));
        }
        #endregion

        #region Internal
        private class NorthAmericaRegion : Region
        {
            public override string SpectatorUrl
            {
                get { return "spectator.na1.lol.riotgames.com"; }
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
        #endregion

        #region Other Regions
        //Brazil
        //LatinAmericaNorth
        //LatinAmericaSouth
        //Russia
        //Turkey
        //Korea
        //Taiwan
        //Oceania
        //Singapore
        #endregion
    }
}