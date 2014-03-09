using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace FluentMigrator.Runner.Announcers
{
    /// <summary>
    /// Announcer wwrapper to replace connection string passwords in all messages with  "XXXXX"
    /// </summary>
    public sealed class HidePasswordsAnnouncer : IAnnouncer
    {
        private readonly IAnnouncer announcer;
        private static readonly Regex matchPwd = new Regex("(PWD=|PASSWORD=)([^;]*);", RegexOptions.IgnoreCase);

        HidePasswordsAnnouncer(IAnnouncer announcer)
        {
            if (announcer == null) throw new ArgumentNullException("announcer");
            this.announcer = announcer;
        }

        private string FilterMessage(string message)
        {
            return matchPwd.Replace(message, "$1XXXX;");
        }

        public void Dispose()
        {
            announcer.Dispose();
        }

        public void Heading(string message)
        {
            announcer.Heading(FilterMessage(message));
        }

        public void Say(string message)
        {
            announcer.Say(FilterMessage(message));
        }

        public void Emphasize(string message)
        {
            announcer.Emphasize(FilterMessage(message));
        }

        public void Sql(string sql)
        {
            announcer.Sql(FilterMessage(sql));
        }

        public void ElapsedTime(TimeSpan timeSpan)
        {
            announcer.ElapsedTime(timeSpan);
        }

        public void Error(string message)
        {
            announcer.Error(FilterMessage(message));
        }

        public void Write(string message, bool escaped)
        {
            announcer.Write(FilterMessage(message), escaped);
        }
    }
}
