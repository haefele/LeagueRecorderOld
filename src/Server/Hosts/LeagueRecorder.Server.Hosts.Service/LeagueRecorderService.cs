using LeagueRecorder.Server.Infrastructure;

namespace LeagueRecorder.Server.Hosts.Service
{
    public class LeagueRecorderService
    {
        #region Fields
        private Bootstrapper _bootstrapper;
        #endregion

        #region Methods
        public void Start()
        {
            this._bootstrapper = new Bootstrapper();
            this._bootstrapper.Start();
        }

        public void Stop()
        {
            this._bootstrapper.Dispose();
        }
        #endregion
    }
}