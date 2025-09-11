using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Interfaces
{
    public interface IRelmModelApartment
    {
        string ApartmentId { get; set; }
        int UserId { get; set; }
        string UserEmail { get; set; }
        string UserName { get; set; }
        IRelmMember Member { get; set; }
    }
}
