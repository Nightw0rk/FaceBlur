using FaceBlurShared.Services.Factory.Store.Implentation;
using FaceBlurShared.Services.Interfaces;
using System;

namespace FaceBlurShared.Services.Factory.Store
{
    public class FaceBlurStoreFactory
    {
        public static IFaceBlurStore CreateInstance(string type, string connectionString)
        {
            switch(type)
            {
                case "Mongo":
                    return new FaceBlurStoreMongo(connectionString);
                default:
                    throw new NotImplementedException();
            }
            
        }
    }
}
