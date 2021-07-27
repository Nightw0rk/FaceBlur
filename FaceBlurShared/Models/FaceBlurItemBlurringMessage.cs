using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FaceBlurShared.Models
{
    public class FaceBlurItemBlurringMessage
    {
        public string Id { get; set; }
        public List<Rectangle> faces { get; set; }
    }
}
