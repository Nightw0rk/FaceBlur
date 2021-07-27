using FaceBlurShared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FaceBlurShared.Services.Interfaces
{
    public interface IFaceBlurStore
    {
        Task<FaceBlurItem> Get(string Id);
        Task<FaceBlurItem> GetByUrl(Uri uri);
        Task<FaceBlurItem> Create(Uri uri);
        Task<FaceBlurItem> Update(FaceBlurItem item);
        Task<FaceBlurItem> GetByHash(string hash);
    }
}
