using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Interfaces
{
    public interface IRelmMember
    {
        long Id { get; set; }
        string Name { get; set; }
        string Email { get; set; }
        string Login { get; set; }

        T GetValue<T>();
        void SetValue<T>(T value);
    }
}
