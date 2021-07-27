using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FaceBlurShared.Models
{
    public class ResultBlurResoponseModel
    {
        public string status { get; set; }
        public string jobId { get; set; }
        public long proccessedTime { get; set; }
        public string resultUrl { get; set; }
    }
}
