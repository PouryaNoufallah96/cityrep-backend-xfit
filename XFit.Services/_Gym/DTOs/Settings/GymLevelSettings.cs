using Xfit.Domain.Common;

namespace XFit.Services._Gym.DTOs.Settings
{
    public class GymLevelSettings : List<GymLevelData>
    {

    }


    public class GymLevelData
    {
        public GymLevel Level { get; set; } 
        public string Title { get; set; }
        public decimal Price { get; set; }

    }


}
