using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FaceBlur.Models.Abstract
{
    public abstract class AbstractResponse<T>
    {
        public bool success { get; set; }
        public string message { get; set; }

        public T result { get; set; }
    }
}
