using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerminalGuiFluentTesting;

/// <summary>
/// Which v2 driver simulation should be used
/// </summary>
public enum V2TestDriver
{
    /// <summary>
    /// The v2 windows driver with simulation I/O but core driver classes
    /// </summary>
    V2Win,

    /// <summary>
    /// The v2 net driver with simulation I/O but core driver classes
    /// </summary>
    V2Net
}
