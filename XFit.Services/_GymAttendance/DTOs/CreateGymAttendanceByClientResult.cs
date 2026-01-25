using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xfit.Domain.Collections;

namespace XFit.Services._GymAttendance.DTOs
{
    public class CreateGymAttendanceByClientResult
    {
        public GymAttendanceState State { get; set; }
        public string GatewayUrl { get; set; }
        public decimal Remain { get; set; }
    }

}
