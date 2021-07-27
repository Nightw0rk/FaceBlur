using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FaceBlur.Models
{
    public class UserFaceBlurResponseModel
    {
        public string taskId { get; set; }
        public string resultUrl { get; set; }
    }
    public class UserFaceBlurResponse : Abstract.AbstractResponse<UserFaceBlurResponseModel>
    {

    }
}
