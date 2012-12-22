using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Launchy
{
    public interface IEntry
    {
        void Execute();
        string Command { get; set; }
        string Title { get; set; }
    }
}
