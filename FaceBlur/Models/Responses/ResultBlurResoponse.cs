using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FaceBlurShared.Models;

namespace FaceBlur.Models.Responses
{
    
    public class ResultBlurResoponse : Abstract.AbstractResponse<ResultBlurResoponseModel>
    {
        public static ResultBlurResoponse fromFaceBlurItem(FaceBlurItem item)
        {
            return new ResultBlurResoponse()
            {
                success = item != null,
                message = item == null ? "" : item.errorMessage,
                result = item == null ? null : new ResultBlurResoponseModel()
                {
                    status = item.status.ToString("g"),
                    jobId = item.Id,
                    proccessedTime = item.processTime ,
                    resultUrl = item.publicUrl == null ? null : item.publicUrl.ToString()
                }
            };
        }
    }
}
