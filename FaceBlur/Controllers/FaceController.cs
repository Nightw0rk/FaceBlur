using FaceBlur.Models;
using FaceBlur.Models.Responses;
using FaceBlurShared.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FaceBlur.Controllers
{
    [Route("api/face")]
    [ApiController]
    public class FaceController : ControllerBase
    {
        public FaceController(IFaceBlurStore store, IFaceBlurQueue queu)
        {
            Store = store;
            Queue = queu;
        }

        public IFaceBlurStore Store { get; }
        public IFaceBlurQueue Queue { get; }

        [HttpGet("result/{id}", Name = "Result")]
        public async Task<ActionResult<ResultBlurResoponse>> Get(string id)
        {
            try
            {
                var faceblurItem = await Store.Get(id);
                return ResultBlurResoponse.fromFaceBlurItem(faceblurItem);
            } catch(Exception error)
            {
                return new ResultBlurResoponse()
                {
                    success = false,
                    message = error.Message
                };
            }
        }

        [HttpPost("blur")]
        public async Task<ActionResult<UserFaceBlurResponse>> Post([FromBody] UserFaceBlurRequestModel value)
        {
            try
            {
                var uri = new Uri(value.url);
                var faceBluritem =await Store.GetByUrl(uri);
                if (faceBluritem == null)
                {

                    faceBluritem= await Store.Create(uri);
                    Queue.Push(faceBluritem);
                }
                return new UserFaceBlurResponse()
                {
                    success = true,
                    message = "",
                    result = new UserFaceBlurResponseModel()
                    {
                        taskId = faceBluritem.Id,
                        resultUrl = $"{Request.Scheme}://{Request.Host}{Url.RouteUrl("Result", new { id = faceBluritem.Id })}"
                    }
                };
            }
            catch (Exception error)
            {
                return new UserFaceBlurResponse()
                {
                    success = false,
                    message = error.Message
                };
            }
        }
    }
}
