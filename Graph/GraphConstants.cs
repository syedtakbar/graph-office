using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace graph_office.Graph
{
    public static class GraphConstants
    {
        // Defines the permission scopes used by the app
        public readonly static string[] Scopes =
        {
            "User.Read",
            "MailboxSettings.Read",
            "Calendars.ReadWrite"
        };
    }
    
}
