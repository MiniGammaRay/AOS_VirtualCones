using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Talos.Models
{
    public class MLCInfo
    {
        public string MLCID { get; set; }
        public string Model { get; set; }
        public string Manufacturer { get; set; }
        public string SN { get; set; }

        public static float[,] LeafPositions(int banks, int leaves)
        {
            // start with the MLC120
            float[,] values = new float[banks, leaves];

            for (int i = 0; i < banks; i++)
                for (int j = 0; j < leaves; j++)
                    values[banks - 1, leaves - 1] = 0.0f;

            return values;
        }
    }
}
