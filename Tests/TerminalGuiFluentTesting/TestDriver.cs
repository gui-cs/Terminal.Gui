using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerminalGuiFluentTesting;

/// <summary>
/// Which driver simulation should be used for testing
/// </summary>
public enum TestDriver
{
    /// <summary>
    /// The Windows driver with simulation I/O but core driver classes
    /// </summary>
    Windows,

    /// <summary>
    /// The DotNet driver with simulation I/O but core driver classes
    /// </summary>
    DotNet
}
